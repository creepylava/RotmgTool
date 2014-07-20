using System;
using System.Collections.Generic;
using System.Text;

namespace RotmgTool.SWF
{
	internal class ABCWriter
	{
		private ABCFile abc;
		public byte[] buf;
		private uint pos;

		public ABCWriter(ABCFile abc)
		{
			this.abc = abc;
			buf = new byte[1024];

			writeU16(abc.minorVersion);
			writeU16(abc.majorVersion);

			writeU30((uint)(abc.ints.Length <= 1 ? 0 : abc.ints.Length));
			for (int i = 1; i < abc.ints.Length; i++)
				writeS32(abc.ints[i].Value);

			writeU30((uint)(abc.uints.Length <= 1 ? 0 : abc.uints.Length));
			for (int i = 1; i < abc.uints.Length; i++)
				writeU32(abc.uints[i].Value);

			writeU30((uint)(abc.doubles.Length <= 1 ? 0 : abc.doubles.Length));
			for (int i = 1; i < abc.doubles.Length; i++)
				writeD64(abc.doubles[i].Value);

			writeU30((uint)(abc.strings.Length <= 1 ? 0 : abc.strings.Length));
			for (int i = 1; i < abc.strings.Length; i++)
				writeString(abc.strings[i]);

			writeU30((uint)(abc.namespaces.Length <= 1 ? 0 : abc.namespaces.Length));
			for (int i = 1; i < abc.namespaces.Length; i++)
				writeNamespace(abc.namespaces[i]);

			writeU30((uint)(abc.namespaceSets.Length <= 1 ? 0 : abc.namespaceSets.Length));
			for (int i = 1; i < abc.namespaceSets.Length; i++)
				writeNamespaceSet(abc.namespaceSets[i]);

			writeU30((uint)(abc.multinames.Length <= 1 ? 0 : abc.multinames.Length));
			for (int i = 1; i < abc.multinames.Length; i++)
				writeMultiname(abc.multinames[i]);

			writeU30((uint)abc.methods.Length);
			for (int i = 0; i < abc.methods.Length; i++)
				writeMethodInfo(abc.methods[i]);

			writeU30((uint)abc.metadata.Length);
			for (int i = 0; i < abc.metadata.Length; i++)
				writeMetadata(abc.metadata[i]);

			writeU30((uint)abc.instances.Length);
			for (int i = 0; i < abc.instances.Length; i++)
				writeInstance(abc.instances[i]);

			for (int i = 0; i < abc.classes.Length; i++)
				writeClass(abc.classes[i]);

			writeU30((uint)abc.scripts.Length);
			for (int i = 0; i < abc.scripts.Length; i++)
				writeScript(abc.scripts[i]);

			writeU30((uint)abc.bodies.Length);
			for (int i = 0; i < abc.bodies.Length; i++)
				writeMethodBody(abc.bodies[i]);

			Array.Resize(ref buf, (int)pos);
		}

		private void writeU8(byte v)
		{
			if (pos == buf.Length)
				Array.Resize(ref buf, buf.Length * 2);
			buf[pos++] = v;
		}

		private void writeU16(ushort v)
		{
			writeU8((byte)(v & 0xFF));
			writeU8((byte)(v >> 8));
		}

		private void writeS24(int v)
		{
			writeU8((byte)(v & 0xFF));
			writeU8((byte)(v >> 8));
			writeU8((byte)(v >> 16));
		}

		private void writeU32(ulong v)
		{
			if (v < 128)
			{
				writeU8((byte)(v));
			}
			else if (v < 16384)
			{
				writeU8((byte)((v & 0x7F) | 0x80));
				writeU8((byte)((v >> 7) & 0x7F));
			}
			else if (v < 2097152)
			{
				writeU8((byte)((v & 0x7F) | 0x80));
				writeU8((byte)((v >> 7) | 0x80));
				writeU8((byte)((v >> 14) & 0x7F));
			}
			else if (v < 268435456)
			{
				writeU8((byte)((v & 0x7F) | 0x80));
				writeU8((byte)(v >> 7 | 0x80));
				writeU8((byte)(v >> 14 | 0x80));
				writeU8((byte)((v >> 21) & 0x7F));
			}
			else
			{
				writeU8((byte)((v & 0x7F) | 0x80));
				writeU8((byte)(v >> 7 | 0x80));
				writeU8((byte)(v >> 14 | 0x80));
				writeU8((byte)(v >> 21 | 0x80));
				writeU8((byte)((v >> 28) & 0x0F));
			}
		}

		private void writeS32(long v)
		{
			writeU32((ulong)v);
		}

		private void writeU30(ulong v)
		{
			writeU32(v);
		}

