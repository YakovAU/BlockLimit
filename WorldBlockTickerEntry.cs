using System;
using System.IO;

public class WorldBlockTickerEntry
{
	public WorldBlockTickerEntry()
	{
		long num = WorldBlockTickerEntry.nextTickEntryID;
		WorldBlockTickerEntry.nextTickEntryID = num + 1L;
		this.tickEntryID = num;
	}

	public WorldBlockTickerEntry(int _clrIdx, Vector3i _pos, int _id, ulong _scheduledTime)
	{
		this.clrIdx = _clrIdx;
		long num = WorldBlockTickerEntry.nextTickEntryID;
		WorldBlockTickerEntry.nextTickEntryID = num + 1L;
		this.tickEntryID = num;
		this.worldPos = _pos;
		this.blockID = _id;
		this.scheduledTime = _scheduledTime;
	}

	public void Read(BinaryReader _br, int _chunkX, int _chunkZ, int _version)
	{
		this.worldPos = new Vector3i((int)_br.ReadByte() + _chunkX * 16, (int)_br.ReadByte(), (int)_br.ReadByte() + _chunkZ * 16);
		this.blockID = (int)_br.ReadUInt16();
		this.scheduledTime = _br.ReadUInt64();
		this.clrIdx = (int)_br.ReadUInt16();
	}

	public void Write(BinaryWriter _bw)
	{
		_bw.Write((byte)World.toBlockXZ(this.worldPos.x));
		_bw.Write((byte)this.worldPos.y);
		_bw.Write((byte)World.toBlockXZ(this.worldPos.z));
		_bw.Write((ushort)this.blockID);
		_bw.Write(this.scheduledTime);
		_bw.Write((ushort)this.clrIdx);
	}

	public override bool Equals(object _obj)
	{
		if (_obj is WorldBlockTickerEntry)
		{
			WorldBlockTickerEntry worldBlockTickerEntry = (WorldBlockTickerEntry)_obj;
			return worldBlockTickerEntry != null && this.worldPos.Equals(worldBlockTickerEntry.worldPos) && this.blockID == worldBlockTickerEntry.blockID && this.clrIdx == worldBlockTickerEntry.clrIdx && worldBlockTickerEntry.tickEntryID == this.tickEntryID;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return WorldBlockTickerEntry.ToHashCode(this.clrIdx, this.worldPos, this.blockID);
	}

	public static int ToHashCode(int _clrIdx, Vector3i _pos, int _blockID)
	{
		int num = 31 * _clrIdx + _pos.x;
		num = 31 * num + _pos.y;
		return 31 * num + _pos.z;
	}

	public long GetChunkKey()
	{
		return WorldChunkCache.MakeChunkKey(World.toChunkXZ(this.worldPos.x), World.toChunkXZ(this.worldPos.z), this.clrIdx);
	}

	public Vector3i worldPos;

	public int blockID;

	public ulong scheduledTime;

	public int clrIdx;

	private static long nextTickEntryID;

	public long tickEntryID;
}
