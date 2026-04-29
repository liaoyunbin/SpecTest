# 任务系统

## 问题陈述

需要任务系统管理支线任务，驱动玩家探索。

## 提议方案

实现 QuestSystem 状态机 + 条件检测。

## 核心功能

- 任务状态机 (未接/进行中/完成)
- 条件检测 (击杀/收集/到达)
- 任务奖励发放
- 任务日志 (UI 面板)

## 预期成果

- QuestSystem
- 10+ 任务 (Luban 配置)
- 任务日志 UI

## 相关约束

- 依赖 event-bus, config-manager, ui-manager, save-system