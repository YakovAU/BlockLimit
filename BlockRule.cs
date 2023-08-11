using System;

public class BlockRule<O, M>
{
	public BlockRule()
	{
	}

	public BlockRule(O _output, M[] _mask)
	{
		this.Output = _output;
		this.Mask = _mask;
	}

	public O Output;

	public M[] Mask;
}
