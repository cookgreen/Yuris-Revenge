using OpenRA.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace OpenRA.Mods.YR.Graphics
{
    /// <summary>
    /// Mostly Used by Yuri Magnetic Tank
    /// </summary>
    public class RectangleRenderable : IRenderable, IFinalizedRenderable
    {
        private const int RECTANGLE_WIDTH = 10;
        private WPos start; //magnetic tank position
        private WPos end;   //target position
        private Color color;//color
        public RectangleRenderable(WPos start, WPos end, Color color)
        {
            this.start = start;
            this.end = end;
            this.color = color;
        }
        public bool IsDecoration
        {
            get
            {
                return true;
            }
        }

        public PaletteReference Palette
        {
            get
            {
                return null;
            }
        }

        public WPos Pos
        {
            get
            {
                return start;
            }
        }

        public int ZOffset
        {
            get
            {
                return 0;
            }
        }

        public IRenderable AsDecoration()
        {
            return new RectangleRenderable(start, end, color);
        }

        public IRenderable OffsetBy(WVec offset)
        {
            return new RectangleRenderable(start, end, color);
        }

        public IFinalizedRenderable PrepareRender(WorldRenderer wr)
        {
            return this;
        }

        public void Render(WorldRenderer wr)
        {
            float startRectPosX;
            float startRectPosY;
            float endRectPosX;
            float endRectPosY;
            
            float2 startToEndVect = new float2(start.X - end.X, start.Y - end.Y);
            float vectLength = startToEndVect.Length;
            double angle = Math.Atan((start.Y - end.Y) / (start.X - end.X));
            double theta = 90 - angle;

            startRectPosX = (float)(start.X + RECTANGLE_WIDTH / 2 * Math.Cos(theta));
            startRectPosY = (float)(start.Y - RECTANGLE_WIDTH / 2 * Math.Sin(theta));
            endRectPosX = (float)(end.X - RECTANGLE_WIDTH / 2 * Math.Cos(angle));
            endRectPosY = (float)(end.Y + RECTANGLE_WIDTH / 2 * Math.Sin(angle));

            float3 startPos = new float3(startRectPosX, startRectPosY, start.Z);
            float3 endPos = new float3(endRectPosX, endRectPosY, end.Z);

            //Just draw a rectangle between tank and the target
            Game.Renderer.WorldRgbaColorRenderer.FillRect(startPos, endPos, color);
        }

        public void RenderDebugGeometry(WorldRenderer wr)
        {
        }

        public Rectangle ScreenBounds(WorldRenderer wr)
        {
            return Rectangle.Empty;
        }

        public IRenderable WithPalette(PaletteReference newPalette)
        {
            return new RectangleRenderable(start, end, color);
        }

        public IRenderable WithZOffset(int newOffset)
        {
            return new RectangleRenderable(start, end, color);
        }
    }
}
