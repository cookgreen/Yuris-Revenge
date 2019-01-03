using OpenRA.Graphics;
using OpenRA.Mods.Cnc.Graphics;
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
                string paletteName = string.IsNullOrEmpty(palette) ? playerPalette : palette;
                pr = WorldRenderer.Palette(paletteName);
                cachedPalette = palette;
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

            var size = new float2(voxel.Size[0] * scale, voxel.Size[1] * scale);
            ModelAnimation animation = new ModelAnimation(voxel, () => WVec.Zero, () => new List<WRot>(){ WRot.Zero }, () => false, () => 0, true);

            List<ModelAnimation> components = new List<ModelAnimation>();
            components.Add(animation);

            WRot lightSource = new WRot(WAngle.Zero, new WAngle(256) - new WAngle(lightPitch), new WAngle(lightYaw));
            WRot camera = new WRot(WAngle.Zero, new WAngle(256), new WAngle(256));

            Game.Renderer.WorldModelRenderer.BeginFrame();
            Game.Renderer.WorldModelRenderer.RenderAsync(WorldRenderer, components, camera,
                scale, GroundNormal, lightSource, lightAmbientColor, lightDiffuseColor, pr, prNormals, prShadow);
            Game.Renderer.WorldModelRenderer.EndFrame();
        }
    }
}
