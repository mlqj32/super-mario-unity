# 岩浆火球停在最高点修复

已实际打开用户截图 D:/DevTools/temp/codex-clipboard-8c26e082-2408-4fa7-8c56-295c07e23ec4.png，问题对应从岩浆跃出的 Podoboo。

## 原因与修改

初速度 5.5、每步重力 0.125，44 步后竖直速度恰好为 0。原代码仅在 vy != 0 时更新位置和重力，因此最高点停止计算，直到下一轮周期直接重置。

- 重力更新条件增加“仍在岩浆等待高度之上”，最高点速度为 0 时也继续受重力。
- 下降至 -32 后停止，保留原 150 步周期；等待期间隐藏火球并跳过伤害判定，下一次跃出恢复显示。
- 未更改初速度、重力、周期、地图、旋转火棒或玩家发射的火球。
- 新周期测试纳入 RunEffectsAudit，验证连续三轮、最高点、下降贴图、入池隐藏、再次出现、暂停恢复及初代/日版实际地图实例。

## 实测

- 修复前：MARIO_PODOBOO_FAIL checks=68 errors=57，见 _zcode_tmp/podoboo-before.log。
- 修复后 Unity：MARIO_PODOBOO_PASS checks=68 errors=0，见 _zcode_tmp/podoboo-after.log。
- 构建：MARIO_BUILD_PASS errors=0 warnings=0 bytes=191869703，见 _zcode_tmp/podoboo-build.log。
- Windows：MARIO_PODOBOO_PASS checks=68 errors=0；MARIO_EFFECTS_PASS checks=454 errors=0（包含上述 68 项）。进程退出 0，见 Validation/player-podoboo-fix.log。
- 72 份关卡/区域 SHA256 均未改变，见 podoboo-map-integrity.json。本轮运行火球与相关特效专项，没有重新运行全部游戏专项。

已实际打开 PlayerPodobooFix/podoboo-cycle-044.png、060.png、100.png，分别确认最高点、向下移动并翻转贴图、回到岩浆后消失。另保存 020/151 帧截图，配合周期断言验证再次跃出。

最新文件：D:/Unity/project/超级马里奥/Builds/Windows/超级马里奥.exe。
