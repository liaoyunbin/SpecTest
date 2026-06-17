# UIFrameworkLib — 商业级 UI 框架设计文档

## 一、设计目标

构建一个**数据驱动、VC 分离、高内聚低耦合**的 UI 框架，满足商业级项目的稳定性、性能和可维护性要求。

### 核心原则

| 原则 | 说明 |
|------|------|
| **VC 分离** | View 只做表现逻辑，Controller 只做业务逻辑 |
| **1:1 约束** | 一个 Controller 对应且仅对应一个 View，由 UIController\<T\> 泛型保证 |
| **Manager 全权负责 View 生命周期** | 加载、缓存、实例化、挂载、销毁全部由 UIManager 掌控 |
| **接口极简** | 对外 `Open<T>()` / `Close<T>()`；对内 Controller 仅注入一个 `CloseAction` 委托 |
| **Type 即身份** | 框架内部以 `typeof(T)` 作为唯一标识，零字符串操作，零命名约定依赖 |
| **Controller 无框架依赖** | Controller 是纯 C# 类，不依赖 UnityEngine 运行时，可独立单元测试 |

### 职责边界（红线）

| 层 | 可以做什么 | 不可以做什么 |
|----|-----------|-------------|
| **UIView** (MonoBehaviour) | 动画、交互控制、遮罩、组件绑定 | 访问 Controller/UIContext、调用业务 API |
| **UIController\<T\>** (纯 C#) | 业务逻辑、事件订阅、数据刷新、声明 PrefabPath + Layer | 创建/加载/销毁 View、访问框架单例 |
| **UIManager** | 生命周期编排、View 加载/缓存/实例化、队列调度 | 处理具体业务逻辑 |

PrefabPath 和 Layer 由 Controller 的 `abstract` 属性声明，UIManager 直接从 `controller.PrefabPath` / `controller.Layer` 读取，不需要外部配置。

---

## 二、架构概览

```
┌──────────────────────────────────────────────────┐
│                    调用方                          │
│        Open<ShopController>() / Close<T>()        │
├──────────────────────────────────────────────────┤
│                UIManager                           │
│  ┌──────────────┐ ┌────────────────────────┐     │
│  │UIViewCache   │ │ UIControllerRegistry    │     │
│  │(隐藏View缓存) │ │ (工厂 + 持久化)         │     │
│  └──────────────┘ └────────────────────────┘     │
│  ┌──────────────────────────────────────────┐    │
│  │  队列调度 · 三层级栈 · 双通道动画 · 编排    │    │
│  └──────────────────────────────────────────┘    │
├──────────────────────┬───────────────────────────┤
│  UIView (表现层)       │  UIController<T> (逻辑层)   │
│  - PlayEnterAnimation│  - OnInit(首次,View已就绪) │
│  - PlayExitAnimation │  - OnOpen(args, 动画后)   │
│  - SetInteractive    │  - PrefabPath / Layer     │
│  - (无框架引用)       │  - CloseSelf()             │
│                      │  - (仅持有 CloseAction)     │
├──────────────────────┴───────────────────────────┤
│  AssetMgr (单例)                                  │
│  UIContext (internal)                             │
│  内部全部以 Type 做键                              │
└──────────────────────────────────────────────────┘
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

### 3.2 状态机 UIStateMachine

**文件：** Core/UIStateMachine.cs

```
None → Loading → AnimationEnter → Opened → AnimationExit → Closed
  ↓         ↓            ↓
Closed   Closed        Closed      (中断/失败紧急回退)
```

- 6 状态，`TryTransitionTo()` 校验合法性
- `IsTransitioning` 判断过渡中
- 异步操作前后检查状态替代 CancellationTokenSource

### 3.3 UIContext

**文件：** Core/UIContext.cs  
**可见性：** `internal`

```csharp
internal class UIContext
{
    public Type ControllerType { get; }        // typeof(ShopController)，框架内部唯一标识
    public UIView View { get; set; }
    public IUIController Controller { get; set; }
    public UIStateMachine StateMachine { get; } = new();

    public bool IsOpened      => StateMachine.CurrentState == UIState.Opened;
    public bool IsLoading     => StateMachine.CurrentState == UIState.Loading;
    public bool IsInAnimation => StateMachine.CurrentState == UIState.AnimationEnter
                              || StateMachine.CurrentState == UIState.AnimationExit;
    public bool IsClosed      => StateMachine.CurrentState == UIState.Closed;

    public bool TryTransition(UIState target) => StateMachine.TryTransitionTo(target);
    public void ForceClose();
    public void Dispose();
}
```

### 3.4 层级枚举 UILayer

| 层级 | 枚举值 | 行为 |
|------|--------|------|
| Background | 0 | 单例。打开时关闭其上所有 Normal 和 Popup |
| Normal | 1 | 栈管理。打开时关闭所有 Popup，上一个 OnHide，Back 时恢复 |
| Popup | 2 | 相互替换。新 Popup 关闭当前 Popup |

---

## 四、UIView 基类

**文件：** UIView/UIView.cs

纯表现层。不持有 Controller 引用，不持有 Context 引用。动画能力通过静态类 `AnimationFactory` 调用，不在基类中持有任何动画相关引用。

```csharp
public abstract class UIView : MonoBehaviour
{
    // === 子类重写 ===
    public virtual void PlayEnterAnimation(Action onComplete)
    {
        AnimationFactory.PlayEnter(UIAnimationType.Fade, (RectTransform)transform, 0.3f, onComplete);
    }

    public virtual void PlayExitAnimation(Action onComplete)
    {
        AnimationFactory.PlayExit(UIAnimationType.Fade, (RectTransform)transform, 0.2f, onComplete);
    }

    // === 交互控制 ===
    public void SetInteractive(bool enabled);
    public bool IsInteractable { get; internal set; }
}
```

| 方法 | 说明 |
|------|------|
| `PlayEnterAnimation(onComplete)` | 入场动画（virtual，默认淡入 0.3s） |
| `PlayExitAnimation(onComplete)` | 退场动画（virtual，默认淡出 0.2s） |
| `SetInteractive(bool)` | CanvasGroup 级别 interactable + blocksRaycasts |
| `IsInteractable` (bool 属性) | 框架设置的交互许可标记，业务层在按钮回调中自行判断 |

子类 override `PlayEnterAnimation` / `PlayExitAnimation` 时，通过 `AnimationFactory.PlayEnter/PlayExit(UIAnimationType.xxx, ...)` 选择动画效果。

---

## 五、Controller 体系

### 5.1 IUIController（公共接口）

**文件：** Controller/IUIController.cs

```csharp
public interface IUIController
{
    void SetView(UIView view);

    void OnInit();      // 首次：SetView 之后。View 已就绪
    void OnOpen(object args);   // 每次打开：动画结束后，携带 Open<T> 传入的参数。UI 可见可交互
    void OnHide();      // 退场前
    void OnDispose();   // 清理
}
```

### 5.2 UIController\<T\>（泛型基类）

**文件：** Controller/UIController.cs

```csharp
public abstract class UIController<T> : IUIController where T : UIView
{
    // === 框架注入 ===
    public T View { get; private set; }
    internal Action CloseAction { get; set; }  // 唯一注入

    // === 静态元数据（子类声明，UIManager 读取） ===
    public abstract string PrefabPath { get; }
    public abstract UILayer Layer { get; }

    // === IUIController 显式实现 ===
    void IUIController.SetView(UIView view)
    {
        View = view as T;
        if (View == null)
            Debug.LogError($"[UIFrameworkLib] 类型不匹配: {typeof(T).Name} vs {view?.GetType().Name}");
    }

    // === 生命周期（业务层重写） ===
    protected internal virtual void OnInit() { }
    protected internal virtual void OnOpen(object args) { }    // 每次 · 动画已结束
    protected internal virtual void OnHide() { }
    protected internal virtual void OnDispose() { }

    // === 辅助 ===
    protected void CloseSelf() => CloseAction?.Invoke();
}
```

**使用示例：**

```csharp
public class ShopController : UIController<ShopView>
{
    public override string PrefabPath => "Prefabs/ShopPanel";
    public override UILayer Layer => UILayer.Normal;

    protected internal override void OnInit()
    {
        View.m_BtnClose.onClick.AddListener(CloseSelf);
    }

    protected internal override void OnOpen(object args)
    {
        var categoryId = (int)args;
        // UI 已可见，刷新数据
        RefreshShopData(categoryId);
    }
}
```

---

## 六、UIManager

**文件：** Manager/UIManager.cs

### 6.1 对外接口

```csharp
public class UIManager : Singleton<UIManager>
{
    public void Open<T>(object args = null) where T : IUIController;
    public void Close<T>() where T : IUIController;
    public void CloseAll();
    public void ClearCache();

    // 调试用
    public int ActiveCount { get; }
    public int CachedViewCount => _viewCache.Count;
}
```

- `typeof(T)` 即为 Controller 唯一标识，无 string 重载

### 6.2 子模块：UIViewCache

**文件：** Manager/UIViewCache.cs

```csharp
internal class UIViewCache
{
    public GameObject Get(Type controllerType);
    public void Store(Type controllerType, GameObject go);
    public void Clear();
    public int Count { get; }
}
```

### 6.3 子模块：UIControllerRegistry

**文件：** Manager/UIControllerRegistry.cs

```csharp
internal class UIControllerRegistry
{
    private Dictionary<Type, IUIController> m_AllControllers = new();

    /// <summary>启动时扫描并创建所有 IUIController 实例</summary>
    public void InitControllers()
    {
        var subTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IUIController).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);

        m_AllControllers.Clear();
        foreach (var item in subTypes)
        {
            var con = Activator.CreateInstance(item) as IUIController;
            m_AllControllers[item] = con;  // Type 为键
        }
    }

    /// <summary>获取 Controller（首次调用 OnInit 由外部根据 View == null 判断）</summary>
    public T GetController<T>() where T : IUIController
    {
        return m_AllControllers.TryGetValue(typeof(T), out var c) ? (T)c : default;
    }

    public void Clear()
    {
        foreach (var kv in m_AllControllers)
            (kv.Value as IDisposable)?.Dispose();
        m_AllControllers.Clear();
    }
}
```

- `InitControllers()` 启动时调用一次，扫描并创建所有 `IUIController` 实例，Type 为键。
- `GetController<T>()` 直接返回已创建的实例。
- 不再需要 `isFirstTime` 标志——`UIController<T>.View == null` 即表示首次。

### 6.4 队列调度

```
Open<T>(args)：
  同一界面已 Opened → 直接 OnOpen(args)
  已有排队项 → 替换（队列深度 = 1）
  空闲 → 立即执行
