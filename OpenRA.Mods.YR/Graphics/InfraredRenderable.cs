using OpenRA.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.YR.Graphics
{
    /// <summary>
    /// Mostly used by Soviet Hero - Boris
    /// </summary>
    public class InfraredRenderable : IRenderable, IFinalizedRenderable
    { 
        private WPos start; 
        private WPos end;   
        private Color color;
        public InfraredRenderable(WPos start, WPos end, Color color)
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
            return new InfraredRenderable(start, end, color);
        }

        public IRenderable OffsetBy(WVec offset)
        {
            return new InfraredRenderable(start, end, color);
        }

        public IFinalizedRenderable PrepareRender(WorldRenderer wr)
        {
            return this;
        }

        public void Render(WorldRenderer wr)
        {
            float3 startPos = new float3(start.X, start.Y, start.Z);
            float3 endPos = new float3(end.X, end.Y, end.Z);
            
            Game.Renderer.WorldRgbaColorRenderer.DrawLine(startPos, endPos, 10, color);
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
            return new InfraredRenderable(start, end, color);
        }

        public IRenderable WithZOffset(int newOffset)
        {
            return new InfraredRenderable(start, end, color);
        }
    }
}
