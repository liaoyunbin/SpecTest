# UI 管理器 —— 实现任务

## 第一阶段：基础框架

### 1. 创建目录结构
- [ ] 创建 `Assets/Scripts/UIManager/` 目录
- [ ] 创建 `Core/`、`Data/`、`Animation/`、`Panels/` 子目录

### 2. 实现数据模型
- [ ] 创建 `PanelType.cs` — 枚举：Base/Normal/Dialog/Toast
- [ ] 创建 `PanelStackItem.cs` — 栈元素类
- [ ] 创建 `PanelConfig.cs` — 面板配置类

### 3. 实现 Panel 基类
- [ ] 创建 `Panel.cs` — 面板基类
- [ ] 定义 `PanelName` 抽象属性
- [ ] 定义 `panelType` 字段
- [ ] 定义 `BindComponents()` 抽象方法

### 4. 实现 Controller 基类
- [ ] 创建 `Controller.cs` — 控制器基类
- [ ] 定义 `OnOpen()`、`OnClose()` 抽象方法
- [ ] 创建泛型 `Controller<TPanel>` 类
- [ ] 定义 `SetPanel()` 方法
- [ ] 定义 `OnInit()`、`OnPause()`、`OnResume()`、`OnRefresh()`、`OnDispose()` 虚方法

### 5. 实现 Panel-Controller 注册表
- [ ] 创建 `PanelControllerRegistry.cs`
- [ ] 实现 `Register<TPanel, TController>()` 方法
- [ ] 实现 `GetControllerType()` 方法
- [ ] 实现 `CreateController()` 方法

### 6. 实现 UIManager 单例
- [ ] 创建 `UIManager.cs`，继承 Singleton
- [ ] 实现主面板栈 `Stack<PanelStackItem>`
- [ ] 实现提示面板列表 `List<Panel>`
- [ ] 实现已打开面板字典 `Dictionary<string, Panel>`
- [ ] 实现 Panel-Controller 映射 `Dictionary<Panel, Controller>`
- [ ] 实现缓存面板字典 `Dictionary<string, Panel>`
- [ ] 实现当前面板引用（CurrentBasePanel/CurrentNormalPanel/CurrentDialogPanel）

---

## 第二阶段：面板栈操作

### 7. 实现底层面板入栈
- [ ] 弹出当前底层面板
- [ ] 弹出所有普通面板
- [ ] 弹出所有弹窗面板
- [ ] 创建 Panel 实例
- [ ] 创建 Controller 并绑定
- [ ] 调用 Controller.OnOpen()
- [ ] 压入栈
- [ ] 更新 CurrentBasePanel

### 8. 实现普通面板入栈
- [ ] 弹出所有弹窗面板
- [ ] 创建 Panel 实例
- [ ] 创建 Controller 并绑定
- [ ] 调用 Controller.OnOpen()
- [ ] 压入栈（保留上个普通面板）
- [ ] 更新 CurrentNormalPanel

### 9. 实现弹窗面板入栈
- [ ] 判断栈顶是否为弹窗
- [ ] 如果是弹窗 → 替换
- [ ] 如果不是弹窗 → 正常压入
- [ ] 更新 CurrentDialogPanel

### 10. 实现提示面板入栈
- [ ] 独立管理，不影响主栈
- [ ] 支持同时存在多个提示
- [ ] 实现自动关闭逻辑

### 11. 实现关闭面板
- [ ] 实现关闭当前面板
- [ ] 调用 Controller.OnClose()
- [ ] 调用 Controller.Dispose()
- [ ] 实现关闭指定面板
- [ ] 恢复上层面板（OnResume）
- [ ] 更新当前面板引用

---

## 第三阶段：重复打开处理

### 12. 实现底层面板重复打开
- [ ] 检测是否已打开
- [ ] 已打开 → 忽略，返回现有实例

### 13. 实现普通面板重复打开
- [ ] 检测是否已在栈中
- [ ] 已是栈顶 → 忽略，返回现有实例
- [ ] 不在栈顶 → 移到栈顶 + 调用 Controller.OnRefresh

### 14. 实现弹窗面板重复打开
- [ ] 检测是否已打开
- [ ] 已打开 → 调用 Controller.OnRefresh，返回现有实例