		private void writeExact(byte[] ptr, int len)
		{
			while (pos + len > buf.Length)
				Array.Resize(ref buf, buf.Length * 2);
			Buffer.BlockCopy(ptr, 0, buf, (int)pos, len);
			pos += (uint)len;
		}

		private void writeD64(double v)
		{
			if (pos + 8 > buf.Length)
				Array.Resize(ref buf, buf.Length * 2);
			Buffer.BlockCopy(BitConverter.GetBytes(v), 0, buf, (int)pos, 8);
			pos += 8;
		}

		private void writeString(string v)
		{
			byte[] data = Encoding.UTF8.GetBytes(v);
			writeU30((uint)data.Length);
			writeExact(data, data.Length);
		}

		private void writeBytes(byte[] v)
		{
			writeU30((uint)v.Length);
			writeExact(v, v.Length);
		}

		private void writeNamespace(ABCFile.Namespace v)
		{
			writeU8((byte)v.kind);
			writeU30(v.name);
		}

		private void writeNamespaceSet(uint[] v)
		{
			writeU30((uint)v.Length);
			foreach (var value in v)
				writeU30(value);
		}

		private void writeMultiname(ABCFile.Multiname v)
		{
			writeU8((byte)v.kind);
			switch (v.kind)
			{
				case ASType.QName:
				case ASType.QNameA:
					writeU30(v.QName.ns);
					writeU30(v.QName.name);
					break;
				case ASType.RTQName:
				case ASType.RTQNameA:
					writeU30(v.RTQName.name);
					break;
				case ASType.RTQNameL:
				case ASType.RTQNameLA:
					break;
				case ASType.Multiname:
				case ASType.MultinameA:
					writeU30(v.MultiName.name);
					writeU30(v.MultiName.nsSet);
					break;
				case ASType.MultinameL:
				case ASType.MultinameLA:
					writeU30(v.MultinameL.nsSet);
					break;
				case ASType.TypeName:
					writeU30(v.TypeName.name);
					writeU30((uint)v.TypeName.parameters.Length);
					foreach (var value in v.TypeName.parameters)
						writeU30(value);
					break;
				default:
					throw new Exception("Unknown Multiname kind");
			}
		}

		private void writeMethodInfo(ABCFile.MethodInfo v)
		{
			writeU30((uint)v.paramTypes.Length);
			writeU30(v.returnType);
			foreach (var value in v.paramTypes)
				writeU30(value);
			writeU30(v.name);
			writeU8((byte)v.flags);
			if ((v.flags & MethodFlags.HAS_OPTIONAL) != 0)
			{
				writeU30((uint)v.options.Length);
				foreach (var option in v.options)
					writeOptionDetail(option);
			}
			if ((v.flags & MethodFlags.HAS_PARAM_NAMES) != 0)
			{
				foreach (var value in v.paramNames)
					writeU30(value);
			}
		}

		private void writeOptionDetail(ABCFile.OptionDetail v)
		{
			writeU30(v.val);
			writeU8((byte)v.kind);
		}

		private void writeMetadata(ABCFile.Metadata v)
		{
			writeU30(v.name);
			writeU30((uint)v.keys.Length);
			foreach (var key in v.keys)
				writeU30(key);
			foreach (var value in v.values)
				writeU30(value);
		}

		private void writeInstance(ABCFile.Instance v)
		{
			writeU30(v.name);
			writeU30(v.superName);
			writeU8((byte)v.flags);
			if ((v.flags & InstanceFlags.ProtectedNs) != 0)
				writeU30(v.protectedNs);
			writeU30((uint)v.interfaces.Length);
			foreach (var value in v.interfaces)
				writeU30(value);
			writeU30(v.iinit);
			writeU30((uint)v.traits.Length);
			foreach (var value in v.traits)
				writeTrait(value);
		}

		private void writeTrait(ABCFile.TraitsInfo v)
		{
			writeU30(v.name);
			writeU8(v.kindAttr);
			switch (v.kind)
			{
				case TraitKind.Slot:
				case TraitKind.Const:
					writeU30(v.Slot.slotId);
					writeU30(v.Slot.typeName);
					writeU30(v.Slot.vindex);
					if (v.Slot.vindex != 0)
						writeU8((byte)v.Slot.vkind);
					break;
				case TraitKind.Class:
					writeU30(v.Class.slotId);
					writeU30(v.Class.classi);
					break;
				case TraitKind.Function:
					writeU30(v.Function.slotId);
					writeU30(v.Function.functioni);
					break;
				case TraitKind.Method:
				case TraitKind.Getter:
				case TraitKind.Setter:
					writeU30(v.Method.dispId);
					writeU30(v.Method.method);
					break;
				default:
					throw new Exception("Unknown trait kind");
			}
			if ((v.attr & TraitAttributes.Metadata) != 0)
			{
				writeU30((uint)v.metadata.Length);
				foreach (var value in v.metadata)
					writeU30(value);
			}
		}

