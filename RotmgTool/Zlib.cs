/*
    Copyright (C) 2014 creepylava

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to
    deal in the Software without restriction, including without limitation the
    rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
    sell copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
    FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
    IN THE SOFTWARE.
*/

using System;
using System.IO;
using System.IO.Compression;

namespace RotMG.Common.IO
{
	public static class Zlib
	{
		private static uint ADLER32(byte[] data)
		{
			const uint MODULO = 0xfff1;
			uint A = 1, B = 0;
			for (int i = 0; i < data.Length; i++)
			{
				A = (A + data[i]) % MODULO;
				B = (B + A) % MODULO;
			}
			return (B << 16) | A;
		}

		public static byte[] Decompress(byte[] buffer)
		{
			// Refer to http://www.ietf.org/rfc/rfc1950.txt for zlib format
			if (buffer.Length < 6)
				throw new ArgumentException("Invalid ZLIB buffer.");

			byte CMF = buffer[0];
			byte FLG = buffer[1];

			var CM = (byte)(CMF & 0xf);
			var CINFO = (byte)(CMF >> 4);
			if (CM != 8)
				throw new NotSupportedException("Invalid compression method.");
			if (CINFO != 7)
				throw new NotSupportedException("Unsupported window size.");

			bool FDICT = (FLG & 0x20) != 0;
			var FLEVEL = (byte)(FLG >> 6);
			if (FDICT)
				throw new NotSupportedException("Preset dictionary not supported.");
			if (((CMF << 8) + FLG) % 31 != 0)
				throw new InvalidDataException("Invalid header checksum");

			var input = new MemoryStream(buffer, 2, buffer.Length - 6);
			var output = new MemoryStream();
			using (var deflate = new DeflateStream(input, CompressionMode.Decompress))
				deflate.CopyTo(output);
			var result = output.ToArray();

			int index = buffer.Length - 4;
			uint checksum =
				(uint)(buffer[index++] << 24) | (uint)(buffer[index++] << 16) |
				(uint)(buffer[index++] << 8) | (uint)(buffer[index++] << 0);
			if (checksum != ADLER32(result))
				throw new InvalidDataException("Invalid data checksum");

			return result;
		}
	}
}