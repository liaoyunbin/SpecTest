# 敌人 Tick 驱动技能系统

## 问题陈述

现有的协程驱动技能方案使用 `WaitForSeconds` 管理技能阶段，存在以下问题：
- 无法在技能执行中暂停（`WaitForSeconds` 不受自定义暂停控制）
- 使用 `Time.timeScale` 暂停会连带影响 UI
- 无法查询技能前摇进度（无法显示 Boss 前摇进度条）
- 技能阶段分散在协程中，调试困难
- 无法实现子弹时间/慢动作效果

## 提议方案

用 **Tick 驱动 + 手动累积 deltaTime** 替代协程方案：

- 技能基类 `EnemySkill` 用 `Tick(float deltaTime)` 驱动
- 技能分为 5 个阶段：Windup → Active → Recovery → Cooldown → Idle
- 暂停 = 不传 deltaTime（或传入 0）
- 慢动作 = deltaTime × factor
- 所有阶段进度可通过 `PhaseProgress`（0→1）查询

核心技术点：
- **组件化技能**：每个技能是独立组件，通过 `SkillRunner` 调度
- **阶段钩子**：每个阶段有 `OnXxxStart` / `OnXxxTick(elapsed)` / `OnXxxEnd` 钩子
- **暂停机制**：`isPaused ? 跳过整个 Update : 正常 Tick`
- **批量暂停**：`EnemyManager.PauseAll()` / `ResumeAll()`

## 预期成果

- `EnemySkill` 抽象基类（Tick 驱动，5 阶段状态机）
- `SkillRunner` 技能调度器
- 3 种示例技能实现（近战/远程/Boss地刺）
- `EnemyManager` 批量暂停管理
- 完整暂停/恢复/慢动作支持

## 非目标

- 不包含具体敌人 AI 实现（属于 Enemy 系统）
- 不包含 SkillConfig 的 Luban 表配置（属于 metroidvania-with-ai）
- 不包含特效/音效具体实现

## 相关约束

- 基于 Unity + C# 技术栈
- 技能不依赖协程（纯 Update 驱动）
- Config 数据通过 SkillConfig 注入（来源 Luban 表或 SO）
- 与 metroidvania-with-ai 架构兼容
