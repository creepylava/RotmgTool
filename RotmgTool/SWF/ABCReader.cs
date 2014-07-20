using System;
using System.Collections.Generic;
using System.Text;

namespace RotmgTool.SWF
{
	internal class ABCReader
	{
		private readonly byte[] buf;
		private uint pos;
		public ABCFile abc;

		public ABCReader(byte[] buf, uint pos)
		{
			try
			{
				this.buf = buf;
				this.pos = pos;
				abc = new ABCFile();

				abc.minorVersion = readU16();
				abc.majorVersion = readU16();

				Func<uint, uint> atLeastOne = (uint n) => { return n != 0 ? n : 1; };

				abc.ints = new long?[atLeastOne(readU30())];
				for (int i = 1; i < abc.ints.Length; i++)
					abc.ints[i] = readS32();

				abc.uints = new ulong?[atLeastOne(readU30())];
				for (int i = 1; i < abc.uints.Length; i++)
					abc.uints[i] = readU32();

				abc.doubles = new double?[atLeastOne(readU30())];
				for (int i = 1; i < abc.doubles.Length; i++)
					abc.doubles[i] = readD64();

				abc.strings = new string[atLeastOne(readU30())];
				for (int i = 1; i < abc.strings.Length; i++)
					abc.strings[i] = readString();

				abc.namespaces = new ABCFile.Namespace[atLeastOne(readU30())];
				for (int i = 1; i < abc.namespaces.Length; i++)
					abc.namespaces[i] = readNamespace();

				abc.namespaceSets = new uint[atLeastOne(readU30())][];
				for (int i = 1; i < abc.namespaceSets.Length; i++)
					abc.namespaceSets[i] = readNamespaceSet();

				abc.multinames = new ABCFile.Multiname[atLeastOne(readU30())];
				for (int i = 1; i < abc.multinames.Length; i++)
					abc.multinames[i] = readMultiname();

				abc.methods = new ABCFile.MethodInfo[readU30()];
				for (int i = 0; i < abc.methods.Length; i++)
					abc.methods[i] = readMethodInfo();

				abc.metadata = new ABCFile.Metadata[readU30()];
				for (int i = 0; i < abc.metadata.Length; i++)
					abc.metadata[i] = readMetadata();

				abc.instances = new ABCFile.Instance[readU30()];
				for (int i = 0; i < abc.instances.Length; i++)
					abc.instances[i] = readInstance();

				abc.classes = new ABCFile.Class[abc.instances.Length];
				for (int i = 0; i < abc.classes.Length; i++)
					abc.classes[i] = readClass();

				abc.scripts = new ABCFile.Script[readU30()];
				for (int i = 0; i < abc.scripts.Length; i++)
					abc.scripts[i] = readScript();

				abc.bodies = new ABCFile.MethodBody[readU30()];
				for (int i = 0; i < abc.bodies.Length; i++)
					abc.bodies[i] = readMethodBody();
			}
			catch (Exception e)
			{
				throw new Exception(string.Format("Error at {0} (0x{1:x8}):", pos, pos), e);
			}
		}

		private byte readU8()
		{
			return buf[pos++];
		}

		private ushort readU16()
		{
			return (ushort)(readU8() | readU8() << 8);
		}

		private int readS24()
		{
			return (int)((uint)(readU8() | readU8() << 8) | (uint)(readU8() << 24) >> 8);
		}

		/// Note: may return values larger than 0xFFFFFFFF.
		private ulong readU32()
		{
			Func<ulong> next = () => { return readU8(); }; // force ulong

			ulong result = next();
			if (0 == (result & 0x00000080))
				return result;
			result = result & 0x0000007f | next() << 7;
			if (0 == (result & 0x00004000))
				return result;
			result = result & 0x00003fff | next() << 14;
			if (0 == (result & 0x00200000))
				return result;
			result = result & 0x001fffff | next() << 21;
			if (0 == (result & 0x10000000))
				return result;
			return result & 0x0fffffff | next() << 28;
		}

		private long readS32()
		{
			ulong l = readU32();
			if ((l & 0xFFFFFFFF00000000) != 0) // preserve unused bits
				return (long)(l | 0xFFFFFFF000000000);
			return (int)l;
		}