```

### 6.5 层级与栈管理

| 层级 | 规则 |
|------|------|
| Background | 单例，打开时关闭所有 Normal 和 Popup |
| Normal | 入栈，打开时关闭所有 Popup，上一个 OnHide |
| Popup | 相互替换 |

---

## 七、AssetMgr 资源加载

**文件：** Manager/AssetMgr.cs

```csharp
public class AssetMgr : Singleton<AssetMgr>
{
    public async UniTask<GameObject> LoadPrefabAsync(string prefabPath)
    {
        var req = Resources.LoadAsync<GameObject>(prefabPath);
        await req.ToUniTask();
        return req.asset as GameObject;
    }
}
```

- 继承 `Singleton<AssetMgr>`，通过 `AssetMgr.Instance` 访问
- 需要切换 Addressables / AssetBundle 时直接修改此类源码

---

## 八、UIRoot

**文件：** UIView/UIRoot.cs

```
[UIRoot] (Canvas, SortingOrder=10000)
├── BackgroundLayer  (SortingOrder=0)
├── NormalLayer      (SortingOrder=100)
└── PopupLayer       (SortingOrder=200)
```

---

## 九、生命周期流程

### 9.1 打开流程

```
Open<T>(args)
  → Type key = typeof(T)
  → 队列调度
  → ExecuteOpen(key, args)

