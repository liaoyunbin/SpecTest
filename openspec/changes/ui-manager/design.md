# UIFrameworkLib — 商业级 UI 框架设计文档

## 一、设计目标

构建一个**数据驱动、VC 分离**的 UI 框架，满足商业级项目的稳定性、性能和可维护性要求。

### 核心原则

| 原则 | 说明 |
|------|------|
| **VC 分离** | View 只做表现逻辑，Controller 只做业务逻辑 |
| **1:1 约束** | 一个 Controller 对应且仅对应一个 View，由 UIController\<T\> 泛型保证 |
| **Controller 自管理 View** | Controller 实现 CreateViewAsync()，通过框架工具方法加载/复用 View |
| **接口简洁** | 对外只有 `Open<T>()` / `Close<T>()`，内部处理所有异步细节 |
| **类型安全** | 通过泛型 Controller 类型推导 UIKey，无反射字符串匹配 |

---

## 二、架构概览

```
┌─────────────────────────────────────────────┐
│                  调用方                       │
│    Open<ShopController>() / Close<T>()      │
├─────────────────────────────────────────────┤
│                 UIManager                    │
│  ┌──────┐ ┌──────────┐ ┌──────┐ ┌────────┐ │
│  │Queue │ │DualChannel│ │Stack │ │Cache   │ │
│  └──────┘ └──────────┘ └──────┘ └────────┘ │
├─────────────────────────────────────────────┤
│   UIContext / UIStateMachine / UIItemConfig  │
├──────────────┬──────────────────────────────┤
│   UIView     │    UIController<T>           │
│  (表现层)     │     (逻辑层)                  │
│  - Animation │    - CreateViewAsync()       │
│  - Interactive│   - OnInit / OnOpen         │
│              │    - OnShown / OnHide        │
│              │    - OnDispose               │
├──────────────┴──────────────────────────────┤
│   UIResourceLoader (只加载原始 Prefab)       │
│   UIConfigLoader (外部配置 → UIItemConfig)   │
│   Singleton<T> (泛型单例基类)                │
└─────────────────────────────────────────────┘
```

---

## 三、核心模块

### 3.1 泛型单例 Singleton\<T\>

**文件：** Core/Singleton.cs

```csharp
public abstract class Singleton<T> where T : class, new()
{
    public static T Instance { get; }
}
```

- 线程安全的双检锁实现
- UIManager 和 UIResourceLoader 均继承此基类

### 3.2 状态机 UIStateMachine

**文件：** Core/UIStateMachine.cs

```
None → Loading → AnimationEnter → Opened → AnimationExit → Closed
  ↓         ↓            ↓
Closed   Closed        Closed      (中断/失败时的紧急回退)
```

- 6 个明确状态（None / Loading / AnimationEnter / Opened / AnimationExit / Closed）
- 所有转换走 `TryTransitionTo()`，非法转换自动拦截并日志警告
- `IsTransitioning` 判断是否处于过渡状态
- **替代 CancellationTokenSource：** 异步操作前后检查 `CurrentState`，若状态已被外部变更（如 CloseAll），则中断流程

### 3.3 UIContext

**文件：** Core/UIContext.cs

每个 UI 实例的运行时状态容器，生命周期为：打开时创建 → 关闭时释放。

```csharp
public class UIContext
{
    public string UIKey { get; }           // UI 唯一标识
    public int InstanceId { get; }         // 每次打开递增
    public UIView View { get; private set; }
    public IUIController Controller { get; private set; }
    public UIItemConfig Config { get; }
    public UIStateMachine StateMachine { get; }
    public UIContext PreviousContext { get; set; }  // 栈中上一个 UI
}
```

关键方法：
- `BindController(controller)` — View 创建前绑定 Controller
- `SetView(view)` — Controller 创建 View 后设置 View
- `Dispose()` — 完全清理

### 3.4 配置模型 UIItemConfig

**文件：** Core/UIItemConfig.cs

