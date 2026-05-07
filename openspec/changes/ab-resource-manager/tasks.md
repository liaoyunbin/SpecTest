# AB 包资源管理器 —— 实现任务

## 1. 基础数据模型

- [ ] 1.1 创建 `AssetInfo` 类：包含 assetName、bundleName、dependencies、assetPath、resourcesPath、assetType、fallbackAssetName、referencedBundles 字段
- [ ] 1.2 创建 `LoadState` 枚举：Unloaded、Loading、Loaded、Failed
- [ ] 1.3 创建 `BundleInfo` 类：包含 bundleName、bundle、refCount、instanceCount、state、isPermanent、loadedAssets 字段
- [ ] 1.4 创建 `AssetRef` 类：包含 assetName、asset、refCount、bundleName、state、pendingCallbacks 字段
- [ ] 1.5 创建 `LoadMode` 枚举：Editor、AssetBundleLocal、AssetBundleRemote、Resources
- [ ] 1.6 创建 `AssetInfoConfig` ScriptableObject：包含 List\<AssetInfo\> 和 GetByName 查询方法

## 2. 打包策略实现

- [ ] 2.1 编写 AB 打包配置文件（JSON）：定义每个 AB 的名称、资源列表、isPermanent 标记
- [ ] 2.2 实现打包脚本 `AssetBundleBuilder`：遍历配置，按粒度策略分配资源到对应 AB
- [ ] 2.3 设置各资源的 AssetBundleName，调用 `BuildPipeline.BuildAssetBundles` 执行打包
- [ ] 2.4 打包完成后自动生成 `AssetInfoConfig` ScriptableObject（填充所有字段）
- [ ] 2.5 创建 `ShaderVariantCollection` 并收录所有用到的 Shader 变体
- [ ] 2.6 打包脚本增加 SVC 校验：GraphicsSettings 中未注册 SVC 时输出编译警告
- [ ] 2.7 将生成的 AssetInfoConfig 放入 `Resources/` 目录（自举加载用）

## 3. IResourceProvider 接口

- [ ] 3.1 定义 `IResourceProvider` 接口：Initialize、Cleanup、LoadAsset\<T\>、LoadAssetAsync\<T\>、ReleaseAsset、InstantiateAsset、DestroyAsset、IsLoaded、CleanupForSceneChange、GetDebugInfo
- [ ] 3.2 在 ResourceManager 中持有 `IResourceProvider _provider` 字段
- [ ] 3.3 实现 `SetLoadMode(LoadMode mode)` 方法：先调用旧 Provider 的 Cleanup()，再创建新 Provider 并 Initialize()

## 4. EditorAssetProvider

- [ ] 4.1 实现 `Initialize()`：通过 Resources.Load 加载 AssetInfoConfig
- [ ] 4.2 实现 `LoadAsset<T>(assetName)`：从 AssetInfo 读取 assetPath，调用 AssetDatabase.LoadAssetAtPath\<T\>(path)
- [ ] 4.3 实现 `LoadAssetAsync<T>()`：协程包装同步 Load（Editor 不支持真正异步）
- [ ] 4.4 实现 `ReleaseAsset()`：模拟引用计数 -1，不做实际卸载
- [ ] 4.5 实现模拟引用计数检测：OnDestroy / 场景切换时检查未配对的 Load/Release 并输出警告
- [ ] 4.6 实现 `InstantiateAsset(assetName, parent)`：LoadAsset + Instantiate（Editor 下 instanceCount 也做模拟追踪）
- [ ] 4.7 实现 `DestroyAsset(instance)`：Update instanceCount + Destroy
- [ ] 4.8 实现 `CleanupForSceneChange()`：无操作 + 输出引用计数配对检查结果

## 5. AssetBundleProviderBase（抽象基类）

- [ ] 5.1 创建 `AssetBundleProviderBase` 抽象类，实现 IResourceProvider
- [ ] 5.2 实现 bundleCache / assetCache / instanceBundleMap 字典
- [ ] 5.3 实现 `LoadAsset<T>(assetName)` 同步加载：检查缓存 → 查询依赖 → 加载 AB → 加载资源 → 失败时尝试降级 → 缓存 + 引用计数
- [ ] 5.4 实现 `LoadAssetAsync<T>(assetName, callback, progress)` 异步加载：AssetRef 状态机 + pendingCallbacks 防并发
- [ ] 5.5 实现依赖自动加载：`LoadDependencies(bundleName)` → 从 Manifest 查询并递归加载
- [ ] 5.6 实现双层引用计数（refCount + instanceCount）：LoadAsset 时 refCount++；InstantiateAsset 时 instanceCount++
- [ ] 5.7 实现 `InstantiateAsset(assetName, parent)`：LoadAsset → Instantiate → 遍历 referencedBundles 按 AB instanceCount++
- [ ] 5.8 实现 `DestroyAsset(instance)`：查询 instanceBundleMap → 各 AB instanceCount-- → GameObject.Destroy
- [ ] 5.9 实现 `ReleaseAsset(assetName)`：refCount-- → 归零时从 assetCache 移除 → 检查 refCount==0 && instanceCount==0 → UnloadBundle
- [ ] 5.10 实现 `UnloadBundle(bundleName)`：refCount+instanceCount 归零时调用 bundle.Unload(true)
- [ ] 5.11 实现 `CleanupForSceneChange()`：卸载所有 isPermanent=false 且 引用计数归零的 AB
- [ ] 5.12 定义抽象方法：`abstract AssetBundle LoadSingleBundle(string bundleName)` / `abstract string GetBundlePath(string bundleName)`

## 6. AssetBundleLocalProvider（本地 AB）

