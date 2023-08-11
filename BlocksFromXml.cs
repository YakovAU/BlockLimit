using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

public class BlocksFromXml
{
	public static IEnumerator CreateBlocks(XmlFile _xmlFile, bool _fillLookupTable, bool _bEditMode = false)
	{
		XElement root = _xmlFile.XmlDoc.Root;
		if (!root.HasElements)
		{
			throw new Exception("No element <blocks> found!");
		}
		if (root.HasAttribute("defaultDescriptionKey"))
		{
			Block.defaultBlockDescriptionKey = root.GetAttribute("defaultDescriptionKey");
		}
		MicroStopwatch msw = new MicroStopwatch(true);
		foreach (XElement xelement in root.Elements("block"))
		{
			if (xelement.HasAttribute(XNames.shapes))
			{
				string attribute = xelement.GetAttribute(XNames.shapes);
				ShapesFromXml.CreateShapeVariants(_bEditMode, xelement, attribute);
			}
			else
			{
				BlocksFromXml.ParseBlock(_bEditMode, xelement);
			}
			if (msw.ElapsedMilliseconds > 50L)
			{
				yield return null;
				msw.ResetAndRestart();
			}
		}
		IEnumerator<XElement> enumerator = null;
		if (Application.isPlaying)
		{
			Resources.UnloadUnusedAssets();
		}
		yield break;
		yield break;
	}

	public static void ParseBlock(bool _bEditMode, XElement elementBlock)
	{
		DynamicProperties dynamicProperties = BlocksFromXml.ParseProperties(elementBlock);
		string attribute = elementBlock.GetAttribute(XNames.name);
		Block block = BlocksFromXml.CreateBlock(_bEditMode, attribute, dynamicProperties);
		bool flag;
		BlocksFromXml.ParseItemDrops(block, elementBlock, out flag);
		if (!flag)
		{
			BlocksFromXml.LoadExtendedItemDrops(block);
		}
		BlocksFromXml.InitBlock(block);
	}

	public static void ParseExtendedBlock(XElement elementBlock, out string extendedBlockName, out string excludedPropertiesList)
	{
		IEnumerable<XElement> enumerable = from e in elementBlock.Elements(XNames.property)
			where e.GetAttribute(XNames.name) == "Extends"
			select e;
		XElement xelement = ((enumerable != null) ? enumerable.FirstOrDefault<XElement>() : null);
		if (xelement != null)
		{
			extendedBlockName = xelement.GetAttribute(XNames.value);
			excludedPropertiesList = xelement.GetAttribute(XNames.param1);
			return;
		}
		extendedBlockName = null;
		excludedPropertiesList = null;
	}

	public static DynamicProperties ParseProperties(XElement elementBlock)
	{
		string text;
		string text2;
		BlocksFromXml.ParseExtendedBlock(elementBlock, out text, out text2);
		DynamicProperties dynamicProperties = BlocksFromXml.CreateProperties(text, text2);
		BlocksFromXml.LoadProperties(dynamicProperties, elementBlock);
		return dynamicProperties;
	}

	public static DynamicProperties CreateProperties(string extendedBlockName = null, string excludedPropertiesList = null)
	{
		DynamicProperties dynamicProperties = new DynamicProperties();
		if (extendedBlockName != null)
		{
			Block blockByName = Block.GetBlockByName(extendedBlockName, false);
			if (blockByName == null)
			{
				throw new Exception(string.Format("Could not find Extends block {0}", extendedBlockName));
			}
			HashSet<string> hashSet = new HashSet<string> { Block.PropCreativeMode };
			if (!string.IsNullOrEmpty(excludedPropertiesList))
			{
				foreach (string text in excludedPropertiesList.Split(',', StringSplitOptions.None))
				{
					hashSet.Add(text.Trim());
				}
			}
			dynamicProperties.CopyFrom(blockByName.Properties, hashSet);
		}
		return dynamicProperties;
	}

	public static void LoadProperties(DynamicProperties properties, XElement elementBlock)
	{
		foreach (XElement xelement in elementBlock.Elements(XNames.property))
		{
			properties.Add(xelement, true);
		}
	}

