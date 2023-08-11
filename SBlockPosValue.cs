using System;

public struct SBlockPosValue
{
	public SBlockPosValue(Vector3i _blockPosition, BlockValue _blockValue)
	{
		this.blockPos = _blockPosition;
		this.blockValue = _blockValue;
	}

	public Vector3i blockPos;

	public BlockValue blockValue;
}
