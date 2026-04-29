# Architecture — 类银河城游戏系统架构

## 架构总览

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│                       5 层分层架构                              │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │  Layer 5  表现层  ── UI / 动画 / 特效 / 相机 / 音效      │  │
│  └───────────────────────────────────────────────────────────┘  │
│                            ↑                                    │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │  Layer 4  玩法层  ── 战斗 / 探索 / 成长 / 任务 / 解谜    │  │
│  └───────────────────────────────────────────────────────────┘  │
│                            ↑                                    │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │  Layer 3  实体层  ── 角色 / 敌人 / NPC / 道具 / 投射物   │  │
│  └───────────────────────────────────────────────────────────┘  │
│                            ↑                                    │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │  Layer 2  系统层  ── 场景 / 存档 / 输入 / 事件 / UI / 配置│  │
│  └───────────────────────────────────────────────────────────┘  │
│                            ↑                                    │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │  Layer 1  基础层  ── 资源 / 对象池 / 工具集 / Singleton  │  │
│  └───────────────────────────────────────────────────────────┘  │
│                                                                 │
│  核心原则: 上层依赖下层，下层不依赖上层，同层通过 EventBus 解耦  │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 模块依赖图

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│                              Layer 5  表现层                                 │
│                                                                             │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐     │
│  │UI Panels │  │ Animation│  │   VFX    │  │  Camera  │  │  Audio   │     │
│  │(Base/    │  │ Controller│ │  System  │  │Cinemachine│  │Output    │     │
│  │Normal/   │  │          │  │          │  │          │  │          │     │
│  │Dialog/   │  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘     │
│  │Toast)    │       │             │             │             │           │
│  └────┬─────┘       │             │             │             │           │
│       │             │             │             │             │           │
│       └─────────────┼─────────────┼─────────────┼─────────────┘           │
│                     │             │             │                         │
├─────────────────────┼─────────────┼─────────────┼─────────────────────────┤
│                     │             │             │                         │
│                              Layer 4  玩法层                               │
│                     │             │             │                         │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐                  │
│  │ Combat   │  │Explore   │  │ Growth   │  │  Quest   │                  │
│  │ System   │  │ System   │  │ System   │  │ System   │                  │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘                  │
│       │             │             │             │                         │
│       └─────────────┼─────────────┼─────────────┘                         │
│                     │             │                                       │
├─────────────────────┼─────────────┼───────────────────────────────────────┤
│                     │             │                                       │
│                              Layer 3  实体层                               │
│                     │             │                                       │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │
│  │  Player  │  │  Enemy   │  │   NPC    │  │   Item   │  │Projectile│   │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘   │
│       │             │             │             │             │           │
│       └─────────────┼─────────────┼─────────────┼─────────────┘           │
│                     │             │             │                         │
├─────────────────────┼─────────────┼─────────────┼─────────────────────────┤
│                     │             │             │                         │
│                              Layer 2  系统层                               │
│                     │             │             │                         │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │
│  │  Scene   │  │  Save    │  │  Input   │  │  Config  │  │ EventBus │   │
│  │ Manager  │  │ Manager  │  │ Manager  │  │ Manager  │  │          │   │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘   │
│       │             │             │             │             │           │
│  ┌──────────┐  ┌──────────┐      │             │             │           │
│  │UIManager │  │  Audio   │      │             │             │           │
│  └────┬─────┘  │ Manager  │      │             │             │           │
│       │        └────┬─────┘      │             │             │           │
│       │             │             │             │             │           │
│       └─────────────┼─────────────┼─────────────┼─────────────┘           │
│                     │             │             │                         │
├─────────────────────┼─────────────┼─────────────┼─────────────────────────┤
│                     │             │             │                         │
│                              Layer 1  基础层                               │
│                     │             │             │                         │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐                  │
│  │ Resource │  │  Object  │  │  Utils   │  │Singleton │                  │
│  │  Loader  │  │   Pool   │  │          │  │          │                  │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘                  │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 跨层数据流

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│  ┌─────────────┐     ┌─────────────┐     ┌─────────────┐       │
│  │   Luban     │────▶│  Config     │────▶│  各系统     │       │
│  │  (Excel表)  │     │  Manager    │     │  查询配置   │       │
│  └─────────────┘     └─────────────┘     └──────┬──────┘       │
│                                                  │              │
│  配置管线                                         │ 游戏状态     │
│  ════════                                        │              │
│  Excel → Luban → C# 类                          ▼              │
│              → JSON/Binary               ┌─────────────┐       │
│                                          │  SaveManager │       │
│                                          └──────┬──────┘       │
│                                                  │              │
│  存档管线                                         │              │
│  ════════                                        ▼              │
│  SaveData ←→ Newtonsoft.Json ←→ .json 文件      │
│                                                                 │
│  事件管线                                                        │
│  ════════                                                        │
│  任意系统 ──Publish──▶ EventBus ──Subscribe──▶ 关心者           │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 场景架构

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│   Persistent Scene (常驻，永不卸载)                              │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │                                                         │   │
│   │  ┌──────────────────────────────────────────────────┐   │   │
│   │  │  所有 Manager 单例                                │   │   │
│   │  │  GameManager / SaveManager / InputManager        │   │   │
│   │  │  ConfigManager / AudioManager / EventBus         │   │   │
│   │  └──────────────────────────────────────────────────┘   │   │
│   │                                                         │   │
│   │  ┌──────────────────────────────────────────────────┐   │   │
│   │  │  UIManager → UIRoot (Canvas)                     │   │   │
│   │  │  ToastLayer / DialogLayer / PanelLayer           │   │   │
│   │  └──────────────────────────────────────────────────┘   │   │
│   │                                                         │   │
│   │  ┌──────────────────────────────────────────────────┐   │   │
│   │  │  Player (DontDestroyOnLoad)                       │   │   │
│   │  │  Rigidbody2D + Camera + 核心组件                  │   │   │
│   │  └──────────────────────────────────────────────────┘   │   │
│   │                                                         │   │
│   │  ┌──────────┐                                          │   │
│   │  │EventSystem│                                          │   │
│   │  └──────────┘                                          │   │
│   │                                                         │   │
│   └─────────────────────────────────────────────────────────┘   │
│                              │                                   │
│         ┌────────────────────┼────────────────────┐             │
│         ▼                    ▼                    ▼             │
│   ┌──────────┐        ┌──────────┐        ┌──────────┐        │
│   │Forest_01 │        │ Cave_01  │        │Castle_01 │        │
│   │(Additive)│        │(Additive)│        │(Additive)│        │
│   │          │        │          │        │          │        │
│   │Tilemap   │        │Tilemap   │        │Tilemap   │        │
│   │Enemies   │        │Enemies   │        │Boss      │        │
│   │NPCs      │        │Traps     │        │NPCs      │        │
│   └──────────┘        └──────────┘        └──────────┘        │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 核心系统清单

