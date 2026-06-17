# UIFrameworkLib — 实现任务

## 阶段 1：核心模型

### 1.1 UILayer
- [x] 创建 `Core/UILayer.cs`
- [x] 定义三种层级：Background / Normal / Popup
- [x] 添加层级行为注释

### 1.2 UIStateMachine
- [x] 创建 `Core/UIStateMachine.cs`
- [x] 定义 UIState 枚举：None→Loading→AnimationEnter→Opened→AnimationExit→Closed
- [x] 实现 ValidTransitions 合法转换映射
- [x] 实现 TryTransitionTo() 方法
- [x] 实现 IsTransitioning 属性

### 1.3 UIItemConfig
- [x] 创建 `Core/UIItemConfig.cs`
- [x] 定义精简配置字段：UIKey / PrefabPath / Layer
- [x] 去除 Luban 依赖，与外部数据格式解耦

### 1.4 UIContext
- [x] 创建 `Core/UIContext.cs`
- [x] 定义标识：UIKey / InstanceId
- [x] 定义核心引用：View / Controller / Config
- [x] 定义状态：StateMachine / OpenTime / PreviousContext
- [x] 分离 BindController() 和 SetView() 两步绑定
- [x] 去除 CancellationTokenSource，使用状态机检查替代
- [x] 实现 Dispose()

### 1.5 Singleton<T>
- [x] 创建 `Core/Singleton.cs`
- [x] 实现线程安全双检锁单例
- [x] UIManager 和 UIResourceLoader 继承此基类

---

## 阶段 2：Controller 和 View 基类

### 2.1 IUIController
- [x] 创建 `Controller/IUIController.cs`
- [x] 定义为 public 公共接口
- [x] 定义：BindContext / SetView / OnInit / OnOpen / OnShown / OnHide / OnDispose

### 2.2 UIController<T>
- [x] 创建 `Controller/UIController.cs`
- [x] 实现泛型基类，持有强类型 View 引用
- [x] 实现 IUIController 接口（显式实现 + 生命周期转发）
- [x] 定义抽象方法 CreateViewAsync() — 折中方案核心
- [x] 实现工具方法 LoadFromResources() / LoadFromCache()
- [x] 定义 5 个生命周期虚方法
- [x] 实现 CloseSelf() 辅助方法
- [x] 内部实例化 Prefab 到 UIRoot 对应层级

### 2.3 UIView
- [x] 创建 `UIView/UIView.cs`
- [x] 去除 UIMark 和 AutoBind，改为手动绑定
- [x] 实现 CanvasGroup 交互控制
- [x] 实现 Selectable 快照（禁用/恢复）
- [x] 实现默认入场/退场动画（FadeEnter / FadeExit）
- [x] 添加内置动画 Helper：Fade / Scale / SlideUp / SlideDown / BlackFade
- [x] 定义 ShowMask() / HideMask() 虚方法
- [x] 所有动画使用 UniTask + Time.deltaTime 驱动

### 2.4 UIRoot
- [x] 创建 `UIView/UIRoot.cs`
- [x] 实现 [RuntimeInitializeOnLoadMethod] 自动初始化
- [x] 创建 3 个层级节点：BackgroundLayer(SO=0) / NormalLayer(SO=100) / PopupLayer(SO=200)
- [x] 实现 GetLayer() 方法
- [x] DontDestroyOnLoad 跨场景持久化

---

## 阶段 3：Manager 核心

### 3.1 去除 UIPool → 替换为隐藏 View 缓存
- [x] 删除 UIPool.cs
- [x] 在 UIManager 中添加 CachedViews 字典
- [x] 实现 GetCachedView() / CacheView() / ClearCache()
- [x] 关闭 UI 时隐藏缓存（SetActive(false)），不销毁

