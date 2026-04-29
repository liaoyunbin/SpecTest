# 角色控制器

## 问题陈述

银河城角色需要流畅的物理移动 + 13 种状态切换，是游戏手感的绝对核心。

## 提议方案

使用 State Pattern 实现角色控制器，Animator 只负责动画表现。

## 核心功能

- Rigidbody2D + Collider2D 物理移动
- State Pattern 状态机 (Idle/Run/Jump/Fall/Dash/WallSlide/WallJump/DoubleJump/Attack/Hurt/Death/Slide/Interact)
- 地面检测
- 可变高度跳跃 + 土狼时间 + 输入缓冲
- HP/MP/ATK/DEF 属性
- Animator 集成（仅动画表现）

## 能力系统

- 能力基类 `PlayerAbility`
- Dash (冲刺)
- WallSlide + WallJump (攀墙+蹬墙跳)
- DoubleJump (二段跳)
- 能力解锁/切换管理
- 能力槽 UI 数据

## 预期成果

- Player 角色物理学
- 13 状态 PlayerStateMachine
- 3+ 可解锁能力
- 手感可微调

## 相关约束

- 依赖 input-manager, event-bus
- 角色是 Persistent Scene 中 DontDestroyOnLoad 实体
- 手感调参必须手动测试