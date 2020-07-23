using OpenRA;
using OpenRA.Graphics;
using OpenRA.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.YR.SpriteLoaders
{
	public class PcxLoader : ISpriteLoader
	{
		class PcxFrame : ISpriteFrame
		{
			public Size Size { get; set; }
			public Size FrameSize { get; set; }
			public float2 Offset { get; set; }
			public byte[] Data { get; set; }
			public bool DisableExportPadding { get { return false; } }

			public SpriteFrameType Type
			{
				get
				{
					return SpriteFrameType.BGRA;
				}
			}
		}
		public bool TryParseSprite(Stream s, out ISpriteFrame[] frames, out TypeDictionary metadata)
		{
			frames = new ISpriteFrame[1];
			metadata = new TypeDictionary();

			return true;
		}
	}
}
