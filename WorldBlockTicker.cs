using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class WorldBlockTicker
{
	public WorldBlockTicker(World _world)
	{
		this.world = _world;
	}

	public void Cleanup()
	{
		object obj = this.lockObject;
		lock (obj)
		{
			this.scheduledTicksSorted.Clear();
			this.scheduledTicksDict.Clear();
			this.chunkToScheduledTicks.Clear();
		}
	}

	public void Tick(ArraySegment<long> _activeChunks, EntityPlayer _thePlayer, GameRandom _rnd)
	{
		if (!GameManager.bTickingActive)
		{
			return;
		}
		if (!this.world.IsRemote())
		{
			this.tickScheduled(_rnd);
		}
		if (!this.world.IsRemote())
		{
			this.tickRandom(_activeChunks, _rnd);
		}
	}

	public void AddScheduledBlockUpdate(int _clrIdx, Vector3i _pos, int _blockId, ulong _ticks)
	{
		if (_blockId == 0)
		{
			return;
		}
		WorldBlockTickerEntry worldBlockTickerEntry = new WorldBlockTickerEntry(_clrIdx, _pos, _blockId, _ticks + GameTimer.Instance.ticks);
		object obj = this.lockObject;
		lock (obj)
		{
			if (!this.scheduledTicksDict.ContainsKey(worldBlockTickerEntry.GetHashCode()))
			{
				this.add(worldBlockTickerEntry);
			}
		}
	}

	private void add(WorldBlockTickerEntry _wbte)
	{
		this.scheduledTicksDict.Add(_wbte.GetHashCode(), _wbte);
		this.scheduledTicksSorted.Add(_wbte, null);
		HashSet<WorldBlockTickerEntry> hashSet = this.chunkToScheduledTicks[_wbte.GetChunkKey()];
		if (hashSet == null)
		{
			hashSet = new HashSet<WorldBlockTickerEntry>();
			this.chunkToScheduledTicks[_wbte.GetChunkKey()] = hashSet;
		}
		hashSet.Add(_wbte);
	}

	private void execute(WorldBlockTickerEntry _wbte, GameRandom _rnd, ulong _ticksIfLoaded)
	{
		BlockValue block = this.world.GetBlock(_wbte.clrIdx, _wbte.worldPos);
		if (block.type == _wbte.blockID)
		{
			block.Block.UpdateTick(this.world, _wbte.clrIdx, _wbte.worldPos, block, false, _ticksIfLoaded, _rnd);
		}
	}

	private bool tickScheduled(GameRandom _rnd)
	{
		object obj = this.lockObject;
		int num;
		lock (obj)
		{
			num = this.scheduledTicksSorted.Count;
			if (num != this.scheduledTicksDict.Count)
			{
				throw new Exception("WBT: Invalid dict state");
			}
		}
		if (num > 100)
		{
			num = 100;
		}
		for (int i = 0; i < num; i++)
		{
			obj = this.lockObject;
			WorldBlockTickerEntry worldBlockTickerEntry;
			lock (obj)
			{
				worldBlockTickerEntry = (WorldBlockTickerEntry)this.scheduledTicksSorted.GetKey(0);
				if (worldBlockTickerEntry.scheduledTime > GameTimer.Instance.ticks)
				{
					break;
				}
				this.scheduledTicksSorted.Remove(worldBlockTickerEntry);
				this.scheduledTicksDict.Remove(worldBlockTickerEntry.GetHashCode());
				HashSet<WorldBlockTickerEntry> hashSet = this.chunkToScheduledTicks[worldBlockTickerEntry.GetChunkKey()];
				if (hashSet != null)
				{
					hashSet.Remove(worldBlockTickerEntry);
				}
			}
			if (!this.world.IsChunkAreaLoaded(worldBlockTickerEntry.worldPos.x, worldBlockTickerEntry.worldPos.y, worldBlockTickerEntry.worldPos.z))
			{
				int num2 = World.toChunkXZ(worldBlockTickerEntry.worldPos.x);
				int num3 = World.toChunkXZ(worldBlockTickerEntry.worldPos.z);
				if (this.world.GetChunkSync(num2, num3) != null)
				{
					this.AddScheduledBlockUpdate(worldBlockTickerEntry.clrIdx, worldBlockTickerEntry.worldPos, worldBlockTickerEntry.blockID, (ulong)(30 + _rnd.RandomRange(0, 15)));
				}
			}
			else
			{
				this.execute(worldBlockTickerEntry, _rnd, 0UL);
			}
		}
		return this.scheduledTicksSorted.Count != 0;
	}

	private void tickRandom(ArraySegment<long> _activeChunkSet, GameRandom _rnd)
	{
		if (this.randomTickIndex >= this.randomTickChunkKeys.Count)
		{
			this.randomTickChunkKeys.Clear();
			if (_activeChunkSet.Count > this.randomTickChunkKeys.Capacity)
			{
				this.randomTickChunkKeys.Capacity = _activeChunkSet.Count * 2;
			}
			int num = _activeChunkSet.Offset + _activeChunkSet.Count;
			for (int i = _activeChunkSet.Offset; i < num; i++)
			{
				this.randomTickChunkKeys.Add(_activeChunkSet.Array[i]);
			}
			this.randomTickCountPerFrame = Math.Max(_activeChunkSet.Count / 100, 1);
			this.randomTickIndex = 0;
		}
		int num2 = 0;
		while (this.randomTickIndex < this.randomTickChunkKeys.Count && num2 < this.randomTickCountPerFrame)
		{
			long num3 = this.randomTickChunkKeys[this.randomTickIndex];
			Chunk chunkSync = this.world.ChunkCache.GetChunkSync(num3);
			this.tickChunkRandom(chunkSync, _rnd);
			this.randomTickIndex++;
			num2++;
		}
	}

	private void tickChunkRandom(Chunk chunk, GameRandom _rnd)
	{
		if (chunk == null)
		{
			return;
		}
		if (chunk.NeedsLightCalculation)
		{
			return;
		}
		if (GameTimer.Instance.ticks - chunk.LastTimeRandomTicked < 1200UL)
		{
			return;
		}
		ulong num = GameTimer.Instance.ticks - chunk.LastTimeRandomTicked;
		chunk.LastTimeRandomTicked = GameTimer.Instance.ticks;
		DictionaryKeyList<Vector3i, int> tickedBlocks = chunk.GetTickedBlocks();
		DictionaryKeyList<Vector3i, int> dictionaryKeyList = tickedBlocks;
		lock (dictionaryKeyList)
		{
			for (int i = tickedBlocks.list.Count - 1; i >= 0; i--)
			{
				Vector3i vector3i = tickedBlocks.list[i];
				BlockValue block = chunk.GetBlock(World.toBlockXZ(vector3i.x), vector3i.y, World.toBlockXZ(vector3i.z));
				if (this.scheduledTicksDict.Count == 0 || !this.scheduledTicksDict.ContainsKey(WorldBlockTickerEntry.ToHashCode(chunk.ClrIdx, vector3i, block.type)))
				{
					block.Block.UpdateTick(this.world, chunk.ClrIdx, vector3i, block, true, num, _rnd);
				}
			}
		}
	}

	public void OnChunkAdded(WorldBase _world, Chunk _c, GameRandom _rnd)
	{
		ChunkCustomData chunkCustomData;
		if (!_c.ChunkCustomData.dict.TryGetValue("wbt.sch", out chunkCustomData) || chunkCustomData == null)
		{
			return;
		}
		_c.ChunkCustomData.Remove("wbt.sch");
		using (PooledBinaryReader pooledBinaryReader = MemoryPools.poolBinaryReader.AllocSync(false))
		{
			pooledBinaryReader.SetBaseStream(new MemoryStream(chunkCustomData.data));
			int num = (int)pooledBinaryReader.ReadUInt16();
			int num2 = (int)pooledBinaryReader.ReadByte();
			for (int i = 0; i < num; i++)
			{
				WorldBlockTickerEntry worldBlockTickerEntry = new WorldBlockTickerEntry();
				worldBlockTickerEntry.Read(pooledBinaryReader, _c.X, _c.Z, num2);
				object obj = this.lockObject;
				lock (obj)
				{
					if (!this.scheduledTicksDict.ContainsKey(worldBlockTickerEntry.GetHashCode()))
					{
						if (worldBlockTickerEntry.scheduledTime > GameTimer.Instance.ticks)
						{
							this.add(worldBlockTickerEntry);
						}
						else
						{
							this.execute(worldBlockTickerEntry, _rnd, GameTimer.Instance.ticks - worldBlockTickerEntry.scheduledTime);
						}
					}
				}
			}
		}
	}

	public void OnChunkRemoved(Chunk _c)
	{
		this.addScheduleInformationToChunk(_c, true);
	}

	public void OnChunkBeforeSave(Chunk _c)
	{
		this.addScheduleInformationToChunk(_c, false);
	}

	private void addScheduleInformationToChunk(Chunk _c, bool _bChunkIsRemoved)
	{
		HashSet<WorldBlockTickerEntry> hashSet = this.chunkToScheduledTicks[_c.Key];
		object obj = this.lockObject;
		lock (obj)
		{
			if (hashSet == null)
			{
				return;
			}
			if (_bChunkIsRemoved)
			{
				this.chunkToScheduledTicks.Remove(_c.Key);
			}
		}
		if (hashSet.Count == 0)
		{
			return;
		}
		ChunkCustomData chunkCustomData = new ChunkCustomData("wbt.sch", ulong.MaxValue, false);
		using (PooledExpandableMemoryStream pooledExpandableMemoryStream = MemoryPools.poolMemoryStream.AllocSync(true))
		{
			using (PooledBinaryWriter pooledBinaryWriter = MemoryPools.poolBinaryWriter.AllocSync(false))
			{
				pooledBinaryWriter.SetBaseStream(pooledExpandableMemoryStream);
				pooledBinaryWriter.Write((ushort)hashSet.Count);
				pooledBinaryWriter.Write(1);
				foreach (WorldBlockTickerEntry worldBlockTickerEntry in hashSet)
				{
					worldBlockTickerEntry.Write(pooledBinaryWriter);
					if (_bChunkIsRemoved)
					{
						obj = this.lockObject;
						lock (obj)
						{
							this.scheduledTicksSorted.Remove(worldBlockTickerEntry);
							this.scheduledTicksDict.Remove(worldBlockTickerEntry.GetHashCode());
						}
					}
				}
			}
			chunkCustomData.data = pooledExpandableMemoryStream.ToArray();
		}
		_c.ChunkCustomData.Set("wbt.sch", chunkCustomData);
		_c.isModified = true;
	}

	public int GetCount()
	{
		object obj = this.lockObject;
		int count;
		lock (obj)
		{
			count = this.scheduledTicksDict.Count;
		}
		return count;
	}

	private readonly object lockObject = new object();

	private readonly SortedList scheduledTicksSorted = new SortedList(new WorldBlockTicker.EntryComparer());

	private readonly Dictionary<int, WorldBlockTickerEntry> scheduledTicksDict = new Dictionary<int, WorldBlockTickerEntry>();

	private readonly DictionarySave<long, HashSet<WorldBlockTickerEntry>> chunkToScheduledTicks = new DictionarySave<long, HashSet<WorldBlockTickerEntry>>();

	private readonly World world;

	private int randomTickIndex;

	private int randomTickCountPerFrame;

	private readonly List<long> randomTickChunkKeys = new List<long>();

	private class EntryComparer : IComparer
	{
		public int Compare(object _o1, object _o2)
		{
			WorldBlockTickerEntry worldBlockTickerEntry = (WorldBlockTickerEntry)_o1;
			WorldBlockTickerEntry worldBlockTickerEntry2 = (WorldBlockTickerEntry)_o2;
			if (worldBlockTickerEntry.scheduledTime < worldBlockTickerEntry2.scheduledTime)
			{
				return -1;
			}
			if (worldBlockTickerEntry.scheduledTime > worldBlockTickerEntry2.scheduledTime)
			{
				return 1;
			}
			if (worldBlockTickerEntry.tickEntryID < worldBlockTickerEntry2.tickEntryID)
			{
				return -1;
			}
			if (worldBlockTickerEntry.tickEntryID > worldBlockTickerEntry2.tickEntryID)
			{
				return 1;
			}
			return 0;
		}
	}
}