	public static Block CreateBlock(bool _bEditMode, string blockName, DynamicProperties properties)
	{
		Block block;
		if (properties.Values.ContainsKey("Class"))
		{
			string text = properties.Values["Class"];
			Type typeWithPrefix = ReflectionHelpers.GetTypeWithPrefix("Block", text);
			if (typeWithPrefix == null || (block = Activator.CreateInstance(typeWithPrefix) as Block) == null)
			{
				throw new Exception(string.Concat(new string[] { "Class '", text, "' not found on block ", blockName, "!" }));
			}
		}
		else
		{
			block = new Block();
		}
		block.Properties = properties;
		block.SetBlockName(blockName);
		block.ResourceScale = 1f;
		properties.ParseFloat(Block.PropResourceScale, ref block.ResourceScale);
		BlockPlacement blockPlacement = BlockPlacement.None;
		if (properties.Values.ContainsKey("Place"))
		{
			string text2 = properties.Values["Place"];
			try
			{
				blockPlacement = (BlockPlacement)Activator.CreateInstance(ReflectionHelpers.GetTypeWithPrefix("BlockPlacement", text2));
			}
			catch (Exception)
			{
				throw new Exception(string.Concat(new string[] { "No block placement class '", text2, "' found on block ", blockName, "!" }));
			}
		}
		block.BlockPlacementHelper = blockPlacement;
		string text3 = properties.Values["Material"];
		block.blockMaterial = MaterialBlock.fromString(text3);
		if (text3 == null || text3.Length == 0)
		{
			throw new Exception("Block with name=" + blockName + " has no material defined");
		}
		if (block.blockMaterial == null)
		{
			throw new Exception(string.Concat(new string[] { "Block with name=", blockName, " references a not existing material '", text3, "'" }));
		}
		BlockShape blockShape;
		if (properties.Values.ContainsKey("Shape"))
		{
			string text4 = properties.Values["Shape"];
			try
			{
				blockShape = (BlockShape)Activator.CreateInstance(ReflectionHelpers.GetTypeWithPrefix("BlockShape", text4));
				goto IL_218;
			}
			catch (Exception)
			{
				throw new Exception("Shape class '" + text4 + "' not found for block " + blockName);
			}
		}
		blockShape = new BlockShapeNew();
		block.Properties.Values["Model"] = "Cube";
		IL_218:
		block.shape = blockShape;
		if (properties.Values.ContainsKey("ShapeMinBB"))
		{
			Vector3 vector = StringParsers.ParseVector3(properties.Values["ShapeMinBB"], 0, -1);
			block.shape.SetMinAABB(vector);
		}
		if (properties.Values.ContainsKey("Mesh"))
		{
			block.MeshIndex = byte.MaxValue;
			string text5 = properties.Values["Mesh"];
			for (int i = 0; i < MeshDescription.meshes.Length; i++)
			{
				if (text5.Equals(MeshDescription.meshes[i].Name))
				{
					block.MeshIndex = (byte)i;
					break;
				}
			}
			if (block.MeshIndex == 255)
			{
				throw new Exception("Unknown mesh attribute '" + text5 + "' on block " + blockName);
			}
		}
		if (!_bEditMode && properties.Values.ContainsKey("Stacknumber"))
		{
			block.Stacknumber = int.Parse(properties.Values["Stacknumber"]);
		}
		else
		{
			block.Stacknumber = 500;
		}
		if (properties.Values.ContainsKey("Light"))
		{
			block.SetLightValue(StringParsers.ParseFloat(properties.Values["Light"], 0, -1, NumberStyles.Any));
		}
		if (properties.Values.ContainsKey("MovementFactor"))
		{
			block.MovementFactor = StringParsers.ParseFloat(properties.Values["MovementFactor"], 0, -1, NumberStyles.Any);
		}
		else
		{
			block.MovementFactor = block.blockMaterial.MovementFactor;
		}
		if (block.MovementFactor <= 0f)
		{
			block.MovementFactor = 1f;
		}
		block.IsCheckCollideWithEntity |= block.MovementFactor != 1f;
		if (properties.Values.ContainsKey("EconomicValue"))
		{
			block.EconomicValue = StringParsers.ParseFloat(properties.Values["EconomicValue"], 0, -1, NumberStyles.Any);
		}
		if (properties.Values.ContainsKey("Collide"))
		{
			string text6 = properties.Values["Collide"];
			block.BlockingType = 0;
			if (text6.ContainsCaseInsensitive("sight"))
			{
				block.BlockingType |= 1;
			}
			if (text6.ContainsCaseInsensitive("movement"))
			{
				block.BlockingType |= 2;
			}
			if (text6.ContainsCaseInsensitive("bullet"))
			{
				block.BlockingType |= 4;
			}
			if (text6.ContainsCaseInsensitive("rocket"))
			{
				block.BlockingType |= 8;
			}
			if (text6.ContainsCaseInsensitive("arrow"))
			{
				block.BlockingType |= 32;
			}
			if (text6.ContainsCaseInsensitive("melee"))
			{
				block.BlockingType |= 16;
			}
		}
		else
		{
			block.BlockingType = (block.blockMaterial.IsCollidable ? 255 : 0);
		}
		string text7;
		if (properties.Values.TryGetString("Path", out text7))
		{
			if (text7.EqualsCaseInsensitive("solid"))
			{
				block.PathType = 1;
			}
			if (text7.EqualsCaseInsensitive("scan"))
			{
				block.PathType = -1;
			}
		}
		else if (properties.Values.TryGetString("Model", out text7) && (text7.EqualsCaseInsensitive("cube") || text7.EqualsCaseInsensitive("cube_glass") || text7.EqualsCaseInsensitive("cube_frame")))
		{
			block.PathType = 1;
		}
		string text8;
		if (properties.Values.TryGetString("WaterFlow", out text8))
		{
			block.WaterFlowMask = StringParsers.ParseWaterFlowMask(text8);
		}
		string text9;
		if (properties.Values.TryGetString("WaterClipPlane", out text9))
		{
			block.WaterClipPlane = StringParsers.ParsePlane(text9);
			block.WaterClipEnabled = true;
		}
		else
		{
			block.WaterClipEnabled = false;
		}
		string @string = properties.GetString("Texture");
		if (@string.Length > 0)
		{
			try
			{
				if (@string.Contains(","))
				{
					string[] array = @string.Split(',', StringSplitOptions.None);
					block.SetSideTextureId(array);
				}
				else
				{
					int num = int.Parse(@string);
					block.SetSideTextureId(num);
				}
			}
			catch (Exception)
			{
				throw new Exception("Error parsing texture id '" + @string + "' in block with name=" + blockName);
			}
		}
		properties.ParseInt("TerrainIndex", ref block.TerrainTAIndex);
		if (properties.Values.ContainsKey("BlockTag"))
		{
			block.BlockTag = EnumUtils.Parse<BlockTags>(properties.Values["BlockTag"], false);
		}
		if (properties.Values.ContainsKey("StabilitySupport"))
		{
			block.StabilitySupport = properties.GetBool("StabilitySupport");
		}
		else
		{
			block.StabilitySupport = block.blockMaterial.StabilitySupport;
		}
		if (properties.Values.ContainsKey("StabilityFull"))
		{
			block.StabilityFull = properties.GetBool("StabilityFull");
		}
		if (properties.Values.ContainsKey("StabilityIgnore"))
		{
			block.StabilityIgnore = properties.GetBool("StabilityIgnore");
		}
		if (properties.Values.ContainsKey("Density"))
		{
			block.Density = (sbyte)(properties.GetFloat("Density") * (float)(block.shape.IsTerrain() ? MarchingCubes.DensityTerrain : MarchingCubes.DensityAir));
		}
		else
		{
			block.Density = (block.shape.IsTerrain() ? MarchingCubes.DensityTerrain : MarchingCubes.DensityAir);
		}
		DynamicProperties dynamicProperties = properties.Classes["RepairItems"];
		if (dynamicProperties != null)
		{
			block.RepairItems = new List<Block.SItemNameCount>();
			foreach (KeyValuePair<string, object> keyValuePair in dynamicProperties.Values.Dict.Dict)
			{
				Block.SItemNameCount sitemNameCount = default(Block.SItemNameCount);
				sitemNameCount.ItemName = keyValuePair.Key;
				sitemNameCount.Count = int.Parse(dynamicProperties.Values[keyValuePair.Key]);
				block.RepairItems.Add(sitemNameCount);
			}
		}
		DynamicProperties dynamicProperties2 = properties.Classes["RepairItemsMeshDamage"];
		if (dynamicProperties2 != null)
		{
			block.RepairItemsMeshDamage = new List<Block.SItemNameCount>();
			foreach (KeyValuePair<string, object> keyValuePair2 in dynamicProperties2.Values.Dict.Dict)
			{
				Block.SItemNameCount sitemNameCount2;
				sitemNameCount2.ItemName = keyValuePair2.Key;
				sitemNameCount2.Count = int.Parse(dynamicProperties2.Values[keyValuePair2.Key]);
				block.RepairItemsMeshDamage.Add(sitemNameCount2);
			}
		}
		if (properties.Values.ContainsKey("RestrictSubmergedPlacement"))
		{
			block.bRestrictSubmergedPlacement = properties.GetBool("RestrictSubmergedPlacement");
		}
		return block;
	}

