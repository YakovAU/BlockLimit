using System;
using System.Collections.Generic;
using System.IO;

public class ChunkBlockLayer : IMemoryPoolableObject
{
	private byte[] allocArray8Bit(bool _bClear, byte _val)
	{
		List<byte[]> poolCBLLower8BitArrCache = MemoryPools.poolCBLLower8BitArrCache;
		byte[] array2;
		lock (poolCBLLower8BitArrCache)
		{
			byte[] array;
			if (MemoryPools.poolCBLLower8BitArrCache.Count == 0)
			{
				array = new byte[1024];
			}
			else
			{
				array = MemoryPools.poolCBLLower8BitArrCache[MemoryPools.poolCBLLower8BitArrCache.Count - 1];
				MemoryPools.poolCBLLower8BitArrCache.RemoveAt(MemoryPools.poolCBLLower8BitArrCache.Count - 1);
			}
			if (_bClear)
			{
				Utils.Memset(array, _val, array.Length);
			}
			array2 = array;
		}
		return array2;
	}

	private void freeArray8Bit(byte[] _array)
	{
		List<byte[]> poolCBLLower8BitArrCache = MemoryPools.poolCBLLower8BitArrCache;
		lock (poolCBLLower8BitArrCache)
		{
			if (_array != null && MemoryPools.poolCBLLower8BitArrCache.Count < 10000)
			{
				MemoryPools.poolCBLLower8BitArrCache.Add(_array);
			}
		}
	}

	private byte[] allocArray24Bit(bool _bClear)
	{
		List<byte[]> poolCBLUpper24BitArrCache = MemoryPools.poolCBLUpper24BitArrCache;
		byte[] array2;
		lock (poolCBLUpper24BitArrCache)
		{
			byte[] array;
			if (MemoryPools.poolCBLUpper24BitArrCache.Count == 0)
			{
				array = new byte[3072];
			}
			else
			{
				array = MemoryPools.poolCBLUpper24BitArrCache[MemoryPools.poolCBLUpper24BitArrCache.Count - 1];
				MemoryPools.poolCBLUpper24BitArrCache.RemoveAt(MemoryPools.poolCBLUpper24BitArrCache.Count - 1);
			}
			if (_bClear)
			{
				Utils.Memset(array, 0, array.Length);
			}
			array2 = array;
		}
		return array2;
	}

	private void freeArray24Bit(byte[] _array)
	{
		List<byte[]> poolCBLUpper24BitArrCache = MemoryPools.poolCBLUpper24BitArrCache;
		lock (poolCBLUpper24BitArrCache)
		{
			if (_array != null && MemoryPools.poolCBLUpper24BitArrCache.Count < 10000)
			{
				MemoryPools.poolCBLUpper24BitArrCache.Add(_array);
			}
		}
	}

	public static int GetTempBufSize()
	{
		return 3072;
	}

	public BlockValue GetAt(int _x, int _y, int _z)
	{
		int num = _x + (_z << 4) + (_y & 3) * 16 * 16;
		return this.GetAt(num);
	}

	public BlockValue GetAt(int offs)
	{
		uint num = (uint)this.lower8BitSameValue;
		if (this.m_Lower8Bits != null)
		{
			num = (uint)this.m_Lower8Bits[offs];
		}
		if (this.m_Upper24Bits != null)
		{
			int num2 = offs * 3;
			num |= (uint)(((int)this.m_Upper24Bits[num2] << 8) | ((int)this.m_Upper24Bits[num2 + 1] << 16) | ((int)this.m_Upper24Bits[num2 + 2] << 24));
		}
		BlockValue air = new BlockValue(num);
		if (air.type >= Block.list.Length || air.Block == null)
		{
			air = BlockValue.Air;
		}
		return air;
	}

	public int GetIdAt(int _x, int _y, int _z)
	{
		int num = _x + (_z << 4) + (_y & 3) * 16 * 16;
		return this.GetIdAt(num);
	}

