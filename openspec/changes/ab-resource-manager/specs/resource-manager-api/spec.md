# ResourceManager API 规格

## ADDED Requirements

### Requirement: 资源清单配置
系统 SHALL 维护一份 AssetInfo 配置表，记录每个资源的名称、所属 AB、依赖 AB 列表、资源类型、降级资源名和 prefab 引用的 AB 列表。AssetInfoConfig SHALL 通过 `Resources.Load` 加载，不放入 AB。

#### Scenario: 查询资源信息
- **WHEN** 通过 assetName 查询配置表
- **THEN** 系统返回对应的 AssetInfo（包含 bundleName、dependencies、assetType、fallbackAssetName、referencedBundles）

#### Scenario: AssetInfoConfig 自举加载
- **WHEN** AB Provider 的 Initialize 被调用
- **THEN** 系统通过 `Resources.Load<AssetInfoConfig>("AssetInfoConfig")` 加载配置，再初始化 AB 系统

#### Scenario: 查询不存在的资源
- **WHEN** 查询一个不在配置表中的资源名称
- **THEN** 系统记录错误日志并返回 null

### Requirement: 同步加载资源 API
系统 SHALL 提供 `LoadAsset<T>(string assetName)` 同步加载方法。方法内部自动处理依赖 AB 加载和资源缓存。加载失败时自动尝试降级资源。

#### Scenario: 同步加载 GameObject 资源
- **WHEN** 调用 `LoadAsset<GameObject>("Hero")`
- **THEN** 系统自动加载 hero.ab 及其依赖 AB，从 AB 中加载 Hero 预制体，缓存后返回

#### Scenario: 加载失败自动降级
- **WHEN** 加载 "Hero_Skin_02" 失败，且 AssetInfo 中 fallbackAssetName = "Hero_Default"
- **THEN** 系统自动尝试加载 "Hero_Default"，成功后返回降级资源

#### Scenario: 降级也失败
- **WHEN** 加载 "Hero_Skin_02" 失败，fallbackAssetName = "Hero_Default" 也加载失败
- **THEN** 系统返回 null 并记录错误日志

#### Scenario: 同步加载已缓存资源
- **WHEN** 调用 `LoadAsset<Sprite>("icon_gold")` 且该资源已加载
- **THEN** 系统直接返回缓存对象，refCount +1，不访问文件系统

### Requirement: 异步加载资源 API
系统 SHALL 提供 `LoadAssetAsync<T>(string assetName, Action<T> onComplete, Action<float> onProgress)` 异步加载方法，支持进度回调。多人同时异步加载同一资源时只发起一次加载，完成后统一通知。

#### Scenario: 异步加载带进度
- **WHEN** 调用 `LoadAssetAsync<GameObject>("BattleScene", callback, progressCallback)`
- **THEN** 系统异步加载依赖 AB 和主 AB，在每个阶段通过 progressCallback 报告进度（0.0 ~ 1.0），加载完成后通过 callback 返回资源

#### Scenario: 并发同一资源只加载一次
- **WHEN** 两个 UI 面板同时调用 LoadAssetAsync("icon_gold")
- **THEN** 系统只发起一次加载，加载完成后两个面板的回调均被执行

### Requirement: InstantiateAsset / DestroyAsset API
系统 SHALL 提供 `InstantiateAsset(string assetName, Transform parent)` 方法替代 Unity 的 `Instantiate`，内部自动追踪实例对 AB 的引用。同步提供 `DestroyAsset(GameObject instance)` 方法替代 Unity 的 `Destroy`，自动解除 AB 引用追踪。

#### Scenario: 通过 ResourceManager 实例化
- **WHEN** 调用 `InstantiateAsset("Hero", parent)`
- **THEN** 系统自动 LoadAsset + Instantiate + 追踪所有被引用 AB 的 instanceCount+1

#### Scenario: 通过 ResourceManager 销毁
- **WHEN** 调用 `DestroyAsset(heroInstance)`
- **THEN** 系统查找该实例关联的 AB 列表，各 AB 的 instanceCount-1，然后调用 Destroy

### Requirement: 释放资源 API
系统 SHALL 提供 `ReleaseAsset(string assetName)` 方法，将指定资源的引用计数减 1。当 refCount 和 instanceCount 均归零时自动触发 AB 卸载。

#### Scenario: 正常释放资源
- **WHEN** 调用 `ReleaseAsset("Hero")` 且 refCount=1
- **THEN** 系统将 refCount 减为 0，从缓存移除资源，若所属 AB 的 refCount 和 instanceCount 均归零则卸载 AB

### Requirement: 加载路径优先级
系统 SHALL 按以下优先级加载 AB 文件：先检查 `Application.persistentDataPath`（热更新目录），找不到再检查 `Application.streamingAssetsPath`（包内目录）。Android 平台 StreamingAssets 使用 `UnityWebRequestAssetBundle` 加载。

#### Scenario: 热更新资源优先
- **WHEN** hero.ab 同时存在于 persistentDataPath 和 streamingAssetsPath
- **THEN** 系统加载 persistentDataPath 中的版本

