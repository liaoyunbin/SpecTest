# 实现任务（总索引）

> **每个子系统的详细任务已拆分到对应的子 Change 中。**
> 本文件仅列出环境搭建、基础层、表现层和跨系统集成任务。
> 各子系统具体实现 → 进入对应子 Change 的 tasks.md。

## 📁 子 Change 任务索引

| 子 Change | Layer | 状态 | AI% |
|-----------|-------|------|-----|
| [ui-manager](../ui-manager/) | 2 | ✅ ready | 100% |
| [enemy-tick-skill-system](../enemy-tick-skill-system/) | 3 | 📋 draft | 100% |
| [event-bus](../event-bus/) | 2 | 📋 draft | 100% |
| [input-manager](../input-manager/) | 2 | 📋 draft | 70% |
| [config-manager](../config-manager/) | 2 | 📋 draft | 60% |
| [audio-manager](../audio-manager/) | 2 | 📋 draft | 90% |
| [save-system](../save-system/) | 2 | 📋 draft | 100% |
| [scene-manager](../scene-manager/) | 2 | 📋 draft | 100% |
| [player-controller](../player-controller/) | 3 | 📋 draft | 80-100% |
| [enemy-ai](../enemy-ai/) | 3 | 📋 draft | 100% |
| [item-system](../item-system/) | 3 | 📋 draft | 100% |
| [npc-system](../npc-system/) | 3 | 📋 draft | 100% |
| [combat-system](../combat-system/) | 4 | 📋 draft | 100% |
| [exploration-system](../exploration-system/) | 4 | 📋 draft | 80% |
| [growth-system](../growth-system/) | 4 | 📋 draft | 100% |
| [quest-system](../quest-system/) | 4 | 📋 draft | 100% |

---

## 第一阶段：环境搭建

### 1. 项目创建与目录初始化
- [ ] 创建 Unity 2D URP 项目
- [ ] 按 Architecture.md 创建完整目录结构
- [ ] 安装依赖包 (`newtonsoft-json`, `inputsystem`, `cinemachine`)
- [ ] 配置 Luban 工具链
- [ ] 创建 Persistent Scene 和 2 个测试 Additive Scene

**AI 可生成**: 0%（手动配置 Unity）

---

## 第二阶段：Layer 1 — 基础层

### 2. Singleton 基类
- [ ] `MonoSingleton<T>` + `PersistentSingleton<T>`

### 3. 工具集
- [ ] `Extensions.cs` + `MathUtil.cs`

### 4. 对象池
- [ ] `ObjectPool.cs` (Prewarm + Shrink + 泛型 Get)

### 5. 资源加载器
- [ ] `ResourceLoader` (异步加载 + 缓存 + 进度回调)

**AI 可生成**: 100%

---

## 第三阶段：Layer 6 — 表现层（跨系统）

### 24. UI 面板实现
> 所有 Panel 基于 UIManager 框架。具体 UI 面板见 [ui-manager](../ui-manager/tasks.md)
- [ ] MainMenu + GameHUD + 背包/装备/地图/技能/任务/商店/设置/对话/存档面板
- [ ] Toast 提示系统

### 25. 动画集成
- [ ] Animator Controller (Player + Enemy)
- [ ] 动画参数切换 + 动画事件

### 26. 相机配置
- [ ] Cinemachine 跟随 + 区域限制 + 震屏

### 27. 特效集成
- [ ] 攻击/受击/拾取/Boss/环境特效 + 屏幕后处理

---

## 第四阶段：内容填充

### 28. Luban 配置表填充
> 表结构定义见 [config-manager](../config-manager/)
- [ ] 9 个表: 物品/技能/敌人/场景/等级/任务/掉落/HUD/对话

### 29. 关卡搭建
> 场景方案见 [scene-manager](../scene-manager/)
- [ ] Forest/Cave 区域 Tilemap + 敌人/NPC/道具/物件 + 边界 + SpawnPoint

---

## 第五阶段：打磨

### 30. 手感调优
> Player 参数见 [player-controller](../player-controller/)
- [ ] 移动/跳跃/战斗参数 (必须手动测试调优)

### 31. 性能优化
- [ ] 对象池监控 + 场景预加载 + UI 缓存 + GC 检查

---

## 优先级汇总

| 优先级 | 阶段 | 说明 |
|--------|------|------|
| 🔴 P0 | 第一阶段 + 第二阶段 | 环境 + 基础层 |
| 🔴 P0 | Layer 2 所有子 Change | 系统层，运行时基础 |
| 🟡 P1 | Layer 3 所有子 Change | 实体层，游戏内容基础 |
| 🟡 P1 | Layer 4 所有子 Change | 玩法层，核心游戏性 |
| 🟢 P2 | 第三阶段 | 表现层，视觉打磨 |
| 🟢 P3 | 第四阶段 + 第五阶段 | 内容 + 打磨 |

---

## 最小可玩版本 (MVP)

| 任务 | 内容 |
|------|------|
| 1-5 | 基础层 (环境 + Layer 1) |
| 6-12 | 系统层 (EventBus → SceneManager) |
| 13 | Player 基础移动/跳跃 ([player-controller](../player-controller/)) |
| 15 | 2-3 种基础敌人 ([enemy-tick-skill-system](../enemy-tick-skill-system/) + [enemy-ai](../enemy-ai/)) |
| 16 | 物品拾取 ([item-system](../item-system/)) |
| 20 | 基础战斗 ([combat-system](../combat-system/)) |
| 24 | GameHUD + MainMenu |
| 26 | 基础相机跟随 |
| 29 | 1 个测试关卡 |
