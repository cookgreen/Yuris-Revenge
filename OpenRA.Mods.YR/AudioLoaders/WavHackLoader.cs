using OpenRA.Mods.Common.AudioLoaders;
using OpenRA.Mods.YR.FileFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.YR.AudioLoaders
{
    //HACK
    class WavHackLoader : ISoundLoader
    {
        bool IsWave(Stream s)
        {
            var start = s.Position;
            var type = s.ReadASCII(4);
            s.Position += 4;
            var format = s.ReadASCII(4);
            s.Position = start;

            return type == "RIFF" && format == "WAVE";
        }

        bool ISoundLoader.TryParseSound(Stream stream, out ISoundFormat sound)
        {
            try
            {
                if (IsWave(stream))
                {
                    sound = new WavHackFormat(stream);
                    return true;
                }
            }
            catch
            {
                // Not a (supported) WAV
            }

            sound = null;
            return false;
        }
    }

    //HACK
    public sealed class WavHackFormat : ISoundFormat
    {
        public int Channels { get { return channels; } }
        public int SampleBits { get { return sampleBits; } }
        public int SampleRate { get { return sampleRate; } }
        public float LengthInSeconds { get { return WavHackReader.WaveLength(sourceStream); } }
        public Stream GetPCMInputStream() { return wavStreamFactory(); }
        public void Dispose() { sourceStream.Dispose(); }

        readonly Stream sourceStream;
        readonly Func<Stream> wavStreamFactory;
        readonly short channels;
        readonly int sampleBits;
        readonly int sampleRate;

        public WavHackFormat(Stream stream)
        {
            sourceStream = stream;

            if (!WavHackReader.LoadSound(stream, out wavStreamFactory, out channels, out sampleBits, out sampleRate))
                throw new InvalidDataException();
        }
    }
}
