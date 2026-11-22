# 特效、库巴与火焰显示核查

2026-10-17，Unity 6000.3.15f1，Windows x64。工程 `D:/Unity/project/超级马里奥`。

## 修复原因

1. 火球碰墙、顶撞天花板、击中敌人后原先直接 `alive=false`，没有创建爆炸。现在生成独立三帧 Firework，21 个逻辑帧后清理；地面反弹、离屏清理不误触发爆炸，爆炸不占火球发射名额。
2. 砖块碎片原先被映射到整块 Solid Brick，再裁为 8×8，且与地形同层。现使用已有的专用 8×8 BrickShard，保留地上／地下／城堡配色和镜像翻转动画。
3. 弹出金币与其他瞬时特效改为独立前景层，避免被管道等前景地形遮住。现有金币四帧、普通金币三种图像、CoinPop → “200” → 清理均通过实际渲染检查。
4. 补充敌人击败、龟壳启动、道具取得、1UP 和旗杆得分飘字。飘字只显示已有得分，不额外重复计分。连锁踩壳／顶砖遍历改为有界索引，避免生成飘字时修改集合导致异常。
5. 库巴喷火补充上下翻转动画。库巴朝向按玩家位置决定，喷火从对应方向嘴部生成，避免身体朝向与火焰方向相反。熔岩火球下落时也使用翻转姿态。
6. 实际地图测试发现日版库巴出生时脚部嵌入桥／地面 8 像素，水平碰撞会把它沿整排地形挤到屏幕外并删除。现于生成运行时对象时对齐脚下地形，覆盖日版 8-4 的两只库巴；没有改写可编辑关卡数据。
7. 补充原先缺失的通关烟花及水下气泡。初代烟花按时间末位 1／3／6 触发；日版按时间与金币末位相同触发，奇数 3 发、偶数 6 发。每发烟花增加 500 分，播放结束后才切关。气泡从水下角色处产生、上升并在水面清理。
8. 下旗、断桥、结算和死亡阶段继续推进瞬时特效，避免它们被冻结。暂停仍冻结逻辑。
9. 日版断桥原先改变碰撞宽度时，独立地形贴图仍绘制整格。原始地形渲染现在尊重运行时宽高，断桥的 8 像素缩短能正确显示。

实现主要位于 `MarioEffects.cs`、`MarioActors.cs`、`MarioGame.cs`、`PixelLibrary.cs`。新增特效使用项目已有参考素材，没有替换成任意绘制的爆炸或火焰。

## 检查覆盖

| 对象 | 本轮检查 |
| --- | --- |
| 火球与爆炸 | 四帧飞行；左右墙、天花板、Goomba、Beetle、BeetleShell、BulletBill、Bowser 命中；三帧爆炸像素；清理和发射名额 |
| 金币 | 地上／地下／城堡弹出四帧，转为“200”及消失，普通金币三种图像，HUD 沿用金币中文专项 |
| 碎片与得分 | 四片专用贴图、三类配色、两种镜像姿态；击败／道具／1UP 飘字；连锁得分不异常 |
| 库巴 | 两部作品共 17 只，包括迷宫区域与日版 8-4 两只；身体／嘴部四姿态、地形站立、嘴部喷火、方向；五发火球受击与倒置死亡 |
| 火焰与投射物 | 库巴喷火两姿态、城堡火棒四帧、熔岩火球升降姿态、锤子四帧；放在城堡墙前逐像素检查遮挡 |
| 道具动画 | 火焰花、星星四帧；既有蘑菇出砖、生存、人物星星调色与发射姿态回归继续通过 |
| 其他瞬时效果 | 水下气泡上升与消失、两部作品烟花触发规则、六发烟花播放与切关；下旗／断桥／结算／死亡阶段的特效清理 |
| 断桥 | 初代及日版斧头触发、可见桥面缩短、库巴下落 |

`MarioEffectsAudit.cs` 共 **386 项**。像素检查读取实际相机输出，按精灵位置、透明区域和水平／垂直翻转与源图逐像素比对；没有用“资源存在”代替显示检查。

测试包含平地隔离场景、定点进入真实地图和直接推进库巴逻辑。它们不代表 64 关正常路线完整通关，也不是 NES 整机逐周期时序证明。

## 最终实际输出

```text
MARIO_VALIDATION_PASS levels=64 areas=136 nodes=83609 checks=2267 failed=0
MARIO_PHYSICS_REFERENCE_PASS scenarios=12 frames=2160 maxPositionError=0
MARIO_BUILD_PASS errors=0 warnings=0 bytes=168905392
MARIO_EFFECTS_PASS checks=386 errors=0
MARIO_PLAYER_SMOKE_PASS courses=64 simulatedFrames=46080 errors=0
MARIO_GAMEPLAY_AUDIT_PASS checks=713 failed=0
MARIO_COIN_CHINESE_JAPAN_PASS checks=189 errors=0
MARIO_LAKITU_PASS checks=17 errors=0
MARIO_PIRANHA_PASS checks=21 errors=0
MARIO_SETTINGS_PASS checks=28 errors=0
MARIO_SETTINGS_RESTART_PASS volume=0.35 fill=True fullscreen=False
MARIO_POWERUP_ANIMATION_PASS mushroomSurvivalFrames=81 enemies=4 renderedFrames=24 errors=0
MARIO_CASTLE_ANIMATION_PASS renderedFrames=24 runCells=3 flameCells=4 starPalettes=4 crouchHeight=22 errors=0
UNITY_YAML_PASS {"pass_": true, "files": 65, "blocks": 72, "assetGuids": 1672, "localReferences": 5, "externalReferences": 69, "errors": []}
```

最终 Unity 验证、构建、Windows Player、设置重启回归及结构检查退出码均为 0。

## 证据

- `PlayerEffectsFix/effects-checks.txt`：386 项实际 Player 逐项结果。
- `player-effects-fix.log`、`player-effects-settings-restart.log`：实际 Player 日志。
- `../_zcode_tmp/effects-verified-validation.log`：最终编译验证。
- `../_zcode_tmp/effects-build.log`：最终构建。
- 已实际打开查看 `PlayerEffectsFix/explosion-0.png`、`explosion-7.png`、`explosion-14.png`，三种爆炸姿态完整可见。
- 已实际打开查看 `coin-shards-Castle.png`、`coin-score-Overworld.png`、`finish-firework.png`。
- 已实际打开查看 `boss-J-1-4-0.png`、`boss-1-4-8.png`、`boss-J-8-4-8.png`，库巴站在地形上、嘴部火焰可见。

更新后的程序：`../Builds/Windows/超级马里奥.exe`，同目录数据文件需保留。
