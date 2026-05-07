# ResourceManager API 规格

## ADDED Requirements

### Requirement: 资源清单配置
系统 SHALL 维护一份 AssetInfo 配置表，记录每个资源的名称、所属 AB、依赖 AB 列表和资源类型。配置表 SHALL 在 AB 打包后自动生成。

#### Scenario: 查询资源信息
- **WHEN** 通过 assetName 查询配置表
- **THEN** 系统返回对应的 AssetInfo（包含 bundleName、dependencies、assetType）

#### Scenario: 查询不存在的资源
- **WHEN** 查询一个不在配置表中的资源名称
- **THEN** 系统记录错误日志并返回 null

### Requirement: 同步加载资源 API
系统 SHALL 提供 `LoadAsset<T>(string assetName)` 同步加载方法。方法内部自动处理依赖 AB 加载和资源缓存。

#### Scenario: 同步加载 GameObject 资源
- **WHEN** 调用 `LoadAsset<GameObject>("Hero")`
- **THEN** 系统自动加载 hero.ab 及其依赖 AB，从 AB 中加载 Hero 预制体，缓存后返回

#### Scenario: 同步加载已缓存资源
- **WHEN** 调用 `LoadAsset<Sprite>("icon_gold")` 且该资源已加载
- **THEN** 系统直接返回缓存对象，refCount +1，不访问文件系统

### Requirement: 异步加载资源 API
系统 SHALL 提供 `LoadAssetAsync<T>(string assetName, Action<T> onComplete, Action<float> onProgress)` 异步加载方法，支持进度回调。

#### Scenario: 异步加载带进度
- **WHEN** 调用 `LoadAssetAsync<GameObject>("BattleScene", callback, progressCallback)`
- **THEN** 系统异步加载依赖 AB 和主 AB，在每个阶段通过 progressCallback 报告进度（0.0 ~ 1.0），加载完成后通过 callback 返回资源

#### Scenario: 异步加载进度计算
- **WHEN** 资源有 3 个依赖 AB 需要加载
- **THEN** 加载进度 = (已完成步骤 + 当前步骤进度) / 总步骤数，其中总步骤数 = 依赖 AB 数量 + 主 AB 加载 + 资源加载

### Requirement: 释放资源 API
系统 SHALL 提供 `ReleaseAsset(string assetName)` 方法，将指定资源的引用计数减 1。当引用计数归零时自动触发卸载。

#### Scenario: 正常释放资源
- **WHEN** 调用 `ReleaseAsset("Hero")` 且 refCount=1
- **THEN** 系统将 refCount 减为 0，从缓存移除资源，若所属 AB 的 refCount 也归零则卸载 AB

### Requirement: 加载路径优先级
系统 SHALL 按以下优先级加载 AB 文件：先检查 `Application.persistentDataPath`（热更新目录），找不到再检查 `Application.streamingAssetsPath`（包内目录）。

#### Scenario: 热更新资源优先
- **WHEN** hero.ab 同时存在于 persistentDataPath 和 streamingAssetsPath
- **THEN** 系统加载 persistentDataPath 中的版本

#### Scenario: 回退到包内资源
- **WHEN** hero.ab 只存在于 streamingAssetsPath，persistentDataPath 中不存在
- **THEN** 系统加载 streamingAssetsPath 中的版本

### Requirement: 并发加载限制
系统 SHALL 限制最大并发异步加载数为可配置值（默认 5）。超出限制的加载请求 SHALL 进入等待队列。

#### Scenario: 并发数限制
- **WHEN** 当前已有 5 个异步加载正在进行，再次发起第 6 个异步加载请求
- **THEN** 第 6 个请求进入等待队列，待有任务完成后自动开始执行

### Requirement: 编辑器模式兼容
在 Unity Editor 下，系统 SHALL 支持使用 `AssetDatabase.LoadAssetAtPath` 加载资源，无需提前打包 AB。

#### Scenario: 编辑器模式加载
- **WHEN** 在 Unity Editor 中运行且未配置 AB 路径
- **THEN** 系统使用 AssetDatabase 加载资源，开发流程与真机模式一致
