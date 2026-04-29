# 输入管理器

## 问题陈述

需要统一管理玩家输入，支持键盘+手柄，支持按键绑定和改键。

## 提议方案

基于 Unity Input System 封装 InputManager 单例。

## 核心功能

- InputActionAsset 配置 (移动/跳跃/攻击/冲刺/交互/菜单)
- 行动映射查询接口
- 按键改键功能
- 键盘 + 手柄统一抽象

## 预期成果

- InputManager 单例
- InputActionAsset 配置
- 改键支持

## 相关约束

- 依赖 com.unity.inputsystem