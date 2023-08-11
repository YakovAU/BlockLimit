using System;
using System.Collections.Generic;
using UnityEngine.Scripting;

[Preserve]
public class NetPackageSetBlock : NetPackage
{
	public NetPackageSetBlock Setup(PersistentPlayerData _persistentPlayerData, List<BlockChangeInfo> _blockChanges, int _localPlayerThatChanged)
	{
		this.persistentPlayerId = ((_persistentPlayerData != null) ? _persistentPlayerData.UserIdentifier : null);
		this.blockChanges = _blockChanges;
		this.localPlayerThatChanged = _localPlayerThatChanged;
		return this;
	}

	public override void read(PooledBinaryReader _br)
	{
		this.persistentPlayerId = PlatformUserIdentifierAbs.FromStream(_br, false, false);
		int num = (int)_br.ReadInt16();
		this.blockChanges = new List<BlockChangeInfo>();
		for (int i = 0; i < num; i++)
		{
			BlockChangeInfo blockChangeInfo = new BlockChangeInfo();
			blockChangeInfo.Read(_br);
			this.blockChanges.Add(blockChangeInfo);
		}
		this.localPlayerThatChanged = _br.ReadInt32();
	}

	public override void write(PooledBinaryWriter _bw)
	{
		base.write(_bw);
		this.persistentPlayerId.ToStream(_bw, false);
		int count = this.blockChanges.Count;
		_bw.Write((short)count);
		for (int i = 0; i < count; i++)
		{
			this.blockChanges[i].Write(_bw);
		}
		_bw.Write(this.localPlayerThatChanged);
	}

	public override void ProcessPackage(World _world, GameManager _callbacks)
	{
		if (SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
		{
			_callbacks.SetBlocksOnClients(this.localPlayerThatChanged, this);
		}
		if (_world == null || _world.ChunkClusters[0] == null)
		{
			return;
		}
		if (DynamicMeshManager.CONTENT_ENABLED)
		{
			foreach (BlockChangeInfo blockChangeInfo in this.blockChanges)
			{
				DynamicMeshManager.ChunkChanged(blockChangeInfo.pos, -1, blockChangeInfo.blockValue.type);
			}
		}
		_callbacks.ChangeBlocks(this.persistentPlayerId, this.blockChanges);
	}

	public override int GetLength()
	{
		return this.blockChanges.Count * 16;
	}

	private List<BlockChangeInfo> blockChanges;

	private PlatformUserIdentifierAbs persistentPlayerId;

	private int localPlayerThatChanged;
}
