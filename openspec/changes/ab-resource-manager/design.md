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
- Shader 变体收集不自动从项目扫描，需手动维护 ShaderVariantCollection

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

### 决策 7：Instantiate/Destroy 集成引用计数 — 解决副本资源追踪

**选择**：引用计数的触发点从 LoadAsset 调用次数，改为 Instantiate/Destroy 对 AB 隐式引用的追踪。ResourceManager 暴露 `InstantiateAsset(string assetName)` 和 `DestroyAsset(GameObject instance)` 包装方法，内部自动维护 AB 引用关系。

**问题**：当前设计 LoadAsset 时 refCount++，但 Instantiate 出来的副本持有 AB 中的材质/贴图/网格引用，这些是隐式的。Destroy 副本时没有 refCount--，导致引用计数无法真实反映资源使用状态。

**方案**：

```csharp
// 用户层使用
GameObject hero = ResourceManager.Instance.InstantiateAsset("Hero", parent);

// 销毁时
ResourceManager.Instance.DestroyAsset(hero);
```

**实现**：
- `AssetInfo` 新增 `referencedBundles` 字段，记录该 Prefab 引用了哪些 AB（含依赖链）
- `InstantiateAsset` 内部：先 LoadAsset → Instantiate → 遍历 referencedBundles，对每个 AB 的 `_instanceCount`++
- `DestroyAsset` 内部：查找该实例对应的 AB 列表 → 各 AB 的 `_instanceCount`-- → 归零且 refCount 也为零时 Unload(true)
- BundleInfo 新增 `_instanceCount` 字段，与 `refCount`（纯 LoadAsset 计数）解耦
- 当 `refCount == 0 && _instanceCount == 0` 时，触发 AB 卸载

**替代方案**：只在 AB 级别计数（不做实例追踪）—— 否决，因为 Instantiate 出来的副本对 AB 的引用是不可见的，这会导致生产环境随机出现粉色/消失的 bug。

### 决策 8：Android StreamingAssets 兼容 — 平台感知加载路径

**选择**：`GetBundlePath` / `LoadSingleBundle` 增加平台分支。Android 平台 StreamingAssets 中的 AB 使用 `UnityWebRequestAssetBundle` 加载（因为 APK 内不是文件系统），其他平台保持 `LoadFromFile`。

**实现**：
```
#if UNITY_ANDROID && !UNITY_EDITOR
    Android 路径：jar:file:// + Application.dataPath + "!/assets/" + bundlePath
    使用 UnityWebRequestAssetBundle 异步读取
    等待完成 → DownloadHandlerAssetBundle.GetContent
#else
    AssetBundle.LoadFromFile(path)
#endif
```

**理由**：Android 的 StreamingAssets 被打包在 APK 内部，不是真实文件系统路径，`File.Exists` 和 `LoadFromFile` 都会失败。

### 决策 9：AssetInfoConfig 自举加载 — 突破鸡生蛋问题

**选择**：AssetInfoConfig 使用 `Resources.Load<AssetInfoConfig>` 加载，不放入 AB。它是一种"元配置"，必须在任何 AB 加载之前可用。AB Provider 启动时，先通过 Resources API 加载 AssetInfoConfig，再初始化 AB 系统。

**理由**：如果 AssetInfoConfig 本身在某个 AB 中，加载第一个资源时还无法知道该配置在哪个 AB，形成循环依赖。Resources 是 Unity 内置路径，绕过了 AB 系统，且 AssetInfoConfig 本身只有几 KB，对首包体积影响可忽略。

### 决策 10：异步加载并发竞争防护 — AssetRef 状态机

**选择**：AssetRef 引入状态机（Unloaded → Loading → Loaded → Failed），当多人同时异步加载同一资源时，第一个请求标记为 Loading，后续请求的回调加入等待队列，加载完成后统一通知。

**实现**：AssetRef 加入 `List<Action<Object>> pendingCallbacks` 和 `LoadState state`。LoadAssetAsync 发现 state == Loading 时，将回调加入 pendingCallbacks 而不发起新请求。加载完成后遍历 pendingCallbacks 统一回调。

**理由**：防止同一个资源被并发加载多次，导致出现多个 AssetRef 且引用计数错乱。

### 决策 11：全局/场景资源区分 — BundleInfo.isPermanent

