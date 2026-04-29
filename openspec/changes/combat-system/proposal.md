# 战斗系统

## 问题陈述

银河城战斗需要流畅的伤害判定、清晰的反馈和 boss 多阶段。

## 提议方案

实现 CombatSystem 单例，管理伤害计算、受伤反馈和打击感。

## 核心功能

- 伤害计算 (ATK-DEF 公式 + Luban 参数)
- 攻击判定 (近战判定帧/远程投射物碰撞)
- 受伤反馈 (击退/闪红/无敌帧)
- 打击感 (顿帧/震屏)
- 击杀/死亡流程
- Boss 多阶段切换

## 预期成果

- CombatSystem 单例
- 伤害公式可配
- 多层打击反馈

## 相关约束

- 依赖 player-controller, enemy-tick-skill-system, enemy-ai
- 依赖 event-bus, config-manager