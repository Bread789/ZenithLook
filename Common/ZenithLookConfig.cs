using System.ComponentModel;
using Terraria.ModLoader.Config;

// [Label] 在新版 tModLoader 里被标记为过时（官方推荐改用本地化 .hjson，
// 但过时标记不影响功能：它仍作为配置界面文案的后备来源）。这里只为消掉编译警告。
#pragma warning disable CS0618

namespace ZenithLook.Common;

/// <summary>
/// 客户端配置。改动会立即生效，不需要重载模组。
/// </summary>
public class ZenithLookConfig : ModConfig
{
	// 只影响自己画面的东西，所以是客户端配置。
	public override ConfigScope Mode => ConfigScope.ClientSide;

	[DefaultValue(true), Label("启用天顶外观")]
	public bool Enabled { get; set; }

	[DefaultValue(true), Label("近战武器")]
	public bool IncludeMelee { get; set; }

	[DefaultValue(true), Label("远程武器")]
	public bool IncludeRanged { get; set; }

	[DefaultValue(true), Label("魔法武器")]
	public bool IncludeMagic { get; set; }

	[DefaultValue(true), Label("召唤武器")]
	public bool IncludeSummon { get; set; }

	[DefaultValue(true), Label("其它伤害类型（如灾厄盗贼）")]
	public bool IncludeOther { get; set; }

	/// <summary>
	/// 自带发光遮罩的武器贴图层尺寸可能和天顶剑不一致，替换后会出现错位的发光残影。
	/// 默认跳过这几把武器；想要 100% 全覆盖可以把它关掉。
	/// </summary>
	[DefaultValue(true), Label("跳过自带发光遮罩的武器")]
	public bool SkipGlowMaskItems { get; set; }

	// 配置保存后立刻重新套用。
	public override void OnChanged()
	{
		ZenithLookSystem.Apply();
	}
}
