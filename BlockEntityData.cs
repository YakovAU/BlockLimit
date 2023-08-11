using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
public class BlockEntityData
{
	public BlockEntityData()
	{
	}

	public BlockEntityData(BlockValue _blockValue, Vector3i _pos)
	{
		this.pos = _pos;
		this.blockValue = _blockValue;
	}

	private void GetMaterials()
	{
		if (this.Property == null)
		{
			this.Property = new MaterialPropertyBlock();
		}
		if (this.renderers != null)
		{
			this.renderers.Clear();
		}
		else
		{
			this.renderers = new List<Renderer>();
		}
		this.transform.GetComponentsInChildren<Renderer>(true, this.renderers);
	}

	public void Apply()
	{
		if (!this.bHasTransform)
		{
			return;
		}
		if (GameManager.IsDedicatedServer)
		{
			return;
		}
		for (int i = 0; i < this.renderers.Count; i++)
		{
			this.renderers[i].SetPropertyBlock(this.Property);
		}
	}

	public void Cleanup()
	{
		if (this.renderers != null)
		{
			this.renderers.Clear();
		}
	}

	public void SetMaterialColor(string name, Color value)
	{
		this.GetMaterials();
		if (this.renderers == null)
		{
			return;
		}
		if (GameManager.IsDedicatedServer)
		{
			return;
		}
		this.Property.SetColor(name, value);
	}

	public void SetMaterialValue(string name, float value)
	{
		this.GetMaterials();
		if (this.renderers == null)
		{
			return;
		}
		if (GameManager.IsDedicatedServer)
		{
			return;
		}
		this.Property.SetFloat(name, value);
	}

	public void SetMaterialColor(Color color)
	{
		this.GetMaterials();
		this.Property.SetColor("_Color", color);
		this.Apply();
	}

	public void UpdateTemperature()
	{
	}

	public override string ToString()
	{
		string text = "EntityBlockCreationData ";
		BlockValue blockValue = this.blockValue;
		return text + blockValue.ToString();
	}

	private MaterialPropertyBlock Property;

	private List<Renderer> renderers;

	public BlockValue blockValue;

	public Vector3i pos;

	public Transform transform;

	public bool bHasTransform;

	public bool bRenderingOn;

	public bool bNeedsTemperature;
}
