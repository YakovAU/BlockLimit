using System;
using System.IO;

public class ChunkBlockChannel
{
	public ChunkBlockChannel(long _defaultValue, int _bytesPerVal = 1)
	{
		this.defaultValue = _defaultValue;
		this.bytesPerVal = _bytesPerVal;
		this.sameValue = new byte[64 * this.bytesPerVal];
		this.fillSameValue(-1L);
		this.layers = new CBCLayer[64 * this.bytesPerVal];
	}

	private CBCLayer allocLayer()
	{
		CBCLayer cbclayer = MemoryPools.poolCBC.AllocSync(true);
		cbclayer.InitData(1024);
		return cbclayer;
	}

	private void freeLayer(int _idx)
	{
		if (this.layers[_idx] == null)
		{
			return;
		}
		MemoryPools.poolCBC.FreeSync(this.layers[_idx]);
		this.layers[_idx] = null;
	}

	private int calcOffset(int _x, int _y, int _z)
	{
		return _x + _z * 16 + (_y & 3) * 16 * 16;
	}

	private void fillSameValue(long _value = -1L)
	{
		long num = ((_value == -1L) ? this.defaultValue : _value);
		for (int i = 0; i < this.bytesPerVal; i++)
		{
			byte b = (byte)(num >> i * 8);
			for (int j = 63; j >= 0; j--)
			{
				this.sameValue[j * this.bytesPerVal + i] = b;
			}
		}
	}

	private long getSameValue(int _idx)
	{
		long num = 0L;
		for (int i = 0; i < this.bytesPerVal; i++)
		{
			num |= (long)((long)((ulong)this.sameValue[_idx + i]) << i * 8);
		}
		return num;
	}

	private void setSameValue(int _idx, long _value)
	{
		for (int i = 0; i < this.bytesPerVal; i++)
		{
			this.sameValue[_idx + i] = (byte)_value;
			_value >>= 8;
		}
	}

	private long getData(int _idx, int _offs)
	{
		long num = 0L;
		for (int i = 0; i < this.bytesPerVal; i++)
		{
			CBCLayer cbclayer = this.layers[_idx + i];
			if (cbclayer == null)
			{
				break;
			}
			num |= (long)((long)((ulong)cbclayer.data[_offs]) << i * 8);
		}
		return num;
	}

	private long getSetData(int _idx, int _offs, long _value)
	{
		long num = 0L;
		for (int i = 0; i < this.bytesPerVal; i++)
		{
			CBCLayer cbclayer = this.layers[_idx + i];
			if (cbclayer == null)
			{
				break;
			}
			num |= (long)((long)((ulong)cbclayer.data[_offs]) << i * 8);
			cbclayer.data[_offs] = (byte)_value;
			_value >>= 8;
		}
		return num;
	}

	public long GetSet(int _x, int _y, int _z, long _value)
	{
		int num = (_y >> 2) * this.bytesPerVal;
		if (this.layers[num] == null)
		{
			long num2 = this.getSameValue(num);
			if (num2 == _value)
			{
				return _value;
			}
			for (int i = 0; i < this.bytesPerVal; i++)
			{
				CBCLayer cbclayer = this.allocLayer();
				this.layers[num + i] = cbclayer;
				byte b = (byte)(num2 >> i * 8);
				for (int j = 1023; j >= 0; j--)
				{
					cbclayer.data[j] = b;
				}
			}
		}
		int num3 = this.calcOffset(_x, _y, _z);
		return this.getSetData(num, num3, _value);
	}

	public void Set(int _x, int _y, int _z, long _value)
	{
		int num = (_y >> 2) * this.bytesPerVal;
		if (this.layers[num] == null)
		{
			long num2 = this.getSameValue(num);
			if (num2 == _value)
			{
				return;
			}
			for (int i = 0; i < this.bytesPerVal; i++)
			{
				CBCLayer cbclayer = this.allocLayer();
				this.layers[num + i] = cbclayer;
				byte b = (byte)(num2 >> i * 8);
				for (int j = 1023; j >= 0; j--)
				{
					cbclayer.data[j] = b;
				}
			}
		}
		int num3 = this.calcOffset(_x, _y, _z);
		for (int k = 0; k < this.bytesPerVal; k++)
		{
			CBCLayer cbclayer = this.layers[num + k];
			if (cbclayer == null)
			{
				break;
			}
			cbclayer.data[num3] = (byte)_value;
			_value >>= 8;
		}
	}

