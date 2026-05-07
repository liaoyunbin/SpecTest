# AB 文件加载规格

## ADDED Requirements

### Requirement: 同步加载 AB 文件
系统 SHALL 提供 `LoadFromFile` 同步加载 AB 文件的能力，返回 `AssetBundle` 对象。已加载的 AB SHALL 缓存到 `bundleCache`，后续请求直接返回缓存对象。

#### Scenario: 首次加载 AB
- **WHEN** 调用 `LoadBundle("hero.ab")` 且该 AB 尚未缓存
- **THEN** 系统从文件系统加载 hero.ab，创建 BundleInfo 并缓存，返回 AssetBundle 对象

#### Scenario: 重复加载已缓存 AB
- **WHEN** 调用 `LoadBundle("hero.ab")` 且该 AB 已在缓存中
- **THEN** 系统直接返回缓存的 AssetBundle 对象，不重复读取文件

### Requirement: 异步加载 AB 文件
系统 SHALL 提供 `LoadFromFileAsync` 异步加载 AB 文件的能力，通过协程实现，不阻塞主线程。

#### Scenario: 异步加载大 AB
- **WHEN** 调用 `LoadBundleAsync("battle_scene.ab")` 
- **THEN** 系统通过协程异步加载，加载期间主线程不卡顿，加载完成后通过回调通知

### Requirement: 自动加载依赖 AB
加载任意 AB 时，系统 SHALL 先从 Manifest 查询其所有依赖 AB，按依赖顺序优先加载依赖 AB，再加载主 AB。

#### Scenario: 加载有依赖的 AB
- **WHEN** hero.ab 依赖 shared_mat.ab，调用 `LoadBundle("hero.ab")`
- **THEN** 系统先加载 shared_mat.ab，再加载 hero.ab

#### Scenario: 依赖循环检测
- **WHEN** AB 之间存在循环依赖配置
- **THEN** 系统记录错误日志并跳过已加载的 AB，避免死循环

### Requirement: 加载状态管理
每个 AB SHALL 具有明确的加载状态：Unloaded（未加载）、Loading（加载中）、Loaded（已加载）、Failed（加载失败）。

#### Scenario: 防止重复加载
- **WHEN** 一个 AB 状态为 Loading，再次请求加载该 AB
- **THEN** 系统忽略重复请求，避免并发加载同一文件

### Requirement: 加载失败处理
AB 加载失败时 SHALL 标记状态为 Failed，记录错误日志，并向上层返回 null。系统 SHALL 支持最多 3 次重试加载。

#### Scenario: 加载失败并重试
- **WHEN** hero.ab 加载失败（文件不存在或损坏）
- **THEN** 系统标记状态为 Failed，记录错误日志，返回 null，并支持调用方重试加载

### Requirement: Android 平台 StreamingAssets 兼容
在 Android 平台上，从 StreamingAssets 加载 AB 时 SHALL 使用 `UnityWebRequestAssetBundle` 而非 `LoadFromFile`。因为 APK 包内的 StreamingAssets 不是真实文件系统。

#### Scenario: Android 平台加载包内 AB
- **WHEN** 运行时平台为 Android 且 AB 在 StreamingAssets 中
- **THEN** 系统使用 `UnityWebRequestAssetBundle` 读取 APK 内资源，加载完成后再从内容中获取 AssetBundle

#### Scenario: 非 Android 平台保持 LoadFromFile
- **WHEN** 运行时平台为 Windows / iOS / macOS 且 AB 在 StreamingAssets 中
- **THEN** 系统使用 `AssetBundle.LoadFromFile` 加载

### Requirement: AssetRef 异步加载并发控制
当多个调用方同时异步加载同一资源时，系统 SHALL 只发起一次加载请求，后续请求等待该次加载完成。AssetRef SHALL 持有状态机（Unloaded → Loading → Loaded）和等待回调队列。

#### Scenario: 并发异步加载同一资源
- **WHEN** PanelA 和 PanelB 同时调用 LoadAssetAsync("icon_gold")，AssetRef 状态为 Unloaded
- **THEN** 第一个请求将状态置为 Loading 并开始加载；第二个请求的回调加入 pendingCallbacks 队列，不发起新的加载

#### Scenario: 并发等待加载完成后统一回调
- **WHEN** icon_gold 加载完成
- **THEN** 系统遍历 pendingCallbacks 队列，依次调用所有等待方的 onComplete 回调，将状态置为 Loaded
