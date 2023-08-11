using System;
using System.Collections.Generic;
using System.Globalization;
using Platform;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
public class Block
{
	public static bool BlocksLoaded
	{
		get
		{
			return Block.list != null;
		}
	}

	public RecipeUnlockData[] UnlockedBy
	{
		get
		{
			if (this.unlockedBy == null)
			{
				if (this.Properties.Values.ContainsKey(Block.PropUnlockedBy))
				{
					string[] array = this.Properties.Values[Block.PropUnlockedBy].Split(',', StringSplitOptions.None);
					if (array.Length != 0)
					{
						this.unlockedBy = new RecipeUnlockData[array.Length];
						for (int i = 0; i < array.Length; i++)
						{
							this.unlockedBy[i] = new RecipeUnlockData(array[i]);
						}
					}
				}
				else
				{
					this.unlockedBy = new RecipeUnlockData[0];
				}
			}
			return this.unlockedBy;
		}
	}

	public bool IsCollideMovement
	{
		get
		{
			return (this.BlockingType & 2) != 0;
		}
	}

	public bool IsCollideSight
	{
		get
		{
			return (this.BlockingType & 1) != 0;
		}
	}

	public bool IsCollideBullets
	{
		get
		{
			return (this.BlockingType & 4) != 0;
		}
	}

	public bool IsCollideRockets
	{
		get
		{
			return (this.BlockingType & 8) != 0;
		}
	}

	public bool IsCollideMelee
	{
		get
		{
			return (this.BlockingType & 16) != 0;
		}
	}

	public bool IsCollideArrows
	{
		get
		{
			return (this.BlockingType & 32) != 0;
		}
	}

	public bool IsNotifyOnLoadUnload
	{
		get
		{
			return this.bNotifyOnLoadUnload || this.shape.IsNotifyOnLoadUnload;
		}
		set
		{
			this.bNotifyOnLoadUnload = value;
		}
	}

	public List<ShapesFromXml.ShapeCategory> ShapeCategories { get; private set; }

	public virtual bool AllowBlockTriggers
	{
		get
		{
			return false;
		}
	}

	public Block()
	{
		this.shape = new BlockShapeCube();
		this.shape.Init(this);
		this.Properties = new DynamicProperties();
		this.blockMaterial = MaterialBlock.air;
		this.MeshIndex = 0;
	}

	public static Vector3 StringToVector3(string _input)
	{
		Vector3 zero = Vector3.zero;
		StringParsers.SeparatorPositions separatorPositions = StringParsers.GetSeparatorPositions(_input, ',', 2, 0, -1);
		int num = 255;
		int num2 = 255;
		int num3 = 255;
		StringParsers.TryParseSInt32(_input, out num, 0, separatorPositions.Sep1 - 1, NumberStyles.Integer);
		if (separatorPositions.TotalFound > 0)
		{
			StringParsers.TryParseSInt32(_input, out num2, separatorPositions.Sep1 + 1, separatorPositions.Sep2 - 1, NumberStyles.Integer);
		}
		if (separatorPositions.TotalFound > 1)
		{
			StringParsers.TryParseSInt32(_input, out num3, separatorPositions.Sep2 + 1, separatorPositions.Sep3 - 1, NumberStyles.Integer);
		}
		zero.x = (float)num / 255f;
		zero.y = (float)num2 / 255f;
		zero.z = (float)num3 / 255f;
		return zero;
	}

	private void AddRandomTintColor(string color, string materialName)
	{
		this.AddRandomTintColor(Block.StringToVector3(color), materialName);
	}

	private void AddRandomTintColor(Vector3 color, string materialName)
	{
		if (this.randomTintColors == null)
		{
			this.randomTintColors = new List<Block.TintColorOnMaterial>();
		}
		Block.TintColorOnMaterial tintColorOnMaterial;
		tintColorOnMaterial.color = color;
		tintColorOnMaterial.materialName = materialName;
		this.randomTintColors.Add(tintColorOnMaterial);
	}