```csharp
public class UIItemConfig
{
    public string UIKey { get; set; }       // UI 标识（与 Controller 类名对应）
    public string PrefabPath { get; set; }  // Resources 路径
    public UILayer Layer { get; set; }      // Background / Normal / Popup
}
```

### 3.5 层级枚举 UILayer

**文件：** Core/UILayer.cs

| 层级 | 枚举值 | 行为 |
|------|--------|------|
| Background | 0 | 底层背景，同一时间只允许一个。打开时弹出（关闭）其上所有 Normal 和 Popup |
| Normal | 1 | 标准界面，支持 Back 栈操作。打开新 Normal 时隐藏上一个（入栈），Back 时恢复 |
| Popup | 2 | 弹窗界面，相互替换。新 Popup 替换当前 Popup，自带全屏遮罩 |

---

## 四、UIView 基类

**文件：** UIView/UIView.cs

### 4.1 职责

纯表现层，继承 MonoBehaviour。负责：
- 动画播放（提供多种内置动画 Helper）
- 交互控制（CanvasGroup 级别 + Selectable 快照）
- Popup 遮罩（虚方法，子类可选实现）

### 4.2 生命周期方法

| 方法 | 说明 |
|------|------|
| `PlayEnterAnimation(onComplete)` | 播放入场动画（virtual，默认淡入 0.3s） |
| `PlayExitAnimation(onComplete)` | 播放退场动画（virtual，默认淡出 0.2s） |
| `ShowMask()` / `HideMask()` | 遮罩控制（virtual，Popup 子类实现） |

### 4.3 交互控制

- `SetInteractive(bool)` — CanvasGroup 级别的 interactable + blocksRaycasts
- `DisableAllSelectables()` — 快照所有 Selectable 并禁用（入场动画前）
- `RestoreSelectables()` — 恢复快照（入场动画结束）

### 4.4 内置动画 Helper

子类可在 override `PlayEnterAnimation` / `PlayExitAnimation` 中直接调用：

| Helper 方法 | 效果 |
|------------|------|
| `FadeEnter` / `FadeExit` | 透明度淡入淡出（默认） |
| `ScaleEnter` / `ScaleExit` | 缩放弹性效果（弹入到 1.1 再回到 1.0） |
| `SlideUpEnter` / `SlideUpExit` | 从下往上滑入 / 往上滑出 |
| `SlideDownEnter` / `SlideDownExit` | 从上往下滑入 / 往下滑出 |
| `BlackFadeEnter` / `BlackFadeExit` | 黑屏渐入渐出（需传入全屏黑色 Image） |

示例：
```csharp
public class ShopView : UIView
{
    public Button m_BtnClose;

    private void Awake()
    {
        m_BtnClose = transform.Find("BtnClose").GetComponent<Button>();
    }

    public override void PlayEnterAnimation(Action onComplete)
    {
        ScaleEnter(onComplete, 0.35f);  // 缩放弹入
    }

    public override void PlayExitAnimation(Action onComplete)
    {
        FadeExit(onComplete, 0.15f);   // 淡出
    }
}
```

---

## 五、Controller 体系

### 5.1 IUIController（内部接口）

**文件：** Controller/IUIController.cs

框架内部使用的多态接口，业务层不需要也不应该直接接触。

```csharp
internal interface IUIController
{
    UIContext Context { get; }
    void BindContext(UIContext context);
    void SetView(UIView view);
    void OnInit();
    void OnOpen(object args);
    void OnShown();
    void OnHide();
    void OnDispose();
}
```

### 5.2 UIController\<T\>（泛型基类）

**文件：** Controller/UIController.cs

```csharp
public abstract class UIController<T> : IUIController where T : UIView
{
    public T View { get; private set; }
    public UIContext Context { get; private set; }
    public UIItemConfig Config => Context?.Config;

    // 子类必须实现 — 创建或获取 View
    public abstract UniTask<T> CreateViewAsync();

    // 框架提供的工具方法
    protected UniTask<T> LoadFromResources(string prefabPath);
    protected T LoadFromCache();

    // 生命周期钩子
    protected internal virtual void OnInit();
    protected internal virtual void OnOpen(object args);
    protected internal virtual void OnShown();
    protected internal virtual void OnHide();
    protected internal virtual void OnDispose();

    // 辅助方法
    protected void CloseSelf();
}
```