		private uint readU30()
		{
			return (uint)readU32() & 0x3FFFFFFF;
		}

		private void readExact(byte[] buff, int len)
		{
			Buffer.BlockCopy(buf, (int)pos, buff, 0, len);
			pos += (uint)len;
		}

		private double readD64()
		{
			double retVal = BitConverter.ToDouble(buf, (int)pos);
			pos += 8;
			return retVal;
		}

		private string readString()
		{
			var buf = new byte[readU30()];
			readExact(buf, buf.Length);
			string s = Encoding.UTF8.GetString(buf);
			return s;
		}

		private byte[] readBytes()
		{
			var r = new byte[readU30()];
			readExact(r, r.Length);
			return r;
		}

		private ABCFile.Namespace readNamespace()
		{
			var r = new ABCFile.Namespace();
			r.kind = (ASType)readU8();
			r.name = readU30();
			return r;
		}

		private uint[] readNamespaceSet()
		{
			var r = new uint[readU30()];
			for (int i = 0; i < r.Length; i++)
				r[i] = readU30();
			return r;
		}

		private ABCFile.Multiname readMultiname()
		{
			var r = new ABCFile.Multiname();
			r.kind = (ASType)readU8();
			switch (r.kind)
			{
				case ASType.QName:
				case ASType.QNameA:
					r.QName.ns = readU30();
					r.QName.name = readU30();
					break;
				case ASType.RTQName:
				case ASType.RTQNameA:
					r.RTQName.name = readU30();
					break;
				case ASType.RTQNameL:
				case ASType.RTQNameLA:
					break;
				case ASType.Multiname:
				case ASType.MultinameA:
					r.MultiName.name = readU30();
					r.MultiName.nsSet = readU30();
					break;
				case ASType.MultinameL:
				case ASType.MultinameLA:
					r.MultinameL.nsSet = readU30();
					break;
				case ASType.TypeName:
					r.TypeName.name = readU30();
					r.TypeName.parameters = new uint[readU30()];
					for (int i = 0; i < r.TypeName.parameters.Length; i++)
						r.TypeName.parameters[i] = readU30();
					break;
				default:
					throw new Exception("Unknown Multiname kind");
			}
			return r;
		}

		private ABCFile.MethodInfo readMethodInfo()
		{
			var r = new ABCFile.MethodInfo();
			r.paramTypes = new uint[readU30()];
			r.returnType = readU30();
			for (int i = 0; i < r.paramTypes.Length; i++)
				r.paramTypes[i] = readU30();
			r.name = readU30();
			r.flags = (MethodFlags)readU8();
			if ((r.flags & MethodFlags.HAS_OPTIONAL) != 0)
			{
				r.options = new ABCFile.OptionDetail[readU30()];
				for (int i = 0; i < r.options.Length; i++)
					r.options[i] = readOptionDetail();
			}
			if ((r.flags & MethodFlags.HAS_PARAM_NAMES) != 0)
			{
				r.paramNames = new uint[readU30()];
				for (int i = 0; i < r.paramNames.Length; i++)
					r.paramNames[i] = readU30();
			}
			return r;
		}

		private ABCFile.OptionDetail readOptionDetail()
		{
			var r = new ABCFile.OptionDetail();
			r.val = readU30();
			r.kind = (ASType)readU8();
			return r;
		}

		private ABCFile.Metadata readMetadata()
		{
			var r = new ABCFile.Metadata();
			r.name = readU30();
			uint len = readU30();
			r.keys = new uint[len];
			r.values = new uint[len];
			for (int i = 0; i < r.keys.Length; i++)
				r.keys[i] = readU30();
			for (int i = 0; i < r.values.Length; i++)
				r.values[i] = readU30();
			return r;
		}

		private ABCFile.Instance readInstance()
		{
			var r = new ABCFile.Instance();
			r.name = readU30();
			r.superName = readU30();
			r.flags = (InstanceFlags)readU8();
			if ((r.flags & InstanceFlags.ProtectedNs) != 0)
				r.protectedNs = readU30();
			r.interfaces = new uint[readU30()];
			for (int i = 0; i < r.interfaces.Length; i++)
				r.interfaces[i] = readU30();
			r.iinit = readU30();
			r.traits = new ABCFile.TraitsInfo[readU30()];
			for (int i = 0; i < r.traits.Length; i++)
				r.traits[i] = readTrait();

			return r;
		}

