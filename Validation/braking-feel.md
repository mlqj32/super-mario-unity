# 制动与空中控制调整

用户反馈加速容易冲过头、刹不住。旧版地面满速松手要 15 步（约 250ms），向右滑 16.9375px、向左滑 17.8125px；空中无输入直接返回，完全不减速。此外空中最高速度由当前速度决定，松开加速键不能降速。

参考 [蔚蓝公开 Player.cs](https://github.com/NoelFB/Celeste/blob/master/Source/Player/Player.cs) 的普通移动分支：MaxRun=90、RunAccel=1000、AirMult=0.65。普通无特殊状态的松手制动理论时间为地面 90ms、空中约 138ms；RunReduce=400 用于同向超速回落，并不是普通松手摩擦。这里借鉴主动制动与空中可调整的设计，没有照搬冲刺、攀墙或整套跳跃系统。

马里奥采用满跑速地面 6 步（约 100ms）、空中 9 步（约 150ms）停稳；反向输入分别最多 4/6 步刹住旧方向。减速使用固定步长并夹到目标值，不会越过零或残留微小漂移。空中也根据当前加速键选择走速/跑速，松开加速键继续按方向最多 4 步降到走速。

起步加速保持地面 18 步、空中 24 步；走速/跑速上限仍为 1.5/2.5px 每步，持续按方向的助跑跳跃距离继续回归检查。150ms 预输入、100ms 离地宽限和短/长跳规则保留。水中和可选严格原始模式未应用新制动。

验证包含左右两向、走/跑、地面/空中松手距离与时间、停稳不反弹、反向制动距离、空中松开/重新按加速键、保持加速跳跃的距离。制动上限要求：地面不超过半格（8px），空中不超过 10.5px。自动测试验证运动和输入规则，不等同于真人对手感的最终评价，也不代表 64 关人工通关。

本轮验证日志：`_zcode_tmp/braking-validation.log`、`_zcode_tmp/braking-build.log`、`Validation/player-braking-fix.log`。

## Unity 实测

| 满跑速场景 | 修复前 | 修复后 |
| --- | --- | --- |
| 地面松手到停稳 | 15 步，16.9375～17.8125px | 6 步，6.0625～6.375px |
| 空中松手到停稳 | 持续滑行，无主动制动 | 9 步，9.75～10.25px |
| 地面反向消除旧速度 | 最多 9 步 | 4 步，旧方向滑行 3.75px |
| 持续按方向与加速的完整跳跃 | 142.5px | 142.5px |

范围对应左右两个方向；原有有符号速度字节向下取整会产生小于 1px 的累计方向差异。步行地面松手为 4 步、1.875～2.0625px。起步和原关卡所需最高速度没有提高。

```text
MARIO_VALIDATION_PASS levels=64 areas=136 nodes=83609 checks=2796 failed=0
MARIO_PHYSICS_REFERENCE_PASS scenarios=12 frames=2160 maxPositionError=0
MARIO_BUILD_PASS errors=0 warnings=0 bytes=168965976
MARIO_CONTROLS_PASS checks=91 errors=0
MARIO_PLAYER_SMOKE_PASS courses=64 simulatedFrames=46080 errors=0
```

原始模式的零误差只验证保留的原始分支，不代表本轮主动制动与 NES 相同。

实际 Windows Player 的专项明细见 `PlayerBrakingFix/controls-checks.txt`。既有动画、玩法 777 项、金币中文 211 项、朱盖姆 17 项、食人花 46 项、炮台 166 项、设置 38 项、特效 386 项、重开 153 项均通过。验证、构建及 Player 进程最终退出码均为 0。