**折中方案说明：**
Controller 自管理 View 创建（实现 CreateViewAsync），框架提供 LoadFromResources 等工具方法：
1. 先检查 UIManager 是否有缓存的隐藏实例（上次关闭时缓存）
2. 无缓存时通过 UIResourceLoader 加载 Prefab 并实例化
3. 实例化后挂载到 UIRoot 对应层级

**使用示例：**
```csharp
public class ShopController : UIController<ShopView>
{
    public override async UniTask<ShopView> CreateViewAsync()
    {
        return await LoadFromResources("Prefabs/ShopPanel");
    }

    protected internal override void OnInit()
    {
        View.m_BtnClose.onClick.AddListener(CloseSelf);
    }

    protected internal override void OnOpen(object args)
    {
        var categoryId = (int)args;
        // 刷新数据...
    }
}
```

---

## 六、UIManager 核心管理器

**文件：** Manager/UIManager.cs

### 6.1 对外接口

```csharp
// 通过 Controller 类型打开/关闭 UI
UIManager.Instance.Open<ShopController>(args);
UIManager.Instance.Close<ShopController>();

// 通过字符串 UIKey（调试工具用）
UIManager.Instance.Open("Shop", args);
UIManager.Instance.Close("Shop");

// 紧急关闭全部
UIManager.Instance.CloseAll();

// 清空缓存 View
UIManager.Instance.ClearCache();
```

### 6.2 Controller 持久化

```csharp
private readonly Dictionary<string, IUIController> _controllers = new();
```

- Controller 首次创建后持久化保存，不会销毁
- 首次创建时调用 `OnInit()`，后续只调用 `OnOpen()` / `OnShown()` / `OnHide()`
- `_initializedControllers` HashSet 跟踪已初始化过的 Controller

### 6.3 View 隐藏缓存

**替代 UIPool：** 关闭 UI 时不销毁 View，而是 `SetActive(false)` 缓存到 `CachedViews` 字典。

```csharp
public readonly Dictionary<string, GameObject> CachedViews = new();
```

- 下次打开同一 UI 时，Controller 的 `LoadFromResources()` 先检查缓存
- `ClearCache()` 手动清空所有缓存实例
- 对比对象池：更简单，无容量限制，无池管理开销

### 6.4 请求队列

```
Open<T>() 执行逻辑：
  同一界面已打开（Opened 状态） → 直接刷新 Controller.OnOpen(args)
  同一界面已在队列中            → 刷新参数（去重）
  有任务在执行或双通道忙        → 入队等待（上限 10）
  无任务                        → 立即执行
```

`QueueItem` 内部类：
```csharp
private class QueueItem
{
    public string UIKey { get; }
    public object Args { get; set; }
}
```

### 6.5 双通道动画

维护两个独立通道实现退场与进场动画重叠：

```csharp
private UIContext _exitingContext;  // 退场通道
private UIContext _enteringContext; // 进场通道
private bool IsBusy => _exitingContext != null || _enteringContext != null;
```

### 6.6 UIKey 解析

```csharp
private string ResolveUIKey<T>() where T : IUIController
{
    var name = typeof(T).Name;
    return name.EndsWith("Controller")
        ? name[..^"Controller".Length]
        : name;
}
```

### 6.7 栈管理

```csharp
private readonly List<UIContext> _normalStack = new();
```

| 层级 | 管理方式 |
|------|---------|
| Background | 单例，打开时关闭所有 Normal 和 Popup |
| Normal | 入栈，上一个自动 OnHide，Back 时恢复 OnShown |
| Popup | 相互替换，记录栈顶 Normal 为 PreviousContext |

