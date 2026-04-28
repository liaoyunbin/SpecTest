# 时间轴编辑器 —— 设计文档

## 整体布局

```
┌──────────────────────────────────────────────────────────────────────────┐
│                        TOP 面板 — 命令与控制中心                          │
│  [New] [Open] [Save]  │  ▶ ⏸ ⏹ │ 00:00:03.2 │ FPS:30 │ 🔍 搜索        │
├──────────────────────────────────────────────────────────────────────────┤
│                     时间轴面板 — 核心编辑区                                │
│                                                                          │
│  ┌──────────────┬───────────────────────────────────────────────┬───┐   │
│  │              │  时间标尺（吸顶固定）                           │ ▓ │   │
│  │              │  0    1    2    3    4    5    6    7        │ █ │   │
│  │   轨道头      │  ═══════════════════════════════════════      │ █ │   │
│  │   (固定宽度)  │                                               │ ▓ │   │
│  │              │  剪辑区                                        │ ▓ │   │
│  │ ▼ 📁 G1     │  🎬 T1  ┌──────┐  ┌──────┐                  │ ▓ │   │
│  │   🎬 T1     │          │  A   │  │  B   │                  │ ▓ │   │
│  │              │          └──────┘  └──────┘                  │ ▓ │   │
│  │ ▶ 📁 G2     │  🎬 T2       ┌──────────┐                   │ ▓ │   │
│  │              │               │    C     │                   │ ▓ │   │
│  │ ▼ 📁 G3     │  ⚡ T3  ●        ●          ●               │ ░ │   │
│  │   ⚡ T3     │        0.5      2.0        4.5               │ ░ │   │
│  │              │                                               │ ░ │   │
│  │  [+] 添加    │                                               │ ░ │   │
│  │    Group     │                                               │ ░ │   │
│  ├──────────────┴───────────────────────────────────────────────┴───┤   │
│  │              ← 水平滑动条（时间平移）→                              │   │
│  │              [▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓]│   │
│  └──────────────────────────────────────────────────────────────────┘   │
│                                                                          │
│  竖向滑动条在最右侧，贯穿时间轴面板全高（包含水平滑动条高度）               │
└──────────────────────────────────────────────────────────────────────────┘
```

---

## 数据模型

### 层级关系

```
TimelineData
└── groups: List<Group>
    └── tracks: List<Track>
        └── clips: List<Clip>
```

### 类定义

```csharp
public class TimelineData
{
    public string name;
    public float duration;
    public float frameRate;
    public List<Group> groups;
}

public class Group
{
    public string id;
    public string name;
    public Color color;
    public bool isExpanded;      // 折叠/展开
    public List<Track> tracks;
}

public class Track
{
    public string id;
    public string name;
    public bool isMuted;         // 静音/禁用
    public bool isLocked;        // 锁定（禁止编辑）
    public List<Clip> clips;
}

public class Clip
{
    public string id;
    public string name;
    public float start;          // 开始时间（秒）
    public float duration;       // 持续时长（秒）
    public Color color;
    public object data;          // 关联数据
}
```

---

## 面板设计

### TOP 面板

| 区域 | 内容 | 说明 |
|------|------|------|
| 文件操作 | New / Open / Save | 时间轴数据文件管理 |
| 播放控制 | ▶ 播放 / ⏸ 暂停 / ⏹ 停止 | 播放头移动控制 |
| 时间码 | `00:00:03.2` | 当前播放头位置 |
| 帧率 | FPS:30 | 帧率显示与切换 |
| 搜索 | 🔍 | 搜索轨道/剪辑块名称 |

### 时间轴面板

```
┌──────────────┬──────────────────────────────────────────────────┐
│   轨道头       │   剪辑区                                         │
│              │                                                 │
│  Group 列表    │   时间标尺（吸顶，不随垂直滚动移动）               │
│  ├─ 组名       │                                                 │
│  ├─ 折叠/展开  │   各 Group/Track 对应的 Clip 渲染区域             │
│  ├─ 颜色标识   │                                                 │
│  └─ 轨道列表   │   - Clip 块水平放置，位置由 start 决定           │
│      ├─ 轨道名 │   - Clip 块宽度 = duration × 像素/秒             │
│      ├─ 👁     │   - 拖拽移动 = 改变 start                        │
│      ├─ 🔒     │   - 拖拽边缘 = 改变 duration                     │
│      └─ [+]    │                                                 │
│              │   播放头（可拖拽的竖线，标记当前时间位置）          │
└──────────────┴──────────────────────────────────────────────────┘
```

---

## Group 折叠/展开（方案 B — 压缩条）

折叠时，Group 内容压缩为一条横线，显示轨道数量：

