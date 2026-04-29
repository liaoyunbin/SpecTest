# 角色控制器 —— 设计文档

> 父级: [metroidvania-with-ai 技术设计方案](../metroidvania-with-ai/design.md)

## 核心方案

角色操控用 State Pattern，Animator 只管理动画表现。

## 角色状态清单 (13 状态)

| 状态 | 说明 | 动画 |
|------|------|------|
| Idle | 待机 | Idle |
| Run | 奔跑 | Run |
| Jump | 跳跃 | Jump |
| Fall | 下落 | Fall |
| Land | 落地 | Land |
| Dash | 冲刺 (能力) | Dash |
| WallSlide | 攀墙 (能力) | WallSlide |
| WallJump | 蹬墙跳 (能力) | WallJump |
| DoubleJump | 二段跳 (能力) | DoubleJump |
| Attack | 攻击 | Attack |
| Hurt | 受伤 | Hurt |
| Death | 死亡 | Death |
| Slide | 滑铲 (能力) | Slide |

## 状态转换关系

```
Idle ←→ Run
Idle → Jump → Fall → Land → Idle
Idle/Run/Jump/Fall → Dash → 返回空中状态
Fall (触墙) → WallSlide → WallJump
Jump/Fall (再跳跃) → DoubleJump
```

## 技能系统

> **基类由 [character-skill-system](../character-skill-system/design.md) 提供** (CharacterSkill / SkillRunner)

### PlayerSkill 继承层

```csharp
public class PlayerSkill : CharacterSkill
{
    protected new Player Owner => (Player)base.Owner;

    public override bool CanUse()
    {
        if (!IsReady) return false;
        if (Owner.IsDead) return false;
        if (!Owner.AbilityManager.IsUnlocked(Config.unlockConditionId)) return false;
        if (!Owner.Attributes.HasEnoughMp(Config.mpCost)) return false;
        if (!Owner.Input.GetSkillButton(Config.id)) return false;
        return OnCanUse();
    }

    public override void Activate()
    {
        Owner.Attributes.currentMp -= Config.mpCost;
        base.Activate();
    }

    protected virtual bool OnCanUse() => true;
}
```

### 3 种 PlayerSkill 实现

**DashSkill**: 水平方向快速移动 + 无敌帧
**DoubleJumpSkill**: 空中再次跳跃 (CanUse: !IsGrounded)  
**WallSlideSkill**: 触墙下滑 (可持续技能，通过 Interrupt 结束)