	public static void ParseItemDrops(Block block, XElement elementBlock, out bool dropExtendsOff)
	{
		dropExtendsOff = false;
		foreach (XElement xelement in elementBlock.Elements())
		{
			if (xelement.Name == XNames.dropextendsoff)
			{
				dropExtendsOff = true;
			}
			else if (xelement.Name == XNames.drop)
			{
				XElement xelement2 = xelement;
				string attribute = xelement2.GetAttribute(XNames.name);
				int num = 1;
				int num2 = 1;
				if (xelement2.HasAttribute(XNames.count))
				{
					StringParsers.ParseMinMaxCount(xelement2.GetAttribute(XNames.count), out num, out num2);
				}
				float num3 = 1f;
				DynamicProperties.ParseFloat(xelement2, "prob", ref num3);
				num3 *= block.ResourceScale;
				EnumDropEvent enumDropEvent = EnumDropEvent.Destroy;
				if (xelement2.HasAttribute(XNames.event_))
				{
					enumDropEvent = EnumUtils.Parse<EnumDropEvent>(xelement2.GetAttribute(XNames.event_), false);
				}
				float num4 = 0f;
				DynamicProperties.ParseFloat(xelement2, "stick_chance", ref num4);
				string text = null;
				if (xelement2.HasAttribute(XNames.tool_category))
				{
					text = xelement2.GetAttribute(XNames.tool_category);
				}
				string attribute2 = xelement2.GetAttribute(XNames.tag);
				block.AddDroppedId(enumDropEvent, attribute, num, num2, num3, block.ResourceScale, num4, text, attribute2);
			}
		}
	}

