# 可插拔加载模式 — 规格

## ADDED Requirements

### Requirement: IResourceProvider 接口定义
系统 SHALL 定义 `IResourceProvider` 接口，包含所有加载模式必须实现的方法：Initialize、Cleanup、LoadAsset、LoadAssetAsync、ReleaseAsset、IsLoaded、CleanupForSceneChange。

#### Scenario: 所有 Provider 实现同一接口
- **WHEN** 创建任意 Provider（Editor / AssetBundle / Resources 等）
- **THEN** 该 Provider 必须实现 `IResourceProvider` 接口的所有方法

### Requirement: ResourceManager 作为 Facade 层
ResourceManager SHALL 不直接实现加载逻辑，而是将加载请求委托给当前激活的 `IResourceProvider` 实例。

#### Scenario: 委托加载请求
- **WHEN** 调用 `ResourceManager.Instance.LoadAsset<GameObject>("Hero")`
- **THEN** 系统将请求委托给 `_provider.LoadAsset<GameObject>("Hero")`

### Requirement: 加载模式枚举
系统 SHALL 提供 `LoadMode` 枚举，包含至少以下值：Editor、AssetBundleLocal、AssetBundleRemote、Resources。

#### Scenario: 枚举覆盖所有模式
- **WHEN** 代码中引用 `LoadMode` 枚举
- **THEN** 可选择的值为 Editor、AssetBundleLocal、AssetBundleRemote、Resources

### Requirement: 编辑器模式支持 Inspector 选择
在 Unity Editor 中，ResourceManager 组件 SHALL 在 Inspector 中暴露 LoadMode 下拉菜单，开发人员可手动切换模式。

#### Scenario: Inspector 中选择加载模式
- **WHEN** 选中 ResourceManager GameObject 查看 Inspector
- **THEN** 系统显示 LoadMode 下拉菜单，可选 Editor / AssetBundleLocal / AssetBundleRemote / Resources

### Requirement: 真机模式自动判断
在非 Editor 环境下，系统 SHALL 自动判断加载模式：若启用了热更新则使用 AssetBundleRemote，否则使用 AssetBundleLocal。

#### Scenario: 真机热更新模式
- **WHEN** 真机运行且配置中热更新开关为 true
- **THEN** 系统自动使用 AssetBundleRemoteProvider

#### Scenario: 真机本地 AB 模式
- **WHEN** 真机运行且配置中热更新开关为 false
- **THEN** 系统自动使用 AssetBundleLocalProvider

### Requirement: 模式切换时清理旧 Provider
切换加载模式时，系统 SHALL 先调用当前 Provider 的 `Cleanup()` 方法（释放已加载的 AB、清空缓存），再创建新的 Provider 并调用 `Initialize()`。

#### Scenario: 从 AB Local 切换到 AB Remote
- **WHEN** 调用 `SetLoadMode(LoadMode.AssetBundleRemote)` 且当前模式为 AssetBundleLocal
- **THEN** 系统先调用 LocalProvider.Cleanup()，再创建 RemoteProvider 并调用 Initialize()

### Requirement: EditorAssetProvider 使用 AssetDatabase
在 Unity Editor 中启用 Editor 模式时，系统 SHALL 使用 `AssetDatabase.LoadAssetAtPath` 加载资源，使用 `AssetInfo.assetPath` 字段定位资源。

#### Scenario: Editor 模式加载资源
- **WHEN** 加载模式为 Editor，调用 LoadAsset<GameObject>("Hero")
- **THEN** 系统从 AssetInfo 中读取 assetPath = "Assets/Prefabs/Hero.prefab"，调用 AssetDatabase.LoadAssetAtPath<T>(assetPath)

### Requirement: AssetBundleProviderBase 抽象基类
AB Local 和 AB Remote Provider SHALL 继承同一个抽象基类 `AssetBundleProviderBase`，复用双层引用计数、Bundle 缓存、依赖自动加载逻辑。

#### Scenario: 子类仅覆写差异方法
- **WHEN** 查看 AssetBundleLocalProvider 和 AssetBundleRemoteProvider 的代码
- **THEN** 两个子类仅覆写 `GetBundlePath()` 和 `Initialize()` 方法，LoadAsset / ReleaseAsset 等由基类实现

### Requirement: AssetInfo 配置表适配多模式
AssetInfo 配置表 SHALL 包含所有加载模式所需的路径/名称字段：assetPath（Editor 模式）、bundleName + dependencies（AB 模式）、resourcesPath（Resources 模式）。

#### Scenario: 不同 Provider 读取不同字段
- **WHEN** EditorAssetProvider 查询 AssetInfo
- **THEN** Provider 读取 assetPath 字段
- **WHEN** AssetBundleLocalProvider 查询 AssetInfo
- **THEN** Provider 读取 bundleName 和 dependencies 字段

### Requirement: 外部通过配置选择加载模式
系统 SHALL 支持通过 ScriptableObject 配置文件指定加载模式，允许同一包体在不同环境（开发/测试/生产）下使用不同加载策略。

#### Scenario: 配置文件控制加载模式
- **WHEN** 配置文件 ResourceConfig.asset 中 loadMode = AssetBundleRemote
- **THEN** ResourceManager.Awake 时读取配置，自动创建对应的 Provider
