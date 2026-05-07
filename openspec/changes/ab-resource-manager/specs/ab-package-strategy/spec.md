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

### Requirement: Shader 变体预热
打包流程 SHALL 维护一个 `ShaderVariantCollection`（SVC），收录所有游戏用到的 Shader 变体。SVC SHALL 在 `GraphicsSettings` 中注册，打包时 Unity 自动将 SVC 中的变体打入各 AB。

#### Scenario: SVC 确保变体可用
- **WHEN** 不同场景的 AB 使用同一 Shader 的不同 keyword 组合
- **THEN** 所有变体均包含在 AB 中，不会出现因变体缺失导致的粉色材质

#### Scenario: 缺少 SVC 时打包报警
- **WHEN** 打包时 GraphicsSettings 中未注册 SVC 或 SVC 为空
- **THEN** 打包脚本发出编译警告，提示可能导致 Shader 变体缺失

### Requirement: 全局资源 Permanent 标记
打包配置 SHALL 支持将指定 AB 标记为 Permanent（全局常驻）。Permanent AB 的 BundleInfo.isPermanent = true，场景切换时不会被 CleanupForSceneChange 卸载。

#### Scenario: 全局 UI 图集标记为 Permanent
- **WHEN** 打包配置中 common_ui.ab 被标记为 Permanent
- **THEN** 该 AB 在场景切换时保留在内存中，不被卸载

### Requirement: Android APK 不压缩 .ab 文件
打包脚本 SHALL 在打包完成后输出配置提醒，告知开发者需在 Android 构建配置中设置 `aaptOptions { noCompress '.ab', '.json' }`，确保 StreamingAssets 中的 .ab 文件不被 APK 二次压缩。

#### Scenario: 打包后输出 Android 配置提醒
- **WHEN** AssetBundleBuilder 打包完成且目标平台包含 Android
- **THEN** 系统在 Console 输出警告信息，提示配置 mainTemplate.gradle 的 noCompress 选项

#### Scenario: 未配置 noCompress 时 Editor 内模拟警告
- **WHEN** 在 Editor 中以 Android 平台运行且 RESOURCE_DEBUG 开启
- **THEN** 系统检测到 .ab 文件从 APK 路径加载时输出性能警告

### Requirement: Resources 双重打包检测
打包脚本 SHALL 在分配资源到 AB 之前，扫描所有 `Resources/` 目录下的文件，与 AB 打包配置中的资源列表做交集比对。若同一资源同时存在于 Resources 和 AB 中，SHALL 输出编译错误并阻止打包。

#### Scenario: 检测到双重打包
- **WHEN** hero.prefab 同时存在于 `Assets/Resources/Hero.prefab` 和 AB 打包配置中
- **THEN** 打包脚本输出错误日志并调用 `EditorApplication.Exit(1)` 阻止打包

#### Scenario: 无双重打包正常通过
- **WHEN** Resources 目录与 AB 配置中的资源路径无交集
- **THEN** 打包脚本正常继续执行

### Requirement: referencedBundles 自动计算
打包脚本 SHALL 在生成 AssetInfoConfig 时，自动计算每个 Prefab 资源的 `referencedBundles` 字段（该 Prefab 及其依赖链引用的所有 AB 名称列表），无需手动配置。

#### Scenario: Prefab 的 referencedBundles 自动填充
- **WHEN** Hero.prefab 使用了 hero.ab 中的网格、common_mat.ab 中的材质、common_tex.ab 中的贴图
- **THEN** AssetInfo 中 Hero 的 referencedBundles = ["hero", "common_mat", "common_tex"]

#### Scenario: 嵌套 Prefab 的依赖链合并
- **WHEN** Hero.prefab 内部嵌套了 Weapon.prefab（属于 weapon.ab）
- **THEN** Hero 的 referencedBundles 包含 hero.ab 的依赖 + weapon.ab 及其依赖链
