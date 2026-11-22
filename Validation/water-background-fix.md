# 初代水关背景修复

2-2 截图中的整片浅蓝背景源于水层缺失。参考项目通过区域属性的 `stretches` 添加全画面水体和波纹，导入工具只导出了 `creation` 列表，漏掉了这一层。原有 `Scenery Water Underwater` 贴图仍在工程中，但实际没有被绘制。

运行时现在自动补齐缺失的水层：顶部在世界高度 176，向下覆盖到画面底部，排序在所有关卡物体后面；水体使用参考贴图中的深蓝色，顶部使用原有白色波纹。水层随镜头覆盖 256 像素视野，滚动到关卡末端也没有空缺。水下城堡同时修正区域底色的选择，使其与参考的水下设置一致。

关卡编辑器预览使用相同的水层判断。已有显式 Water 节点或日版原生瓦片的区域保留原有绘制。水层是独立视觉对象，不参与碰撞、敌人逻辑、出口或地图长度计算，也不会写入可编辑关卡数据。

验证入口：Unity 菜单 **Mario → Validate underwater backgrounds**，或 `-executeMethod MarioWaterBackgroundValidation.Run`。

**644 项检查通过**：所有区域的水层切换、初代 5 个水下区域、日版 6 个水下入口、镜头起点／中段／末端的整帧像素比对、水面边界专项回归及 64 个关卡资源的数据保持检查。检查记录见 [checks.txt](WaterBackground/checks.txt)。

实际渲染：[2-2](WaterBackground/2-2-1.png)、[5-2](WaterBackground/5-2-3.png)、[6-2](WaterBackground/6-2-6.png)、[7-2](WaterBackground/7-2-1.png)、[8-4 水下城堡](WaterBackground/8-4-4.png)。

最新包已统一保留为 `Builds/Windows/超级马里奥.exe`，包含此前旗杆、平衡绳线和游泳边界修复。其余历史构建已清理。