### 15. 实现提示面板重复打开
- [ ] 允许重复，直接创建新实例

---

## 第四阶段：加载锁定

### 16. 实现加载状态管理
- [ ] 添加 isLoading 标志
- [ ] 添加 currentLoadingPanelName

### 17. 实现加载锁定逻辑
- [ ] 开始加载时设置 isLoading = true
- [ ] 加载过程中忽略所有新请求
- [ ] 加载完成后设置 isLoading = false

### 18. 实现加载遮罩
- [ ] 创建 LoadingMask 预制体
- [ ] 加载时显示遮罩
- [ ] 加载完成时隐藏遮罩

---

## 第五阶段：缓存策略

### 19. 实现缓存容器
- [ ] 实现 cachedPanels 字典
- [ ] 实现 cacheTimestamps 时间戳字典

### 20. 实现缓存获取
- [ ] 打开面板时检查缓存
- [ ] 缓存命中 → 复用 Panel，创建新 Controller
- [ ] 缓存未命中 → 加载预制体

### 21. 实现缓存清理
- [ ] 定期清理过期缓存
- [ ] 缓存数量超限时清理最旧的

---

## 第六阶段：过渡动画

### 22. 实现动画基类
- [ ] 创建 `PanelAnimation.cs`
- [ ] 定义 duration 和 curve 参数
- [ ] 定义 Play 方法

### 23. 实现淡入淡出动画
- [ ] 创建 `FadeAnimation.cs`
- [ ] 使用 CanvasGroup 控制透明度

### 24. 实现缩放动画
- [ ] 创建 `ScaleAnimation.cs`
- [ ] 使用 RectTransform.localScale

### 25. 集成动画到面板
- [ ] Panel 添加动画类型配置
- [ ] 打开时播放进入动画
- [ ] 关闭时播放退出动画

---

## 第七阶段：生命周期

### 26. 实现面板生命周期
- [ ] Controller.OnInit — Controller 初始化时调用
- [ ] Controller.OnOpen — 面板打开时调用
- [ ] Controller.OnClose — 面板关闭时调用
- [ ] Controller.OnPause — 被上层面板遮挡时调用
- [ ] Controller.OnResume — 恢复显示时调用
- [ ] Controller.OnRefresh — 重复打开时调用
- [ ] Controller.OnDispose — Controller 销毁时调用

---

## 第八阶段：配置系统

### 27. 实现配置加载
- [ ] 创建面板配置 JSON 文件
- [ ] 实现配置解析
- [ ] 实现 GetPanelConfig 方法

### 28. 实现预制体加载
- [ ] 从 Resources 加载预制体
- [ ] 缓存已加载的预制体

---

## 第九阶段：测试与验证

### 29. 创建测试面板
- [ ] 创建测试底层面板（MenuPanel + MenuController）
- [ ] 创建测试普通面板（ShopPanel + ShopController）
- [ ] 创建测试弹窗面板（ConfirmPanel + ConfirmController）
- [ ] 创建测试提示面板（ToastPanel + ToastController）

### 30. 注册测试面板
- [ ] 在初始化时注册所有测试面板到 PanelControllerRegistry

### 31. 验证功能
- [ ] 验证四种面板类型入栈行为
- [ ] 验证 Panel-Controller 绑定
- [ ] 验证重复打开处理
- [ ] 验证加载锁定
- [ ] 验证缓存策略
- [ ] 验证过渡动画
- [ ] 验证生命周期调用

---

## 优先级

| 优先级 | 阶段 | 说明 |
|--------|------|------|
| 高 | 第一阶段 | 基础框架，Panel + Controller 架构 |
| 高 | 第二阶段 | 面板栈操作，核心功能 |
| 高 | 第三阶段 | 重复打开处理，边界情况 |
| 高 | 第四阶段 | 加载锁定，稳定性保障 |
| 中 | 第五阶段 | 缓存策略，性能优化 |
| 中 | 第六阶段 | 过渡动画，用户体验 |
| 中 | 第七阶段 | 生命周期，扩展性 |
| 低 | 第八阶段 | 配置系统，灵活性 |
| 低 | 第九阶段 | 测试验证，质量保障 |
