using System;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ZenithLook.Common;

/// <summary>
/// 核心逻辑：把所有「武器」的贴图槽位换成天顶剑的贴图资源。
/// <para/>
/// 物品图标、手持外观、地上掉落、商店与箱子里的图标，全都通过
/// <see cref="TextureAssets.Item"/> 取图，所以只要在贴图加载完成后换掉索引，
/// 游戏里所有用到它的地方都会自动变成天顶剑的外观。
/// 这里只是把同一个已加载的资源引用复制过去，不额外占显存，也不需要自带 PNG。
/// </summary>
public class ZenithLookSystem : ModSystem
{
	// 记录被替换前的原贴图，用于还原（关掉配置、切换分类时使用）。
	private static Asset<Texture2D>[] _originals;

	// 上次套用时看到的物品总数；数字变了说明模组内容变了（例如刚进世界/重载），需要重套一次。
	private static int _appliedCount = -1;

	private static bool _contentReady;

	public override void PostSetupContent()
	{
		_contentReady = true;
	}

	public override void PostUpdateEverything()
	{
		// 只在世界里做，此时所有贴图与模组内容都已完全就绪。
		if (!_contentReady || Main.gameMenu)
			return;

		int ready = ReadyItemCount();
		if (ready > 0 && ready != _appliedCount)
			Apply();
	}

	public override void Unload()
	{
		Restore();
		_originals = null;
		_appliedCount = -1;
		_contentReady = false;
	}

	/// <summary>
	/// 按当前配置重新套用一次（先还原，再替换），可安全重复调用。
	/// </summary>
	public static void Apply()
	{
		Restore();

		int itemCount = ReadyItemCount();
		if (itemCount <= 0 || Main.instance is null)
			return;

		ZenithLookConfig config = ModContent.GetInstance<ZenithLookConfig>();
		if (config is null || !config.Enabled) {
			_appliedCount = itemCount;
			return;
		}

		if (ItemID.Zenith >= itemCount)
			return;

		// 先确保天顶剑贴图已被加载，否则之后游戏调用 LoadItem 时可能把我们的替换覆盖回去。
		Main.instance.LoadItem(ItemID.Zenith);
		Asset<Texture2D> zenithTexture = TextureAssets.Item[ItemID.Zenith];
		if (zenithTexture is null)
			return;

		var originals = new Asset<Texture2D>[itemCount];

		for (int type = 1; type < itemCount; type++) {
			if (type == ItemID.Zenith)
				continue;

			if (!ShouldReplace(type, config))
				continue;

			originals[type] = TextureAssets.Item[type];
			TextureAssets.Item[type] = zenithTexture;
		}

		_originals = originals;
		_appliedCount = itemCount;
	}

	/// <summary>
	/// 把之前替换掉的槽位还原成原贴图。
	/// </summary>
	public static void Restore()
	{
		if (_originals is null || TextureAssets.Item is null)
			return;

		int count = Math.Min(_originals.Length, TextureAssets.Item.Length);
		for (int type = 1; type < count; type++) {
			Asset<Texture2D> original = _originals[type];
			if (original is null)
				continue;

			TextureAssets.Item[type] = original;
			_originals[type] = null;
		}

		_originals = null;
	}

	private static int ReadyItemCount()
	{
		if (TextureAssets.Item is null)
			return 0;

		return Math.Min(TextureAssets.Item.Length, ItemLoader.ItemCount);
	}

	private static bool ShouldReplace(int type, ZenithLookConfig config)
	{
		// ItemsByType 是每种物品的一份样板实例，世界加载后一定可用。
		if (!ContentSamples.ItemsByType.TryGetValue(type, out Item sample))
			return false;

		return IsWeapon(sample, config);
	}

	private static bool IsWeapon(Item item, ZenithLookConfig config)
	{
		if (item.damage <= 0)
			return false;

		// 弹药、饰品、工具、钓竿不算武器。
		if (item.ammo != AmmoID.None)
			return false;
		if (item.accessory)
			return false;
		if (item.pick > 0 || item.axe > 0 || item.hammer > 0)
			return false;
		if (item.fishingPole > 0)
			return false;

		// 自带发光遮罩的武器，遮罩图尺寸通常和本体不一致，替换后会留下错位的发光残影。
		if (config.SkipGlowMaskItems && item.glowMask >= 0)
			return false;

		// 按四大伤害类型分流；其它模组自定义类型（例如灾厄盗贼）归到「其它」。
		if (item.CountsAsClass(DamageClass.Melee))
			return config.IncludeMelee;
		if (item.CountsAsClass(DamageClass.Ranged))
			return config.IncludeRanged;
		if (item.CountsAsClass(DamageClass.Magic))
			return config.IncludeMagic;
		if (item.CountsAsClass(DamageClass.Summon))
			return config.IncludeSummon;

		return config.IncludeOther;
	}
}
