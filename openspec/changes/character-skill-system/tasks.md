# 通用角色技能系统 —— 实现任务

> 父级: [metroidvania-with-ai](../metroidvania-with-ai/)

## 核心框架

### 1. 创建目录和数据类
- [ ] 创建 `Assets/Scripts/Core/Skill/` 目录
- [ ] 创建 `SkillPhase.cs` — 5 阶段枚举
- [ ] 创建 `SkillConfig.cs` — 技能配置类 (含玩家扩展字段)
- [ ] 创建 `ISkillOwner.cs` — 技能持有者接口

**AI 可生成**: 100%

### 2. 实现 CharacterSkill 基类
- [ ] Tick(float deltaTime) 5 阶段状态机
- [ ] TransitionTo() 阶段切换 + 进入/退出钩子
- [ ] PhaseElapsed / PhaseProgress 进度追踪
- [ ] 全部虚方法钩子 (13个)
- [ ] Activate() / Interrupt() / ForceStop() / CanUse()

**AI 可生成**: 100%

### 3. 实现 SkillRunner
- [ ] AddSkill / GetSkill 技能管理
- [ ] Tick(float deltaTime) 统一调度
- [ ] TryExecute() / Interrupt() / ForceStopAll()

**AI 可生成**: 100%

---

## 优先级

| 优先级 | 任务 | 说明 |
|--------|------|------|
| 🔴 P0 | 1-3 | 共用核心，enemy-skill 和 player-skill 的前置依赖 |

## 依赖

```
Layer 1: ObjectPool (投射物/特效)
```