ExecuteOpen:
  1. controller = _registry.GetController<T>()
     → PrefabPath = controller.PrefabPath
     → Layer = controller.Layer
  2. 创建 Context(key), 状态 → Loading
  3. View 加载：
     a. _viewCache.Get(key) → 有缓存 SetActive(true)
     b. 无缓存 → AssetMgr.Instance.LoadPrefabAsync(PrefabPath)
     → Instantiate(prefab, UIRoot.GetLayer(Layer))
  4. 状态检查（仍 Loading？）
  5. bool isFirstView = controller.View == null
  6. controller.SetView(view)
  7. 注入 CloseAction 到 controller
  8. if (isFirstView) controller.OnInit()     ← 首次设置 View
  9. 层级注册 → 状态 AnimationEnter
  10. view.IsInteractable = false             ← 框架设置标记
  11. view.SetInteractive(false)              ← CanvasGroup 关闭射线
  12. view.PlayEnterAnimation() → await
  13. 状态 → Opened
  14. view.SetInteractive(true)
  15. view.IsInteractable = true              ← 框架恢复标记
  16. controller.OnOpen(args)                ← 携带原始参数
  17. ProcessNext()
```

### 9.2 关闭流程

```
StartExit(ctx):
  1. ctx.IsOpened 检查
  2. ctx.TryTransition(AnimationExit)
  3. view.IsInteractable = false
  4. controller.OnHide()
  5. view.SetInteractive(false)
  6. view.PlayExitAnimation() → await
  7. ctx.TryTransition(Closed)
  8. _viewCache.Store(ctx.ControllerType, view.gameObject)
  9. 栈出栈 + RestorePreviousNormal
  10. ProcessNext()
