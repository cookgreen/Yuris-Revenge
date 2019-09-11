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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenRA.Graphics;
using OpenRA.Traits;
using Color = OpenRA.Primitives.Color;

namespace OpenRA.Mods.YR.Traits
{
    using GUtil = OpenRA.Graphics.Util;
    public class EnvironmentPaletteEffectInfo : ITraitInfo
    {
        public readonly string[] ExcludePalette = { "cursor", "chrome", "colorpicker", "fog", "shroud", "effect" };

        public readonly float Ratio = 0.6f;

        [Desc("Measured in ticks.")]
        public readonly int Length = 20;

        public readonly Color Color = Color.White;

        [Desc("Set this when using multiple independent flash effects.")]
        public readonly string Type = null;

        public object Create(ActorInitializer init) { return new EnvironmentPaletteEffect(this); }
    }

    public class EnvironmentPaletteEffect : IPaletteModifier, ITick
    {
        private EnvironmentPaletteEffectInfo info;
        private int remainingFrames;
        public EnvironmentPaletteEffectInfo Info
        {
            get
            {
                return info;
            }
        }

        public EnvironmentPaletteEffect(EnvironmentPaletteEffectInfo info)
        {
            this.info = info;
        }

        public void Enable(int ticks)
        {
            if (ticks == -1)
                remainingFrames = Info.Length;
            else
                remainingFrames = ticks;
        }

        void ITick.Tick(Actor self)
        {
            if (remainingFrames > 0)
                remainingFrames--;
        }
        public void AdjustPalette(IReadOnlyDictionary<string, MutablePalette> palettes)
        {
            if (remainingFrames == 0)
                return;

            foreach (var pal in palettes)
            {
                if (info.ExcludePalette.Contains(pal.Key))
                {
                    continue;
                }
                for (var x = 0; x < Palette.Size; x++)
                {
                    var orig = pal.Value.GetColor(x);
                    var c = Info.Color;
                    var color = Color.FromArgb(orig.A, ((int)c.R).Clamp(0, 255), ((int)c.G).Clamp(0, 255), ((int)c.B).Clamp(0, 255));
                    var final = GUtil.PremultipliedColorLerp(info.Ratio, orig, GUtil.PremultiplyAlpha(Color.FromArgb(orig.A, color)));
                    pal.Value.SetColor(x, final);
                }
            }
        }
    }
}