### Layer 1 — 基础层

| 系统 | 职责 | 主要API |
|------|------|---------|
| **ResourceLoader** | 预制体异步加载与卸载；资源缓存管理；加载进度回调 | `LoadAsync<T>(path)`, `Unload(path)`, `Preload(paths)` |
| **ObjectPool** | GameObject 复用；预热与缩容；支持多种对象类型分类管理 | `Get<T>(prefab)`, `Release(obj)`, `Prewarm(count)` |
| **Utils** | Vector/Transform 扩展方法；数学工具；协程/异步辅助；Gizmos 调试绘制 | 静态工具类 |
| **Singleton** | MonoBehaviour 单例基类；DontDestroyOnLoad 持久单例；懒加载单例 | `MonoSingleton<T>`, `PersistentSingleton<T>` |

### Layer 2 — 系统层

| 系统 | 职责 | 主要API |
|------|------|---------|
| **GameManager** | 多场景加载/卸载 (Additive)；场景切换进度；场景切换时 UI 联动；玩家出生点管理；相邻场景预加载 | `LoadArea(name)`, `UnloadArea(name)`, `OnProgress` 事件 |
| **SaveManager** | 多槽位存档 (JSON)；序列化/反序列化 (Newtonsoft.Json)；存档校验（版本号兼容）；自动存档（检查点触发）；存档加密 | `Save(slot, data)`, `Load(slot)`, `DeleteSlot(slot)` |
| **InputManager** | 输入映射 (Input System)；按键绑定与改键；手柄支持；输入缓冲队列 | `GetAxis(name)`, `GetButtonDown(name)`, `Rebind(action)` |
| **ConfigManager** | Luban 表数据加载；配置查询接口；Editor 模式热重载；多语言文本表 | `Tables.Instance.TbXxx.Get(id)` |
| **EventBus** | 全局事件发布/订阅；系统间解耦通信；事件日志 (Debug模式) | `Publish(event, data)`, `Subscribe(event, handler)` |
| **AudioManager** | BGM 播放/切换/淡入淡出；SFX 2D/3D 播放；音量分组控制 (Master/BGM/SFX)；音频对象池 | `PlayBGM(id)`, `PlaySFX(id, pos)`, `SetVolume(group)` |
| **UIManager** | 四种面板类型管理 (Base/Normal/Dialog/Toast)；Panel + Controller 架构；面板栈操作 (Push/Pop/BringToTop)；面板缓存策略；过渡动画；加载锁定 | `OpenPanel<T>(args)`, `ClosePanel()`, `ShowToast(msg)` |