```

### 9.3 关闭全部

```
CloseAll():
  1. 清空队列
  2. 遍历活跃 Context → ForceClose + OnDispose + Destroy View
  3. _viewCache.Clear()
  4. _registry.Clear()
```

---

## 十、动画系统

### 10.1 设计原则

- UIView 不内置动画实现，通过静态类 `AnimationFactory` 调用
- 每个动画效果一个策略类，同时包含 Enter 和 Exit 两个动作
- 不做预注册，按枚举即时创建策略实例
- 默认使用 `UniTask` + `Time.deltaTime` 驱动
- 替换方式：继承重写策略类，或修改 `AnimationFactory` 的 switch 分支

### 10.2 动画类型枚举

**文件：** Animation/UIAnimationType.cs

```csharp
public enum UIAnimationType
{
    Fade,
    Scale,
    SlideUp,
    SlideDown,
    BlackFade,
    None,
}
```

### 10.3 策略接口

**文件：** Animation/IAnimationStrategy.cs

```csharp
public interface IAnimationStrategy
{
    void Enter(RectTransform target, float duration, Action onComplete);
    void Exit(RectTransform target, float duration, Action onComplete);
}
```

### 10.4 静态工厂 AnimationFactory

**文件：** Animation/AnimationFactory.cs

```csharp
public static class AnimationFactory
{
    private static readonly Dictionary<UIAnimationType, IAnimationStrategy> _cache = new();

    public static void PlayEnter(UIAnimationType type, RectTransform target, float duration, Action onComplete)
    {
        GetStrategy(type).Enter(target, duration, onComplete);
    }

    public static void PlayExit(UIAnimationType type, RectTransform target, float duration, Action onComplete)
    {
        GetStrategy(type).Exit(target, duration, onComplete);
    }

