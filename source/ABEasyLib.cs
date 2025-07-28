using ABEasyLib.ABCache;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ABEasyLib
{
    public class ABEasyLibMod : Mod
    {
        public ABEasyLibMod(ModContentPack content) : base(content)
        {

        }
    }
    public class PawnTextureCache
    {
        private static MultithreadCacheExpirated<Pawn,PawnTextureCache> allPawnTextureCache = new MultithreadCacheExpirated<Pawn, PawnTextureCache>();
        public List<Apparel> postApparels = new List<Apparel>();
        public List<Apparel> preApparels = new List<Apparel>();
        public List<Apparel> OverrideApparels = new List<Apparel>();
        private readonly Pawn pawn;
        private PawnTextureCache(Pawn pawn)
        {
             this.pawn = pawn;
        }
        public static bool GetPawnTextureCache(Pawn pawn,out PawnTextureCache cache)
        {
            if (allPawnTextureCache == null)
            {
                allPawnTextureCache = new MultithreadCacheExpirated<Pawn, PawnTextureCache>(cleanupInterval: TimeSpan.FromSeconds(30));
            }
            if (allPawnTextureCache.Count == 0||!allPawnTextureCache.ContainKey(pawn))
            {
                var aa = new PawnTextureCache(pawn);
                allPawnTextureCache.Set(pawn, aa);
            }
            return allPawnTextureCache.TryGet(pawn, out cache);
        }
        public void GetPawnCacheWithApparel(Apparel apparel, Vector2 size, Rot4 direction, out Texture texture)
        {
            if (pawn != null && pawn.apparel != null && pawn.apparel.WornApparel != null)
            {
                pawn.apparel.WornApparel.Add(apparel);
                if (postApparels == null)
                {
                    postApparels = new List<Apparel>();
                }
                postApparels.Add(apparel);
                pawn.apparel.Notify_ApparelChanged();
                RenderTexture rt = PortraitsCache.Get(pawn, size, direction);
                texture = rt;
                if (postApparels.Contains(apparel))
                {
                    postApparels.Remove(apparel);
                }
                pawn.apparel.WornApparel.Remove(apparel);
                pawn.apparel.Notify_ApparelChanged();
                return;
            }
            texture = null;
        }

        public List<Apparel> GetApparelList(List<Apparel> origin)
        {
            if (OverrideApparels.NullOrEmpty())
            {
                var list = pawn.apparel.WornApparel;
                if (list!= origin)
                {
                    return origin;
                }
                return list;
            }
            else
            {
                return OverrideApparels;
            }
        }
    }
    public static class ABEasyUtility
    {
        private static MultithreadCacheExpirated<int, bool> _ColonistsCache = new MultithreadCacheExpirated<int, bool>(cleanupInterval: TimeSpan.FromMinutes(1));

        static ABEasyUtility()
        {
            if (_ColonistsCache == null)
            {
                _ColonistsCache = new MultithreadCacheExpirated<int, bool>(cleanupInterval: TimeSpan.FromMinutes(1));
            }
        }
        public static bool IsColonist(Pawn pawn)
        {

            if (pawn == null)
            {
                return false;
            }
            int a = pawn.GetHashCode();
            if (_ColonistsCache.Count == 0 || !_ColonistsCache.TryGet(a, out bool ac))
            {
                bool isc = pawn.IsColonist;
                _ColonistsCache.Set(a, isc);
                return isc;
            }
            else
            {
                return _ColonistsCache.TryGet(a, out var isc) && isc;

            }
        }
        public static GUIStyle GetTextStyle(TextAnchor anchor)
        {
            GUIStyle style = Text.CurFontStyle;
            return new GUIStyle(style)
            {
                alignment = anchor
            };
        }
    }

    public abstract class ABGraphicData
    {
        private Type WorkNode;
        private Def def;
        public bool isShowing;
        public Vector3 northOffset;
        public Vector3 southOffset;
        public Vector3 eastOffset;
        public Vector3 westOffset;
        public Vector3 northRot;
        public Vector3 southRot;
        public Vector3 eastORot;
        public Vector3 westRot;
        public ABGraphicData(Type workNode, Def def)
        {
            this.WorkNode = workNode;
            this.def = def;
        }
    }

}

