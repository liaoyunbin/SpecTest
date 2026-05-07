# AB 包资源管理器 —— 设计文档

## Context

当前项目资源加载方式分散，部分模块使用 `Resources.Load`，部分直接在 Inspector 中拖拽引用，缺乏统一的资源生命周期管理。引入 AssetBundle 管理系统的核心动机：

- 减小首包体积，支持按需下载
- 支持热更新资源（无需重新发版即可替换图片、模型等）
- 精确控制内存：按场景加载/卸载，避免资源常驻内存
- 统一资源加载入口，便于后续维护和性能分析

## Goals / Non-Goals

**Goals:**
- 提供统一的同步/异步资源加载 API
- 自动处理 AB 依赖加载顺序
- 基于引用计数的 AB 和资源卸载
- 按场景/功能划分的打包策略
- 支持热更新的版本比对和差异下载

**Non-Goals:**
- 不实现资源加密/解密（后续版本考虑）
- 不实现资源预加载调度策略（在加载 API 基础上由上层模块自行控制）
- 不实现编辑器内的可视化打包工具（使用脚本+配置表驱动）

## Decisions

### 决策 1：打包粒度 —— 按场景/功能打包 + 共享资源独立打包

**选择**：混合策略。大资源（> 1MB）独立打包，小资源（< 100KB）合并打包，同时使用的资源合并打包，共享资源（UI 图集、通用 Shader）独立打包。

**替代方案**：按类型打包（textures.ab / models.ab）—— 否决，因为加载一个资源需要加载整个类型的 AB，内存不可控。

**理由**：混合策略在加载粒度和 AB 数量之间取得平衡，是最适合实际项目的方案。

### 决策 2：架构分层 —— ResourceManager + BundleInfo + AssetRef

**选择**：

```
┌─────────────────────────────────────────────────────────┐
│                  ResourceManager（Facade 层）              │
│   统一入口，将请求委托给 IResourceProvider                    │
├─────────────────────────────────────────────────────────┤
│  - _provider: IResourceProvider                         │
│  - _assetInfoConfig: AssetInfoConfig                    │
│  - _loadMode: LoadMode                                  │
├─────────────────────────────────────────────────────────┤
│  + LoadAsset<T>(assetName): T                          │
│  + LoadAssetAsync<T>(assetName, callback, progress)    │
│  + ReleaseAsset(assetName)                             │
│  + SetLoadMode(mode)                                   │
│  + CleanupForSceneChange()                             │
└─────────────────────────────────────────────────────────┘
```

**IResourceProvider**：资源加载策略接口，定义了 Initialize、LoadAsset、LoadAssetAsync、ReleaseAsset、CleanupForSceneChange 等方法。

**AssetBundleProviderBase**：AB 模式的公共抽象基类，内含双层引用计数、Bundle 缓存、依赖自动加载逻辑。LocalProvider 和 RemoteProvider 继承它。

**双层引用计数**：AssetRef.refCount 跟踪单个资源使用次数，BundleInfo.refCount 为其下所有 AssetRef.refCount 之和。BundleInfo.refCount 归零时 Unload(true) 整体卸载。

**替代方案**：单层计数（只跟踪 AB 级别）—— 否决，因为无法精确控制单个资源的生命周期。

### 决策 3：卸载策略 —— Unload(true) + 引用计数归零

**选择**：引用计数归零时调用 `AssetBundle.Unload(true)`，同步卸载 AB 及其所有资源。不使用 `Unload(false)` 以避免产生无法追踪的"孤儿资源"。

**理由**：
- `Unload(true)`：内存彻底释放，但要求场景中确定无引用。引用计数归零保证了这一点。
- `Unload(false)`：虽然 AB 文件卸载了，但资源仍在内存中，形成无法通过 AB 系统管理的孤儿资源，容易导致内存泄漏。

### 决策 4：加载路径优先级 —— PersistentDataPath > StreamingAssets

**选择**：先检查持久化目录（热更新下载的 AB），找不到再从 StreamingAssets（包内自带）加载。

**理由**：热更新的资源会覆盖包内资源，保证玩家获取最新资源。

### 决策 5：加载模式 —— 提供同步和异步两套 API

**选择**：`LoadAsset<T>` (同步) 和 `LoadAssetAsync<T>` (异步带进度回调)。

**理由**：
- 同步：适合小资源、编辑器调试、加载后立即使用的场景
- 异步：适合大资源、场景切换带 Loading 界面、热更新下载的场景

### 决策 6：可插拔加载模式 — 策略模式

**选择**：将资源加载的核心能力抽象为 `IResourceProvider` 接口，不同加载模式实现为该接口的不同 Provider。`ResourceManager` 作为外观层（Facade），通过枚举或配置选择当前 Provider 并将所有请求委托给它。