		private ABCFile.TraitsInfo readTrait()
		{
			var r = new ABCFile.TraitsInfo();
			r.name = readU30();
			r.kindAttr = readU8();
			switch (r.kind)
			{
				case TraitKind.Slot:
				case TraitKind.Const:
					r.Slot.slotId = readU30();
					r.Slot.typeName = readU30();
					r.Slot.vindex = readU30();
					if (r.Slot.vindex != 0)
						r.Slot.vkind = (ASType)readU8();
					else
						r.Slot.vkind = ASType.Void;
					break;
				case TraitKind.Class:
					r.Class.slotId = readU30();
					r.Class.classi = readU30();
					break;
				case TraitKind.Function:
					r.Function.slotId = readU30();
					r.Function.functioni = readU30();
					break;
				case TraitKind.Method:
				case TraitKind.Getter:
				case TraitKind.Setter:
					r.Method.dispId = readU30();
					r.Method.method = readU30();
					break;
				default:
					throw new Exception("Unknown trait kind");
			}
			if ((r.attr & TraitAttributes.Metadata) != 0)
			{
				r.metadata = new uint[readU30()];
				for (int i = 0; i < r.metadata.Length; i++)
					r.metadata[i] = readU30();
			}
			return r;
		}

		private ABCFile.Class readClass()
		{
			var r = new ABCFile.Class();
			r.cinit = readU30();
			r.traits = new ABCFile.TraitsInfo[readU30()];
			for (int i = 0; i < r.traits.Length; i++)
				r.traits[i] = readTrait();
			return r;
		}

		private ABCFile.Script readScript()
		{
			var r = new ABCFile.Script();
			r.sinit = readU30();
			r.traits = new ABCFile.TraitsInfo[readU30()];
			for (int i = 0; i < r.traits.Length; i++)
				r.traits[i] = readTrait();
			return r;
		}

