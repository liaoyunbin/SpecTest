# 技术设计方案

> 游戏设计见: [CoreLoop.md](CoreLoop.md)  
> 系统架构见: [Architecture.md](Architecture.md)

## 技术选型决策

### 选型总览

| 技术点 | 选型 | 备选方案 | 决策理由 |
|--------|------|----------|----------|
| 引擎 | Unity + C# | Unreal, Godot | 团队熟悉，2D 生态完善，社区资料丰富 |
| 渲染管线 | 2D URP | Built-in, HDRP | URP 对 2D 优化好，支持 2D Renderer + 后处理 |
| 物理 | Unity 2D Physics | 自研物理 | 内置稳定，配合 Rigidbody2D + Collider2D 满足需求 |
| UI 框架 | UIManager (Panel+Controller) | uGUI 原生, MVVM | 已在设计，两层架构职责清晰，美术友好 |
| UI 渲染 | uGUI + Canvas | UI Toolkit | uGUI 成熟稳定，银河城 UI 复杂度适中 |
| 场景方案 | Persistent + Additive | 单场景, 多场景(非Additive) | 无缝切换，管理简单，Player 持久化 |
| 配置管线 | Luban | ScriptableObject, 手写 JSON | 策划可填 Excel，导出自动化，类型安全 |
| 存档方案 | Newtonsoft.Json | BinaryFormatter, PlayerPrefs | 可读可调试，版本兼容易处理，生态成熟 |
| 输入系统 | Unity Input System | 旧 Input Manager | 支持手柄/键盘统一抽象，改键功能完善 |
| 相机 | Cinemachine | 手写相机 | 平滑跟随、区域限制、震屏开箱即用 |
| 事件系统 | 自研 EventBus | UnityEvent, C# event | 全局解耦，跨系统通信，轻量无依赖 |
| 对象池 | 自研 ObjectPool | PoolManager 插件 | 轻量够用，无外部依赖，可定制 |

### 为什么选 Additive Scene 而不是单场景？

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│  方案 A: 单大场景                    方案 B: Additive Scene     │
│  ────────────────                    ──────────────────────     │
│  所有内容在一个场景                   Persistent + 按需加载      │
│                                                                 │
│  优点: 无加载黑屏                   优点: 按需加载，内存可控     │
│  缺点: 内存占用大，全场景            缺点: 需管理场景加载卸载     │
│       一次性加载时间长                    需要预加载策略         │
│                                                                 │
│  选择 B 的原因:                                                  │
│  - 银河城地图大，单场景内存不可控                                 │
│  - 相邻场景预加载可消除感知延迟                                   │
│  - 常驻 Manager/Player 符合架构约束                               │
│  - 每个 Additive Scene 职责单一，易于协作                         │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 数据管线设计

### 配置管线 (Luban)

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│  策划操作                          程序使用                       │
│  ────────                          ────────                      │
│                                                                 │
│  ┌──────────┐                     ┌──────────────────────┐     │
│  │Excel 表  │                     │var cfg =             │     │
│  │物品.xlsx │  ── Luban ──►      │Tables.Instance.      │     │
│  │敌人.xlsx │    导出             │TbItem.Get(1001);     │     │
│  │技能.xlsx │                     │cfg.Name // "长剑"    │     │
│  │场景.xlsx │                     └──────────────────────┘     │
│  │任务.xlsx │                                                     │
│  └──────────┘                                                     │
│                                                                 │
│  Luban 输出:                                                      │
│  ├── C# 代码 (Tables.cs, TbItem.cs, ...)                        │
│  └── 数据文件 (JSON/Binary → StreamingAssets/LubanData/)        │
│                                                                 │
│  ConfigManager 启动时加载 → 全表常驻内存 (只读)                   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Luban 配置表设计

| 表名 | 关键字段 | 用途 |
|------|----------|------|
| **TbItem** | id, name, type, icon, atk, def, price, desc | 物品/装备定义 |
| **TbSkill** | id, name, type, mpCost, unlockCondition, desc | 技能定义 |
| **TbEnemy** | id, name, hp, atk, def, exp, dropTable, prefab, aiType | 敌人定义 |
| **TbScene** | id, sceneName, displayName, hudId, bgmId, neighbors[] | 场景定义 |
| **TbLevel** | level, expNeed, hpGrow, mpGrow, atkGrow, defGrow | 等级数值曲线 |
| **TbQuest** | id, name, type, acceptNpc, condition[], reward[], desc | 任务定义 |
| **TbDrop** | id, enemyId, itemId, rate, minCount, maxCount | 掉落表 |
| **TbHUD** | id, prefabName, elements[] | HUD 配置 |
| **TbDialogue** | id, npcId, condition, lines[] | 对话内容 |