### 3.2 UIManager
- [x] 创建 `Manager/UIManager.cs`
- [x] 继承 Singleton<UIManager>
- [x] 实现 Open<T>() / Close<T>() / Open(string) / Close(string) 接口
- [x] 实现 CloseAll() / ClearCache() 维护方法
- [x] 实现请求队列（上限 10，去重，满丢弃最旧）
- [x] 实现双通道动画（_exitingContext / _enteringContext）
- [x] 实现异步加载（委托 Controller.CreateViewAsync）
- [x] 实现 Controller 持久化（首次创建后常驻）
- [x] 使用状态机检查替代 CancellationTokenSource
- [x] 实现安全执行（全局 try-catch）
- [x] 实现 Normal 栈管理
- [x] 实现 UIKey 按命名约定解析（类名去 "Controller" 后缀）

### 3.3 UIResourceLoader
- [x] 创建 `Manager/UIResourceLoader.cs`
- [x] 继承 Singleton<UIResourceLoader>
- [x] 职责单一：仅从 Resources 异步加载 Prefab，返回原始 GameObject
- [x] 不负责实例化、不负责缓存、不负责对象池
- [x] 实现超时看门狗（仅日志警告）

---

## 阶段 4：配置和编辑器工具

### 4.1 UIConfigLoader
- [x] 创建 `Config/UIConfigLoader.cs`
- [x] 实现 Load() 批量加载（接受 List<UIItemConfig>）
- [x] 实现 Get() / GetAllKeys() 查询
- [x] 支持运行时热重载

### 4.2 UIEditorWindow
- [x] 创建 `Editor/UIEditorWindow.cs`
- [x] 显示活跃 UI 列表和状态 + 层级 + 打开时间
- [x] 显示 Normal 栈
- [x] 显示缓存 View 统计（替代 Pool 统计）
- [x] 提供 CloseAll / ClearCache 操作按钮
- [x] 提供手动打开 UI 功能（下拉选择）
- [x] 运行时自动刷新

---

## 阶段 5：v12 — Type 即身份 + Controller 零注入

### 5.1 删除 UIKeyResolver
- [ ] 删除 `Core/UIKeyResolver.cs`
- [ ] 框架内部全部 `Dictionary<Type, ...>` 替代 `Dictionary<string, ...>`
- [ ] `UIContext.UIKey` (string) → `UIContext.ControllerType` (Type)

### 5.2 UIContext 精简 + 降级
- [ ] `UIContext` 可见性从 `public` 改为 `internal`
- [ ] 删除 `InstanceId` / `PreviousContext` / `BindController()` / `SetView()`
- [ ] 新增语义方法：`IsOpened` / `IsLoading` / `IsInAnimation` / `IsClosed` / `TryTransition()` / `ForceClose()`
- [ ] Manager 中所有 `ctx.StateMachine.CurrentState ==` 改为 `ctx.IsOpened` 等

### 5.3 IUIResourceLoader 接口
- [ ] 创建 `Core/IUIResourceLoader.cs`（`LoadPrefabAsync`）

### 5.4 IUIController 精简
- [ ] 删除 `UIContext Context` 和 `BindContext()`
- [ ] 删除 `OnShown()`（合并到 `OnOpen`，动画后调用）
- [ ] `OnOpen` 不再带 args 参数

### 5.5 UIController\<T\> 精简
- [ ] 仅注入 `internal Action CloseAction`，不注入 Config
- [ ] 删除 `CreateViewAsync()` / `LoadFromResources()` / `LoadFromCache()` / `InstantiateView()`
- [ ] `OnInit()` 在 `SetView()` 之后调用

### 5.6 UIView 移除反向引用
- [ ] 删除 `public IUIController Controller` / `public UIContext Context` / `Internal_SetContext()`

### 5.7 UIManager 只拆 2 子模块
- [ ] 创建 `Manager/UIViewCache.cs`（Type 为键）
- [ ] 创建 `Manager/UIControllerRegistry.cs`（Type 为键；AutoRegister；FindType 仅用于外部配置 string→Type）
- [ ] 队列调度 / 层级栈管理 留在 UIManager 内（队列深度 1）

### 5.8 View 加载权移交 UIManager
- [ ] 删掉 `controller.CreateViewAsync()`，改为 UIManager 全权加载
- [ ] 注入 `CloseAction` + `controller.SetView(view)`
- [ ] `if (isFirstTime) controller.OnInit()` 在 SetView 之后

