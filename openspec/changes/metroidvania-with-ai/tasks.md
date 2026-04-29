# 实现任务

> 任务按 Layer 依赖顺序排列，从底向上。每个任务标注 AI 可生成的比例。
> 
> **📁 子 Change 索引**: 每个系统对应一个独立子 Change，带有完整 proposal/design/tasks。

| 子 Change | Layer | 任务 | 状态 |
|-----------|-------|------|------|
| [ui-manager](../ui-manager/) | 2 | UI 面板管理 | ✅ ready |
| [enemy-tick-skill-system](../enemy-tick-skill-system/) | 3 | 敌人技能 | 📋 draft |
| [scene-manager](../scene-manager/) | 2 | 场景管理 | ⏳ 待设计 |
| [save-system](../save-system/) | 2 | 存档管理 | ⏳ 待设计 |
| [input-manager](../input-manager/) | 2 | 输入管理 | ⏳ 待设计 |
| [event-bus](../event-bus/) | 2 | 事件总线 | ⏳ 待设计 |
| [audio-manager](../audio-manager/) | 2 | 音频管理 | ⏳ 待设计 |
| [config-manager](../config-manager/) | 2 | 配置管理 | ⏳ 待设计 |
| [player-controller](../player-controller/) | 3 | 角色控制 | ⏳ 待设计 |
| [enemy-ai](../enemy-ai/) | 3 | 敌人 AI | ⏳ 待设计 |
| [item-system](../item-system/) | 3 | 物品系统 | ⏳ 待设计 |
| [npc-system](../npc-system/) | 3 | NPC 系统 | ⏳ 待设计 |
| [combat-system](../combat-system/) | 4 | 战斗系统 | ⏳ 待设计 |
| [exploration-system](../exploration-system/) | 4 | 探索系统 | ⏳ 待设计 |
| [growth-system](../growth-system/) | 4 | 成长系统 | ⏳ 待设计 |
| [quest-system](../quest-system/) | 4 | 任务系统 | ⏳ 待设计 |

## 第一阶段：环境搭建

### 1. 项目创建与目录初始化
- [ ] 创建 Unity 2D URP 项目
- [ ] 按 Architecture.md 创建完整目录结构
- [ ] 安装依赖包:
  - [ ] `com.unity.nuget.newtonsoft-json` (存档)
  - [ ] `com.unity.inputsystem` (输入)
  - [ ] `com.unity.cinemachine` (相机)
- [ ] 配置 Luban 工具链
- [ ] 创建 Persistent Scene 和 2 个测试 Additive Scene

**AI 可生成**: 0%（手动配置 Unity）

---

## 第二阶段：Layer 1 — 基础层

### 2. Singleton 基类
- [ ] 实现 `MonoSingleton<T>` — 普通单例
- [ ] 实现 `PersistentSingleton<T>` — DontDestroyOnLoad 持久单例

**AI 可生成**: 100%

### 3. 工具集
- [ ] 实现 `Extensions.cs` — Transform/GameObject/Vector 扩展方法
- [ ] 实现 `MathUtil.cs` — 常用数学工具

**AI 可生成**: 100%

### 4. 对象池
- [ ] 实现 `ObjectPool.cs` — 通用对象池
- [ ] 支持 `Prewarm(count)` — 预热
- [ ] 支持 `Shrink()` — 定时缩容
- [ ] 支持泛型 `Get<T>(prefab)` — 类型安全获取

**AI 可生成**: 100%

### 5. 资源加载器
- [ ] 实现 `ResourceLoader` — 异步加载预制体
- [ ] 实现资源缓存池
- [ ] 实现 `LoadAsync<T>(path)` + 进度回调

**AI 可生成**: 100%

---

## 第三阶段：Layer 2 — 系统层

### 6. EventBus
- [ ] 实现全局事件发布/订阅
- [ ] 实现 `Publish(eventName, data)`
- [ ] 实现 `Subscribe(eventName, handler)` / `Unsubscribe`
- [ ] 实现 Debug 模式事件日志

**AI 可生成**: 100%