### 存档管线 (Newtonsoft.Json)

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│  保存时:                                                         │
│  各系统状态 → SaveData (聚合) → JsonConvert.SerializeObject     │
│       → save_NNN.json → Application.persistentDataPath         │
│                                                                 │
│  读档时:                                                         │
│  save_NNN.json → JsonConvert.DeserializeObject<SaveData>       │
│       → 各系统恢复状态 → 查 Luban 表补全详细数据                 │
│                                                                 │
│  存档数据结构:                                                   │
│  SaveData                                                       │
│  ├── saveVersion: int (版本号，用于兼容)                         │
│  ├── saveTime: string                                           │
│  ├── playTime: float                                            │
│  ├── player: PlayerData (位置/HP/MP/等级/经验)                   │
│  ├── exploration: ExplorationData (场景/房间/探索度/存档点)     │
│  ├── inventory: InventoryData (物品/能力/装备/货币)             │
│  └── progress: ProgressData (Boss/任务/事件)                   │
│                                                                 │
│  关键原则: 存档只存 ID 引用，具体数值从 Luban 表查询              │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 场景架构设计

### Persistent Scene 结构

```
Persistent Scene
│
├── [Manager] GameManager        ← 场景加载/卸载/切换
├── [Manager] UIManager          ← UI 面板栈管理
├── [Manager] SaveManager        ← 存档/读档
├── [Manager] InputManager       ← 输入映射
├── [Manager] ConfigManager      ← Luban 配置查询
├── [Manager] AudioManager       ← 音频播放
│
├── UIRoot (Canvas, ScreenSpace-Overlay)
│   ├── ToastLayer
│   ├── DialogLayer
│   └── PanelLayer
│
├── Player (DontDestroyOnLoad)
│   ├── Rigidbody2D
│   ├── Collider2D
│   ├── Animator
│   ├── SpriteRenderer
│   ├── PlayerStateMachine
│   └── CinemachineVirtualCamera (跟随)
│
└── EventSystem
```

### Additive Scene 结构

```
Additive Scene (如 Forest_01)
│
├── Grid + Tilemap (地面/平台/墙壁)
├── 装饰层 Tilemap (背景/细节)
├── Enemies (挂载 Enemy + EnemyAI)
├── NPCs (挂载 NPC + DialogueTree)
├── Items/Pickups (挂载 Item + Collider2D Trigger)
├── InteractObjects (存档点/传送门/开关/能力门)
├── 场景入口点 (SpawnPoint)
└── 场景边界触发器 (场景切换触发)
```

### 场景切换流程

```
1. 玩家接触场景边界触发器
2. GameManager.LoadArea(targetScene)
3. 显示 LoadingMask (UIManager, 设置 isLoading=true)
4. 关闭所有 Dialog + Normal 面板
5. 卸载不再需要的 Additive Scene
6. 加载目标 Additive Scene (异步, LoadSceneMode.Additive)
7. 替换底层 HUD (新场景 HUD)
8. 传送 Player 到 SpawnPoint
9. 隐藏 LoadingMask (设置 isLoading=false)
10. 触发 OnSceneChanged 事件
```

---

## EventBus 事件设计

### 事件列表

| 事件名 | 发布者 | 订阅者 | 参数 |
|--------|--------|--------|------|
| `EnemyKilled` | Enemy | QuestSystem, AchievementSystem | enemyId, position |
| `ItemCollected` | Item | GrowthSystem, QuestSystem | itemId, count |
| `AbilityUnlocked` | GrowthSystem | ExploreSystem, UIManager | abilityId |
| `BossDefeated` | Boss(Enemy) | QuestSystem, ExploreSystem | bossId, areaId |
| `PlayerDied` | Player | GameManager, UIManager | deathPosition |
| `PlayerRespawned` | GameManager | Player, UIManager | checkpointId |
| `CheckpointActivated` | InteractObj | SaveManager, ExploreSystem | checkpointId |
| `AreaEntered` | GameManager | AudioManager, ExploreSystem | areaId |
| `AreaExited` | GameManager | AudioManager | areaId |
| `QuestAccepted` | NPC | QuestSystem, UIManager | questId |
| `QuestCompleted` | QuestSystem | UIManager, GrowthSystem | questId |
| `LevelUp` | GrowthSystem | UIManager, VFX | newLevel |
| `SceneLoadStart` | GameManager | UIManager, AudioManager | sceneName |
| `SceneLoadComplete` | GameManager | UIManager, ExploreSystem | sceneName |

