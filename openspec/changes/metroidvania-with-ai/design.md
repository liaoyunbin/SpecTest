# 技术设计方案（总索引）

> 游戏设计见: [CoreLoop.md](CoreLoop.md)  
> 系统架构见: [Architecture.md](Architecture.md)  
> 
> **每个子系统的详细设计已拆分到对应的子 Change 中。**

## 技术选型总览

| 技术点 | 选型 | 子系统 |
|--------|------|--------|
| 引擎 | Unity + C# / 2D URP | - |
| UI 框架 | UIManager (Panel+Controller) | [ui-manager](../ui-manager/) |
| 场景方案 | Persistent + Additive | [scene-manager](../scene-manager/) |
| 配置管线 | Luban | [config-manager](../config-manager/) |
| 存档方案 | Newtonsoft.Json | [save-system](../save-system/) |
| 输入系统 | Unity Input System | [input-manager](../input-manager/) |
| 相机 | Cinemachine | (主 tasks) |
| 事件系统 | 自研 EventBus | [event-bus](../event-bus/) |
| 对象池 | 自研 ObjectPool | (Layer 1 基础层) |
| 敌人技能 | Tick 驱动状态机 | [enemy-tick-skill-system](../enemy-tick-skill-system/) |
| 角色控制 | State Pattern 13 状态 | [player-controller](../player-controller/) |
| 敌人 AI | State Pattern 行为树 | [enemy-ai](../enemy-ai/) |
| 物品系统 | Item + 分类 | [item-system](../item-system/) |
| NPC | 对话树 + 商店 | [npc-system](../npc-system/) |
| 战斗 | 伤害公式 + 打击感 | [combat-system](../combat-system/) |
| 探索 | 小地图 + 能力门控 | [exploration-system](../exploration-system/) |
| 成长 | 等级 + 装备 + 背包 | [growth-system](../growth-system/) |
| 任务 | 状态机 + 条件检测 | [quest-system](../quest-system/) |

## 跨系统设计（保留在主文档）

### 数据管线

配置数据：Luban 表（只读）→ [config-manager](../config-manager/)  
存档数据：Newtonsoft.Json（读写）→ [save-system](../save-system/)  
事件通信：EventBus → [event-bus](../event-bus/)

### 场景架构

Persistent Scene + Additive Scene → [scene-manager](../scene-manager/design.md)

### 游戏流程

```
启动 → MainMenu → Persistent加载 → 首个 Additive Scene → 游戏开始
                     ↑
                     └── 读档 → 恢复场景+状态 → 游戏开始
```

```
死亡 → 死亡动画 → PlayerDied → 复活 → 传送存档点 → PlayerRespawned
```

### 运行时数据全景

| 只读 (Luban) | 读写 (游戏状态) |
|--------------|----------------|
| TbItem, TbSkill, TbEnemy, TbScene, TbLevel, TbQuest, TbDrop, TbHUD, TbDialogue | Player状态, InventoryData, ExplorationData, ProgressData |

### 性能策略总览

| 策略 | 适用对象 | 实现位置 |
|------|----------|----------|
| 对象池 | 投射物/特效 | Layer 1 |
| 相邻预加载 | 场景 | [scene-manager](../scene-manager/) |
| UI 缓存 | 高频面板 | [ui-manager](../ui-manager/) |
| 敌人暂停 | 屏幕外/UI打开时 | [enemy-ai](../enemy-ai/) |
| 异步加载 | 所有资源 | Layer 1 ResourceLoader |