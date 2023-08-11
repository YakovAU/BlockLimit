using System;
using System.Runtime.CompilerServices;

[Serializable]
public struct BlockValue : IEquatable<BlockValue>
{
	public BlockValue(uint _rawData)
	{
		this.rawData = _rawData;
		this.damage = 0;
	}

	public BlockValue set(int _type, byte _meta, byte _damage, byte _rotation)
	{
		this.type = _type;
		this.meta = _meta;
		this.damage = (int)_damage;
		this.rotation = _rotation;
		return this;
	}

	public Block Block
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return Block.list[this.type];
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint GetTypeMasked(uint _v)
	{
		return _v & 32767U;
	}

	public bool isair
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return this.type == 0;
		}
	}

	public bool isWater
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return this.type == 32512 || this.type == 32513 || this.type == 32514;
		}
	}

	public int type
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return (int)(this.rawData & 32767U);
		}
		set
		{
			this.rawData = (this.rawData & 4294934528U) | (uint)((long)value & 32767L);
		}
	}

	public byte rotation
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return (byte)((this.rawData & 1015808U) >> 15);
		}
		set
		{
			this.rawData = (this.rawData & 4293951487U) | (uint)((uint)(value & 31) << 15);
		}
	}

	public byte meta
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return (byte)((this.rawData & 15728640U) >> 20);
		}
		set
		{
			this.rawData = (this.rawData & 4279238655U) | (uint)((uint)(value & 15) << 20);
		}
	}

	public byte meta2
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return (byte)((this.rawData & 251658240U) >> 24);
		}
		set
		{
			this.rawData = (this.rawData & 4043309055U) | (uint)((uint)(value & 15) << 24);
		}
	}

	private byte meta3
	{
		get
		{
			return (byte)((this.rawData & 805306368U) >> 28);
		}
		set
		{
			this.rawData = (this.rawData & 3489660927U) | (uint)((uint)(value & 3) << 28);
		}
	}

	public byte meta2and1
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return (byte)(((int)this.meta2 << 4) | (int)this.meta);
		}
		set
		{
			this.meta2 = (byte)((value >> 4) & 15);
			this.meta = value & 15;
		}
	}

	private byte rotationAndMeta3
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return (byte)(((int)this.rotation << 2) | (int)this.meta3);
		}
		set
		{
			this.rotation = (byte)((value >> 2) & 31);
			this.meta3 = value & 3;
		}
	}

	public bool hasdecal
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return (this.rawData & 2147483648U) > 0U;
		}
		set
		{
			this.rawData = (this.rawData & 2147483647U) | (value ? 2147483648U : 0U);
		}
	}

	public BlockFaceFlag rotatedWaterFlowMask
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return BlockFaceFlags.RotateFlags(this.Block.WaterFlowMask, this.rotation);
		}
	}

	public BlockFace decalface
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return (BlockFace)((this.rawData & 15728640U) >> 20);
		}
		set
		{
			this.rawData = (this.rawData & 4279238655U) | (uint)((uint)value << 20);
		}
	}

	public byte decaltex
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return (byte)((this.rawData & 251658240U) >> 24);
		}
		set
		{
			this.rawData = (this.rawData & 4043309055U) | (uint)((uint)value << 24);
		}
	}

	public bool ischild
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			return (this.rawData & 1073741824U) > 0U;
		}
		set
		{
			this.rawData = (this.rawData & 3221225471U) | (value ? 1073741824U : 0U);
		}
	}

	public int parentx
	{
		get
		{
			int num = (int)((this.rawData & 251658240U) >> 24);
			return ((num & 8) != 0) ? (-(num & 7)) : (num & 7);
		}
		set
		{
			int num = ((value < 0) ? (8 | (-value & 7)) : (value & 7));
			this.rawData = (this.rawData & 4043309055U) | (uint)((uint)num << 24);
		}
	}

	public int parenty
	{
		get
		{
			int rotationAndMeta = (int)this.rotationAndMeta3;
			return ((rotationAndMeta & 32) != 0) ? (-(rotationAndMeta & 31)) : (rotationAndMeta & 31);
		}
		set
		{
			int num = ((value < 0) ? (32 | (-value & 31)) : (value & 31));
			this.rotationAndMeta3 = (byte)num;
		}
	}

	public int parentz
	{
		get
		{
			int num = (int)((this.rawData & 15728640U) >> 20);
			return ((num & 8) != 0) ? (-(num & 7)) : (num & 7);
		}
		set
		{
			int num = ((value < 0) ? (8 | (-value & 7)) : (value & 7));
			this.rawData = (this.rawData & 4279238655U) | (uint)((uint)num << 20);
		}
	}

	public Vector3i parent
	{
		get
		{
			return new Vector3i(this.parentx, this.parenty, this.parentz);
		}
		set
		{
			this.parentx = value.x;
			this.parenty = value.y;
			this.parentz = value.z;
		}
	}

	public static uint ConvertOldRawData_2(uint _oldRawData)
	{
		uint num = (_oldRawData & BlockValue.MetadataMaskOld_2) >> BlockValue.MetadataShiftOld_2;
		uint num2 = (_oldRawData & BlockValue.Metadata2MaskOld_2) >> BlockValue.Metadata2ShiftOld_2;
		uint num3 = (_oldRawData & BlockValue.RotationMaskOld_2) >> BlockValue.RotationShiftOld_2;
		return new BlockValue
		{
			type = (int)(_oldRawData & BlockValue.TypeMaskOld_2),
			meta = (byte)num,
			meta2 = (byte)num2,
			rotation = (byte)num3
		}.rawData;
	}

	public static uint ConvertOldRawData_1(uint _oldRawData)
	{
		uint num = (_oldRawData & BlockValue.MetadataMaskOld_1) >> BlockValue.MetadataShiftOld_1;
		uint num2 = (_oldRawData & BlockValue.Metadata2MaskOld_1) >> BlockValue.Metadata2ShiftOld_1;
		uint num3 = (_oldRawData & BlockValue.DamageMaskOld_1) >> BlockValue.DamageShiftOld_1;
		uint num4 = (_oldRawData & BlockValue.RotationMaskOld_1) >> BlockValue.RotationShiftOld_1;
		return new BlockValue
		{
			type = (int)(_oldRawData & 32767U),
			meta = (byte)num,
			meta2 = (byte)num2,
			damage = (int)((byte)num3),
			rotation = (byte)num4
		}.rawData;
	}

	public int GetForceToOtherBlock(BlockValue _other)
	{
		return Utils.FastMin(this.Block.blockMaterial.StabilityGlue, _other.Block.blockMaterial.StabilityGlue);
	}

	public int ToItemType()
	{
		return this.type;
	}

	public ItemValue ToItemValue()
	{
		return new ItemValue
		{
			type = this.type
		};
	}

	public override int GetHashCode()
	{
		return this.type;
	}

	public override bool Equals(object _other)
	{
		return _other is BlockValue && ((BlockValue)_other).type == this.type;
	}

	public bool Equals(BlockValue _other)
	{
		return _other.type == this.type;
	}

	public override string ToString()
	{
		if (!this.ischild)
		{
			return string.Format("id={0} r={1} d={2} m={3} m2={4} m3={5}", new object[] { this.type, this.rotation, this.damage, this.meta, this.meta2, this.meta3 });
		}
		return string.Format("id={0} px={1} py={2} pz={3}", new object[] { this.type, this.parentx, this.parenty, this.parentz });
	}

	public const uint TypeMask = 32767U;

	public const uint RotationMax = 31U;

	private const uint RotationMask = 1015808U;

	private const int RotationShift = 15;

	public const uint MetadataMax = 15U;

	private const uint MetadataMask = 15728640U;

	private const int MetadataShift = 20;

	private const uint Metadata2Mask = 251658240U;

	private const int Metadata2Shift = 24;

	private const uint Metadata3Mask = 805306368U;

	private const int Metadata3Shift = 28;

	private const uint ChildMask = 1073741824U;

	private const int ChildShift = 30;

	private const uint HasDecalMask = 2147483648U;

	private const int HasDecalShift = 31;

	public static BlockValue Air;

	public uint rawData;

	public int damage;

	public static uint TypeMaskOld_2 = 2047U;

	public static uint RotationMaskOld_2 = 63488U;

	public static int RotationShiftOld_2 = 11;

	public static uint MetadataMaskOld_2 = 15728640U;

	public static int MetadataShiftOld_2 = 20;

	public static uint Metadata2MaskOld_2 = 251658240U;

	public static int Metadata2ShiftOld_2 = 24;

	public static uint DamageMaskOld_1 = 983040U;

	public static int DamageShiftOld_1 = 16;

	public static uint RotationMaskOld_1 = 61440U;

	public static int RotationShiftOld_1 = 12;

	public static uint DecalFaceMaskOld_1 = 251658240U;

	public static int DecalFaceShiftOld_1 = 24;

	public static uint MetadataMaskOld_1 = 15728640U;

	public static int MetadataShiftOld_1 = 20;

	public static uint Metadata2MaskOld_1 = 251658240U;

	public static int Metadata2ShiftOld_1 = 24;
}
