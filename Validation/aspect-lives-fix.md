# 默认比例与初始生命调整

2026-10-17，Unity 6000.3.15f1。

- 默认保持原始 256:240（16:15）像素画面比例。小窗 512×480、标准 768×720、大窗 1024×960；屏幕不足时等比缩小。全屏和最大化使用比例适配，宽屏两侧留边。
- 旧安装首次运行新版时，将之前默认拉伸的保存值迁移为原始比例，保留音量、全屏/窗口与窗口档位。之后主动选择铺满仍可保存。
- 初代及日版二代新游戏、选关、游戏结束后再开局均为 10 条命。场景启动也重置生命，兼容旧场景序列化的 3 条命和编辑器直接试玩。
- 死亡一次剩 9 条命；手动重玩本关保留当前命数，不会补满。

验证覆盖设置首次迁移、后续偏好保存、三档窗口无拉伸、64 关初始生命和死亡扣命、重开确认与最后一命游戏结束。实际 Windows 截图已打开检查：`PlayerAspectLivesFix/window-original-ratio.png` 和 `PlayerAspectLivesFix/restart-death-prompt.png`，分别显示无拉伸的 768×720 窗口和剩余生命 9。

Unity 输出：`MARIO_VALIDATION_PASS levels=64 areas=136 nodes=83609 checks=2514 failed=0`。

最终 Windows 构建与实际运行输出：

```text
MARIO_BUILD_PASS errors=0 warnings=0 bytes=168944464
MARIO_GAMEPLAY_AUDIT_PASS checks=777 failed=0
MARIO_SETTINGS_PASS checks=38 errors=0
MARIO_RESTART_PASS checks=153 errors=0
MARIO_PLAYER_SMOKE_PASS courses=64 simulatedFrames=46080 errors=0
MARIO_SETTINGS_RESTART_PASS volume=0.35 fill=False fullscreen=False
```

最终构建的 `window-original-ratio.png` 与独立重启后的 `settings-after-restart.png` 已打开检查，确认窗口无拉伸，重启后仍为原始比例。64 关运行属于自动回归，并非人工逐关通关。

构建结果见 `build-result.txt`，编译/构建日志见 `_zcode_tmp/aspect-lives-validation.log`、`_zcode_tmp/aspect-lives-build.log`。实际 Player 回归见 `player-aspect-lives-fix.log`、`PlayerAspectLivesFix/settings-checks.txt`、`PlayerAspectLivesFix/player-result.txt`；独立进程设置重启验证见 `player-aspect-lives-reload.log`。
