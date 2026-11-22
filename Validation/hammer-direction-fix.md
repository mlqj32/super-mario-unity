# 锤子龟朝向修复

已实际打开用户截图 D:/DevTools/temp/codex-clipboard-a5324198-5723-46aa-afa4-7c7f5dad4e4a.png，并放大检查原始 Pixels/0074.png，确认源贴图朝左。

## 原因与修复

原 RefreshVisuals 将锤子龟当作普通行走敌人，用 vx 正负翻转贴图；它的 vx 是周期性左右踱步速度，投锤却按玩家位置瞄准，因此会背对玩家投掷，并随着踱步反复转身。

- 新 HammerBroDirection 使用玩家与敌人的水平中心判断目标侧，显示与投锤共用此判断。
- 活着的锤子龟朝向玩家；玩家绕背后，显示与下一次投锤跟随目标侧变化。
- 飞出的锤子保留原抛物线与水平速度；死亡状态保留原倒置尸体绘制逻辑。

## 验证

- 修复前：MARIO_HAMMER_BRO_FAIL checks=110 errors=46，见 _zcode_tmp/hammer-before.log。
- 修复后：MARIO_HAMMER_BRO_PASS checks=110 errors=0，见 _zcode_tmp/hammer-after.log。覆盖两侧、不同踱步/跳跃/投掷阶段、渲染像素、绕背、旧锤轨迹、死亡姿态及现有地图实例。
- Unity 全量：MARIO_VALIDATION_PASS levels=64 areas=136 nodes=83609 checks=5702 failed=0。
- 原运动参考：MARIO_PHYSICS_REFERENCE_PASS scenarios=12 frames=2160 maxPositionError=0。
- MARIO_BUILD_PASS errors=0 warnings=0 bytes=191859463。
- 全量与构建日志：_zcode_tmp/hammer-validation-build.log。
- 72 份关卡/区域 SHA256 均未改变，见 hammer-map-integrity.json。

已实际打开 Windows 截图 PlayerHammerFix/hammer-bros-3-1-face-left.png 与 hammer-bros-3-1-face-right.png：两只锤子龟分别正确朝向左侧/右侧玩家，举锤姿势同步翻转。使用真实地图的定位测试，未声称人工从头打通全部关卡。

最新版本：D:/Unity/project/超级马里奥/Builds/Windows/超级马里奥.exe。

Windows 全流程运行已结束，进程退出 0：MARIO_PLAYER_SMOKE_PASS courses=64 simulatedFrames=122880 errors=0；SYOBON_PLAYER_PASS stages=4 areas=8 checks=156 errors=0。详见 Validation/player-hammer-fix.log 与 PlayerHammerFix/ 下各项结果。