### Layer 3 — 实体层

| 系统 | 职责 | 组件构成 |
|------|------|----------|
| **Player** | 角色物理/移动/跳跃/攀墙/冲刺/滑铲；HP/MP/ATK 属性；动画状态机驱动；地面检测；受伤/死亡/复活 | `Rigidbody2D`, `Collider2D`, `Animator`, `SpriteRenderer` |
| **Enemy** | 敌人属性 (HP/ATK/DEF)；行为状态机 (巡逻/追击/攻击/死亡)；视野检测；攻击判定生成；掉落物管理；Boss 多阶段。**技能系统**: Tick 驱动状态机 (EnemySkill + SkillRunner)，支持暂停/慢动作/前摇进度查询，详见 [enemy-tick-skill-system](../enemy-tick-skill-system/design.md) | `Rigidbody2D`, `Collider2D`, `Animator`, `EnemyAI`, `EnemySkill`, `SkillRunner` |
| **NPC** | 对话树系统；商店交互；任务触发/完成；状态持久 (已对话/已交易) | `Collider2D`, `DialogueTree`, `QuestTrigger` |
| **Item** | 拾取检测；根据 Luban ID 获取配置；类型分类 (消耗品/装备/能力/关键物品/货币)；拾取特效 | `Collider2D(Trigger)`, `ItemData`, `PickupEffect` |
| **Projectile** | 飞行轨迹；伤害判定；生命周期管理；对象池集成 | `Rigidbody2D`, `Collider2D`, `Trajectory` |
| **InteractObj** | 存档点；传送门；可破坏物；开关机关；单向平台；能力门 | `Collider2D`, `InteractionLogic` |

### Layer 4 — 玩法层

| 系统 | 职责 | 关键逻辑 |
|------|------|----------|
| **CombatSystem** | 攻击判定 (近战/远程/技能)；伤害计算 (ATK-DEF公式)；受伤反馈 (击退/闪红/无敌帧)；打击感 (顿帧/震屏)；击杀/死亡流程 | `DoDamage(attacker, target)`, `CalcDamage(atk, def)` |
| **ExploreSystem** | 小地图实时渲染；区域探索度计算；已探索/未探索标记；传送点记录；能力门控检测；捷径解锁 | `UpdateMiniMap()`, `CheckGating(ability)` |
| **GrowthSystem** | 经验/等级 (Luban曲线)；装备槽位装配；能力解锁条件检测；背包物品管理 (排序/分类/使用) | `AddExp(amount)`, `EquipItem(id)`, `UnlockAbility(id)` |
| **QuestSystem** | 任务状态机 (未接/进行中/完成)；任务条件检测 (击杀/收集/到达)；任务奖励发放；任务日志 | `AcceptQuest(id)`, `CheckCondition()`, `CompleteQuest(id)` |
| **PuzzleSystem** | 开关/机关链；时序谜题；平台移动/旋转 | `ActivateSwitch(id)`, `CheckPuzzleState()` |

### Layer 5 — 表现层

