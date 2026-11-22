# 键盘与手柄全菜单操作

## 修改

- 引入 Unity Input System 1.16.0，输入后端设为 Both，保留现有 IMGUI 鼠标交互。采用南／东／西键等语义控制，替换固定 JoystickButton 编号，支持常见 Xbox、DualShock、DualSense 的设备布局。
- `MarioInput` 统一读取键盘、左摇杆、十字键及按钮。游戏内移动、蹲下、水管、藤蔓、跳跃／游泳、跑步／开火、暂停和死亡快重开均走同一输入快照。
- `MarioMenuNavigation` 用同一份控件定义供绘制和导航。标题、作品选择、64 关选关、暂停、设置、重开确认、结局、死亡及退出确认均可脱离鼠标操作。
- 黄色边框显示焦点；橙色底色保留作品／窗口档位的当前值。支持方向键／WASD、Tab／Shift+Tab、手柄左摇杆／十字键、按住重复、摇杆死区、鼠标与按键切换、返回时恢复父页面焦点。
- 音量行左右每次调整 5%，可长按；全屏时窗口大小控件禁用并跳过；Q/E 或肩键切换作品。菜单底部随输入方式显示提示，PlayStation 使用叉／圈等提示。
- 新增返回标题和退出游戏入口及默认取消的确认框。重开仍默认取消。修正从标题打开选关后“返回”错误停在暂停页的问题。
- 菜单确认不会穿透到角色跳跃／开火；离开窗口清输入，使用中的手柄断开自动暂停，重新连接后可继续。
- Windows 首轮运行发现既有窗口策略依赖当前线程的活动窗口，后台启动时无法挂接。改为枚举本进程的 UnityWndClass 主窗口，继续阻止任意拖拽缩放，保留最大化／还原；不会挂接其他进程或输入辅助窗口。

## 验证方法及边界

`MarioMenuAudit` 创建临时 Unity Input System Keyboard/Gamepad 设备，排队发送真实设备状态事件，再调用生产输入读取函数及菜单处理函数。检查键盘和手柄控制映射、全部 64 关选择、方向导航可达性、按住重复与漂移、音量和显示设置、禁用项跳过、重开与退出取消、死亡快重开、断线／重连，以及输入不穿透。

专项共 206 项。测试设备、输入设置和测试用偏好均在 finally 中恢复，不替换实体设备或用户保存的设置。编辑器测试启用临时的 player input update 特性，避免 Edit Mode 不提供按钮帧边沿；实际 Windows 回归使用正常 Player 输入系统。

这里的手柄自动化使用虚拟 Gamepad；没有实体 Xbox／PlayStation 人工手感验收。菜单“退出游戏”的取消流程被测试，实际退出动作使用 Unity 的 Application.Quit。普通旧式 Joystick、厂商非标准驱动和系统重映射组合尚未逐设备验证。

## 验证记录

- `_zcode_tmp/ui-navigation-focused2.log`：`MARIO_MENU_PASS checks=206 errors=0`。
- `_zcode_tmp/ui-full-validation.log`：`MARIO_VALIDATION_PASS levels=64 areas=136 nodes=83609 checks=3147 failed=0`，运动参考 2160 帧最大位置误差为 0。
- `MARIO_INPUT_CONFIG_PASS backend=Both inputSystem=1.16.0`：项目 YAML 与 manifest／lock 版本一致。

最终构建和实际 Windows 输出（进程退出码均为 0）：

```text
MARIO_BUILD_PASS errors=0 warnings=0 bytes=170191239
MARIO_SETTINGS_PASS checks=38 errors=0
MARIO_MENU_PASS checks=206 errors=0
MARIO_PLAYER_SMOKE_PASS courses=64 simulatedFrames=46080 errors=0
UNITY_YAML_PASS files=65 blocks=72 assetGuids=1723 localReferences=5 externalReferences=69 errors=[]
```

最终日志为 `_zcode_tmp/ui-final-build.log`、`Validation/player-menu-final.log`，专项明细与截图在 `Validation/PlayerMenuFinal/`。窗口挂接／最大化问题修复前的 `PlayerMenuFix` 仅为中间结果，不作为最终通过证据。已有动画、玩法、金币中文、敌人、特效、重开、操作、电梯、弹簧和 4-4 迷宫专项均通过。

已打开检查 `PlayerMenuFinal/menu-title-pad.png`、`menu-settings-pad.png`，标题焦点、设置音量焦点与中文提示正常；同时已查看前一构建相同 UI 的选关和重开确认截图。中文字体新增字符覆盖检查共 142 字，缺字数 0。

交付程序：`Builds/Windows/超级马里奥.exe`，运行时须保留同目录数据文件。
