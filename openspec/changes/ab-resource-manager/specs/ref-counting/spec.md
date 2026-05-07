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

### Requirement: 实例级引用计数（Instantiate/Destroy 追踪）
BundleInfo SHALL 新增 `instanceCount` 字段，追踪通过 Instantiate 创建的实例对 AB 的隐式引用。AB 的实际卸载条件为：`refCount == 0 && instanceCount == 0`。

#### Scenario: Instantiate 增加实例计数
- **WHEN** 调用 `InstantiateAsset("Hero")` 创建实例
- **THEN** 系统查找该 Prefab 引用的所有 AB（含依赖链），对每个 AB 的 instanceCount +1

#### Scenario: Destroy 减少实例计数
- **WHEN** 调用 `DestroyAsset(heroInstance)` 销毁实例
- **THEN** 系统查找该实例关联的 AB 列表，对每个 AB 的 instanceCount -1

#### Scenario: 双重计数归零才卸载 AB
- **WHEN** hero.ab 的 refCount = 0 且 instanceCount = 0
- **THEN** 系统调用 `bundle.Unload(true)` 卸载 AB
- **WHEN** hero.ab 的 refCount = 0 但 instanceCount = 1
- **THEN** 系统不卸载 AB（场景中还有实例持有该 AB 的资源）

### Requirement: 引用归零自动卸载
当 AssetRef.refCount 归零时 SHALL 从缓存移除该资源引用；当 BundleInfo.refCount 与 instanceCount 均为 0 时 SHALL 调用 `AssetBundle.Unload(true)` 卸载整个 AB 及其所有已加载资源。

#### Scenario: 资源引用归零卸载资源
- **WHEN** "Hero" 的 refCount 从 1 减为 0
- **THEN** 系统从 assetCache 移除 "Hero"，更新所属 BundleInfo 的 refCount

#### Scenario: AB 三重计数归零卸载 AB
- **WHEN** hero.ab 下所有资源的 refCount 归零，且 instanceCount 也归零
- **THEN** 系统确认 loadedAssets 为空后，调用 `bundle.Unload(true)` 卸载 AB，从 bundleCache 移除

### Requirement: 全局资源 Permanent 保护
被标记为 isPermanent = true 的 BundleInfo，在 `CleanupForSceneChange()` 时 SHALL 不被卸载，即使引用计数归零。

#### Scenario: 场景切换保留全局 AB
- **WHEN** 调用 `CleanupForSceneChange()` 且 common_ui.ab 的 isPermanent = true
- **THEN** common_ui.ab 保留在内存中，不参与卸载

#### Scenario: 非 Permanent AB 正常卸载
- **WHEN** 调用 `CleanupForSceneChange()` 且 hero.ab 的 isPermanent = false，引用计数为 0
- **THEN** hero.ab 被正常卸载

### Requirement: 强制清理未使用资源
系统 SHALL 提供 `UnloadUnusedAssets()` 方法，遍历所有 AB，将非 Permanent 且引用计数归零的 AB 全部卸载。

#### Scenario: 场景切换时强制清理
- **WHEN** 调用 `CleanupForSceneChange()`
- **THEN** 系统遍历 bundleCache，卸载所有 isPermanent = false 且 refCount = 0 且 instanceCount = 0 的 AB

### Requirement: 释放不存在的资源处理
当尝试释放未加载的资源时，系统 SHALL 记录警告日志且不崩溃。

#### Scenario: 释放未加载的资源
- **WHEN** 调用 `ReleaseAsset("NotLoadedAsset")` 但该资源不在 assetCache 中
- **THEN** 系统记录警告日志，不抛出异常