	public virtual void Init()
	{
		if (Block.nameToBlockCaseInsensitive.ContainsKey(this.blockName))
		{
			Log.Error("Block " + this.blockName + " is found multiple times, overriding with latest definition!");
		}
		Block.nameToBlock[this.blockName] = this;
		Block.nameToBlockCaseInsensitive[this.blockName] = this;
		if (this.Properties.Values.ContainsKey(Block.PropTag))
		{
			this.Tags = FastTags.Parse(this.Properties.Values[Block.PropTag]);
		}
		if (this.Properties.Values.ContainsKey(Block.PropLightOpacity))
		{
			int.TryParse(this.Properties.Values[Block.PropLightOpacity], out this.lightOpacity);
		}
		else
		{
			this.lightOpacity = Math.Max(this.blockMaterial.LightOpacity, (int)this.shape.LightOpacity);
		}
		this.Properties.ParseColorHex(Block.PropTintColor, ref this.tintColor);
		for (int i = 1; i < 99; i++)
		{
			string text = Block.propRandomTintColorNames[i];
			if (!this.Properties.Values.ContainsKey(text))
			{
				break;
			}
			string text2 = "";
			if (this.Properties.Params1.ContainsKey(text))
			{
				text2 = this.Properties.Params1[text];
			}
			this.AddRandomTintColor(this.Properties.Values[text], text2);
		}
		StringParsers.TryParseBool(this.Properties.Values[Block.PropCanPickup], out this.CanPickup, 0, -1, true);
		if (this.CanPickup && this.Properties.Params1.ContainsKey(Block.PropCanPickup))
		{
			this.PickedUpItemValue = this.Properties.Params1[Block.PropCanPickup];
		}
		if (this.Properties.Values.ContainsKey(Block.PropFuelValue))
		{
			int.TryParse(this.Properties.Values[Block.PropFuelValue], out this.FuelValue);
		}
		if (this.Properties.Values.ContainsKey(Block.PropWeight))
		{
			int num;
			int.TryParse(this.Properties.Values[Block.PropWeight], out num);
			this.Weight = new DataItem<int>(num);
		}
		if (this.Properties.Values.ContainsKey(Block.PropCanMobsSpawnOn))
		{
			this.CanMobsSpawnOn = StringParsers.ParseBool(this.Properties.Values[Block.PropCanMobsSpawnOn], 0, -1, true);
		}
		else
		{
			this.CanMobsSpawnOn = false;
		}
		if (this.Properties.Values.ContainsKey(Block.PropCanPlayersSpawnOn))
		{
			this.CanPlayersSpawnOn = StringParsers.ParseBool(this.Properties.Values[Block.PropCanPlayersSpawnOn], 0, -1, true);
		}
		else
		{
			this.CanPlayersSpawnOn = true;
		}
		if (this.Properties.Values.ContainsKey(Block.PropPickupTarget))
		{
			this.PickupTarget = this.Properties.Values[Block.PropPickupTarget];
		}
		if (this.Properties.Values.ContainsKey(Block.PropPickupSource))
		{
			this.PickupSource = this.Properties.Values[Block.PropPickupSource];
		}
		if (this.Properties.Values.ContainsKey(Block.PropPlaceAltBlockValue))
		{
			this.placeAltBlockNames = this.Properties.Values[Block.PropPlaceAltBlockValue].Split(',', StringSplitOptions.None);
		}
		if (this.Properties.Values.ContainsKey(Block.PropPlaceShapeCategories))
		{
			string[] array = this.Properties.Values[Block.PropPlaceShapeCategories].Split(',', StringSplitOptions.None);
			this.ShapeCategories = new List<ShapesFromXml.ShapeCategory>();
			foreach (string text3 in array)
			{
				ShapesFromXml.ShapeCategory shapeCategory;
				if (ShapesFromXml.shapeCategories.TryGetValue(text3, out shapeCategory))
				{
					this.ShapeCategories.Add(shapeCategory);
				}
				else
				{
					Log.Error("Block " + this.blockName + " has unknown ShapeCategory " + text3);
				}
			}
		}
		if (this.Properties.Values.ContainsKey(Block.PropIndexName))
		{
			this.IndexName = this.Properties.Values[Block.PropIndexName];
		}
		this.Properties.ParseBool(Block.PropCanDecorateOnSlopes, ref this.CanDecorateOnSlopes);
		this.SlopeMaxCos = 90f;
		this.Properties.ParseFloat(Block.PropSlopeMax, ref this.SlopeMaxCos);
		this.SlopeMaxCos = Mathf.Cos(this.SlopeMaxCos * 0.0174532924f);
		if (this.Properties.Values.ContainsKey(Block.PropIsTerrainDecoration))
		{
			this.IsTerrainDecoration = StringParsers.ParseBool(this.Properties.Values[Block.PropIsTerrainDecoration], 0, -1, true);
		}
		if (this.Properties.Values.ContainsKey(Block.PropIsDecoration))
		{
			this.IsDecoration = StringParsers.ParseBool(this.Properties.Values[Block.PropIsDecoration], 0, -1, true);
		}
		if (this.Properties.Values.ContainsKey(Block.PropDistantDecoration))
		{
			this.IsDistantDecoration = StringParsers.ParseBool(this.Properties.Values[Block.PropDistantDecoration], 0, -1, true);
		}
		if (this.Properties.Values.ContainsKey(Block.PropBigDecorationRadius))
		{
			this.BigDecorationRadius = int.Parse(this.Properties.Values[Block.PropBigDecorationRadius]);
		}
		if (this.Properties.Values.ContainsKey(Block.PropSmallDecorationRadius))
		{
			this.SmallDecorationRadius = int.Parse(this.Properties.Values[Block.PropSmallDecorationRadius]);
		}
		this.Properties.ParseFloat(Block.PropGndAlign, ref this.GroundAlignDistance);
		this.Properties.ParseBool(Block.PropIgnoreKeystoneOverlay, ref this.IgnoreKeystoneOverlay);
		this.LPHardnessScale = 1f;
		if (this.Properties.Values.ContainsKey(Block.PropLPScale))
		{
			this.LPHardnessScale = StringParsers.ParseFloat(this.Properties.Values[Block.PropLPScale], 0, -1, NumberStyles.Any);
		}
		if (this.Properties.Values.ContainsKey(Block.PropMapColor))
		{
			this.MapColor = StringParsers.ParseColor32(this.Properties.Values[Block.PropMapColor]);
			this.bMapColorSet = true;
		}
		if (this.Properties.Values.ContainsKey(Block.PropMapColor2))
		{
			this.MapColor2 = StringParsers.ParseColor32(this.Properties.Values[Block.PropMapColor2]);
			this.bMapColor2Set = true;
		}
		if (this.Properties.Values.ContainsKey(Block.PropMapElevMinMax))
		{
			this.MapElevMinMax = StringParsers.ParseVector2i(this.Properties.Values[Block.PropMapElevMinMax], ',');
		}
		else
		{
			this.MapElevMinMax = Vector2i.zero;
		}
		if (this.Properties.Values.ContainsKey(Block.PropMapSpecular))
		{
			this.MapSpecular = StringParsers.ParseFloat(this.Properties.Values[Block.PropMapSpecular], 0, -1, NumberStyles.Any);
		}
		if (this.Properties.Values.ContainsKey(Block.PropGroupName))
		{
			string[] array3 = this.Properties.Values[Block.PropGroupName].Split(',', StringSplitOptions.None);
			if (array3.Length != 0)
			{
				this.GroupNames = new string[array3.Length];
				for (int k = 0; k < array3.Length; k++)
				{
					this.GroupNames[k] = array3[k].Trim();
				}
			}
		}
		if (this.Properties.Values.ContainsKey(Block.PropCustomIcon))
		{
			this.CustomIcon = this.Properties.Values[Block.PropCustomIcon];
		}
		if (this.Properties.Values.ContainsKey(Block.PropCustomIconTint))
		{
			this.CustomIconTint = StringParsers.ParseHexColor(this.Properties.Values[Block.PropCustomIconTint]);
		}
		else
		{
			this.CustomIconTint = Color.white;
		}
		if (this.Properties.Values.ContainsKey(Block.PropPlacementWireframe))
		{
			this.bHasPlacementWireframe = StringParsers.ParseBool(this.Properties.Values[Block.PropPlacementWireframe], 0, -1, true);
		}
		else
		{
			this.bHasPlacementWireframe = true;
		}
		if (this.Properties.Values.ContainsKey(Block.PropMultiBlockDim))
		{
			this.isMultiBlock = true;
			Vector3i vector3i = StringParsers.ParseVector3i(this.Properties.Values[Block.PropMultiBlockDim], 0, -1, false);
			List<Vector3i> list = new List<Vector3i>();
			if (this.Properties.Values.ContainsKey(Block.PropMultiBlockLayer + "0"))
			{
				int num2 = 0;
				while (this.Properties.Values.ContainsKey(Block.PropMultiBlockLayer + num2.ToString()))
				{
					string[] array4 = this.Properties.Values[Block.PropMultiBlockLayer + num2.ToString()].Split(',', StringSplitOptions.None);
					for (int l = 0; l < array4.Length; l++)
					{
						array4[l] = array4[l].Trim();
						if (array4[l].Length > vector3i.x)
						{
							throw new Exception("Multi block layer entry " + l.ToString() + " too long for block " + this.blockName);
						}
						for (int m = 0; m < array4[l].Length; m++)
						{
							if (array4[l][m] != ' ')
							{
								list.Add(new Vector3i(m, num2, l));
							}
						}
					}
					num2++;
				}
			}
			else
			{
				int num3 = vector3i.x / 2;
				int num4 = Mathf.RoundToInt((float)vector3i.x / 2f + 0.1f) - 1;
				int num5 = vector3i.z / 2;
				int num6 = Mathf.RoundToInt((float)vector3i.z / 2f + 0.1f) - 1;
				for (int n = -num3; n <= num4; n++)
				{
					for (int num7 = 0; num7 < vector3i.y; num7++)
					{
						for (int num8 = -num5; num8 <= num6; num8++)
						{
							list.Add(new Vector3i(n, num7, num8));
						}
					}
				}
			}
			this.multiBlockPos = new Block.MultiBlockArray(vector3i, list);
		}
		if (this.Properties.Values.ContainsKey(Block.PropHeatMapStrength))
		{
			this.HeatMapStrength = StringParsers.ParseFloat(this.Properties.Values[Block.PropHeatMapStrength], 0, -1, NumberStyles.Any);
		}
		if (this.Properties.Values.ContainsKey(Block.PropHeatMapTime))
		{
			this.HeatMapTime = uint.Parse(this.Properties.Values[Block.PropHeatMapTime]);
		}
		if (this.Properties.Values.ContainsKey(Block.PropHeatMapFrequency))
		{
			this.HeatMapFrequency = uint.Parse(this.Properties.Values[Block.PropHeatMapFrequency]);
		}
		this.FallDamage = 1f;
		if (this.Properties.Values.ContainsKey(Block.PropFallDamage))
		{
			this.FallDamage = StringParsers.ParseFloat(this.Properties.Values[Block.PropFallDamage], 0, -1, NumberStyles.Any);
		}
		if (this.Properties.Values.ContainsKey(Block.PropCount))
		{
			this.Count = int.Parse(this.Properties.Values[Block.PropCount]);
		}
		if (this.Properties.Values.ContainsKey("ImposterExclude"))
		{
			this.bImposterExclude = StringParsers.ParseBool(this.Properties.Values["ImposterExclude"], 0, -1, true);
		}
		if (this.Properties.Values.ContainsKey("ImposterExcludeAndStop"))
		{
			this.bImposterExcludeAndStop = StringParsers.ParseBool(this.Properties.Values["ImposterExcludeAndStop"], 0, -1, true);
		}
		if (this.Properties.Values.ContainsKey("ImposterDontBlock"))
		{
			this.bImposterDontBlock = StringParsers.ParseBool(this.Properties.Values["ImposterDontBlock"], 0, -1, true);
		}
		this.AllowedRotations = EBlockRotationClasses.No45;
		if (this.Properties.Values.ContainsKey(Block.PropAllowAllRotations) && StringParsers.ParseBool(this.Properties.Values[Block.PropAllowAllRotations], 0, -1, true))
		{
			this.AllowedRotations |= EBlockRotationClasses.Basic45;
		}
		if (this.Properties.Values.ContainsKey("OnlySimpleRotations") && StringParsers.ParseBool(this.Properties.Values["OnlySimpleRotations"], 0, -1, true))
		{
			this.AllowedRotations &= ~(EBlockRotationClasses.Headfirst | EBlockRotationClasses.Sideways);
		}
		if (this.Properties.Values.ContainsKey("AllowedRotations"))
		{
			this.AllowedRotations = EBlockRotationClasses.None;
			foreach (string text4 in this.Properties.Values["AllowedRotations"].Split(',', StringSplitOptions.None))
			{
				EBlockRotationClasses eblockRotationClasses;
				if (EnumUtils.TryParse<EBlockRotationClasses>(text4, out eblockRotationClasses, true))
				{
					this.AllowedRotations |= eblockRotationClasses;
				}
				else
				{
					Log.Error(string.Concat(new string[] { "Rotation class '", text4, "' not found for block '", this.blockName, "'" }));
				}
			}
		}
		if (this.Properties.Values.ContainsKey("PlaceAsRandomRotation"))
		{
			this.PlaceRandomRotation = StringParsers.ParseBool(this.Properties.Values["PlaceAsRandomRotation"], 0, -1, true);
		}
		if (this.Properties.Values.ContainsKey(Block.PropIsPlant))
		{
			this.bIsPlant = StringParsers.ParseBool(this.Properties.Values[Block.PropIsPlant], 0, -1, true);
		}
		this.Properties.ParseString("CustomPlaceSound", ref this.CustomPlaceSound);
		this.Properties.ParseString("UpgradeSound", ref this.UpgradeSound);
		this.Properties.ParseString("DowngradeFX", ref this.DowngradeFX);
		this.Properties.ParseString("DestroyFX", ref this.DestroyFX);
		if (this.Properties.Values.ContainsKey(Block.PropBuffsWhenWalkedOn))
		{
			this.BuffsWhenWalkedOn = this.Properties.Values[Block.PropBuffsWhenWalkedOn].Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
			if (this.BuffsWhenWalkedOn.Length < 1)
			{
				this.BuffsWhenWalkedOn = null;
			}
		}
		this.Properties.ParseBool(Block.PropIsReplaceRandom, ref this.IsReplaceRandom);
		if (this.Properties.Values.ContainsKey(Block.PropCraftExpValue))
		{
			StringParsers.TryParseFloat(this.Properties.Values[Block.PropCraftExpValue], out this.CraftComponentExp, 0, -1, NumberStyles.Any);
		}
		if (this.Properties.Values.ContainsKey(Block.PropCraftTimeValue))
		{
			StringParsers.TryParseFloat(this.Properties.Values[Block.PropCraftTimeValue], out this.CraftComponentTime, 0, -1, NumberStyles.Any);
		}
		if (this.Properties.Values.ContainsKey(Block.PropLootExpValue))
		{
			StringParsers.TryParseFloat(this.Properties.Values[Block.PropLootExpValue], out this.LootExp, 0, -1, NumberStyles.Any);
		}
		if (this.Properties.Values.ContainsKey(Block.PropDestroyExpValue))
		{
			StringParsers.TryParseFloat(this.Properties.Values[Block.PropDestroyExpValue], out this.DestroyExp, 0, -1, NumberStyles.Any);
		}
		this.Properties.ParseString(Block.PropParticleOnDeath, ref this.deathParticleName);
		if (this.Properties.Values.ContainsKey(Block.PropPlaceExpValue))
		{
			StringParsers.TryParseFloat(this.Properties.Values[Block.PropPlaceExpValue], out this.PlaceExp, 0, -1, NumberStyles.Any);
		}
		if (this.Properties.Values.ContainsKey(Block.PropUpgradeExpValue))
		{
			StringParsers.TryParseFloat(this.Properties.Values[Block.PropUpgradeExpValue], out this.UpgradeExp, 0, -1, NumberStyles.Any);
		}
		this.Properties.ParseFloat(Block.PropEconomicValue, ref this.EconomicValue);
		this.Properties.ParseFloat(Block.PropEconomicSellScale, ref this.EconomicSellScale);
		this.Properties.ParseInt(Block.PropEconomicBundleSize, ref this.EconomicBundleSize);
		if (this.Properties.Values.ContainsKey(Block.PropSellableToTrader))
		{
			StringParsers.TryParseBool(this.Properties.Values[Block.PropSellableToTrader], out this.SellableToTrader, 0, -1, true);
		}
		this.Properties.ParseString(Block.PropTraderStageTemplate, ref this.TraderStageTemplate);
		if (this.Properties.Values.ContainsKey(Block.PropPickupJournalEntry))
		{
			this.PickupJournalEntry = this.Properties.Values[Block.PropPickupJournalEntry];
		}
		if (this.Properties.Values.ContainsKey(Block.PropCreativeMode))
		{
			this.CreativeMode = EnumUtils.Parse<EnumCreativeMode>(this.Properties.Values[Block.PropCreativeMode], false);
		}
		if (this.Properties.Values.ContainsKey(Block.PropFilterTag))
		{
			this.FilterTags = this.Properties.Values[Block.PropFilterTag].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			if (this.FilterTags.Length < 1)
			{
				this.FilterTags = null;
			}
		}
		this.SortOrder = this.Properties.GetString(Block.PropCreativeSort1);
		this.SortOrder += this.Properties.GetString(Block.PropCreativeSort2);
		if (this.Properties.Values.ContainsKey(Block.PropDisplayType))
		{
			this.DisplayType = this.Properties.Values[Block.PropDisplayType];
		}
		if (this.Properties.Values.ContainsKey(Block.PropItemTypeIcon))
		{
			this.ItemTypeIcon = this.Properties.Values[Block.PropItemTypeIcon];
		}
		if (this.Properties.Values.ContainsKey(Block.PropAutoShape))
		{
			this.AutoShapeType = EnumUtils.Parse<EAutoShapeType>(this.Properties.Values[Block.PropAutoShape], false);
			if (this.AutoShapeType != EAutoShapeType.None)
			{
				string[] array5 = this.blockName.Split(':', StringSplitOptions.None);
				this.autoShapeBaseName = array5[0];
				this.autoShapeShapeName = array5[1];
			}
		}
		this.MaxDamage = this.blockMaterial.MaxDamage;
		this.Properties.ParseInt(Block.PropMaxDamage, ref this.MaxDamage);
		this.Properties.ParseInt(Block.PropStartDamage, ref this.StartDamage);
		this.Properties.ParseInt(Block.PropStage2Health, ref this.Stage2Health);
		this.Properties.ParseFloat(Block.PropDamage, ref this.Damage);
		if (this.Properties.Values.ContainsKey(Block.PropActivationDistance))
		{
			int.TryParse(this.Properties.Values[Block.PropActivationDistance], out this.activationDistance);
		}
		if (this.Properties.Values.ContainsKey(Block.PropPlacementDistance))
		{
			int.TryParse(this.Properties.Values[Block.PropPlacementDistance], out this.placementDistance);
		}
		if (this.Properties.Values.ContainsKey("PassThroughDamage"))
		{
			this.EnablePassThroughDamage = StringParsers.ParseBool(this.Properties.Values["PassThroughDamage"], 0, -1, true);
		}
		if (this.Properties.Values.ContainsKey("CopyPaintOnDowngrade"))
		{
			string[] array6 = this.Properties.Values["CopyPaintOnDowngrade"].Split(',', StringSplitOptions.None);
			HashSet<BlockFace> hashSet = new HashSet<BlockFace>();
			for (int num9 = 0; num9 < array6.Length; num9++)
			{
				char c = array6[num9][0];
				if (c <= 'E')
				{
					if (c != 'B')
					{
						if (c == 'E')
						{
							hashSet.Add(BlockFace.East);
						}
					}
					else
					{
						hashSet.Add(BlockFace.Bottom);
					}
				}
				else if (c != 'N')
				{
					switch (c)
					{
					case 'S':
						hashSet.Add(BlockFace.South);
						break;
					case 'T':
						hashSet.Add(BlockFace.Top);
						break;
					case 'W':
						hashSet.Add(BlockFace.West);
						break;
					}
				}
				else
				{
					hashSet.Add(BlockFace.North);
				}
			}
			this.RemovePaintOnDowngrade = new List<BlockFace>();
			for (int num10 = 0; num10 < 6; num10++)
			{
				if (!hashSet.Contains((BlockFace)num10))
				{
					this.RemovePaintOnDowngrade.Add((BlockFace)num10);
				}
			}
		}
		string @string = this.Properties.GetString("UseGlobalUV");
		if (@string.Length > 0)
		{
			this.UVModesPerSide = 0U;
			if (!@string.Contains(","))
			{
				char c2 = @string[0];
				Block.UVMode uvmode = ((c2 == 'G') ? Block.UVMode.Global : ((c2 == 'L') ? Block.UVMode.Local : Block.UVMode.Default));
				for (int num11 = 0; num11 < this.cUVModeSides; num11++)
				{
					this.UVModesPerSide |= (uint)((uint)uvmode << ((num11 * this.cUVModeBits) & 31));
				}
			}
			else
			{
				int num12 = 0;
				foreach (char c3 in @string)
				{
					if (c3 != ',')
					{
						Block.UVMode uvmode2 = ((c3 == 'G') ? Block.UVMode.Global : ((c3 == 'L') ? Block.UVMode.Local : Block.UVMode.Default));
						this.UVModesPerSide |= (uint)((uint)uvmode2 << (num12 & 31));
						num12 += this.cUVModeBits;
					}
				}
			}
		}
		if (this.Properties.Values.ContainsKey(Block.PropRadiusEffect))
		{
			string[] array7 = this.Properties.Values[Block.PropRadiusEffect].Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
			List<BlockRadiusEffect> list2 = new List<BlockRadiusEffect>();
			foreach (string text5 in array7)
			{
				int num14 = text5.IndexOf('(');
				int num15 = text5.IndexOf(')');
				BlockRadiusEffect blockRadiusEffect = default(BlockRadiusEffect);
				if (num14 != -1 && num15 != -1 && num15 > num14 + 1)
				{
					blockRadiusEffect.radius = StringParsers.ParseFloat(text5.Substring(num14 + 1, num15 - num14 - 1), 0, -1, NumberStyles.Any);
					blockRadiusEffect.variable = text5.Substring(0, num14);
				}
				else
				{
					blockRadiusEffect.radius = 1f;
					blockRadiusEffect.variable = text5;
				}
				list2.Add(blockRadiusEffect);
			}
			this.RadiusEffects = list2.ToArray();
		}
		else
		{
			this.RadiusEffects = null;
		}
		if (this.Properties.Values.ContainsKey(Block.PropDescriptionKey))
		{
			this.DescriptionKey = this.Properties.Values[Block.PropDescriptionKey];
		}
		else
		{
			this.DescriptionKey = string.Format("{0}Desc", this.blockName);
			if (!Localization.Exists(this.DescriptionKey))
			{
				this.DescriptionKey = Block.defaultBlockDescriptionKey;
			}
		}
		if (this.Properties.Values.ContainsKey(Block.PropCraftingSkillGroup))
		{
			this.CraftingSkillGroup = this.Properties.Values[Block.PropCraftingSkillGroup];
		}
		else
		{
			this.CraftingSkillGroup = "";
		}
		if (this.Properties.Values.ContainsKey(Block.PropHarvestOverdamage))
		{
			this.HarvestOverdamage = StringParsers.ParseBool(this.Properties.Values[Block.PropHarvestOverdamage], 0, -1, true);
		}
		this.bShowModelOnFall = !this.Properties.Values.ContainsKey(Block.PropShowModelOnFall) || StringParsers.ParseBool(this.Properties.Values[Block.PropShowModelOnFall], 0, -1, true);
		if (this.Properties.Values.ContainsKey("HandleFace"))
		{
			this.HandleFace = EnumUtils.Parse<BlockFace>(this.Properties.Values["HandleFace"], false);
		}
		if (this.Properties.Values.ContainsKey("DisplayInfo"))
		{
			this.DisplayInfo = EnumUtils.Parse<Block.EnumDisplayInfo>(this.Properties.Values["DisplayInfo"], false);
		}
		if (this.Properties.Values.ContainsKey("SelectAlternates"))
		{
			this.SelectAlternates = StringParsers.ParseBool(this.Properties.Values["SelectAlternates"], 0, -1, true);
		}
		if (this.Properties.Values.ContainsKey(Block.PropNoScrapping))
		{
			this.NoScrapping = StringParsers.ParseBool(this.Properties.Values[Block.PropNoScrapping], 0, -1, true);
		}
		this.VehicleHitScale = 1f;
		this.Properties.ParseFloat(Block.PropVehicleHitScale, ref this.VehicleHitScale);
		if (this.Properties.Values.ContainsKey("UiBackgroundTexture") && !StringParsers.TryParseSInt32(this.Properties.Values["UiBackgroundTexture"], out this.uiBackgroundTextureId, 0, -1, NumberStyles.Integer))
		{
			this.uiBackgroundTextureId = -1;
		}
		this.Properties.ParseString(Block.PropBlockAddedEvent, ref this.blockAddedEvent);
		this.Properties.ParseString(Block.PropBlockDowngradeEvent, ref this.blockDowngradeEvent);
		this.Properties.ParseString(Block.PropBlockDowngradedToEvent, ref this.blockDowngradedToEvent);
	}

	public virtual void LateInit()
	{
		this.shape.LateInit();
		if (this.AutoShapeType == EAutoShapeType.Shape)
		{
			this.autoShapeHelper = Block.GetBlockByName(this.autoShapeBaseName + ":" + ShapesFromXml.VariantHelperName, false);
		}
		if (this.Properties.Values.ContainsKey(Block.PropSiblingBlock))
		{
			this.SiblingBlock = ItemClass.GetItem(this.Properties.Values[Block.PropSiblingBlock], false).ToBlockValue();
			if (this.SiblingBlock.Equals(BlockValue.Air))
			{
				throw new Exception("Block with name '" + this.Properties.Values[Block.PropSiblingBlock] + "' not found in block " + this.blockName);
			}
		}
		else
		{
			this.SiblingBlock = BlockValue.Air;
		}
		if (this.Properties.Values.ContainsKey("MirrorSibling"))
		{
			string text = this.Properties.Values["MirrorSibling"];
			this.MirrorSibling = ItemClass.GetItem(text, false).ToBlockValue().type;
			if (this.MirrorSibling == 0)
			{
				throw new Exception("Block with name '" + text + "' not found in block " + this.blockName);
			}
		}
		else
		{
			this.MirrorSibling = 0;
		}
		if (this.Properties.Values.ContainsKey(Block.PropUpgradeBlockClassToBlock))
		{
			this.UpgradeBlock = Block.GetBlockValue(this.Properties.Values[Block.PropUpgradeBlockClassToBlock], false);
			if (this.UpgradeBlock.isair)
			{
				throw new Exception("Block with name '" + this.Properties.Values[Block.PropUpgradeBlockClassToBlock] + "' not found in block " + this.blockName);
			}
		}
		else
		{
			this.UpgradeBlock = BlockValue.Air;
		}
		if (this.Properties.Values.ContainsKey(Block.PropDowngradeBlock))
		{
			this.DowngradeBlock = Block.GetBlockValue(this.Properties.Values[Block.PropDowngradeBlock], false);
			if (this.DowngradeBlock.isair)
			{
				throw new Exception("Block with name '" + this.Properties.Values[Block.PropDowngradeBlock] + "' not found in block " + this.blockName);
			}
		}
		else
		{
			this.DowngradeBlock = BlockValue.Air;
		}
		if (this.Properties.Values.ContainsKey(Block.PropLockpickDowngradeBlock))
		{
			this.LockpickDowngradeBlock = Block.GetBlockValue(this.Properties.Values[Block.PropLockpickDowngradeBlock], false);
			if (this.LockpickDowngradeBlock.isair)
			{
				throw new Exception("Block with name '" + this.Properties.Values[Block.PropLockpickDowngradeBlock] + "' not found in block " + this.blockName);
			}
		}
		else
		{
			this.LockpickDowngradeBlock = this.DowngradeBlock;
		}
		if (this.Properties.Values.ContainsKey("ImposterExchange"))
		{
			this.ImposterExchange = Block.GetBlockValue(this.Properties.Values["ImposterExchange"], false).type;
			if (this.Properties.Params1.ContainsKey("ImposterExchange"))
			{
				this.ImposterExchangeTexIdx = (byte)int.Parse(this.Properties.Params1["ImposterExchange"]);
			}
		}
		if (this.Properties.Values.ContainsKey("MergeInto"))
		{
			this.MergeIntoId = Block.GetBlockValue(this.Properties.Values["MergeInto"], false).type;
			if (this.MergeIntoId == 0)
			{
				Log.Warning("Warning: MergeInto block with name '{0}' not found!", new object[] { this.Properties.Values["MergeInto"] });
			}
			if (this.Properties.Params1.ContainsKey("MergeInto"))
			{
				string[] array = this.Properties.Params1["MergeInto"].Split(',', StringSplitOptions.None);
				if (array.Length == 6)
				{
					this.MergeIntoTexIds = new int[6];
					for (int i = 0; i < this.MergeIntoTexIds.Length; i++)
					{
						this.MergeIntoTexIds[i] = int.Parse(array[i].Trim());
					}
				}
			}
		}
	}