### 7. InputManager
- [ ] 配置 InputActionAsset (移动/跳跃/攻击/冲刺/交互/菜单)
- [ ] 实现行动映射查询接口
- [ ] 实现按键改键功能
- [ ] 支持键盘 + 手柄统一抽象

**AI 可生成**: 70%（AI 生成代码 + 你在 Unity 配置 InputAction）

### 8. ConfigManager + Luban 接入
- [ ] 定义所有 Luban 表结构 (Excel 模板)
- [ ] 配置 Luban 导出规则
- [ ] 实现 ConfigManager 加载 Luban 数据
- [ ] 实现 Editor 热重载

**AI 可生成**: 60%（AI 生成代码 + 你配 Luban + 填 Excel）

### 9. AudioManager
- [ ] 实现 BGM 播放/切换/淡入淡出
- [ ] 实现 SFX 2D/3D 播放
- [ ] 实现音量分组控制 (Master/BGM/SFX)
- [ ] 实现音频对象池

**AI 可生成**: 90%

### 10. SaveManager
- [ ] 实现 `SaveData` 数据结构
- [ ] 实现 `Vector2Converter` (Newtonsoft.Json)
- [ ] 实现多槽位存档 (`Save(slot, data)` / `Load(slot)`)
- [ ] 实现存档版本校验
- [ ] 实现自动存档触发逻辑

**AI 可生成**: 100%

### 11. UIManager
- [ ] 实现 `Panel.cs` — 面板基类
- [ ] 实现 `Controller.cs` + `Controller<TPanel>` — 控制器基类
- [ ] 实现 `PanelControllerRegistry` — 注册表
- [ ] 实现 `UIManager` — 面板栈管理 (Base/Normal/Dialog/Toast)
- [ ] 实现面板缓存策略
- [ ] 实现加载锁定 (isLoading)
- [ ] 实现加载遮罩 (LoadingMask)
- [ ] 注册面板: PanelControllerRegistry.Register<HUD, HUDController>()

**AI 可生成**: 100%（完整设计已有）

### 12. GameManager（场景管理）
- [ ] 实现多场景加载/卸载 (Additive)
- [ ] 实现场景配置读取 (Luban → SceneConfig)
- [ ] 实现场景切换 + 进度事件
- [ ] 实现场景切换 UI 联动 (关闭弹窗/普通面板，替换HUD)
- [ ] 实现玩家出生点/SpawnPoint 管理
- [ ] 实现相邻场景预加载

**AI 可生成**: 100%

---

## 第四阶段：Layer 3 — 实体层

### 13. Player 角色控制器
- [ ] 配置 Rigidbody2D + Collider2D
- [ ] 实现地面检测
- [ ] 实现水平移动 + 加速/减速
- [ ] 实现跳跃 (可变高度) + 土狼时间 + 输入缓冲
- [ ] 实现角色属性 (HP/MP/ATK/DEF)
- [ ] 配置 Animator + 动画状态机
- [ ] 实现角色受伤/死亡逻辑

**AI 可生成**: 80%（核心逻辑 AI 生成，手感调参必须手动）

### 14. Player 能力系统
- [ ] 实现能力基类 `PlayerAbility`
- [ ] 实现 Dash (冲刺)
- [ ] 实现 WallSlide + WallJump (攀墙)
- [ ] 实现 DoubleJump (二段跳)
- [ ] 实现能力解锁/切换管理
- [ ] 实现能力槽 UI 数据 (供 HUD 显示)

**AI 可生成**: 100%

### 15. Enemy 基类 + AI + 技能系统
> **技能系统详见独立变更**: [enemy-tick-skill-system](../enemy-tick-skill-system/design.md)
- [ ] 实现 `Enemy` 基类 (HP/ATK/DEF/掉落、暂停控制、SkillRunner 集成)
- [ ] 实现 `EnemyAI` 状态机 (Patrol/Chase/Attack/Death、技能选择)
- [ ] 实现 `EnemySkill` 抽象基类 (Tick 驱动、5阶段状态机)
- [ ] 实现 `SkillRunner` 技能调度器
- [ ] 实现 3 种示例技能 (MeleeSlash / Fireball / GroundSpike)
- [ ] 实现 `EnemyManager` 批量暂停管理
- [ ] 实现视野检测/追击范围
- [ ] 实现受伤/死亡逻辑
- [ ] 实现掉落物生成

