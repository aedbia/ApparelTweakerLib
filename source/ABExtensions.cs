using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;
using Verse;

namespace ABEasyLib
{
    namespace ABExtensions
    {
        public static class ABWidgetsExtensions
        {
            const float SCROLLBAR_WIDTH = 16f;
            private static string reset = null;
            /// <summary>
            /// 绘制带有滚动条的面板
            /// </summary>
            /// <param name="inRect">面板的矩形区域</param>
            /// <param name="list">要显示的内容列表</param>
            /// <param name="loc">滚动位置</param>
            /// <param name="isVertical">是否垂直滚动，默认为true</param>
            /// <param name="showScrollBar">是否显示滚动条，默认为true</param>
            public static void DrawScrollPanel(Rect inRect, List<ScrollViewContent> list, ref Vector2 loc, bool isVertical = true, bool showScrollBar = true)
            {
                if (list.NullOrEmpty()) return;
                float contentLength = CalculateContentLength(list, isVertical);

                float scrollbarOffset = showScrollBar ? SCROLLBAR_WIDTH : 0;
                Rect viewRect = CreateViewRect(inRect, contentLength, isVertical, scrollbarOffset);
                try
                {
                    Widgets.BeginScrollView(inRect, ref loc, viewRect, showScrollBar);
                    Widgets.BeginGroup(viewRect);

                    float viewMin = isVertical ? loc.y : loc.x;
                    float viewMax = viewMin + (isVertical ? viewRect.height : viewRect.width);

                    float position = 0;
                    int count = list.Count;

                    for (int i = 0; i < count; i++)
                    {
                        ScrollViewContent content = list[i];
                        float size = isVertical ? content.Height : content.Width;

                        if (position + size >= viewMin && position < viewMax)
                        {
                            Rect contentRect = CreateContentRect(position, content, viewRect, isVertical);
                            content.DrawContect(contentRect);
                        }

                        position += size + 5f;
                    }
                }
                finally
                {
                    Widgets.EndGroup();
                    Widgets.EndScrollView();
                }
            }

            // 提取的辅助方法
            private static float CalculateContentLength(List<ScrollViewContent> list, bool isVertical)
            {
                float length = 0;
                int count = list.Count;
                for (int i = 0; i < count; i++)
                {
                    length += (isVertical ? list[i].Height : list[i].Width) + 5;
                }
                return length - 5f;
            }

            private static Rect CreateViewRect(Rect inRect, float contentLength, bool isVertical, float scrollbarOffset)
            {
                if (isVertical)
                {
                    float viewWidth = contentLength > inRect.height ? Mathf.Max(inRect.width - scrollbarOffset, 0) : inRect.width;
                    return new Rect(5, 5, viewWidth, contentLength);
                }
                else
                {
                    float viewHeight = contentLength > inRect.width ? Mathf.Max(inRect.height - scrollbarOffset, 0) : inRect.height;
                    return new Rect(5, 5, contentLength, viewHeight);
                }
            }

            private static Rect CreateContentRect(float position, ScrollViewContent content, Rect viewRect, bool isVertical)
            {
                if (isVertical)
                {
                    return new Rect(0, position, Math.Min(content.Width, viewRect.width), content.Height);
                }
                else
                {
                    return new Rect(position, 0, content.Width, Mathf.Min(content.Height, viewRect.height));
                }
            }
            private static readonly string tooltipGrid = "DrawTooltipsGrid";

