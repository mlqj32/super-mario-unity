# 通关音乐与换关同步

用户截图：`D:/DevTools/temp/codex-clipboard-58c33967-d935-44a1-b913-ba16acc993a0.png`，已实际打开查看；画面为初代 5-1。音频问题由代码与运行回归定位，截图本身不能验证声音。

## 根因

原流程从 Clear 阶段开始固定经过 211 个模拟帧（约 3.51 秒）就允许换关；实际 Stage Clear 长 5.641497 秒，World Clear 长 6.264127 秒。通关音乐又经通用音效 AudioSource.PlayOneShot 播放，切换关卡没有停止这条通道，因此剩余通关音乐会与下一关背景音乐重叠。最终 8-4 的通关音乐也可能与 Ending 叠播。

修复前回归 `finish-audio-before.log` 覆盖初代／日版的普通关、4-4 城堡、8-4 结局：六种路径全部在第 211 帧提前换关或进入结局，6 项等待检查失败。

## 修复

- 增加独立的非循环通关／结局 AudioSource，以正常音高播放，不继承 Hurry 的背景音乐加速。
- Clear 阶段至少等待音频资源完整时长，并在实际播放模式额外检查音源已结束；同时要求时间奖励全部结算、烟花播放完毕。原有最低结算展示时间保留。
- 静音时仍保持相同通关节奏；无声自动测试依据音频资源长度推进。音频资源缺失时退回最低结算时间，不永久卡住。
- LoadLocation（包括切关、选关、重开、传送）和返回标题统一停止旧背景音乐、音效、通关／结局音源。最终结局在 World Clear 结束后独立播放 Ending，重新开始时停止 Ending。
- 暂停／继续同步处理通关和音效通道。

## 验证

`MarioFinishAudioAudit` 的 89 项确定性检查覆盖两部作品的普通关／城堡／结局，分别开启声音和静音，音源隔离、音高、暂停、完整时长、目标关卡、重开清理、长时间奖励结算、烟花完成和缺失资源退路。

实际 Windows 检查会启用 AudioSource，读取 DSP 时间和播放采样位置，验证即便提前跑完模拟帧也不能抢在音乐之前换关，并验证暂停／继续、静音结局、下一关背景音乐和音效清理。它验证实际音频播放状态与采样推进，不等同于人工听音评价。

专项日志：`_zcode_tmp/finish-audio-focused.log`，输出 `MARIO_FINISH_AUDIO_PASS checks=89 errors=0`。全工程、构建和 Windows 最终结果完成后记录如下。

```text
MARIO_VALIDATION_PASS levels=64 areas=136 nodes=83609 checks=3236 failed=0
MARIO_PHYSICS_REFERENCE_PASS scenarios=12 frames=2160 maxPositionError=0
MARIO_BUILD_PASS errors=0 warnings=0 bytes=170200967
MARIO_FINISH_AUDIO_PASS checks=119 errors=0
MARIO_PLAYER_SMOKE_PASS courses=64 simulatedFrames=46080 errors=0
```

Editor 验证、Windows 构建、实际 Player 三个进程退出码均为 0。既有窗口设置、菜单手柄、动画、玩法、特效、操作、电梯、弹簧和迷宫专项均通过。

实际音频结果：1-1 等待 5.909 秒后换关（测试途中主动暂停约 0.24 秒，音频长 5.641497 秒）；4-4 等待 6.293 秒后进入 5-1；静音 J-8-4 等待 6.293 秒后进入结局（后二者音频长 6.264127 秒）。三者均检测到音频采样推进，旧通关音轨没有带入下一状态；下一关背景音乐正常启动，重玩和返回标题清理通过。

最终日志：`_zcode_tmp/finish-audio-validation.log`、`_zcode_tmp/finish-audio-build.log`、`Validation/player-finish-audio-fix.log`。119 项详情：`Validation/PlayerFinishAudioFix/finish-audio-checks.txt`。已更新 `Builds/Windows/超级马里奥.exe`。
