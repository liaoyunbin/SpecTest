# AB 包资源管理器 —— 实现任务

## 1. 基础数据模型

- [ ] 1.1 创建 `AssetInfo` 类：包含 assetName、bundleName、dependencies、assetPath、resourcesPath、assetType 字段
- [ ] 1.2 创建 `LoadState` 枚举：Unloaded、Loading、Loaded、Failed
- [ ] 1.3 创建 `BundleInfo` 类：包含 bundleName、bundle、refCount、state、loadedAssets 字段
- [ ] 1.4 创建 `AssetRef` 类：包含 assetName、asset、refCount、bundleName 字段
- [ ] 1.5 创建 `LoadMode` 枚举：Editor、AssetBundleLocal、AssetBundleRemote、Resources
- [ ] 1.6 创建 `AssetInfoConfig` ScriptableObject：包含 List\<AssetInfo\> 和 GetByName 查询方法

## 2. 打包策略实现

- [ ] 2.1 编写 AB 打包配置文件（JSON），定义每个 AB 名称及包含的资源列表
- [ ] 2.2 实现打包脚本 `AssetBundleBuilder`：遍历配置，按粒度策略分配资源到对应 AB
- [ ] 2.3 设置各资源的 AssetBundleName，调用 `BuildPipeline.BuildAssetBundles` 执行打包
- [ ] 2.4 打包完成后自动生成 `AssetInfoConfig` ScriptableObject（填充所有字段，包括 assetPath 和 bundleName）

## 3. IResourceProvider 接口

- [ ] 3.1 定义 `IResourceProvider` 接口：Initialize、Cleanup、LoadAsset\<T\>、LoadAssetAsync\<T\>、ReleaseAsset、IsLoaded、CleanupForSceneChange
- [ ] 3.2 在 ResourceManager 中持有 `IResourceProvider _provider` 字段
- [ ] 3.3 实现 `SetLoadMode(LoadMode mode)` 方法：先调用旧 Provider 的 Cleanup()，再创建新 Provider 并 Initialize()

## 4. EditorAssetProvider

- [ ] 4.1 实现 `EditorAssetProvider.Initialize()`：加载 AssetInfoConfig
- [ ] 4.2 实现 `EditorAssetProvider.LoadAsset<T>(assetName)`：从 AssetInfo 读取 assetPath，调用 AssetDatabase.LoadAssetAtPath\<T\>(path)
- [ ] 4.3 实现 `EditorAssetProvider.LoadAssetAsync<T>()`：协程包装同步 Load（Editor 不支持真正异步）
- [ ] 4.4 实现 `EditorAssetProvider.ReleaseAsset()`：无操作
- [ ] 4.5 实现 `EditorAssetProvider.CleanupForSceneChange()`：无操作

## 5. AssetBundleProviderBase（抽象基类，复用引用计数/缓存/依赖逻辑）

- [ ] 5.1 创建 `AssetBundleProviderBase` 抽象类，实现 IResourceProvider
- [ ] 5.2 实现 bundleCache / assetCache 缓存字典
- [ ] 5.3 实现 `LoadAsset<T>(assetName)` 同步加载：检查缓存 → 查询依赖 → 加载 AB → 加载资源 → 缓存 + 引用计数
- [ ] 5.4 实现 `LoadAssetAsync<T>(assetName, callback, progress)` 异步加载
- [ ] 5.5 实现依赖自动加载：`LoadDependencies(bundleName)` → 从 Manifest 查询并递归加载
- [ ] 5.6 实现双层引用计数：LoadAsset 时 refCount++；ReleaseAsset 时 refCount--；归零时卸载
- [ ] 5.7 实现 `UnloadBundle(bundleName)`：BundleInfo.refCount 归零时调用 bundle.Unload(true)
- [ ] 5.8 实现 `CleanupForSceneChange()`：卸载所有 refCount=0 的 AB
- [ ] 5.9 定义抽象方法供子类覆写：`abstract AssetBundle LoadSingleBundle(string bundleName)` / `abstract string GetBundlePath(string bundleName)`

## 6. AssetBundleLocalProvider（本地 AB）