```
展开:                          折叠:
▼ 📁 G1                         ▶ 📁 G1 [3轨道]
────────────────────────        ══════════════════
  🎬 T1  ┌───┐ ┌───┐
  🎬 T2       ┌─────┐
  🎬 T3  ┌──┐
```

高度计算公式：
- 展开：`h_header + (trackCount × h_track)`
- 折叠：`h_header + h_bar`

其中 `h_header = 24px`, `h_track = 48px`, `h_bar = 4px`

---

## 拖拽排序

| 操作 | 方向 | 效果 | 限制 |
|------|------|------|------|
| 拖拽 Group | 垂直 ↑↓ | 在 groups 列表中重新排序 | 全列表范围 |
| 拖拽 Track | 垂直 ↑↓ | 在当前 Group.tracks 中重新排序 | 不可跨 Group |
| 拖拽 Clip | 水平 ↔ | 改变 clip.start | 同 Track 内 |
| 拖拽 Clip 边缘 | 水平 ↔ | 改变 clip.duration | 最小 0.1s |

---

## 滚动与交互

### 滑动条布局

```
竖向滑动条 → 时间轴面板最右侧，贯穿全高（包含水平滑动条高度）
水平滑动条 → 剪辑区底部，左对齐剪辑区左边缘，宽度 = 剪辑区宽度
```

### 交互分配

| 区域 | 操作 | 效果 |
|------|------|------|
| 左侧（轨道头） | 鼠标滚轮 | 垂直上下滚动 |
| 左侧（轨道头） | 拖动竖向滑动条 | 垂直上下滚动 |
| 右侧（剪辑区） | 鼠标滚轮 | 缩放时间标尺 |
| 右侧（剪辑区） | 拖动水平滑动条 | 时间平移（左右移动视口） |
| 时间标尺 | 不参与垂直滚动 | 始终吸顶固定 |

### 缩放锚点

```
缩放时以鼠标所在 x 坐标对应的可见时间点为锚点，该点对应的时间在缩放前后保持不变，
两侧均匀拉伸或压缩。
```

### 渲染计算参数

```csharp
zoomLevel: float       // 缩放比例，1.0 = 默认
scrollTime: float      // 当前视口左边缘对应的时间
像素/秒 = zoomLevel × 基准像素/秒
可见时间范围 = viewportWidth / (像素/秒)
Clip.x = (clip.start - scrollTime) × (像素/秒)
Clip.width = clip.duration × (像素/秒)
```

---

## 右键菜单

```
右键 Group Header:
┌─────────────────────┐
│ 📂 折叠 / 展开       │
│ 📂 折叠所有 / 展开所有│
│ ─────────────────── │
│ 🎨 设置颜色          │
│ ✏️ 重命名            │
│ [+] 添加轨道         │
│ ─────────────────── │
│ 🗑️ 删除             │
└─────────────────────┘
```

---

## 组件层级关系

```
EditorWindow (TimelineEditorWindow)
│
├── TOP 面板（固定，不滚动）
│   ├── 文件操作按钮组
│   ├── 播放控制按钮组
│   ├── 时间码标签
│   ├── 帧率设置
│   └── 搜索框
│
└── 时间轴面板
    └── ScrollRect（垂直，竖向滑动条在右侧贯穿全高）
        ├── 时间标尺（独立，吸顶）
        └── Content（垂直可滚动）
            ├── 轨道头区域
            │   ├── Group 1
            │   │   ├── Group Header（折叠/展开/颜色/名称）
            │   │   ├── Track 1
            │   │   └── [+] 添加轨道
            │   ├── Group 2
            │   └── [+] 添加 Group
            │
            └── 剪辑区
                ├── ScrollRect（水平，水平滑动条在底部）
                │   └── Clip 渲染内容
                │       ├── Track 行的背景
                │       └── Clip 块（可拖拽）
                └── 播放头（独立，叠加在剪辑区上方）
```

---

## 目录结构

```
Assets/
└── Scripts/
    └── TimelineEditor/
        ├── Data/
        │   ├── TimelineData.cs
        │   ├── Group.cs
        │   ├── Track.cs
        │   └── Clip.cs
        ├── Core/
        │   └── TimelineEditorWindow.cs
        ├── UI/
        │   ├── TopPanel.cs
        │   ├── TrackHeaderPanel.cs
        │   ├── ClipArea.cs
        │   └── TimeRuler.cs
        └── Interaction/
            ├── DragController.cs
            └── ZoomController.cs
```

---

## 依赖关系

- `TimelineEditorWindow` 持有 `TopPanel`、`TrackHeaderPanel`、`ClipArea`、`TimeRuler`
- `ClipArea` 持有 `DragController` 和 `ZoomController`
- 所有组件共享一个 `TimelineData` 实例
