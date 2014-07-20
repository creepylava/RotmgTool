using System;
using System.Collections.Generic;
using System.Linq;

namespace RotmgTool.SWF
{
	internal class ABCFile
	{
		public ushort minorVersion, majorVersion;

		public long?[] ints;
		public ulong?[] uints;
		public double?[] doubles;
		public string[] strings;
		public Namespace[] namespaces;
		public uint[][] namespaceSets;
		public Multiname[] multinames;

		public MethodInfo[] methods;
		public Metadata[] metadata;
		public Instance[] instances;
		public Class[] classes;
		public Script[] scripts;
		public MethodBody[] bodies;

		public ABCFile()
		{
			majorVersion = 46;
			minorVersion = 16;

			ints = new long?[1];

			uints = new ulong?[1];

			doubles = new double?[1];

			strings = new string[1];
			namespaces = new Namespace[1];
			namespaceSets = new uint[1][];
			multinames = new Multiname[1];
		}

		public struct Namespace
		{
			public ASType kind;
			public uint name;
		}

		public struct Multiname
		{
			public ASType kind;

			public struct _QName
			{
				public uint ns, name;
			}

			public _QName QName;

			public struct _RTQName
			{
				public uint name;
			}

			public _RTQName RTQName;

			public struct _Multiname
			{
				public uint name, nsSet;
			}

			public _Multiname MultiName;

			public struct _MultinameL
			{
				public uint nsSet;
			}

			public _MultinameL MultinameL;

			public struct _TypeName
			{
				public uint name;
				public uint[] parameters;
			}

			public _TypeName TypeName;
		}

		public struct MethodInfo
		{
			public uint[] paramTypes;
			public uint returnType;
			public uint name;
			public MethodFlags flags; // MethodFlags bitmask
			public OptionDetail[] options;
			public uint[] paramNames;
		}

		public struct OptionDetail
		{
			public uint val;
			public ASType kind;
		}

		public struct Metadata
		{
			public uint name;
			public uint[] keys, values;
		}

		public struct Instance
		{
			public uint name;
			public uint superName;
			public InstanceFlags flags; // InstanceFlags bitmask
			public uint protectedNs;
			public uint[] interfaces;
			public uint iinit;
			public TraitsInfo[] traits;
		}

		public struct TraitsInfo
		{
			public uint name;
			public byte kindAttr;

			public struct _Slot
			{
				public uint slotId;
				public uint typeName;
				public uint vindex;
				public ASType vkind;
			}

			public _Slot Slot;

			public struct _Class
			{
				public uint slotId;
				public uint classi;
			}

			public _Class Class;

			public struct _Function
			{
				public uint slotId;
				public uint functioni;
			}

			public _Function Function;

			public struct _Method
			{
				public uint dispId;
				public uint method;
			}

			public _Method Method;
			public uint[] metadata;

			public TraitKind kind
			{
				get { return (TraitKind)(kindAttr & 0xF); }
				set { kindAttr = (byte)((kindAttr & 0xF0) | (byte)value); }
			}

			// TraitAttributes bitmask
			public TraitAttributes attr
			{
				get { return (TraitAttributes)(kindAttr >> 4); }
				set { kindAttr = (byte)((uint)(kindAttr & 0xF) | ((uint)value << 4)); }
			}
		}

		public struct Class
		{
			public uint cinit;
			public TraitsInfo[] traits;
		}

		public struct Script
		{
			public uint sinit;
			public TraitsInfo[] traits;
		}

		public struct MethodBody
		{
			public uint method;
			public uint maxStack;
			public uint localCount;
			public uint initScopeDepth;
			public uint maxScopeDepth;
			public Instruction[] instructions;
			public ExceptionInfo[] exceptions;
			public TraitsInfo[] traits;

			public string error;
			public byte[] rawBytes;
		}

		/// Destination for a jump or exception block boundary
		public struct Label
		{
			public uint index;

			/// instruction index
			public int offset;

