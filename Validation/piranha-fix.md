# 食人花管道遮挡修复

2026-10-17，Unity 6000.3.15f1，Windows x64。

## 原因与修改

用户截图 `D:/DevTools/temp/codex-clipboard-c346b66c-ec39-49f6-85cd-400259d83fb9.png` 已实际打开检查，显示食人花在管道内部仍整株可见。

日版管道由 `spriteOverride` 地形块绘制，排序为 0；普通敌人的排序为 1，因此原先食人花会画在日版管道前方。初代具名 Pipe 对象排序为 2，旧检查未覆盖日版这一差异。

- 食人花独立置于实体地形之后。管内部分由管道及周围地形遮住，管外部分保留正常的伸缩与贴图动画；其他小怪、火焰的排序不变。
- 复测发现日版倒挂花的原始节点位置采用管口贴图上边缘，实际开口在下边缘，相差 16 像素。生成运行时对象时依据实际管口地形下沿修正锚点，避免花伸出一半仍藏在管内。
- 没有重导入、覆盖或直接改写可编辑关卡资源；本次修改位于 `Assets/Scripts/MarioGame.cs`。

## 复现与验证

新增 `MarioPiranhaAudit.cs`，使用实际关卡 1-2、J-1-1、J-5-1。对缩回、半伸出、完全伸出三种姿态，分别渲染有食人花和隐藏食人花的场景，比较像素差异，确认管内没有泄露像素且管外确实可见；另检查锚点与管口是否对齐。

修复前重现 4 项遮挡失败：日版普通、倒挂花缩回时各泄露 188 像素，半伸出时各泄露 104 像素。仅修正绘制层级后的复测暴露了倒挂锚点问题。最终 21 项专项检查全部通过。

```text
MARIO_VALIDATION_PASS levels=64 areas=136 nodes=83609 checks=1844 failed=0
MARIO_PHYSICS_REFERENCE_PASS scenarios=12 frames=2160 maxPositionError=0
MARIO_BUILD_PASS errors=0 warnings=0 bytes=168847360
MARIO_PIRANHA_PASS checks=21 errors=0
MARIO_LAKITU_PASS checks=17 errors=0
MARIO_GAMEPLAY_AUDIT_PASS checks=713 failed=0
MARIO_COIN_CHINESE_JAPAN_PASS checks=171 errors=0
MARIO_POWERUP_ANIMATION_PASS mushroomSurvivalFrames=81 enemies=4 renderedFrames=24 errors=0
MARIO_CASTLE_ANIMATION_PASS renderedFrames=24 runCells=3 flameCells=4 starPalettes=4 crouchHeight=22 errors=0
MARIO_PLAYER_SMOKE_PASS courses=64 simulatedFrames=46080 errors=0
UNITY_YAML_PASS {"pass_": true, "files": 65, "blocks": 72, "assetGuids": 1648, "localReferences": 5, "externalReferences": 69, "errors": []}
```

最终 Unity 编译验证、Windows 构建、实际 Player 和结构检查退出码均为 0。64 关结果为自动运行回归，不代表逐关正常路线完整通关。

## 画面与日志

实际打开查看了以下 Player 截图：

- `PlayerPiranhaFix/piranha-1-0.png`：日版普通花完全缩回，管壁无花朵穿透。
- `PlayerPiranhaFix/piranha-1-1.png`：日版普通花半伸出。
- `PlayerPiranhaFix/piranha-1-2.png`：日版普通花完全伸出。
- `PlayerPiranhaFix/piranha-2-2.png`：日版倒挂花从管口下沿伸出。

专项逐项结果：`PlayerPiranhaFix/piranha-checks.txt`。实际运行日志：`player-piranha-fix.log`。

修复前日志：`../_zcode_tmp/piranha-before.log`；最终 Unity 验证：`../_zcode_tmp/piranha-final-validation.log`；构建：`../_zcode_tmp/piranha-build.log`。

更新后的程序：`../Builds/Windows/超级马里奥.exe`，同目录数据文件需保留。
