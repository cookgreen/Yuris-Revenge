#region Copyright & License Information
/*
 * Written by Cook Green of YR Mod
 * Follows GPLv3 License as the OpenRA engine:
 * 
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion
using OpenRA.Graphics;
using OpenRA.Mods.Common.Widgets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.YR.Widgets
{
    public class ResourceBarSpriteWidget : ResourceBarWidget
    {
        public string PowerImageCollection = "power";
        public string PowerHeighImage = "heigh";
        public string PowerMiddleImage = "middle";
        public string PowerLowImage = "low";
        public Func<string> GetPowerState = () => "low";

        protected EWMA providedLerp = new EWMA(0.3f);
        protected EWMA usedLerp = new EWMA(0.3f);
        [ObjectCreator.UseCtor]
        public ResourceBarSpriteWidget(World world) : base(world)
        {
        }

        public override void Draw()
        {
            /*TODO: Calculate the heigh, middle and low percent and show*/
            var scaleBy = 100.0f;
            var provided = GetProvided();
            var used = GetUsed();
            var max = Math.Max(provided, used);
            while (max >= scaleBy)
                scaleBy *= 2;

            var providedFrac = providedLerp.Update(provided / scaleBy);
            var usedFrac = usedLerp.Update(used / scaleBy);

            var b = RenderBounds;
            var powerHeigh = ChromeProvider.GetImage(PowerImageCollection, PowerHeighImage);
            var powerMiddle = ChromeProvider.GetImage(PowerImageCollection, PowerMiddleImage);
            var powerLow = ChromeProvider.GetImage(PowerImageCollection, PowerLowImage);
            Sprite s = null;
            var powerState = GetPowerState();
            if (Orientation == ResourceBarOrientation.Vertical)
            {
                var tl = new float2(b.X, (int)float2.Lerp(b.Bottom, b.Top, providedFrac));
                var br = tl + new float2(b.Width, (int)(providedFrac * b.Height));
                switch(powerState)
                {
                    case "low":
                        s = powerLow;
                        break;
                    case "middle":
                        s = powerMiddle;
                        break;
                    case "heigh":
                        s = powerHeigh;
                        break;
                }
                var x = (b.Left + b.Right - s.Size.X) / 2;
                var y = float2.Lerp(b.Bottom, b.Top, usedFrac) - s.Size.Y / 2;
                var totalY = b.Bottom;
                var totalNum = (totalY - y) / s.Size.Y;
                for (int i = 0; i < totalNum; i++)
                {
                    float lastY = y + i * s.Size.Y;
                    Game.Renderer.RgbaSpriteRenderer.DrawSprite(s, new float2(x, lastY));
                }
            }
            else
            {
                var tl = new float2(b.X, b.Y);
                var br = tl + new float2((int)(providedFrac * b.Width), b.Height);
                switch (powerState)
                {
                    case "low":
                        s = powerLow;
                        break;
                    case "middle":
                        s = powerMiddle;
                        break;
                    case "heigh":
                        s = powerHeigh;
                        break;
                }
                var x = float2.Lerp(b.Left, b.Right, usedFrac) - s.Size.X / 2;
                var y = (b.Bottom + b.Top - s.Size.Y) / 2;
                var totalX = b.Left;
                var totalNum = (totalX - x) / s.Size.X;
                for (int i = 0; i < totalNum; i++)
                {
                    float lastX = x + i * s.Size.X;
                    Game.Renderer.RgbaSpriteRenderer.DrawSprite(s, new float2(x, lastX));
                }
            }
        }
    }
}
