# 热更新规格

## ADDED Requirements

### Requirement: 版本号比对
系统启动时 SHALL 向服务器请求最新版本号，与本地持久化存储的版本号进行比对。版本号不一致时 SHALL 触发资源更新流程。

#### Scenario: 版本一致无需更新
- **WHEN** 服务器版本号为 1.2.0，本地版本号也为 1.2.0
- **THEN** 系统跳过更新流程，直接进入游戏

#### Scenario: 版本不一致触发更新
- **WHEN** 服务器版本号为 1.3.0，本地版本号为 1.2.0
- **THEN** 系统向服务器请求 `files.json` 资源清单，启动更新流程

### Requirement: 资源清单比对
系统 SHALL 请求服务器的资源清单（`files.json`），包含所有 AB 的 MD5 值和文件大小。与本地已下载 AB 的 MD5 值逐项比对，生成差异下载列表。

#### Scenario: 差异文件识别
- **WHEN** 服务器 `hero.ab` 的 MD5 为 "abc123"，本地 `hero.ab` 的 MD5 为 "xyz789"
- **THEN** 系统将 `hero.ab` 加入下载列表

#### Scenario: 新增文件识别
- **WHEN** 服务器资源清单包含 `shop.ab`，但本地不存在该文件
- **THEN** 系统将 `shop.ab` 加入下载列表

### Requirement: 资源下载
系统 SHALL 通过 HTTP 逐个下载差异列表中的 AB 文件，下载过程中报告进度（当前文件进度 + 总体进度）。

#### Scenario: 下载 AB 文件
- **WHEN** 下载列表包含 hero.ab（1MB）和 shop.ab（500KB）
- **THEN** 系统先下载 hero.ab，再下载 shop.ab，每次下载完成后校验 MD5

### Requirement: MD5 完整性校验
每个 AB 文件下载完成后 SHALL 计算其 MD5 值，与服务器清单中的值比对。不匹配时 SHALL 重新下载该文件（最多重试 3 次）。

#### Scenario: MD5 校验通过
- **WHEN** hero.ab 下载完成，计算的 MD5 与服务器清单一致
- **THEN** 系统将文件保存到 persistentDataPath，更新本地版本号

#### Scenario: MD5 校验失败重试
- **WHEN** hero.ab 下载完成，计算的 MD5 与服务器清单不一致
- **THEN** 系统重新下载该文件，最多重试 3 次；3 次均失败则记录错误

### Requirement: 本地版本持久化
所有差异文件下载并校验完成后 SHALL 将新的版本号持久化到本地。下次启动时以此为本地版本号。

#### Scenario: 更新完成持久化版本
- **WHEN** 所有差异文件下载并校验完成
- **THEN** 系统将新版本号写入 `persistentDataPath/version.json`

### Requirement: 更新失败回退
当热更新过程中出现不可恢复的错误时，系统 SHALL 使用包内 StreamingAssets 中的资源继续运行，保证游戏基本可用。

#### Scenario: 网络断开时回退
- **WHEN** 热更新下载过程中网络断开且无法恢复
- **THEN** 系统使用 StreamingAssets 中的包内资源继续运行，不阻塞游戏启动

### Requirement: Manifest 同步更新
热更新下载时，Manifest 文件 SHALL 作为第一个被下载/更新的文件，确保后续 AB 加载时依赖关系是最新的。

#### Scenario: Manifest 优先更新
- **WHEN** 版本更新包含 hero_v2.ab（新依赖 shared_mat_v2.ab）
- **THEN** 系统先下载更新 Manifest，再下载 hero_v2.ab 和 shared_mat_v2.ab，保证 Manifest 中的依赖信息正确

#### Scenario: Manifest 版本不匹配检测
- **WHEN** 本地 Manifest 版本与服务器版本不一致
- **THEN** 系统将 Manifest 加入差异下载列表的首位

### Requirement: AssetInfoConfig 热更新
AssetInfoConfig SHALL 支持热更新。打包时 AssetInfoConfig 同时放入 `Resources/`（包内兜底）和独立的 `asset_config.ab`（热更新用）。启动加载时优先从 `persistentDataPath/asset_config.ab` 加载，不存在则从 `Resources` 回退。

#### Scenario: 热更新后使用新版 AssetInfoConfig
- **WHEN** persistentDataPath 中存在 asset_config.ab（热更新下载的新版本）
- **THEN** 系统从 asset_config.ab 加载 AssetInfoConfig，使用新版资源配置

#### Scenario: 无热更新时使用包内 AssetInfoConfig
- **WHEN** persistentDataPath 中不存在 asset_config.ab
- **THEN** 系统从 Resources.Load 加载包内 AssetInfoConfig

#### Scenario: asset_config.ab 参与版本比对
- **WHEN** 服务器 files.json 中 asset_config.ab 的 MD5 与本地不一致
- **THEN** 系统将 asset_config.ab 加入差异下载列表

### Requirement: 旧 AB 文件清理
热更新全部完成并校验通过后，系统 SHALL 以服务器 Manifest 中的 AB 列表为白名单，删除 persistentDataPath 中不在白名单内的 .ab 文件。

#### Scenario: 清理旧版本 AB 文件
- **WHEN** 热更新成功，persistentDataPath 中存在 hero_v1.ab（不在服务器 Manifest 中）
- **THEN** 系统删除 hero_v1.ab，释放存储空间

#### Scenario: 热更新失败时不执行清理
- **WHEN** 热更新过程中出现错误导致回退到包内资源
- **THEN** 系统不执行旧 AB 清理，保留所有现有文件

#### Scenario: 白名单内的文件保留
- **WHEN** hero.ab 在服务器 Manifest 的白名单中
- **THEN** 系统保留 hero.ab，不删除