- [ ] 6.1 实现 `Initialize()`：Resources.Load AssetInfoConfig → 加载 Manifest → 初始化缓存
- [ ] 6.2 实现 `LoadSingleBundle(bundleName)`：调用 AssetBundle.LoadFromFile(GetBundlePath(bundleName))
- [ ] 6.3 实现 `GetBundlePath(bundleName)`：优先 persistentDataPath，其次 streamingAssetsPath
- [ ] 6.4 实现 Android 平台适配：`#if UNITY_ANDROID && !UNITY_EDITOR` → UnityWebRequestAssetBundle 加载包内 AB
- [ ] 6.5 实现 `Cleanup()`：遍历卸载所有 Bundle，清空缓存

## 7. AssetBundleRemoteProvider（远程 AB + 热更新）

- [ ] 7.1 实现 `VersionManager`：启动时请求服务器 version.json，与本地版本号比对
- [ ] 7.2 实现 `FileListLoader`：请求 files.json，解析 AB 名称、MD5、文件大小
- [ ] 7.3 实现 MD5 差异比对：逐项比对本地与服务器 AB 的 MD5，生成下载列表
- [ ] 7.4 Manifest 优先下载：差异列表中 Manifest 排在首位
- [ ] 7.5 实现 `ABDownloader`：通过 UnityWebRequest 逐个下载 AB 文件，报告进度
- [ ] 7.6 实现下载后 MD5 校验：不匹配时重试（最多 3 次）
- [ ] 7.7 实现更新完成后持久化新版本号到 `version.json`
- [ ] 7.8 实现更新失败回退：网络异常或下载失败时使用 StreamingAssets 资源继续运行
- [ ] 7.9 实现 `Initialize()`：版本比对 → 差异下载 → 加载 Manifest（从 persistentDataPath）→ Resources.Load AssetInfoConfig
- [ ] 7.10 覆写 `LoadSingleBundle(bundleName)`：本地已有 → LoadFromFile；本地无 → 下载 → LoadFromFile
- [ ] 7.11 覆写 `GetBundlePath(bundleName)`：统一返回 persistentDataPath 路径

## 8. ResourcesProvider

- [ ] 8.1 实现 `Initialize()`：加载 AssetInfoConfig（可选）
- [ ] 8.2 实现 `LoadAsset<T>(assetName)`：从 AssetInfo 读取 resourcesPath，调用 Resources.Load\<T\>(path)
- [ ] 8.3 实现 `ReleaseAsset(assetName)`：调用 Resources.UnloadAsset
- [ ] 8.4 实现 `InstantiateAsset` / `DestroyAsset`：直接委托给 Instantiate / Destroy
- [ ] 8.5 实现 `Cleanup()`：无操作

## 9. ResourceManager 主类

- [ ] 9.1 创建 `ResourceManager` MonoBehaviour 单例
- [ ] 9.2 Inspector 中暴露 LoadMode 下拉菜单（Editor 模式）
- [ ] 9.3 Awake 时根据 LoadMode 创建对应 Provider 并调用 Initialize()
- [ ] 9.4 真机环境自动判断：热更新开启 → RemoteProvider；否则 → LocalProvider
- [ ] 9.5 所有公开 API 委托给 _provider：LoadAsset / LoadAssetAsync / InstantiateAsset / DestroyAsset / ReleaseAsset / CleanupForSceneChange

## 10. 配置系统

- [ ] 10.1 创建 `ResourceConfig` ScriptableObject：包含 loadMode、bundleRootPath、baseUrl、maxConcurrentLoads 字段
- [ ] 10.2 ResourceManager.Awake 时从 Resources 加载 ResourceConfig
- [ ] 10.3 实现并发加载限制：维护加载队列，最大并发数从配置读取（默认 5）

## 11. 调试与诊断系统

- [ ] 11.1 BundleInfo 增加统计字段：loadTimestamp、loadDuration、estimatedSize
- [ ] 11.2 实现 `GetLoadedBundles()`：返回所有已加载 AB 的名称、大小、refCount、instanceCount
- [ ] 11.3 实现 `GetLoadedAssets()`：返回所有已缓存资源的名称、类型、refCount
- [ ] 11.4 实现 `GetMemorySummary()`：返回 AB 总数、总内存占用、各 AB 内存分布
- [ ] 11.5 实现 IMGUI Debug UI 面板：`#if RESOURCE_DEBUG` 宏控制编译
- [ ] 11.6 Debug 面板树形展示 AB → Asset 层级，实时更新引用计数

## 12. 集成与测试

- [ ] 12.1 编写示例：Editor 模式下 InstantiateAsset Hero 并 DestroyAsset
- [ ] 12.2 编写示例：AB Local 模式下异步加载战斗场景并显示进度条
- [ ] 12.3 编写示例：AB Remote 模式下启动热更新流程并下载资源
- [ ] 12.4 编写示例：加载道具列表，关闭界面时正确释放所有资源
- [ ] 12.5 编写示例：场景切换时调用 CleanupForSceneChange，验证 Permanent AB 保留
- [ ] 12.6 功能测试：Inspector 中切换模式，加载/释放正确
- [ ] 12.7 功能测试：Editor → AB Local 切换，资源加载正常
- [ ] 12.8 功能测试：热更新版本比对和下载正常
- [ ] 12.9 功能测试：同时加载同一资源，引用计数正确累加
- [ ] 12.10 功能测试：模式切换时旧 Provider 资源已清理
- [ ] 12.11 功能测试：Destroy 实例后 instanceCount 归零，AB 正确卸载
- [ ] 12.12 功能测试：资源加载失败时降级到 fallbackAssetName
- [ ] 12.13 功能测试：Android 平台 StreamingAssets 加载正常
- [ ] 12.14 功能测试：Editor 模式下 Load/Release 未配对时输出警告
