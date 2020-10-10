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
using OpenRA.Mods.Cnc.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Widgets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.YR.Widgets
{
    public class VoxelWidget : Widget
    {
        public string Palette = "";
        public string PlayerPalette = "player";
        public string NormalsPalette = "normals";
        public string ShadowPalette = "shadow";
        public float Scale = 12f;
        public int LightPitch;
        public int LightYaw;
        public float[] lightAmbientColor = new float[] { 0.6f, 0.6f, 0.6f };
        public float[] lightDiffuseColor = new float[] { 0.4f, 0.4f, 0.4f };
        public Func<string> GetPalette;
        public Func<string> GetPlayerPalette;
        public Func<string> GetNormalsPalette;
        public Func<string> GetShadowPalette;
        public Func<float[]> GetLightAmbientColor;
        public Func<float[]> GetLightDiffuseColor;
        public Func<float> GetScale;
        public Func<int> GetLightPitch;
        public Func<int> GetLightYaw;
        public Func<Voxel> GetVoxel;
        public int2 PreviewOffset { get; private set; }
        public int2 IdealPreviewSize { get; private set; }

        private World world;
        IFinalizedRenderable[] renderables;

        protected readonly WorldRenderer WorldRenderer;

        [ObjectCreator.UseCtor]
        public VoxelWidget(WorldRenderer worldRenderer)
        {
            GetPalette = () => Palette;
            GetPlayerPalette = () => PlayerPalette;
            GetNormalsPalette = () => NormalsPalette;
            GetShadowPalette = () => ShadowPalette;
            GetLightAmbientColor = () => lightAmbientColor;
            GetLightDiffuseColor = () => lightDiffuseColor;
            GetScale = () => Scale;
            GetLightPitch = () => LightPitch;
            GetLightYaw = () => LightYaw;
            WorldRenderer = worldRenderer;
            world = worldRenderer.World;
        }

        protected VoxelWidget(VoxelWidget other)
			: base(other)
		{
            Palette = other.Palette;
            GetPalette = other.GetPalette;
            GetVoxel = other.GetVoxel;

            WorldRenderer = other.WorldRenderer;
        }

        public override Widget Clone()
        {
            return new VoxelWidget(this);
        }

        private Voxel cachedVoxel;
        private string cachedPalette;
        private string cachedPlayerPalette;
        private string cachedNormalsPalette;
        private string cachedShadowPalette;
        private float cachedScale;
        private float[] cachedLightAmbientColor = new float[] { 0, 0, 0};
        private float[] cachedLightDiffuseColor = new float[] { 0, 0, 0};
        private int cachedLightPitch;
        private int cachedLightYaw;
        private PaletteReference pr;
        private PaletteReference prPlayer;
        private PaletteReference prNormals;
        private PaletteReference prShadow;
        private float2 offset = float2.Zero;
        private float[] GroundNormal = new float[] { 0, 0, 1, 1 };

        public override void Draw()
        {
            if (renderables == null)
            {
                return;
            }

            var scale = 1f;
            var origin = RenderOrigin + new int2(RenderBounds.Size.Width / 2, RenderBounds.Size.Height / 2);

            // The scale affects world -> screen transform, which we don't want when drawing the (fixed) UI.
            if (scale != 1f)
                origin = (1f / scale * origin.ToFloat2()).ToInt2();

            Game.Renderer.Flush();
            // TODO: This was completely removed from the API
            // Game.Renderer.SetViewportParams(-origin - PreviewOffset, scale);

            foreach (var r in renderables)
                r.Render(WorldRenderer);
            
            Game.Renderer.Flush();
            // TODO: This was completely removed from the API
            // Game.Renderer.SetViewportParams(WorldRenderer.Viewport.TopLeft, WorldRenderer.Viewport.Zoom);
        }

        public override void PrepareRenderables()
        {
            var voxel = GetVoxel();
            var palette = GetPalette();
            var playerPalette = GetPlayerPalette();
            var normalsPalette = GetNormalsPalette();
            var shadowPalette = GetShadowPalette();
            var scale = GetScale();
            var lightAmbientColor = GetLightAmbientColor();
            var lightDiffuseColor = GetLightDiffuseColor();
            var lightPitch = GetLightPitch();
            var lightYaw = GetLightYaw();

            if (voxel == null || palette == null)
                return;

            if (voxel != cachedVoxel)
            {
                offset = 0.5f * (new float2(RenderBounds.Size) - new float2(voxel.Size[0], voxel.Size[1]));
                cachedVoxel = voxel;
            }

            if (palette != cachedPalette)
            {
                if (string.IsNullOrEmpty(palette) && string.IsNullOrEmpty(playerPalette))
                {
                    return;
                }
                string paletteName = string.IsNullOrEmpty(palette) ? playerPalette : palette;
                pr = WorldRenderer.Palette(paletteName);
                cachedPalette = paletteName;
            }

            if (playerPalette != cachedPlayerPalette)
            {
                prPlayer = WorldRenderer.Palette(playerPalette);
                cachedPlayerPalette = playerPalette;
            }

            if (normalsPalette != cachedNormalsPalette)
            {
                prNormals = WorldRenderer.Palette(normalsPalette);
                cachedNormalsPalette = normalsPalette;
            }

            if (shadowPalette != cachedShadowPalette)
            {
                prShadow = WorldRenderer.Palette(shadowPalette);
                cachedShadowPalette = shadowPalette;
            }

            if (scale != cachedScale)
            {
                //offset *= scale;
                cachedScale = scale;
            }

            if (lightPitch != cachedLightPitch)
            {
                cachedLightPitch = lightPitch;
            }

            if (lightYaw != cachedLightYaw)
            {
                cachedLightYaw = lightYaw;
            }

            if (cachedLightAmbientColor[0] != lightAmbientColor[0] || cachedLightAmbientColor[1] != lightAmbientColor[1] || cachedLightAmbientColor[2] != lightAmbientColor[2])
            {
                cachedLightAmbientColor = lightAmbientColor;
            }

            if (cachedLightDiffuseColor[0] != lightDiffuseColor[0] || cachedLightDiffuseColor[1] != lightDiffuseColor[1] || cachedLightDiffuseColor[2] != lightDiffuseColor[2])
            {
                cachedLightDiffuseColor = lightDiffuseColor;
            }
            if (cachedVoxel == null)
            {
                return;
            }
            var size = new float2(cachedVoxel.Size[0] * cachedScale, cachedVoxel.Size[1] * cachedScale);
            ModelAnimation animation = new ModelAnimation(
                cachedVoxel, 
                () => WVec.Zero, 
                () => new List<WRot>() {
                    new WRot(
                        new WAngle(-45),
                        new WAngle(-30),
                        new WAngle(360)
                    )
                }, 
                () => false,
                () => 0,
                true);
            
            ModelPreview preview = new ModelPreview(
                new ModelAnimation[] { animation }, WVec.Zero, 0,
                cachedScale,
                new WAngle(cachedLightPitch), 
                new WAngle(cachedLightYaw),
                cachedLightAmbientColor,
                cachedLightDiffuseColor,
                new WAngle(),
                pr,
                prNormals,
                prShadow);

            List<ModelPreview> previews = new List<ModelPreview>() {
                preview
            };


            // Calculate the preview bounds
            PreviewOffset = int2.Zero;
            IdealPreviewSize = int2.Zero;

            var rs = previews.SelectMany(p => ((IActorPreview)p).ScreenBounds(WorldRenderer, WPos.Zero));

            if (rs.Any())
            {
                var b = rs.First();
                foreach (var rr in rs.Skip(1))
                    b = OpenRA.Primitives.Rectangle.Union(b, rr);

                IdealPreviewSize = new int2(b.Width, b.Height);
                PreviewOffset = -new int2(b.Left, b.Top) - IdealPreviewSize / 2;
            }

            renderables = previews
                .SelectMany(p => ((IActorPreview)p).Render(WorldRenderer, WPos.Zero))
                .OrderBy(WorldRenderer.RenderableScreenZPositionComparisonKey)
                .Select(r => r.PrepareRender(WorldRenderer))
                .ToArray();
        }
    }
}
