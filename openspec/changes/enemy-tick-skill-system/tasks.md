# 怪物技能 — 实现任务

> **基类由 [character-skill-system](../character-skill-system/tasks.md) 实现**
> 本任务仅在 CharacterSkill 完成后执行。

- [ ] 实现 `EnemySkill.cs` — 继承 CharacterSkill，重写 CanUse() 加入 AI 判断
- [ ] 实现 `MeleeSlashSkill.cs` — 近战挥砍 (距离检测 + OverlapCircleNonAlloc)
- [ ] 实现 `FireballSkill.cs` — 火球投射 (对象池 + 方向瞄准)
- [ ] 实现 `GroundSpikeSkill.cs` — Boss 地刺 (警告指示器 + 持续伤害)
- [ ] 实现 `SkillFactory.cs` — 通过 className 反射创建技能

**AI 可生成**: 100%

## 前置依赖

必须先完成:
- [character-skill-system](../character-skill-system/tasks.md) — CharacterSkill / SkillRunner / ISkillOwner