**选择**：BundleInfo 新增 `isPermanent` 标记。全局 AB（common_ui、common_shader、全局音频等）标记为 isPermanent = true。`CleanupForSceneChange()` 只卸载 isPermanent = false 且引用计数归零的 AB。

**配置方式**：在打包配置 JSON 中指定哪些 AB 是 Permanent。

**理由**：场景切换不应卸载全局常驻的 UI 图集和 Shader，否则每个场景都要重新加载，导致卡顿和不必要的内存波动。

### 决策 12：资源降级 — fallbackAssetName

**选择**：AssetInfo 新增 `fallbackAssetName` 字段，加载失败时自动尝试加载降级资源。

**实现**：LoadAsset 失败后，若 fallbackAssetName 不为空，递归调用 LoadAsset(fallbackAssetName)。二级降级也失败则返回 null 并记录错误。

**典型降级链**：Hero_Skin_02 → Hero_Default → null

**理由**：真实项目线上环境无法保证所有资源都正确下载/存在，降级机制保证游戏基本可用性（而不是穿模/白模/粉色）。

### 决策 13：Shader 变体预处理 — ShaderVariantCollection

**选择**：打包流程中 SHALL 创建并维护 `ShaderVariantCollection`（SVC），收录所有游戏用到的 Shader 变体。在 `GraphicsSettings` 中注册 SVC，Unity 打包 AB 时会自动将 SVC 中的变体打入 AB。

**不在 ResourceManager 代码层面处理**，但在打包配置中强制要求 SVC 存在且覆盖所有已知变体组合。

**理由**：AB 打包只包含已编译的 Shader 变体。不同场景的 AB 可能用到不同的变体组合，默认不会互相包含。缺失变体 = 粉色材质。

### 决策 14：可观测性/诊断系统 — Debug UI + 统计

**选择**：BundleInfo 和 AssetRef 内置加载耗时、加载时间戳等统计字段。通过编译宏 `RESOURCE_DEBUG` 控制诊断代码是否编译。

**提供接口**：
- `GetLoadedBundles()` → 返回所有已加载 AB 名称 + 大小 + 引用数
- `GetLoadedAssets()` → 返回所有已缓存资源名称 + 类型 + 引用数  
- `GetMemorySummary()` → 总 AB 内存占用

**Debug UI**：IMGUI 面板（仅在 `#if RESOURCE_DEBUG` 下编译），树形展示 AB → Asset 层级，实时更新引用计数和内存占用。

**理由**：没有可观测性的资源系统是黑盒。线上问题时无法定位"哪个 AB 没卸载"、"内存为什么涨"。

### 决策 15：Android APK 压缩标志 — StreamingAssets 不压缩 .ab 文件

**选择**：打包 APK 时，通过 `build.gradle` 配置 `aaptOptions { noCompress '.ab' }` 或在 Unity Player Settings → Publishing Settings 中勾选 "Split Application Binary"，确保 StreamingAssets 中的 .ab 文件不被二次压缩。

**问题**：Android APK 本质是 ZIP 包，默认会对所有文件进行压缩。如果 .ab 文件被 APK 压缩，`UnityWebRequestAssetBundle` 加载时需要先解压到内存，导致：
- 加载耗时翻倍（解压 + 解析 AB）
- 内存峰值翻倍（压缩数据 + 解压后数据同时存在）
- 部分设备解压失败导致加载崩溃

**实现**：
- Unity 侧：`AssetBundleBuilder` 打包脚本输出警告，提示开发者配置 APK 不压缩 .ab
- Gradle 侧：在主模板 `mainTemplate.gradle` 中添加 `android { aaptOptions { noCompress '.ab', '.json' } }`
- 文档说明：在打包配置文档中明确标注此步骤为必选

**替代方案**：不做任何配置，依赖 Unity 默认行为 —— 否决，因为默认压缩会导致 Android 平台加载性能严重下降。

**理由**：AB 文件本身已经过 LZ4/LZMA 压缩，APK 二次压缩不仅无益（压缩率极低），反而严重损害加载性能。

### 决策 16：iOS 平台路径处理 — Application.dataPath 差异

**选择**：iOS 平台 StreamingAssets 路径使用 `Application.dataPath + "/Raw"` 而非通用的 `Application.streamingAssetsPath`（后者在 iOS 返回 `file://` 协议的 URL，`File.Exists` 无法识别）。