			/// signed offset relative to said instruction
			public uint absoluteOffset; /// internal temporary value used during reading and writing
		}

		public struct Instruction
		{
			public Opcode opcode;

			public struct Argument
			{
				public byte ubytev;
				public long intv;
				public ulong uintv;
				public uint index;

				public Label jumpTarget;
				public Label[] switchTargets;
			}

			public Argument[] arguments;
		}

		public struct ExceptionInfo
		{
			public Label from, to, target;
			public uint excType;
			public uint varName;
		}
	}


	internal enum ASType : byte
	{
		Void = 0x00, // not actually interned
		Undefined = Void,
		Utf8 = 0x01,
		Decimal = 0x02,
		Integer = 0x03,
		UInteger = 0x04,
		PrivateNamespace = 0x05,
		Double = 0x06,
		QName = 0x07, // ns::name, const ns, const name
		Namespace = 0x08,
		Multiname = 0x09, //[ns...]::name, const [ns...], const name
		False = 0x0A,
		True = 0x0B,
		Null = 0x0C,
		QNameA = 0x0D, // @ns::name, const ns, const name
		MultinameA = 0x0E, // @[ns...]::name, const [ns...], const name
		RTQName = 0x0F, // ns::name, var ns, const name
		RTQNameA = 0x10, // @ns::name, var ns, const name
		RTQNameL = 0x11, // ns::[name], var ns, var name
		RTQNameLA = 0x12, // @ns::[name], var ns, var name
		Namespace_Set = 0x15, // a set of namespaces - used by multiname
		PackageNamespace = 0x16, // a namespace that was derived from a package
		PackageInternalNs = 0x17, // a namespace that had no uri
		ProtectedNamespace = 0x18,
		ExplicitNamespace = 0x19,
		StaticProtectedNs = 0x1A,
		MultinameL = 0x1B,
		MultinameLA = 0x1C,
		TypeName = 0x1D,
		Max
	}

	/* These enumerations are as they are documented in the AVM bytecode specification.
       They are actually a single enumeration (see above), but in some contexts only certain values are valid.

    enum NamespaceKind : ubyte
    {
        Namespace = 0x08,
        PackageNamespace = 0x16,
        PackageInternalNs = 0x17,
        ProtectedNamespace = 0x18,
        ExplicitNamespace = 0x19,
        StaticProtectedNs = 0x1A,
        PrivateNs = 0x05
    }

    enum MultinameKind : ubyte
    {
        QName = 0x07,
        QNameA = 0x0D,
        RTQName = 0x0F,
        RTQNameA = 0x10,
        RTQNameL = 0x11,
        RTQNameLA = 0x12,
        Multiname = 0x09,
        MultinameA = 0x0E,
        MultinameL = 0x1B,
        MultinameLA = 0x1C
    }

    enum ConstantKind : ubyte
    {
        Int = 0x03, // integer
        UInt = 0x04, // uinteger
        Double = 0x06, // double
        Utf8 = 0x01, // string
        True = 0x0B, // -
        False = 0x0A, // -
        Null = 0x0C, // -
        Undefined = 0x00, // -
        Namespace = 0x08, // namespace
        PackageNamespace = 0x16, // namespace
        PackageInternalNs = 0x17, // Namespace
        ProtectedNamespace = 0x18, // Namespace
        ExplicitNamespace = 0x19, // Namespace
        StaticProtectedNs = 0x1A, // Namespace
        PrivateNs = 0x05, // namespace
    }
    */

	internal enum MethodFlags : byte
	{
		NEED_ARGUMENTS = 0x01,
		// Suggests to the run-time that an "arguments" object (as specified by the ActionScript 3.0 Language Reference) be created. Must not be used together with NEED_REST. See Chapter 3.
		NEED_ACTIVATION = 0x02, // Must be set if this method uses the newactivation opcode.
		NEED_REST = 0x04,
		// This flag creates an ActionScript 3.0 rest arguments array. Must not be used with NEED_ARGUMENTS. See Chapter 3.
		HAS_OPTIONAL = 0x08,
		// Must be set if this method has optional parameters and the options field is present in this method_info structure.
		SET_DXNS = 0x40, // Must be set if this method uses the dxns or dxnslate opcodes.
		HAS_PARAM_NAMES = 0x80, // Must be set when the param_names field is present in this method_info structure.
	}

