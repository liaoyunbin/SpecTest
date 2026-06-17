# UIFrameworkLib — 实现任务

## 阶段 1：核心模型（纯 C#，无 Unity 依赖）

### 1.1 UILayer
- [x] 创建 `Core/UILayer.cs`
- [x] 定义枚举：Normal / Popup / Toast / Loading

### 1.2 UIStateMachine
- [x] 创建 `Core/UIStateMachine.cs`
- [x] 定义 UIState 枚举：None→Loading→AnimationEnter→Opened→AnimationExit→Closed
- [x] 实现 ValidTransitions 合法转换映射
- [x] 实现 TryTransitionTo() 方法
- [x] 实现 IsTransitioning 属性

### 1.3 UIItemConfig
- [x] 创建 `Core/UIItemConfig.cs`
- [x] 定义配置字段：UIKey / PrefabPath / Layer / IsSingleton / CacheOnClose / IsModal / ControllerTypeName
- [x] 实现 FromLuban() 转换方法
- [x] 定义 LubanUIConfig 示意类

### 1.4 UIContext
- [x] 创建 `Core/UIContext.cs`
- [x] 定义标识：UIKey / InstanceId
- [x] 定义核心引用：View / Controller / Config
- [x] 定义状态：StateMachine / Cts / OpenTime / PreviousContext
- [x] 实现 Bind() / Cancel() / Dispose()

---

## 阶段 2：Controller 和 View 基类

### 2.1 IUIController
- [x] 创建 `Controller/IUIController.cs`
- [x] 定义内部接口：BindContext / OnInit / OnOpen / OnShown / OnHide / OnDispose

### 2.2 UIController<T>
- [x] 创建 `Controller/UIController.cs`
- [x] 实现泛型基类，持有强类型 View 引用
- [x] 实现 IUIController 接口
- [x] 定义 5 个生命周期虚方法
- [x] 实现 CloseSelf() 辅助方法

### 2.3 UIView
- [x] 创建 `UIView/UIView.cs`
- [x] 定义 AutoBind() 抽象方法（UIMark 生成）
- [x] 实现 CanvasGroup 交互控制
- [x] 实现 Selectable 快照（禁用/恢复）
- [x] 实现默认入场/退场动画（DOTween 淡入淡出）
- [x] 定义 ShowMask() / HideMask() 虚方法

### 2.4 UIRoot
- [x] 创建 `UIView/UIRoot.cs`
- [x] 实现 [RuntimeInitializeOnLoadMethod] 自动初始化
- [x] 创建 4 个层级节点（各带 Canvas + GraphicRaycaster）
- [x] 实现 GetLayer() 方法

---

## 阶段 3：Manager 核心

### 3.1 UIPool
- [x] 创建 `Manager/UIPool.cs`
- [x] 实现 Get() / Return() 方法
- [x] 限制每 UIKey 最多缓存 5 个
- [x] 实现 Clear() 方法

### 3.2 UIManager
- [x] 创建 `Manager/UIManager.cs`
- [x] 实现 Singleton
- [x] 实现 Open<T>() / Close<T>() / Open(string) 接口
- [x] 实现 CloseAll() / ClearPool() 维护方法
- [x] 实现请求队列（上限 10）
- [x] 实现双通道动画（_exitingContext / _enteringContext）
- [x] 实现异步加载（Resources.LoadAsync）
- [x] 实现超时看门狗
- [x] 实现安全执行（全局 try-catch）
- [x] 实现 Normal 栈管理

---

## 阶段 4：配置和编辑器工具

### 4.1 UIConfigLoader
- [x] 创建 `Config/UIConfigLoader.cs`
- [x] 实现 LoadFromLuban() 批量加载
- [x] 实现 Get() / GetAllKeys() 查询
- [x] 支持运行时热重载

### 4.2 UIMark
- [x] 创建 `Editor/UIMark.cs`
- [x] 定义标记组件 + Comment 字段

### 4.3 UIEditorWindow
- [x] 创建 `Editor/UIEditorWindow.cs`
- [x] 显示活跃 UI 列表和状态
- [x] 显示 Normal 栈
- [x] 显示 Pool 统计
- [x] 提供 CloseAll / ClearPool 操作按钮
- [x] 提供手动打开 UI 功能（下拉选择）
- [x] 运行时自动刷新