**RegisterContext 逻辑：**
- Background：关闭所有 Normal 和 Popup，替换当前 Background
- Normal：上一个 Normal OnHide → 入栈
- Popup：当前 Popup StartExit → 替换

**UnregisterContext 逻辑：**
- 从 `_activeContexts` 移除
- 从对应层级引用移除
- 尝试恢复上一个 Normal（RestorePreviousNormal）

### 6.8 状态机替代 CancellationToken

在异步操作的关键节点检查状态机状态，而非使用 CancellationToken：

```csharp
// Controller 创建 View
var view = await controller.CreateViewAsync();
// 检查：如果在加载期间被 CloseAll 中断，状态已经不是 Loading
if (ctx.StateMachine.CurrentState != UIState.Loading)
{
    if (view != null) Object.Destroy(view.gameObject);
    CleanupAndNext(ctx);
    return;
}
```

---

## 七、UIResourceLoader 资源管理器

**文件：** Manager/UIResourceLoader.cs

```csharp
public class UIResourceLoader : Singleton<UIResourceLoader>
{
    public async UniTask<GameObject> LoadPrefabAsync(string prefabPath);
}
```

- 继承 Singleton\<T\>，通过 `Instance` 访问
- 职责单一：仅从 Resources 异步加载 Prefab，返回原始 GameObject
- 不负责实例化、不负责缓存、不负责对象池
- 提供超时看门狗（仅日志警告，不中断流程）
- 可通过继承重写 `LoadPrefabAsync` 切换到 Addressables / AssetBundle

---

## 八、UIConfigLoader 配置加载器

**文件：** Config/UIConfigLoader.cs

```csharp
public class UIConfigLoader
{
    public void Load(List<UIItemConfig> configs);
    public UIItemConfig Get(string uiKey);
    public string[] GetAllKeys();
    public void Reload(List<UIItemConfig> configs);
}
```

- 与具体数据格式解耦（Luban / ScriptableObject / JSON 均可）
- 外部自行将原始数据转换为 `List<UIItemConfig>` 后调用 Load
- 支持运行时热重载

---

## 九、UIRoot

**文件：** UIView/UIRoot.cs

层级结构（由低到高）：

```
[UIRoot] (Canvas, SortingOrder=10000)
├── BackgroundLayer  (SortingOrder=0)
├── NormalLayer      (SortingOrder=100)
└── PopupLayer       (SortingOrder=200)
```

- `[RuntimeInitializeOnLoadMethod]` 场景加载前自动初始化
- `DontDestroyOnLoad` 跨场景持久化
- `GetLayer(UILayer)` 根据层级获取对应 Transform

---

## 十、生命周期流程

### 10.1 打开流程 (ExecuteOpen)

```
EnqueueOpen(uiKey, args)
  ├─ 已打开（Opened）→ 直接刷新 OnOpen
  ├─ 已在队列 → 刷新 args
  ├─ 队列忙 → 入队
  └─ 空闲 → 执行

ExecuteOpen(item)
  1. 创建 UIContext, 状态 → Loading
  2. 获取/创建 Controller（持久化）
     - 首次 → _controllers[uiKey] = controller
     - 首次 → OnInit()
  3. Controller.CreateViewAsync()
     - 检查缓存 → 复用
     - 异步加载 Prefab → 实例化
  4. 状态检查（是否仍为 Loading？）
  5. 注册到层级管理 (RegisterContext)
  6. 状态 → AnimationEnter
  7. 禁用交互 + 遮罩(Popup)
  8. OnOpen(args)
  9. PlayEnterAnimation → 等待完成
  10. 状态 → Opened
  11. 恢复交互
  12. OnShown()
  13. _enteringContext = null
  14. ProcessNext()
```

### 10.2 关闭流程 (StartExit)