            public static void DrawTooltipsGrid(Texture[] textures, Vector2 size, int displayCount = 4, bool doBackground = true)
            {
                //Text.Font = GameFont.Small;
                Vector2 pos = UI.MousePositionOnUIInverted;
                if (pos.x + size.x > UI.screenWidth)
                {
                    float a = pos.x - size.x;
                    pos.x = a > 0 ? a : 0;
                }
                if (pos.y - size.y > 0)
                {
                    pos.y -= size.y;
                }
                Rect bgRect = new Rect(0, 0, size.x, size.y)
                {
                    position = pos
                };
                if (!LongEventHandler.AnyEventWhichDoesntUseStandardWindowNowOrWaiting)
                {
                    Find.WindowStack.ImmediateWindow(153 * tooltipGrid.GetHashCode() + 62346, bgRect, WindowLayer.Super, delegate
                    {
                        DrawInner(bgRect.AtZero(), textures, displayCount);
                    }, doBackground: doBackground);
                    //Log.Warning("000");
                }
                else
                {
                    Widgets.DrawShadowAround(bgRect);
                    Widgets.DrawWindowBackground(bgRect);
                    DrawInner(bgRect, textures, displayCount);
                }
            }
            private static void DrawInner(Rect rect, Texture[] texture, int displayCount)
            {
                Rect rect2 = rect;
                rect2.yMin += 4f;
                rect2.yMax -= 4f;
                if (texture.NullOrEmpty())
                {
                    return;
                }
                int c;
                int cv;
                if (texture.Count() <= displayCount)
                {
                    c = texture.Count();
                    cv = 1;
                }
                else
                {
                    c = displayCount;
                    cv = texture.Count() % displayCount + 1;
                }
                Rect texLoc = new Rect(rect2.x, rect2.y, rect2.width / c, rect2.height / cv);
                for (int a = 0; a < texture.Count(); a++)
                {
                    var tex = texture[a];
                    GUI.DrawTexture(texLoc, tex);
                    if (a + 1 % displayCount == 0)
                    {
                        texLoc.x = rect2.x;
                        texLoc.y += texLoc.height;
                    }
                    else
                    {
                        texLoc.x += texLoc.width;
                    }
                }
            }

            public static bool DrawAdjust(Rect rectAd, string label, ref float x, ref float y, float min, float max, float interval, Action resetAction)
            {
                Rect rectLa = rectAd.TopPart(0.4f);
                Widgets.Label(rectLa.LeftPart(0.7f), label);
                bool work = false;
                if (reset == null)
                {
                    reset = "Reset".Translate();
                }
                if (Widgets.ButtonText(rectLa.RightPart(0.3f).TopHalf(), reset))
                {
                    if (resetAction != null)
                    {
                        resetAction();
                    }
                    work = true;
                }
                Rect rectXYL = rectAd.BottomPart(0.6f).TopHalf();
                if (HorizontalSlider(rectXYL, ref x, min, max, interval))
                {
                    work = true;
                }
                rectXYL.y += rectXYL.height;
                if (HorizontalSlider(rectXYL, ref y, min, max, interval))
                {
                    work = true;
                }
                Widgets.DrawLineHorizontal(rectAd.x + 5f, rectAd.y + rectAd.height, rectAd.width - 5f, Color.gray);
                return work;
            }
            public static bool HorizontalSlider(Rect inRect, ref float x, float min, float max, float interval)
            {
                bool work = false;
                Rect minus = new Rect(inRect.x, inRect.y, inRect.height, inRect.height);
                inRect.x += inRect.height;
                inRect.width -= 2 * inRect.height;
                if (Widgets.ButtonImage(minus, TexButton.Minus))
                {
                    x = x > min ? x - interval : min;
                    work = true;
                }
                    ;
                float x0 = Widgets.HorizontalSlider(inRect, x, min, max);
                if (x != x0)
                {
                    x = x0;
                    work = true;
                }
                minus.x += (inRect.width + inRect.x);
                if (Widgets.ButtonImage(minus, TexButton.Plus))
                {
                    x = x < max ? x + interval : max;
                    work = true;
                }
                return work;
            }
        }

        public abstract class ScrollViewContent
        {
            string ID;
            public virtual string Id
            {
                get
                {
                    return ID;
                }
            }
            float SCROLL_WIDTH;
            public virtual float Width
            {
                get
                {
                    return SCROLL_WIDTH;
                }
            }
            float SCROLL_HEIGHT;
            public virtual float Height
            {
                get
                {
                    return SCROLL_HEIGHT;
                }
            }

            string NAME;
            public virtual string displayName
            {
                get
                {
                    return NAME;
                }
            }
            public ScrollViewContent(float width, float height, string id, string name)
            {
                ID = id;
                SCROLL_WIDTH = width;
                SCROLL_HEIGHT = height;
                NAME =name;
            }

            public virtual void DrawContect(Rect inRect)
            {

            }