    private static IAnimationStrategy GetStrategy(UIAnimationType type)
    {
        if (!_cache.TryGetValue(type, out var strategy))
        {
            strategy = type switch
            {
                UIAnimationType.Fade      => new FadeStrategy(),
                UIAnimationType.Scale     => new ScaleStrategy(),
                UIAnimationType.SlideUp   => new SlideUpStrategy(),
                UIAnimationType.SlideDown => new SlideDownStrategy(),
                UIAnimationType.BlackFade => new BlackFadeStrategy(),
                _                         => new NoneStrategy(),
            };
            _cache[type] = strategy;
        }
        return strategy;
    }
}
```

### 10.5 动画策略类

**目录：** `Animation/Strategies/`

| 类 | 枚举 | Enter | Exit |
|----|------|-------|------|
| `FadeStrategy` | Fade | 透明度 0→1 | 透明度 1→0 |
| `ScaleStrategy` | Scale | 0→1.1→1（弹性） | 1→0 |
| `SlideUpStrategy` | SlideUp | 从下往上滑入 | 往上滑出 |
| `SlideDownStrategy` | SlideDown | 从上往下滑入 | 往下滑出 |
| `BlackFadeStrategy` | BlackFade | 黑屏渐入 | 黑屏渐出 |
| `NoneStrategy` | None | 立即回调 | 立即回调 |

示例策略实现：

```csharp
public class FadeStrategy : IAnimationStrategy
{
    public async void Enter(RectTransform target, float duration, Action onComplete)
    {
        var cg = target.GetComponent<CanvasGroup>();
        if (cg == null) { onComplete?.Invoke(); return; }
        cg.alpha = 0f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
        cg.alpha = 1f;
        onComplete?.Invoke();
    }

    public async void Exit(RectTransform target, float duration, Action onComplete)
    {
        var cg = target.GetComponent<CanvasGroup>();
        if (cg == null) { onComplete?.Invoke(); return; }
        cg.alpha = 1f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
        cg.alpha = 0f;
        onComplete?.Invoke();
    }
}
```

### 10.6 使用方式

```csharp
// UIView 子类
public class ShopView : UIView
{
    public override void PlayEnterAnimation(Action onComplete)
    {
        AnimationFactory.PlayEnter(UIAnimationType.Scale, (RectTransform)transform, 0.35f, onComplete);
    }
}
```

业务方自定义动画：继承 `ScaleStrategy` 重写 `Enter`/`Exit`，或修改 `AnimationFactory.GetStrategy` 的 switch 分支。

---

## 十一、编辑器调试窗口

**文件：** Editor/UIEditorWindow.cs

菜单 `UIFrameworkLib/UI Debugger`：活跃 UI 列表（显示 `ControllerType.Name`）、Normal 栈、缓存统计、CloseAll / ClearCache。

---

## 十二、目录结构

```
Assets/Scripts/UIFrameworkLib/
├── Core/
│   ├── Singleton.cs
│   ├── UILayer.cs
│   ├── UIStateMachine.cs
│   └── UIContext.cs              (internal)
├── Animation/
│   ├── UIAnimationType.cs
│   ├── IAnimationStrategy.cs
│   ├── AnimationFactory.cs         (静态入口)
│   └── Strategies/
│       ├── FadeStrategy.cs
│       ├── ScaleStrategy.cs
│       ├── SlideUpStrategy.cs
│       ├── SlideDownStrategy.cs
│       ├── BlackFadeStrategy.cs
│       └── NoneStrategy.cs
├── UIView/
│   ├── UIView.cs
│   └── UIRoot.cs
├── Controller/
│   ├── IUIController.cs
│   └── UIController.cs
├── Manager/
│   ├── UIManager.cs
│   ├── UIViewCache.cs
│   ├── UIControllerRegistry.cs
│   └── AssetMgr.cs
└── Editor/
    └── UIEditorWindow.cs