	public static void InitStatic()
	{
		Block.nameToBlock = new Dictionary<string, Block>();
		Block.nameToBlockCaseInsensitive = new CaseInsensitiveStringDictionary<Block>();
		Block.list = new Block[Block.MAX_BLOCKS];
		Block.propRandomTintColorNames = new string[99];
		for (int i = 0; i < 99; i++)
		{
			Block.propRandomTintColorNames[i] = Block.PropRandomTintColor + i.ToString();
		}
	}

	public static void LateInitAll()
	{
		for (int i = 0; i < Block.MAX_BLOCKS; i++)
		{
			if (Block.list[i] != null)
			{
				Block.list[i].LateInit();
			}
		}
		int type = BlockValue.Air.type;
		for (int j = 0; j < Block.MAX_BLOCKS; j++)
		{
			Block block = Block.list[j];
			if (block != null)
			{
				int num = block.MaxDamage;
				int num2 = 0;
				int num3 = block.DowngradeBlock.type;
				while (num3 != type)
				{
					Block block2 = Block.list[num3];
					num += block2.MaxDamage;
					num3 = block2.DowngradeBlock.type;
					if (++num2 > 10)
					{
						Log.Warning("Block '{0}' over downgrade limit", new object[] { block.blockName });
						break;
					}
				}
				block.MaxDamagePlusDowngrades = num;
			}
		}
	}

	public virtual bool FilterIndexType(BlockValue bv)
	{
		return true;
	}

	public Vector2 GetPathOffset(int _rotation)
	{
		if (this.PathType != -1)
		{
			return Vector2.zero;
		}
		return this.shape.GetPathOffset(_rotation);
	}

	public static void Cleanup()
	{
		Block.nameToBlock = null;
		Block.nameToBlockCaseInsensitive = null;
		Block.list = null;
		Block.fullMappingDataForClients = null;
		Block.propRandomTintColorNames = null;
	}

	public void CopyDroppedFrom(Block _other)
	{
		foreach (KeyValuePair<EnumDropEvent, List<Block.SItemDropProb>> keyValuePair in _other.itemsToDrop)
		{
			EnumDropEvent key = keyValuePair.Key;
			List<Block.SItemDropProb> value = keyValuePair.Value;
			List<Block.SItemDropProb> list = (this.itemsToDrop.ContainsKey(key) ? this.itemsToDrop[key] : null);
			if (list == null)
			{
				list = new List<Block.SItemDropProb>();
				this.itemsToDrop[key] = list;
			}
			for (int i = 0; i < value.Count; i++)
			{
				bool flag = true;
				int num = 0;
				while (flag && num < list.Count)
				{
					if (list[num].name == value[i].name)
					{
						flag = false;
					}
					num++;
				}
				if (flag)
				{
					list.Add(value[i]);
				}
			}
		}
	}

	public virtual BlockFace getInventoryFace()
	{
		return BlockFace.North;
	}

	public virtual byte GetLightValue(BlockValue _blockValue)
	{
		return this.lightValue;
	}

	public virtual Block SetLightValue(float _lightValueInPercent)
	{
		this.lightValue = (byte)(15f * _lightValueInPercent);
		return this;
	}

	public virtual bool UseBuffsWhenWalkedOn(World world, Vector3i _blockPos, BlockValue _blockValue)
	{
		return true;
	}

	public virtual bool IsMovementBlocked(IBlockAccess _world, Vector3i _blockPos, BlockValue _blockValue, BlockFace _face)
	{
		if (this.isMultiBlock && _blockValue.ischild)
		{
			Vector3i parentPos = this.multiBlockPos.GetParentPos(_blockPos, _blockValue);
			BlockValue block = _world.GetBlock(parentPos);
			if (block.ischild)
			{
				Log.Error("IsMovementBlocked {0} at {1} has child parent, {2} at {3} ", new object[] { this, _blockPos, block.Block, parentPos });
				return true;
			}
			return this.IsMovementBlocked(_world, parentPos, block, _face);
		}
		else
		{
			if (!this.IsCollideMovement)
			{
				return false;
			}
			if (this.BlocksMovement == 0)
			{
				return this.shape.IsMovementBlocked(_blockValue, _face);
			}
			return this.BlocksMovement == 1;
		}
	}

