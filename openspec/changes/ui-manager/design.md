# UI 管理器 —— 设计文档

## 整体架构

```
┌─────────────────────────────────────────────────────────────────┐
│                        UIManager                                │
│                        (Singleton)                              │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐      │
│  │  PanelStack  │    │  CacheMgr    │    │ AnimationMgr │      │
│  │  面板栈管理  │    │  缓存管理    │    │  动画管理    │      │
│  └──────┬───────┘    └──────┬───────┘    └──────┬───────┘      │
│         │                   │                   │               │
│         ▼                   ▼                   ▼               │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                    UIRoot (Canvas)                       │    │
│  │                                                         │    │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐   │    │
│  │  │  ToastLayer  │  │ DialogLayer  │  │ PanelLayer   │   │    │
│  │  │  (提示层)    │  │  (弹窗层)    │  │ (普通+底层)  │   │    │
│  │  └──────────────┘  └──────────────┘  └──────────────┘   │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## Panel + Controller 架构

### 架构设计

```
┌─────────────────────────────────────────────────────────────────┐
│                    Panel + Controller 架构                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │                                                         │   │
│   │   ┌─────────────────┐       ┌─────────────────┐        │   │
│   │   │     Panel       │       │   Controller    │        │   │
│   │   │                 │       │                 │        │   │
│   │   │  - UI组件引用   │ ◄──── │  - 数据管理     │        │   │
│   │   │  - 无逻辑代码   │       │  - 业务逻辑     │        │   │
│   │   │  - 纯展示      │ ────► │  - 事件响应     │        │   │
│   │   │                 │       │  - UI更新       │        │   │
│   │   └─────────────────┘       └─────────────────┘        │   │
│   │                                                         │   │
│   └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│   特点:                                                         │
│   - Panel 只持有 UI 组件引用，不包含任何逻辑                     │
│   - Controller 持有 Panel 引用，负责所有逻辑                     │
│   - 数据直接在 Controller 中管理                                 │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 职责划分

```
┌─────────────────────────────────────────────────────────────────┐
│                      职责划分                                   │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Panel 层职责:                                                   │
│  ─────────────                                                  │
│  - 持有 UI 组件引用（SerializeField）                            │
│  - 绑定组件到 Controller                                        │
│  - 不包含任何业务逻辑                                            │
│  - 不包含数据                                                    │
│                                                                 │
│  Controller 层职责:                                              │
│  ─────────────────                                              │
│  - 管理面板数据                                                  │
│  - 处理业务逻辑                                                  │
│  - 响应 UI 事件                                                  │
│  - 更新 UI 显示                                                  │
│  - 调用服务层                                                    │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 四种面板类型

### 层级结构

```
┌─────────────────────────────────────────────────────────────────┐
│                         面板层级                                │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   Layer 4    ┌──────────────────────────────────────────────┐  │
│   提示层     │  Toast / Tips / Loading                      │  │
│               │  (独立层，对其余三种无响应)                    │  │
│               └──────────────────────────────────────────────┘  │
│                                                                 │
│   Layer 3    ┌──────────────────────────────────────────────┐  │
│   弹窗层     │  Dialog / Confirm / Alert                    │  │
│               │  (入栈时互相替换)                             │  │
│               └──────────────────────────────────────────────┘  │
│                                                                 │
│   Layer 2    ┌──────────────────────────────────────────────┐  │
│   普通层     │  Inventory / Shop / Profile                  │  │
│               │  (可叠加，入栈时弹出上层弹窗)                  │  │
│               └──────────────────────────────────────────────┘  │
│                                                                 │
│   Layer 1    ┌──────────────────────────────────────────────┐  │
│   底层       │  MainMenu / GameUI / BattleUI               │  │
│               │  (入栈时替换，弹出所有上层面板)                │  │
│               └──────────────────────────────────────────────┘  │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 入栈行为规则

```
┌──────────────┬─────────────────────────────────────────────────┐
│ 新面板类型    │ 入栈行为                                        │
├──────────────┼─────────────────────────────────────────────────┤
│ 底层面板      │ 1. 弹出当前底层面板                             │
│              │ 2. 弹出所有普通面板                             │
│              │ 3. 弹出所有弹窗面板                             │
│              │ 4. 压入新底层面板                               │
├──────────────┼─────────────────────────────────────────────────┤
│ 普通面板      │ 1. 弹出所有弹窗面板                             │
│              │ 2. 压入新普通面板（上个普通面板保留）             │
├──────────────┼─────────────────────────────────────────────────┤
│ 弹窗面板      │ 如果栈顶是弹窗 → 替换                           │
│              │ 如果栈顶不是弹窗 → 正常压入                      │
├──────────────┼─────────────────────────────────────────────────┤
│ 提示面板      │ 独立显示，不影响主栈                            │
│              │ 可同时存在多个提示                              │
└──────────────┴─────────────────────────────────────────────────┘
```