		private ABCFile.MethodBody readMethodBody()
		{
			var r = new ABCFile.MethodBody();
			r.method = readU30();
			r.maxStack = readU30();
			r.localCount = readU30();
			r.initScopeDepth = readU30();
			r.maxScopeDepth = readU30();
			r.instructions = null;

			uint len = readU30();
			var instructionAtOffset = new uint[len];
			r.rawBytes = new byte[len];
			Buffer.BlockCopy(buf, (int)pos, r.rawBytes, 0, (int)len);

			Func<ABCFile.Label, ABCFile.Label> translateLabel = (ABCFile.Label label) =>
			{
				uint absoluteOffset = label.absoluteOffset;
				uint instructionOffset = absoluteOffset;
				while (true)
				{
					if (instructionOffset >= len)
					{
						label.index = (uint)r.instructions.Length;
						instructionOffset = len;
						break;
					}
					if (instructionOffset <= 0)
					{
						label.index = 0;
						instructionOffset = 0;
						break;
					}
					if (instructionAtOffset[instructionOffset] != uint.MaxValue)
					{
						label.index = instructionAtOffset[instructionOffset];
						break;
					}
					instructionOffset--;
				}
				label.offset = (int)(absoluteOffset - instructionOffset);
				return label;
			};

			uint start = pos;
			uint end = pos + len;

			Func<uint> offset = () => { return pos - start; };

			try
			{
				for (int i = 0; i < instructionAtOffset.Length; i++)
					instructionAtOffset[i] = uint.MaxValue;
				var instructionOffsets = new List<uint>();
				var instrs = new List<ABCFile.Instruction>();
				while (pos < end)
				{
					uint instructionOffset = offset();
					pos = start + instructionOffset;
					instructionAtOffset[instructionOffset] = (uint)instrs.Count;
					ABCFile.Instruction instruction;
					instruction.opcode = (Opcode)readU8();
					var opcodeInfo = OpcodeInfo.opcodeDict[instruction.opcode];
					instruction.arguments = new ABCFile.Instruction.Argument[opcodeInfo.argumentTypes.Length];
					for (int i = 0; i < instruction.arguments.Length; i++)
					{
						var type = opcodeInfo.argumentTypes[i];
						switch (type)
						{
							case OpcodeArgumentType.Unknown:
								throw new Exception("Don't know how to decode OP_" + opcodeInfo.name);

							case OpcodeArgumentType.UByteLiteral:
								instruction.arguments[i].ubytev = readU8();
								break;
							case OpcodeArgumentType.IntLiteral:
								instruction.arguments[i].intv = readS32();
								break;
							case OpcodeArgumentType.UIntLiteral:
								instruction.arguments[i].uintv = readU32();
								break;

							case OpcodeArgumentType.Int:
							case OpcodeArgumentType.UInt:
							case OpcodeArgumentType.Double:
							case OpcodeArgumentType.String:
							case OpcodeArgumentType.Namespace:
							case OpcodeArgumentType.Multiname:
							case OpcodeArgumentType.Class:
							case OpcodeArgumentType.Method:
								instruction.arguments[i].index = readU30();
								break;

							case OpcodeArgumentType.JumpTarget:
								int delta = readS24();
								instruction.arguments[i].jumpTarget.absoluteOffset = (uint)(offset() + delta);
								break;

							case OpcodeArgumentType.SwitchDefaultTarget:
								instruction.arguments[i].jumpTarget.absoluteOffset = (uint)(instructionOffset + readS24());
								break;

							case OpcodeArgumentType.SwitchTargets:
								instruction.arguments[i].switchTargets = new ABCFile.Label[readU30() + 1];
								for (int j = 0; j < instruction.arguments[i].switchTargets.Length; j++)
									instruction.arguments[i].switchTargets[j].absoluteOffset = (uint)(instructionOffset + readS24());
								break;
						}
					}
					instrs.Add(instruction);
					instructionOffsets.Add(instructionOffset);
				}
				r.instructions = instrs.ToArray();

				if (pos > end)
					throw new Exception("Out-of-bounds code read error");

				// convert jump target offsets to instruction indices
				for (int i = 0; i < r.instructions.Length; i++)
				{
					var opcodeInfo = OpcodeInfo.opcodeDict[r.instructions[i].opcode];
					for (int j = 0; j < opcodeInfo.argumentTypes.Length; j++)
						switch (opcodeInfo.argumentTypes[j])
						{
							case OpcodeArgumentType.JumpTarget:
							case OpcodeArgumentType.SwitchDefaultTarget:
								r.instructions[i].arguments[j].jumpTarget = translateLabel(r.instructions[i].arguments[j].jumpTarget);
								break;
							case OpcodeArgumentType.SwitchTargets:
								for (int k = 0; k < r.instructions[i].arguments[j].switchTargets.Length; k++)
									r.instructions[i].arguments[j].switchTargets[k] =
										translateLabel(r.instructions[i].arguments[j].switchTargets[k]);
								break;
							default:
								break;
						}
				}
			}
			catch (Exception e)
			{
				r.instructions = null;
				r.error = e.Message;
				instructionAtOffset.Initialize();
			}
			pos = end;

			r.exceptions = new ABCFile.ExceptionInfo[readU30()];
			for (int i = 0; i < r.exceptions.Length; i++)
			{
				r.exceptions[i] = readExceptionInfo();
				r.exceptions[i].from = translateLabel(r.exceptions[i].from);
				r.exceptions[i].to = translateLabel(r.exceptions[i].to);
				r.exceptions[i].target = translateLabel(r.exceptions[i].target);
			}
			r.traits = new ABCFile.TraitsInfo[readU30()];
			for (int i = 0; i < r.traits.Length; i++)
				r.traits[i] = readTrait();
			return r;
		}

		private ABCFile.ExceptionInfo readExceptionInfo()
		{
			var r = new ABCFile.ExceptionInfo();
			r.from.absoluteOffset = readU30();
			r.to.absoluteOffset = readU30();
			r.target.absoluteOffset = readU30();
			r.excType = readU30();
			r.varName = readU30();
			return r;
		}
	}
}