	public virtual bool IsSeeThrough(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
	{
		if (!this.isMultiBlock || !_blockValue.ischild)
		{
			return !this.IsCollideSight && !_world.IsWater(_blockPos);
		}
		Vector3i parentPos = this.multiBlockPos.GetParentPos(_blockPos, _blockValue);
		BlockValue block = _world.GetBlock(parentPos);
		if (block.ischild)
		{
			Log.Error("IsSeeThrough {0} at {1} has child parent, {2} at {3} ", new object[] { this, _blockPos, block.Block, parentPos });
			return true;
		}
		return this.IsSeeThrough(_world, _clrIdx, parentPos, block);
	}

	public virtual bool IsMovementBlocked(IBlockAccess _world, Vector3i _blockPos, BlockValue _blockValue, BlockFaceFlag _sides)
	{
		if (this.isMultiBlock && _blockValue.ischild)
		{
			Vector3i parentPos = this.multiBlockPos.GetParentPos(_blockPos, _blockValue);
			BlockValue block = _world.GetBlock(parentPos);
			if (block.ischild)
			{
				Log.Error("IsMovementBlocked {0} at {1} has child parent, {2} at {3} ", new object[] { this, _blockPos, block.Block, parentPos });
				return true;
			}
			return this.IsMovementBlocked(_world, parentPos, block, _sides);
		}
		else
		{
			if (_sides == BlockFaceFlag.None)
			{
				return this.IsMovementBlocked(_world, _blockPos, _blockValue, BlockFace.None);
			}
			for (int i = 0; i <= 5; i++)
			{
				if (((1 << i) & (int)_sides) != 0 && !this.IsMovementBlocked(_world, _blockPos, _blockValue, (BlockFace)i))
				{
					return false;
				}
			}
			return true;
		}
	}

	public virtual bool IsWaterBlocked(IBlockAccess _world, Vector3i _blockPos, BlockValue _blockValue, BlockFaceFlag _sides)
	{
		return this.IsMovementBlocked(_world, _blockPos, _blockValue, _sides);
	}

	public bool IsMovementBlocked(IBlockAccess _world, Vector3i _blockPos, BlockValue _blockValue, Vector3 _entityPos)
	{
		if (this.isMultiBlock && _blockValue.ischild)
		{
			Vector3i parentPos = this.multiBlockPos.GetParentPos(_blockPos, _blockValue);
			BlockValue block = _world.GetBlock(parentPos);
			if (block.ischild)
			{
				Log.Error("IsMovementBlocked {0} at {1} has child parent, {2} at {3} ", new object[] { this, _blockPos, block.Block, parentPos });
				return true;
			}
			return this.IsMovementBlocked(_world, parentPos, block, _entityPos);
		}
		else
		{
			BlockFaceFlag blockFaceFlag = BlockFaceFlags.FrontSidesFromPosition(_blockPos, _entityPos);
			if (blockFaceFlag == BlockFaceFlag.None)
			{
				return this.IsMovementBlocked(_world, _blockPos, _blockValue, BlockFace.None);
			}
			for (int i = 2; i <= 5; i++)
			{
				if (((1 << i) & (int)blockFaceFlag) != 0 && !this.IsMovementBlocked(_world, _blockPos, _blockValue, (BlockFace)i))
				{
					return false;
				}
			}
			return true;
		}
	}

	public bool IsMovementBlockedAny(IBlockAccess _world, Vector3i _blockPos, BlockValue _blockValue, Vector3 _entityPos)
	{
		if (this.isMultiBlock && _blockValue.ischild)
		{
			Vector3i parentPos = this.multiBlockPos.GetParentPos(_blockPos, _blockValue);
			BlockValue block = _world.GetBlock(parentPos);
			if (block.ischild)
			{
				Log.Error("IsMovementBlockedAny {0} at {1} has child parent, {2} at {3} ", new object[] { this, _blockPos, block.Block, parentPos });
				return true;
			}
			return this.IsMovementBlockedAny(_world, parentPos, block, _entityPos);
		}
		else
		{
			BlockFaceFlag blockFaceFlag = BlockFaceFlags.FrontSidesFromPosition(_blockPos, _entityPos);
			if (blockFaceFlag == BlockFaceFlag.None)
			{
				return this.IsMovementBlocked(_world, _blockPos, _blockValue, BlockFace.None);
			}
			for (int i = 2; i <= 5; i++)
			{
				if (((1 << i) & (int)blockFaceFlag) != 0 && this.IsMovementBlocked(_world, _blockPos, _blockValue, (BlockFace)i))
				{
					return true;
				}
			}
			return false;
		}
	}

	public virtual float GetStepHeight(IBlockAccess world, Vector3i blockPos, BlockValue blockDef, BlockFace stepFace)
	{
		if (!this.IsCollideMovement)
		{
			return 0f;
		}
		return this.shape.GetStepHeight(blockDef, stepFace);
	}

	public float MinStepHeight(BlockValue blockDef, BlockFaceFlag stepSides)
	{
		float num = -1f;
		for (int i = 2; i <= 5; i++)
		{
			if (((1 << i) & (int)stepSides) != 0)
			{
				if (num < 0f)
				{
					num = this.GetStepHeight(null, Vector3i.zero, blockDef, (BlockFace)i);
				}
				else
				{
					num = Math.Min(num, this.GetStepHeight(null, Vector3i.zero, blockDef, (BlockFace)i));
				}
			}
		}
		return Math.Max(num, 0f);
	}

	public float MaxStepHeight(BlockValue blockDef, BlockFaceFlag stepSides)
	{
		float num = -1f;
		for (int i = 2; i <= 5; i++)
		{
			if (((1 << i) & (int)stepSides) != 0)
			{
				if (num < 0f)
				{
					num = this.GetStepHeight(null, Vector3i.zero, blockDef, (BlockFace)i);
				}
				else
				{
					num = Math.Max(num, this.GetStepHeight(null, Vector3i.zero, blockDef, (BlockFace)i));
				}
			}
		}
		return Math.Max(num, 0f);
	}

	public float MinStepHeight(Vector3i blockPos, BlockValue blockDef, Vector3 entityPos)
	{
		BlockFaceFlag blockFaceFlag = BlockFaceFlags.FrontSidesFromPosition(blockPos, entityPos);
		return this.MinStepHeight(blockDef, blockFaceFlag);
	}

	public float MaxStepHeight(Vector3i blockPos, BlockValue blockDef, Vector3 entityPos)
	{
		BlockFaceFlag blockFaceFlag = BlockFaceFlags.FrontSidesFromPosition(blockPos, entityPos);
		return this.MaxStepHeight(blockDef, blockFaceFlag);
	}

	public virtual float GetHardness()
	{
		return this.blockMaterial.Hardness.Value;
	}

	public virtual int GetWeight()
	{
		int num = 0;
		if (this.Weight != null)
		{
			num = this.Weight.Value;
		}
		return num;
	}

	public Block.UVMode GetUVMode(int side)
	{
		return (Block.UVMode)((ulong)(this.UVModesPerSide >> side * this.cUVModeBits) & (ulong)((long)this.cUVModeMask));
	}

	public virtual Rect getUVRectFromSideAndMetadata(int _meshIndex, BlockFace _side, Vector3[] _vertices, BlockValue _blockValue)
	{
		return this.getUVRectFromSideAndMetadata(_meshIndex, _side, (_vertices != null) ? _vertices[0] : Vector3.zero, _blockValue);
	}

	public virtual Rect getUVRectFromSideAndMetadata(int _meshIndex, BlockFace _side, Vector3 _worldPos, BlockValue _blockValue)
	{
		int sideTextureId = this.GetSideTextureId(_blockValue, _side);
		if (sideTextureId < 0)
		{
			return UVRectTiling.Empty.uv;
		}
		UVRectTiling[] uvMapping = MeshDescription.meshes[_meshIndex].textureAtlas.uvMapping;
		if (sideTextureId >= uvMapping.Length)
		{
			return UVRectTiling.Empty.uv;
		}
		UVRectTiling uvrectTiling = uvMapping[sideTextureId];
		if (uvrectTiling.blockW == 1 && uvrectTiling.blockH == 1)
		{
			return uvrectTiling.uv;
		}
		float x = _worldPos.x;
		float y = _worldPos.y;
		float z = _worldPos.z;
		switch (_side)
		{
		case BlockFace.Top:
			return new Rect(uvrectTiling.uv.x + (float)Utils.FastRoundToIntAndMod(x, uvrectTiling.blockW) * uvrectTiling.uv.width, uvrectTiling.uv.y + (float)Utils.FastRoundToIntAndMod(z, uvrectTiling.blockH) * uvrectTiling.uv.height, uvrectTiling.uv.width, uvrectTiling.uv.height);
		case BlockFace.Bottom:
			return new Rect(uvrectTiling.uv.x + uvrectTiling.uv.width * (float)(uvrectTiling.blockW - 1) - (float)Utils.FastRoundToIntAndMod(x, uvrectTiling.blockW) * uvrectTiling.uv.width, uvrectTiling.uv.y + (float)Utils.FastRoundToIntAndMod(z, uvrectTiling.blockH) * uvrectTiling.uv.height, uvrectTiling.uv.width, uvrectTiling.uv.height);
		case BlockFace.North:
			return new Rect(uvrectTiling.uv.x + uvrectTiling.uv.width * (float)(uvrectTiling.blockW - 1) - (float)Utils.FastRoundToIntAndMod(x, uvrectTiling.blockW) * uvrectTiling.uv.width, uvrectTiling.uv.y + (float)Utils.FastRoundToIntAndMod(y, uvrectTiling.blockH) * uvrectTiling.uv.height, uvrectTiling.uv.width, uvrectTiling.uv.height);
		case BlockFace.West:
			return new Rect(uvrectTiling.uv.x + uvrectTiling.uv.width * (float)(uvrectTiling.blockW - 1) - (float)Utils.FastRoundToIntAndMod(z, uvrectTiling.blockW) * uvrectTiling.uv.width, uvrectTiling.uv.y + (float)Utils.FastRoundToIntAndMod(y, uvrectTiling.blockH) * uvrectTiling.uv.height, uvrectTiling.uv.width, uvrectTiling.uv.height);
		case BlockFace.South:
			return new Rect(uvrectTiling.uv.x + (float)Utils.FastRoundToIntAndMod(x, uvrectTiling.blockW) * uvrectTiling.uv.width, uvrectTiling.uv.y + (float)Utils.FastRoundToIntAndMod(y, uvrectTiling.blockH) * uvrectTiling.uv.height, uvrectTiling.uv.width, uvrectTiling.uv.height);
		case BlockFace.East:
			return new Rect(uvrectTiling.uv.x + (float)Utils.FastRoundToIntAndMod(z, uvrectTiling.blockW) * uvrectTiling.uv.width, uvrectTiling.uv.y + (float)Utils.FastRoundToIntAndMod(y, uvrectTiling.blockH) * uvrectTiling.uv.height, uvrectTiling.uv.width, uvrectTiling.uv.height);
		default:
			return new Rect(0f, 0f, 0f, 0f);
		}
	}

	public virtual Rect getUVRectFromSideAndRotationWedged(int _meshIndex, BlockFace _side, int _rotation, Vector3 _vertex, BlockValue _blockValue)
	{
		int sideTextureId = this.GetSideTextureId(_blockValue, _side);
		if (sideTextureId == -1)
		{
			return UVRectTiling.Empty.uv;
		}
		UVRectTiling uvrectTiling = MeshDescription.meshes[_meshIndex].textureAtlas.uvMapping[sideTextureId];
		if (uvrectTiling.blockW == 1 && uvrectTiling.blockH == 1)
		{
			return uvrectTiling.uv;
		}
		float x = _vertex.x;
		float y = _vertex.y;
		float z = _vertex.z;
		switch (_rotation)
		{
		case 1:
		case 3:
		case 7:
		case 9:
		case 11:
			return new Rect(uvrectTiling.uv.x + uvrectTiling.uv.width * (float)(uvrectTiling.blockW - 1) - (float)Utils.FastRoundToIntAndMod(x, uvrectTiling.blockW) * uvrectTiling.uv.width, uvrectTiling.uv.y + (float)Utils.FastRoundToIntAndMod(y, uvrectTiling.blockH) * uvrectTiling.uv.height, uvrectTiling.uv.width, uvrectTiling.uv.height);
		case 2:
		case 6:
		case 10:
			return new Rect(uvrectTiling.uv.x + uvrectTiling.uv.width * (float)(uvrectTiling.blockW - 1) - (float)Utils.FastRoundToIntAndMod(z, uvrectTiling.blockW) * uvrectTiling.uv.width, uvrectTiling.uv.y + (float)Utils.FastRoundToIntAndMod(y, uvrectTiling.blockH) * uvrectTiling.uv.height, uvrectTiling.uv.width, uvrectTiling.uv.height);
		}
		return new Rect(uvrectTiling.uv.x + (float)Utils.FastRoundToIntAndMod(z, uvrectTiling.blockW) * uvrectTiling.uv.width, uvrectTiling.uv.y + (float)Utils.FastRoundToIntAndMod(y, uvrectTiling.blockH) * uvrectTiling.uv.height, uvrectTiling.uv.width, uvrectTiling.uv.height);
	}

	public virtual void GetCollidingAABB(BlockValue _blockValue, int _x, int _y, int _z, float _distortedAddY, Bounds _aabb, List<Bounds> _aabbList)
	{
		Block.staticList_IntersectRayWithBlockList.Clear();
		this.GetCollisionAABB(_blockValue, _x, _y, _z, _distortedAddY, Block.staticList_IntersectRayWithBlockList);
		for (int i = 0; i < Block.staticList_IntersectRayWithBlockList.Count; i++)
		{
			Bounds bounds = Block.staticList_IntersectRayWithBlockList[i];
			if (_aabb.Intersects(bounds))
			{
				_aabbList.Add(bounds);
			}
		}
	}

	public virtual void GetCollisionAABB(BlockValue _blockValue, int _x, int _y, int _z, float _distortedAddY, List<Bounds> _result)
	{
		Vector3 vector = new Vector3(0f, _distortedAddY, 0f);
		foreach (Bounds bounds2 in this.shape.GetBounds(_blockValue))
		{
			bounds2.center += new Vector3((float)_x, (float)_y, (float)_z);
			bounds2.max += vector;
			_result.Add(bounds2);
		}
	}

	public virtual IList<Bounds> GetClipBoundsList(BlockValue _blockValue, Vector3 _blockPos)
	{
		return this.shape.GetBounds(_blockValue);
	}

	public virtual bool UpdateTick(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, bool _bRandomTick, ulong _ticksIfLoaded, GameRandom _rnd)
	{
		return false;
	}

	public virtual void DoExchangeAction(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, string _action, int _itemCount)
	{
	}

	public virtual void OnBlockLoaded(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
	{
		if (!_blockValue.ischild)
		{
			this.shape.OnBlockLoaded(_world, _clrIdx, _blockPos, _blockValue);
		}
	}

	public virtual void OnBlockUnloaded(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
	{
		if (!_blockValue.ischild)
		{
			this.shape.OnBlockUnloaded(_world, _clrIdx, _blockPos, _blockValue);
		}
	}

	public virtual void OnNeighborBlockChange(WorldBase world, int _clrIdx, Vector3i _myBlockPos, BlockValue _myBlockValue, Vector3i _blockPosThatChanged, BlockValue _newNeighborBlockValue, BlockValue _oldNeighborBlockValue)
	{
	}

	public static bool CanFallBelow(WorldBase _world, int _x, int _y, int _z)
	{
		BlockValue block = _world.GetBlock(_x, _y - 1, _z);
		Block block2 = block.Block;
		return block.isair || !block2.StabilitySupport;
	}

	public virtual ulong GetTickRate()
	{
		return 10UL;
	}

	public virtual void OnBlockAdded(WorldBase _world, Chunk _chunk, Vector3i _blockPos, BlockValue _blockValue)
	{
		if (_blockValue.ischild)
		{
			return;
		}
		this.shape.OnBlockAdded(_world, _chunk, _blockPos, _blockValue);
		if (this.isMultiBlock)
		{
			this.multiBlockPos.AddChilds(_world, _chunk, _chunk.ClrIdx, _blockPos, _blockValue);
		}
		if (!string.IsNullOrEmpty(this.blockAddedEvent))
		{
			GameEventManager.Current.HandleAction(this.blockAddedEvent, null, null, false, _blockPos, "", "", false, true, "", null);
		}
	}

	public virtual void OnBlockReset(WorldBase _world, Chunk _chunk, Vector3i _blockPos, BlockValue _blockValue)
	{
		bool ischild = _blockValue.ischild;
	}

	public virtual void OnBlockRemoved(WorldBase _world, Chunk _chunk, Vector3i _blockPos, BlockValue _blockValue)
	{
		if (!_blockValue.ischild)
		{
			this.shape.OnBlockRemoved(_world, _chunk, _blockPos, _blockValue);
			if (this.isMultiBlock)
			{
				this.multiBlockPos.RemoveChilds(_world, _chunk.ClrIdx, _blockPos, _blockValue);
				return;
			}
		}
		else if (this.isMultiBlock)
		{
			this.multiBlockPos.RemoveParentBlock(_world, _chunk.ClrIdx, _blockPos, _blockValue);
		}
	}

	public virtual void OnBlockValueChanged(WorldBase _world, Chunk _chunk, int _clrIdx, Vector3i _blockPos, BlockValue _oldBlockValue, BlockValue _newBlockValue)
	{
		if (_oldBlockValue.ischild)
		{
			return;
		}
		this.shape.OnBlockValueChanged(_world, _blockPos, _clrIdx, _oldBlockValue, _newBlockValue);
		if (this.isMultiBlock && _oldBlockValue.rotation != _newBlockValue.rotation)
		{
			if (_world.ChunkClusters[_clrIdx] == null)
			{
				return;
			}
			this.multiBlockPos.RemoveChilds(_world, _clrIdx, _blockPos, _oldBlockValue);
			this.multiBlockPos.AddChilds(_world, _chunk, _clrIdx, _blockPos, _newBlockValue);
		}
	}

	public virtual void OnBlockEntityTransformBeforeActivated(WorldBase _world, Vector3i _blockPos, int _cIdx, BlockValue _blockValue, BlockEntityData _ebcd)
	{
		this.shape.OnBlockEntityTransformBeforeActivated(_world, _blockPos, _cIdx, _blockValue, _ebcd);
	}

	public virtual void OnBlockEntityTransformAfterActivated(WorldBase _world, Vector3i _blockPos, int _cIdx, BlockValue _blockValue, BlockEntityData _ebcd)
	{
		this.shape.OnBlockEntityTransformAfterActivated(_world, _blockPos, _cIdx, _blockValue, _ebcd);
		_ebcd.UpdateTemperature();
		this.ForceAnimationState(_blockValue, _ebcd);
		if (this.GroundAlignDistance != 0f)
		{
			((World)_world).m_ChunkManager.AddGroundAlignBlock(_ebcd);
		}
		if (_world.TryRetrieveAndRemovePendingDowngradeBlock(_blockPos) && !string.IsNullOrEmpty(this.blockDowngradedToEvent))
		{
			GameEventManager.Current.HandleAction(this.blockDowngradedToEvent, null, null, false, _blockPos, "", "", false, true, "", null);
		}
	}

	public virtual void ForceAnimationState(BlockValue _blockValue, BlockEntityData _ebcd)
	{
	}

	public virtual int DamageBlock(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, int _damagePoints, int _entityIdThatDamaged, ItemActionAttack.AttackHitInfo _attackHitInfo = null, bool _bUseHarvestTool = false, bool _bBypassMaxDamage = false)
	{
		return this.OnBlockDamaged(_world, _clrIdx, _blockPos, _blockValue, _damagePoints, _entityIdThatDamaged, _attackHitInfo, _bUseHarvestTool, _bBypassMaxDamage, 0);
	}

	public virtual int OnBlockDamaged(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, int _damagePoints, int _entityIdThatDamaged, ItemActionAttack.AttackHitInfo _attackHitInfo, bool _bUseHarvestTool, bool _bBypassMaxDamage, int _recDepth = 0)
	{
		ChunkCluster chunkCluster = _world.ChunkClusters[_clrIdx];
		if (chunkCluster == null)
		{
			return 0;
		}
		if (this.isMultiBlock && _blockValue.ischild)
		{
			Vector3i parentPos = this.multiBlockPos.GetParentPos(_blockPos, _blockValue);
			BlockValue block = chunkCluster.GetBlock(parentPos);
			if (block.ischild)
			{
				Log.Error("Block on position {0} with name '{1}' should be a parent but is not! (6)", new object[]
				{
					parentPos,
					block.Block.blockName
				});
				return 0;
			}
			return block.Block.OnBlockDamaged(_world, _clrIdx, parentPos, block, _damagePoints, _entityIdThatDamaged, _attackHitInfo, _bUseHarvestTool, _bBypassMaxDamage, _recDepth + 1);
		}
		else
		{
			Block block2 = _blockValue.Block;
			int damage = _blockValue.damage;
			bool flag = damage >= block2.MaxDamage;
			int num = damage + _damagePoints;
			chunkCluster.InvokeOnBlockDamagedDelegates(_blockPos, _blockValue, _damagePoints, _entityIdThatDamaged);
			if (num < 0)
			{
				if (!this.UpgradeBlock.isair)
				{
					BlockValue blockValue = this.UpgradeBlock;
					blockValue = BlockPlaceholderMap.Instance.Replace(blockValue, _world.GetGameRandom(), _blockPos.x, _blockPos.z, false);
					blockValue.rotation = this.convertRotation(_blockValue, blockValue);
					blockValue.meta = _blockValue.meta;
					blockValue.damage = 0;
					Block block3 = blockValue.Block;
					if (!block3.shape.IsTerrain())
					{
						_world.SetBlockRPC(_clrIdx, _blockPos, blockValue);
						if (chunkCluster.GetTextureFull(_blockPos) != 0L)
						{
							GameManager.Instance.SetBlockTextureServer(_blockPos, BlockFace.None, 0, _entityIdThatDamaged);
						}
					}
					else
					{
						_world.SetBlockRPC(_clrIdx, _blockPos, blockValue, block3.Density);
					}
					DynamicMeshManager.ChunkChanged(_blockPos, _entityIdThatDamaged, _blockValue.type);
					return blockValue.damage;
				}
				if (_blockValue.damage != 0)
				{
					_blockValue.damage = 0;
					_world.SetBlockRPC(_clrIdx, _blockPos, _blockValue);
				}
				return 0;
			}
			else
			{
				if (this.Stage2Health > 0)
				{
					int num2 = block2.MaxDamage - this.Stage2Health;
					if (damage < num2 && num >= num2)
					{
						num = num2;
					}
				}
				if (!flag && num >= block2.MaxDamage)
				{
					int num3 = num - block2.MaxDamage;
					DynamicMeshManager.ChunkChanged(_blockPos, _entityIdThatDamaged, _blockValue.type);
					Block.DestroyedResult destroyedResult = this.OnBlockDestroyedBy(_world, _clrIdx, _blockPos, _blockValue, _entityIdThatDamaged, _bUseHarvestTool);
					if (destroyedResult != Block.DestroyedResult.Keep)
					{
						if (!this.DowngradeBlock.isair && destroyedResult == Block.DestroyedResult.Downgrade)
						{
							if (_recDepth == 0)
							{
								this.SpawnDowngradeFX(_world, _blockValue, _blockPos, block2.tintColor, _entityIdThatDamaged);
							}
							BlockValue blockValue2 = this.DowngradeBlock;
							blockValue2 = BlockPlaceholderMap.Instance.Replace(blockValue2, _world.GetGameRandom(), _blockPos.x, _blockPos.z, false);
							blockValue2.rotation = _blockValue.rotation;
							blockValue2.meta = _blockValue.meta;
							Block block4 = blockValue2.Block;
							if (!block4.shape.IsTerrain())
							{
								_world.SetBlockRPC(_clrIdx, _blockPos, blockValue2);
								if (chunkCluster.GetTextureFull(_blockPos) != 0L)
								{
									if (this.RemovePaintOnDowngrade == null)
									{
										GameManager.Instance.SetBlockTextureServer(_blockPos, BlockFace.None, 0, _entityIdThatDamaged);
									}
									else
									{
										for (int i = 0; i < this.RemovePaintOnDowngrade.Count; i++)
										{
											GameManager.Instance.SetBlockTextureServer(_blockPos, this.RemovePaintOnDowngrade[i], 0, _entityIdThatDamaged);
										}
									}
								}
								_world.AddPendingDowngradeBlock(_blockPos);
								if (!string.IsNullOrEmpty(this.blockDowngradeEvent))
								{
									GameEventManager.Current.HandleAction(this.blockDowngradeEvent, null, _world.GetEntity(_entityIdThatDamaged) as EntityPlayer, false, _blockPos, "", "", false, true, "", null);
								}
							}
							else
							{
								_world.SetBlockRPC(_clrIdx, _blockPos, blockValue2, block4.Density);
							}
							if ((num3 > 0 && this.EnablePassThroughDamage) || _bBypassMaxDamage)
							{
								block4.OnBlockDamaged(_world, _clrIdx, _blockPos, blockValue2, num3, _entityIdThatDamaged, _attackHitInfo, _bUseHarvestTool, _bBypassMaxDamage, _recDepth + 1);
							}
						}
						else
						{
							Entity entity = _world.GetEntity(_entityIdThatDamaged);
							QuestEventManager.Current.BlockDestroyed(block2, _blockPos, entity);
							this.SpawnDestroyFX(_world, _blockValue, _blockPos, this.GetColorForSide(_blockValue, BlockFace.Top), _entityIdThatDamaged);
							_world.SetBlockRPC(_clrIdx, _blockPos, BlockValue.Air);
							TileEntityLootContainer tileEntityLootContainer = _world.GetTileEntity(_clrIdx, _blockPos) as TileEntityLootContainer;
							if (tileEntityLootContainer != null)
							{
								tileEntityLootContainer.OnDestroy();
								for (int j = 0; j < LocalPlayerUI.PlayerUIs.Count; j++)
								{
									if (LocalPlayerUI.PlayerUIs[j].windowManager.IsWindowOpen("looting") && ((XUiC_LootWindow)LocalPlayerUI.PlayerUIs[j].xui.GetWindow("windowLooting").Controller).GetLootBlockPos() == _blockPos)
									{
										LocalPlayerUI.PlayerUIs[j].windowManager.Close("looting");
									}
								}
								Chunk chunk = _world.GetChunkFromWorldPos(_blockPos) as Chunk;
								if (chunk != null)
								{
									chunk.RemoveTileEntityAt<TileEntityLootContainer>((World)_world, World.toBlock(_blockPos));
								}
							}
						}
					}
					return block2.MaxDamage;
				}
				if (_blockValue.damage != num)
				{
					_blockValue.damage = num;
					if (!block2.shape.IsTerrain())
					{
						_world.SetBlocksRPC(new List<BlockChangeInfo>
						{
							new BlockChangeInfo(_blockPos, _blockValue, false, true)
						});
					}
					else
					{
						sbyte density = _world.GetDensity(_clrIdx, _blockPos);
						sbyte b = (sbyte)Utils.FastMin(-1f, (float)MarchingCubes.DensityTerrain * (1f - (float)num / (float)block2.MaxDamage));
						if ((_damagePoints > 0 && b > density) || (_damagePoints < 0 && b < density))
						{
							_world.SetBlockRPC(_clrIdx, _blockPos, _blockValue, b);
						}
						else
						{
							_world.SetBlockRPC(_clrIdx, _blockPos, _blockValue);
						}
					}
				}
				return _blockValue.damage;
			}
		}
	}

	public virtual bool IsHealthShownInUI(BlockValue _bv)
	{
		if (this.Stage2Health > 0)
		{
			return _bv.Block.MaxDamage - _bv.damage > this.Stage2Health;
		}
		return _bv.damage > 0;
	}

	private byte convertRotation(BlockValue _oldBV, BlockValue _newBV)
	{
		return _oldBV.rotation;
	}

	public void AddDroppedId(EnumDropEvent _eEvent, string _name, int _minCount, int _maxCount, float _prob, float _resourceScale, float _stickChance, string _toolCategory, string _tag)
	{
		List<Block.SItemDropProb> list;
		this.itemsToDrop.TryGetValue(_eEvent, out list);
		if (list == null)
		{
			list = new List<Block.SItemDropProb>();
			this.itemsToDrop[_eEvent] = list;
		}
		list.Add(new Block.SItemDropProb(_name, _minCount, _maxCount, _prob, _resourceScale, _stickChance, _toolCategory, _tag));
	}

	public bool HasItemsToDropForEvent(EnumDropEvent _eEvent)
	{
		return this.itemsToDrop.ContainsKey(_eEvent);
	}

	public void DropItemsOnEvent(WorldBase _world, BlockValue _blockValue, EnumDropEvent _eEvent, float _overallProb, Vector3 _dropPos, Vector3 _randomPosAdd, float _lifetime, int _entityId, bool _bGetSameItemIfNoneFound)
	{
		GameRandom gameRandom = _world.GetGameRandom();
		this.itemsDropped.Clear();
		List<Block.SItemDropProb> list;
		if (!this.itemsToDrop.TryGetValue(_eEvent, out list))
		{
			if (_bGetSameItemIfNoneFound)
			{
				ItemValue itemValue = _blockValue.ToItemValue();
				this.itemsDropped.Add(new ItemStack(itemValue, 1));
			}
		}
		else
		{
			for (int i = 0; i < list.Count; i++)
			{
				Block.SItemDropProb sitemDropProb = list[i];
				int num = gameRandom.RandomRange(sitemDropProb.minCount, sitemDropProb.maxCount + 1);
				if (num > 0)
				{
					if (sitemDropProb.stickChance < 0.001f || gameRandom.RandomFloat > sitemDropProb.stickChance)
					{
						if (sitemDropProb.name.Equals("[recipe]"))
						{
							List<Recipe> recipes = CraftingManager.GetRecipes(_blockValue.Block.GetBlockName());
							if (recipes.Count > 0)
							{
								for (int j = 0; j < recipes[0].ingredients.Count; j++)
								{
									if (recipes[0].ingredients[j].count / 2 > 0)
									{
										ItemStack itemStack = new ItemStack(recipes[0].ingredients[j].itemValue, recipes[0].ingredients[j].count / 2);
										this.itemsDropped.Add(itemStack);
									}
								}
							}
						}
						else
						{
							ItemValue itemValue2 = (sitemDropProb.name.Equals("*") ? _blockValue.ToItemValue() : new ItemValue(ItemClass.GetItem(sitemDropProb.name, false).type, false));
							if (!itemValue2.IsEmpty() && sitemDropProb.prob > gameRandom.RandomFloat)
							{
								this.itemsDropped.Add(new ItemStack(itemValue2, num));
							}
						}
					}
					else
					{
						Vector3i vector3i = World.worldToBlockPos(_dropPos);
						if (!GameManager.Instance.World.IsWithinTraderArea(vector3i) && (_overallProb >= 0.999f || gameRandom.RandomFloat < _overallProb))
						{
							BlockValue blockValue = Block.GetBlockValue(sitemDropProb.name, false);
							if (!blockValue.isair && _world.GetBlock(vector3i).isair)
							{
								_world.SetBlockRPC(vector3i, blockValue);
							}
						}
					}
				}
			}
		}
		for (int k = 0; k < this.itemsDropped.Count; k++)
		{
			if (_overallProb >= 0.999f || gameRandom.RandomFloat < _overallProb)
			{
				ItemClass itemClass = this.itemsDropped[k].itemValue.ItemClass;
				_lifetime = ((_lifetime > 0.001f) ? _lifetime : ((itemClass != null) ? itemClass.GetLifetimeOnDrop() : 0f));
				if (_lifetime > 0.001f)
				{
					_world.GetGameManager().ItemDropServer(this.itemsDropped[k], _dropPos, _randomPosAdd, _entityId, _lifetime, false);
				}
			}
		}
	}

	public float GetResistance()
	{
		return this.blockMaterial.Resistance;
	}

	public bool intersectRayWithBlock(BlockValue _blockValue, int _x, int _y, int _z, Ray _ray, out Vector3 _hitPoint, World _world)
	{
		Block.staticList_IntersectRayWithBlockList.Clear();
		this.GetCollisionAABB(_blockValue, _x, _y, _z, 0f, Block.staticList_IntersectRayWithBlockList);
		for (int i = 0; i < Block.staticList_IntersectRayWithBlockList.Count; i++)
		{
			if (Block.staticList_IntersectRayWithBlockList[i].IntersectRay(_ray))
			{
				_hitPoint = new Vector3((float)_x, (float)_y, (float)_z);
				return true;
			}
		}
		_hitPoint = Vector3.zero;
		return false;
	}

	public virtual Block.DestroyedResult OnBlockDestroyedByExplosion(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, int _playerThatStartedExpl)
	{
		ChunkCluster chunkCluster = _world.ChunkClusters[_clrIdx];
		if (chunkCluster != null)
		{
			chunkCluster.InvokeOnBlockDamagedDelegates(_blockPos, _blockValue, _blockValue.Block.MaxDamage, _playerThatStartedExpl);
		}
		return Block.DestroyedResult.Downgrade;
	}

	public virtual void OnBlockStartsToFall(WorldBase _world, Vector3i _blockPos, BlockValue _blockValue)
	{
		_world.SetBlockRPC(_blockPos, BlockValue.Air);
	}

	public virtual bool CanPlaceBlockAt(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, bool _bOmitCollideCheck = false)
	{
		if (_blockPos.y > 253)
		{
			return false;
		}
		if (!GameManager.Instance.IsEditMode() && ((World)_world).IsWithinTraderPlacingProtection(_blockPos))
		{
			return false;
		}
		Block block = _blockValue.Block;
		return (!block.isMultiBlock || _blockPos.y + block.multiBlockPos.dim.y < 254) && (GameManager.Instance.IsEditMode() || !block.bRestrictSubmergedPlacement || !this.IsUnderwater(_world, _blockPos, _blockValue)) && (GameManager.Instance.IsEditMode() || _bOmitCollideCheck || !this.overlapsWithOtherBlock(_world, _clrIdx, _blockPos, _blockValue));
	}

	public Vector3i GetFreePlacementPosition(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, EntityAlive _entityPlacing)
	{
		Vector3i vector3i = _blockPos;
		int num = 15;
		while (_blockValue.Block.overlapsWithOtherBlock(_world, _clrIdx, vector3i, _blockValue))
		{
			Vector3 vector = _entityPlacing.getHeadPosition() - (vector3i.ToVector3() + Vector3.one * 0.5f);
			Vector3 vector2;
			BlockFace blockFace;
			vector3i = Voxel.OneVoxelStep(vector3i, vector3i.ToVector3() + Vector3.one * 0.5f, vector, out vector2, out blockFace);
			if (--num <= 0)
			{
				break;
			}
		}
		if (num <= 0)
		{
			vector3i = _blockPos;
		}
		return vector3i;
	}

	private bool overlapsWithOtherBlock(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
	{
		if (!this.isMultiBlock)
		{
			int type = _world.GetBlock(_clrIdx, _blockPos).type;
			return type != 0 && !Block.list[type].blockMaterial.IsGroundCover;
		}
		byte rotation = _blockValue.rotation;
		for (int i = this.multiBlockPos.Length - 1; i >= 0; i--)
		{
			Vector3i vector3i = _blockPos + this.multiBlockPos.Get(i, _blockValue.type, (int)rotation);
			int type2 = _world.GetBlock(_clrIdx, vector3i).type;
			if (type2 != 0 && !Block.list[type2].blockMaterial.IsGroundCover)
			{
				return true;
			}
		}
		return false;
	}

	public bool IsUnderwater(WorldBase _world, Vector3i _blockPos, BlockValue _blockValue)
	{
		if (this.isMultiBlock)
		{
			int num = _blockPos.y + this.multiBlockPos.dim.y - 1;
			for (int i = 0; i < this.multiBlockPos.Length; i++)
			{
				Vector3i vector3i = _blockPos + this.multiBlockPos.Get(i, _blockValue.type, (int)_blockValue.rotation);
				if (vector3i.y == num && _world.IsWater(vector3i))
				{
					return true;
				}
			}
		}
		else if (_world.IsWater(_blockPos))
		{
			return true;
		}
		return false;
	}

	public virtual BlockValue OnBlockPlaced(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, GameRandom _rnd)
	{
		return _blockValue;
	}

	public virtual void OnBlockPlaceBefore(WorldBase _world, ref BlockPlacement.Result _bpResult, EntityAlive _ea, GameRandom _rnd)
	{
		DynamicMeshManager.ChunkChanged(_bpResult.blockPos, (_ea != null) ? _ea.entityId : (-2), _bpResult.blockValue.type);
		if (this.SelectAlternates)
		{
			byte rotation = _bpResult.blockValue.rotation;
			_bpResult.blockValue = _bpResult.blockValue.Block.GetAltBlockValue(_ea.inventory.holdingItemItemValue.Meta);
			_bpResult.blockValue.rotation = rotation;
		}
		else
		{
			string placeAltBlockValue = this.GetPlaceAltBlockValue(_world);
			_bpResult.blockValue = ((placeAltBlockValue.Length == 0) ? _bpResult.blockValue : Block.GetBlockValue(placeAltBlockValue, false));
		}
		Block block = _bpResult.blockValue.Block;
		if (block.PlaceRandomRotation)
		{
			int num;
			bool flag;
			do
			{
				num = _rnd.RandomRange(28);
				if (num < 4)
				{
					flag = (block.AllowedRotations & EBlockRotationClasses.Basic90) > EBlockRotationClasses.None;
				}
				else if (num < 8)
				{
					flag = (block.AllowedRotations & EBlockRotationClasses.Headfirst) > EBlockRotationClasses.None;
				}
				else if (num < 24)
				{
					flag = (block.AllowedRotations & EBlockRotationClasses.Sideways) > EBlockRotationClasses.None;
				}
				else
				{
					flag = (block.AllowedRotations & EBlockRotationClasses.Basic45) > EBlockRotationClasses.None;
				}
			}
			while (!flag);
			_bpResult.blockValue.rotation = (byte)num;
		}
	}

	public virtual void PlaceBlock(WorldBase _world, BlockPlacement.Result _result, EntityAlive _ea)
	{
		Block block = _result.blockValue.Block;
		if (block.shape.IsTerrain())
		{
			_world.SetBlockRPC(_result.clrIdx, _result.blockPos, _result.blockValue, this.Density);
		}
		else if (!block.IsTerrainDecoration)
		{
			_world.SetBlockRPC(_result.clrIdx, _result.blockPos, _result.blockValue, MarchingCubes.DensityAir);
		}
		else
		{
			_world.SetBlockRPC(_result.clrIdx, _result.blockPos, _result.blockValue);
		}
		if (this.blockName.Equals("keystoneBlock") && _ea is EntityPlayerLocal)
		{
			IAchievementManager achievementManager = PlatformManager.NativePlatform.AchievementManager;
			if (achievementManager == null)
			{
				return;
			}
			achievementManager.SetAchievementStat(EnumAchievementDataStat.LandClaimPlaced, 1);
		}
	}

	public virtual Block.DestroyedResult OnBlockDestroyedBy(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, int _entityId, bool _bUseHarvestTool)
	{
		return Block.DestroyedResult.Downgrade;
	}

	public virtual ItemStack OnBlockPickedUp(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, int _entityId)
	{
		ItemStack itemStack = new ItemStack((this.PickedUpItemValue == null) ? _blockValue.ToItemValue() : ItemClass.GetItem(this.PickedUpItemValue, false), 1);
		return (this.PickupTarget == null) ? itemStack : new ItemStack(new ItemValue(ItemClass.GetItem(this.PickupTarget, false).type, false), 1);
	}

	public virtual bool OnBlockActivated(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, EntityAlive _player)
	{
		TileEntityLootContainer tileEntityLootContainer = _world.GetTileEntity(_clrIdx, _blockPos) as TileEntityLootContainer;
		if (tileEntityLootContainer != null)
		{
			_player.AimingGun = false;
			Vector3i vector3i = tileEntityLootContainer.ToWorldPos();
			tileEntityLootContainer.bWasTouched = tileEntityLootContainer.bTouched;
			_world.GetGameManager().TELockServer(_clrIdx, vector3i, tileEntityLootContainer.entityId, _player.entityId, null);
			return true;
		}
		bool flag = this.CanPickup;
		Block block = _blockValue.Block;
		if (EffectManager.GetValue(PassiveEffects.BlockPickup, null, 0f, _player, null, block.Tags, true, true, true, true, 1, true, false) > 0f)
		{
			flag = true;
		}
		if (!flag)
		{
			return false;
		}
		if (!_world.CanPickupBlockAt(_blockPos, _world.GetGameManager().GetPersistentLocalPlayer()))
		{
			_player.PlayOneShot("keystone_impact_overlay", false);
			return false;
		}
		if (_blockValue.damage > 0)
		{
			GameManager.ShowTooltip(_player as EntityPlayerLocal, Localization.Get("ttRepairBeforePickup"), string.Empty, "ui_denied", null);
			return false;
		}
		ItemStack itemStack = block.OnBlockPickedUp(_world, _clrIdx, _blockPos, _blockValue, _player.entityId);
		if (!_player.inventory.CanTakeItem(itemStack) && !_player.bag.CanTakeItem(itemStack))
		{
			GameManager.ShowTooltip(_player as EntityPlayerLocal, Localization.Get("xuiInventoryFullForPickup"), string.Empty, "ui_denied", null);
			return false;
		}
		QuestEventManager.Current.BlockPickedUp(block.GetBlockName(), _blockPos);
		QuestEventManager.Current.ItemAdded(itemStack);
		_world.GetGameManager().PickupBlockServer(_clrIdx, _blockPos, _blockValue, _player.entityId, null);
		return false;
	}

	public virtual bool OnEntityCollidedWithBlock(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, Entity _entity)
	{
		return false;
	}

	public virtual void OnEntityWalking(WorldBase _world, int _x, int _y, int _z, BlockValue _blockValue, Entity entity)
	{
	}

	public virtual bool CanPlantStay(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
	{
		return true;
	}

	public void SetBlockName(string _blockName)
	{
		this.blockName = _blockName;
	}

	public string GetBlockName()
	{
		return this.blockName;
	}

	public EAutoShapeType GetAutoShapeType()
	{
		return this.AutoShapeType;
	}

	public string GetAutoShapeBlockName()
	{
		return this.autoShapeBaseName;
	}

	public string GetAutoShapeShapeName()
	{
		return this.autoShapeShapeName;
	}

	public Block GetAutoShapeHelperBlock()
	{
		return this.autoShapeHelper;
	}

	public string GetLocalizedAutoShapeShapeName()
	{
		return Localization.Get("shape" + this.GetAutoShapeShapeName());
	}

	public bool AutoShapeSupportsShapeName(string _shapeName)
	{
		return this.AutoShapeType == EAutoShapeType.Helper && this.ContainsAlternateBlock(this.autoShapeBaseName + ":" + _shapeName);
	}

	public int AutoShapeAlternateShapeNameIndex(string _shapeName)
	{
		if (this.AutoShapeType == EAutoShapeType.Helper)
		{
			return this.GetAlternateBlockIndex(this.autoShapeBaseName + ":" + _shapeName);
		}
		return -1;
	}

	public virtual string GetLocalizedBlockName()
	{
		if (this.localizedBlockName != null)
		{
			return this.localizedBlockName;
		}
		if (this.AutoShapeType != EAutoShapeType.None)
		{
			return this.localizedBlockName = this.blockMaterial.GetLocalizedMaterialName() + " - " + this.GetLocalizedAutoShapeShapeName();
		}
		return this.localizedBlockName = Localization.Get(this.GetBlockName());
	}

	public virtual string GetLocalizedBlockName(ItemValue _itemValueRef)
	{
		if (this.AutoShapeType != EAutoShapeType.Helper || _itemValueRef.ToBlockValue().Equals(BlockValue.Air))
		{
			return this.GetLocalizedBlockName();
		}
		this.GetAltBlocks();
		return this.placeAltBlockClasses[_itemValueRef.Meta].GetLocalizedBlockName();
	}

	public string GetIconName()
	{
		return this.CustomIcon ?? this.GetBlockName();
	}

	public void SetSideTextureId(int _textureId)
	{
		this.singleTextureId = _textureId;
		this.bTextureForEachSide = false;
	}

	public void SetSideTextureId(string[] _texIds)
	{
		this.sideTextureIds = new int[_texIds.Length];
		for (int i = 0; i < _texIds.Length; i++)
		{
			this.sideTextureIds[i] = int.Parse(_texIds[i]);
		}
		this.bTextureForEachSide = true;
	}

	public int GetSideTextureId(BlockValue _blockValue, BlockFace _side)
	{
		if (this.bTextureForEachSide)
		{
			int num = this.shape.MapSideAndRotationToTextureIdx(_blockValue, _side);
			if (num >= this.sideTextureIds.Length)
			{
				num = 0;
			}
			return this.sideTextureIds[num];
		}
		return this.singleTextureId;
	}

	public MaterialBlock GetMaterialForSide(BlockValue _blockValue, BlockFace _side)
	{
		MaterialBlock materialBlock = null;
		int sideTextureId = this.GetSideTextureId(_blockValue, _side);
		Block block = _blockValue.Block;
		if (sideTextureId != -1 && MeshDescription.meshes[(int)block.MeshIndex].textureAtlas.uvMapping.Length > sideTextureId)
		{
			materialBlock = MeshDescription.meshes[(int)block.MeshIndex].textureAtlas.uvMapping[sideTextureId].material;
		}
		if (materialBlock == null)
		{
			materialBlock = block.blockMaterial;
		}
		return materialBlock;
	}

	public int GetUiBackgroundTextureId(BlockValue _blockValue, BlockFace _side)
	{
		if (this.uiBackgroundTextureId < 0)
		{
			return this.GetSideTextureId(_blockValue, _side);
		}
		return this.uiBackgroundTextureId;
	}

	public string GetParticleForSide(BlockValue _blockValue, BlockFace _side)
	{
		MaterialBlock materialForSide = this.GetMaterialForSide(_blockValue, _side);
		if (materialForSide != null && materialForSide.ParticleCategory != null)
		{
			return materialForSide.ParticleCategory;
		}
		if (materialForSide != null && materialForSide.SurfaceCategory != null)
		{
			return materialForSide.SurfaceCategory;
		}
		return null;
	}

	public string GetDestroyParticle(BlockValue _blockValue)
	{
		if (this.blockMaterial.ParticleDestroyCategory != null)
		{
			return this.blockMaterial.ParticleDestroyCategory;
		}
		if (this.blockMaterial.ParticleCategory != null)
		{
			return this.blockMaterial.ParticleCategory;
		}
		if (this.blockMaterial.SurfaceCategory != null)
		{
			return this.blockMaterial.SurfaceCategory;
		}
		return null;
	}

	public Color GetColorForSide(BlockValue _blockValue, BlockFace _side)
	{
		TextureAtlas textureAtlas = MeshDescription.meshes[(int)_blockValue.Block.MeshIndex].textureAtlas;
		int sideTextureId = this.GetSideTextureId(_blockValue, _side);
		if (sideTextureId != -1 && textureAtlas.uvMapping.Length > sideTextureId)
		{
			return textureAtlas.uvMapping[sideTextureId].color;
		}
		return Color.gray;
	}

	public Color GetMapColor(BlockValue _blockValue, Vector3 _normal, int _yPos)
	{
		Color color;
		if (!this.bMapColorSet)
		{
			if (_normal.x > 0.5f || _normal.z > 0.5f || _normal.x < -0.5f || _normal.z < -0.5f)
			{
				color = this.GetColorForSide(_blockValue, BlockFace.South);
			}
			else
			{
				color = this.GetColorForSide(_blockValue, BlockFace.Top);
			}
		}
		else
		{
			color = this.MapColor;
		}
		float num = this.MapSpecular;
		if (this.bMapColor2Set && this.MapElevMinMax.y != this.MapElevMinMax.x)
		{
			float num2 = (float)Utils.FastMax(_yPos - this.MapElevMinMax.x, 0) / (float)(this.MapElevMinMax.y - this.MapElevMinMax.x);
			color = Color.Lerp(this.MapColor, this.MapColor2, num2);
			num = Utils.FastMax(num - num2 * 0.5f, 0f);
		}
		float num3 = (_normal.z + 1f) / 2f * (_normal.x + 1f) / 2f;
		num3 *= 2f;
		color = Utils.Saturate(color * 0.5f + color * num3);
		color.a = num;
		return color;
	}

	public static bool CanDrop(BlockValue _blockValue)
	{
		return !_blockValue.Equals(BlockValue.Air);
	}

	public virtual bool IsElevator()
	{
		return false;
	}

	public virtual bool IsElevator(int rotation)
	{
		return false;
	}

	public virtual bool IsPlant()
	{
		return this.blockMaterial.IsPlant || this.bIsPlant;
	}

	public bool HasTag(BlockTags _tag)
	{
		return this.BlockTag == _tag;
	}

	public bool HasAnyFastTags(FastTags _tags)
	{
		return this.Tags.Test_AnySet(_tags);
	}

	public bool HasAllFastTags(FastTags _tags)
	{
		return this.Tags.Test_AllSet(_tags);
	}

	public virtual bool CanRepair(BlockValue _blockValue)
	{
		return _blockValue.damage > 0;
	}

	public virtual string GetActivationText(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
	{
		Block block = _blockValue.Block;
		TileEntityLootContainer tileEntityLootContainer = _world.GetTileEntity(_clrIdx, _blockPos) as TileEntityLootContainer;
		if (tileEntityLootContainer != null)
		{
			string text = block.GetLocalizedBlockName();
			PlayerActionsLocal playerInput = ((EntityPlayerLocal)_entityFocusing).playerInput;
			string text2 = playerInput.Activate.GetBindingXuiMarkupString(XUiUtils.EmptyBindingStyle.EmptyString, XUiUtils.DisplayStyle.Plain, null) + playerInput.PermanentActions.Activate.GetBindingXuiMarkupString(XUiUtils.EmptyBindingStyle.EmptyString, XUiUtils.DisplayStyle.Plain, null);
			if (!tileEntityLootContainer.bTouched)
			{
				return string.Format(Localization.Get("lootTooltipNew"), text2, text);
			}
			if (tileEntityLootContainer.IsEmpty())
			{
				return string.Format(Localization.Get("lootTooltipEmpty"), text2, text);
			}
			return string.Format(Localization.Get("lootTooltipTouched"), text2, text);
		}
		else
		{
			if (!this.CanPickup && EffectManager.GetValue(PassiveEffects.BlockPickup, null, 0f, _entityFocusing, null, _blockValue.Block.Tags, true, true, true, true, 1, true, false) <= 0f)
			{
				return null;
			}
			if (!_world.CanPickupBlockAt(_blockPos, _world.GetGameManager().GetPersistentLocalPlayer()))
			{
				return null;
			}
			string text3 = block.GetBlockName();
			if (!string.IsNullOrEmpty(block.PickedUpItemValue))
			{
				text3 = block.PickedUpItemValue;
			}
			else if (!string.IsNullOrEmpty(block.PickupTarget))
			{
				text3 = block.PickupTarget;
			}
			return string.Format(Localization.Get("pickupPrompt"), Localization.Get(text3));
		}
	}

	public void SpawnDowngradeFX(WorldBase _world, BlockValue _blockValue, Vector3i _blockPos, Color _color, int _entityIdThatCaused)
	{
		Block block = _blockValue.Block;
		if (block.DowngradeFX != null)
		{
			this.SpawnFX(_world, _blockPos, 1f, _color, _entityIdThatCaused, block.DowngradeFX);
			return;
		}
		this.SpawnDestroyParticleEffect(_world, _blockValue, _blockPos, 1f, _color, _entityIdThatCaused);
	}

	public void SpawnDestroyFX(WorldBase _world, BlockValue _blockValue, Vector3i _blockPos, Color _color, int _entityIdThatCaused)
	{
		Block block = _blockValue.Block;
		if (block.DestroyFX != null)
		{
			this.SpawnFX(_world, _blockPos, 1f, _color, _entityIdThatCaused, block.DestroyFX);
			return;
		}
		this.SpawnDestroyParticleEffect(_world, _blockValue, _blockPos, 1f, _color, _entityIdThatCaused);
	}

	public virtual void SpawnDestroyParticleEffect(WorldBase _world, BlockValue _blockValue, Vector3i _blockPos, float _lightValue, Color _color, int _entityIdThatCaused)
	{
		if (this.deathParticleName != null)
		{
			_world.GetGameManager().SpawnParticleEffectServer(new ParticleEffect(this.deathParticleName, World.blockToTransformPos(_blockPos) + new Vector3(0f, 0.5f, 0f), _lightValue, _color, this.blockMaterial.SurfaceCategory + "destroy", null, true), _entityIdThatCaused, false, false);
			return;
		}
		MaterialBlock materialForSide = this.GetMaterialForSide(_blockValue, BlockFace.Top);
		string destroyParticle = this.GetDestroyParticle(_blockValue);
		if (destroyParticle != null && materialForSide.SurfaceCategory != null)
		{
			_world.GetGameManager().SpawnParticleEffectServer(new ParticleEffect("blockdestroy_" + destroyParticle, World.blockToTransformPos(_blockPos) + new Vector3(0f, 0.5f, 0f), _lightValue, _color, this.blockMaterial.SurfaceCategory + "destroy", null, true), _entityIdThatCaused, true, true);
		}
	}

	public void SpawnFX(WorldBase _world, Vector3i _blockPos, float _lightValue, Color _color, int _entityIdThatCaused, string _fxName)
	{
		string[] array = _fxName.Split(',', StringSplitOptions.None);
		_world.GetGameManager().SpawnParticleEffectServer(new ParticleEffect(array[0], World.blockToTransformPos(_blockPos) + new Vector3(0f, 0.5f, 0f), _lightValue, _color, array[1], null, true), _entityIdThatCaused, false, false);
	}

	public static BlockValue GetBlockValue(string _blockName, bool _caseInsensitive = false)
	{
		Block blockByName = Block.GetBlockByName(_blockName, _caseInsensitive);
		if (blockByName != null)
		{
			return blockByName.ToBlockValue();
		}
		return BlockValue.Air;
	}

	public static Block GetBlockByName(string _blockname, bool _caseInsensitive = false)
	{
		if (Block.nameToBlock == null)
		{
			return null;
		}
		Block block;
		if (_caseInsensitive)
		{
			Block.nameToBlockCaseInsensitive.TryGetValue(_blockname, out block);
		}
		else
		{
			Block.nameToBlock.TryGetValue(_blockname, out block);
		}
		return block;
	}

	public BlockValue ToBlockValue()
	{
		return new BlockValue
		{
			type = this.blockID
		};
	}

	public static BlockValue GetBlockValue(int _blockType)
	{
		if (Block.list[_blockType] == null)
		{
			return BlockValue.Air;
		}
		return new BlockValue
		{
			type = _blockType
		};
	}

	public BlockValue GetBlockValueFromProperty(string _propValue)
	{
		BlockValue blockValue = BlockValue.Air;
		if (!this.Properties.Values.ContainsKey(_propValue))
		{
			throw new Exception("You need to specify a property with name '" + _propValue + "' for the block " + this.blockName);
		}
		blockValue = Block.GetBlockValue(this.Properties.Values[_propValue], false);
		if (blockValue.Equals(BlockValue.Air))
		{
			throw new Exception("Block with name '" + this.Properties.Values[_propValue] + "' not found!");
		}
		return blockValue;
	}

	public virtual bool ShowModelOnFall()
	{
		return this.bShowModelOnFall;
	}

	public virtual bool HasBlockActivationCommands(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
	{
		bool flag = _world.GetTileEntity(_clrIdx, _blockPos) is TileEntityLootContainer;
		bool flag2 = this.CanPickup;
		if (EffectManager.GetValue(PassiveEffects.BlockPickup, null, 0f, _entityFocusing, null, _blockValue.Block.Tags, true, true, true, true, 1, true, false) > 0f)
		{
			flag2 = true;
		}
		return flag2 || flag;
	}

	public virtual BlockActivationCommand[] GetBlockActivationCommands(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
	{
		bool flag = _world.GetTileEntity(_clrIdx, _blockPos) is TileEntityLootContainer;
		bool flag2 = false;
		bool flag3 = this.CanPickup;
		if (EffectManager.GetValue(PassiveEffects.BlockPickup, null, 0f, _entityFocusing, null, _blockValue.Block.Tags, true, true, true, true, 1, true, false) > 0f)
		{
			flag3 = true;
		}
		if (flag3)
		{
			this.cmds[0].enabled = true;
			flag2 = true;
		}
		if (flag)
		{
			this.cmds[1].enabled = true;
			flag2 = true;
		}
		if (!flag2)
		{
			return Array.Empty<BlockActivationCommand>();
		}
		return this.cmds;
	}

	public virtual bool OnBlockActivated(string _commandName, WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue, EntityAlive _player)
	{
		if (_commandName == this.cmds[0].text || _commandName == this.cmds[1].text)
		{
			this.OnBlockActivated(_world, _cIdx, _blockPos, _blockValue, _player);
			return true;
		}
		return false;
	}

	public virtual void RenderDecorations(Vector3i _worldPos, BlockValue _blockValue, Vector3 _drawPos, Vector3[] _vertices, LightingAround _lightingAround, long _fulltexture, VoxelMesh[] _meshes, INeighborBlockCache _nBlocks)
	{
		this.shape.renderDecorations(_worldPos, _blockValue, _drawPos, _vertices, _lightingAround, _fulltexture, _meshes, _nBlocks);
	}

	public virtual bool IsExplosionAffected()
	{
		return true;
	}

	public int GetActivationDistanceSq()
	{
		int num = this.activationDistance;
		if (num == 0)
		{
			return (int)(Constants.cCollectItemDistance * Constants.cCollectItemDistance);
		}
		return num * num;
	}

	public int GetPlacementDistanceSq()
	{
		int num = this.placementDistance;
		if (num == 0)
		{
			num = this.activationDistance;
		}
		if (num == 0)
		{
			return (int)(Constants.cDigAndBuildDistance * Constants.cDigAndBuildDistance);
		}
		return num * num;
	}

	public virtual void CheckUpdate(BlockValue _oldBV, BlockValue _newBV, out bool bUpdateMesh, out bool bUpdateNotify, out bool bUpdateLight)
	{
		bUpdateMesh = (bUpdateNotify = (bUpdateLight = true));
	}

	public virtual bool RotateVerticesOnCollisionCheck(BlockValue _blockValue)
	{
		return true;
	}

	public virtual bool ActivateBlock(WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue, bool isOn, bool isPowered)
	{
		return false;
	}

	public virtual bool ActivateBlockOnce(WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue)
	{
		return false;
	}

	public virtual void OnTriggerAddedFromPrefab(BlockTrigger _trigger, Vector3i _blockPos, BlockValue _blockValue, FastTags _questTags)
	{
	}

	public virtual void OnTriggerRefresh(BlockTrigger _trigger, BlockValue _bv, FastTags questTag)
	{
	}

	public virtual void OnTriggerChanged(BlockTrigger _trigger, Chunk _chunk, Vector3i _blockPos, BlockValue _blockValue)
	{
	}

	public virtual void OnTriggerChanged(BlockTrigger _trigger, Chunk _chunk, Vector3i _blockPos, BlockValue _blockValue, List<BlockChangeInfo> _blockChanges)
	{
	}

	public virtual void OnTriggered(EntityPlayer _player, WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue, List<BlockChangeInfo> _blockChanges)
	{
	}

	public void HandleTrigger(EntityPlayer _player, World _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue)
	{
		if (SingletonMonoBehaviour<ConnectionManager>.Instance.IsClient)
		{
			SingletonMonoBehaviour<ConnectionManager>.Instance.SendToServer(NetPackageManager.GetPackage<NetPackageBlockTrigger>().Setup(_cIdx, _blockPos, _blockValue), false);
			return;
		}
		BlockTrigger blockTrigger = ((Chunk)_world.ChunkClusters[_cIdx].GetChunkSync(World.toChunkXZ(_blockPos.x), _blockPos.y, World.toChunkXZ(_blockPos.z))).GetBlockTrigger(World.toBlock(_blockPos));
		if (blockTrigger != null)
		{
			_world.triggerManager.TriggerBlocks(_player, _player.prefab, blockTrigger);
		}
	}

	public override string ToString()
	{
		return this.blockName;
	}

	private static void assignIdsLinear()
	{
		bool[] array = new bool[Block.MAX_BLOCKS];
		List<Block> list = new List<Block>(Block.nameToBlock.Count);
		Block.nameToBlock.CopyValuesTo(list);
		Block.assignLeftOverBlocks(array, list);
	}

	private static void assignId(Block _b, int _id, bool[] _usedIds)
	{
		Block.list[_id] = _b;
		_b.blockID = _id;
		_usedIds[_id] = true;
	}

	private static void assignLeftOverBlocks(bool[] _usedIds, List<Block> _unassignedBlocks)
	{
		foreach (KeyValuePair<string, int> keyValuePair in Block.fixedBlockIds)
		{
			if (Block.nameToBlock.ContainsKey(keyValuePair.Key))
			{
				Block block = Block.nameToBlock[keyValuePair.Key];
				if (_unassignedBlocks.Contains(block))
				{
					_unassignedBlocks.Remove(block);
					Block.assignId(block, keyValuePair.Value, _usedIds);
				}
			}
		}
		int num = 0;
		int num2 = 255;
		foreach (Block block2 in _unassignedBlocks)
		{
			if (block2.shape.IsTerrain())
			{
				while (_usedIds[++num])
				{
				}
				Block.assignId(block2, num, _usedIds);
			}
			else
			{
				while (_usedIds[++num2])
				{
				}
				Block.assignId(block2, num2, _usedIds);
			}
		}
		Log.Out("Block IDs total {0}, terr {1}, last {2}", new object[]
		{
			Block.nameToBlock.Count,
			num,
			num2
		});
	}

	private static void assignIdsFromMapping()
	{
		List<Block> list = new List<Block>();
		bool[] array = new bool[Block.MAX_BLOCKS];
		foreach (KeyValuePair<string, Block> keyValuePair in Block.nameToBlock)
		{
			int idForName = Block.nameIdMapping.GetIdForName(keyValuePair.Key);
			if (idForName >= 0)
			{
				Block.assignId(keyValuePair.Value, idForName, array);
			}
			else
			{
				list.Add(keyValuePair.Value);
			}
		}
		Block.assignLeftOverBlocks(array, list);
	}

	private static void createFullMappingForClients()
	{
		NameIdMapping nameIdMapping = new NameIdMapping(null, Block.MAX_BLOCKS);
		foreach (KeyValuePair<string, Block> keyValuePair in Block.nameToBlock)
		{
			nameIdMapping.AddMapping(keyValuePair.Value.blockID, keyValuePair.Key, false);
		}
		Block.fullMappingDataForClients = nameIdMapping.SaveToArray();
	}

	public static void AssignIds()
	{
		if (Block.nameIdMapping != null)
		{
			Log.Out("Block IDs with mapping");
			Block.assignIdsFromMapping();
		}
		else
		{
			Log.Out("Block IDs withOUT mapping");
			Block.assignIdsLinear();
		}
		Block.createFullMappingForClients();
	}

	public virtual bool IsTileEntitySavedInPrefab()
	{
		return false;
	}

	public virtual string GetCustomDescription(Vector3i _blockPos, BlockValue _bv)
	{
		return "";
	}

	public string GetPlaceAltBlockValue(WorldBase _world)
	{
		if (this.placeAltBlockNames != null && this.placeAltBlockNames.Length != 0)
		{
			return this.placeAltBlockNames[_world.GetGameRandom().RandomRange(0, this.placeAltBlockNames.Length)];
		}
		return string.Empty;
	}

	public Block GetAltBlock(int _typeId)
	{
		this.GetAltBlocks();
		if (this.placeAltBlockClasses != null && this.placeAltBlockClasses.Length != 0)
		{
			return this.placeAltBlockClasses[_typeId];
		}
		return Block.list[0];
	}

	public BlockValue GetAltBlockValue(int typeID)
	{
		return this.GetAltBlock(typeID).ToBlockValue();
	}

	public string[] GetAltBlockNames()
	{
		return this.placeAltBlockNames;
	}

	public Block[] GetAltBlocks()
	{
		if (this.placeAltBlockClasses == null && this.placeAltBlockNames != null)
		{
			this.placeAltBlockClasses = new Block[this.placeAltBlockNames.Length];
			for (int i = 0; i < this.placeAltBlockNames.Length; i++)
			{
				this.placeAltBlockClasses[i] = Block.GetBlockByName(this.placeAltBlockNames[i], false);
			}
		}
		return this.placeAltBlockClasses;
	}

	public int AlternateBlockCount()
	{
		return this.placeAltBlockNames.Length;
	}

	public bool ContainsAlternateBlock(string block)
	{
		for (int i = 0; i < this.placeAltBlockNames.Length; i++)
		{
			if (this.placeAltBlockNames[i] == block)
			{
				return true;
			}
		}
		return false;
	}

	public int GetAlternateBlockIndex(string block)
	{
		for (int i = 0; i < this.placeAltBlockNames.Length; i++)
		{
			if (this.placeAltBlockNames[i] == block)
			{
				return i;
			}
		}
		return -1;
	}

	public static void GetShapeCategories(IEnumerable<Block> _altBlocks, List<ShapesFromXml.ShapeCategory> _targetList)
	{
		_targetList.Clear();
		foreach (Block block in _altBlocks)
		{
			if (block.ShapeCategories != null)
			{
				foreach (ShapesFromXml.ShapeCategory shapeCategory in block.ShapeCategories)
				{
					if (!_targetList.Contains(shapeCategory))
					{
						_targetList.Add(shapeCategory);
					}
				}
			}
		}
		_targetList.Sort();
	}

	public int GetShownMaxDamage()
	{
		if (this is BlockDoor)
		{
			return this.MaxDamagePlusDowngrades;
		}
		return this.MaxDamage;
	}

	public bool SupportsRotation(byte _rotation)
	{
		if (_rotation < 4)
		{
			return (this.AllowedRotations & EBlockRotationClasses.Basic90) > EBlockRotationClasses.None;
		}
		if (_rotation < 8)
		{
			return (this.AllowedRotations & EBlockRotationClasses.Headfirst) > EBlockRotationClasses.None;
		}
		if (_rotation < 24)
		{
			return (this.AllowedRotations & EBlockRotationClasses.Sideways) > EBlockRotationClasses.None;
		}
		return (this.AllowedRotations & EBlockRotationClasses.Basic45) > EBlockRotationClasses.None;
	}

	public void RotateHoldingBlock(ItemClassBlock.ItemBlockInventoryData _blockInventoryData, bool _increaseRotation, bool _playSoundOnRotation = true)
	{
		if (_blockInventoryData.mode == BlockPlacement.EnumRotationMode.Auto)
		{
			_blockInventoryData.mode = BlockPlacement.EnumRotationMode.Simple;
		}
		BlockValue blockValue = _blockInventoryData.itemValue.ToBlockValue();
		blockValue.rotation = _blockInventoryData.rotation;
		blockValue = this.BlockPlacementHelper.OnPlaceBlock(_blockInventoryData.mode, _blockInventoryData.localRot, _blockInventoryData.world, blockValue, _blockInventoryData.hitInfo.hit, _blockInventoryData.holdingEntity.position).blockValue;
		int rotation = (int)_blockInventoryData.rotation;
		_blockInventoryData.rotation = this.BlockPlacementHelper.LimitRotation(_blockInventoryData.mode, ref _blockInventoryData.localRot, _blockInventoryData.hitInfo.hit, _increaseRotation, blockValue, blockValue.rotation);
		if (_playSoundOnRotation && rotation != (int)_blockInventoryData.rotation)
		{
			_blockInventoryData.holdingEntity.PlayOneShot("rotateblock", false);
		}
	}

	public void GroundAlign(BlockEntityData _data)
	{
		if (!_data.bHasTransform)
		{
			return;
		}
		BlockValue blockValue = _data.blockValue;
		int type = blockValue.type;
		Transform transform = _data.transform;
		GameObject gameObject = transform.gameObject;
		gameObject.SetActive(false);
		Vector3 vector = Vector3.zero;
		int num = 0;
		Ray ray = new Ray(Vector3.zero, Vector3.down);
		Vector3 vector2 = new Vector3(0.5f, 0.75f, 0.5f) - Origin.position;
		float num2 = this.GroundAlignDistance + 0.5f;
		Vector3i pos = _data.pos;
		Vector3 vector3 = transform.position;
		Vector3 vector4;
		if (!this.isMultiBlock)
		{
			vector4 = new Vector3(0f, float.MinValue, 0f);
			ray.origin = pos.ToVector3() + vector2;
			RaycastHit raycastHit;
			bool flag = Physics.SphereCast(ray, 0.22f, out raycastHit, num2 - 0.22f + 0.25f, 1082195968);
			if (!flag)
			{
				flag = Physics.SphereCast(ray, 0.48f, out raycastHit, num2 - 0.48f + 0.25f, 1082195968);
			}
			if (flag)
			{
				vector4 = raycastHit.point;
				vector = raycastHit.normal;
				num = 1;
			}
		}
		else
		{
			if (blockValue.ischild)
			{
				pos = new Vector3i(blockValue.parentx, blockValue.parenty, blockValue.parentz);
			}
			vector4 = vector3;
			vector4.y = float.MinValue;
			byte rotation = blockValue.rotation;
			for (int i = this.multiBlockPos.Length - 1; i >= 0; i--)
			{
				Vector3i vector3i = this.multiBlockPos.Get(i, type, (int)rotation);
				if (vector3i.y == 0)
				{
					ray.origin = (pos + vector3i).ToVector3() + vector2;
					RaycastHit raycastHit;
					if (Physics.SphereCast(ray, 0.22f, out raycastHit, num2 - 0.22f + 0.25f, 1082195968))
					{
						if (vector4.y < raycastHit.point.y)
						{
							vector4.y = raycastHit.point.y;
						}
						vector += raycastHit.normal;
						num++;
					}
				}
			}
			if (num > 0)
			{
				vector *= 1f / (float)num;
				vector.Normalize();
			}
		}
		if (num > 0)
		{
			vector3 = vector4;
			Quaternion quaternion = transform.rotation;
			quaternion = Quaternion.FromToRotation(Vector3.up, vector) * quaternion;
			transform.SetPositionAndRotation(vector3, quaternion);
		}
		gameObject.SetActive(true);
	}

	public const int cAirId = 0;

	public const int cTerrainStartId = 1;

	public const int cGeneralStartId = 256;

	public const int cWaterId = 32512;

	public const int cWaterPOIId = 32513;

	public const int cWaterDataId = 32514;

	public static int MAX_BLOCKS = 32768;

	public static int ItemsStartHere = Block.MAX_BLOCKS;

	public static bool FallInstantly = false;

	public const int BlockFaceDrawn_Top = 1;

	public const int BlockFaceDrawn_Bottom = 2;

	public const int BlockFaceDrawn_North = 4;

	public const int BlockFaceDrawn_West = 8;

	public const int BlockFaceDrawn_South = 16;

	public const int BlockFaceDrawn_East = 32;

	public const int BlockFaceDrawn_AllORD = 63;

	public const int BlockFaceDrawn_All = 255;

	public static float cWaterLevel = 62.88f;

	public static string PropCanPickup = "CanPickup";

	protected static string PropPickupTarget = "PickupTarget";

	protected static string PropPickupSource = "PickupSource";

	public static string PropPlaceAltBlockValue = "PlaceAltBlockValue";

	protected static string PropPlaceShapeCategories = "ShapeCategories";

	public static string PropSiblingBlock = "SiblingBlock";

	protected static string PropFuelValue = "FuelValue";

	protected static string PropWeight = "Weight";

	protected static string PropCanMobsSpawnOn = "CanMobsSpawnOn";

	protected static string PropCanPlayersSpawnOn = "CanPlayersSpawnOn";

	protected static string PropIndexName = "IndexName";

	protected static string PropCanDecorateOnSlopes = "CanDecorateOnSlopes";

	protected static string PropSlopeMax = "SlopeMax";

	protected static string PropIsTerrainDecoration = "IsTerrainDecoration";

	protected static string PropIsDecoration = "IsDecoration";

	protected static string PropDistantDecoration = "IsDistantDecoration";

	protected static string PropBigDecorationRadius = "BigDecorationRadius";

	protected static string PropSmallDecorationRadius = "SmallDecorationRadius";

	protected static string PropGndAlign = "GndAlign";

	protected static string PropIgnoreKeystoneOverlay = "IgnoreKeystoneOverlay";

	public static string PropUpgradeBlockClass = "UpgradeBlock";

	public static string PropUpgradeBlockClassToBlock = Block.PropUpgradeBlockClass + ".ToBlock";

	public static string PropUpgradeBlockItemCount = "ItemCount";

	public static string PropUpgradeBlockClassItemCount = Block.PropUpgradeBlockClass + ".ItemCount";

	public static string PropDowngradeBlock = "DowngradeBlock";

	public static string PropLockpickDowngradeBlock = "LockPickDowngradeBlock";

	protected static string PropLPScale = "LPHardnessScale";

	protected static string PropMapColor = "Map.Color";

	protected static string PropMapColor2 = "Map.Color2";

	protected static string PropMapSpecular = "Map.Specular";

	protected static string PropMapElevMinMax = "Map.ElevMinMax";

	protected static string PropGroupName = "Group";

	public static string PropCustomIcon = "CustomIcon";

	protected static string PropCustomIconTint = "CustomIconTint";

	protected static string PropPlacementWireframe = "PlacementWireframe";

	protected static string PropMultiBlockDim = "MultiBlockDim";

	protected static string PropMultiBlockLayer = "MultiBlockLayer";

	protected static string PropIsPlant = "IsPlant";

	protected static string PropHeatMapStrength = "HeatMapStrength";

	protected static string PropHeatMapTime = "HeatMapTime";

	protected static string PropHeatMapFrequency = "HeatMapFrequency";

	protected static string PropFallDamage = "FallDamage";

	protected static string PropBuffsWhenWalkedOn = "BuffsWhenWalkedOn";

	protected static string PropRadiusEffect = "ActiveRadiusEffects";

	protected static string PropCount = "Count";

	protected static string PropAllowAllRotations = "AllowAllRotations";

	protected static string PropActivationDistance = "ActivationDistance";

	protected static string PropPlacementDistance = "PlacementDistance";

	protected static string PropIsReplaceRandom = "IsReplaceRandom";

	protected static string PropCraftExpValue = "CraftComponentExpValue";

	protected static string PropCraftTimeValue = "CraftComponentTimeValue";

	protected static string PropLootExpValue = "LootExpValue";

	protected static string PropDestroyExpValue = "DestroyExpValue";

	protected static string PropParticleOnDeath = "ParticleOnDeath";

	protected static string PropPlaceExpValue = "PlaceExpValue";

	protected static string PropUpgradeExpValue = "UpgradeExpValue";

	protected static string PropEconomicValue = "EconomicValue";

	protected static string PropEconomicSellScale = "EconomicSellScale";

	protected static string PropEconomicBundleSize = "EconomicBundleSize";

	protected static string PropSellableToTrader = "SellableToTrader";

	protected static string PropTraderStageTemplate = "TraderStageTemplate";

	public static string PropPickupJournalEntry = "PickupJournalEntry";

	public static string PropResourceScale = "ResourceScale";

	public static string PropMaxDamage = "MaxDamage";

	protected static string PropStartDamage = "StartDamage";

	protected static string PropStage2Health = "Stage2Health";

	protected static string PropDamage = "Damage";

	public static string PropDescriptionKey = "DescriptionKey";

	protected static string PropActionSkillGroup = "ActionSkillGroup";

	protected static string PropCraftingSkillGroup = "CraftingSkillGroup";

	protected static string PropShowModelOnFall = "ShowModelOnFall";

	protected static string PropLightOpacity = "LightOpacity";

	protected static string PropHarvestOverdamage = "HarvestOverdamage";

	public static string PropTintColor = "TintColor";

	public static string PropRandomTintColor = "RandomTintColor";

	public static string PropCreativeMode = "CreativeMode";

	public static string PropFilterTag = "FilterTags";

	public static string PropTag = "Tags";

	public static string PropCreativeSort1 = "SortOrder1";

	public static string PropCreativeSort2 = "SortOrder2";

	public static string PropDisplayType = "DisplayType";

	public static string PropUnlockedBy = "UnlockedBy";

	public static string PropNoScrapping = "NoScrapping";

	public static string PropVehicleHitScale = "VehicleHitScale";

	public static string PropItemTypeIcon = "ItemTypeIcon";

	public static string PropAutoShape = "AutoShape";

	public static string PropBlockAddedEvent = "AddedEvent";

	public static string PropBlockDowngradeEvent = "DowngradeEvent";

	public static string PropBlockDowngradedToEvent = "DowngradedToEvent";

	public static NameIdMapping nameIdMapping;

	public static byte[] fullMappingDataForClients;

	private static Dictionary<string, Block> nameToBlock;

	private static Dictionary<string, Block> nameToBlockCaseInsensitive;

	public static Block[] list;

	private static string[] propRandomTintColorNames;

	public static string defaultBlockDescriptionKey = "";

	public int blockID;

	public DynamicProperties Properties;

	public BlockShape shape;

	public int BlockingType;

	public BlockValue SiblingBlock;

	public BlockTags BlockTag;

	public BlockPlacement BlockPlacementHelper;

	public float LPHardnessScale;

	public float MovementFactor;

	public bool CanPickup;

	public string PickedUpItemValue;

	public string PickupTarget;

	public string PickupSource;

	public byte BlocksMovement;

	public int FuelValue;

	public DataItem<int> Weight;

	public bool CanMobsSpawnOn;

	public bool CanPlayersSpawnOn;

	public string IndexName;

	public bool CanDecorateOnSlopes;

	public float SlopeMaxCos;

	public bool IsTerrainDecoration;

	public bool IsDecoration;

	public bool IsDistantDecoration;

	public int BigDecorationRadius;

	public int SmallDecorationRadius;

	public float GroundAlignDistance;

	public bool IgnoreKeystoneOverlay;

	public const int cPathScan = -1;

	public const int cPathSolid = 1;

	public int PathType;

	public float PathOffsetX;

	public float PathOffsetZ;

	public BlockFaceFlag WaterFlowMask = BlockFaceFlag.All;

	public bool WaterClipEnabled;

	public Plane WaterClipPlane;

	public BlockValue DowngradeBlock;

	public BlockValue LockpickDowngradeBlock;

	public BlockValue UpgradeBlock;

	public string[] GroupNames = new string[] { "Decor/Miscellaneous" };

	public string CustomIcon;

	public Color CustomIconTint;

	public bool bHasPlacementWireframe;

	public bool bUserHidden;

	public float FallDamage;

	public float HeatMapStrength;

	public uint HeatMapTime;

	public uint HeatMapFrequency;

	public string[] BuffsWhenWalkedOn;

	public BlockRadiusEffect[] RadiusEffects;

	public string DescriptionKey;

	public string CraftingSkillGroup = "";

	public string ActionSkillGroup = "";

	public bool IsReplaceRandom = true;

	public float CraftComponentExp = 1f;

	public float CraftComponentTime = 1f;

	public float LootExp = 1f;

	public float DestroyExp = 1f;

	protected string deathParticleName;

	public float EconomicValue;

	public float EconomicSellScale = 1f;

	public int EconomicBundleSize = 1;

	public bool SellableToTrader = true;

	public string TraderStageTemplate;

	public float PlaceExp = 1f;

	public float UpgradeExp = 1f;

	public int Count = 1;

	public int Stacknumber = 500;

	public bool HarvestOverdamage;

	public bool SelectAlternates;

	public string PickupJournalEntry = string.Empty;

	public EnumCreativeMode CreativeMode;

	public string[] FilterTags;

	public bool NoScrapping;

	public string SortOrder;

	public string DisplayType = "defaultBlock";

	private RecipeUnlockData[] unlockedBy;

	public string ItemTypeIcon = "";

	private EAutoShapeType AutoShapeType;

	private string autoShapeBaseName;

	private string autoShapeShapeName;

	private Block autoShapeHelper;

	public float VehicleHitScale;

	private Color MapColor;

	private bool bMapColorSet;

	private Color MapColor2;

	private bool bMapColor2Set;

	private float MapSpecular;

	private Vector2i MapElevMinMax;

	private byte lightValue;

	public int lightOpacity;

	public Color tintColor = Color.clear;

	public Color defaultTintColor = Color.clear;

	public Vector3 tintColorV = Vector3.one;

	public List<Block.TintColorOnMaterial> randomTintColors;

	public byte MeshIndex;

	public List<Block.SItemNameCount> RepairItems;

	public List<Block.SItemNameCount> RepairItemsMeshDamage;

	public bool bRestrictSubmergedPlacement;

	protected string blockAddedEvent;

	protected string blockDowngradeEvent;

	protected string blockDowngradedToEvent;

	public bool isMultiBlock;

	public Block.MultiBlockArray multiBlockPos;

	public const int BT_All = 255;

	public const int BT_None = 0;

	public const int BT_Sight = 1;

	public const int BT_Movement = 2;

	public const int BT_Bullets = 4;

	public const int BT_Rockets = 8;

	public const int BT_Melee = 16;

	public const int BT_Arrows = 32;

	public bool IsCheckCollideWithEntity;

	private bool bTextureForEachSide;

	private int singleTextureId;

	private int[] sideTextureIds;

	private int uiBackgroundTextureId = -1;

	public int TerrainTAIndex = 1;

	private bool bNotifyOnLoadUnload;

	private bool bIsPlant;

	private bool bShowModelOnFall;

	public Dictionary<EnumDropEvent, List<Block.SItemDropProb>> itemsToDrop = new EnumDictionary<EnumDropEvent, List<Block.SItemDropProb>>();

	public bool IsSleeperBlock;

	public bool IsRandomlyTick;

	private string[] placeAltBlockNames;

	private Block[] placeAltBlockClasses;

	public MaterialBlock blockMaterial;

	public bool StabilitySupport = true;

	public bool StabilityIgnore;

	public bool StabilityFull;

	public sbyte Density;

	private string blockName;

	private string localizedBlockName;

	public float ResourceScale;

	public int MaxDamage;

	public int MaxDamagePlusDowngrades;

	public int StartDamage;

	private int Stage2Health;

	public float Damage;

	public EBlockRotationClasses AllowedRotations;

	public bool PlaceRandomRotation;

	public string CustomPlaceSound;

	public string UpgradeSound;

	public string DowngradeFX;

	public string DestroyFX;

	private int activationDistance;

	private int placementDistance;

	public int cUVModeBits = 2;

	public int cUVModeMask = 3;

	public int cUVModeSides = 7;

	private uint UVModesPerSide;

	public bool bImposterExclude;

	public bool bImposterExcludeAndStop;

	public int ImposterExchange;

	public byte ImposterExchangeTexIdx;

	public bool bImposterDontBlock;

	public int MergeIntoId;

	public int[] MergeIntoTexIds;

	public int MirrorSibling;

	protected static List<Bounds> staticList_IntersectRayWithBlockList = new List<Bounds>();

	public BlockFace HandleFace = BlockFace.None;

	public bool EnablePassThroughDamage;

	public List<BlockFace> RemovePaintOnDowngrade;

	public FastTags Tags;

	public bool HasTileEntity;

	public Block.EnumDisplayInfo DisplayInfo;

	private BlockActivationCommand[] cmds = new BlockActivationCommand[]
	{
		new BlockActivationCommand("take", "hand", false, false),
		new BlockActivationCommand("Search", "search", false, false)
	};

	private List<ItemStack> itemsDropped = new List<ItemStack>();

	private static readonly Dictionary<string, int> fixedBlockIds = new Dictionary<string, int>
	{
		{ "air", 0 },
		{ "water", 32512 },
		{ "terrWaterPOI", 32513 },
		{ "waterdata", 32514 }
	};

	public struct SItemDropProb
	{
		public SItemDropProb(string _name, int _minCount, int _maxCount, float _prob, float _resourceScale, float _stickChance, string _toolCategory, string _tag)
		{
			this.name = _name;
			this.minCount = _minCount;
			this.maxCount = _maxCount;
			this.prob = _prob;
			this.resourceScale = _resourceScale;
			this.stickChance = _stickChance;
			this.toolCategory = _toolCategory;
			this.tag = _tag;
		}

		public string name;

		public int minCount;

		public int maxCount;

		public float prob;

		public float resourceScale;

		public float stickChance;

		public string toolCategory;

		public string tag;
	}

	public struct TintColorOnMaterial
	{
		public string materialName;

		public Vector3 color;
	}

	public struct SItemNameCount
	{
		public string ItemName;

		public int Count;
	}

	public class MultiBlockArray
	{
		public MultiBlockArray(Vector3i _dim, List<Vector3i> _pos)
		{
			this.dim = _dim;
			this.pos = _pos.ToArray();
			this.Length = _pos.Count;
		}

		public Vector3i Get(int _idx, int _blockId, int _rotation)
		{
			Vector3 vector = Block.list[_blockId].shape.GetRotation(new BlockValue
			{
				type = _blockId,
				rotation = (byte)_rotation
			}) * this.pos[_idx].ToVector3();
			return new Vector3i(Mathf.RoundToInt(vector.x), Mathf.RoundToInt(vector.y), Mathf.RoundToInt(vector.z));
		}

		public Vector3i GetParentPos(Vector3i _childPos, BlockValue _blockValue)
		{
			return new Vector3i(_childPos.x + _blockValue.parentx, _childPos.y + _blockValue.parenty, _childPos.z + _blockValue.parentz);
		}

		public void AddChilds(WorldBase _world, Chunk _chunk, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
		{
			ChunkCluster chunkCluster = _world.ChunkClusters[_clrIdx];
			if (chunkCluster == null)
			{
				return;
			}
			byte rotation = _blockValue.rotation;
			for (int i = this.Length - 1; i >= 0; i--)
			{
				Vector3i vector3i = this.Get(i, _blockValue.type, (int)rotation);
				if (!(vector3i == Vector3i.zero))
				{
					Vector3i vector3i2 = _blockPos + vector3i;
					int num = World.toBlockXZ(vector3i2.x);
					int num2 = World.toBlockXZ(vector3i2.z);
					int y = vector3i2.y;
					if (y >= 0 && y < 254)
					{
						Chunk chunk = (Chunk)chunkCluster.GetChunkFromWorldPos(vector3i2);
						if (chunk == null)
						{
							long num3 = WorldChunkCache.MakeChunkKey(World.toChunkXZ(vector3i2.x), World.toChunkXZ(vector3i2.z));
							if (_chunk.Key == num3)
							{
								chunk = _chunk;
							}
						}
						if (chunk != null)
						{
							BlockValue block = chunk.GetBlock(num, y, num2);
							if (block.isair || !block.Block.shape.IsTerrain())
							{
								BlockValue blockValue = _blockValue;
								blockValue.ischild = true;
								blockValue.parentx = -vector3i.x;
								blockValue.parenty = -vector3i.y;
								blockValue.parentz = -vector3i.z;
								chunk.SetBlock(_world, num, y, num2, blockValue, false, false);
							}
						}
					}
				}
			}
		}

		public void RemoveChilds(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
		{
			ChunkCluster chunkCluster = _world.ChunkClusters[_clrIdx];
			if (chunkCluster == null)
			{
				return;
			}
			byte rotation = _blockValue.rotation;
			for (int i = this.Length - 1; i >= 0; i--)
			{
				Vector3i vector3i = this.Get(i, _blockValue.type, (int)rotation);
				if ((vector3i.x != 0 || vector3i.y != 0 || vector3i.z != 0) && chunkCluster.GetBlock(_blockPos + vector3i).type == _blockValue.type)
				{
					chunkCluster.SetBlock(_blockPos + vector3i, true, BlockValue.Air, true, MarchingCubes.DensityAir, false, false, false, true);
				}
			}
		}

		public void RemoveParentBlock(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
		{
			ChunkCluster chunkCluster = _world.ChunkClusters[_clrIdx];
			if (chunkCluster == null)
			{
				return;
			}
			Vector3i parentPos = this.GetParentPos(_blockPos, _blockValue);
			BlockValue block = chunkCluster.GetBlock(parentPos);
			if (!block.ischild && block.type == _blockValue.type)
			{
				chunkCluster.SetBlock(parentPos, BlockValue.Air, true, true);
			}
		}

		public bool ContainsPos(WorldBase _world, int _clrIdx, Vector3i _parentPos, BlockValue _blockValue, Vector3i _posToCheck)
		{
			if (_world.ChunkClusters[_clrIdx] == null)
			{
				return false;
			}
			byte rotation = _blockValue.rotation;
			for (int i = this.Length - 1; i >= 0; i--)
			{
				if (_parentPos + this.Get(i, _blockValue.type, (int)rotation) == _posToCheck)
				{
					return true;
				}
			}
			return false;
		}

		public int Length;

		public Vector3i dim;

		private Vector3i[] pos;
	}

	public enum UVMode : byte
	{
		Default,
		Global,
		Local
	}

	public enum EnumDisplayInfo
	{
		None,
		Name,
		Description,
		Custom
	}

	public enum DestroyedResult
	{
		Keep,
		Downgrade,
		Remove
	}
}
