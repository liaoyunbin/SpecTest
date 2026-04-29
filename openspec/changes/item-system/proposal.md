# 物品系统

## 问题陈述

游戏需要统一的物品拾取和类型分类系统。

## 提议方案

实现 Item 基类 + Trigger 拾取检测，类型由 Luban 配置表定义。

## 核心功能

- Trigger 拾取检测 (Collider2D)
- 类型分类 (消耗品/装备/能力/关键物品/货币)
- 拾取特效
- 拾取通知 (Toast)
- 投射物系统 (飞行轨迹/碰撞伤害/对象池)

## 物品类型

| 类型 | 说明 | 示例 |
|------|------|------|
| Consumable | 消耗品 (使用后消失) | 药水、食物 |
| Equipment | 装备 (可穿戴) | 武器、护甲、饰品 |
| Ability | 能力 (解锁新技能) | 二段跳、冲刺 |
| KeyItem | 关键物品 (剧情相关) | 钥匙、信件 |
| Currency | 货币 | 金币、稀有币 |

## 预期成果

- Item 基类
- 5 种物品类型
- 投射物系统 + 对象池

## 相关约束

- 依赖 config-manager (Luban 表查询物品属性)
- 依赖 event-bus (ItemCollected 事件)
- 依赖 ui-manager (拾取 Toast)