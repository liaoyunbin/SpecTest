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
- [x] 定义为 internal 内部接口
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

## 阶段 5：文档

### 5.1 设计文档
- [x] 更新 `openspec/changes/ui-manager/design.md`
- [x] 覆盖所有核心模块、架构分层、生命周期流程
- [x] 记录从 v1 到 v9 的变更历史

### 5.2 伪代码
- [x] 更新 `openspec/changes/ui-manager/pseudo-code.cs`
- [x] 覆盖所有模块的伪代码实现
- [x] 包含业务层使用示例
- [x] 包含生命周期流程图
- [x] 明确标注「状态机替代 CancellationToken」「UIPool 替代为隐藏缓存」等关键设计决策