# UIFrameworkLib — 商业级 UI 框架需求文档

## 一、设计目标

构建一个**数据驱动、VC 分离**的 UI 框架，满足商业级项目的稳定性、性能和可维护性要求。

### 核心原则

| 原则 | 说明 |
|------|------|
| **VC 分离** | View 只做表现逻辑，Controller 只做业务逻辑 |
| **1:1 约束** | 一个 Controller 对应且仅对应一个 View，由类型系统保证 |
| **数据驱动** | UI 配置（层级、缓存策略等）由 Luban 配置表驱动 |
| **接口简洁** | 对外只有 `Open<T>()` / `Close<T>()`，内部处理所有异步细节 |
| **类型安全** | 注册时编译期校验，运行时通过注册表保证 |

---

## 二、全局需求清单

### 2.1 状态机

```
None → Loading → AnimationEnter → Opened → AnimationExit → Closed
```

- 6 个明确状态，所有转换走 `TryTransitionTo()`，非法转换自动拦截
- `IsTransitioning` 判断是否处于过渡状态（Loading / AnimationEnter / AnimationExit）

### 2.2 异步加载

- 使用 `Resources.LoadAsync` 异步加载 Prefab
- 支持 CancellationToken 取消
- 超时看门狗（5 秒无响应强制跳转 Opened）
- 加载失败进入 Closed

### 2.3 双通道动画

- 维护 `_enteringContext` 和 `_exitingContext` 两个独立通道
- 入场动画和退场动画可重叠播放
- 覆盖式跳转（A → B）：A 退场动画与 B 入场动画同时进行

### 2.4 请求队列

| 模式 | 语义 | 适用场景 |
|------|------|---------|
| `WaitForOpen`（默认） | 前一个 UI 到达 Opened 后，下一个开始加载 | 连续打开多个平级界面（Shop → Bag） |
| `WaitForClose` | 前一个 UI 完全关闭（Closed）后，下一个才开始 | 过渡场景（关卡切换），两个 UI 不应共存 |

- 队列上限 10 个，满时丢弃最旧请求

### 2.5 队列模式（QueueMode）

```
Open<T>(QueueMode.WaitForOpen)    → 默认行为
Open<T>(QueueMode.WaitForClose)   → 等待前一个完全关闭

ProcessNext() 根据 QueueMode 判断：
  WaitForOpen  → 当前 reaching Opened 即触发
  WaitForClose → 需所有非 Closed Context 清空才触发
```

### 2.6 层级管理

| 层级 | 枚举值 | 行为 |
|------|--------|------|
| Background | 0 | 底层背景，同一时间只允许一个。打开时弹出（关闭）其上所有 Normal 和 Popup |
| Normal | 1 | 标准界面，支持 Back 栈操作。打开新 Normal 时隐藏上一个（入栈），Back 时恢复 |
| Popup | 2 | 弹窗界面，相互替换。新 Popup 替换当前 Popup，自带全屏遮罩，在 Normal 之上 |

### 2.7 Popup 遮罩

- 每个 Popup 自带半透明全屏 Image（RaycastTarget = true）
- 遮罩的显示/隐藏由 UIView 基类在生命周期中自动管理
- 无全局遮罩管理器，无遮罩计数

### 2.8 入场动画结束后才能点击

- `UIView.DisableAllSelectables()` — 入场动画前禁用所有 Selectable
- `UIView.RestoreSelectables()` — 入场动画结束恢复
- 退场动画开始时再次禁用

### 2.9 自动绑定

- UIMark 组件标记需要绑定的节点
- 编辑器代码生成器自动生成强类型字段（无反射）

### 2.10 UI 栈管理

- Normal 界面入栈，支持 Back（关闭当前，恢复上一个）
- 上一个 Normal 入栈时自动 `OnHide`，恢复时自动 `OnShown`
- Popup 记录栈顶 Normal 为 PreviousContext，不独立入栈
- Toast / Loading 不进入栈

### 2.11 生命周期

| 方法 | 调用次数 | 调用时机 |
|------|---------|---------|
| `OnInit()` | 仅一次 | 实例化后、OnOpen 之前。适合注册事件 |
| `OnOpen(args)` | 每次打开 | 加载完成、入场动画前。适合接收参数刷新数据 |
| `OnShown()` | 每次打开 | 入场动画结束。UI 可见可交互 |
| `OnHide()` | 每次隐藏 | 退场动画开始 / 被其他 Normal 覆盖 |
| `OnDispose()` | 仅一次 | 销毁前。适合清理事件绑定 |

### 2.12 VC 分离

**View（UIView）：**
- 继承 MonoBehaviour
- 纯表现逻辑：组件绑定（UIMark）、动画播放、交互控制
- 不直接访问 Model，不处理业务逻辑

**Controller（UIController\<T\>）：**
- 纯 C# 类，不继承 MonoBehaviour
- 持有 T View 的强类型引用（编译期确定）
- 处理按钮点击、事件订阅、调用 Model/Service
- 通过 View 提供的接口更新 UI

**约束：** `public abstract class UIController<T> : IUIController where T : UIView`