| 系统 | 职责 | 包含面板/组件 |
|------|------|--------------|
| **UI Panels** | Base: MainMenu, GameHUD；Normal: 背包/装备/地图/技能/任务/商店/设置；Dialog: 确认框/存档/对话/物品详情；Toast: 获得物品/成就/任务进度/伤害数字 | 基于 Panel + Controller 架构 |
| **Animation** | 角色动画状态机 (Idle/Run/Jump/Attack/Hurt/etc.)；敌人动画 (巡逻/追击/攻击/死亡)；过渡动画 (场景切换/传送)；动画事件 (判定帧/音效时机) | `Animator Controller` |
| **VFX** | 攻击特效；受击特效；拾取特效；环境特效 (火/水/雾)；Boss 技能特效；屏幕后处理 (受伤闪屏/状态覆盖) | `ParticleSystem`, `PostProcessing` |
| **Camera** | 平滑跟随 (Cinemachine)；相机区域限制 (Confiner)；房间锁定相机 (Boss房)；相机震动；动态 FOV | `CinemachineVirtualCamera` |

---

## 系统间通信规则

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│  通信方式              适用场景                示例              │
│  ─────────────────────────────────────────────────────────────  │
│                                                                 │
│  直接引用              同层强耦合关系          CombatSystem     │
│  (Inspector绑定)       生命周期严格绑定         → Player        │
│                                                                 │
│  Singleton 访问         全局唯一服务            UIManager.       │
│                         上层通用依赖            Instance.Show    │
│                                                Toast()          │
│                                                                 │
│  EventBus              跨层/跨系统通知          Enemy死亡        │
│                         一对多广播              → QuestSystem   │
│                         解耦通信                → Achievement   │
│                                                                 │
│  ConfigManager         配置数据查询             Tables.         │
│  (Luban)               静态只读数据             Instance.       │
│                                                TbItem.Get(1001) │
│                                                                 │
│  SaveManager           存档/读档                SaveManager.    │
│                         玩家进度持久化           Instance        │
│                                                .Save(0, data)   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 架构约束

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│  约束 1: 依赖方向                                               │
│  ──────────────                                                 │
│  上层可依赖下层，下层禁止依赖上层                                │
│  Layer 1 是最底层，不依赖任何业务模块                            │
│  违反示例: 在 SaveManager 中直接调用 UIManager ✗               │
│                                                                 │
│  约束 2: Manager 实例化管理                                     │
│  ─────────────────────────                                     │
│  所有 Manager 必须通过 Singleton 访问                           │
│  禁止 new Manager() 或 FindObjectOfType<Manager>()             │
│  Manager 之间通过 Singleton.Instance 引用或 EventBus 通信      │
│                                                                 │
│  约束 3: 场景职责分离                                           │
│  ────────────────────                                           │
│  Persistent Scene: 只放 Manager、UIRoot、Player、EventSystem    │
│  Additive Scene: 只放 Tilemap、Enemy、NPC、Item、场景特有组件   │
│  禁止在 Additive Scene 中放置 Manager                           │
│                                                                 │
│  约束 4: 配置与存档分离                                         │
│  ────────────────────────                                       │
│  Luban 表 → 只读配置 (物品数值、敌人属性、场景定义)             │
│  JSON 存档 → 读写状态 (玩家位置、拥有物品、Boss击败记录)        │
│  存档中只存 ID 引用，不存具体数值                                │
│  禁止在 Lua 表中存玩家状态                                      │
│                                                                 │
│  约束 5: UI 二段式架构                                          │
│  ────────────────────                                           │
│  Panel: 只含 UI 组件引用，不含任何逻辑                          │
│  Controller: 所有业务逻辑和数据管理                              │
│  禁止在 Panel 中写业务逻辑                                       │
│  禁止在 Controller 中直接操作其他 Panel                         │
│                                                                 │
│  约束 6: 事件命名规范                                           │
│  ──────────────────                                             │
│  事件名: 过去式动词 + 主语 + 动作                               │
│  示例: "EnemyKilled", "ItemCollected", "AbilityUnlocked"       │
│  禁止使用中文事件名                                              │
│                                                                 │
│  约束 7: 异步加载安全                                           │
│  ────────────────────                                           │
│  所有场景/资源加载必须是异步的                                   │
│  加载中设置 isLoading 锁，阻塞新的加载请求                      │
│  禁止同步加载导致游戏卡顿                                        │
│                                                                 │
│  约束 8: 对象池强制                                             │
│  ────────────────                                               │
│  频繁创建/销毁的对象必须使用对象池                               │
│  包括: 投射物、特效粒子、伤害数字、敌人尸体                     │
│  禁止频繁 Instantiate/Destroy                                   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 目录结构

