# AB 包打包策略规格

## ADDED Requirements

### Requirement: 按场景/功能划分打包粒度
系统 SHALL 支持按场景或功能模块划分 AssetBundle 打包粒度。大资源（文件 > 1MB）SHALL 独立打包为一个 AB，同类小资源（文件 < 100KB）SHALL 合并打包为一个 AB。

#### Scenario: 大资源独立打包
- **WHEN** 一个模型文件 hero.fbx 大小超过 1MB
- **THEN** 系统将其打包为独立的 hero.ab

#### Scenario: 小资源合并打包
- **WHEN** 一个场景下有 20 个音效文件，每个小于 100KB
- **THEN** 系统将其合并打包为一个 sfx.ab

### Requirement: 共享资源独立打包
被多个 AB 引用的共享资源 SHALL 独立打包为一个单独的 AB，其他 AB 以依赖方式引用。图集资源 SHALL 单独打包，避免被打散到多个 AB 导致重复加载。

#### Scenario: 图集独立打包
- **WHEN** UI 图集 `common_atlas` 被登录界面、商城界面、背包界面同时使用
- **THEN** 系统将图集打包为 `common_atlas.ab`，三个界面 AB 仅记录对图集 AB 的依赖

### Requirement: 打包时生成依赖清单
每次执行 BuildPipeline.BuildAssetBundles 时 SHALL 自动生成 AssetBundleManifest，记录所有 AB 之间的依赖关系。

#### Scenario: 依赖清单生成
- **WHEN** 打包完成
- **THEN** 系统在输出目录生成包含所有 AB 依赖映射的总 Manifest 文件

### Requirement: 压缩格式选择
打包时 SHALL 支持 LZMA 和 LZ4 两种压缩格式。下载分发的 AB 包 SHALL 使用 LZMA 格式以减小体积，本地加载的 AB 包 SHALL 使用 LZ4 格式以提高加载速度。

#### Scenario: LZ4 本地加载
- **WHEN** 打包目标平台为本地加载
- **THEN** 系统使用 ChunkBasedCompression (LZ4) 格式打包
