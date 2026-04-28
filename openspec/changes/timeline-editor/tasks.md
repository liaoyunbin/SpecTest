# 时间轴编辑器 —— 实现任务

## 第一阶段：数据模型与基础框架

### 1. 创建目录结构
- [ ] 创建 `Assets/Scripts/TimelineEditor/` 目录
- [ ] 创建 `Data/`、`Core/`、`UI/`、`Interaction/` 子目录

### 2. 实现数据模型
- [ ] 创建 `Clip.cs` — id, name, start, duration, color, data
- [ ] 创建 `Track.cs` — id, name, isMuted, isLocked, clips 列表
- [ ] 创建 `Group.cs` — id, name, color, isExpanded, tracks 列表
- [ ] 创建 `TimelineData.cs` — name, duration, frameRate, groups 列表
- [ ] 为所有数据类添加 `[Serializable]` 特性

### 3. 创建编辑器窗口骨架
- [ ] 创建 `TimelineEditorWindow.cs`，继承 `EditorWindow`
- [ ] 添加 `[MenuItem("Window/TimelineEditor")]` 菜单入口
- [ ] 使用 EditorGUILayout / GUILayout 划分 TOP 面板和时间轴面板区域
- [ ] 在 OnGUI 中绘制两个面板的分割线

---

## 第二阶段：TOP 面板

### 4. 实现 TopPanel
- [ ] 创建 `TopPanel.cs`
- [ ] 实现文件操作按钮（New、Open、Save）
- [ ] 实现播放控制按钮（▶ 播放 / ⏸ 暂停 / ⏹ 停止）
- [ ] 实现时间码显示标签
- [ ] 实现帧率切换下拉框
- [ ] 实现搜索输入框

### 5. 播放状态管理
- [ ] 实现 isPlaying 状态切换
- [ ] 实现播放时播放头自动前进（使用 EditorApplication.update）
- [ ] 时间码实时更新显示

---

## 第三阶段：时间轴面板 — 轨道头

### 6. 实现 TrackHeaderPanel
- [ ] 创建 `TrackHeaderPanel.cs`
- [ ] 遍历 TimelineData.groups 渲染 Group Header
- [ ] 渲染 Group 折叠/展开三角箭头（▶ / ▼）
- [ ] 渲染 Group 颜色标识和名称
- [ ] 渲染 Group 的 [🖊️] 重命名和 [🗑️] 删除按钮
- [ ] 展开状态下渲染 Track 行
- [ ] 每条 Track 渲染图标、名称、👁 显示/隐藏、🔒 锁定按钮
- [ ] 渲染 [+] 添加轨道按钮（每组底部）
- [ ] 渲染 [+] 添加 Group 按钮（整个列表底部）

### 7. 实现 Group 折叠/展开（方案 B）
- [ ] 折叠状态：显示 `▶ 📁 GroupName [N轨道]`，下方一条压缩横线
- [ ] 展开状态：显示 `▼ 📁 GroupName`，下方展开全部 Track 行
- [ ] 高度计算：展开 = `24 + N × 48`，折叠 = `24 + 4`
- [ ] 渲染时跳过折叠 Group 内 Track 的绘制

### 8. 实现拖拽排序
- [ ] 创建 `DragController.cs`
- [ ] 实现 Group 垂直拖拽排序（在 groups 列表内重排）
- [ ] 实现 Track 垂直拖拽排序（仅同 Group 内，不可跨 Group）
- [ ] 拖拽时显示插入指示线
- [ ] 拖拽过程中被拖对象半透明显示
- [ ] 处理拖拽时 Data 层列表重排

---

## 第四阶段：时间轴面板 — 剪辑区

### 9. 实现 TimeRuler
- [ ] 创建 `TimeRuler.cs`
- [ ] 根据 zoomLevel 和 scrollTime 计算刻度间距
- [ ] 绘制主要刻度线（带时间标签）和次要刻度线
- [ ] 实现吸顶固定（不随垂直滚动移动）