- [ ] 6.1 实现 `Initialize()`：从 StreamingAssets 加载 Manifest，加载 AssetInfoConfig，初始化缓存
- [ ] 6.2 实现 `LoadSingleBundle(bundleName)`：调用 AssetBundle.LoadFromFile(GetBundlePath(bundleName))
- [ ] 6.3 实现 `GetBundlePath(bundleName)`：优先 persistentDataPath，其次 streamingAssetsPath
- [ ] 6.4 实现 `Cleanup()`：遍历卸载所有 Bundle，清空缓存

## 7. AssetBundleRemoteProvider（远程 AB + 热更新）

- [ ] 7.1 实现 `VersionManager` 类：启动时请求服务器 version.json，与本地版本号比对
- [ ] 7.2 实现 `FileListLoader`：请求 files.json，解析 AB 名称、MD5、文件大小
- [ ] 7.3 实现 MD5 差异比对：逐项比对本地与服务器 AB 的 MD5，生成下载列表
- [ ] 7.4 实现 `ABDownloader`：通过 UnityWebRequest 逐个下载 AB 文件，报告进度
- [ ] 7.5 实现下载后 MD5 校验：不匹配时重试（最多 3 次）
- [ ] 7.6 实现更新完成后持久化新版本号到 `version.json`
- [ ] 7.7 实现更新失败回退：网络异常或下载失败时使用 StreamingAssets 资源继续运行
- [ ] 7.8 实现 `Initialize()`：版本比对 → 差异下载 → 加载 Manifest（从 persistentDataPath）→ 加载 AssetInfoConfig
- [ ] 7.9 覆写 `LoadSingleBundle(bundleName)`：本地已有 → LoadFromFile；本地无 → 下载 → LoadFromFile
- [ ] 7.10 覆写 `GetBundlePath(bundleName)`：统一返回 persistentDataPath 路径

## 8. ResourcesProvider

- [ ] 8.1 实现 `Initialize()`：加载 AssetInfoConfig（可选）
- [ ] 8.2 实现 `LoadAsset<T>(assetName)`：从 AssetInfo 读取 resourcesPath，调用 Resources.Load\<T\>(path)
- [ ] 8.3 实现 `ReleaseAsset(assetName)`：调用 Resources.UnloadAsset
- [ ] 8.4 实现 `Cleanup()`：无操作

## 9. ResourceManager 主类

- [ ] 9.1 创建 `ResourceManager` MonoBehaviour 单例
- [ ] 9.2 在 Inspector 中暴露 LoadMode 下拉菜单（Editor 模式）
- [ ] 9.3 Awake 时根据 LoadMode 创建对应 Provider 并调用 Initialize()
- [ ] 9.4 真机环境自动判断：热更新开启 → RemoteProvider；否则 → LocalProvider
- [ ] 9.5 所有公开 API（LoadAsset / LoadAssetAsync / ReleaseAsset / CleanupForSceneChange）委托给 _provider

## 10. 配置系统

- [ ] 10.1 创建 `ResourceConfig` ScriptableObject：包含 loadMode、bundleRootPath、baseUrl、maxConcurrentLoads 字段
- [ ] 10.2 ResourceManager.Awake 时从 Resources 加载 ResourceConfig
- [ ] 10.3 实现并发加载限制：维护加载队列，最大并发数从配置读取（默认 5）

## 11. 集成与测试

- [ ] 11.1 编写示例：Editor 模式下同步加载 Hero 预制体并实例化
- [ ] 11.2 编写示例：AB Local 模式下异步加载战斗场景并显示进度条
- [ ] 11.3 编写示例：AB Remote 模式下启动热更新流程并下载资源
- [ ] 11.4 编写示例：加载道具列表，关闭界面时正确释放所有资源
- [ ] 11.5 编写示例：场景切换时调用 CleanupForSceneChange
- [ ] 11.6 功能测试：Inspector 中切换模式，加载/释放正确
- [ ] 11.7 功能测试：Editor → AB Local 切换，资源加载正常
- [ ] 11.8 功能测试：热更新版本比对和下载正常
- [ ] 11.9 功能测试：同时加载同一资源，引用计数正确累加
- [ ] 11.10 功能测试：模式切换时旧 Provider 资源已清理，新 Provider 可正常加载