### 5.9 Controller 工厂替代反射拼接
- [ ] 删除 `UIManager.CreateController(string typeName)`
- [ ] 改为 `UIControllerRegistry` 字典注册（Type 为键）

### 5.10 UIItemConfig 瘦身
- [ ] 删除 `UIItemConfig.UIKey`
- [ ] `UIConfigLoader.Load()` 入口接收 `List<(string uiKey, UIItemConfig)>`，内部转换一次后以 Type 存储

### 5.11 设计文档
- [x] 更新 `design.md` 至 v12
- [x] 更新架构图、模块说明、生命周期流程、目录结构
- [x] 记录 v12 vs v11 变更对比表

### 5.12 EditorWindow 适配
- [ ] 适配 Type 键 API + `ctx.IsOpened` 替代 `StateMachine.CurrentState`

---

## 阶段 6：v15 — IAssetMgr 重命名 + 动画策略抽取

### 6.1 资源加载重命名
- [ ] `IUIResourceLoader.cs` → `Core/IAssetMgr.cs`
- [ ] `UIResourceLoader.cs` → `Manager/AssetMgr.cs`
- [ ] 全局替换所有引用

### 6.2 动画策略接口
- [ ] 创建 `Animation/IAnimationStrategy.cs`（`void Play(RectTransform, float, Action)`)
- [ ] 创建 `Animation/IAnimationFactory.cs`（`IAnimationStrategy GetStrategy(UIAnimationType)`）

### 6.3 动画策略类实现
- [ ] 创建 `Animation/UIAnimationType.cs`（枚举，FadeEnter/Exit, ScaleEnter/Exit, SlideUp/Down Enter/Exit, BlackFade Enter/Exit, None）
- [ ] 创建 `Animation/Strategies/FadeEnterStrategy.cs`
- [ ] 创建 `Animation/Strategies/FadeExitStrategy.cs`
- [ ] 创建 `Animation/Strategies/ScaleEnterStrategy.cs`
- [ ] 创建 `Animation/Strategies/ScaleExitStrategy.cs`
- [ ] 创建 `Animation/Strategies/SlideUpEnterStrategy.cs`
- [ ] 创建 `Animation/Strategies/SlideUpExitStrategy.cs`
- [ ] 创建 `Animation/Strategies/SlideDownEnterStrategy.cs`
- [ ] 创建 `Animation/Strategies/SlideDownExitStrategy.cs`
- [ ] 创建 `Animation/Strategies/BlackFadeEnterStrategy.cs`
- [ ] 创建 `Animation/Strategies/BlackFadeExitStrategy.cs`
- [ ] 创建 `Animation/Strategies/NoneStrategy.cs`

### 6.4 默认工厂
- [ ] 创建 `Animation/DefaultAnimationFactory.cs`（字典映射 枚举→策略实例）

### 6.5 UIView 重构
- [ ] 删除所有内置动画 Helper 方法（FadeEnter/Exit, ScaleEnter/Exit, SlideUp/Enter/Exit, SlideDown Enter/Exit, BlackFade Enter/Exit, Fade 私有方法, Scale 私有方法）
- [ ] 新增 `internal IAnimationFactory AnimationFactory { get; set; }` 属性（默认 DefaultAnimationFactory）
- [ ] `PlayEnterAnimation` / `PlayExitAnimation` 默认实现改为 `AnimationFactory.GetStrategy(type).Play(...)`
- [ ] 删除 `UIAnimationType` 旧枚举（与 UIView 同文件），迁移到独立 `Animation/UIAnimationType.cs`

### 6.6 UIManager 注入
- [ ] 新增 `public void SetAnimationFactory(IAnimationFactory factory)` 方法
- [ ] View 加载后注入 `AnimationFactory` 到 View

### 6.7 设计文档
- [x] 更新 `design.md` 至 v15
- [x] 更新目录结构、架构图、动画系统章节
- [x] 记录 v15 变更