```
Assets/
└── Scripts/
    ├── Core/                    ← Layer 1: 基础层
    │   ├── Singleton/
    │   │   ├── MonoSingleton.cs
    │   │   └── PersistentSingleton.cs
    │   ├── Pool/
    │   │   └── ObjectPool.cs
    │   ├── Resource/
    │   │   └── ResourceLoader.cs
    │   └── Utils/
    │       ├── Extensions.cs
    │       └── MathUtil.cs
    │
    ├── Systems/                 ← Layer 2: 系统层
    │   ├── GameManager.cs
    │   ├── SaveManager.cs
    │   ├── InputManager.cs
    │   ├── ConfigManager.cs
    │   ├── AudioManager.cs
    │   └── EventBus.cs
    │
    ├── Entities/                ← Layer 3: 实体层
    │   ├── Player/
    │   │   ├── Player.cs
    │   │   ├── PlayerStateMachine.cs
    │   │   └── PlayerAbility.cs
    │   ├── Enemy/
    │   │   ├── Enemy.cs
    │   │   ├── EnemyAI.cs
    │   │   ├── SkillRunner.cs
    │   │   ├── SkillFactory.cs
    │   │   ├── Skills/
    │   │   │   ├── EnemySkill.cs
    │   │   │   ├── MeleeSlashSkill.cs
    │   │   │   ├── FireballSkill.cs
    │   │   │   └── GroundSpikeSkill.cs
    │   │   └── Manager/
    │   │       └── EnemyManager.cs
    │   ├── NPC/
    │   │   └── NPC.cs
    │   └── Items/
    │       ├── Item.cs
    │       └── Projectile.cs
    │
    ├── Gameplay/                ← Layer 4: 玩法层
    │   ├── Combat/
    │   │   └── CombatSystem.cs
    │   ├── Exploration/
    │   │   └── ExploreSystem.cs
    │   ├── Growth/
    │   │   └── GrowthSystem.cs
    │   └── Quest/
    │       └── QuestSystem.cs
    │
    ├── UI/                      ← Layer 5: 表现层 (UIManager)
    │   ├── Core/
    │   │   ├── UIManager.cs
    │   │   ├── Panel.cs
    │   │   ├── Controller.cs
    │   │   └── PanelControllerRegistry.cs
    │   └── Panels/
    │       ├── HUD/
    │       ├── Inventory/
    │       ├── Map/
    │       └── Dialog/
    │
    └── LubanGen/                ← Luban 生成
        └── Tables.cs
```

---

## 启动流程

```
┌─────────────────────────────────────────────────────────────────┐
│                    游戏启动时序                                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  1. Persistent Scene 加载                                        │
│     │                                                           │
│     ├── 2. Singleton 初始化 (按依赖顺序)                         │
│     │   ├── EventBus.Init()                                     │
│     │   ├── ResourceLoader.Init()                               │
│     │   ├── ConfigManager.Init() → 加载 Luban 数据              │
│     │   ├── InputManager.Init()                                 │
│     │   ├── AudioManager.Init()                                 │
│     │   ├── SaveManager.Init()                                  │
│     │   └── UIManager.Init() → 创建 UIRoot                      │
│     │                                                           │
│     ├── 3. 面板注册                                              │
│     │   └── PanelControllerRegistry.Register<HUD, HUDController>()│
│     │                                                           │
│     ├── 4. GameManager.Init()                                   │
│     │                                                           │
│     └── 5. 加载初始场景                                          │
│         ├── 显示 LoadingScreen (UIManager)                       │
│         ├── 加载首个 Additive Scene                              │
│         └── 隐藏 LoadingScreen                                   │
│                                                                 │
│  6. 玩家可操作                                                   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```
