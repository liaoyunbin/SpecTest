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
| 敌人技能 | Tick 驱动状态机 | [character-skill-system](../character-skill-system/) (通用) + [enemy-tick-skill-system](../enemy-tick-skill-system/) (怪物) |
| 角色控制 | State Pattern 13状态 + PlayerSkill | [player-controller](../player-controller/) |
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

### 属性系统设计

> **跨系统共享设计** — 接口和数据结构由主文档定义，具体实现逻辑见各子系统。
> 
> **设计决策: 枚举 + 字段混合方案**
> - 核心固定属性 (HP/ATK/DEF...) → 命名字段（零GC，每帧伤害计算直接访问）
> - 可扩展属性 (抗性/特殊...) → 枚举 key + 字典（灵活扩展，不改结构体）
> - `AttrType` 枚举定义所有属性 ID，统一索引器 `this[AttrType]` 供 Buff 叠加和 Luban 对接

#### 属性枚举定义

```csharp
public enum AttrType
{
    MaxHP,
    MaxMP,
    ATK,
    DEF,
    SPD,
    CRI_Rate,        // 暴击率 (千分比，100 = 10%)
    CRI_Damage,      // 暴击伤害加成 (百分比，50 = +50%)
    // 可扩展:
    // FireResist, PoisonResist, LifeSteal,
}
```

#### 属性分类

| 类型 | 说明 | 玩家 | 怪物 |
|------|------|------|------|
| 一级属性 | 基础数值 | HP/MP/ATK/DEF/SPD/CRI_Rate/CRI_Damage/EXP | HP/ATK/DEF/SPD |
| 二级属性 | 衍生计算 | Damage(ATK-DEF)、CritChance、DamageReduction(DEF/(DEF+100)) | 同玩家 |

#### 玩家属性来源链路

```
Luban TbLevel (等级基础) ──┐
装备加成                   ├── Recalculate() → totalAttr → CombatSystem (伤害计算)
Buff/状态修正 ─────────────┘
```

#### 怪物属性来源链路

```
Luban TbEnemy → Enemy.attr (一次性读取) → (可选 Buff) → CombatSystem
```

#### 核心接口

```csharp
public interface IAttributeProvider
{
    BaseAttributes GetBaseAttributes();
}

public interface IAttackable : IAttributeProvider { }
public interface IDamageable
{
    void TakeDamage(int damage);
    bool IsDead { get; }
}
public interface IBuffable
{
    void ApplyBuff(Buff buff);
    void RemoveBuff(int buffId);
}
```

#### BaseAttributes — 混合访问

```csharp
[Serializable]
public class BaseAttributes
{
    // 核心属性 — 字段 (高频访问零GC)
    public int maxHp, maxMp, atk, def, spd, criRate, criDamage;

    // 扩展属性 — 字典 (灵活扩展)
    public Dictionary<AttrType, int> extras = new();

    // 统一索引器 — Buff叠加 / Luban对接走枚举
    public int this[AttrType type]
    {
        get => type switch
        {
            AttrType.MaxHP      => maxHp,
            AttrType.MaxMP      => maxMp,
            AttrType.ATK        => atk,
            AttrType.DEF        => def,
            AttrType.SPD        => spd,
            AttrType.CRI_Rate   => criRate,
            AttrType.CRI_Damage => criDamage,
            _ => extras.GetValueOrDefault(type, 0)
        };
        set
        {
            switch (type)
            {
                case AttrType.MaxHP:      maxHp = value; break;
                case AttrType.MaxMP:      maxMp = value; break;
                case AttrType.ATK:        atk = value; break;
                case AttrType.DEF:        def = value; break;
                case AttrType.SPD:        spd = value; break;
                case AttrType.CRI_Rate:   criRate = value; break;
                case AttrType.CRI_Damage: criDamage = value; break;
                default: extras[type] = value; break;
            }
        }
    }

    // 运算符重载 — 便捷叠加
    public static BaseAttributes operator +(BaseAttributes a, BaseAttributes b)
    {
        var r = new BaseAttributes();
        r.maxHp = a.maxHp + b.maxHp;
        r.maxMp = a.maxMp + b.maxMp;
        r.atk   = a.atk   + b.atk;
        r.def   = a.def   + b.def;
        r.spd   = a.spd   + b.spd;
        r.criRate   = a.criRate   + b.criRate;
        r.criDamage = a.criDamage + b.criDamage;
        foreach (var kv in a.extras)
            r.extras[kv.Key] = kv.Value + b.extras.GetValueOrDefault(kv.Key, 0);
        return r;
    }
}
```

#### PlayerAttributes — 多源叠加

```csharp
public class PlayerAttributes
{
    public BaseAttributes baseAttr;     // Luban 等级基础
    public BaseAttributes equipAttr;    // 装备加成
    public BaseAttributes buffAttr;     // Buff 修正
    public BaseAttributes totalAttr;    // 缓存: base + equip + buff

    public int currentHp, currentMp;
    public int level, exp;

    public void Recalculate()
    {
        totalAttr = baseAttr + equipAttr + buffAttr;
    }
}
```

#### 使用方式对比

```csharp
// 伤害计算 — 用字段，零GC
int dmg = Mathf.Max(1, player.totalAttr.atk - enemy.attr.def);

// Buff 叠加 — 用索引器，枚举驱动
void ApplyPotion()
{
    player.buffAttr[AttrType.ATK] += 10;
    player.buffAttr[AttrType.SPD] += 5;
    player.Recalculate();
}

// 遍历所有属性 — Buff 系统用
void ApplyPercentBuff(float percent)
{
    foreach (AttrType type in Enum.GetValues<AttrType>())
        attrs.buffAttr[type] += (int)(attrs.baseAttr[type] * percent);
}

// Luban 对接 — 枚举 key 天然匹配
void LoadFromLuban(TbLevel levelConfig)
{
    baseAttr[AttrType.MaxHP] = levelConfig.hp;
    baseAttr[AttrType.ATK]   = levelConfig.atk;
    // ...
}
```

#### 伤害计算通用公式

```csharp
// 基础伤害
int rawDamage = Max(1, ATK - DEF);
// 暴击判定 (千分比)
if (Random(0,1000) < CRI_Rate) rawDamage ×= (1.5 + CRI_Damage/100);
// 对玩家额外防御减免
if (target is Player) rawDamage ×= (1 - DEF/(DEF+100));
```

#### 属性重算触发时机

| 事件 | 触发 |
|------|------|
| 升级 | GrowthSystem → Recalculate |
| 装备/卸下 | GrowthSystem → Recalculate |
| Buff施加/移除 | BuffSystem → Recalculate |
| 使用消耗品 | GrowthSystem (仅 currentHp) |

#### Luban 表

**TbLevel**: level, expNeed, hp, mp, atk, def, spd  
**TbEnemy**: id, name, hp, atk, def, spd, exp, dropTable, aiType  
**TbSkillConfig**: id, name, type, damage(base on ATK), cooldown, windup, range  

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