**AI 可生成**: 100%

### 16. Item 拾取系统
- [ ] 实现 `Item` 基类 + Trigger 检测
- [ ] 实现类型分类 (消耗品/装备/能力/关键物品/货币)
- [ ] 实现拾取特效
- [ ] 实现拾取通知 (Toast)

**AI 可生成**: 100%

### 17. Projectile 投射物
- [ ] 实现飞行轨迹
- [ ] 实现碰撞伤害判定
- [ ] 实现生命周期管理
- [ ] 集成对象池

**AI 可生成**: 100%

### 18. InteractObj 场景物件
- [ ] 实现存档点
- [ ] 实现传送门
- [ ] 实现能力门 (检测能力是否解锁)
- [ ] 实现可破坏物
- [ ] 实现开关/机关

**AI 可生成**: 100%

### 19. NPC 系统
- [ ] 实现 NPC 交互触发
- [ ] 实现对话树系统
- [ ] 实现商店交互
- [ ] 实现任务触发/完成

**AI 可生成**: 100%

---

## 第五阶段：Layer 4 — 玩法层

### 20. CombatSystem 战斗系统
- [ ] 实现伤害计算 (ATK-DEF 公式 + Luban 参数)
- [ ] 实现攻击判定 (近战判定帧/远程投射物)
- [ ] 实现受伤反馈 (击退/闪红/无敌帧)
- [ ] 实现打击感 (顿帧/震屏)
- [ ] 实现击杀/死亡流程
- [ ] 实现 Boss 多阶段切换

**AI 可生成**: 100%

### 21. ExploreSystem 探索系统
- [ ] 实现小地图实时渲染
- [ ] 实现区域探索度计算
- [ ] 实现能力门控检测
- [ ] 实现捷径解锁
- [ ] 实现隐藏区域检测

**AI 可生成**: 80%

### 22. GrowthSystem 成长系统
- [ ] 实现经验/等级 (Luban 曲线查询)
- [ ] 实现装备槽位 + 属性加成
- [ ] 实现背包物品管理 (排序/分类/使用)
- [ ] 实现碎片收集 + 上限提升

**AI 可生成**: 100%

### 23. QuestSystem 任务系统
- [ ] 实现任务状态机 (未接/进行中/完成)
- [ ] 实现条件检测 (击杀/收集/到达)
- [ ] 实现任务奖励发放
- [ ] 实现任务日志

**AI 可生成**: 100%

---

## 第六阶段：Layer 5 — 表现层

### 24. UI 面板实现
- [ ] 实现 MainMenu (底层)
- [ ] 实现 GameHUD — 血条/蓝条/货币/小地图/能力图标 (底层)
- [ ] 实现 InventoryPanel — 背包 (普通层)
- [ ] 实现 EquipmentPanel — 装备 (普通层)
- [ ] 实现 MapPanel — 大地图 (普通层)
- [ ] 实现 SkillPanel — 技能 (普通层)
- [ ] 实现 QuestPanel — 任务日志 (普通层)
- [ ] 实现 SettingsPanel — 设置 (普通层)
- [ ] 实现 ShopPanel — 商店 (普通层)
- [ ] 实现 ConfirmDialog — 确认框 (弹窗层)
- [ ] 实现 SaveLoadPanel — 存档/读档 (弹窗层)
- [ ] 实现 DialogPanel — NPC 对话 (弹窗层)
- [ ] 实现 ItemDetailPanel — 物品详情 (弹窗层)
- [ ] 实现 Toast 提示 (提示层)

**AI 可生成**: Panel代码100%，Controller代码90%，Unity布局0%

### 25. 动画集成
- [ ] 创建 Animator Controller (Player + Enemy)
- [ ] 实现动画参数切换代码
- [ ] 实现动画事件 (判定帧/音效时机)
- [ ] 实现场景切换过渡动画

**AI 可生成**: 代码100%，Animator Controller 0%（手动配置）