**实现**：
```csharp
#if UNITY_IOS && !UNITY_EDITOR
    string streamingPath = Application.dataPath + "/Raw/" + bundleName;
#else
    string streamingPath = Path.Combine(Application.streamingAssetsPath, bundleName);
#endif
```

**理由**：iOS 的 `Application.streamingAssetsPath` 返回 `Application.dataPath + "/Raw"` 但带有 `file://` 前缀。`File.Exists("file:///...")` 在 iOS 上返回 false，导致路径优先级判断失效，系统误认为包内无此 AB 而报错。

### 决策 17：Resources 双重打包检测 — 构建期校验

**选择**：`AssetBundleBuilder` 打包脚本在分配资源到 AB 之前，扫描所有 `Resources/` 目录下的文件，与 AB 打包配置中的资源列表做交集比对。若同一资源既在 Resources 又在 AB 中，输出编译错误并阻止打包。

**实现**：
```csharp
// AssetBundleBuilder 中
var resourcesFiles = Directory.GetFiles(Application.dataPath + "/Resources", "*", SearchOption.AllDirectories);
var abFiles = config.GetAllAssetPaths();
var duplicates = resourcesFiles.Intersect(abFiles).ToList();
if (duplicates.Any()) {
    foreach (var dup in duplicates)
        Debug.LogError($"资源 {dup} 同时存在于 Resources 和 AB 中，将导致包体膨胀！");
    EditorApplication.Exit(1); // 阻止打包
}
```

**理由**：同一资源同时存在于 Resources 和 AB 中，Unity 打包时会各存一份，导致包体膨胀。且 Resources 中的副本永远不会被使用（因为 AB 系统优先加载 AB），纯属浪费。

### 决策 18：SubAsset 加载支持 — 扩展 LoadAsset API

**选择**：`LoadAsset<T>` 和 `LoadAssetAsync<T>` 增加可选参数 `string subAssetName = null`。当 subAssetName 不为空时，从已加载的主 Asset 中提取子资源。

**典型场景**：
- FBX 模型中的子 AnimationClip：`LoadAsset<AnimationClip>("Hero", subAssetName: "Run")`
- SpriteAtlas 中的子 Sprite：`LoadAsset<Sprite>("CommonAtlas", subAssetName: "icon_gold")`
- 图集/精灵表：一个 AB 包含一个 Texture2D + 多个 Sprite

**实现**：
```csharp
public T LoadAsset<T>(string assetName, string subAssetName = null) where T : Object
{
    var mainAsset = LoadMainAsset(assetName);
    if (string.IsNullOrEmpty(subAssetName)) return mainAsset as T;
    
    var subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);
    // 或从已加载的 AB 中：mainAsset is Texture2D → 查找对应 Sprite
    return subAssets.FirstOrDefault(s => s.name == subAssetName) as T;
}
```

**AssetInfo 扩展**：新增 `subAssets` 字段（`List<string>`），打包时自动收集 FBX/Texture2D 的子资源名称列表。

**替代方案**：每个子资源单独打包为一个 AB —— 否决，因为 SpriteAtlas 的 Sprite 必须与 Texture 在同一个 AB 中才能正确引用。

**理由**：Unity 的 AB 系统以 Asset 为单位，一个 FBX 或 SpriteAtlas 是一个主 Asset，其子资源（AnimationClip、Sprite）不能单独加载。不提供 SubAsset 支持意味着要么放弃图集合批，要么每个 Sprite 单独打包（导致 DrawCall 爆炸）。

### 决策 19：AssetInfoConfig 热更新机制 — 双通道加载

**选择**：AssetInfoConfig 采用"双通道"加载策略。启动时优先从 `persistentDataPath` 加载（热更新版本），若不存在则从 `Resources` 回退（包内版本）。热更新流程中，AssetInfoConfig 作为一个特殊的 AB 文件参与版本比对和下载。

**实现**：
- 打包时：AssetInfoConfig 同时放入 `Resources/`（包内兜底）和一个独立的 `asset_config.ab`（热更新用）
- 加载时：`AssetInfoConfigLoader` 先检查 `persistentDataPath/asset_config.ab` 是否存在 → 存在则从 AB 加载 → 不存在则 `Resources.Load`
- 热更新时：`asset_config.ab` 参与 MD5 比对，有变更时下载到 persistentDataPath
- 版本号持久化：AssetInfoConfig 的版本号独立于资源版本号，记录在 `config_version.json`

