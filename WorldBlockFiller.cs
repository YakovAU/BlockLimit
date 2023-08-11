using System;

public class WorldBlockFiller
{
	public WorldBlockFiller(int iBiomeColorId, WorldBiomeProviderFromImage _biomeProvider, GameRandom _rand, WorldBiomes _rules)
	{
		this.m_RandomGenerator = _rand;
		this.m_iChunkDimension = 65536;
		this.m_BlocksToFill = new byte[this.m_iChunkDimension];
		this.m_iThisBiomeColorId = iBiomeColorId;
		this.m_GenRules = _rules;
	}

	public void resetBlockInformation()
	{
		for (int i = 0; i < this.m_iChunkDimension; i++)
		{
			this.m_BlocksToFill[i] = byte.MaxValue;
		}
		this.m_iMaxX = 0;
		this.m_iMinX = 16;
		this.m_iMaxY = 0;
		this.m_iMinY = 256;
		this.m_iMaxZ = 0;
		this.m_iMinZ = 16;
		this.m_iFillCount = 0;
		this.m_iAreaCount = 0;
	}

	public void setBlockToFill(int x, int y, int z, byte top)
	{
		this.m_iMaxX = ((x > this.m_iMaxX) ? x : this.m_iMaxX);
		this.m_iMaxY = ((x > this.m_iMaxY) ? x : this.m_iMaxY);
		this.m_iMaxZ = ((x > this.m_iMaxZ) ? x : this.m_iMaxZ);
		this.m_iMinX = ((x < this.m_iMinX) ? x : this.m_iMinX);
		this.m_iMinY = ((x < this.m_iMinY) ? x : this.m_iMinY);
		this.m_iMinZ = ((x < this.m_iMinZ) ? x : this.m_iMinZ);
		this.setBlockArrayValue(x, y, z, top);
		this.m_iFillCount++;
		if (y == 0)
		{
			this.m_iAreaCount++;
		}
	}

	public void fillChunk(Chunk c)
	{
		if (this.m_iAreaCount == 0)
		{
			return;
		}
		BiomeDefinition biomeDefinition = null;
		this.m_GenRules.GetBiomeMap().TryGetValue((uint)this.m_iThisBiomeColorId, out biomeDefinition);
		if (biomeDefinition != null)
		{
			int num = this.m_iAreaCount;
			int num2 = -1;
			for (int i = 0; i < biomeDefinition.m_DecoBlocks.Count; i++)
			{
				BiomeBlockDecoration biomeBlockDecoration = biomeDefinition.m_DecoBlocks[i];
				this.fillLevel(c, biomeBlockDecoration, num2, ref num);
			}
			num = this.m_iAreaCount;
			for (int j = 0; j < biomeDefinition.m_Layers.Count; j++)
			{
				num2 = biomeDefinition.m_Layers[j].m_Depth;
			}
		}
	}

	private void fillLevel(Chunk c, BiomeBlockDecoration bb, int iLayerDepth, ref int iAvailableCount)
	{
		double num = (double)bb.prob;
		double num2 = (double)bb.clusterProb;
		int num3 = (int)((double)this.m_iAreaCount * num);
		int num4 = this.m_RandomGenerator.RandomRange(this.m_iMinX, this.m_iMaxX + 1);
		int num5 = this.m_RandomGenerator.RandomRange(this.m_iMinZ, this.m_iMaxZ + 1);
		byte b = this.getBlockArrayValue(num4, 0, num5);
		if (b == 255)
		{
			return;
		}
		while (iAvailableCount >= 0 && num3 >= 0)
		{
			bool flag = false;
			if (this.getBlockArrayValue(num4, (int)(b + 1), num5) == 255)
			{
				int num6 = 0;
				while (!flag)
				{
					if (num6 >= 9)
					{
						break;
					}
					int num7 = Math.Max(0, num4 - num6);
					while (!flag && num7 < Math.Min(16, num4 + num6))
					{
						int num8 = Math.Max(0, num5 - num6);
						while (!flag && num8 < Math.Min(16, num5 + num6))
						{
							b = this.getBlockArrayValue(num7, 0, num8);
							if (b != 255)
							{
								flag = true;
								num4 = num7;
								num5 = num8;
							}
							num8++;
						}
						num7++;
					}
					num6++;
				}
			}
			else
			{
				flag = true;
			}
			if (!flag)
			{
				Log.Error("did not find spot to place decoration");
				return;
			}
			int num9 = this.setDecorationBlock(c, num4, (int)b, iLayerDepth, num5, num2, bb.blockValue);
			iAvailableCount -= num9;
			num3 -= num9;
			do
			{
				num4 = this.m_RandomGenerator.RandomRange(this.m_iMinX, this.m_iMaxX + 1);
				num5 = this.m_RandomGenerator.RandomRange(this.m_iMinZ, this.m_iMaxZ + 1);
				b = this.getBlockArrayValue(num4, 0, num5);
			}
			while (b == 255);
		}
	}

	private int setDecorationBlock(Chunk c, int x, int y, int d, int z, double probability, BlockValue blockValue)
	{
		int num = 1;
		int num2 = ((d >= 0) ? this.m_RandomGenerator.RandomRange(0, d) : d);
		if (num2 >= y)
		{
			return 0;
		}
		if (probability > 0.0)
		{
			for (int i = -1; i <= 1; i++)
			{
				for (int j = -1; j <= 1; j++)
				{
					int num3 = ((d > 1) ? (-1) : 0);
					while (num3 <= ((d > 1) ? 1 : 0) && y + num3 - num2 > 0 && y + num3 - num2 < y)
					{
						if (i + x >= 0 && i + x < 16 && j + z >= 0 && j + z < 16 && this.m_RandomGenerator.RandomDouble < probability)
						{
							c.SetBlockRaw(i + x, y + num3 - num2, j + z, blockValue);
							this.setBlockArrayValue(i + x, y + num3 - num2, j + z, byte.MaxValue);
							num++;
						}
						num3++;
					}
				}
			}
		}
		c.SetBlockRaw(x, y - num2, z, blockValue);
		this.setBlockArrayValue(x, y - num2, z, byte.MaxValue);
		return num;
	}

	private byte getBlockArrayValue(int x, int y, int z)
	{
		return this.m_BlocksToFill[((x << 4) + z << 8) + y];
	}

	private void setBlockArrayValue(int x, int y, int z, byte value)
	{
		this.m_BlocksToFill[((x << 4) + z << 8) + y] = value;
	}

	private int m_iMaxX;

	private int m_iMinX;

	private int m_iMaxY;

	private int m_iMinY;

	private int m_iMaxZ;

	private int m_iMinZ;

	private byte[] m_BlocksToFill;

	private GameRandom m_RandomGenerator;

	private int m_iChunkDimension;

	private int m_iFillCount;

	private int m_iAreaCount;

	private int m_iThisBiomeColorId;

	private WorldBiomes m_GenRules;
}