	public int GetIdAt(int offs)
	{
		uint num = (uint)this.lower8BitSameValue;
		if (this.m_Lower8Bits != null)
		{
			num = (uint)this.m_Lower8Bits[offs];
		}
		if (this.m_Upper24Bits != null)
		{
			int num2 = offs * 3;
			num |= (uint)((uint)this.m_Upper24Bits[num2] << 8);
			num &= 32767U;
		}
		return (int)num;
	}

	public void SetAt(int _x, int _y, int _z, uint _fullBlock)
	{
		int num = _x + (_z << 4) + (_y & 3) * 16 * 16;
		uint num2 = (uint)((this.m_Lower8Bits != null) ? this.m_Lower8Bits[num] : this.lower8BitSameValue);
		if (this.m_Upper24Bits != null)
		{
			num2 |= (uint)((uint)this.m_Upper24Bits[num * 3] << 8);
			num2 &= 32767U;
		}
		byte b = (byte)_fullBlock;
		if (this.m_Lower8Bits == null && this.lower8BitSameValue != b)
		{
			this.m_Lower8Bits = this.allocArray8Bit(true, this.lower8BitSameValue);
		}
		if (this.m_Lower8Bits != null)
		{
			this.m_Lower8Bits[num] = b;
		}
		if ((_fullBlock & 4294967040U) != 0U)
		{
			if (this.m_Upper24Bits == null)
			{
				this.m_Upper24Bits = this.allocArray24Bit(true);
			}
			this.m_Upper24Bits[num * 3] = (byte)(_fullBlock >> 8);
			this.m_Upper24Bits[num * 3 + 1] = (byte)(_fullBlock >> 16);
			this.m_Upper24Bits[num * 3 + 2] = (byte)(_fullBlock >> 24);
		}
		else if (this.m_Upper24Bits != null)
		{
			this.m_Upper24Bits[num * 3] = 0;
			this.m_Upper24Bits[num * 3 + 1] = 0;
			this.m_Upper24Bits[num * 3 + 2] = 0;
		}
		if (!Block.BlocksLoaded)
		{
			return;
		}
		uint num3 = _fullBlock & 32767U;
		Block block = Block.list[(int)num2];
		Block block2 = Block.list[(int)num3];
		if (num2 == 0U && num3 != 0U)
		{
			this.blockRefCount++;
			if (block2 != null && block2.IsRandomlyTick)
			{
				this.tickRefCount++;
			}
		}
		else if (num2 != 0U && num3 == 0U)
		{
			this.blockRefCount--;
			if (block != null && block.IsRandomlyTick)
			{
				this.tickRefCount--;
			}
		}
		else if (block != null && block.IsRandomlyTick && block2 != null && !block2.IsRandomlyTick)
		{
			this.tickRefCount--;
		}
		else if (block != null && !block.IsRandomlyTick && block2 != null && block2.IsRandomlyTick)
		{
			this.tickRefCount++;
		}
		if (block2 != null && block2.IsNotifyOnLoadUnload)
		{
			object obj = this.lockObj;
			lock (obj)
			{
				if (this.notifyLoadUnloadCallbackBlocks == null)
				{
					this.notifyLoadUnloadCallbackBlocks = new HashSet<int>();
				}
				if (!this.notifyLoadUnloadCallbackBlocks.Contains(num))
				{
					this.notifyLoadUnloadCallbackBlocks.Add(num);
				}
				goto IL_283;
			}
		}
		if (block != null && block.IsNotifyOnLoadUnload && this.notifyLoadUnloadCallbackBlocks != null)
		{
			object obj = this.lockObj;
			lock (obj)
			{
				this.notifyLoadUnloadCallbackBlocks.Remove(num);
			}
		}
		IL_283:
		if (this.bOnlyTerrain && block2 != null && !block2.shape.IsTerrain())
		{
			this.bOnlyTerrain = false;
		}
	}