### 10. 实现 ClipArea 基础渲染
- [ ] 创建 `ClipArea.cs`
- [ ] 遍历所有展开的 Group/Track，渲染 Clip 块
- [ ] Clip 块位置 = `(clip.start - scrollTime) × 像素/秒`
- [ ] Clip 块宽度 = `clip.duration × 像素/秒`
- [ ] Clip 块用实心矩形渲染，使用 clip.color

### 11. 实现播放头
- [ ] 渲染播放头竖线（当前时间位置）
- [ ] 播放头 x 位置 = `(currentTime - scrollTime) × 像素/秒`
- [ ] 支持拖拽播放头以更改当前时间

### 12. 实现 Clip 拖拽交互
- [ ] 拖拽 Clip 实体 → 改变 clip.start
- [ ] 拖拽 Clip 左/右边缘 → 改变 clip.duration（最小 0.1s）
- [ ] 拖拽时实时更新 Clip 渲染位置

---

## 第五阶段：滚动与缩放

### 13. 实现垂直滚动
- [ ] 创建 ScrollRect 作为垂直容器
- [ ] 竖向滑动条在最右侧，贯穿时间轴面板全高
- [ ] 滚动范围 = 所有 Group 展开/折叠后的总高度
- [ ] 轨道头和剪辑区同步垂直滚动（共享同一 ScrollRect）
- [ ] 时间标尺不参与垂直滚动（吸顶）

### 14. 实现水平滚动（时间平移）
- [ ] 水平滑动条在剪辑区底部，左对齐剪辑区左边缘
- [ ] 水平滑动条宽度 = 剪辑区宽度
- [ ] 拖动水平滑动条更新 scrollTime
- [ ] 轨道头不参与水平滚动（始终固定）

### 15. 实现缩放
- [ ] 创建 `ZoomController.cs`
- [ ] 鼠标在剪辑区时，滚轮缩放时间标尺
- [ ] zoomLevel 范围：0.1 ~ 10.0
- [ ] 以鼠标 x 位置为锚点进行缩放
- [ ] 缩放后重新计算 Clip 位置和宽度

### 16. 滚动事件分配
- [ ] 鼠标在轨道头区域滚轮 → 垂直滚动
- [ ] 鼠标在剪辑区滚轮 → 缩放
- [ ] 拖动竖向滑动条 → 垂直滚动
- [ ] 拖动水平滑动条 → 时间平移

---

## 第六阶段：右键菜单与细节

### 17. 实现右键菜单
- [ ] 右键 Group Header 弹出菜单
- [ ] 折叠/展开单个 Group
- [ ] 折叠所有 / 展开所有
- [ ] 设置颜色
- [ ] 重命名
- [ ] 添加轨道
- [ ] 删除 Group

### 18. 实现基础拖放
- [ ] 支持从 Project 窗口拖入资源创建 Clip
- [ ] 根据资源类型自动判断 Track 类型

### 19. 最终验证
- [ ] 验证 Group 和 Track 的拖拽排序正确
- [ ] 验证 Group 折叠/展开正常
- [ ] 验证 Clip 拖拽移动和调整时长
- [ ] 验证时间标尺缩放和平移
- [ ] 验证竖向/水平滑动条联动正确
- [ ] 代码编译无错误

---

## 优先级

| 优先级 | 阶段 | 说明 |
|--------|------|------|
| 高 | 第一阶段 | 数据模型 + 窗口骨架，是后续所有工作的基础 |
| 高 | 第二阶段 | TOP 面板，提供基本操作入口 |
| 高 | 第三阶段 | 轨道头，时间轴的核心层级展示 |
| 高 | 第四阶段 | 剪辑区，时间轴的视觉主体 |
| 中 | 第五阶段 | 滚动与缩放，提升交互体验 |
| 低 | 第六阶段 | 右键菜单和细节打磨 |
