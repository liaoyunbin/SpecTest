# 通用角色技能系统 (Character Skill System)

## 问题陈述

当前怪物技能和玩家技能各自独立设计，但存在大量重复逻辑：
- 5 阶段状态机 (Windup→Active→Recovery→Cooldown→Idle)
- Tick 驱动 + 暂停/慢动作支持
- 冷却倒计时
- 前摇进度查询
- 技能配置结构

这些应该在角色层面统一，怪物和玩家只在触发条件和目标选择上有差异。

## 提议方案

抽离 **CharacterSkill** 通用基类，作为 Layer 2 与 Layer 3 之间的共用层：

- `CharacterSkill` — 5 阶段 Tick 驱动状态机（通用）
- `ISkillOwner` — 技能持有者接口（统一获取属性/朝向/动画）
- `SkillRunner` — 技能调度器（统一管理技能列表）
- `SkillConfig` — 技能配置（Luban 表，怪物玩家共用字段）

怪物和玩家分别继承：
- `EnemySkill : CharacterSkill` — AI 触发 + 距离条件
- `PlayerSkill : CharacterSkill` — 按键触发 + MP 消耗 + 解锁条件

## 预期成果

- 统一的技能执行框架
- 怪物和玩家技能共享配置表结构
- 玩家技能获得暂停/慢动作支持

## 非目标

- 不包含具体技能实现（属于各子类）
- 不包含 AI/Player 的触发逻辑（属于各子系统）

## 相关约束

- Tick 驱动，不依赖协程
- 依赖 Layer 1 对象池（投射物/特效复用）
- 无 Unity 特定依赖（纯 C# 可测试）