**替代方案**：AssetInfoConfig 永远只从 Resources 加载 —— 否决，因为新增资源类型或修改配置后，热更新无法感知新资源配置。

**理由**：AssetInfoConfig 是资源加载的"地图"。如果地图不能热更新，新增的资源类型（如新英雄的新骨骼配置）就无法被 AB 系统识别。

### 决策 20：CRC 校验 — LoadFromFile 完整性保障

**选择**：`AssetBundle.LoadFromFile(path, crc)` 传入 CRC 值进行加载时完整性校验。AssetInfo 新增 `crc` 字段，打包时自动计算每个 AB 的 CRC 并写入配置表。

**实现**：
```csharp
public AssetBundle LoadSingleBundle(string bundleName)
{
    var info = _assetInfoConfig.GetBundleInfo(bundleName);
    var path = GetBundlePath(bundleName);
    return AssetBundle.LoadFromFile(path, 0, info.crc);
}
```

**CRC 计算**：打包脚本在 `BuildPipeline.BuildAssetBundles` 完成后，遍历输出目录的每个 .ab 文件，计算 CRC32 并写入 AssetInfoConfig。

**理由**：`LoadFromFile` 不校验文件完整性。如果 AB 文件在磁盘上损坏（闪存坏块、下载不完整、文件系统错误），加载不会报错但资源内容随机错乱。CRC 校验以极低成本（每个 AB 4 字节）防止此类问题。

### 决策 21：旧 AB 文件清理策略 — 基于 Manifest 的白名单

**选择**：热更新完成后，以服务器 Manifest 中的 AB 列表为"白名单"，删除 persistentDataPath 中不在白名单内的 .ab 文件。

**实现**：
```csharp
public void CleanupOldBundles()
{
    var validBundles = new HashSet<string>(serverManifest.GetAllBundleNames());
    var files = Directory.GetFiles(persistentPath, "*.ab");
    foreach (var file in files)
    {
        var name = Path.GetFileName(file);
        if (!validBundles.Contains(name))
        {
            File.Delete(file);
            Debug.Log($"已清理旧 AB 文件: {name}");
        }
    }
}
```

**触发时机**：热更新下载全部完成并校验通过后，在 `Initialize()` 的最后阶段执行。

**安全措施**：仅在热更新成功（所有文件 MD5 校验通过）后才执行清理；若热更新失败回退，不执行清理。

**理由**：多次热更新后，persistentDataPath 中会积累大量旧版本 AB 文件（如 hero_v1.ab、hero_v2.ab、hero_v3.ab），占用用户存储空间。不清理的话，重度玩家可能积累数百 MB 的废弃文件。

### 决策 22：对象池集成 — PoolManager 与 ResourceManager 协作

**选择**：对象池中的 GameObject 同样持有 AB 引用，需要纳入引用计数追踪。提供两种集成方式：

**方式一（推荐）**：对象池通过 `ResourceManager.InstantiateAsset` 创建实例，`DestroyAsset` 销毁实例。对象池的"归还"操作不调用 DestroyAsset，而是将实例设为 inactive。引用计数在对象池"真正销毁"实例时才减少。

**方式二（兼容）**：对象池直接使用 Unity 的 `Instantiate` / `Destroy`，但需要手动调用 `ResourceManager.RegisterPooledInstance(go, assetName)` 和 `UnregisterPooledInstance(go)`。

**实现**：
```csharp
// ResourceManager 新增
public void RegisterPooledInstance(GameObject instance, string assetName)
{
    // 查找该 assetName 的 referencedBundles，instanceCount++
}

public void UnregisterPooledInstance(GameObject instance)
{
    // 查找该实例关联的 AB，instanceCount--
}
```

**理由**：对象池中的 GameObject（如子弹、特效、UI 列表项）持有材质/网格/贴图引用，这些引用指向 AB 中的资源。如果对象池持有 100 个子弹预制体实例，但 AB 系统认为 instanceCount=0 而卸载了 AB，所有子弹会变成粉色。