```
StartExit(ctx)
  1. 状态检查（必须是 Opened）
  2. 状态 → AnimationExit
  3. OnHide()
  4. 禁用交互
  5. PlayExitAnimation → 等待完成
  6. 状态 → Closed
  7. CacheView (SetActive(false)，不销毁)
  8. UnregisterContext
     - 从 _activeContexts 移除
     - 从对应层级引用移除
  9. RestorePreviousNormal (如有)
  10. _exitingContext = null
  11. ProcessNext()
```

### 10.3 关闭全部 (CloseAll)

```
CloseAll()
  1. 清空队列，重置 _isProcessing
  2. 清理 _enteringContext / _exitingContext
  3. 遍历所有活跃 Context → Closed + OnDispose + Destroy View
  4. 清空 _activeContexts / _normalStack / 层级引用
  5. 清理所有缓存 View
  6. 清理所有持久 Controller
```

---

## 十一、动画系统

### 11.1 设计原则

- UIView 提供 `virtual` 方法，子类 override 选择效果
- 所有动画通过 `UniTask` + `Time.deltaTime` 驱动
- 动画完成通过回调通知框架
- 子类在 override 中直接调用内置 Helper

### 11.2 动画类型

| 类型 | UIAnimationType 枚举 | Helper 方法 |
|------|---------------------|------------|
| 淡入淡出 | Fade | FadeEnter / FadeExit |
| 缩放弹性 | Scale | ScaleEnter / ScaleExit |
| 上滑入/出 | SlideUp | SlideUpEnter / SlideUpExit |
| 下滑入/出 | SlideDown | SlideDownEnter / SlideDownExit |
| 黑屏渐入 | BlackFade | BlackFadeEnter / BlackFadeExit |
| 无动画 | None | 立即回调 onComplete |

---

## 十二、编辑器调试窗口

**文件：** Editor/UIEditorWindow.cs

- 菜单 `UIFrameworkLib/UI Debugger`
- 显示：活跃 UI 列表 + 状态 + 层级 + 打开时间
- 显示：Normal 栈
- 显示：缓存 View 统计
- 操作：CloseAll / ClearCache / 手动 Open

---

## 十三、目录结构

```
Assets/Scripts/UIFrameworkLib/
├── Core/
│   ├── Singleton.cs              # 泛型单例基类
│   ├── UILayer.cs                # 层级枚举（Background/Normal/Popup）
│   ├── UIStateMachine.cs         # 6 状态状态机
│   ├── UIItemConfig.cs           # 配置模型
│   └── UIContext.cs              # 运行时上下文
├── UIView/
│   ├── UIView.cs                 # View 基类（动画 + 交互 + 遮罩）
│   └── UIRoot.cs                 # 场景根节点（3 层结构）
├── Controller/
│   ├── IUIController.cs          # 内部接口
│   └── UIController.cs           # 泛型基类（Controller 自管理 View）
├── Manager/
│   ├── UIManager.cs              # 核心管理器（队列/双通道/栈/缓存）
│   └── UIResourceLoader.cs       # 资源加载器（只加载原始 Prefab）
├── Config/
│   └── UIConfigLoader.cs         # 配置加载器
└── Editor/
    └── UIEditorWindow.cs         # 运行时调试窗口
```

---

## 十四、变更记录

| 版本 | 变更内容 |
|------|---------|
| v1 | 初始版本：Luban 配置 + UIPool + UIRegistry + UIKeyAttribute + 自动绑定 |
| v2 | 去除 Luban 依赖、去除 IsSingleton/CacheOnClose/IsModal 字段、去除 UIKeyAttribute |
| v3 | 资源加载抽离到 UIResourceLoader |
| v4 | UILayer 精简为三层（Background/Normal/Popup），修改栈语义 |
| v5 | 简化动画策略，去除 UIMark，改为手动绑定 |
| v6 | 迁移到 UniTask，简化队列逻辑，Controller 持久化 |
| v7 | 添加 Singleton<T> 泛型单例基类 |
| v8 | 折中方案：Controller 自管理 View 创建，框架提供工具方法 |
| v9 | 去除 UIPool → 隐藏 View 缓存，UIResourceLoader 只返回 GameObject，状态机替代 CancellationToken |