---

## 数据模型

### 核心类定义

```csharp
public enum PanelType
{
    Base,       // 底层面板
    Normal,     // 普通面板
    Dialog,     // 弹窗面板
    Toast       // 提示面板
}

// Panel 基类 - 只持有组件引用
public abstract class Panel : MonoBehaviour
{
    public abstract string PanelName { get; }
    public PanelType panelType;
    
    protected virtual void Awake()
    {
        BindComponents();
    }
    
    protected abstract void BindComponents();
}

// Controller 基类 - 负责逻辑和数据
public abstract class Controller
{
    public abstract void OnOpen(params object[] args);
    public abstract void OnClose();
}

// 泛型 Controller
public abstract class Controller<TPanel> : Controller where TPanel : Panel
{
    protected TPanel panel;
    
    public void SetPanel(TPanel panel)
    {
        this.panel = panel;
        OnInit();
    }
    
    protected virtual void OnInit() { }
    
    public virtual void OnPause() { }
    public virtual void OnResume() { }
    public virtual void OnRefresh(params object[] args) { }
    
    public void Dispose()
    {
        OnDispose();
        panel = null;
    }
    
    protected virtual void OnDispose() { }
}

// UIManager
public class UIManager : Singleton<UIManager>
{
    // 主面板栈
    private Stack<PanelStackItem> mainStack = new Stack<PanelStackItem>();
    
    // 提示面板列表（独立管理）
    private List<Panel> toastList = new List<Panel>();
    
    // 已打开的面板
    private Dictionary<string, Panel> openPanels = new Dictionary<string, Panel>();
    
    // Panel -> Controller 映射
    private Dictionary<Panel, Controller> panelControllers = new Dictionary<Panel, Controller>();
    
    // 缓存的面板
    private Dictionary<string, Panel> cachedPanels = new Dictionary<string, Panel>();
    
    // 当前各层最顶部的面板
    public Panel CurrentBasePanel { get; private set; }
    public Panel CurrentNormalPanel { get; private set; }
    public Panel CurrentDialogPanel { get; private set; }
    
    // 加载状态
    private bool isLoading = false;
    private string currentLoadingPanelName;
}

public class PanelStackItem
{
    public Panel panel;
    public PanelType type;
    public object[] args;
}

public class PanelConfig
{
    public string panelName;
    public string prefabPath;
    public PanelType panelType;
    public Type controllerType;
    public bool cacheOnClose = false;
    public float toastDuration = 2f;
}
```

### Panel-Controller 注册表

```csharp
public static class PanelControllerRegistry
{
    private static Dictionary<Type, Type> registry = new Dictionary<Type, Type>();
    
    public static void Register<TPanel, TController>()
        where TPanel : Panel
        where TController : Controller<TPanel>, new()
    {
        registry[typeof(TPanel)] = typeof(TController);
    }
    
    public static Type GetControllerType(Type panelType)
    {
        return registry.TryGetValue(panelType, out var ctrlType) ? ctrlType : null;
    }
    
    public static Controller CreateController(Type controllerType)
    {
        return Activator.CreateInstance(controllerType) as Controller;
    }
}
```

---

## 具体实现示例

### Shop 面板示例

```csharp
// Panel - 只有组件引用
public class ShopPanel : Panel
{
    public override string PanelName => "Shop";
    
    [Header("组件引用")]
    public Transform content;
    public GameObject itemPrefab;
    public Text goldText;
    public Button buyButton;
    public Button closeButton;
    
    protected override void BindComponents()
    {
        // 组件已通过 SerializeField 绑定
    }
}

// Controller - 所有逻辑
public class ShopController : Controller<ShopPanel>
{
    private List<ShopItem> items = new List<ShopItem>();
    private int selectedItemId = -1;
    private int playerGold;
    private ShopService shopService;
    
    protected override void OnInit()
    {
        shopService = ServiceLocator.Get<ShopService>();
        
        panel.buyButton.onClick.AddListener(OnBuyClick);
        panel.closeButton.onClick.AddListener(OnCloseClick);
    }
    
    public override void OnOpen(params object[] args)
    {
        int categoryId = args.Length > 0 ? (int)args[0] : 0;
        
        LoadData(categoryId);
        RefreshUI();
    }
    
    public override void OnClose()
    {
        items.Clear();
        selectedItemId = -1;
    }
    
    private void LoadData(int categoryId)
    {
        items = shopService.GetItems(categoryId);
        playerGold = PlayerData.gold;
    }
    
    private void RefreshUI()
    {
        panel.goldText.text = playerGold.ToString();
        
        foreach (Transform child in panel.content)
        {
            Destroy(child.gameObject);
        }
        
        foreach (var item in items)
        {
            var itemObj = Instantiate(panel.itemPrefab, panel.content);
            var itemUI = itemObj.GetComponent<ShopItemUI>();
            itemUI.SetData(item, OnItemClick);
        }
    }
    
    private void OnItemClick(int itemId)
    {
        selectedItemId = itemId;
    }
    
    private void OnBuyClick()
    {
        if (selectedItemId < 0) return;
        
        var item = items.Find(i => i.id == selectedItemId);
        if (item == null) return;
        
        if (playerGold >= item.price)
        {
            playerGold -= item.price;
            PlayerData.gold = playerGold;
            PlayerData.AddItem(item);
            
            panel.goldText.text = playerGold.ToString();
            UIManager.Instance.ShowToast("购买成功");
        }
        else
        {
            UIManager.Instance.ShowToast("金币不足");
        }
    }
    
    private void OnCloseClick()
    {
        UIManager.Instance.ClosePanel();
    }
    
    protected override void OnDispose()
    {
        panel.buyButton.onClick.RemoveListener(OnBuyClick);
        panel.closeButton.onClick.RemoveListener(OnCloseClick);
    }
}
```