	public void Fill(uint _fullBlock)
	{
		uint num = _fullBlock & 32767U;
		Block block = Block.list[(int)num];
		this.freeArray8Bit(this.m_Lower8Bits);
		this.m_Lower8Bits = null;
		this.lower8BitSameValue = (byte)_fullBlock;
		if ((_fullBlock & 4294967040U) != 0U)
		{
			if (this.m_Upper24Bits == null)
			{
				this.m_Upper24Bits = this.allocArray24Bit(true);
			}
			byte b = (byte)(_fullBlock >> 8);
			byte b2 = (byte)(_fullBlock >> 16);
			byte b3 = (byte)(_fullBlock >> 24);
			for (int i = 0; i < this.m_Upper24Bits.Length; i += 3)
			{
				this.m_Upper24Bits[i] = b;
				this.m_Upper24Bits[i + 1] = b2;
				this.m_Upper24Bits[i + 2] = b3;
			}
		}
		else
		{
			this.freeArray24Bit(this.m_Upper24Bits);
			this.m_Upper24Bits = null;
		}
		this.bOnlyTerrain = block.shape.IsTerrain();
		object obj = this.lockObj;
		lock (obj)
		{
			if (this.notifyLoadUnloadCallbackBlocks != null)
			{
				this.notifyLoadUnloadCallbackBlocks.Clear();
			}
			else if (block.IsNotifyOnLoadUnload)
			{
				this.notifyLoadUnloadCallbackBlocks = new HashSet<int>();
			}
			if (block.IsNotifyOnLoadUnload)
			{
				for (int j = 0; j < 1024; j++)
				{
					this.notifyLoadUnloadCallbackBlocks.Add(j);
				}
			}
		}
	}

	public void Reset()
	{
		this.freeArray8Bit(this.m_Lower8Bits);
		this.m_Lower8Bits = null;
		this.lower8BitSameValue = 0;
		this.freeArray24Bit(this.m_Upper24Bits);
		this.m_Upper24Bits = null;
		this.blockRefCount = 0;
		this.tickRefCount = 0;
		object obj = this.lockObj;
		lock (obj)
		{
			if (this.notifyLoadUnloadCallbackBlocks != null)
			{
				this.notifyLoadUnloadCallbackBlocks.Clear();
			}
		}
	}

	public void Cleanup()
	{
	}

	private void updateRefCounts()
	{
		this.blockRefCount = 0;
		this.tickRefCount = 0;
		for (int i = 1023; i >= 0; i--)
		{
			int idAt = this.GetIdAt(i);
			if (idAt > 0 && idAt < Block.list.Length)
			{
				Block block = Block.list[idAt];
				if (block != null)
				{
					this.blockRefCount++;
					if (block.IsRandomlyTick)
					{
						this.tickRefCount++;
					}
					if (block.IsNotifyOnLoadUnload)
					{
						object obj = this.lockObj;
						lock (obj)
						{
							if (this.notifyLoadUnloadCallbackBlocks == null)
							{
								this.notifyLoadUnloadCallbackBlocks = new HashSet<int>();
							}
							this.notifyLoadUnloadCallbackBlocks.Add(i);
						}
					}
				}
			}
		}
	}

	public void OnLoad(WorldBase _world, int _clrIdx, int _x, int _y, int _z)
	{
		if (this.notifyLoadUnloadCallbackBlocks != null)
		{
			object obj = this.lockObj;
			lock (obj)
			{
				foreach (int num in this.notifyLoadUnloadCallbackBlocks)
				{
					BlockValue at = this.GetAt(num);
					int num2 = num / 256 + _y;
					int num3 = num % 256;
					int num4 = num3 % 16 + _x;
					int num5 = num3 / 16 + _z;
					at.Block.OnBlockLoaded(_world, _clrIdx, new Vector3i(num4, num2, num5), at);
				}
			}
		}
	}

	public void OnUnload(WorldBase _world, int _clrIdx, int _x, int _y, int _z)
	{
		if (this.notifyLoadUnloadCallbackBlocks != null)
		{
			object obj = this.lockObj;
			lock (obj)
			{
				foreach (int num in this.notifyLoadUnloadCallbackBlocks)
				{
					BlockValue at = this.GetAt(num);
					int num2 = num / 256 + _y;
					int num3 = num % 256;
					int num4 = num3 % 16 + _x;
					int num5 = num3 / 16 + _z;
					at.Block.OnBlockUnloaded(_world, _clrIdx, new Vector3i(num4, num2, num5), at);
				}
			}
		}
	}