	public long Get(int _x, int _y, int _z)
	{
		int num = (_y >> 2) * this.bytesPerVal;
		if (num < 0)
		{
			return 0L;
		}
		CBCLayer cbclayer = this.layers[num];
		if (cbclayer == null)
		{
			return this.getSameValue(num);
		}
		int num2 = this.calcOffset(_x, _y, _z);
		if (this.bytesPerVal == 1)
		{
			return (long)((ulong)cbclayer.data[num2]);
		}
		return this.getData(num, num2);
	}

	public void Read(BinaryReader _br, uint _version, bool _bNetworkRead)
	{
		if (_version > 34U)
		{
			for (int i = 0; i < 64; i++)
			{
				int num = i * this.bytesPerVal;
				bool flag = _br.ReadByte() == 1;
				for (int j = 0; j < this.bytesPerVal; j++)
				{
					int num2 = num + j;
					if (!flag)
					{
						if (this.layers[num2] == null)
						{
							this.layers[num2] = this.allocLayer();
						}
						_br.Read(this.layers[num2].data, 0, 1024);
					}
					else
					{
						this.sameValue[num2] = _br.ReadByte();
						this.freeLayer(num2);
					}
				}
				this.onLayerRead(num);
			}
			return;
		}
		for (int k = 0; k < 64; k++)
		{
			int num3 = k * this.bytesPerVal;
			bool flag2 = _br.ReadBoolean();
			for (int l = 0; l < this.bytesPerVal; l++)
			{
				if (!flag2)
				{
					if (this.layers[num3 + l] == null)
					{
						this.layers[num3 + l] = this.allocLayer();
					}
					_br.Read(this.layers[num3 + l].data, 0, 1024);
				}
				else
				{
					this.sameValue[num3 + l] = _br.ReadByte();
					this.freeLayer(num3 + l);
				}
			}
			this.onLayerRead(num3);
		}
	}

	public void Write(BinaryWriter _bw, bool _bNetworkWrite, byte[] temp)
	{
		int num = 0;
		for (int i = 0; i < 64; i++)
		{
			int num2 = i * this.bytesPerVal;
			bool flag = this.layers[num2] == null;
			temp[num++] = (flag ? 1 : 0);
			if (num == temp.Length)
			{
				_bw.Write(temp, 0, num);
				num = 0;
			}
			for (int j = 0; j < this.bytesPerVal; j++)
			{
				if (!flag)
				{
					if (num > 0)
					{
						_bw.Write(temp, 0, num);
						num = 0;
					}
					_bw.Write(this.layers[num2 + j].data, 0, 1024);
				}
				else
				{
					temp[num++] = this.sameValue[num2 + j];
					if (num == temp.Length)
					{
						_bw.Write(temp, 0, num);
						num = 0;
					}
				}
			}
		}
		if (num > 0)
		{
			_bw.Write(temp, 0, num);
		}
	}

	private void onLayerRead(int _idx)
	{
		if (this.layers[_idx] == null)
		{
			return;
		}
		this.checkSameValue(_idx);
	}

	public void CheckSameValue()
	{
		for (int i = 63; i >= 0; i--)
		{
			this.checkSameValue(i * this.bytesPerVal);
		}
	}

	private void checkSameValue(int _idx)
	{
		if (this.layers[_idx] == null)
		{
			return;
		}
		long data = this.getData(_idx, 0);
		for (int i = 1; i < 1024; i++)
		{
			if (data != this.getData(_idx, i))
			{
				return;
			}
		}
		this.setSameValue(_idx, data);
		for (int j = 0; j < this.bytesPerVal; j++)
		{
			this.freeLayer(_idx + j);
		}
	}

	public bool HasSameValue(int _y)
	{
		int num = (_y >> 2) * this.bytesPerVal;
		return this.layers[num] == null;
	}

	public long GetSameValue(int _y)
	{
		int num = (_y >> 2) * this.bytesPerVal;
		return this.getSameValue(num);
	}