---

## 重复打开处理策略

```
┌──────────────┬─────────────────────────────────────────────────┐
│ 面板类型      │ 重复打开行为                                    │
├──────────────┼─────────────────────────────────────────────────┤
│ 底层面板      │ 忽略                                            │
│              │ 已打开则直接返回现有实例，不做任何操作             │
├──────────────┼─────────────────────────────────────────────────┤
│ 普通面板      │ 置顶                                            │
│              │ 已在栈中 → 移到栈顶 + 调用 OnRefresh             │
│              │ 已是栈顶 → 忽略，直接返回                        │
├──────────────┼─────────────────────────────────────────────────┤
│ 弹窗面板      │ 忽略 + 刷新                                     │
│              │ 已打开 → 调用 OnRefresh，不重新创建              │
├──────────────┼─────────────────────────────────────────────────┤
│ 提示面板      │ 允许重复                                        │
│              │ 可同时显示多个相同 Toast                         │
└──────────────┴─────────────────────────────────────────────────┘
```

---

## 加载中锁定策略

```
┌─────────────────────────────────────────────────────────────────┐
│                    加载中锁定策略                                │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  规则:                                                          │
│  1. 开始加载面板时，设置 isLoading = true                        │
│  2. 加载过程中，所有新请求直接忽略                               │
│  3. 加载完成后，设置 isLoading = false                          │
│  4. 显示加载遮罩，提示用户等待                                   │
│                                                                 │
│  示例:                                                          │
│  t=0s  点击 B → 开始加载，锁定                                   │
│  t=1s  点击 C → 忽略                                            │
│  t=2s  点击 D → 忽略                                            │
│  t=5s  B 加载完成，解锁                                          │
│  结果: 只打开 B                                                  │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 缓存策略

```
┌─────────────────────────────────────────────────────────────────┐
│                        缓存策略                                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  配置项:                                                        │
│  - cacheOnClose: 关闭时是否缓存                                  │
│  - maxCacheCount: 最大缓存数量（默认 10）                        │
│  - cacheExpireTime: 缓存过期时间（默认 300s）                    │
│                                                                 │
│  缓存清理:                                                       │
│  1. 定期清理过期缓存（每 30s 检查一次）                          │
│  2. 缓存数量超限时，清理最旧的                                   │
│                                                                 │
│  缓存命中:                                                       │
│  - 从缓存取出面板                                               │
│  - SetActive(true)                                              │
│  - 创建新 Controller 并绑定                                     │
│  - 调用 OnOpen()                                                │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 过渡动画

### 动画类型

```csharp
public enum PanelAnimationType
{
    None,           // 无动画
    Fade,           // 淡入淡出
    Scale,          // 缩放
    SlideLeft,      // 左滑入
    SlideRight,     // 右滑入
    SlideTop,       // 上滑入
    SlideBottom     // 下滑入
}
```

### 动画参数

```csharp
public class PanelAnimation : MonoBehaviour
{
    public float duration = 0.3f;
    public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
}
```

---

## 面板生命周期