	public int GetTickRefCount()
	{
		return this.tickRefCount;
	}

	public bool IsOnlyTerrain()
	{
		return this.bOnlyTerrain;
	}

	public void AddIndexedBlocks(int _curLayerIdx, DictionarySave<string, List<Vector3i>> _indexedBlocksDict)
	{
		for (int i = 0; i < 1024; i++)
		{
			int idAt = this.GetIdAt(i);
			Block block = Block.list[idAt];
			if (block != null && block.IndexName != null)
			{
				BlockValue at = this.GetAt(i);
				if (block.FilterIndexType(at))
				{
					if (!_indexedBlocksDict.ContainsKey(block.IndexName))
					{
						_indexedBlocksDict[block.IndexName] = new List<Vector3i>();
					}
					int num = (_curLayerIdx << 2) + i / 256;
					int num2 = i % 256 % 16;
					int num3 = i % 256 / 16;
					_indexedBlocksDict[block.IndexName].Add(new Vector3i(num2, num, num3));
				}
			}
		}
	}

	public void Read(BinaryReader stream, uint _version, bool _bNetworkRead)
	{
		if (_version < 30U)
		{
			throw new Exception("Chunk version " + _version.ToString() + " not supported any more!");
		}
		if (stream.ReadBoolean())
		{
			if (this.m_Lower8Bits == null)
			{
				this.m_Lower8Bits = this.allocArray8Bit(false, 0);
			}
			stream.Read(this.m_Lower8Bits, 0, 1024);
		}
		else
		{
			if (this.m_Lower8Bits != null)
			{
				this.freeArray8Bit(this.m_Lower8Bits);
				this.m_Lower8Bits = null;
			}
			this.lower8BitSameValue = stream.ReadByte();
		}
		if (stream.ReadBoolean())
		{
			if (this.m_Upper24Bits == null)
			{
				this.m_Upper24Bits = this.allocArray24Bit(false);
			}
			stream.Read(this.m_Upper24Bits, 0, 3072);
		}
		else if (this.m_Upper24Bits != null)
		{
			this.freeArray24Bit(this.m_Upper24Bits);
			this.m_Upper24Bits = null;
		}
		this.updateRefCounts();
		this.CheckOnlyTerrain();
	}

	public void Write(BinaryWriter _bw, bool _bNetworkWrite)
	{
		_bw.Write(this.m_Lower8Bits != null);
		if (this.m_Lower8Bits != null)
		{
			_bw.Write(this.m_Lower8Bits, 0, 1024);
		}
		else
		{
			_bw.Write(this.lower8BitSameValue);
		}
		_bw.Write(this.m_Upper24Bits != null);
		if (this.m_Upper24Bits != null)
		{
			_bw.Write(this.m_Upper24Bits, 0, 3072);
		}
	}

	public void CopyFrom(ChunkBlockLayer _other)
	{
		if (_other.m_Lower8Bits != null)
		{
			if (this.m_Lower8Bits == null)
			{
				this.m_Lower8Bits = this.allocArray8Bit(true, 0);
			}
			Array.Copy(_other.m_Lower8Bits, this.m_Lower8Bits, this.m_Lower8Bits.Length);
		}
		else if (this.m_Lower8Bits != null)
		{
			this.freeArray8Bit(this.m_Lower8Bits);
		}
		if (_other.m_Upper24Bits != null)
		{
			if (this.m_Upper24Bits == null)
			{
				this.m_Upper24Bits = this.allocArray24Bit(true);
			}
			Array.Copy(_other.m_Upper24Bits, this.m_Upper24Bits, this.m_Upper24Bits.Length);
		}
		else if (this.m_Upper24Bits != null)
		{
			this.freeArray24Bit(this.m_Upper24Bits);
		}
		this.bOnlyTerrain = _other.bOnlyTerrain;
		this.blockRefCount = _other.blockRefCount;
		this.tickRefCount = _other.tickRefCount;
	}

