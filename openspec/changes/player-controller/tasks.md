# 角色控制器 —— 实现任务

> 父级: [metroidvania-with-ai tasks](../metroidvania-with-ai/tasks.md)

### 基础控制
- [ ] 配置 Rigidbody2D + Collider2D
- [ ] 实现地面检测
- [ ] 实现水平移动 + 加速/减速
- [ ] 实现跳跃 (可变高度) + 土狼时间 + 输入缓冲
- [ ] 实现角色属性 (HP/MP/ATK/DEF)
- [ ] 配置 Animator + 动画状态机
- [ ] 实现角色受伤/死亡逻辑

**AI 可生成**: 80%（核心逻辑 AI 生成，手感调参必须手动）

### 技能系统
> **基类由 [character-skill-system](../character-skill-system/tasks.md) 实现**
> 本任务仅在 CharacterSkill 完成后执行。

- [ ] 实现 `PlayerSkill.cs` — 继承 CharacterSkill，CanUse: 按键+MP+解锁
- [ ] 实现 `SkillRunner` 集成到 Player.Update()
- [ ] 实现 `DashSkill.cs` — 水平快速移动 + 无敌帧
- [ ] 实现 `DoubleJumpSkill.cs` — 空中再跳跃 (CanUse: !IsGrounded)
- [ ] 实现 `WallSlideSkill.cs` — 触墙下滑 (可持续技能)

**AI 可生成**: 100%

### 能力系统
- [ ] 实现能力基类 `PlayerAbility`
- [ ] 实现能力解锁/切换管理
- [ ] 实现能力槽 UI 数据 (供 HUD 显示)

**AI 可生成**: 100%