### 26. 相机配置
- [ ] 配置 Cinemachine Virtual Camera 平滑跟随
- [ ] 配置相机区域限制 (Confiner)
- [ ] 实现房间锁定相机 (Boss 房)
- [ ] 实现相机震动接口

**AI 可生成**: 代码80%，Cinemachine 配置 0%（手动配置）

### 27. 特效集成
- [ ] 攻击/受击/拾取特效
- [ ] Boss 技能特效
- [ ] 环境特效
- [ ] 屏幕后处理 (受伤闪屏)

**AI 可生成**: 代码30%，特效制作0%（需要美术资产）

---

## 第七阶段：内容填充

### 28. Luban 配置表填充
- [ ] 物品表 (20+ 物品)
- [ ] 技能表 (全部能力定义)
- [ ] 敌人表 (10+ 敌人类型)
- [ ] 场景表 (全部场景配置)
- [ ] 等级表 (1-50 级曲线)
- [ ] 任务表 (10+ 任务)
- [ ] 掉落表
- [ ] 对话表

**AI 可生成**: 70%（生成初始数据，你调整平衡）

### 29. 关卡搭建
- [ ] 搭建 Forest 区域 (2-3 个子场景)
- [ ] 搭建 Cave 区域 (2-3 个子场景)
- [ ] 放置敌人/NPC/道具/场景物件
- [ ] 配置场景边界触发器
- [ ] 配置 SpawnPoint

**AI 可生成**: 10%（主要在 Unity 手动操作）

---

## 第八阶段：打磨

### 30. 手感调优
- [ ] 角色移动参数 (速度/加速度/摩擦力)
- [ ] 跳跃参数 (高度/滞空/土狼时间/输入缓冲)
- [ ] 战斗手感 (顿帧时长/击退距离/无敌帧时长)

**AI 可生成**: 10%（建议参数，实际必须手动测试调优）

### 31. 性能优化
- [ ] 对象池预热 + 监控
- [ ] 场景预加载优化
- [ ] UI 缓存命中率验证
- [ ] GC Alloc 检查与修复

**AI 可生成**: 70%

---

## 优先级汇总

| 优先级 | 阶段 | 任务数 | 说明 |
|--------|------|--------|------|
| 🔴 P0 | 第一阶段 | 1 | 项目跑起来 |
| 🔴 P0 | 第二阶段 | 4 | 基础层，所有上层依赖 |
| 🔴 P0 | 第三阶段 | 7 | 系统层，运行时基础 |
| 🟡 P1 | 第四阶段 | 7 | 实体层，游戏内容基础 |
| 🟡 P1 | 第五阶段 | 4 | 玩法层，核心游戏性 |
| 🟢 P2 | 第六阶段 | 4 | 表现层，视觉打磨 |
| 🟢 P3 | 第七阶段 | 2 | 内容填充 |
| 🟢 P3 | 第八阶段 | 2 | 打磨优化 |

---

## AI 生成比例估算

| 层级 | AI 可生成 | 必须手动 |
|------|----------|----------|
| Layer 1 基础层 | ~95% | 5% |
| Layer 2 系统层 | ~85% | 15% (Unity 配置) |
| Layer 3 实体层 | ~85% | 15% (手感调参+配置) |
| Layer 4 玩法层 | ~90% | 10% |
| Layer 5 表现层 | ~45% | 55% (布局+动画+特效) |
| 内容填充 | ~40% | 60% (画地图+配表) |

---

## 最小可玩版本 (MVP)

仅包含以下任务即可跑通核心循环：

| 任务 | 内容 |
|------|------|
| 1-12 | 基础层 + 系统层 (必须全部完成) |
| 13 | Player 基础移动/跳跃 (不含高级能力) |
| 15 | 2-3 种基础敌人 (含 Tick 驱动技能系统，详见 [enemy-tick-skill-system](../enemy-tick-skill-system/design.md)) |
| 16 | 物品拾取 |
| 20 | 基础战斗 (近战攻击 + 伤害计算) |
| 24 | GameHUD + MainMenu |
| 26 | 基础相机跟随 |
| 29 | 1 个测试关卡 |

**MVP 周期**: 约 20 个核心任务
