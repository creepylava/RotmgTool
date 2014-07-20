using System;
using System.Runtime.InteropServices;
using System.Text;

namespace RotmgTool.Network
{
	[StructLayout(LayoutKind.Explicit)]
	internal struct FUnion
	{
		[FieldOffset(0)] public uint UInt32;
		[FieldOffset(0)] public ulong UInt64;
		[FieldOffset(0)] public float Float32;
		[FieldOffset(0)] public double Float64;
	}

	internal class NBufferReader
	{
		private readonly byte[] buffer;
		private int pos;

		public NBufferReader(byte[] buffer)
		{
			this.buffer = buffer;
			pos = 0;
		}

		public int Position
		{
			get { return pos; }
			set { pos = value; }
		}

		public ulong ReadUInt64()
		{
			return (ulong)(
				(buffer[pos++] << 56) |
				(buffer[pos++] << 48) |
				(buffer[pos++] << 40) |
				(buffer[pos++] << 32) |
				(buffer[pos++] << 24) |
				(buffer[pos++] << 16) |
				(buffer[pos++] << 8) |
				(buffer[pos++] << 0));
		}

		public uint ReadUInt32()
		{
			return (uint)(
				(buffer[pos++] << 24) |
				(buffer[pos++] << 16) |
				(buffer[pos++] << 8) |
				(buffer[pos++] << 0));
		}

		public ushort ReadUInt16()
		{
			return (ushort)(
				(buffer[pos++] << 8) |
				(buffer[pos++] << 0));
		}

		public byte ReadByte()
		{
			return buffer[pos++];
		}

		public float ReadSingle()
		{
			var union = new FUnion();
			union.UInt32 = ReadUInt32();
			return union.Float32;
		}

		public double ReadDouble()
		{
			var union = new FUnion();
			union.UInt64 = ReadUInt64();
			return union.Float64;
		}

		public byte[] ReadBytes(uint count)
		{
			var ret = new byte[count];
			Buffer.BlockCopy(buffer, pos, ret, 0, (int)count);
			pos += (int)count;
			return ret;
		}

		public string ReadUTF()
		{
			ushort count = ReadUInt16();
			string ret = Encoding.UTF8.GetString(buffer, pos, count);
			pos += count;
			return ret;
		}

		public string Read32UTF()
		{
			uint count = ReadUInt32();
			string ret = Encoding.UTF8.GetString(buffer, pos, (int)count);
			pos += (int)count;
			return ret;
		}

		public bool ReadBoolean()
		{
			return ReadByte() != 0;
		}
	}
}