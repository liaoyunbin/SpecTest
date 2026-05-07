# 引用计数规格

## ADDED Requirements

### Requirement: 资源级引用计数
每个从 AB 加载的资源 SHALL 拥有独立的引用计数（AssetRef.refCount）。每次 LoadAsset 成功时引用计数 +1，每次 ReleaseAsset 时引用计数 -1。

#### Scenario: 加载资源增加引用计数
- **WHEN** 调用 `LoadAsset<GameObject>("Hero")` 首次加载
- **THEN** 系统创建 AssetRef，refCount 设为 1

#### Scenario: 重复加载同一资源增加引用计数
- **WHEN** `Hero` 资源已加载（refCount=1），再次调用 `LoadAsset<GameObject>("Hero")`
- **THEN** 系统返回缓存的 asset 对象，refCount 增加为 2

#### Scenario: 释放资源减少引用计数
- **WHEN** 调用 `ReleaseAsset("Hero")` 时 refCount=2
- **THEN** 系统将 refCount 减为 1，不触发卸载

### Requirement: AB 级引用计数
每个 AB SHALL 拥有引用计数（BundleInfo.refCount），为该 AB 下所有 AssetRef.refCount 之和。BundleInfo.refCount 归零时 SHALL 触发 AB 卸载。

#### Scenario: AB 引用计数追踪
- **WHEN** hero.ab 下有 "Hero" (refCount=2) 和 "HeroUI" (refCount=1) 两个资源
- **THEN** hero.ab 的 BundleInfo.refCount 等于 3

### Requirement: 引用归零自动卸载
当 AssetRef.refCount 归零时 SHALL 从缓存移除该资源引用；当 BundleInfo.refCount 归零时 SHALL 调用 `AssetBundle.Unload(true)` 卸载整个 AB 及其所有已加载资源。

#### Scenario: 资源引用归零卸载资源
- **WHEN** "Hero" 的 refCount 从 1 减为 0
- **THEN** 系统从 assetCache 移除 "Hero"，更新所属 BundleInfo 的 refCount

#### Scenario: AB 引用归零卸载 AB
- **WHEN** hero.ab 下所有资源的 refCount 归零，BundleInfo.refCount 变为 0
- **THEN** 系统确认 loadedAssets 为空后，调用 `bundle.Unload(true)` 卸载 AB，从 bundleCache 移除

### Requirement: 强制清理未使用资源
系统 SHALL 提供 `UnloadUnusedAssets()` 方法，遍历所有 AB，将 refCount 为 0 的 AB 全部卸载。

#### Scenario: 场景切换时强制清理
- **WHEN** 调用 `CleanupForSceneChange()` 
- **THEN** 系统遍历 bundleCache，卸载所有 refCount 为 0 的 AB

### Requirement: 释放不存在的资源处理
当尝试释放未加载的资源时，系统 SHALL 记录警告日志且不崩溃。

#### Scenario: 释放未加载的资源
- **WHEN** 调用 `ReleaseAsset("NotLoadedAsset")` 但该资源不在 assetCache 中
- **THEN** 系统记录警告日志，不抛出异常
