最新版本已扩展为初代32关 + 日版二代32关，共64关，并修复金币特效、补齐中文界面。最终构建、1,806项检查和实际Player证据见 [本轮验证报告](coin-chinese-japan-fix.md)。下面保留初代交付历史，不代表最新关卡总数。

# 历史验证记录（初代）

日期：2026-10-17。Unity：6000.3.15f1。目标：Windows x64。

## 已交付

- 原生 Unity 工程与 `Assets/Scenes/Main.unity` 启动场景。
- 1-1 至 8-4 共 32 个可编辑关卡，包含 63 个区域、8,444 个地图对象。
- `Mario → Level Editor` 关卡编辑窗口和 32 关直接选关界面。
- `Builds/Windows/超级马里奥.exe` 及其完整运行依赖。

## 实际检查结果

以下为工具实际输出；最新 Unity 检查在最终运行时代码上重新执行，进程退出码为 0。

```text
MARIO_VALIDATION_PASS levels=32 areas=63 nodes=8444 checks=486 failed=0
MARIO_PHYSICS_REFERENCE_PASS scenarios=12 frames=2160 maxPositionError=0
MARIO_BUILD_PASS errors=0 warnings=0 bytes=143681648
MARIO_PLAYER_SMOKE_PASS courses=32 simulatedFrames=23040 errors=0
UNITY_YAML_PASS {"pass_": true, "files": 33, "blocks": 40, "assetGuids": 1261, "localReferences": 5, "externalReferences": 37, "errors": []}
```

| 证据 | 检查内容 |
| --- | --- |
| `results.json`、`summary.txt` | 864 项关卡结构、传送、运行、运动、道具、换关、结局、编辑器及实际动画像素检查 |
| `../_zcode_tmp/gameplay-audit-verified.log` | 最后一次 Unity 批处理编译和验证日志 |
| `physics-reference.csv`、`physics-diff.csv`、`physics-summary.txt` | 12 组、2,160 帧独立 C 参考与 Unity C# 控制器的位置对比 |
| `physics-reference-notes.md` | 参考代码版本、输入序列、地面约束及验证范围 |
| `build-result.txt`、`../_zcode_tmp/gameplay-audit-build.log` | Windows 构建结果和完整日志 |
| `PlayerGameplayAudit/*result.txt`、`player-gameplay-audit.log` | 最新 Windows 版 361 项玩法检查和 32 关、道具、小怪、城堡动画测试 |
| `PlayerPowerupFix/player-result.txt`、`player-powerup-fix.log` | 修复后实际 Windows 可执行版运行记录 |
| `structure.json` | Unity YAML 解析、文档数、对象引用和资源 GUID 检查 |

本次修复详见 [蘑菇与小怪动画修复报告](powerup-animation-fix.md)。实际 Windows 可执行版在 Direct3D 11 下通过测试；新增专项测试确认蘑菇被顶砖后持续 81 帧存活且可拾取，4 种小怪经过 24 个实际渲染帧呈现不同姿态。已打开检查蘑菇第 80 帧截图及全部 6 张小怪采样帧的拼图。完整记录在 `PlayerPowerupFix`。

早期无效截屏和过时拼图已移动到 `../_zcode_tmp/archive/obsolete-player-2026-10-15`，不计入上述证据。临时研究和测试程序已归档，游戏运行不依赖归档文件。

## 精度结论与未验收项

最新 864 项检查与 19 类修复见 [玩法系统核查报告](gameplay-audit-fix.md)。旧 [城堡与人物动画修复报告](castle-animation-fix.md)、`PlayerPowerupFix` 和 `PlayerCastleFix` 保留为历史证据；最新运行版回归仍覆盖这些场景。

走路、跑步、刹车、地面反向、空中转向和长短跳等 12 组测试输入中，原生控制器与独立 SMB-Vanilla C 参考的逐帧位置一致，最大误差为 0 像素。该测试使用统一的平地约束，并非 NES 整机模拟。

地图由 FullScreenMario 的初代关卡描述转换而来，尚未通过原始 ROM 数据逐砖交叉核验。敌人 AI、出生/休眠时机、碰撞边角、弹簧、天平、计时器与奖励计分仍有近似实现。未覆盖全部原版漏洞、硬件延迟和完整多人模式。

32 关运行压力测试检查稳定性，不等于正常路线自动通关。尚未逐关人工通关，手柄也未进行实物测试。因此，当前交付是具备明确验证证据的原生复刻工程，不能据此认证整款游戏与原版 100% 一致。

## 复现

在工程根目录执行：

```powershell
& 'D:/Unity/Editors/6000.3.15f1/Editor/Unity.com' -batchmode -quit -projectPath $PWD.Path -executeMethod MarioValidation.Run -logFile '_zcode_tmp/validation-final.log'
python Tools/validate-project.py
& 'D:/Unity/Editors/6000.3.15f1/Editor/Unity.com' -batchmode -quit -projectPath $PWD.Path -executeMethod ProjectSetup.Build -logFile '_zcode_tmp/build.log'
& './Builds/Windows/超级马里奥.exe' -mario-smoke -mario-output "$($PWD.Path)/Validation/PlayerFinal" -logFile "$($PWD.Path)/Validation/player-final.log" -screen-width 768 -screen-height 720 -screen-fullscreen 0 -force-d3d11 | Out-Null
```

批处理运行时，应先关闭正在打开此工程的 Unity 编辑器。实际 Player 测试会自动退出。