## Risks / Trade-offs

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| 引用计数忘记 Release 导致 AB 无法卸载 | 内存泄漏 | 提供 `CleanupForSceneChange()` 强制清理；日志告警长时间未释放的资源 |
| 依赖关系配置错误导致加载时材质丢失 | 粉色/材质丢失 | 打包后自动生成 AssetInfo 配置表而非手动维护；加载时校验依赖完整性 |
| 异步加载并发过多导致内存峰值 | 卡顿/OOM | 限制最大并发加载数（默认 5）；支持加载优先级队列 |
| 热更新下载失败导致资源不完整 | 游戏无法运行 | 下载后 MD5 校验；失败自动回退到包内资源；支持断点续传 |
| 图集被打散到多个 AB 导致内存翻倍 | 内存翻倍 | 图集单独打包，其他 AB 依赖引用 |
| 模式切换时 Provider 未清理导致资源泄漏 | 内存泄漏 / 粉色材质 | `SetLoadMode()` 内部先调用当前 Provider 的 `Cleanup()` 再切换 |
| Instantiate 副本引用不可追踪 | 场景中有实例但 AB 已被卸载 → 粉色/消失 | 不直接使用 Unity 的 Instantiate/Destroy，全部通过 ResourceManager 的 `InstantiateAsset` / `DestroyAsset` 包装 |
| Android 平台 StreamingAssets LoadFromFile 失败 | Android 加载崩溃 | 平台适配 `#if UNITY_ANDROID`，使用 UnityWebRequest 加载 APK 内 AB |
| AssetInfoConfig 本身在 AB 中形成循环依赖 | 首次加载失败 | AssetInfoConfig 放入 Resources 文件夹，走 Resources.Load 绕过 AB 系统 |
| 多人同时异步加载同一资源导致重复加载 | 引用计数错乱 / 内存泄漏 | AssetRef 状态机 + pendingCallbacks 等待队列 |
| 场景切换时全局 AB 被误卸载 | 卡顿 / 不必要重加载 | BundleInfo.isPermanent 标记，CleanupForSceneChange 跳过 Permanent AB |
| Shader 变体缺失导致粉色材质 | 粉色/效果丢失 | 打包流程必需 ShaderVariantCollection，收录所有变体 |
| 缺乏诊断手段，问题定位困难 | 无法定位内存/性能问题 | RESOURCE_DEBUG 宏控制 Debug UI + 统计接口 |
| Android APK 压缩 .ab 文件导致加载性能下降 | 加载慢 / 内存峰值 / 崩溃 | build.gradle 配置 noCompress '.ab'；打包脚本输出配置提醒 |
| iOS 路径 file:// 前缀导致 File.Exists 失败 | iOS 加载失败 | 平台宏区分 iOS 路径，使用 Application.dataPath + "/Raw" |
| Resources 与 AB 双重打包导致包体膨胀 | 包体变大 | 打包脚本构建期检测，发现重复则阻止打包 |
| SubAsset 无法加载导致图集/动画无法使用 | 功能缺失 | LoadAsset 增加 subAssetName 参数；AssetInfo 增加 subAssets 字段 |
| AssetInfoConfig 无法热更新导致新资源不可识别 | 热更新后新资源加载失败 | 双通道加载：persistentDataPath 优先，Resources 兜底 |
| AB 文件磁盘损坏导致资源错乱 | 随机显示异常 | LoadFromFile 传入 CRC 校验；下载后 MD5 校验 |
| 旧版本 AB 文件积累占用存储空间 | 用户存储空间浪费 | 热更新成功后基于 Manifest 白名单清理旧 .ab 文件 |
| 对象池实例持有 AB 引用但未被追踪 | 对象池对象变粉色/消失 | RegisterPooledInstance / UnregisterPooledInstance API |

## Open Questions

- 是否需要支持加载优先级队列？如果需要，优先级分为几级？
- 热更新增量包（差分更新）是否在首版实现？
- 是否需要支持运行时热切换 Provider（不退出游戏切换加载模式）？
- SimulationProvider（Mock 模式用于单元测试）是否在首版实现？
- AssetInfoConfig 是否也支持 JSON 纯文本格式（无需 ScriptableObject，更灵活的启动加载）？
- Debug UI 是否需要发布版也保留（通过隐藏手势激活）？
- 对象池集成方式一（InstantiateAsset/DestroyAsset）是否需要对象池框架改造？改造范围多大？
- 旧 AB 清理是否需要保留最近 N 个版本的回退能力（而非仅保留当前版本）？