```



---

## 十三、变更记录

| 版本 | 变更内容 |
|------|---------|
| v1~v9 | （略） |
| v10 | 高内聚低耦合重构：UIContext 降 internal；移除 Controller View 创建能力；IUIController 去 Context；UIView 去 Controller 引用；UIManager 拆 4 子模块；接口化 |
| v11 | 精简过度设计：修 OnInit 时序 bug；IViewServiceProvider 改委托；合并 OnOpen/OnShown；去 IUIControllerFactory/特性/2 子模块；队列深度降 1；事件改直接调用 |
| v12 | **删除 UIKeyResolver，Type 即身份：** 框架内部全部以 `typeof(T)` 为标识；Controller 不再注入 Config，仅保留 CloseAction；去除所有 string 字典键，改为 Type 键 |
| v13 | **UIView 交互控制简化：** 删除 `DisableAllSelectables()` / `RestoreSelectables()`（快照+禁用所有按钮），改为 `IsInteractable` bool 属性 + `SetInteractive(bool)` CanvasGroup 控制；业务层在按钮回调中自行根据 `IsInteractable` 判断是否响应 |
| v14 | **删除 ShowMask/HideMask：** 遮罩是 Popup Prefab 内部视觉元素，由子类在 `PlayEnterAnimation/PlayExitAnimation` 中自行处理 |
| v15 | **IAssetMgr 重命名 + 动画策略类抽取：** `IUIResourceLoader` → `IAssetMgr`，`UIResourceLoader` → `AssetMgr`；动画能力从 UIView 内置 Helper 抽为独立策略类（`IAnimationStrategy`）+ 工厂（`IAnimationFactory`），UIView 改为组合获取 |
| v16 | **动画工厂改为静态：** `IAnimationFactory` / `DefaultAnimationFactory` 删除；新增静态类 `UIAnimation`（`Play()` + `SetProvider()`）；UIView 不再持有 `AnimationFactory` 属性，`UIManager` 不再有 `SetAnimationFactory()` |
| v17 | **删除 IAssetMgr 接口：** `AssetMgr` 直接继承 `Singleton<AssetMgr>`；删除 `Core/IAssetMgr.cs` |
| v18 | **AnimationFactory 重构：** `UIAnimation` 改名 `AnimationFactory`，`Play` 拆为 `PlayEnter`/`PlayExit`；策略类合并（FadeEnter+Exit→FadeStrategy），Enter/Exit 合入一个策略类；枚举精简（FadeEnter/Exit → Fade）；不做预注册字典，按枚举即时 new |
| v18.1 | **动画策略加缓存：** `AnimationFactory` 内部 `Dictionary<UIAnimationType, IAnimationStrategy>` 缓存首次创建的策略实例 |
| v19 | **修 OnOpen 参数 + AssetMgr 去 virtual：** `OnOpen()` 恢复为 `OnOpen(object args)`，已打开刷新时传递新参数；`AssetMgr.LoadPrefabAsync` 去除 `virtual`（Singleton 下继承重写无意义） |
| v20 | **UIControllerRegistry 预创建：** `InitControllers()` 启动时扫描+批量 `Activator.CreateInstance` 所有 Controller，`GetController<T>()` 直接返回；删除 `Register`/`GetOrCreate`/`AutoRegister`；`isFirstTime` 改为 `controller.View == null` 在 SetView 前判断 |
| v21 | **UIConfigLoader → UIConfigMgr 静态化：** 重命名并改为 `static class`；`Load()`/`Get()`/`Reload()` 改为静态方法；`FindType` 改为 `public static` 供调用；UIManager 不再持有 `ConfigLoader` 属性 |
| v22 | **删除 UIItemConfig + UIConfigMgr：** PrefabPath 和 Layer 移到 `UIController<T>` 的 `abstract` 属性，由子类声明；删除 `Core/UIItemConfig.cs`、`Config/UIConfigMgr.cs`；框架不再需要外部配置加载 |