		private void writeClass(ABCFile.Class v)
		{
			writeU30(v.cinit);
			writeU30((uint)v.traits.Length);
			foreach (var value in v.traits)
				writeTrait(value);
		}

		private void writeScript(ABCFile.Script v)
		{
			writeU30(v.sinit);
			writeU30((uint)v.traits.Length);
			foreach (var value in v.traits)
				writeTrait(value);
		}

		private struct Fixup
		{
			public readonly ABCFile.Label target;
			public readonly uint pos;
			public readonly uint baseOffset;

			public Fixup(ABCFile.Label target, uint pos, uint baseOffset)
			{
				this.target = target;
				this.pos = pos;
				this.baseOffset = baseOffset;
			}
		}

		private void writeMethodBody(ABCFile.MethodBody v)
		{
			writeU30(v.method);
			writeU30(v.maxStack);
			writeU30(v.localCount);
			writeU30(v.initScopeDepth);
			writeU30(v.maxScopeDepth);

			var instructionOffsets = new uint[v.instructions.Length + 1];

			Func<ABCFile.Label, uint> resolveLabel =
				(ABCFile.Label label) => { return (uint)(instructionOffsets[label.index] + label.offset); };

			{
				// we don't know the length before writing all the instructions - swap buffer with a temporary one
				byte[] globalBuf = buf;
				uint globalPos = pos;
				var methodBuf = new byte[1024 * 16];
				buf = methodBuf;
				pos = 0;

				var fixups = new List<Fixup>();

				for (int i = 0; i < v.instructions.Length; i++)
				{
					var instruction = v.instructions[i];
					uint instructionOffset = pos;
					instructionOffsets[i] = instructionOffset;

					var opcodeInfo = OpcodeInfo.opcodeDict[instruction.opcode];
					writeU8((byte)instruction.opcode);

					for (int j = 0; j < opcodeInfo.argumentTypes.Length; j++)
						switch (opcodeInfo.argumentTypes[j])
						{
							case OpcodeArgumentType.Unknown:
								throw new Exception("Don't know how to encode OP_" + opcodeInfo.name);

							case OpcodeArgumentType.UByteLiteral:
								writeU8(instruction.arguments[j].ubytev);
								break;
							case OpcodeArgumentType.IntLiteral:
								writeS32(instruction.arguments[j].intv);
								break;
							case OpcodeArgumentType.UIntLiteral:
								writeU32(instruction.arguments[j].uintv);
								break;

							case OpcodeArgumentType.Int:
							case OpcodeArgumentType.UInt:
							case OpcodeArgumentType.Double:
							case OpcodeArgumentType.String:
							case OpcodeArgumentType.Namespace:
							case OpcodeArgumentType.Multiname:
							case OpcodeArgumentType.Class:
							case OpcodeArgumentType.Method:
								writeU30(instruction.arguments[j].index);
								break;

							case OpcodeArgumentType.JumpTarget:
								fixups.Add(new Fixup(instruction.arguments[j].jumpTarget, pos, pos + 3));
								writeS24(0);
								break;

							case OpcodeArgumentType.SwitchDefaultTarget:
								fixups.Add(new Fixup(instruction.arguments[j].jumpTarget, pos, instructionOffset));
								writeS24(0);
								break;

							case OpcodeArgumentType.SwitchTargets:
								writeU30((uint)instruction.arguments[j].switchTargets.Length - 1);
								foreach (var off in instruction.arguments[j].switchTargets)
								{
									fixups.Add(new Fixup(off, pos, instructionOffset));
									writeS24(0);
								}
								break;
						}
				}

				Array.Resize(ref buf, (int)pos);
				instructionOffsets[v.instructions.Length] = pos;

				foreach (var fixup in fixups)
				{
					pos = fixup.pos;
					writeS24((int)(resolveLabel(fixup.target) - fixup.baseOffset));
				}

				byte[] code = buf;
				// restore global buffer
				buf = globalBuf;
				pos = globalPos;

				writeBytes(code);
			}

			writeU30((uint)v.exceptions.Length);
			foreach (var val in v.exceptions)
			{
				var value = val;
				value.from.absoluteOffset = resolveLabel(value.from);
				value.to.absoluteOffset = resolveLabel(value.to);
				value.target.absoluteOffset = resolveLabel(value.target);
				writeExceptionInfo(value);
			}
			writeU30((uint)v.traits.Length);
			foreach (var value in v.traits)
				writeTrait(value);
		}

		private void writeExceptionInfo(ABCFile.ExceptionInfo v)
		{
			writeU30(v.from.absoluteOffset);
			writeU30(v.to.absoluteOffset);
			writeU30(v.target.absoluteOffset);
			writeU30(v.excType);
			writeU30(v.varName);
		}
	}
}