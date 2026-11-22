# 城堡与人物动画修复（2026-10-17）

针对用户的 1-4 城堡截图，修复以下显示问题：

- 大马里奥蹲下的原图为 16×22，旧渲染按 16 像素高的碰撞框裁掉上方 6 行。单张角色精灵现在按原图尺寸完整绘制，底部对齐，横向居中；地形、藤蔓继续平铺。碰撞和移动参数未改变。
- 快跑的两处分帧计算分别使用 3 和 6 的除数，导致三帧循环不均匀、前 9 帧缺少第三张。现在使用统一帧索引。
- 攀爬新增两帧切换，游泳补全四个划水姿态及交替姿态，无敌配色补齐父级 star 状态，恢复四种颜色循环。
- 活动角色与地形原本同为排序层 0，城堡砖墙能够盖住火焰。角色与火焰现在显示在地形前；管道仍在角色前方，正在出砖的道具保持在砖块后方。

火棒本来就有旋转和四张贴图切换；本次修复其遮挡问题，并验证暂停/恢复。没有改动关卡资源、原始 PNG、物理参数或敌人 AI。

## 验证结果

```text
MARIO_VALIDATION_PASS levels=32 areas=63 nodes=8444 checks=500 failed=0
MARIO_PHYSICS_REFERENCE_PASS scenarios=12 frames=2160 maxPositionError=0
MARIO_BUILD_PASS errors=0 warnings=0 bytes=143685744
MARIO_POWERUP_ANIMATION_PASS mushroomSurvivalFrames=81 enemies=4 renderedFrames=24 errors=0
MARIO_CASTLE_ANIMATION_PASS renderedFrames=24 runCells=3 flameCells=4 starPalettes=4 crouchHeight=22 errors=0
MARIO_PLAYER_SMOKE_PASS courses=32 simulatedFrames=23040 errors=0
UNITY_YAML_PASS files=33 blocks=40 assetGuids=1261 localReferences=5 externalReferences=37 errors=[]
```

验证实际相机输出的每个不透明像素与所选精灵一致，并统计不同画面；不是仅验证动画状态名称。透明位置排除背景变化。早期测试有一次选中了部分出屏的火焰，已修正测试机位，最终测试保证目标在屏内。

最初人物回归记录在 `castle-animation-before.json`，其中 10 项直接复现人物的裁图、缺帧和配色缺陷；该早期记录的火棒像素数量检查受透明背景和出屏裁切影响，不能单独作为火棒故障证据。最终结果以 `results.json` 和实际 Player 结果为准。

实际 Windows Player 截图位于 `PlayerCastleFix`：`castle-crouching.png`、8 张 `castle-run-*.png`、4 张 `castle-star-*.png`。已打开检查蹲下、跑步单帧、跑步八帧拼图和四色拼图。`castle-run.gif` 是采样截图合成的慢放预览，不代表游戏实际帧率。

日志：`../_zcode_tmp/castle-verified-validation.log`、`../_zcode_tmp/castle-verified-build.log`、`player-castle-fix.log`。32 关压力测试不等于逐关人工通关或原版整机逐帧认证。
