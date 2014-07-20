using System;
using System.Collections.Generic;
using System.IO;

namespace RotmgTool.SWF
{
	internal class Tag
	{
		public ushort Type;
		public byte[] Content;
		public bool ForcedLongLength;
	}

	internal class SwfFile
	{
		public byte[] Header;
		public byte Version;
		public Tag[] Tags;

		public SwfFile(byte[] swf)
		{
			byte[] content;
			if (swf[0] == 0x43)
			{
				var compressed = new byte[swf.Length - 8];
				Buffer.BlockCopy(swf, 8, compressed, 0, compressed.Length);
				content = RotMG.Common.IO.Zlib.Decompress(compressed);
			}
			else
			{
				content = new byte[swf.Length - 8];
				Buffer.BlockCopy(swf, 8, content, 0, content.Length);
			}
			Version = swf[3];

			using (var reader = new BinaryReader(new MemoryStream(content)))
			{
				int frameSizeBits = content[0] >> 3;
				int totalBits = frameSizeBits * 4 + 5;
				int frameSizeLen = ((totalBits + 7) & ~7) >> 3;
				Header = reader.ReadBytes(frameSizeLen + 4);

				var tags = new List<Tag>();
				while (reader.BaseStream.Position < reader.BaseStream.Length)
				{
					ushort packedHeader = reader.ReadUInt16();
					var type = (ushort)(packedHeader >> 6);
					var len = (uint)(packedHeader & ((1 << 6) - 1));
					bool forced = false;
					if (len == 0x3f)
					{
						var newLen = reader.ReadUInt32();
						if (newLen < 0x3f)
							forced = true;
						len = newLen;
					}
					tags.Add(new Tag { Type = type, Content = reader.ReadBytes((int)len), ForcedLongLength = forced });
				}
				Tags = tags.ToArray();
			}
		}

		public byte[] Write()
		{
			var ms = new MemoryStream();
			using (var writer = new BinaryWriter(ms))
			{
				writer.Write(Header);
				foreach (var tag in Tags)
				{
					var packedHeader = (ushort)(tag.Type << 6);
					if (tag.Content.Length < 0x3f && !tag.ForcedLongLength)
					{
						packedHeader |= (ushort)tag.Content.Length;
						writer.Write(packedHeader);
					}
					else
					{
						packedHeader |= 0x3f;
						writer.Write(packedHeader);
						writer.Write((uint)tag.Content.Length);
					}
					writer.Write(tag.Content);
				}
			}
			var content = ms.ToArray();
			//var compressed = ZlibStream.CompressBuffer(content);
			ms = new MemoryStream();
			using (var writer = new BinaryWriter(ms))
			{
				writer.Write((byte)0x46);
				writer.Write((byte)0x57);
				writer.Write((byte)0x53);
				writer.Write(Version);
				writer.Write(content.Length + 8);
				writer.Write(content);
			}

			return ms.ToArray();
		}
	}
}