### 2.13 注册机制（UIRegistry）

- 启动时统一调用 `RegisterAll()` 注册所有 Controller
- 单类型注册：`Register<ShopController>()`，自动从 `UIController<T>` 推导 View 类型
- 内部维护 `ControllerType → UIKey` 和 `ControllerType → ViewType` 两个字典
- `UIKey` 默认 = View 类名，可通过 `[UIKey("...")]` 特性覆盖

### 2.14 UIKey 解析优先级

```
ResolveUIKey<TController>()
  1. UIRegistry 注册表（优先）
  2. [UIKey] 特性
  3. 类名去掉 "Controller" 后缀（约定回退）
```

### 2.15 配置表

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| UIKey | string | - | UI 唯一标识（与 Controller 类名对应） |
| PrefabPath | string | - | Resources 相对路径 |
| Layer | int | Normal=1 | Background=0 / Normal=1 / Popup=2 |
| EnterAnimId | int | 0 | 入场动画策略 ID（0=Fade） |
| ExitAnimId | int | 0 | 退场动画策略 ID（0=Fade） |

### 2.16 对象池（UIPool）

- 以 UIKey 为 key，Stack\<GameObject\> 为 value
- 每 UIKey 最多缓存 5 个
- `Get()` / `Return(key, go)` / `Clear()`

### 2.17 动画策略（IUIAnimationStrategy）

- `IUIAnimationStrategy` 接口：`PlayEnter()` / `PlayExit()`
- `UIAnimationRegistry` 中心：`int ID → factory`
- 内置策略：Fade、BlackFade、Scale、Slide
- `UIView` 通过 `GetEnterStrategy()` / `GetExitStrategy()` 获取策略
- `UIRoot.AnimationOverlay` 全屏遮罩层（供 BlackFade 等使用）

### 2.18 启动流程

```
[RuntimeInitializeOnLoadMethod]
  ├── UIRoot.Initialize()         // 创建 Canvas + 4 层节点
  ├── UIRegistry.RegisterAll()    // 注册所有 Controller↔View 映射
  └── UIConfigLoader.Load()       // 加载 Luban 配置
```

### 2.19 编辑器调试

- 菜单 `UIFrameworkLib/UI Debugger`
- 显示：活跃 UI 列表 + 状态 + 栈 + 池统计
- 操作：CloseAll / ClearPool / 手动 Open

### 2.20 命名空间

所有框架代码统一使用 `UIFrameworkLib`

---

## 三、架构分层

```
┌─────────────────────────────────────────────┐
│                  调用方                       │
│    Open<ShopController>() / Close<T>()      │
├─────────────────────────────────────────────┤
│                UIManager                     │
│  ┌──────┐ ┌──────────┐ ┌──────┐ ┌───────┐  │
│  │Queue │ │DualChannel│ │Stack │ │ Pool │  │
│  └──────┘ └──────────┘ └──────┘ └───────┘  │
├─────────────────────────────────────────────┤
│            UIRegistry                        │
│    ControllerType → UIKey → ViewType        │
├─────────────────────────────────────────────┤
│    UIContext / UIStateMachine / UIItemConfig │
├──────────────┬──────────────────────────────┤
│   UIView     │    UIController<T>           │
│  (表现层)     │     (逻辑层)                  │
│  - AutoBind  │    - OnInit / OnOpen         │
│  - Animation │    - OnShown / OnHide        │
│  - Interactive│   - OnDispose               │
└──────────────┴──────────────────────────────┘
```

---

## 四、目录结构

```
Assets/Scripts/UIFrameworkLib/
├── Core/
│   ├── UILayer.cs                # 层级枚举
│   ├── UIStateMachine.cs         # 6 状态状态机
│   ├── UIItemConfig.cs           # 配置模型
│   └── UIContext.cs              # 运行时上下文
├── UIView/
│   ├── UIView.cs                 # View 基类
│   ├── UIRoot.cs                 # 场景根节点
│   └── UIMask.cs                 # Popup 遮罩组件（可选）
├── Controller/
│   ├── IUIController.cs          # 内部接口
│   └── UIController.cs           # 泛型基类 UIController<T>
├── Manager/
│   ├── UIManager.cs              # 核心管理器
│   ├── UIPool.cs                 # 对象池
│   ├── UIResourceLoader.cs       # 资源加载器（池 + 异步加载 + 实例化）
│   └── UIRegistry.cs             # 注册表
├── Animation/
│   ├── IUIAnimationStrategy.cs   # 动画策略接口
│   ├── FadeStrategy.cs           # 淡入淡出
│   ├── BlackFadeStrategy.cs      # 黑屏渐入渐出
│   ├── ScaleStrategy.cs          # 缩放
│   ├── SlideStrategy.cs          # 滑入滑出
│   └── UIAnimationRegistry.cs    # 动画注册中心
├── Config/
│   └── UIConfigLoader.cs         # Luban 配置加载器
└── Editor/
    ├── UIMark.cs                 # 自动绑定标记
    └── UIEditorWindow.cs         # 运行时调试窗口
```