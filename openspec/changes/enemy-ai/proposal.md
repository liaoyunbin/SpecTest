# 敌人 AI

## 问题陈述

敌人需要智能行为，包括巡逻、追击、攻击和 Boss 阶段切换。

## 提议方案

手写 State Pattern 实现敌人 AI 状态机，简单敌人直接手写，复杂 Boss 可用可视化工具辅助。

## 核心功能

- 行为状态机 (Patrol/Chase/Attack/Death)
- 视野检测/追击范围
- 技能选择逻辑 (通过 SkillRunner)
- Boss 阶段切换
- HP/ATK/DEF 属性 + 受伤/死亡 + 掉落

## 预期成果

- EnemyAI 状态机
- 视野/距离检测
- Boss 阶段管理

## 相关约束

- 依赖 enemy-tick-skill-system (技能执行)
- 依赖 event-bus (击杀事件)