	public static void LoadExtendedItemDrops(Block block)
	{
		if (block.Properties.Values.ContainsKey("Extends"))
		{
			Block blockByName = Block.GetBlockByName(block.Properties.Values["Extends"], false);
			block.CopyDroppedFrom(blockByName);
		}
	}

	public static void InitBlock(Block block)
	{
		block.shape.Init(block);
		block.Init();
	}

	public static bool FindExternalModels(XmlFile _xmlFile, string _meshName, Dictionary<string, string> _referencedModels)
	{
		try
		{
			XElement root = _xmlFile.XmlDoc.Root;
			if (!root.HasElements)
			{
				throw new Exception("No element <blocks> found!");
			}
			HashSet<string> hashSet = new HashSet<string>();
			foreach (XElement xelement in root.Elements(XNames.block))
			{
				string attribute = xelement.GetAttribute(XNames.name);
				DynamicProperties dynamicProperties = new DynamicProperties();
				foreach (XElement xelement2 in xelement.Elements(XNames.property))
				{
					dynamicProperties.Add(xelement2, true);
				}
				bool flag = false;
				if (dynamicProperties.Values.ContainsKey("Extends"))
				{
					string text = dynamicProperties.Values["Extends"];
					flag = hashSet.Contains(text);
				}
				bool flag2 = dynamicProperties.Values.ContainsKey("Shape") && dynamicProperties.Values["Shape"].StartsWith("Ext3dModel");
				bool flag3 = dynamicProperties.Values.ContainsKey("Shape") && !dynamicProperties.Values["Shape"].StartsWith("Ext3dModel");
				if ((flag && !flag3) || flag2)
				{
					string text2 = "opaque";
					if (dynamicProperties.Values.ContainsKey("Mesh"))
					{
						text2 = dynamicProperties.Values["Mesh"];
					}
					if (flag || text2.Equals(_meshName))
					{
						string text3 = dynamicProperties.Values["Model"];
						if (text3 != null && !_referencedModels.ContainsKey(text3))
						{
							_referencedModels.Add(text3, dynamicProperties.Params1["Model"]);
						}
						hashSet.Add(attribute);
					}
				}
			}
		}
		catch (Exception ex)
		{
			Log.Error(string.Concat(new string[] { "Loading and parsing '", _xmlFile.Filename, "' (", ex.Message, ")" }));
			Log.Error("Loading of blocks aborted due to errors!");
			Log.Error(ex.StackTrace);
			return false;
		}
		return true;
	}