	internal enum InstanceFlags : byte
	{
		Sealed = 0x01, // The class is sealed: properties can not be dynamically added to instances of the class.
		Final = 0x02, // The class is final: it cannot be a base class for any other class.
		Interface = 0x04, // The class is an interface.
		ProtectedNs = 0x08,
		// The class uses its protected namespace and the protectedNs field is present in the interface_info structure.
	}

	internal enum TraitKind : byte
	{
		Slot = 0,
		Method = 1,
		Getter = 2,
		Setter = 3,
		Class = 4,
		Function = 5,
		Const = 6,
	}

	internal enum TraitAttributes : byte
	{
		Final = 1,
		Override = 2,
		Metadata = 4
	}

	internal enum Opcode : byte
	{
		OP_bkpt = 0x01,
		OP_nop = 0x02,
		OP_throw = 0x03,
		OP_getsuper = 0x04,
		OP_setsuper = 0x05,
		OP_dxns = 0x06,
		OP_dxnslate = 0x07,
		OP_kill = 0x08,
		OP_label = 0x09,
		OP_ifnlt = 0x0C,
		OP_ifnle = 0x0D,
		OP_ifngt = 0x0E,
		OP_ifnge = 0x0F,
		OP_jump = 0x10,
		OP_iftrue = 0x11,
		OP_iffalse = 0x12,
		OP_ifeq = 0x13,
		OP_ifne = 0x14,
		OP_iflt = 0x15,
		OP_ifle = 0x16,
		OP_ifgt = 0x17,
		OP_ifge = 0x18,
		OP_ifstricteq = 0x19,
		OP_ifstrictne = 0x1A,
		OP_lookupswitch = 0x1B,
		OP_pushwith = 0x1C,
		OP_popscope = 0x1D,
		OP_nextname = 0x1E,
		OP_hasnext = 0x1F,
		OP_pushnull = 0x20,
		OP_pushundefined = 0x21,
		OP_pushuninitialized = 0x22,
		OP_nextvalue = 0x23,
		OP_pushbyte = 0x24,
		OP_pushshort = 0x25,
		OP_pushtrue = 0x26,
		OP_pushfalse = 0x27,
		OP_pushnan = 0x28,
		OP_pop = 0x29,
		OP_dup = 0x2A,
		OP_swap = 0x2B,
		OP_pushstring = 0x2C,
		OP_pushint = 0x2D,
		OP_pushuint = 0x2E,
		OP_pushdouble = 0x2F,
		OP_pushscope = 0x30,
		OP_pushnamespace = 0x31,
		OP_hasnext2 = 0x32,
		OP_pushdecimal = 0x33,
		OP_pushdnan = 0x34,
		OP_li8 = 0x35,
		OP_li16 = 0x36,
		OP_li32 = 0x37,
		OP_lf32 = 0x38,
		OP_lf64 = 0x39,
		OP_si8 = 0x3A,
		OP_si16 = 0x3B,
		OP_si32 = 0x3C,
		OP_sf32 = 0x3D,
		OP_sf64 = 0x3E,
		OP_newfunction = 0x40,
		OP_call = 0x41,
		OP_construct = 0x42,
		OP_callmethod = 0x43,
		OP_callstatic = 0x44,
		OP_callsuper = 0x45,
		OP_callproperty = 0x46,
		OP_returnvoid = 0x47,
		OP_returnvalue = 0x48,
		OP_constructsuper = 0x49,
		OP_constructprop = 0x4A,
		OP_callsuperid = 0x4B,
		OP_callproplex = 0x4C,
		OP_callinterface = 0x4D,
		OP_callsupervoid = 0x4E,
		OP_callpropvoid = 0x4F,
		OP_sxi1 = 0x50,
		OP_sxi8 = 0x51,
		OP_sxi16 = 0x52,
		OP_applytype = 0x53,
		OP_newobject = 0x55,
		OP_newarray = 0x56,
		OP_newactivation = 0x57,
		OP_newclass = 0x58,
		OP_getdescendants = 0x59,
		OP_newcatch = 0x5A,
		OP_deldescendants = 0x5B,
		OP_findpropstrict = 0x5D,
		OP_findproperty = 0x5E,
		OP_finddef = 0x5F,
		OP_getlex = 0x60,
		OP_setproperty = 0x61,
		OP_getlocal = 0x62,
		OP_setlocal = 0x63,
		OP_getglobalscope = 0x64,
		OP_getscopeobject = 0x65,
		OP_getproperty = 0x66,
		OP_getpropertylate = 0x67,
		OP_initproperty = 0x68,
		OP_setpropertylate = 0x69,
		OP_deleteproperty = 0x6A,
		OP_deletepropertylate = 0x6B,
		OP_getslot = 0x6C,
		OP_setslot = 0x6D,
		OP_getglobalslot = 0x6E,
		OP_setglobalslot = 0x6F,
		OP_convert_s = 0x70,
		OP_esc_xelem = 0x71,
		OP_esc_xattr = 0x72,
		OP_convert_i = 0x73,
		OP_convert_u = 0x74,
		OP_convert_d = 0x75,
		OP_convert_b = 0x76,
		OP_convert_o = 0x77,
		OP_checkfilter = 0x78,
		OP_convert_m = 0x79,
		OP_convert_m_p = 0x7A,
		OP_coerce = 0x80,
		OP_coerce_b = 0x81,
		OP_coerce_a = 0x82,
		OP_coerce_i = 0x83,
		OP_coerce_d = 0x84,
		OP_coerce_s = 0x85,
		OP_astype = 0x86,
		OP_astypelate = 0x87,
		OP_coerce_u = 0x88,
		OP_coerce_o = 0x89,
		OP_negate_p = 0x8F,
		OP_negate = 0x90,
		OP_increment = 0x91,
		OP_inclocal = 0x92,
		OP_decrement = 0x93,
		OP_declocal = 0x94,
		OP_typeof = 0x95,
		OP_not = 0x96,
		OP_bitnot = 0x97,
		OP_concat = 0x9A,
		OP_add_d = 0x9B,
		OP_increment_p = 0x9C,
		OP_inclocal_p = 0x9D,
		OP_decrement_p = 0x9E,
		OP_declocal_p = 0x9F,
		OP_add = 0xA0,
		OP_subtract = 0xA1,
		OP_multiply = 0xA2,
		OP_divide = 0xA3,
		OP_modulo = 0xA4,
		OP_lshift = 0xA5,
		OP_rshift = 0xA6,
		OP_urshift = 0xA7,
		OP_bitand = 0xA8,
		OP_bitor = 0xA9,
		OP_bitxor = 0xAA,
		OP_equals = 0xAB,
		OP_strictequals = 0xAC,
		OP_lessthan = 0xAD,
		OP_lessequals = 0xAE,
		OP_greaterthan = 0xAF,
		OP_greaterequals = 0xB0,
		OP_instanceof = 0xB1,
		OP_istype = 0xB2,
		OP_istypelate = 0xB3,
		OP_in = 0xB4,
		OP_add_p = 0xB5,
		OP_subtract_p = 0xB6,
		OP_multiply_p = 0xB7,
		OP_divide_p = 0xB8,
		OP_modulo_p = 0xB9,
		OP_increment_i = 0xC0,
		OP_decrement_i = 0xC1,
		OP_inclocal_i = 0xC2,
		OP_declocal_i = 0xC3,
		OP_negate_i = 0xC4,
		OP_add_i = 0xC5,
		OP_subtract_i = 0xC6,
		OP_multiply_i = 0xC7,
		OP_getlocal0 = 0xD0,
		OP_getlocal1 = 0xD1,
		OP_getlocal2 = 0xD2,
		OP_getlocal3 = 0xD3,
		OP_setlocal0 = 0xD4,
		OP_setlocal1 = 0xD5,
		OP_setlocal2 = 0xD6,
		OP_setlocal3 = 0xD7,
		OP_debug = 0xEF,
		OP_debugline = 0xF0,
		OP_debugfile = 0xF1,
		OP_bkptline = 0xF2,
		OP_timestamp = 0xF3,
	}