	public bool IsDefault()
	{
		for (int i = 63; i >= 0; i--)
		{
			if (!this.IsDefaultLayer(i))
			{
				return false;
			}
		}
		return true;
	}

	public bool IsDefault(int _y)
	{
		int num = _y >> 2;
		return this.IsDefaultLayer(num);
	}

	public bool IsDefaultLayer(int _blockLayer)
	{
		return this.isDefault(_blockLayer * this.bytesPerVal);
	}

	private bool isDefault(int _idx)
	{
		this.checkSameValue(_idx);
		return this.layers[_idx] == null && this.getSameValue(_idx) == this.defaultValue;
	}

	public int GetUsedMem()
	{
		int num = 0;
		for (int i = this.layers.Length - 1; i >= 0; i--)
		{
			if (this.layers[i] != null)
			{
				num += 1024;
			}
		}
		num += this.sameValue.Length;
		return num + this.layers.Length * 4;
	}

	public void FreeLayers()
	{
		MemoryPools.poolCBC.FreeSync(this.layers);
		this.fillSameValue(-1L);
	}

	public void Clear(long _defaultValue = -1L)
	{
		for (int i = 0; i < this.layers.Length; i++)
		{
			this.freeLayer(i);
		}
		this.fillSameValue(_defaultValue);
	}

	public void ClearHalf(bool _bClearUpperHalf)
	{
		byte b = (_bClearUpperHalf ? 15 : 240);
		for (int i = 0; i < 64; i++)
		{
			CBCLayer cbclayer = this.layers[i];
			if (cbclayer != null)
			{
				for (int j = 0; j < 1024; j++)
				{
					byte[] data = cbclayer.data;
					int num = j;
					data[num] &= b;
				}
			}
			else
			{
				byte[] array = this.sameValue;
				int num2 = i;
				array[num2] &= b;
			}
		}
	}

	public void SetHalf(bool _bSetUpperHalf, byte _v)
	{
		byte b = (_bSetUpperHalf ? 15 : 240);
		for (int i = 0; i < 64; i++)
		{
			CBCLayer cbclayer = this.layers[i];
			if (cbclayer != null)
			{
				for (int j = 0; j < 1024; j++)
				{
					byte[] data = cbclayer.data;
					int num = j;
					data[num] &= b;
					byte[] data2 = cbclayer.data;
					int num2 = j;
					data2[num2] |= _v;
				}
			}
			else
			{
				byte[] array = this.sameValue;
				int num3 = i;
				array[num3] &= b;
				byte[] array2 = this.sameValue;
				int num4 = i;
				array2[num4] |= _v;
			}
		}
	}

	public void CopyFrom(ChunkBlockChannel _other)
	{
		for (int i = 0; i < _other.layers.Length; i++)
		{
			if (_other.layers[i] != null)
			{
				if (this.layers[i] == null)
				{
					this.layers[i] = this.allocLayer();
				}
				this.layers[i].CopyFrom(_other.layers[i]);
			}
			else
			{
				this.freeLayer(i);
			}
		}
		for (int j = 0; j < _other.sameValue.Length; j++)
		{
			this.sameValue[j] = _other.sameValue[j];
		}
	}

	public void Convert(SmartArray _sa, int _shiftBits)
	{
		for (int i = 0; i < 256; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				for (int k = 0; k < 16; k++)
				{
					byte b = _sa.get(j, i, k);
					byte b2 = (byte)this.Get(j, i, k);
					b2 |= (byte)(b << _shiftBits);
					this.Set(j, i, k, (long)((ulong)b2));
				}
			}
		}
		this.CheckSameValue();
	}

	public void Convert(ChunkBlockLayerLegacy[] m_BlockLayers)
	{
		for (int i = 0; i < 256; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				for (int k = 0; k < 16; k++)
				{
					byte b;
					if (m_BlockLayers[i] != null)
					{
						b = m_BlockLayers[i].GetStabilityAt(j, k);
					}
					else
					{
						b = 0;
					}
					this.Set(j, i, k, (long)((ulong)b));
				}
			}
		}
		this.CheckSameValue();
	}

	private const int cElementsPerLayer = 1024;

	private byte[] sameValue;

	private CBCLayer[] layers;

	private long defaultValue;

	private int bytesPerVal;
}
