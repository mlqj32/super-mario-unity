# 画面铺满、音量设置与窗口控制

2026-10-17，Unity 6000.3.15f1，Windows x64。

## 使用方式

- 标题页、暂停菜单均有“设置”。音量滑块及加减按钮实时调整总音量，静音保留原音量。
- 全屏／窗口按钮切换无边框全屏与普通窗口；快捷键为 F11 或 Alt+Enter，M 静音。
- 默认“铺满”：完整 256×240 画面拉伸到客户区，没有外侧黑边，HUD 不裁切。宽屏会横向拉宽；选择“原始比例”可恢复原比例并留边，不再强制向下取整倍数。
- 窗口有小窗 960×540、标准 1280×720、大窗 1600×900 三个预设。桌面尺寸不足时按比例缩小到可用范围。
- Windows 保留标题栏、最大化和还原，允许标题栏双击最大化；禁止拖拽八个窗口边缘／角自由缩放，也阻止系统菜单的 Size 命令。
- 音量、静音、显示模式、画面比例、窗口预设自动保存，重启后恢复。首次使用默认无边框全屏、铺满、70% 音量。
- 设置内按 Esc 返回上一页；从暂停菜单进入设置期间，游戏保持暂停。

## 实现

旧黑边来自按固定宽高比取整数倍缩放。显示矩形现在支持铺满和原始比例，GUI 使用同一变换映射鼠标坐标。256×240 世界渲染、摄像机范围和物理步进未随窗口尺寸改变。

`MarioSettings.cs` 管理设置和持久化；`WindowsWindowPolicy.cs` 仅在 Windows Player 挂接当前进程的窗口消息，阻止边框尺寸命中及 SC_SIZE，不移除最大化功能，不挂接 Unity 编辑器。销毁时恢复原窗口过程。构建继续启用 Unity resizableWindow 以保留系统最大化能力，手动拖拽限制由上述消息处理实现。

中文字体重新检查：111 个所需汉字全部覆盖。实际截图发现原 ASCII 字库缺少百分号，已补充 8×8 `%`，并加入资源检查；现有 43 个数字、字母及标点字形。

没有重新导入或覆盖可编辑关卡。

## 最终实际输出

```text
MARIO_VALIDATION_PASS levels=64 areas=136 nodes=83609 checks=1881 failed=0
MARIO_PHYSICS_REFERENCE_PASS scenarios=12 frames=2160 maxPositionError=0
MARIO_BUILD_PASS errors=0 warnings=0 bytes=168885424
MARIO_SETTINGS_PASS checks=28 errors=0
MARIO_SETTINGS_RESTART_PASS volume=0.35 fill=True fullscreen=False
MARIO_PLAYER_SMOKE_PASS courses=64 simulatedFrames=46080 errors=0
MARIO_GAMEPLAY_AUDIT_PASS checks=713 failed=0
MARIO_COIN_CHINESE_JAPAN_PASS checks=189 errors=0
MARIO_LAKITU_PASS checks=17 errors=0
MARIO_PIRANHA_PASS checks=21 errors=0
MARIO_POWERUP_ANIMATION_PASS mushroomSurvivalFrames=81 enemies=4 renderedFrames=24 errors=0
MARIO_CASTLE_ANIMATION_PASS renderedFrames=24 runCells=3 flameCells=4 starPalettes=4 crouchHeight=22 errors=0
UNITY_YAML_PASS {"pass_": true, "files": 65, "blocks": 72, "assetGuids": 1670, "localReferences": 5, "externalReferences": 69, "errors": []}
MARIO_DISPLAY_CONFIG_PASS {"defaultScreenWidth": 1280, "defaultScreenHeight": 720, "resizableWindow": 1, "allowFullscreenSwitch": 0, "fullscreenMode": 1}
```

最终 Unity 验证、构建、Player、独立重启验证和结构检查的退出码均为 0。Player 检查包括八个边缘的原生命中消息、实际最大化／还原、全屏往返、四边像素与游戏渲染目标比对、音量边界、静音恢复、设置读回和暂停状态。

重启验证启动第二个 Player 进程，读回第一进程保存的 35% 音量、铺满、窗口模式和小窗预设。自动测试使用独立的 `Mario.Tests.Settings.` 键，不改写正常用户的 `Mario.Settings.` 偏好，重启检查后清理测试键。

这些是自动回归与受控画面验证，不等同于 64 关正常路线完整通关。

## 证据

- `../_zcode_tmp/settings-final-validation.log`：最终 Unity 编译验证。
- `../_zcode_tmp/settings-final-build.log`：最终 Windows 构建。
- `player-settings-final.log`、`player-settings-final-restart.log`：实际运行和第二进程重启日志。
- `PlayerSettingsFinal/settings-checks.txt`：28 项逐项结果。
- 已实际打开检查最终 `PlayerSettingsFinal/settings-window.png`、`fullscreen-fill.png`、`settings-after-restart.png`；设置页百分号完整，重启后显示 35%，全屏游戏覆盖四边。
- 最大化截图 `PlayerSettingsFinal/maximized-fill.png`；补百分号前同一窗口实现的 `PlayerSettingsFix/maximized-fill.png` 也已实际打开检查。
- 更新后的运行程序：`../Builds/Windows/超级马里奥.exe`，同目录数据文件需保留。