	internal enum OpcodeArgumentType
	{
		Unknown,

		UByteLiteral,
		IntLiteral,
		UIntLiteral,

		Int,
		UInt,
		Double,
		String,
		Namespace,
		Multiname,
		Class,
		Method,

		JumpTarget,
		SwitchDefaultTarget,
		SwitchTargets,
	}

	internal struct OpcodeInfo
	{
		public string name;
		public OpcodeArgumentType[] argumentTypes;

		private OpcodeInfo(string name, params OpcodeArgumentType[] args)
		{
			this.name = name;
			argumentTypes = args;
		}

		public static OpcodeInfo[] opcodeInfo =
		{
			new OpcodeInfo("0x00", OpcodeArgumentType.Unknown),
			new OpcodeInfo("bkpt", OpcodeArgumentType.Unknown),
			new OpcodeInfo("nop"),
			new OpcodeInfo("throw"),
			new OpcodeInfo("getsuper", OpcodeArgumentType.Multiname),
			new OpcodeInfo("setsuper", OpcodeArgumentType.Multiname),
			new OpcodeInfo("dxns", OpcodeArgumentType.String),
			new OpcodeInfo("dxnslate"),
			new OpcodeInfo("kill", OpcodeArgumentType.UIntLiteral),
			new OpcodeInfo("label"),
			new OpcodeInfo("0x0A", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0x0B", OpcodeArgumentType.Unknown),
			new OpcodeInfo("ifnlt", OpcodeArgumentType.JumpTarget),
			new OpcodeInfo("ifnle", OpcodeArgumentType.JumpTarget),
			new OpcodeInfo("ifngt", OpcodeArgumentType.JumpTarget),
			new OpcodeInfo("ifnge", OpcodeArgumentType.JumpTarget),
			new OpcodeInfo("jump", OpcodeArgumentType.JumpTarget),
			new OpcodeInfo("iftrue", OpcodeArgumentType.JumpTarget),
			new OpcodeInfo("iffalse", OpcodeArgumentType.JumpTarget),
			new OpcodeInfo("ifeq", OpcodeArgumentType.JumpTarget),
			new OpcodeInfo("ifne", OpcodeArgumentType.JumpTarget),
			new OpcodeInfo("iflt", OpcodeArgumentType.JumpTarget),
			new OpcodeInfo("ifle", OpcodeArgumentType.JumpTarget),
			new OpcodeInfo("ifgt", OpcodeArgumentType.JumpTarget),
			new OpcodeInfo("ifge", OpcodeArgumentType.JumpTarget),
			new OpcodeInfo("ifstricteq", OpcodeArgumentType.JumpTarget),
			new OpcodeInfo("ifstrictne", OpcodeArgumentType.JumpTarget),
			new OpcodeInfo("lookupswitch", OpcodeArgumentType.SwitchDefaultTarget, OpcodeArgumentType.SwitchTargets),
			new OpcodeInfo("pushwith"),
			new OpcodeInfo("popscope"),
			new OpcodeInfo("nextname"),
			new OpcodeInfo("hasnext"),
			new OpcodeInfo("pushnull"),
			new OpcodeInfo("pushundefined"),
			new OpcodeInfo("pushuninitialized", OpcodeArgumentType.Unknown),
			new OpcodeInfo("nextvalue"),
			new OpcodeInfo("pushbyte", OpcodeArgumentType.UByteLiteral),
			new OpcodeInfo("pushshort", OpcodeArgumentType.IntLiteral),
			new OpcodeInfo("pushtrue"),
			new OpcodeInfo("pushfalse"),
			new OpcodeInfo("pushnan"),
			new OpcodeInfo("pop"),
			new OpcodeInfo("dup"),
			new OpcodeInfo("swap"),
			new OpcodeInfo("pushstring", OpcodeArgumentType.String),
			new OpcodeInfo("pushint", OpcodeArgumentType.Int),
			new OpcodeInfo("pushuint", OpcodeArgumentType.UInt),
			new OpcodeInfo("pushdouble", OpcodeArgumentType.Double),
			new OpcodeInfo("pushscope"),
			new OpcodeInfo("pushnamespace", OpcodeArgumentType.Namespace),
			new OpcodeInfo("hasnext2", OpcodeArgumentType.UIntLiteral, OpcodeArgumentType.UIntLiteral),
			new OpcodeInfo("pushdecimal", OpcodeArgumentType.Unknown),
			new OpcodeInfo("pushdnan", OpcodeArgumentType.Unknown),
			new OpcodeInfo("li8"),
			new OpcodeInfo("li16"),
			new OpcodeInfo("li32"),
			new OpcodeInfo("lf32"),
			new OpcodeInfo("lf64"),
			new OpcodeInfo("si8"),
			new OpcodeInfo("si16"),
			new OpcodeInfo("si32"),
			new OpcodeInfo("sf32"),
			new OpcodeInfo("sf64"),
			new OpcodeInfo("0x3F", OpcodeArgumentType.Unknown),
			new OpcodeInfo("newfunction", OpcodeArgumentType.Method),
			new OpcodeInfo("call", OpcodeArgumentType.UIntLiteral),
			new OpcodeInfo("construct", OpcodeArgumentType.UIntLiteral),
			new OpcodeInfo("callmethod", OpcodeArgumentType.UIntLiteral, OpcodeArgumentType.UIntLiteral),
			new OpcodeInfo("callstatic", OpcodeArgumentType.Method, OpcodeArgumentType.UIntLiteral),
			new OpcodeInfo("callsuper", OpcodeArgumentType.Multiname, OpcodeArgumentType.UIntLiteral),
			new OpcodeInfo("callproperty", OpcodeArgumentType.Multiname, OpcodeArgumentType.UIntLiteral),
			new OpcodeInfo("returnvoid"),
			new OpcodeInfo("returnvalue"),
			new OpcodeInfo("constructsuper", OpcodeArgumentType.UIntLiteral),
			new OpcodeInfo("constructprop", OpcodeArgumentType.Multiname, OpcodeArgumentType.UIntLiteral),
			new OpcodeInfo("callsuperid", OpcodeArgumentType.Unknown),
			new OpcodeInfo("callproplex", OpcodeArgumentType.Multiname, OpcodeArgumentType.UIntLiteral),
			new OpcodeInfo("callinterface", OpcodeArgumentType.Unknown),
			new OpcodeInfo("callsupervoid", OpcodeArgumentType.Multiname, OpcodeArgumentType.UIntLiteral),
			new OpcodeInfo("callpropvoid", OpcodeArgumentType.Multiname, OpcodeArgumentType.UIntLiteral),
			new OpcodeInfo("sxi1"),
			new OpcodeInfo("sxi8"),
			new OpcodeInfo("sxi16"),
			new OpcodeInfo("applytype", OpcodeArgumentType.UIntLiteral),
			new OpcodeInfo("0x54", OpcodeArgumentType.Unknown),
			new OpcodeInfo("newobject", OpcodeArgumentType.UIntLiteral),
			new OpcodeInfo("newarray", OpcodeArgumentType.UIntLiteral),
			new OpcodeInfo("newactivation"),
			new OpcodeInfo("newclass", OpcodeArgumentType.Class),
			new OpcodeInfo("getdescendants", OpcodeArgumentType.Multiname),
			new OpcodeInfo("newcatch", OpcodeArgumentType.UIntLiteral),
			new OpcodeInfo("deldescendants", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0x5C", OpcodeArgumentType.Unknown),
			new OpcodeInfo("findpropstrict", OpcodeArgumentType.Multiname),
			new OpcodeInfo("findproperty", OpcodeArgumentType.Multiname),
			new OpcodeInfo("finddef", OpcodeArgumentType.Multiname),
			new OpcodeInfo("getlex", OpcodeArgumentType.Multiname),
			new OpcodeInfo("setproperty", OpcodeArgumentType.Multiname),
			new OpcodeInfo("getlocal", OpcodeArgumentType.UIntLiteral),
			new OpcodeInfo("setlocal", OpcodeArgumentType.UIntLiteral),
			new OpcodeInfo("getglobalscope"),
			new OpcodeInfo("getscopeobject", OpcodeArgumentType.UByteLiteral),
			new OpcodeInfo("getproperty", OpcodeArgumentType.Multiname),
			new OpcodeInfo("getpropertylate"),
			new OpcodeInfo("initproperty", OpcodeArgumentType.Multiname),
			new OpcodeInfo("setpropertylate"),
			new OpcodeInfo("deleteproperty", OpcodeArgumentType.Multiname),
			new OpcodeInfo("deletepropertylate"),
			new OpcodeInfo("getslot", OpcodeArgumentType.UIntLiteral),
			new OpcodeInfo("setslot", OpcodeArgumentType.UIntLiteral),
			new OpcodeInfo("getglobalslot", OpcodeArgumentType.UIntLiteral),
			new OpcodeInfo("setglobalslot", OpcodeArgumentType.UIntLiteral),
			new OpcodeInfo("convert_s"),
			new OpcodeInfo("esc_xelem"),
			new OpcodeInfo("esc_xattr"),
			new OpcodeInfo("convert_i"),
			new OpcodeInfo("convert_u"),
			new OpcodeInfo("convert_d"),
			new OpcodeInfo("convert_b"),
			new OpcodeInfo("convert_o"),
			new OpcodeInfo("checkfilter"),
			new OpcodeInfo("convert_m", OpcodeArgumentType.Unknown),
			new OpcodeInfo("convert_m_p", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0x7B", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0x7C", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0x7D", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0x7E", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0x7F", OpcodeArgumentType.Unknown),
			new OpcodeInfo("coerce", OpcodeArgumentType.Multiname),
			new OpcodeInfo("coerce_b"),
			new OpcodeInfo("coerce_a"),
			new OpcodeInfo("coerce_i"),
			new OpcodeInfo("coerce_d"),
			new OpcodeInfo("coerce_s"),
			new OpcodeInfo("astype", OpcodeArgumentType.Multiname),
			new OpcodeInfo("astypelate"),
			new OpcodeInfo("coerce_u", OpcodeArgumentType.Unknown),
			new OpcodeInfo("coerce_o", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0x8A", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0x8B", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0x8C", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0x8D", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0x8E", OpcodeArgumentType.Unknown),
			new OpcodeInfo("negate_p", OpcodeArgumentType.Unknown),
			new OpcodeInfo("negate"),
			new OpcodeInfo("increment"),
			new OpcodeInfo("inclocal", OpcodeArgumentType.UIntLiteral),
			new OpcodeInfo("decrement"),
			new OpcodeInfo("declocal", OpcodeArgumentType.UIntLiteral),
			new OpcodeInfo("typeof"),
			new OpcodeInfo("not"),
			new OpcodeInfo("bitnot"),
			new OpcodeInfo("0x98", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0x99", OpcodeArgumentType.Unknown),
			new OpcodeInfo("concat", OpcodeArgumentType.Unknown),
			new OpcodeInfo("add_d", OpcodeArgumentType.Unknown),
			new OpcodeInfo("increment_p", OpcodeArgumentType.Unknown),
			new OpcodeInfo("inclocal_p", OpcodeArgumentType.Unknown),
			new OpcodeInfo("decrement_p", OpcodeArgumentType.Unknown),
			new OpcodeInfo("declocal_p", OpcodeArgumentType.Unknown),
			new OpcodeInfo("add"),
			new OpcodeInfo("subtract"),
			new OpcodeInfo("multiply"),
			new OpcodeInfo("divide"),
			new OpcodeInfo("modulo"),
			new OpcodeInfo("lshift"),
			new OpcodeInfo("rshift"),
			new OpcodeInfo("urshift"),
			new OpcodeInfo("bitand"),
			new OpcodeInfo("bitor"),
			new OpcodeInfo("bitxor"),
			new OpcodeInfo("equals"),
			new OpcodeInfo("strictequals"),
			new OpcodeInfo("lessthan"),
			new OpcodeInfo("lessequals"),
			new OpcodeInfo("greaterthan"),
			new OpcodeInfo("greaterequals"),
			new OpcodeInfo("instanceof"),
			new OpcodeInfo("istype", OpcodeArgumentType.Multiname),
			new OpcodeInfo("istypelate"),
			new OpcodeInfo("in"),
			new OpcodeInfo("add_p", OpcodeArgumentType.Unknown),
			new OpcodeInfo("subtract_p", OpcodeArgumentType.Unknown),
			new OpcodeInfo("multiply_p", OpcodeArgumentType.Unknown),
			new OpcodeInfo("divide_p", OpcodeArgumentType.Unknown),
			new OpcodeInfo("modulo_p", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xBA", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xBB", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xBC", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xBD", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xBE", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xBF", OpcodeArgumentType.Unknown),
			new OpcodeInfo("increment_i"),
			new OpcodeInfo("decrement_i"),
			new OpcodeInfo("inclocal_i", OpcodeArgumentType.UIntLiteral),
			new OpcodeInfo("declocal_i", OpcodeArgumentType.UIntLiteral),
			new OpcodeInfo("negate_i"),
			new OpcodeInfo("add_i"),
			new OpcodeInfo("subtract_i"),
			new OpcodeInfo("multiply_i"),
			new OpcodeInfo("0xC8", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xC9", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xCA", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xCB", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xCC", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xCD", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xCE", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xCF", OpcodeArgumentType.Unknown),
			new OpcodeInfo("getlocal0"),
			new OpcodeInfo("getlocal1"),
			new OpcodeInfo("getlocal2"),
			new OpcodeInfo("getlocal3"),
			new OpcodeInfo("setlocal0"),
			new OpcodeInfo("setlocal1"),
			new OpcodeInfo("setlocal2"),
			new OpcodeInfo("setlocal3"),
			new OpcodeInfo("0xD8", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xD9", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xDA", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xDB", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xDC", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xDD", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xDE", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xDF", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xE0", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xE1", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xE2", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xE3", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xE4", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xE5", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xE6", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xE7", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xE8", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xE9", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xEA", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xEB", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xEC", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xED", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xEE", OpcodeArgumentType.Unknown),
			new OpcodeInfo("debug", OpcodeArgumentType.UByteLiteral, OpcodeArgumentType.String, OpcodeArgumentType.UByteLiteral,
				OpcodeArgumentType.UIntLiteral),
			new OpcodeInfo("debugline", OpcodeArgumentType.UIntLiteral),
			new OpcodeInfo("debugfile", OpcodeArgumentType.String),
			new OpcodeInfo("bkptline", OpcodeArgumentType.Unknown),
			new OpcodeInfo("timestamp", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xF4", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xF5", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xF6", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xF7", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xF8", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xF9", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xFA", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xFB", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xFC", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xFD", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xFE", OpcodeArgumentType.Unknown),
			new OpcodeInfo("0xFF", OpcodeArgumentType.Unknown)
		};

		public static Dictionary<Opcode, OpcodeInfo> opcodeDict;

		static OpcodeInfo()
		{
			opcodeDict = Enumerable.Range(0, 0x100).ToDictionary(k => (Opcode)k, k => opcodeInfo[k]);
		}
	}
}