**架构图**：

```
┌──────────────────────────────────────────────────────────────┐
│                  ResourceManager（Facade）                      │
│   LoadAsset / LoadAssetAsync / ReleaseAsset                   │
│                     │  委托给 _provider                          │
│                     ▼                                           │
│             IResourceProvider（策略接口）                         │
│        ┌────────────┼────────────┬──────────────┐              │
│        ▼            ▼            ▼              ▼              │
│  EditorAsset     AssetBundle   AssetBundle   Resources        │
│  Provider        LocalProvider  RemoteProvider Provider        │
│  (AssetDatabase) (本地AB+引用计数)(远程AB+热更) (Resources.Load)  │
│                         ▲              ▲                       │
│                         └──────┬───────┘                       │
│                     AssetBundleProviderBase                     │
│                     (引用计数/缓存/依赖逻辑复用)                    │
└──────────────────────────────────────────────────────────────┘
```

**接口定义**：

```csharp
public interface IResourceProvider
{
    void Initialize();
    void Cleanup();
    T LoadAsset<T>(string assetName) where T : Object;
    void LoadAssetAsync<T>(string name, Action<T> onComplete, Action<float> onProgress = null) where T : Object;
    void ReleaseAsset(string assetName);
    bool IsLoaded(string assetName);
    void CleanupForSceneChange();
}
```

**各 Provider 职责**：

| Provider | 加载方式 | 依赖 | Release 行为 |
|----------|----------|------|-------------|
| `EditorAssetProvider` | `AssetDatabase.LoadAssetAtPath` | AssetInfo 配置表(path 字段) | 无操作 |
| `AssetBundleLocalProvider` | `AssetBundle.LoadFromFile` | Manifest + AssetInfo(bundle/deps) | 引用计数 → Unload(true) |
| `AssetBundleRemoteProvider` | 继承 LocalProvider，附加热更新下载 | 同上 + 服务器 version/files.json | 同上 |
| `ResourcesProvider` | `Resources.Load` | AssetInfo(resourcesPath) | `Resources.UnloadAsset` |

**AB 模式的公共逻辑复用**：`AssetBundleLocalProvider` 和 `AssetBundleRemoteProvider` 共用 `AssetBundleProviderBase` 抽象基类，内含双层引用计数、Bundle 缓存、依赖自动加载。子类仅需覆写 `GetBundlePath()` （本地路径 vs 先下载再返回本地路径）和 `Initialize()` （Local: 直接加载 Manifest；Remote: 先版本比对+差异下载再加载 Manifest）。

**模式切换机制**：在 Editor 中通过 Inspector 枚举选择模式；真机上根据是否启用热更新自动判断（热更开启 → Remote，否则 → Local）。编译期 `#if UNITY_EDITOR` 宏允许 Editor 下测试任意模式。

**替代方案**：将所有加载逻辑硬编码在同一类中，用 if-else 分支区分 —— 否决，违反开闭原则，新增加载方式需要修改核心类。

**理由**：策略模式让每种加载模式独立演进，新增 Addressables 等支持只需新建一个 Provider 类，核心代码零改动。编辑器中开 AssetDatabase 秒级调试，真机自动走 AB，切换零代码。

## Risks / Trade-offs

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| 引用计数忘记 Release 导致 AB 无法卸载 | 内存泄漏 | 提供 `CleanupForSceneChange()` 强制清理；日志告警长时间未释放的资源 |
| 依赖关系配置错误导致加载时材质丢失 | 粉色/材质丢失 | 打包后自动生成 AssetInfo 配置表而非手动维护；加载时校验依赖完整性 |
| 异步加载并发过多导致内存峰值 | 卡顿/OOM | 限制最大并发加载数（默认 5）；支持加载优先级队列 |
| 热更新下载失败导致资源不完整 | 游戏无法运行 | 下载后 MD5 校验；失败自动回退到包内资源；支持断点续传 |
| 图集被打散到多个 AB 导致内存翻倍 | 内存翻倍 | 图集单独打包，其他 AB 依赖引用 |
| 模式切换时 Provider 未清理导致资源泄漏 | 内存泄漏 / 粉色材质 | `SetLoadMode()` 内部先调用当前 Provider 的 `Cleanup()` 再切换 |

## Open Questions

- 是否需要支持加载优先级队列？如果需要，优先级分为几级？
- 热更新增量包（差分更新）是否在首版实现？
- 是否需要支持运行时热切换 Provider（不退出游戏切换加载模式）？
- SimulationProvider（Mock 模式用于单元测试）是否在首版实现？
