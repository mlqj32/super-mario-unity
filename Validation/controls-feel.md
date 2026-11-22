# 操作手感与输入容错

参考工程只读：`D:/Unity/project/LuoWei`。没有修改其游戏代码。

参考 `Assets/Scripts/Gameplay/Platformer2DMovement.cs` 的 0.15 秒 jumpBufferingTime、0.1 秒 coyoteTime 和地面加减速设计，以及 `Assets/Scripts/Gameplay/Player/PlayerMovement.cs` 的菜单关闭后避免确认键误触跳跃的处理。

## 默认玩法

- 预输入保存 9 个约 60Hz 模拟步（约 150ms）。在落地前按下并松开也会执行短跳，继续按住则执行高跳。落地碰撞确立支撑时当步消耗输入，移动平台同样支持。
- 走出边缘后有 6 个模拟步（约 100ms）的补跳机会。真正起跳、踩怪或弹簧弹起会消耗宽限，不提供二段跳。
- 平地从静止到原有步行/跑步速度上限约 18 步（300ms）。满速松开方向最多 6 步（100ms）停稳，反向最多 4 步消除旧方向速度后向新方向加速。步行和跑步最高速度仍为 1.5 和 2.5 像素/步。
- 空中松手最多 9 步（150ms）停稳，反向最多 6 步消除旧方向速度；松开加速键继续保持方向，最多 4 步降回走速，空中也能重新开始加速。方向输入立即改变朝向，火球使用同一帧方向。水中继续使用原有横向规则与一次按键一次划水。
- 原有短跳/高跳竖直规则保留，不修改重力去制造悬浮。
- 暂停、死亡、重开、换区、管道传送清空预输入；菜单共用按键不会穿透触发跳跃。变身停顿期间仅保留尚未超时的按键。

`MarioControls.cs` 管理跳跃意图，`ClassicMotor.responsive` 提供可选横向响应；`MarioGame.responsiveControls` 默认为 true。原有严格运动模式保留作对照：在 Inspector 取消 Responsive Controls 可恢复旧横向运动和严格跳跃时机。原始轨迹零误差测试只适用于该模式，不代表默认响应优化玩法与 NES 逐帧一致。

## 验证范围

本轮制动依据与最新验证见 [蔚蓝方向的制动调整](braking-feel.md)。下列日志与结果是上一轮预输入改造的基线。

`MarioControlsAudit` 使用真实碰撞和模拟步检查落地前 0/1/4/8 步的有效预输入、过期输入、按住/松开跳跃高度、边缘后 1/3/6 步补跳和超时拒绝、禁止二段跳与自动连跳、双向步行/跑步加速和停止、反向、空中发射、移动平台、弹簧、踩怪、水中划水、暂停/死亡/换区/传送/重开清理、变身停顿和原始模式。

日志：`_zcode_tmp/controls-validation.log`、`_zcode_tmp/controls-build.log`、`player-controls-fix.log`。Windows Player 的专项明细与实际模拟轨迹保存在 `PlayerControlsFix/controls-checks.txt`、`PlayerControlsFix/controls-trace.csv`。自动检查覆盖按键规则和运动结果，不替代玩家对手感的主观反馈。

Unity 验证输出：`MARIO_VALIDATION_PASS levels=64 areas=136 nodes=83609 checks=2750 failed=0`。原始模式独立对照：`MARIO_PHYSICS_REFERENCE_PASS scenarios=12 frames=2160 maxPositionError=0`。

最终 Windows 构建和实际运行输出：

```text
MARIO_BUILD_PASS errors=0 warnings=0 bytes=168963416
MARIO_CONTROLS_PASS checks=45 errors=0
MARIO_PLAYER_SMOKE_PASS courses=64 simulatedFrames=46080 errors=0
```

启动场景验证默认开启响应优化；旧动画、玩法、金币、朱盖姆、食人花、炮台、设置、特效和重开回归均通过。专项实测预输入短跳最高 20.3125px、按住跳跃最高 66px；第 6 步离地补跳接受，第 7 步拒绝。已打开检查实际 Player 的 `controls-buffered-jump.png` 和 `controls-ledge-jump.png`。64 关自动回归不是人工通关，按键规则验证使用真实游戏模拟函数，不声称已人工完成键盘/手柄全流程体验。
