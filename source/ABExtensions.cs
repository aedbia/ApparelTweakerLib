using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ABEasyLib
{
    namespace ABExtensions
    {
        public static class ABWidgetsExtensions
        {
            const float SCROLLBAR_WIDTH = 16f;

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
                // 如果列表为空，则不执行任何操作
                if (list.NullOrEmpty())
                {
                    return;
                }

                // 计算内容的总长度
                float contentLength = list.Sum(a =>
                {
                    if (!a.CanDisplay())
                    {
                        return 0;
                    }
                    return (isVertical ? a.Height : a.Width) + 5;

                }) - 5f;

                // 创建视图区域
                float viewWidth = 0;
                float viewHeight = 0;
                float scrollbarOffset = showScrollBar ? SCROLLBAR_WIDTH : 0;

                // 根据滚动方向计算视图区域的宽度和高度
                if (isVertical)
                {
                    viewWidth = contentLength > inRect.height ? Math.Max(inRect.width - scrollbarOffset, 0) : inRect.width;
                    viewHeight = contentLength;
                }
                else
                {
                    viewWidth = contentLength;
                    viewHeight = contentLength > inRect.width ? Math.Max(inRect.height - scrollbarOffset, 0) : inRect.height;
                }

                // 创建视图矩形
                Rect viewRect = new Rect(5, 5, viewWidth, viewHeight);

                // 开始绘制滚动视图
                try
                {
                    Widgets.BeginScrollView(inRect, ref loc, viewRect, showScrollBar);
                    Widgets.BeginGroup(viewRect);

                    float curX = 0;
                    float curY = 0;

                    // 绘制列表项
                    for (int i = 0; i < list.Count; i++)
                    {
                        ScrollViewContent content = list[i];
                        if (IsContentVisible(curX, curY, content, loc, viewRect, isVertical))
                        {
                            Rect contentRect = GetContentRect(curX, curY, content, viewRect, isVertical);
                            content.DrawContect(contentRect);
                        }

                        // 根据滚动方向更新当前位置
                        if (isVertical)
                        {
                            curY += content.Height + 5f;
                        }
                        else
                        {
                            curX += content.Width + 5f;
                        }
                    }
                }
                // 确保滚动视图正确结束
                finally
                {
                    Widgets.EndGroup();
                    Widgets.EndScrollView();
                }
            }

            /// <summary>
            /// 检查内容是否在滚动视图中可见。
            /// </summary>
            /// <param name="curX">当前内容的X轴位置。</param>
            /// <param name="curY">当前内容的Y轴位置。</param>
            /// <param name="content">滚动视图的内容信息。</param>
            /// <param name="loc">滚动视图内容的左上角位置。</param>
            /// <param name="viewRect">滚动视图的可见区域。</param>
            /// <param name="isVertical">滚动视图是否为垂直滚动。</param>
            /// <returns>如果内容在滚动视图中可见，则返回true；否则返回false。</returns>
            private static bool IsContentVisible(float curX, float curY, ScrollViewContent content, Vector2 loc, Rect viewRect, bool isVertical)
            {
                if (isVertical)
                {
                    // 对于垂直滚动，检查内容的Y轴位置是否在视图矩形内
                    return (curY + content.Height >= loc.y && curY <= loc.y + viewRect.height);
                }
                else
                {
                    // 对于水平滚动，检查内容的X轴位置是否在视图矩形内
                    return (curX + content.Width >= loc.x && curX <= loc.x + viewRect.width);
                }
            }

            /// <summary>
            /// 获取内容在滚动视图中的矩形。
            /// </summary>
            /// <param name="curX">当前内容的X轴位置。</param>
            /// <param name="curY">当前内容的Y轴位置。</param>
            /// <param name="content">滚动视图的内容信息。</param>
            /// <param name="viewRect">滚动视图的可见区域。</param>
            /// <param name="isVertical">滚动视图是否为垂直滚动。</param>
            /// <returns>返回内容在滚动视图中的矩形。</returns>
            private static Rect GetContentRect(float curX, float curY, ScrollViewContent content, Rect viewRect, bool isVertical)
            {
                // 根据滚动方向，计算内容的宽度和高度，确保内容的尺寸不会超过视图矩形的尺寸
                return new Rect(
                    curX,
                    curY,
                    isVertical ? Math.Min(content.Width, viewRect.width) : content.Width,
                    isVertical ? content.Height : Math.Min(content.Height, viewRect.height)
                );
            }
            private static readonly string tooltipGrid = "DrawTooltipsGrid";

            public static void DrawTooltipsGrid(Texture[] textures, Vector2 size, int displayCount = 4,bool doBackground = true)
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
            public ScrollViewContent(float width, float height, string id)
            {
                ID = id;
                SCROLL_WIDTH = width;
                SCROLL_HEIGHT = height;
            }

            public virtual void DrawContect(Rect inRect)
            {

            }
            public virtual bool CanDisplay()
            {
                return true;
            }
        }

    }
}