	public static HashSet<int> GetTextureIdsForMesh(XmlFile _xmlFile, string _meshName)
	{
		HashSet<int> hashSet = new HashSet<int>();
		try
		{
			XElement root = _xmlFile.XmlDoc.Root;
			if (!root.HasElements)
			{
				throw new Exception("No element <blocks> found!");
			}
			Dictionary<string, DynamicProperties> dictionary = new Dictionary<string, DynamicProperties>();
			foreach (XElement xelement in root.Elements(XNames.block))
			{
				DynamicProperties dynamicProperties = new DynamicProperties();
				foreach (XElement xelement2 in xelement.Elements(XNames.property))
				{
					dynamicProperties.Add(xelement2, true);
				}
				if (dynamicProperties.Values.ContainsKey("Extends"))
				{
					string text = dynamicProperties.Values["Extends"];
					if (!dictionary.ContainsKey(text))
					{
						Log.Error(string.Format("Extends references not existing block {0}", text));
					}
					else
					{
						DynamicProperties dynamicProperties2 = new DynamicProperties();
						dynamicProperties2.CopyFrom(dictionary[text], null);
						dynamicProperties2.CopyFrom(dynamicProperties, null);
						dynamicProperties = dynamicProperties2;
					}
				}
				string attribute = xelement.GetAttribute(XNames.name);
				try
				{
					dictionary.Add(attribute, dynamicProperties);
				}
				catch (Exception)
				{
					throw new Exception("Duplicate block with name " + attribute);
				}
			}
			foreach (XElement xelement3 in root.Elements(XNames.block))
			{
				DynamicProperties dynamicProperties3 = dictionary[xelement3.GetAttribute(XNames.name)];
				string text2 = "opaque";
				if (dynamicProperties3.Values.ContainsKey("Mesh"))
				{
					text2 = dynamicProperties3.Values["Mesh"];
				}
				if (text2.Equals(_meshName) && (!dynamicProperties3.Values.ContainsKey("Shape") || (!dynamicProperties3.Values["Shape"].Equals("ModelEntity") && !dynamicProperties3.Values["Shape"].Equals("DistantDeco"))))
				{
					string text3 = dynamicProperties3.Values["Texture"];
					try
					{
						if (text3.Contains(","))
						{
							string[] array = text3.Split(new char[] { ',' });
							for (int i = 0; i < array.Length; i++)
							{
								hashSet.Add(int.Parse(array[i]));
							}
						}
						else
						{
							hashSet.Add(int.Parse(text3));
						}
					}
					catch (Exception)
					{
						throw new Exception("Error parsing texture id '" + text3 + "' in block " + xelement3.GetAttribute("id"));
					}
				}
			}
		}
		catch (Exception ex)
		{
			Log.Error(string.Concat(new string[] { "Loading and parsing '", _xmlFile.Filename, "' (", ex.Message, ")" }));
			Log.Error("Loading of blocks aborted due to errors!");
			Log.Error(ex.StackTrace);
		}
		return hashSet;
	}
}
