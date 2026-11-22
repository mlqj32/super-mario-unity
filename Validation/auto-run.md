# 自动加速设置

新增于 2026-10-17，位于标题／暂停菜单的设置页，画面比例选项下方。初代／日版二代默认关闭，开启后键盘方向键、手柄摇杆／十字键移动时自动进入跑速，免去持续按住加速键。

猫里奥采用独立的原版 30 Hz 控制模型，没有独立的走／跑键；方向输入本身会沿原版曲线逐步加速，所以它不读取这个开关，也不会因为设置置灰而退回匀速移动。设置页会显示“猫里奥方向键使用原版自动加速”。

- 点击、回车或手柄确认键切换；焦点移至该项时左键关闭、右键开启。
- 保存于独立的 `autoRun` 偏好键，跨新游戏、复活与程序重启保留。
- 自动加速只影响 `Controls.run`，不产生 `firePressed`；火焰形态仍按原开火键发射，按住也不会因自动加速产生连发。
- 松开方向正常制动；跳跃、菜单输入隔离及手动加速键功能保留。开启后暂停页操作提示显示自动加速与开火键。
- 新增的“自／闭／火”字形由现有 Fusion Pixel 字体生成，145 个中文字符检查通过，缺字为 0。

使用既有 `RunSettingsAudit` 和虚拟设备 `RunMenuAudit` 验证初代／日版二代的默认值、开关保存／读取、键盘与手柄菜单操作、双向走速／跑速、松方向停止、跳跃、手动开火、无自动开火、新游戏／复活保留设置；`SyobonAudit` 连续方向输入回归验证猫里奥的速度从首帧逐步提高且 `UsesAutomaticAcceleration=true`。新增成品重启入口 `-mario-auto-run-reload` 仅使用测试偏好键，检查新进程读取设置后能自动跑且不会开火。

虚拟手柄输入测试不等于实体手柄人工验收。

## 验证结果

```text
MARIO_VALIDATION_PASS levels=64 areas=136 nodes=83609 checks=3740 failed=0
MARIO_PHYSICS_REFERENCE_PASS scenarios=12 frames=2160 maxPositionError=0
MARIO_BUILD_PASS errors=0 warnings=0 bytes=170222023
MARIO_SETTINGS_PASS checks=50 errors=0
MARIO_MENU_PASS checks=245 errors=0
MARIO_PLAYER_SMOKE_PASS courses=64 simulatedFrames=46080 errors=0
MARIO_AUTO_RUN_RESTART_PASS enabled=True run=True fire=False
```

Editor 验证、构建、成品 smoke 和新进程重启检查均退出 0。日志：`_zcode_tmp/auto-run-validation.log`、`_zcode_tmp/auto-run-build.log`、`Validation/player-auto-run.log`、`Validation/player-auto-run-reload.log`。

已实际查看 `Validation/PlayerAutoRun/menu-settings-pad.png` 与 `Validation/PlayerAutoRunReload/auto-run-after-restart.png`：自动加速选项完整显示，中文无缺字，未与窗口档位或底部导航提示重叠，重启后仍为开启。测试使用专用偏好键，不修改正常游戏的用户设置。
