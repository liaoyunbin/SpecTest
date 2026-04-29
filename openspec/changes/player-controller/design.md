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
