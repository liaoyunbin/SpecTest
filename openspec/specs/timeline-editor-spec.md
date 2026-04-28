# 基于时间轴的编辑器 - 规格文档

## 概述
在 Unity 编辑器中实现一个可视化的时间轴编辑器工具，用于编排和管理游戏中的时间序列事件（如动画、对话、技能演出、相机运镜等）。

## 核心功能

### 1. 时间轴数据模型
- **TimelineAsset**：ScriptableObject 资产，存储整个时间轴数据
- **Track（轨道）**：时间轴中的独立轨道，每个轨道包含一组 Clip
- **Clip（片段）**：轨道上的时间片段，包含开始时间、持续时间和具体行为数据
- **Keyframe（关键帧）**：支持在时间轴上添加关键帧事件

### 2. 编辑器窗口功能
- **时间轴视图**：横向时间线，支持缩放和平移
- **轨道列表**：左侧显示轨道名称和类型
- **播放头**：当前时间指示器，支持拖拽
- **剪辑编辑**：拖拽调整 Clip 位置和时长
- **右键菜单**：添加/删除轨道和 Clip

### 3. 运行时播放系统
- **TimelinePlayer**：运行时组件，驱动时间轴播放
- **播放控制**：播放、暂停、停止、跳转
- **事件回调**：Clip 进入/退出时触发事件

### 4. 支持的轨道类型
- **AnimationTrack**：控制 GameObject 动画
- **DialogueTrack**：对话/文本显示
- **EventTrack**：通用事件触发
- **AudioTrack**：音频播放控制

## 技术方案
- 使用 Unity EditorWindow 实现编辑器界面
- 使用 ScriptableObject 存储资产数据
- 使用 IMGUI 或 UI Toolkit 绘制界面
- 使用 C# 委托/事件系统实现运行时回调

## 文件结构
```
Assets/
  Editor/
    TimelineEditor/
      TimelineEditorWindow.cs      - 编辑器主窗口
      TimelineTrackDrawer.cs       - 轨道绘制器
      TimelineClipDrawer.cs        - Clip 绘制器
      TimelineAssetInspector.cs    - 资产 Inspector
  Scripts/
    Timeline/
      TimelineAsset.cs             - 时间轴资产数据
      TimelineTrack.cs             - 轨道数据
      TimelineClip.cs              - 片段数据
      TimelineKeyframe.cs          - 关键帧数据
      TimelinePlayer.cs            - 运行时播放器
      TimelineEvent.cs             - 事件定义
```

## 交付物
1. 完整的时间轴编辑器窗口
2. 可创建和编辑 TimelineAsset
3. 运行时播放演示场景
