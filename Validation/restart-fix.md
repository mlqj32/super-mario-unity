# 死亡快进、重开确认与小马里奥

日期：2026-10-17。Unity 6000.3.15f1。

## 行为

- 死亡动画期间可点击“立即复活”，或按回车、空格、Z、R，以及手柄 A/Start，立即结算此次死亡。仍扣一条生命，保留检查点，以小马里奥复活。没有剩余生命时显示“跳过动画”，进入游戏结束，不会擅自开启新游戏。
- 不操作时保留原来的死亡动画。自动结束与主动跳过共用有状态保护的结算入口，防止重复扣命。
- 暂停菜单“重玩本关”先弹出确认。默认取消；左右键切换、回车确认、ESC 取消，也可鼠标点击。弹窗期间保持暂停，关闭弹窗的按键不会继续触发暂停切换或新游戏。
- 手动确认后恢复小马里奥，从本关起点重开并重置时间、检查点、速度、火焰/无敌状态及传送/攀爬状态；保留分数、金币和生命。取消时保持当前进度和形态。
- 正常过关和区域传送仍保留变身。新增中文字形覆盖检查通过；生命提示增加底板，避免与白云重叠。

## 实现和验证

`MarioRestart.cs` 集中死亡结算、确认状态和输入处理；`MarioGame.cs` 的超时路径复用结算入口；`MarioInterface.cs` 显示死亡入口和模态确认框。固定步长循环先扣除当前步长再执行模拟，避免复活重置累积时间后又减出负值。

`MarioRestartAudit.cs` 覆盖初代与日版二代、小/大/火焰三种形态、默认取消、ESC 取消、确认重开、重复调用、死亡第 0/30/149/150 帧跳过、自动结算、最后一命、检查点与 UI 字形。自动化调用实际输入分派和结算方法；不是物理手柄硬件验收。

实际运行输出：

```text
MARIO_VALIDATION_PASS levels=64 areas=136 nodes=83609 checks=2442 failed=0
MARIO_PHYSICS_REFERENCE_PASS scenarios=12 frames=2160 maxPositionError=0
MARIO_BUILD_PASS errors=0 warnings=0 bytes=168941904
MARIO_RESTART_PASS checks=153 errors=0
MARIO_PLAYER_SMOKE_PASS courses=64 simulatedFrames=46080 errors=0
MARIO_EFFECTS_PASS checks=386 errors=0
MARIO_SETTINGS_PASS checks=28 errors=0
UNITY_YAML_PASS files=65 blocks=72 assetGuids=1696 localReferences=5 externalReferences=69 errors=[]
```

编译和构建日志：`_zcode_tmp/restart-validation.log`、`_zcode_tmp/restart-build.log`；构建结果 `build-result.txt`。Player 日志 `player-restart-fix.log`；专项逐项结果 `PlayerRestartFix/restart-checks.txt`。

已打开检查实际 Player 截图：

- `PlayerRestartFix/restart-death-prompt.png`
- `PlayerRestartFix/restart-confirmation.png`
- `PlayerRestartFix/restart-small-mario.png`

64 关自动运行是回归压力测试，不是逐关人工通关。