            public virtual bool FliterByString(string str)
            {
                return true;
            }
        }

        public static class ABScribeExtensions
        {
            /// <summary>
            /// Only allow vector2, vector3,double and float;
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="values"></param>
            /// <param name="label"></param>
            /// <param name="keepCount"></param>
            /// <param name="defaultValue"></param>
            /// <param name="forceSave"></param>
            public static void Look<T>(ref T values, string label, int keepCount = 0, T defaultValue = default, bool forceSave = false)
            {
                if (Scribe.mode == LoadSaveMode.Saving)
                {
                    if (!forceSave && (values != null || defaultValue == null) && (values == null || values.Equals(defaultValue)))
                    {
                        return;
                    }
                    if (values == null)
                    {
                        if (Scribe.EnterNode(label))
                        {
                            try
                            {
                                Scribe.saver.WriteAttribute("IsNull", "True");
                            }
                            finally
                            {
                                Scribe.ExitNode();
                            }
                        }
                    }
                    else
                    {
                        string keepCountStr = keepCount.ToString();
                        if (values is Vector2 vector2)
                        {
                            string format1 = "({0:F" + keepCountStr + "}, {1:F" + keepCountStr + "})";
                            string a = string.Format(format1, new object[2] { vector2.x, vector2.y });
                            Scribe.saver.WriteElement(label, a);
                        }else
                        if (values is float float0)
                        {
                            string format1 = "{0:F" + keepCountStr + "}";
                            string a = string.Format(format1, new object[1] { float0 });
                            Scribe.saver.WriteElement(label, a);
                        }
                        else
                        if (values is double double0)
                        {
                            string format1 = "{0:F" + keepCountStr + "}";
                            string a = string.Format(format1, new object[1] { double0 });
                            Scribe.saver.WriteElement(label, a);
                        }
                        else
                        if(values is Vector3 vector3)
                        {
                            string format1 = "({0:F" + keepCountStr + "}, {1:F" + keepCountStr + "}, {1:F" + keepCountStr + "})";
                            string a = string.Format(format1, new object[3] { vector3.x, vector3.y,vector3.z});
                            Scribe.saver.WriteElement(label, a);
                        }
                    }
                }
                else if (Scribe.mode == LoadSaveMode.LoadingVars)
                {
                    values = ScribeExtractor.ValueFromNode(Scribe.loader.curXmlParent[label], defaultValue);
                }
            }

            /// <summary>
            /// 读取字典 Load or save 
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="values"></param>
            /// <param name="label"></param>
            public static void LookDeep<T>(ref Dictionary<string,T> values, string label) where T: IExposable
            {
                if (Scribe.EnterNode(label))
                {
                    try
                    {
                        if (Scribe.mode == LoadSaveMode.Saving)
                        {
                            List<string> names = values.Keys.ToList();
                            if (names != null)
                            {
                                foreach (string name in names)
                                {
                                    T target = values[name];
                                    Scribe_Deep.Look(ref target, name);
                                }
                                return;
                            }
                            Scribe.saver.WriteAttribute("IsNull", "True");
                        }
                        else if (Scribe.mode == LoadSaveMode.LoadingVars)
                        {
                            XmlNode curXmlParent = Scribe.loader.curXmlParent;
                            XmlAttribute xmlAttribute = curXmlParent.Attributes["IsNull"];
                            if (xmlAttribute != null && xmlAttribute.Value.Equals("true", StringComparison.InvariantCultureIgnoreCase))
                            {
                                values = null;
                            }
                            else
                            {

                                Dictionary<string, T> list = new Dictionary<string, T>(curXmlParent.ChildNodes.Count);
                                foreach (XmlNode childNode in curXmlParent.ChildNodes)
                                {

                                    string name = childNode.Name;
                                    T a = ScribeExtractor.SaveableFromNode<T>(childNode, null);
                                    list.SetOrAdd(name, a);
                                }
                                values = list;
                            }
                        }
                        return;
                    }catch(Exception ex)
                    { 
                        Log.Warning($"Load or save config {label} failed: "+ex.ToString());
                    }
                    finally
                    {
                        Scribe.ExitNode();
                    }
                }
            }
        }
    }
}
