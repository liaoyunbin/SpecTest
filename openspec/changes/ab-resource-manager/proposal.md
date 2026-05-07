# AB 包资源管理器

## Why

当前项目缺乏统一的 AssetBundle 资源管理系统，资源加载分散在各模块中，依赖管理混乱，卸载时机不明确，容易导致资源丢失、内存泄漏和重复加载问题。需要一个统一的 ResourceManager 来接管所有 AB 包的加载、缓存、引用计数和卸载。

## What Changes

- 新增 **AssetBundle 打包策略**：按场景/功能划分 AB 粒度，共享资源独立打包；ShaderVariantCollection 预热变体
- 新增 **BundleManager**：负责 AB 文件的加载与缓存，自动处理依赖加载顺序；Android 平台兼容适配
- 新增 **资源引用计数系统**：跟踪每个资源的引用次数 + Instantiate 实例追踪，精确控制卸载时机
- 新增 **ResourceManager 统一入口**：提供 InstantiateAsset / DestroyAsset / LoadAsset / LoadAssetAsync / ReleaseAsset API
- 新增 **AssetInfo 配置表**：记录每个资源的所属 AB、依赖关系、降级资源名、引用链；通过 Resources 自举加载
- 新增 **热更新支持**：版本号比对、MD5 校验、差异下载与本地缓存；Manifest 同步更新
- 新增 **可插拔加载模式**：基于策略模式支持 Editor / AssetBundle Local / AssetBundle Remote / Resources 四种加载模式，通过枚举或配置一键切换
- 新增 **调试诊断系统**：GetLoadedBundles / GetMemorySummary API + RESOURCE_DEBUG 宏控制的 IMGUI Debug 面板

## Capabilities

### New Capabilities
- `ab-package-strategy`：AB 包打包粒度策略、Shader 变体预热、永久资源标记
- `bundle-loading`：AB 文件的同步/异步加载、缓存、依赖自动加载、Android 平台兼容、并发控制
- `ref-counting`：资源与 AB 的双层引用计数 + Instance 追踪 + Permanent 保护
- `resource-manager-api`：统一的 InstantiateAsset/DestroyAsset/LoadAsset/ReleaseAsset API + 降级 + Debug
- `hot-update`：基于版本号和 MD5 的热更新流程 + Manifest 同步
- `load-mode-switching`：可插拔加载模式，支持 Editor / AB Local / AB Remote / Resources 四种 Provider + Editor 模拟引用计数检测

### Modified Capabilities
<!-- 本次不修改现有规格 -->

## Impact

- 新增代码目录：`Assets/Scripts/ResourceManager/`
  - `Core/`：IResourceProvider 接口、ResourceManager（Facade）、AssetInfoConfig、ResourceDebugger
  - `Providers/`：EditorAssetProvider、AssetBundleProviderBase、AssetBundleLocalProvider、AssetBundleRemoteProvider、ResourcesProvider
  - `Data/`：AssetInfo、BundleInfo、AssetRef、LoadState
  - `Services/`：ManifestLoader、VersionManager、ABDownloader、RefCounter、BundlePathResolver
- 新增 Resources 配置：`Resources/AssetInfoConfig.asset`（自举加载，不放入 AB）
- 新增打包脚本：`AssetBundleBuilder`（含 SVC 校验 + Permanent 标记）
- 新增 ShaderVariantCollection：需手动维护所有 Shader 变体
- 影响所有资源加载入口：现有 `Instantiate`/`Destroy`/`Resources.Load` 需要迁移到 ResourceManager
- 依赖项：Unity `AssetBundle` API、`AssetBundleManifest`、`AssetDatabase`（Editor）、`UnityWebRequestAssetBundle`（Android）
