# 天顶幻化 / ZenithLook

> 让泰拉瑞亚里的**所有武器**都长成「天顶剑」的样子 —— 纯客户端外观模组，不改数值、不改判定、零网络开销。

[![Terraria](https://img.shields.io/badge/Terraria-1.4.4.9-6a5acd)](https://terraria.org)[![泰拉瑞亚](https://img.shields.io/badge/Terraria-1.4.4.9-6a5acd)](https://terraria.org)
[![tModLoader](https://img.shields.io/badge/tModLoader-2026.8.x-4b8bbe)](https://github.com/tModLoader/tModLoader)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)[![许可证](https://img.shields.io/badge/license-MIT-green)](许可证)

---

## 这是什么

一个只替换贴图的外观摩组。装上之后，背包图标、手持外观、地上掉落、商店与箱子里的图标，**全部变成天顶剑**——包括模组新增的武器（Calamity 等）。

完全在客户端本地完成，不发送任何网络数据，因此：

- 联机时服务器和队友都不需要安装
- 可以随时开关、随时卸载，不留存档痕迹
- 不占额外显存（所有武器共用同一份已加载的贴图资源，模组内不含任何 PNG）

## 安装（玩家）

1. 到 [Releases](../../releases) 下载最新的 `ZenithLook.tmod`1.前往[Releases发布版本](../../releases)下载最新的`ZenithLook.tmod`
2. 放进 `Documents\My Games\Terraria\tModLoader\Mods\`2.放入`Documents\My Games\Terraria ModLoader\Mods\文档\我的游戏\泰拉瑞亚模组加载器\模组\`2.放入`文档\我的游戏\泰拉瑞亚\模组加载器\模组\`2.放入`文档\我的游戏\泰拉瑞亚模组加载器\模组\`
3. 启动游戏 → 模组（Mods）→ 勾选「天顶幻化」→ 重载

> 不需要 .NET SDK，不需要编译。

## 从源码构建（开发者）

```bash
git clone https://github.com/<Bread789>/ZenithLook.git
```

把整个 `ZenithLook` 文件夹放进 `Documents\My Games\Terraria\tModLoader\ModSources\`，然后在游戏里 **模组 → 开发模组 → 构建 + 重载**。将整个`ZenithLook`文件夹放入`Documents\My Games\Terraria ModLoader\ModSources\文档\我的游戏\泰拉瑞亚模组加载器\模组源文件夹\`，然后在游戏里**模组 → 开发模组 → 构建 + 重载**。

也可以用命令行（注意：`-build` 的参数是**路径**，传模组名会在当前目录新建一个空壳，编出零类型的空程序集，载入时报「命名空间与文件夹名不匹配」）：

```powershell
& "<tModLoader安装目录>\dotnet\dotnet.exe" "<tModLoader安装目录>\tModLoader.dll" `
  -build "<ModSources完整路径>\ZenithLook"
```

## 配置

游戏内 **设置 → 模组配置 → 天顶幻化**，修改后立即生效，无需重载。

| 选项 | 默认 | 说明 |
| --- | --- | --- |
| 启用天顶外观 | 开 | 总开关 |
| 近战武器 | 开 | 按伤害类型分流 |
| 远程武器 | 开 | |
| 魔法武器 | 开 | |
| 召唤武器 | 开 | |
| 其它伤害类型 | 开 | 模组自定义类型，如灾厄盗贼 |
| 跳过自带发光遮罩的武器 | 开 | 遮罩尺寸不同会留下错位残影；想要 100% 全覆盖可关闭 |

「武器」的判定：伤害 > 0，且不是弹药、饰品、工具（镐/斧/锤）、钓竿。

## 实现原理

物品在画面上的每一次绘制，都从 `TextureAssets.Item[物品ID]` 取贴图。因此不需要逐把武器做替换，只要在贴图加载完成后，把所有武器槽位指向天顶剑那份**已加载的资源**即可：

```csharp
Main.instance.LoadItem(ItemID.Zenith);
Asset<Texture2D> zenithTexture = TextureAssets.Item[ItemID.Zenith];

for (int type = 1; type < itemCount; type++) {
    if (!IsWeapon(ContentSamples.ItemsByType[type])) continue;
    originals[type] = TextureAssets.Item[type];   // 记录原图，便于还原
    TextureAssets.Item[type] = zenithTexture;     // 换成天顶剑
}
```

几个关键点：

- `Main.LoadItem` 内部是 `if (Asset.State != NotLoaded) return;`，因此替换后不会被游戏重新加载覆盖
- 贴图数组由 `ItemLoader.ResizeArrays` 扩容，所以模组武器同样在覆盖范围内
- 套用时机放在**进入世界后**（`PostUpdateEverything`），避免贴图数组尚未扩容时只覆盖一半
- 关掉配置、切换分类时，按 `originals` 逐项还原

## 联机行为

纯客户端模组（`build.txt` 中 `side = Client`），不做任何同步：

- **没装模组的队友**看到的是原始武器外观（物品 ID 没有变化）
- 装了同一模组的队友，自己那边也会全变成天顶剑，包括看到你手上的武器
- 因为替换按物品 ID 全局生效，**你**看别人手里的武器同样会变成天顶剑

## 常见问题

**载入时报「命名空间与文件夹名不匹配」**
通常是构建产物里没有任何类型（空程序集）。文件夹名必须严格是 `ZenithLook`；命令行构建要传完整路径，而不是模组名。

**贴图没变化**
确认模组已启用并在配置里开启总开关；如果只有个别武器没变，多半是它带发光遮罩（见配置项）。

**需要装 .NET SDK 吗**
不需要。tModLoader 自带运行时与 Roslyn 编译器，游戏内「构建 + 重载」开箱即用。

## 目录结构

```
ZenithLook/
├── build.txt                     模组元信息（side = Client）
├── description.txt               游戏内显示说明
├── ZenithLook.cs                 Mod 入口
└── Common/
    ├── ZenithLookConfig.cs       客户端配置
    └── ZenithLookSystem.cs       贴图替换与还原核心
```

## 许可

[MIT](LICENSE) — 随便用、随便改，保留版权声明即可。

## 致谢

- [tModLoader](https://github.com/tModLoader/tModLoader) 提供模组框架
- 贴图来自泰拉瑞亚原版资源，本模组不包含、也不分发任何游戏美术资源
