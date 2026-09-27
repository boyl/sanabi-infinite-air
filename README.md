# SANABI Infinite Air / 闪避刺客无限悬空

适用于 Steam 版《闪避刺客》（SANABI）的 BepInEx IL2CPP Mod。开启后，角色在空中可以持续水平移动和调整高度；再次切换即可恢复正常下落。代码分别接入主角和 DLC 角色。

## 操作

| 输入 | 功能 |
| --- | --- |
| `F8` 或同时按住 `LB+RB` | 开启／关闭无限悬空 |
| `W/S`、方向键上下、手柄上下方向输入 | 开启后在空中升降 |

切换时角色附近只显示 `AIR: ON` 或 `AIR: OFF`，0.9 秒后自动消失。组合键长按只切换一次，松开后可再次触发。水平移动仍由游戏原本的输入处理。`F8`、手柄开关、升降速度及是否显示状态提示，可在首次运行后生成的 `BepInEx/config/com.codex.sanabi.infiniteair.cfg` 中调整。

## 安装

1. 安装 [BepInEx 6.0.0-pre.1 Unity IL2CPP x64](https://github.com/BepInEx/BepInEx/releases/tag/v6.0.0-pre.1)，将其中内容放到 `SNB.exe` 所在的游戏目录。
2. 启动游戏一次，等 BepInEx 生成 `BepInEx/plugins`，然后退出游戏。
3. 将发布包里的 `BepInEx` 文件夹合并到游戏目录，再启动游戏。

如需卸载，退出游戏后删除 `BepInEx/plugins/SanabiInfiniteAir.dll`。配置文件可以保留。

## 验证范围

开发机使用 Steam Windows 版、Unity 2019.4.41、IL2CPP、BepInEx 6.0.0-pre.1；Steam build ID 为 `21675470`。已在游戏内核对键盘升降及 `LB+RB` 开关；角色附近状态提示仍待视觉复核。DLC 角色补丁已编译及加载，但尚未分别完成实际游玩验收。其他游戏构建版本未验证。

## 源码构建

项目位于 `src/`，目标框架为 `netstandard2.1`。需先运行游戏让 BepInEx 生成 `BepInEx/unhollowed`；随后执行：

```powershell
dotnet build src/SanabiInfiniteAir.csproj -c Release -p:SanabiGameDir="C:\path\to\SANABI"
```

生成的 DLL 位于 `src/bin/Release/netstandard2.1/SanabiInfiniteAir.dll`。源码不包含游戏文件、BepInEx 程序集或用户存档。

English: This BepInEx IL2CPP mod enables controllable infinite air movement in SANABI. Press F8 or LB+RB to toggle; use W/S, Up/Down or your gamepad's vertical movement input to adjust height while airborne. A brief `AIR: ON` / `AIR: OFF` status appears near the character for 0.9 seconds after toggling; the in-game hint can be disabled with `ShowHint = false` in the config. Follow the installation steps above; the required BepInEx version is linked there.