---

## 游戏流程

### 游戏启动

```
Splash/Logo → MainMenu
                  ├── 新游戏 → Persistent 加载 → 首个 Additive Scene → 游戏开始
                  └── 继续 → Persistent 加载 → 读档 → 恢复场景 + 状态 → 游戏开始
```

### 主场景 UI 状态

```
游戏进行中:
  底层: GameHUD (血条/蓝条/货币/小地图/能力图标)
  普通层: 按需 (背包/地图/技能/任务/设置)
  弹窗层: 按需 (确认框/对话/物品详情)
  提示层: 按需 (获得物品/成就/任务进度)
```

### 死亡-复活流程

```
Player 死亡
  → 死亡动画
  → PlayerDied 事件
  → UIManager: 显示死亡提示
  → 短暂延迟
  → UIManager: 关闭所有面板
  → 读档 (或从最后存档点复活)
  → Player 传送到存档点
  → PlayerRespawned 事件
  → 恢复游戏
```

### 存档-读档流程

```
存档:
  玩家到达存档点 → 交互
  → 打开 SavePanel (Normal 层)
  → 选择槽位 → 确认
  → SaveManager.Save(slot, data)
  → 提示 "存档成功" (Toast)

读档:
  MainMenu → 继续游戏
  → 打开 SaveLoadPanel (Normal 层)
  → 显示槽位信息
  → 选择槽位 → 确认
  → SaveManager.Load(slot)
  → 恢复场景 + 所有系统状态
  → 进入游戏
```

---

## 数据内存模型

```
┌─────────────────────────────────────────────────────────────────┐
│                    运行时数据全景                                │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  只读数据 (Luban 表)                读写数据 (游戏状态)           │
│  ══════════════════                ══════════════════           │
│                                                                 │
│  Tables.Instance                    Player.Instance             │
│  ├── TbItem (物品)                   ├── currentHp, maxHp       │
│  ├── TbSkill (技能)                  ├── currentMp, maxMp       │
│  ├── TbEnemy (敌人)                  ├── level, exp             │
│  ├── TbScene (场景)                  ├── position               │
│  ├── TbLevel (等级曲线)              └── unlockedAbilities[]     │
│  ├── TbQuest (任务)                                              │
│  ├── TbDrop (掉落)                  InventoryData               │
│  ├── TbHUD (HUD)                    ├── ownedItemIds[]          │
│  └── TbDialogue (对话)               ├── itemCount{}            │
│                                      └── equipmentSlots{}       │
│  启动时一次性加载                                                │
│  常驻内存，只读                    ExplorationData              │
│                                      ├── currentScene            │
│                                      ├── unlockedRooms[]        │
│                                      └── activatedCheckpoints[] │
│                                                                 │
│                                     ProgressData                │
│                                      ├── defeatedBossIds[]       │
│                                      ├── completedQuestIds[]    │
│                                      └── triggeredEventIds[]    │
│                                                                 │
│                                     存档时序列化为 JSON          │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 性能策略

| 策略 | 适用对象 | 实现方式 |
|------|----------|----------|
| 对象池 | 投射物、特效粒子、伤害数字 | ObjectPool 预热 + 定时缩容 |
| 相邻预加载 | 下一个可能进入的场景 | allowSceneActivation=false |
| UI 缓存 | 高频面板 (背包/地图) | PanelConfig.cacheOnClose=true |
| 对象池 | 敌人 (同屏大量时) | EnemyPool 按类型预热 |
| Tilemap Chunk | 大型 Tilemap | Unity 内置自动管理 |
| 异步加载 | 所有资源/场景 | ResourceLoader + SceneManager.LoadSceneAsync |
