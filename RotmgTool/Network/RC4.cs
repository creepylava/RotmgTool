using System;

namespace RotmgTool.Network
{
	public class RC4
	{
		// Adopted from BouncyCastle

		private static readonly int STATE_LENGTH = 256;

		private byte[] engineState;
		private int x;
		private int y;
		private byte[] workingKey;

		public RC4(byte[] key)
		{
			workingKey = (byte[])key.Clone();

			x = 0;
			y = 0;

			if (engineState == null)
			{
				engineState = new byte[STATE_LENGTH];
			}

			// reset the state of the engine
			for (int i = 0; i < STATE_LENGTH; i++)
			{
				engineState[i] = (byte)i;
			}

			int i1 = 0;
			int i2 = 0;

			for (int i = 0; i < STATE_LENGTH; i++)
			{
				i2 = ((key[i1] & 0xff) + engineState[i] + i2) & 0xff;
				// do the byte-swap inline
				byte tmp = engineState[i];
				engineState[i] = engineState[i2];
				engineState[i2] = tmp;
				i1 = (i1 + 1) % key.Length;
			}
		}

		public void Crypt(byte[] buf, int offset, int len)
		{
			for (int i = 0; i < len; i++)
			{
				x = (x + 1) & 0xff;
				y = (engineState[x] + y) & 0xff;

				// swap
				byte tmp = engineState[x];
				engineState[x] = engineState[y];
				engineState[y] = tmp;

				// xor
				buf[i + offset] = (byte)(buf[i + offset]
				                         ^ engineState[(engineState[x] + engineState[y]) & 0xff]);
			}
		}

		public object SaveState()
		{
			return Tuple.Create((byte[])engineState.Clone(), x, y);
		}

		public void LoadState(object state)
		{
			var s = (Tuple<byte[], int, int>)state;
			engineState = s.Item1;
			x = s.Item2;
			y = s.Item3;
		}
	}
}