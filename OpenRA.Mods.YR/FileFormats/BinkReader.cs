using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.YR.FileFormats
{
    public class BinkFrameIndexTable
    {
        public BinkFrameIndex[] Indices;
    }
    public class BinkFrameIndex
    {
        public uint Offset;
        public bool IsKeyframe;
    }
    public class BinkAudioTrack
    {
        public int ID;
        public int AudioChannels;
        public byte[] AudioData;
        public int SampleBits;
        public int SampleRate;
    }
    public class BinkReader
    {
        public List<BinkAudioTrack> AudioTracks;
        public uint AudioTrackLength;
        public int AudioChannels;
        public byte[] AudioData;
        public int CurrentFrame;
        public Bitmap FrameData;
        public float Framerate;
        public int Frames;
        public bool HasAudio;
        public int SampleBits;
        public int SampleRate;
        public int Width;
        public int Height;
        private Stream stream;
        private BinkFrameIndexTable frameIndexTable;

        public BinkReader(Stream stream)
        {
            this.stream = stream;
            frameIndexTable = new BinkFrameIndexTable();
            AudioTracks = new List<BinkAudioTrack>();
            byte[] buffer = new byte[3];
            stream.Read(buffer, 0, 3);
            string signature = Encoding.UTF8.GetString(buffer);
            if (signature != "BIK")
            {
                throw new Exception("Invalid BINK File!");
            }
            stream.ReadByte();//Version
            stream.ReadUInt32();//File Size
            Frames = stream.ReadInt32();//Number of frames
            stream.ReadFloat();//Largest frame size
            stream.ReadFloat();//Unknown
            Width = stream.ReadInt32();//Video width
            Height = stream.ReadInt32();//Video height
            Framerate = stream.ReadInt32();//Frames per second
            buffer = stream.ReadBytes(4);//Frames per second divider
            buffer = stream.ReadBytes(4);//Flags
            AudioTrackLength = stream.ReadUInt32();
            HasAudio = AudioTrackLength > 0;

            GetAudioInformation();
            LoadEntrytable();
            Reset();
        }

        private void LoadEntrytable()
        {
            frameIndexTable.Indices = new BinkFrameIndex[Frames + 1];
            for (int i = 0; i < Frames + 1; i++)
            {
                var offset = stream.ReadUInt32();
                frameIndexTable.Indices[i] = new BinkFrameIndex();
                if ((offset & 1) == 1)
                {
                    frameIndexTable.Indices[i].IsKeyframe = true;
                    offset &= ~1u;
                }

                frameIndexTable.Indices[i].Offset = offset;
            }
        }

        void GetAudioInformation()
        {
            for (int i = 0; i < AudioTrackLength; i++)
            {
                BinkAudioTrack track = new BinkAudioTrack();
                byte[] buffer = stream.ReadBytes(2);
                buffer = stream.ReadBytes(2);
                track.AudioChannels = buffer[0];
                AudioTracks.Add(track);
            }
            for (int i = 0; i < AudioTrackLength; i++)
            {
                BinkAudioTrack track = AudioTracks[i];
                byte[] buffer = stream.ReadBytes(2);
                byte[] newbuffer = new byte[4];
                newbuffer[0] = buffer[0];
                newbuffer[1] = buffer[1];
                newbuffer[2] = 0;
                newbuffer[3] = 0;
                track.SampleRate = BitConverter.ToInt32(newbuffer, 0);
            }
            for (int i = 0; i < AudioTrackLength; i++)
            {
                BinkAudioTrack track = AudioTracks[i];
                track.ID = stream.ReadInt32();
            }
        }

        internal void AdvanceFrame()
        {
            CurrentFrame++;
            LoadFrame();
        }

        void LoadFrame()
        {
            stream.Seek(frameIndexTable.Indices[CurrentFrame].Offset, SeekOrigin.Begin);
        }
        public void Reset()
        {
            CurrentFrame = 0;
            LoadFrame();
        }
    }
}
