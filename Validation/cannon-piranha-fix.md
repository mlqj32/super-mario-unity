# 炮台、炮弹与食人花近距离行为核查

2026-10-17，Unity 6000.3.15f1。

已打开用户截图 `D:/DevTools/temp/codex-clipboard-9fd1436a-e22b-48d9-b138-aed53c1827bd.png` 和 `D:/DevTools/temp/codex-clipboard-9c791a3a-bfe9-4bda-9b2d-5712d3252aa0.png`。

## 修复

- 炮弹原始贴图朝右，旧渲染却沿用默认朝左的行走敌人翻转逻辑，造成倒着飞。现在向左移动时翻转，向右移动时使用原图；飞行方向在发射时确定，不会随玩家移动转向。
- 16px 高矮炮台使用 32px 高贴图的底部，显示为下半身而缺失炮口。现在取贴图顶部作为短炮台炮口；32/48px 炮台和日版逐格炮台继续使用对应的完整图块。
- 直接由关卡生成的炮弹缺少初速，可能停在空中。现在未指定速度时设为向左飞行。
- 炮台近距离抑制改为参考逻辑约 40px 水平范围，发射速度为 1.5px/帧；完全离屏的炮台不发射。炮声改为已有爆破音效。
- 旧食人花只把靠近时的显示高度归零，却继续推进出管周期，到停留阶段便瞬间全高出现。现在在出管起点等待玩家离开，解除抑制后平滑上升；不会在玩家靠近时突然消失。
- 绿色花近距离阈值采用 33px，日版红色花采用 19px，以玩家与花的左侧坐标差比较，不由高度决定；日版倒挂花按参考逻辑不做这一距离抑制。

图二单张图片无法区分花原先是否已开始伸出。正常规则是：玩家靠近时阻止尚在管内的普通花再次出来；已经开始伸出的花仍完成该轮周期，因此靠近时看见花并不一定是错误。本次修复了实际存在的距离过窄与计时越过隐藏状态的问题。

## 参考与边界

- 初代本地反汇编：`_zcode_tmp/archive/development-2026-10-15/reference/smb1/src/prg.asm` 中 `ChkPlayerNearPipe`（0x21）、`SetupBB`（近距离范围、速度）。
- 日版：`ThirdParty/SMB2JReference/sm2main.asm` 中 `InitPiranhaPlant` 将绿色阈值 0x21、红色阈值 0x13 写入距离判断；`MovePiranhaPlant` 在管内等待时判断距离；`sm2data4.asm` 中 `MoveUpsideDownPiranhaP` 没有玩家距离检查。
- 原始炮弹素材：`Assets/Resources/Pixels/0021.png`。炮台组合素材：`0851.png`、`0852.png`。本次读取并放大检查过这些图像。

仍使用项目既有的原生周期计时，未声称敌人 AI、随机发射或 NES 硬件时序逐帧一致。炮弹穿过一般地形是原作特性，本次没有改为撞墙消失。

## 验证

`MarioPiranhaAudit` 覆盖普通/日版绿/日版红花、左右两侧、900 帧持续靠近、边界距离、平滑出管、靠近已伸出的花、完成周期后继续留管、倒挂花以及原有遮挡像素检查。

`MarioCannonAudit` 覆盖左右发射和贴图朝向、出膛高度、匀速飞行、不追踪转向、踩踏、近距离/离屏抑制、预置炮弹移动与清理、16/32/48px 炮台顶部实际渲染像素，以及现有所有关卡区域中的炮台发射。

完整结果见 `results.json`、`PlayerCannonPiranhaFix/cannon-checks.txt`、`PlayerCannonPiranhaFix/piranha-checks.txt`。Unity 日志 `_zcode_tmp/cannon-piranha-validation.log`，构建日志 `_zcode_tmp/cannon-piranha-build.log`，实际 Player 日志 `player-cannon-piranha-fix.log`。

最终 Unity 输出：`MARIO_VALIDATION_PASS levels=64 areas=136 nodes=83609 checks=2705 failed=0`；人物参考运动 `MARIO_PHYSICS_REFERENCE_PASS scenarios=12 frames=2160 maxPositionError=0`。

最终构建和 Windows Player：

```text
MARIO_BUILD_PASS errors=0 warnings=0 bytes=168953168
MARIO_PIRANHA_PASS checks=46 errors=0
MARIO_CANNON_PASS checks=166 errors=0
MARIO_GAMEPLAY_AUDIT_PASS checks=777 failed=0
MARIO_EFFECTS_PASS checks=386 errors=0
MARIO_PLAYER_SMOKE_PASS courses=64 simulatedFrames=46080 errors=0
```

已实际打开检查 `PlayerCannonPiranhaFix/cannon-fire-left.png`、`cannon-fire-right.png`、`cannon-short-muzzle.png`、`piranha-nearby-stays-hidden.png`。炮弹头朝向飞行方向，矮炮台保留炮口，玩家近距离停留 900 帧后花仍在管内。既有动画、朱盖姆、金币、设置与重开专项也通过。64 关自动回归并非人工逐关通关。
