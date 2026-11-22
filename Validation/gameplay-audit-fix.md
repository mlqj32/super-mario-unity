# 玩法系统核查与修复（2026-10-17）

## 龟壳不滚动的原因

旧 `MarioActors.cs` 把从上方接触龟壳的情况统一写成 `a.vx=0`，没有区分静止壳和移动壳。现在普通乌龟壳和钢盔甲虫壳均执行：第一次踩敌人缩壳；再次踩静止壳或侧踢使其向远离玩家的方向滚动；踩滚动壳使其停止。侧面接触迎面滚来的壳仍会受伤。

## 本轮确认并修复的 19 类问题

| # | 问题 | 修复与验证 |
|---|---|---|
| 1 | 再踩静止龟壳仍停在原地 | 区分启动和停止；两种壳、左右方向、持续滚动、侧踢、撞墙和伤害回归通过。 |
| 2 | 壳停止后可能立刻恢复成乌龟 | 使用独立静止计时，踢出或停止时重置；滚动时间不再算入复活时间。 |
| 3 | 火球飞出屏幕右侧仍占用两个发射名额 | 离开右侧活动范围后回收火球及相关投射物，验证可以再次发射。 |
| 4 | 钢盔甲虫缩壳后失去火焰免疫 | BeetleShell 与 Beetle 一样抵挡玩家火球。 |
| 5 | 踩板栗仔等敌人重复加分 | 移除踩踏分支与 Kill 中重复的奖励；普通板栗仔一次踩踏得 100 分。 |
| 6 | 岩浆火球只跳一次就永久消失 | Podoboo 回到岩浆后等待下一次跳跃，不再被普通坠落清理逻辑删除；450 帧验证多次跳起。 |
| 7 | 小马里奥直接吃火焰花变成火焰形态 | 按小→大、大→火焰处理；三个初始形态分别测试。 |
| 8 | 攀爬藤蔓时整个敌人世界停止更新 | 爬藤只接管人物运动，敌人和平台继续步进；测试上爬、敌人计时和跳离。 |
| 9 | 死亡和重生保留星星、受伤闪烁等临时状态 | 死亡入口清理临时能力、变身冻结和攀爬/传送状态。 |
| 10 | 移动平台上的人物落后一帧，平台循环时可能拖拽人物传送 | 先更新平台，再同步承载位移；覆盖上升、下降、水平承载和循环回收。 |
| 11 | 已死亡敌人和投射物持续堆积在列表与场景中 | 定期回收失效 Actor 及其显示对象，验证大量失效对象不会无限堆积。 |
| 12 | 死亡中开关 F1 选关界面会恢复为正常游戏状态 | 暂停保存并恢复进入前的阶段；标题、死亡和正常游戏均有检查。 |
| 13 | 新关卡计时继承上一关的全局帧余数 | 单独累计有效游戏计时；新关卡和重生从完整计时周期开始。 |
| 14 | 动态生成的投射物可扩张关卡宽度和相机边界 | Character 生成不再改变关卡宽度，地形/分段关卡仍能正常扩展。 |
| 15 | 非压扁类敌人被踩后悬停；被击飞时仍正立 | 非板栗仔死亡使用下落状态，击飞绘制倒置精灵并保持底部锚点。 |
| 16 | 空中发射火球会切回站立发射姿态 | 发射状态保留 jumping 标签，采用跳跃发射贴图。 |
| 17 | 向上运动的火球能先穿入天花板再被下一帧水平检测处理 | 增加上行撞顶处理；检查火球不会在存活状态穿进实体。 |
| 18 | 建立 A 关预览后切换到 B 关或另一区域，Apply 可能覆盖错误目标 | 预览绑定所属关卡和区域；不匹配时禁止应用，使用临时关卡对象验证。 |
| 19 | 进入 Play 时编辑预览仍留在场景里，叠加到正式游戏 | 编辑器进入 Play 前清理预览，包括工具按钮和编辑器自身的 Play 切换。 |

初次新增测试出现 20 个失败用例，包含两种壳、两个方向等重复情形，并不等于 20 个不同根因。原始结果保留在 `gameplay-audit-before.json`；修改前的核心代码副本在 `../_zcode_tmp/archive/gameplay-audit-before`。

## 核查范围与实际输出

审查运行逻辑、角色碰撞、渲染、输入/暂停、关卡数据和关卡编辑器。新增 361 项运行检查与 3 项编辑器检查，叠加原有 500 项，共 864 项。

- 全部 32 关：坠落与重生、所有声明的传送目标、终点所在区域、旗杆/斧头触发和下一关/最终胜利。
- 4-4、7-4、8-4：存在决策器的迷宫段，分别检查成功和失败分支展开。
- 16 种敌人/危险物：普通接触与星星状态接触；另含水下踩怪、弹簧、低天花板蹲伏。
- 保留上一轮蘑菇、四种小怪、人物动画、城堡火棒的回归检查。
- Windows 实际 Player 再运行新增 361 项，并采集完整的龟壳启动/停止截图。

```text
MARIO_VALIDATION_PASS levels=32 areas=63 nodes=8444 checks=864 failed=0
MARIO_PHYSICS_REFERENCE_PASS scenarios=12 frames=2160 maxPositionError=0
MARIO_BUILD_PASS errors=0 warnings=0 bytes=143705200
MARIO_GAMEPLAY_AUDIT_PASS checks=361 failed=0
MARIO_POWERUP_ANIMATION_PASS mushroomSurvivalFrames=81 enemies=4 renderedFrames=24 errors=0
MARIO_CASTLE_ANIMATION_PASS renderedFrames=24 runCells=3 flameCells=4 starPalettes=4 crouchHeight=22 errors=0
MARIO_PLAYER_SMOKE_PASS courses=32 simulatedFrames=23040 errors=0
UNITY_YAML_PASS files=33 blocks=40 assetGuids=1262 localReferences=5 externalReferences=37 errors=[]
```

证据：`results.json`、`summary.txt`、`structure.json`、`build-result.txt`、`player-gameplay-audit.log`、`PlayerGameplayAudit/gameplay-audit-checks.txt`。Unity 日志：`../_zcode_tmp/gameplay-audit-verified.log` 与 `../_zcode_tmp/gameplay-audit-build.log`。

实际运行截图：`PlayerGameplayAudit/shell-01-stopped.png`、四张 `shell-rolling-*.png`、`shell-02-stopped-again.png`。已打开检查六帧拼图 `shell-sequence.png`。`shell-demo.gif` 是采样截图演示，非实际帧率录像。

核查没有覆盖对 32 关逐关人工完整通关。传送/终点测试直接构造抵达状态，验证触发与换关；迷宫测试验证分支展开，不等于自动走完整条路线。地图、敌人 AI、原版特殊规则仍不能据此认证与 NES 原版逐帧完全一致。报告列出的已确认问题已修复，不能将测试全绿解释为所有未知边界情况都不存在。

本轮没有覆盖用户编辑的关卡资源，没有重新导出地图或贴图，也没有修改 ClassicMotor 的运动常量。
