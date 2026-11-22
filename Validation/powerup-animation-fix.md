# 蘑菇消失与小怪动画修复 · 2026-10-17

## 原因与修改

`Character` 同时包含敌人、道具和特效。旧代码在顶砖、火球和龟壳碰撞时，把这个显示分组当成敌人分类。蘑菇被顶砖影响后会被设为死亡状态 `1000`，并在 40 帧后删除。这不是蘑菇没有运动：原来的运动路径存在，最初诊断已纠正，未保留重复的道具运动实现。

现用明确的 `IsEnemy` 与 `IsPowerup` 分类。蘑菇、1UP 和星星被顶砖影响时弹起，火球和龟壳穿过道具，不再把它们送入敌人死亡流程；敌人仍可被正常击败。火焰花不受这些攻击伤害。

板栗仔的参考动画由同一张非对称精灵左右镜像交替构成。旧状态查询只有 `one/two`，而其目录根本没有 `two` 精灵，所以无法产生第二个姿态。已恢复镜像循环；无需新画贴图。小怪动画以自身逻辑年龄推进，暂停时保持原姿态。所有 1,092 张 PNG 与离线参考转换结果一致。

## 复现及验证

新增回归检查在修复前复现了 13 个失败，证据保存在 `powerup-regression-before.json`：板栗仔只有一个姿态；顶砖误杀蘑菇、错误加分、40 帧后删除、无法拾取；火球和龟壳误杀四种道具。

最终实际输出：

```text
MARIO_VALIDATION_PASS levels=32 areas=63 nodes=8444 checks=486 failed=0
MARIO_PHYSICS_REFERENCE_PASS scenarios=12 frames=2160 maxPositionError=0
MARIO_BUILD_PASS errors=0 warnings=0 bytes=143681648
MARIO_PLAYER_SMOKE_PASS courses=32 simulatedFrames=23040 errors=0
MARIO_POWERUP_ANIMATION_PASS mushroomSurvivalFrames=81 enemies=4 renderedFrames=24 errors=0
UNITY_YAML_PASS files=33 blocks=40 assetGuids=1261 localReferences=5 externalReferences=37 errors=[]
```

Windows 专项测试实际顶出 1-1 蘑菇，等待它走到相邻砖上，再从下方顶砖。连续运行超过旧死亡时限，确认仍存活并能拾取。另一组场景连续渲染 24 帧，检查板栗仔、乌龟、甲虫和刺猬的渲染器可见性及姿态切换。食人花的帧切换、暂停冻结、攻击仍能杀敌也纳入 Unity 检查。

- 最新 Player 输出：`PlayerPowerupFix/player-result.txt`、`PlayerPowerupFix/powerup-animation-result.txt`、`player-powerup-fix.log`。
- 已打开检查：`PlayerPowerupFix/enemy-walk-00.png`、`enemy-walk-08.png`、`enemy-animation-sequence.png` 和 `mushroom-80.png`。动画拼图包含全部 6 个采样帧。
- 最新编译/测试日志：`../_zcode_tmp/powerup-final-validation.log`、`../_zcode_tmp/powerup-final-build.log`。
- 运行 Unity 菜单 **Mario → Validate project and gameplay** 可复现编辑器检查；使用 README 中 `-mario-smoke` 参数可复现包含新增专项测试的 Windows 检查。

这是上述具体缺陷的修复与回归验证，不代表已完成 32 关人工通关或全部原版时序一致性认证。