```
┌─────────────────────────────────────────────────────────────────┐
│                       面板生命周期                              │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  打开流程:                                                       │
│  1. 检查缓存/加载预制体                                          │
│  2. 实例化 Panel                                                │
│  3. 创建 Controller 并绑定 Panel                                 │
│  4. 调用 Controller.OnOpen(args)                                │
│  5. 播放打开动画                                                 │
│  6. 压入栈                                                       │
│                                                                 │
│  关闭流程:                                                       │
│  1. 播放关闭动画                                                 │
│  2. 调用 Controller.OnClose()                                   │
│  3. 调用 Controller.Dispose()                                   │
│  4. 弹出栈                                                       │
│  5. 缓存或销毁 Panel                                            │
│                                                                 │
│  暂停/恢复:                                                      │
│  - 被上层面板遮挡时 → Controller.OnPause()                       │
│  - 恢复显示时 → Controller.OnResume()                            │
│                                                                 │
│  刷新:                                                          │
│  - 重复打开时 → Controller.OnRefresh(args)                       │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## API 设计

```csharp
// 注册面板和控制器（初始化时调用）
PanelControllerRegistry.Register<ShopPanel, ShopController>();
PanelControllerRegistry.Register<BagPanel, BagController>();
PanelControllerRegistry.Register<MenuPanel, MenuController>();

// 打开面板
UIManager.Instance.OpenPanel<ShopPanel>(categoryId);

// 关闭面板
UIManager.Instance.ClosePanel();                    // 关闭当前面板
UIManager.Instance.ClosePanel<ShopPanel>();        // 关闭指定面板

// 显示弹窗
UIManager.Instance.ShowDialog("提示", "确定删除？", DialogType.Confirm, 
    onConfirm: () => { /* 确认 */ },
    onCancel: () => { /* 取消 */ }
);

// 显示提示
UIManager.Instance.ShowToast("保存成功", ToastType.Success);
UIManager.Instance.ShowToast("网络异常", ToastType.Error, duration: 3f);

// 获取面板
var shop = UIManager.Instance.GetPanel<ShopPanel>();

// 清空面板栈
UIManager.Instance.ClearPanelStack();
```

---

## 目录结构

```
Assets/
└── Scripts/
    └── UIManager/
        ├── Core/
        │   ├── UIManager.cs
        │   ├── Panel.cs
        │   ├── Controller.cs
        │   ├── PanelControllerRegistry.cs
        │   └── PanelConfig.cs
        ├── Data/
        │   ├── PanelType.cs
        │   └── PanelStackItem.cs
        ├── Animation/
        │   ├── PanelAnimation.cs
        │   ├── FadeAnimation.cs
        │   └── ScaleAnimation.cs
        ├── Panels/
        │   ├── Shop/
        │   │   ├── ShopPanel.cs
        │   │   ├── ShopController.cs
        │   │   └── ShopPanel.prefab
        │   ├── Bag/
        │   │   ├── BagPanel.cs
        │   │   ├── BagController.cs
        │   │   └── BagPanel.prefab
        │   └── Menu/
        │       ├── MenuPanel.cs
        │       ├── MenuController.cs
        │       └── MenuPanel.prefab
        └── Prefabs/
            ├── UIRoot.prefab
            └── LoadingMask.prefab
```

---

## 配置文件示例

```json
{
  "panels": [
    {
      "panelName": "MainMenu",
      "prefabPath": "UI/Panels/MainMenu",
      "panelType": "Base",
      "controllerType": "MenuController",
      "cacheOnClose": false
    },
    {
      "panelName": "Shop",
      "prefabPath": "UI/Panels/Shop",
      "panelType": "Normal",
      "controllerType": "ShopController",
      "cacheOnClose": true
    },
    {
      "panelName": "ConfirmDialog",
      "prefabPath": "UI/Panels/ConfirmDialog",
      "panelType": "Dialog",
      "controllerType": "ConfirmDialogController",
      "cacheOnClose": true
    }
  ],
  "maxCacheCount": 10,
  "cacheExpireTime": 300
}
```

---

## 架构优势

```
┌─────────────────────────────────────────────────────────────────┐
│                  Panel + Controller 优势                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  1. 简洁                                                        │
│     ──────                                                      │
│     只有两个类，比 MVC 少一个 Model 层                           │
│     适合大多数 Unity 项目                                        │
│                                                                 │
│  2. 职责清晰                                                    │
│     ──────────                                                  │
│     Panel: 只管 UI 组件引用                                      │
│     Controller: 管数据和逻辑                                     │
│                                                                 │
│  3. 易于维护                                                    │
│     ──────────                                                  │
│     修改 UI 布局只改 Panel                                       │
│     修改逻辑只改 Controller                                      │
│                                                                 │
│  4. 可测试                                                      │
│     ────────                                                    │
│     Controller 可独立测试                                        │
│     Panel 可用 Mock 替换                                         │
│                                                                 │
│  5. 美术友好                                                    │
│     ──────────                                                  │
│     Panel 只有组件引用，美术可以直接修改预制体                    │
│     不会误改逻辑代码                                             │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```
