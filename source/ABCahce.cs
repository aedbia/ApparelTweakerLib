using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace ABEasyLib
{
    namespace ABCache
    {

        public class MultithreadCacheExpirated<K, T> : IDisposable
        {
            private class CacheItem<S>
            {
                public T Value;
                public DateTime? AbsoluteExpiration;
                public TimeSpan? SlidingExpiration;
                public DateTime LastAccessTime;
                public LinkedListNode<K> ListNode;
            }
            private readonly ConcurrentDictionary<K, CacheItem<T>> _cache;
            private readonly LinkedList<K> _accessList = new LinkedList<K>();
            private readonly int _maxCapacity;
            private readonly Timer _cleanupTimer;
            private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
            private long Hits;
            private long Misses;
            private long Evictions;
            private long Expired;

            public MultithreadCacheExpirated(int maxCapacity = int.MaxValue, TimeSpan? cleanupInterval = null)
            {
                _maxCapacity = maxCapacity;
                _cache = new ConcurrentDictionary<K, CacheItem<T>>();
                if (cleanupInterval != null)
                {
                    TimeSpan interval = (TimeSpan)cleanupInterval;
                    _cleanupTimer = new Timer(CleanExpiredItemsCallback, null, interval, interval);
                }

            }

            public void Set(K key, T value,
                TimeSpan? slidingExpiration = null,
                DateTime? absoluteExpiration = null)
            {
                var now = DateTime.UtcNow;
                var item = new CacheItem<T>
                {
                    Value = value,
                    AbsoluteExpiration = absoluteExpiration,
                    SlidingExpiration = slidingExpiration,
                    LastAccessTime = now
                };

                _lock.EnterWriteLock();
                try
                {
                    if (_cache.TryGetValue(key, out var existing))
                    {
                        if (existing.ListNode != null && existing.ListNode.List == _accessList)
                        {
                            _accessList.Remove(existing.ListNode);
                        }
                    }

                    _cache[key] = item;
                    var newNode = _accessList.AddFirst(key);
                    item.ListNode = newNode;

                    EvictIfNeeded();
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
            public bool TryGet(K key, out T value)
            {
                value = default;
                bool found = false;
                bool expired = false;
                CacheItem<T> item = null;

                _lock.EnterUpgradeableReadLock();
                try
                {
                    if (_cache.TryGetValue(key, out item))
                    {
                        found = true;
                        var now = DateTime.UtcNow;
                        expired = IsExpired(item, now);

                        if (expired)
                        {
                            Interlocked.Increment(ref Expired);
                            Interlocked.Increment(ref Misses);
                        }
                        else
                        {
                            if (item.SlidingExpiration.HasValue)
                            {
                                item.LastAccessTime = now;
                            }

                            _lock.EnterWriteLock();
                            try
                            {
                                if (item.ListNode != null && item.ListNode.List == _accessList)
                                {
                                    _accessList.Remove(item.ListNode);
                                    _accessList.AddFirst(item.ListNode);
                                }
                            }
                            finally
                            {
                                _lock.ExitWriteLock();
                            }

                            value = item.Value;
                            Interlocked.Increment(ref Hits);
                            return true;
                        }
                    }
                }
                finally
                {
                    _lock.ExitUpgradeableReadLock();
                }
                if (found && expired)
                {
                    _lock.EnterWriteLock();
                    try
                    {
                        if (_cache.TryGetValue(key, out item) && IsExpired(item, DateTime.UtcNow))
                        {
                            RemoveItem(key, item);
                        }
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }
                }

                if (!found)
                {
                    Interlocked.Increment(ref Misses);
                }
                return false;
            }

            private bool IsExpired(CacheItem<T> item, DateTime now)
            {
                if (item.AbsoluteExpiration.HasValue && item.AbsoluteExpiration.Value < now)
                    return true;

                if (item.SlidingExpiration.HasValue &&
                    item.LastAccessTime.Add(item.SlidingExpiration.Value) < now)
                    return true;

                return false;
            }

            public void Remove(K key)
            {
                _lock.EnterWriteLock();
                try
                {
                    if (_cache.TryRemove(key, out var item))
                    {
                        RemoveItem(key, item);
                    }
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }

            private void RemoveItem(K key, CacheItem<T> item)
            {
                if (item.ListNode != null && item.ListNode.List == _accessList)
                {
                    _accessList.Remove(item.ListNode);
                }
            }

            private void EvictIfNeeded()
            {
                if (_cache.Count <= _maxCapacity) return;

                _lock.EnterWriteLock();
                try
                {
                    while (_cache.Count > _maxCapacity && _accessList.Last != null)
                    {
                        var oldestKey = _accessList.Last.Value;
                        if (_cache.TryRemove(oldestKey, out var item))
                        {
                            _accessList.RemoveLast();
                            Interlocked.Increment(ref Evictions);
                        }
                    }
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }

            private void CleanExpiredItemsCallback(object state)
            {
                CleanExpiredItems();
            }

            private void CleanExpiredItems()
            {
                var now = DateTime.UtcNow;
                var expiredKeys = new List<K>();

                _lock.EnterReadLock();
                try
                {
                    foreach (var kvp in _cache)
                    {
                        if (IsExpired(kvp.Value, now))
                        {
                            expiredKeys.Add(kvp.Key);
                        }
                    }
                }
                finally
                {
                    _lock.ExitReadLock();
                }

                if (expiredKeys.Count > 0)
                {
                    _lock.EnterWriteLock();
                    try
                    {
                        foreach (var key in expiredKeys)
                        {
                            if (_cache.TryGetValue(key, out var item) &&
                                IsExpired(item, DateTime.UtcNow))
                            {
                                RemoveItem(key, item);
                                Interlocked.Increment(ref Expired);
                            }
                        }
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }
                }
            }

            public void Clear()
            {
                _lock.EnterWriteLock();
                try
                {
                    _cache.Clear();
                    _accessList.Clear();
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }

            public int Count => _cache.Count;

            public void Dispose()
            {
                _lock?.Dispose();
                _cleanupTimer?.Dispose();
            }
        }

        public class MultithreadCache<S, T> : IDisposable
        {
            private readonly ConcurrentDictionary<S, T> _cache = new ConcurrentDictionary<S, T>();
            private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
            public void Set(S key, T value)
            {
                _lock.EnterWriteLock();
                try
                {
                    if (_cache.ContainsKey(key))
                    {
                        _cache[key] = value;
                    }
                    else
                    {
                        _cache.TryAdd(key, value);
                    }

                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }

            public bool TryGet(S key, out T value)
            {
                _lock.EnterReadLock();
                try
                {
                    if (_cache.TryGetValue(key, out value))
                    {
                        return true;
                    }
                    return false;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
            public bool ContainKey(S key)
            {
                _lock.EnterReadLock();
                try
                {
                    if (_cache.ContainsKey(key))
                    {
                        return true;
                    }
                    return false;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
            public bool Remove(S key)
            {
                _lock.EnterWriteLock();
                try
                {
                    return _cache.TryRemove(key, out _);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }

            public void Clear()
            {
                _lock.EnterWriteLock();
                try
                {
                    _cache.Clear();
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
            public void Dispose()
            {
                _lock?.Dispose();
            }

            public int Count => _cache.Count;

            public IEnumerable<KeyValuePair<S, T>> GetAllItems()
            {
                _lock.EnterReadLock();
                try
                {
                    return new List<KeyValuePair<S, T>>(_cache);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }
    }
}
