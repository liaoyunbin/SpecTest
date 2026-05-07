# AB 包资源管理器

## Why

当前项目缺乏统一的 AssetBundle 资源管理系统，资源加载分散在各模块中，依赖管理混乱，卸载时机不明确，容易导致资源丢失、内存泄漏和重复加载问题。需要一个统一的 ResourceManager 来接管所有 AB 包的加载、缓存、引用计数和卸载。

## What Changes

- 新增 **AssetBundle 打包策略**：按场景/功能划分 AB 粒度，共享资源独立打包
- 新增 **BundleManager**：负责 AB 文件的加载与缓存，自动处理依赖加载顺序
- 新增 **资源引用计数系统**：跟踪每个资源的引用次数，精确控制卸载时机
- 新增 **ResourceManager 统一入口**：提供同步和异步两种加载 API
- 新增 **AssetInfo 配置表**：记录每个资源的所属 AB、依赖关系和资源类型
- 新增 **热更新支持**：版本号比对、MD5 校验、差异下载与本地缓存

## Capabilities

### New Capabilities
- `ab-package-strategy`：AB 包打包粒度策略与依赖管理规则
- `bundle-loading`：AB 文件的同步/异步加载、缓存、依赖自动加载
- `ref-counting`：资源与 AB 的双层引用计数，精确控制卸载时机
- `resource-manager-api`：统一的资源加载/释放 API，支持同步和异步
- `hot-update`：基于版本号和 MD5 的热更新流程

### Modified Capabilities
<!-- 本次不修改现有规格 -->

## Impact

- 新增代码目录：`Assets/Scripts/ResourceManager/`
- 新增配置表：`AssetInfoConfig`（记录资源与 AB 的映射关系）
- 影响所有资源加载入口：现有 `Resources.Load` / 直接 `AssetDatabase` 加载需要迁移
- 打包流程变更：需要集成 `BuildPipeline.BuildAssetBundles`
- 依赖项：Unity `AssetBundle` API、`AssetBundleManifest`