	public void CheckOnlyTerrain()
	{
		if (this.m_Upper24Bits != null)
		{
			bool flag = this.m_Upper24Bits[0] == 0;
			int num = 1;
			while (flag && num < this.m_Upper24Bits.Length)
			{
				flag &= this.m_Upper24Bits[num] == 0;
				num++;
			}
			if (flag)
			{
				this.freeArray24Bit(this.m_Upper24Bits);
				this.m_Upper24Bits = null;
			}
		}
		this.bOnlyTerrain = this.m_Upper24Bits == null;
		if (this.m_Lower8Bits == null)
		{
			this.bOnlyTerrain &= this.lower8BitSameValue > 0 && this.lower8BitSameValue <= 128;
			return;
		}
		if (this.bOnlyTerrain)
		{
			for (int i = 0; i < this.m_Lower8Bits.Length; i++)
			{
				uint num2 = (uint)this.m_Lower8Bits[i];
				if (num2 > 128U || num2 == 0U)
				{
					this.bOnlyTerrain = false;
					break;
				}
			}
		}
		bool flag2 = true;
		this.lower8BitSameValue = this.m_Lower8Bits[0];
		for (int j = 1; j < this.m_Lower8Bits.Length; j++)
		{
			if (this.lower8BitSameValue != this.m_Lower8Bits[j])
			{
				flag2 = false;
				this.lower8BitSameValue = 0;
				break;
			}
		}
		if (flag2)
		{
			this.freeArray8Bit(this.m_Lower8Bits);
			this.m_Lower8Bits = null;
			this.bOnlyTerrain &= this.lower8BitSameValue > 0;
		}
	}

	public void LoopOverAllBlocks(Chunk _c, int _yPos, ChunkBlockLayer.LoopBlocksDelegate _delegate, bool _bIncludeChilds = false, bool _bIncludeAirBlocks = false)
	{
		for (int i = 0; i < 1024; i++)
		{
			BlockValue at = this.GetAt(i);
			if ((_bIncludeAirBlocks || !at.isair) && (_bIncludeChilds || !at.ischild))
			{
				int num = i / 256 + _yPos;
				int num2 = i % 256;
				int num3 = num2 % 16;
				int num4 = num2 / 16;
				at.damage = _c.GetDamage(num3, num, num4);
				_delegate(num3, num, num4, at);
			}
		}
	}

	public int GetUsedMem()
	{
		return ((this.m_Lower8Bits != null) ? this.m_Lower8Bits.Length : 1) + ((this.m_Upper24Bits != null) ? this.m_Upper24Bits.Length : 0) + 20 + 2;
	}

	public void SaveBlockMappings(NameIdMapping _mappings)
	{
		if (this.m_Lower8Bits == null && this.m_Upper24Bits == null)
		{
			Block block = this.GetAt(0).Block;
			_mappings.AddMapping(block.blockID, block.GetBlockName(), false);
			return;
		}
		Array.Clear(ChunkBlockLayer.saved, 0, Block.MAX_BLOCKS);
		bool flag = this.m_Lower8Bits != null;
		bool flag2 = this.m_Upper24Bits != null;
		for (int i = 0; i < 1024; i++)
		{
			int num = (int)(flag ? this.m_Lower8Bits[i] : this.lower8BitSameValue);
			num |= (flag2 ? (((int)this.m_Upper24Bits[i * 3] << 8) & 65280) : 0);
			num &= 32767;
			if (!ChunkBlockLayer.saved[num])
			{
				Block block2 = Block.list[num];
				if (block2 != null)
				{
					_mappings.AddMapping(num, block2.GetBlockName(), false);
				}
				ChunkBlockLayer.saved[num] = true;
			}
		}
	}

	private const int cWPow = 4;

	private const int cLayerHeight = 4;

	public const int cArrSize = 1024;

	public static int InstanceCount;

	private byte lower8BitSameValue;

	private byte[] m_Lower8Bits;

	private byte[] m_Upper24Bits;

	private bool bOnlyTerrain;

	private int blockRefCount;

	private int tickRefCount;

	private object lockObj = new object();

	private HashSet<int> notifyLoadUnloadCallbackBlocks;

	private static bool[] saved = new bool[Block.MAX_BLOCKS];

	public delegate void LoopBlocksDelegate(int x, int y, int z, BlockValue bv);
}
