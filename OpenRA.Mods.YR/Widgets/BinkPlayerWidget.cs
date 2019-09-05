using OpenRA.Graphics;
using OpenRA.Mods.YR.FileFormats;
using OpenRA.Widgets;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Size = OpenRA.Primitives.Size;
using Rectangle = OpenRA.Primitives.Rectangle;

namespace OpenRA.Mods.YR.Widgets
{
    public class BinkPlayerWidget : Widget
    {
        public Hotkey CancelKey = new Hotkey(Keycode.ESCAPE, Modifiers.None);
        public float AspectRatio = 1.2f;
        public bool DrawOverlay = true;
        public bool Skippable = true;

        public bool Paused { get { return paused; } }
        public BinkReader Video { get { return video; } }

        Sprite videoSprite, overlaySprite;
        BinkReader video = null;
        string cachedVideo;
        float invLength;
        float2 videoOrigin, videoSize;
        uint[,] overlay;
        bool stopped;
        bool paused;

        Action onComplete;

        public void Load(string filename)
        {
            if (filename == cachedVideo)
                return;
            var video = new BinkReader(Game.ModData.DefaultFileSystem.Open(filename));

            cachedVideo = filename;
            Open(video);
        }

        public void Open(BinkReader video)
        {
            this.video = video;

            stopped = true;
            paused = true;
            Game.Sound.StopVideo();
            onComplete = () => { };

            invLength = video.Framerate * 1f / video.Frames;

            var size = Math.Max(video.Width, video.Height);
            var textureSize = Exts.NextPowerOf2(size);
            var videoSheet = new Sheet(SheetType.BGRA, new Size(textureSize, textureSize));

            videoSheet.GetTexture().ScaleFilter = TextureScaleFilter.Linear;
            //videoSheet.GetTexture().SetData(video.FrameData);

            videoSprite = new Sprite(videoSheet,
                new Rectangle(
                    0,
                    0,
                    video.Width,
                    video.Height),
                TextureChannel.RGBA);

            var scale = Math.Min((float)RenderBounds.Width / video.Width, (float)RenderBounds.Height / video.Height * AspectRatio);
            videoOrigin = new float2(
                RenderBounds.X + (RenderBounds.Width - scale * video.Width) / 2,
                RenderBounds.Y + (RenderBounds.Height - scale * video.Height * AspectRatio) / 2);

            // Round size to integer pixels. Round up to be consistent with the scale calculation.
            videoSize = new float2((int)Math.Ceiling(video.Width * scale), (int)Math.Ceiling(video.Height * AspectRatio * scale));

            if (!DrawOverlay)
                return;

            var scaledHeight = (int)videoSize.Y;
            overlay = new uint[Exts.NextPowerOf2(scaledHeight), 1];
            var black = (uint)255 << 24;
            for (var y = 0; y < scaledHeight; y += 2)
                overlay[y, 0] = black;

            var overlaySheet = new Sheet(SheetType.BGRA, new Size(1, Exts.NextPowerOf2(scaledHeight)));
            overlaySheet.GetTexture().SetData(overlay);
            overlaySprite = new Sprite(overlaySheet, new Rectangle(0, 0, 1, scaledHeight), TextureChannel.RGBA);
        }

        public override void Draw()
        {
            if (video == null)
                return;

            if (!stopped && !paused)
            {
                var nextFrame = 0;
                if (video.HasAudio)
                    nextFrame = (int)float2.Lerp(0, video.Frames, Game.Sound.VideoSeekPosition * invLength);
                else
                    nextFrame = video.CurrentFrame + 1;

                // Without the 2nd check the sound playback sometimes ends before the final frame is displayed which causes the player to be stuck on the first frame
                if (nextFrame > video.Frames || nextFrame < video.CurrentFrame)
                {
                    Stop();
                    return;
                }

                var skippedFrames = 0;
                while (nextFrame > video.CurrentFrame)
                {
                    video.AdvanceFrame();
                    //videoSprite.Sheet.GetTexture().SetData(video.FrameData);
                    skippedFrames++;
                }

                if (skippedFrames > 1)
                    Log.Write("perf", "BikPlayer : {0} skipped {1} frames at position {2}", cachedVideo, skippedFrames, video.CurrentFrame);
            }

            Game.Renderer.RgbaSpriteRenderer.DrawSprite(
                videoSprite,
                videoOrigin,
                videoSize);

            if (DrawOverlay)
                Game.Renderer.RgbaSpriteRenderer.DrawSprite(overlaySprite, videoOrigin, videoSize);
        }

        public override bool HandleKeyPress(KeyInput e)
        {
            if (Hotkey.FromKeyInput(e) != CancelKey || e.Event != KeyInputEvent.Down || !Skippable)
                return false;

            Stop();
            return true;
        }

        public override bool HandleMouseInput(MouseInput mi)
        {
            return RenderBounds.Contains(mi.Location) && Skippable;
        }

        public override string GetCursor(int2 pos)
        {
            return null;
        }

        public void Play()
        {
            PlayThen(() => { });
        }

        public void PlayThen(Action after)
        {
            if (video == null)
                return;

            onComplete = after;
            if (stopped)
                Game.Sound.PlayVideo(video.AudioData, video.AudioChannels, video.SampleBits, video.SampleRate);
            else
                Game.Sound.PlayVideo();

            stopped = paused = false;
        }

        public void Pause()
        {
            if (stopped || paused || video == null)
                return;

            paused = true;
            Game.Sound.PauseVideo();
        }

        public void Stop()
        {
            if (stopped || video == null)
                return;

            stopped = true;
            paused = true;
            Game.Sound.StopVideo();
            video.Reset();
            //videoSprite.Sheet.GetTexture().SetData(video.FrameData);
            Game.RunAfterTick(onComplete);
        }

        public void CloseVideo()
        {
            Stop();
            video = null;
        }
    }
}
