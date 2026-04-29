# 配置管理器

## 问题陈述

需要统一的配置数据管理，策划通过 Excel 维护表数据，自动导出为 C# 类型安全的查询接口。

## 提议方案

使用 Luban (Excel → C# 代码 + 数据文件)，实现 ConfigManager 加载和查询。

## Luban 配置表定义

| 表名 | 关键字段 | 用途 |
|------|----------|------|
| **TbItem** | id, name, type, icon, atk, def, price, desc | 物品/装备定义 |
| **TbSkill** | id, name, type, mpCost, unlockCondition, desc | 技能定义 |
| **TbEnemy** | id, name, hp, atk, def, exp, dropTable, prefab, aiType | 敌人定义 |
| **TbScene** | id, sceneName, displayName, hudId, bgmId, neighbors[] | 场景定义 |
| **TbLevel** | level, expNeed, hpGrow, mpGrow, atkGrow, defGrow | 等级数值曲线 |
| **TbQuest** | id, name, type, acceptNpc, condition[], reward[], desc | 任务定义 |
| **TbDrop** | id, enemyId, itemId, rate, minCount, maxCount | 掉落表 |
| **TbHUD** | id, prefabName, elements[] | HUD 配置 |
| **TbDialogue** | id, npcId, condition, lines[] | 对话内容 |

## 核心功能

- Luban 表数据加载
- 配置查询接口 (`Tables.Instance.TbXxx.Get(id)`)
- Editor 热重载
- 多语言文本表

## 预期成果

- ConfigManager 单例
- 9 个 Luban 表定义
- Editor 模式热重载

## 相关约束

- 依赖 Luban 工具链
- 配置表为只读，启动时全量加载常驻内存
- 存档数据不存入 Luban 表