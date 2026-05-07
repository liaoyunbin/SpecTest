# AB 包资源管理器 —— 实现任务

## 1. 基础数据模型

- [ ] 1.1 创建 `AssetInfo` 类：包含 assetName、bundleName、dependencies、assetType 字段
- [ ] 1.2 创建 `LoadState` 枚举：Unloaded、Loading、Loaded、Failed
- [ ] 1.3 创建 `BundleInfo` 类：包含 bundleName、bundle、refCount、state、loadedAssets 字段
- [ ] 1.4 创建 `AssetRef` 类：包含 assetName、asset、refCount、bundleName 字段

## 2. 打包策略实现

- [ ] 2.1 编写 AB 打包配置文件（JSON），定义每个 AB 名称及包含的资源列表
- [ ] 2.2 实现打包脚本 `AssetBundleBuilder`：遍历配置，按粒度策略分配资源到对应 AB
- [ ] 2.3 设置各资源的 AssetBundleName，调用 `BuildPipeline.BuildAssetBundles` 执行打包
- [ ] 2.4 打包完成后自动生成 `AssetInfo` 配置表（ScriptableObject），记录每个资源的 AssetInfo

## 3. AB 加载模块（BundleManager）

- [ ] 3.1 实现 `LoadBundle(string bundleName)` 同步加载 AB 文件
- [ ] 3.2 实现 `LoadBundleAsync(string bundleName)` 异步加载 AB 文件（协程）
- [ ] 3.3 实现 bundleCache 缓存机制：已加载的 AB 不重复加载
- [ ] 3.4 实现 `LoadState` 状态管理：防止并发加载同一 AB
- [ ] 3.5 实现加载失败处理：标记 Failed、记录日志、支持重试

## 4. 依赖加载模块

- [ ] 4.1 实现 Manifest 加载：启动时加载总 Manifest，缓存到 dependencyManifest
- [ ] 4.2 实现 `LoadDependencies(string bundleName)`：查询 Manifest 获取所有依赖 AB，按序加载
- [ ] 4.3 实现依赖循环检测：已加载的 AB 跳过，防止死循环

## 5. 资源管理 API（ResourceManager 主类）

- [ ] 5.1 创建 `ResourceManager` MonoBehaviour 单例类，初始化缓存字典和加载队列
- [ ] 5.2 实现 `LoadAsset<T>(string assetName)` 同步加载方法
- [ ] 5.3 实现 `LoadAssetAsync<T>(string assetName, callback, progressCallback)` 异步加载方法
- [ ] 5.4 实现 `GetBundlePath(string bundleName)`：优先 persistentDataPath，其次 streamingAssetsPath
- [ ] 5.5 实现并发加载限制：维护加载队列，最大并发数可配置（默认 5）

## 6. 引用计数系统

- [ ] 6.1 实现 AssetRef 级引用计数：LoadAsset 时 refCount++，已缓存直接返回并 refCount++
- [ ] 6.2 实现 BundleInfo 级引用计数：为该 AB 下所有 AssetRef.refCount 之和
- [ ] 6.3 实现 `ReleaseAsset(string assetName)`：refCount--，归零时从 assetCache 移除，更新 BundleInfo
- [ ] 6.4 实现 `UnloadBundle(string bundleName)`：BundleInfo.refCount 归零时调用 `bundle.Unload(true)` 卸载
- [ ] 6.5 实现 `CleanupForSceneChange()`：遍历所有 AB，卸载 refCount=0 的 AB
- [ ] 6.6 实现释放不存在资源的防护：记录警告日志，不崩溃

## 7. 热更新模块

- [ ] 7.1 实现 `VersionManager` 类：启动时请求服务器 version.json，与本地版本号比对
- [ ] 7.2 实现 `FileListLoader`：请求 files.json，解析 AB 名称、MD5、文件大小
- [ ] 7.3 实现 MD5 差异比对：逐项比对本地与服务器 AB 的 MD5，生成下载列表
- [ ] 7.4 实现 `ABDownloader`：通过 UnityWebRequest 逐个下载 AB 文件，报告进度
- [ ] 7.5 实现下载后 MD5 校验：不匹配时重试（最多 3 次）
- [ ] 7.6 实现更新完成后持久化新版本号到 `version.json`
- [ ] 7.7 实现更新失败回退：网络异常或下载失败时使用 StreamingAssets 资源继续运行

## 8. 编辑器模式兼容

- [ ] 8.1 实现编辑器模式检测：UNITY_EDITOR 宏判断是否使用 AssetDatabase
- [ ] 8.2 实现编辑器加载路径：使用 `AssetDatabase.LoadAssetAtPath` 替代 AB 加载
- [ ] 8.3 实现编辑器模式下资源释放为无操作（AssetDatabase 自动管理生命周期）

## 9. 集成与测试

- [ ] 9.1 编写示例脚本：同步加载 Hero 预制体并实例化
- [ ] 9.2 编写示例脚本：异步加载战斗场景并显示进度条
- [ ] 9.3 编写示例脚本：加载道具列表，关闭界面时正确释放所有资源
- [ ] 9.4 编写示例脚本：场景切换时调用 CleanupForSceneChange
- [ ] 9.5 功能测试：编辑器模式下加载/释放正常
- [ ] 9.6 功能测试：真机 AB 模式加载/释放正常
- [ ] 9.7 功能测试：热更新版本比对和下载正常
- [ ] 9.8 功能测试：同时加载同一资源，引用计数正确累加
