using ABEasyLib.ABCache;
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

