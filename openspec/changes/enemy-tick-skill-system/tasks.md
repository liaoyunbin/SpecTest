# 敌人 Tick 驱动技能系统 —— 实现任务

## 第一阶段：核心框架

### 1. 创建目录结构和数据类
- [ ] 创建 `Assets/Scripts/Entities/Enemy/Skills/` 目录
- [ ] 创建 `SkillPhase.cs` — 5 阶段枚举
- [ ] 创建 `SkillConfig.cs` — 技能配置数据类

**AI 可生成**: 100%

### 2. 实现 EnemySkill 抽象基类
- [ ] 核心 Tick(float deltaTime) 方法
- [ ] 5 阶段状态机 (Idle/Windup/Active/Recovery/Cooldown)
- [ ] TransitionTo() 阶段切换逻辑
- [ ] PhaseElapsed + PhaseProgress 进度追踪
- [ ] 全部子类钩子（14 个虚方法）
- [ ] Activate() / Interrupt() / ForceStop() 公共方法

**AI 可生成**: 100%

### 3. 实现 SkillRunner 调度器
- [ ] 技能列表管理 (AddSkill / GetSkill)
- [ ] Tick(float deltaTime) 统一调度
- [ ] SelectBestSkill() — 按优先级+距离选择
- [ ] TryExecute() — 安全执行
- [ ] Interrupt() / ForceStopAll() — 控制方法

**AI 可生成**: 100%

### 4. 实现 SkillFactory
- [ ] 通过 SkillConfig.className 反射创建技能
- [ ] 或通过注册表映射创建

**AI 可生成**: 100%

---

## 第二阶段：具体技能实现

### 5. 实现 MeleeSlashSkill
- [ ] CanUse: 距离检测 + 地面检测
- [ ] Windup: 面向目标 + 播动画
- [ ] Active: 圆形范围伤害判定 (OverlapCircleNonAlloc)
- [ ] Recovery: 设置可招架状态
- [ ] Cooldown: 解除招架状态

**AI 可生成**: 100%

### 6. 实现 FireballSkill
- [ ] CanUse: 距离范围检测
- [ ] Windup: 蓄力动画
- [ ] Active: 从对象池获取投射物，计算方向并发射

**AI 可生成**: 100%

### 7. 实现 GroundSpikeSkill (Boss)
- [ ] Windup: 地面警告指示器 + 闪烁加速
- [ ] Active: 地刺升起 + 持续伤害判定
- [ ] Recovery: 地刺下降动画
- [ ] Cooldown: 回收对象

**AI 可生成**: 100%

---

## 第三阶段：Enemy 集成

### 8. 更新 Enemy 基类
- [ ] 集成 SkillRunner
- [ ] 集成 EnemyAI
- [ ] 在 Update 中统一 Tick
- [ ] 添加 isPaused 控制
- [ ] 添加 Pause() / Resume() 方法
- [ ] OnEnable 注册到 EnemyManager / OnDisable 注销

**AI 可生成**: 100%

### 9. 实现 EnemyManager
- [ ] activeEnemies 列表管理
- [ ] Register() / Unregister()
- [ ] PauseAll() / ResumeAll()
- [ ] PauseOffscreen() — 性能优化
- [ ] 全局事件监听 (自动暂停/恢复)

**AI 可生成**: 100%

---

## 第四阶段：暂停集成

### 10. 暂停场景集成
- [ ] 连接 EventBus 监听全局暂停事件
- [ ] 玩家打开 UI → PauseAll
- [ ] 玩家关闭 UI → ResumeAll
- [ ] 场景加载 → PauseAll
- [ ] 玩家死亡 → PauseAll
- [ ] Cutscene → PauseAll

**AI 可生成**: 80%

### 11. 慢动作支持
- [ ] Enemy 添加 timeScale 字段
- [ ] dt = Time.deltaTime × timeScale
- [ ] 可用于子弹时间效果

**AI 可生成**: 100%

---

## 第五阶段：测试验证

### 12. 创建测试敌人
- [ ] 近战测试怪 (挂载 MeleeSlashSkill)
- [ ] 远程测试怪 (挂载 FireballSkill)
- [ ] Boss 测试 (挂载所有技能)

**AI 可生成**: 100%

### 13. 验证功能
- [ ] 技能正常执行 (Windup→Active→Recovery→Cooldown→Idle)
- [ ] Pause/Resume 技能冻结/恢复
- [ ] Interrupt 打断功能
- [ ] 前摇进度查询 (PhaseProgress)
- [ ] 慢动作 (dt × 0.5)
- [ ] 批量暂停 (EnemyManager.PauseAll)
- [ ] 屏幕外暂停 (EnemyManager.PauseOffscreen)
- [ ] 对象池集成 (投射物/特效回收)

---

## 优先级

| 优先级 | 阶段 | 任务数 | 说明 |
|--------|------|--------|------|
| 🔴 P0 | 第一阶段 | 4 | 核心框架 |
| 🔴 P0 | 第二阶段 | 3 | 3 种技能实现 |
| 🟡 P1 | 第三阶段 | 2 | Enemy 集成 |
| 🟡 P1 | 第四阶段 | 2 | 暂停集成 |
| 🟢 P2 | 第五阶段 | 2 | 测试验证 |

---

## 依赖关系

```
本系统依赖 (需先实现):

Layer 1: Singleton 基类
Layer 1: ObjectPool (投射物 + 特效)
Layer 2: AudioManager (音效播放)
Layer 2: EventBus (暂停事件)

可选:
Layer 2: ConfigManager + Luban (SkillConfig 来源)
```

## AI 生成比例

| 阶段 | AI 可生成 | 必须手动 |
|------|----------|----------|
| 核心框架 | 100% | 0% |
| 技能实现 | 100% | 0% |
| Enemy 集成 | 100% | 0% |
| 暂停集成 | 80% | 20% (配置 EventBus 映射) |
| 测试验证 | 100% | 0% |
