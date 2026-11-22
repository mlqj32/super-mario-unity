# 猫里奥消息框正文修复

用户截图：D:/DevTools/temp/codex-clipboard-56c2c02f-9ce3-4c10-a672-646cf3a94906.png。

## 原因与修改

SyobonEngine 在正文绘制后的下一帧启动关闭动画，用空黑框覆盖正文，此时才 WaitKey 暂停，导致等待期间一直显示空框。将等待移到 type 2 完整正文绘制之后，确认后再继续 type 3 关闭动画。中文字库完整，无需更换。

替换旧的仅验证关闭状态等待的测试：从实际第二关提示砖触发，另覆盖全部 8 类提示、24 行正文的渲染像素、中文字形资源和宽度。每个提示等待 60 步后画面与人物位置保持不变，确认可关闭，重试可清除等待。

## 实测结果

- 修复前：SYOBON_VALIDATION_FAIL checks=150 errors=24；全部正文行 whitePixels=0。见 _zcode_tmp/dialog-before-checks.txt 与 dialog-before.log。
- 修复后：SYOBON_VALIDATION_PASS checks=150 errors=0。见 Validation/Syobon/checks.txt 与 _zcode_tmp/dialog-after.log。
- MARIO_BUILD_PASS errors=0 warnings=0 bytes=191854855。见 _zcode_tmp/dialog-build.log。
- Windows：MARIO_MENU_PASS checks=270 errors=0；SYOBON_PLAYER_PASS stages=4 areas=8 checks=156 errors=0；SYOBON_INTEGRATION_PASS errors=0。进程退出码 0，见 Validation/player-dialog-fix.log。
- SHA256 对比：72 份关卡/区域文件均未改变，见 dialog-map-integrity.json。

已实际打开并检查 Validation/Syobon/dialog-stage-two-sign.png 和 Validation/PlayerDialogFix/syobon-dialog.png，中文正文和继续提示可见。自动化确认经过共同输入接口；本次未进行实体手柄人工测试。

最新可执行文件：D:/Unity/project/超级马里奥/Builds/Windows/超级马里奥.exe。
