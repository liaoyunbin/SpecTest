# 怪物技能 — EnemySkill

> **基类由 [character-skill-system](../character-skill-system/) 提供**
> 本子系统仅包含 EnemySkill 继承层 + 3 种怪物技能实现。

## 问题陈述

怪物技能需要在通用 CharacterSkill 基础上增加 AI 驱动的技能选择和距离条件判断。

## 提议方案

继承 CharacterSkill，重写 CanUse() 加入 AI 判断逻辑。

## EnemySkill 继承层

```csharp
public class EnemySkill : CharacterSkill
{
    protected new Enemy Owner => (Enemy)base.Owner;

    public override bool CanUse()
    {
        if (!IsReady) return false;
        if (Owner.IsStunned || Owner.IsDead) return false;
        return OnCanUse();
    }

    protected virtual bool OnCanUse() => true;
}
```

## 3 种示例技能

### MeleeSlashSkill
- CanUse: 距离 ≤ range + 地面检测
- Windup: 面向目标 + 播动画
- Active: 圆形范围伤害判定 (OverlapCircleNonAlloc)
- Recovery: 设置可招架状态

### FireballSkill
- CanUse: 距离在 minRange~maxRange 之间
- Active: 从对象池获取投射物 + 发射

### GroundSpikeSkill (Boss)
- Windup: 地面警告指示器 + 闪烁加速
- Active: 地刺升起 + 持续伤害判定
- Recovery: 地刺下降动画

## 目录结构

```
Assets/Scripts/Entities/Enemy/Skills/
├── EnemySkill.cs
├── MeleeSlashSkill.cs
├── FireballSkill.cs
├── GroundSpikeSkill.cs
└── SkillFactory.cs
```

## 依赖

- [character-skill-system](../character-skill-system/) — CharacterSkill 基类
- [enemy-ai](../enemy-ai/) — Enemy Owner
- [config-manager](../config-manager/) — SkillConfig (Luban)
- [event-bus](../event-bus/) — EnemyKilled 等事件
