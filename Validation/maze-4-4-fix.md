# 初代 4-4 迷宫路线判定修复

## 原因与范围

上一轮“先上后下”的回答只核对了配置，没有跑实际路线。实际回归复现了用户遇到的循环：第一段上层长台走到断口后，未能保留通过结果。

原先第一段通过区为全局 x=1044..1124、y=112..160，失败区为 x=1024..1104、y=0..48。上层长台在 x=976 结束，下一个平台始于 x=1056，中间有 80 像素断口，头顶还有低天花板。导入的 FullScreenMario 地图使用不同的运动/碰撞实现。当前原生运动下，断口边跳跃的 6 个“体型 × 操作模式”用例全部失败；小个子在某次跳跃中短暂触发通过，随后下落又触发失败，大个子和火力形态未触发通过。直接从断口后的上层平台出发则能正常进入下一段，说明并非通往下一段的触发器完全失效。

## 修改

- 第一段上下路线检测统一移至上层长台末端，全局 x=944..976（段内 x=688，宽 32）。上下检测的高度保持原值。上层被正确记录后，再落下穿过出口不会被底层检测覆盖；第一段全程走下层仍会循环。
- 同步修改可编辑的 `Levels/4-4.asset`、参考 JSON 和导入工具，避免显式恢复参考关卡时带回旧判定。未运行批量导入或工程重置。
- 第二段仍要求最底层通道；中层、上层都不算通过。路线仍为“第一段上层 → 第二段最底层”。第一段要经过上层最后一段长台，第二段要在三层通道处走地面。

这是针对原生运动的关卡判定兼容修正，不代表已与 NES 4-4 全程逐帧对齐。未改动平台几何、底层运动参数或其他关卡。

## 回归范围

`MarioMazeAudit` 54 项检查覆盖默认响应操作和原始操作，小个子、大个子、火力形态；第一段走上层/断口边跳跃/走错下层，第二段三个通道，以及“错误下层 → 重试上层 → 下一段下层 → 生成库巴终点区域”。测试使用实际地形碰撞和真实触发器，不直接设置 `sectionPassed`。为隔离迷宫问题，将角色放在各段路线判定前，危险物伤害被暂时屏蔽；这不是从出生点到库巴的无伤全程机器人通关。

修复前证据：`_zcode_tmp/maze-gap-before.log`（24 项中 6 项失败）；修复后专项：`_zcode_tmp/maze-final-focused.log`（54 项全部通过）。全工程和实际 Windows 运行结果另见本目录最新验证输出。

## 本次完成的验证

```text
MARIO_MAZE_PASS checks=54 errors=0
MARIO_VALIDATION_PASS levels=64 areas=136 nodes=83609 checks=2932 failed=0
MARIO_PHYSICS_REFERENCE_PASS scenarios=12 frames=2160 maxPositionError=0
MARIO_BUILD_PASS errors=0 warnings=0 bytes=168988008
UNITY_YAML_PASS files=65 blocks=72 assetGuids=1704 localReferences=5 externalReferences=69 errors=[]
MARIO_PLAYER_SMOKE_PASS courses=64 simulatedFrames=46080 errors=0
```

全工程日志：`_zcode_tmp/maze-final-validation.log`；构建日志：`_zcode_tmp/maze-build.log`。实际 Windows 程序运行日志：`Validation/player-maze-fix.log`；54 项迷宫运行明细：`Validation/PlayerMazeFix/maze-checks.txt`。既有动画、敌人、特效、重开、操作、电梯和弹簧检查均通过。Windows 运行退出码为 0。

交付：`Builds/Windows/超级马里奥.exe`。关闭旧版游戏后重新打开此程序，从选关重新进入初代 4-4，避免继续运行旧进程内的关卡数据。