#### Scenario: Android 平台包内 AB 加载
- **WHEN** 运行时平台为 Android 且 AB 仅在 StreamingAssets 中
- **THEN** 系统使用 UnityWebRequestAssetBundle 加载 APK 内资源

### Requirement: 并发加载限制
系统 SHALL 限制最大并发异步加载数为可配置值（默认 5）。超出限制的加载请求 SHALL 进入等待队列。

#### Scenario: 并发数限制
- **WHEN** 当前已有 5 个异步加载正在进行，再次发起第 6 个异步加载请求
- **THEN** 第 6 个请求进入等待队列，待有任务完成后自动开始执行

### Requirement: 编辑器模式兼容
在 Unity Editor 下，系统 SHALL 支持使用 `AssetDatabase.LoadAssetAtPath` 加载资源，无需提前打包 AB。EditorAssetProvider SHALL 维护模拟引用计数以检测 Load/Release 配对错误。

#### Scenario: 编辑器模式加载
- **WHEN** 在 Unity Editor 中运行且未配置 AB 路径
- **THEN** 系统使用 AssetDatabase 加载资源，开发流程与真机模式一致

#### Scenario: 编辑器模式引用计数检测
- **WHEN** Editor 模式下调用 LoadAsset("Hero") 但从未调用 ReleaseAsset("Hero")
- **THEN** 系统在 OnDestroy 或退出时输出警告：存在未配对的 Load/Release

### Requirement: 调试与诊断 API
系统 SHALL 提供调试接口：`GetLoadedBundles()`、`GetLoadedAssets()`、`GetMemorySummary()`。通过 `RESOURCE_DEBUG` 编译宏控制诊断代码是否编译。

#### Scenario: 查询已加载 AB 列表
- **WHEN** 调用 `GetLoadedBundles()`
- **THEN** 系统返回所有已加载 AB 的名称、文件大小（估算）、refCount、instanceCount

#### Scenario: 查询内存概览
- **WHEN** 调用 `GetMemorySummary()`
- **THEN** 系统返回已加载 AB 总数、总 AB 内存占用（估算）、各 AB 的内存分布

### Requirement: Debug UI 面板
在 `RESOURCE_DEBUG` 宏开启时，系统 SHALL 提供 IMGUI Debug 面板，树形展示 AB → Asset → 引用计数/内存。

#### Scenario: Debug 面板显示加载层级
- **WHEN** RESOURCE_DEBUG 开启且按指定快捷键
- **THEN** 系统渲染 IMGUI 窗口，树形列出所有已加载 AB，展开 AB 显示其下资源和引用计数

### Requirement: SubAsset 加载 API
系统 SHALL 支持从主 Asset 中加载子资源（SubAsset），如 FBX 中的 AnimationClip、SpriteAtlas 中的 Sprite。`LoadAsset<T>` 和 `LoadAssetAsync<T>` SHALL 增加可选参数 `subAssetName`。

#### Scenario: 加载 FBX 中的子 AnimationClip
- **WHEN** 调用 `LoadAsset<AnimationClip>("Hero", subAssetName: "Run")`
- **THEN** 系统加载 Hero 主 Asset，从中提取名为 "Run" 的 AnimationClip 子资源并返回

#### Scenario: 加载 SpriteAtlas 中的子 Sprite
- **WHEN** 调用 `LoadAsset<Sprite>("CommonAtlas", subAssetName: "icon_gold")`
- **THEN** 系统加载图集主 Asset，从中提取名为 "icon_gold" 的 Sprite 子资源并返回

#### Scenario: 不指定 subAssetName 时加载主 Asset
- **WHEN** 调用 `LoadAsset<GameObject>("Hero")` 不传 subAssetName
- **THEN** 系统行为与之前一致，返回主 Asset

#### Scenario: 子资源名称不存在
- **WHEN** 调用 `LoadAsset<Sprite>("CommonAtlas", subAssetName: "nonexistent")` 但该子资源不存在
- **THEN** 系统记录错误日志并返回 null

### Requirement: CRC 完整性校验
系统 SHALL 在 `AssetBundle.LoadFromFile` 时传入 CRC 值进行完整性校验。AssetInfo SHALL 包含 `crc` 字段（打包时自动计算）。CRC 校验失败时 SHALL 记录错误日志并返回 null。

#### Scenario: CRC 校验通过正常加载
- **WHEN** hero.ab 的 CRC 值与 AssetInfo 中记录的一致
- **THEN** LoadFromFile 正常返回 AssetBundle 对象

#### Scenario: CRC 校验失败
- **WHEN** hero.ab 的 CRC 值与 AssetInfo 中记录的不一致（文件损坏）
- **THEN** LoadFromFile 返回 null，系统记录错误日志 "hero.ab CRC 校验失败，文件可能已损坏"

#### Scenario: CRC 校验失败后尝试降级
- **WHEN** hero.ab CRC 校验失败，且该 AB 在 StreamingAssets 中有备份
- **THEN** 系统尝试从 StreamingAssets 重新复制该 AB 到 persistentDataPath 并再次加载
