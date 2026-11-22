# 朱盖姆持续追踪修复

2026-10-17，Unity 6000.3.15f1，Windows x64。

## 原因与修复

原追踪速度仅 1.25 像素/逻辑帧，低于马里奥的 2.5 跑速；落到相机左侧 64 像素之外后，又被普通敌人的清理规则删除，因此跑远后云不再跟随。

- 追赶速度改为 3.5，采用参考实现的跑速 × 1.4；玩家移动时追踪前方位置，停下时在附近巡游，反向移动后重新追赶。
- 活跃朱盖姆不再因落到屏幕左侧而删除，可以从屏外追上，不瞬移。
- 增加投掷前缩进云中的姿态；重复敌人标记不会堆叠多个活跃朱盖姆。
- 被击败后约 350 逻辑帧从屏幕右侧返回；离开遭遇区域或换关会重置。
- 到达停止标记时取消同帧投掷并向左离场，不再复活；日版地图使用旗杆作为最终停止边界。

主要实现：`Assets/Scripts/MarioLakitu.cs`，回归检查：`Assets/Scripts/MarioLakituAudit.cs`。没有重导入或覆盖用户可编辑关卡。

## 本次实际验证输出

```text
MARIO_VALIDATION_PASS levels=64 areas=136 nodes=83609 checks=1823 failed=0
MARIO_PHYSICS_REFERENCE_PASS scenarios=12 frames=2160 maxPositionError=0
MARIO_BUILD_PASS errors=0 warnings=0 bytes=168842240
MARIO_LAKITU_PASS checks=17 errors=0
MARIO_PLAYER_SMOKE_PASS courses=64 simulatedFrames=46080 errors=0
MARIO_GAMEPLAY_AUDIT_PASS checks=713 failed=0
MARIO_COIN_CHINESE_JAPAN_PASS checks=171 errors=0
MARIO_POWERUP_ANIMATION_PASS mushroomSurvivalFrames=81 enemies=4 renderedFrames=24 errors=0
MARIO_CASTLE_ANIMATION_PASS renderedFrames=24 runCells=3 flameCells=4 starPalettes=4 crouchHeight=22 errors=0
UNITY_YAML_PASS {"pass_": true, "files": 65, "blocks": 72, "assetGuids": 1647, "localReferences": 5, "externalReferences": 69, "errors": []}
```

Unity 验证、构建、Windows Player 和 YAML 检查退出码均为 0。Player 的 Direct3D swapchain 时间戳提示由引擎切换 CPU 时钟处理。

17 项专项检查包括：1200 帧连续全速跑动、停留、反向、从屏外追回、重复标记、投掷姿态、击败后返回、终点离场、换区，以及初代 4-1 / 6-1 / 8-2 和日版 J-4-1 / J-4-2 / J-8-3 的持续卷屏。

实际打开查看了 `PlayerLakituFix/lakitu-chase-000.png`、`lakitu-chase-420.png`、`lakitu-chase-840.png`：4-1 的不同地形位置中朱盖姆持续出现在玩家前上方，中文 HUD 正常显示。

这些真实关卡截图采用受控位置推进，专项全速测试采用平地场景；不代表 64 关正常路线完整通关，也不代表 NES 敌人 AI 逐周期一致。

## 证据与运行

- `PlayerLakituFix/lakitu-checks.txt`：17 项逐项结果。
- `player-lakitu-fix.log`：实际 Windows Player 日志。
- `../_zcode_tmp/lakitu-validation.log`：Unity 编译与完整验证。
- `../_zcode_tmp/lakitu-build.log`：Windows 构建日志。
- `../Builds/Windows/超级马里奥.exe`：已更新的运行程序，同目录数据文件需保留。
