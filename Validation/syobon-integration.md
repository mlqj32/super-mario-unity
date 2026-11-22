# 猫里奥关卡核实与 Unity 接入

核实日期：2026-10-17。

## 关卡数量与版本

Chiku 在 2007 年制作的《しょぼんのアクション / Syobon Action》，通常称为猫里奥，原版为 **4 关**。初始第一关后又制作了三关。随机地图模式不是额外固定关卡。不同网页重制、手机移植或民间续作可能增加关卡，不能把其数量套用于原版。

- [Syobon Action 开发历史](https://en.wikipedia.org/wiki/Syobon_Action)：第一关后新增三关。
- [本次固定版本的移植说明](https://github.com/SageChaozu/SyobonActionMono/blob/8a5b5f512fa94d3af1bfdac5aab804db5a7f7ace/README.md)：选关明确列出 1、2、3、4，并单列 0 键随机模式。

本次接入原版 4 关，包括其附属区域，共 8 份区域地图；现有 32 + 32 关马里奥保持独立，标题页显示三部作品、68 关。没有把附属区域重复计为新的关卡，也没有加入随机模式、其他续关、Mario Forever 或 SMB3。

## 接入方式和操作

标题或选关页选择“猫里奥”，再选择开始或第 1～4 关。鼠标、键盘 Q/E 和手柄 LB/RB 可切换作品。方向键／摇杆移动，Z／空格／手柄 A 跳跃；提示框使用确认键继续。暂停、音量、全屏／窗口、重开确认、失焦和手柄断线保护与现有菜单一致。

采用公开 C# 移植作为参考，替换 XNA 的窗口、输入、渲染与音频层，直接在 Unity 中运行 C#。没有外部游戏窗口、网页、MonoGame 运行程序或模拟器。底层猫里奥逻辑独立于马里奥：以原版 30 Hz 推进，画面为 480×420、Point 采样，默认保持比例。猫里奥没有马里奥式的走／跑两档，设置中的自动加速在此作品置灰，保留之前的马里奥偏好。

每次新开局从 10 命开始。猫里奥保留负命数仍可继续的原版行为；死亡时可立即重试并回到当前区域／检查点。暂停页“重玩本关”默认取消，确认后从本关起点重置机关与检查点。通关依次进入下一关，第四关进入结局。

参考移植中的 `Message N` 占位文字已替换为中文呈现；提示语进行了本地化改写，不宣称逐句原文翻译。来源与完整源码／许可证保留在 `ThirdParty/SyobonActionMono` 和 `ThirdParty/NOTICE.md`。

## 数据与修正

- `Assets/Resources/Syobon/Stages/*.csv`：4 关的 8 个区域，保留源代码的逐格数值；行是纵坐标，列是横坐标，网格间距 29 像素。可用文本编辑器修改，保存后重新进入 Play；不是马里奥的 LevelData 节点格式。
- `Assets/Scripts/Syobon/SyobonEngine.cs`：机关、敌人、场景连接及关卡脚本。`stagep()` 包含不能仅通过地形格表达的机关设置。
- `SyobonUnity.cs`：Unity 纹理绘制、地图读取、输入及运行控制；`MarioSyobon.cs` 连接现有菜单和生命周期。
- 图集按原始 Content.mgcb 指定的青色透明键转换为 alpha，保留原尺寸。修复旧移植中未使用的镜像标记、倒置敌人旋转中心、矩形缓存色彩和圆形绘制适配问题。
- 关卡重载清空整张旧地图及平台计数，防止进入较短区域后残留旧地形；恢复旧移植漏复制的每行最后一格。
- 快速复活保留地下区域及检查点；自动死亡结算期间不会再次扣命。跨作品切换释放猫里奥音轨和渲染资源。

`Tools/validate-syobon.py` 比较初次导入数据与固定参考版本。用户修改 CSV 后，该比较报告差异是预期行为，不代表修改后的地图无法使用。

## 验证范围

专项包括全部区域启动、每个屏幕的模拟和绘制、各区域 1800 帧混合输入、跳跃／移动、隐藏砖、检查点、死亡快重开、负命数、自动扣命、地下复活、关卡顺序、最终结局、提示框、图集边界与 22 个音频资源。另有虚拟手柄选关、移动、暂停、重开、返回及切换作品检查。

已实际查看 `Validation/Syobon/area-contact-sheet.png` 的全部 8 个区域，以及最终 `Validation/PlayerSyobonFinal/` 中的 `syobon-title.png`、`syobon-courses.png`、`syobon-settings.png` 和 `syobon-stage-4.png`。最终设置提示不重叠，猫里奥选关焦点落在关卡按钮上。

实际验证输出：

```text
SYOBON_REFERENCE_PASS stages=4 areas=8 cells=12124 atlases=9 errors=[]
MARIO_CHINESE_FONT_PASS checked=286 missing=[]
MARIO_VALIDATION_PASS levels=64 areas=136 nodes=83609 checks=4172 failed=0
MARIO_PHYSICS_REFERENCE_PASS scenarios=12 frames=2160 maxPositionError=0
MARIO_PLAYER_SMOKE_PASS courses=64 simulatedFrames=122880 errors=0
MARIO_MENU_PASS checks=270 errors=0
SYOBON_PLAYER_PASS stages=4 areas=8 checks=82 errors=0
SYOBON_INTEGRATION_PASS errors=0
MARIO_BUILD_PASS errors=0 warnings=0 bytes=191836423
UNITY_YAML_PASS files=65 blocks=72 assetGuids=1932 errors=[]
MARIO_MAP_INTEGRITY_PASS files=64 changed=0
```

完整 Unity 与 Windows 回归先通过；随后只调整了选关焦点回退、设置提示位置和截图测试场景，重新运行最终菜单 270 项校验、编译打包，以及 Windows 菜单／猫里奥集成回归。上述相关进程退出码均为 0。猫里奥成品音频实际采样为 `samples=470`，随后暂停时采样不变、恢复时继续推进，切换作品后两个专用 AudioSource 已释放。

日志与数据：`_zcode_tmp/syobon-final-validation-build.log`、`_zcode_tmp/syobon-menu-final.log`、`_zcode_tmp/syobon-build-final.log`、`Validation/player-syobon.log`、`Validation/player-syobon-final.log`、`Validation/syobon-reference.json`、`Validation/syobon-mario-map-integrity.json`。一次性移植准备脚本已归档至 `_zcode_tmp/archive/`，不要用它覆盖后续编辑过的地图或运行代码。

这不是四关逐一人工正常通关录像，也未与原始 Windows 程序逐帧对照。测试中的部分机关通过直接布置起始状态来触发；输入设备检查使用虚拟手柄，不等同于实体手柄兼容性验收。
