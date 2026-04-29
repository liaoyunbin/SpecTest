# 敌人 Tick 驱动技能系统 —— 设计文档

## 整体架构

```
┌─────────────────────────────────────────────────────────────────┐
│              Tick 驱动技能系统                                    │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                     Enemy.Update()                      │    │
│  │                          │                              │    │
│  │            ┌─────────────┴─────────────┐               │    │
│  │            ▼                           ▼               │    │
│  │    ┌──────────────┐            ┌──────────────┐        │    │
│  │    │  SkillRunner │            │    EnemyAI   │        │    │
│  │    │  .Tick(dt)   │            │   .Tick(dt)  │        │    │
│  │    └──────┬───────┘            └──────┬───────┘        │    │
│  │           │                           │                 │    │
│  │           ▼                           ▼                 │    │
│  │   ┌──────────────┐           ┌──────────────┐          │    │
│  │   │ EnemySkill   │◄──────────│  选择技能    │          │    │
│  │   │ .Tick(dt)    │  执行     │              │          │    │
│  │   │              │           └──────────────┘          │    │
│  │   │ phase:       │                                      │    │
│  │   │ Windup→      │                                      │    │
│  │   │ Active→      │                                      │    │
│  │   │ Recovery→    │                                      │    │
│  │   │ Cooldown→    │                                      │    │
│  │   │ Idle         │                                      │    │
│  │   └──────────────┘                                      │    │
│  │                                                         │    │
│  │   暂停: if (isPaused) return;                           │    │
│  │                                                         │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 核心设计：deltaTime 传递规则

```
┌─────────────────────────────────────────────────────────────────┐
│              deltaTime 传递规则                                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  正常状态:                                                       │
│  ──────────                                                      │
│  dt = Time.deltaTime;      // ~0.016 @ 60fps                   │
│  SkillRunner.Tick(dt);                                         │
│  → phaseTimer 正常累积，技能正常播放                             │
│                                                                 │
│  暂停状态:                                                       │
│  ──────────                                                      │
│  dt = 0;                   // 时间冻结                          │
│  SkillRunner.Tick(dt);                                          │
│  → phaseTimer 不增长，技能冻结中途                               │
│                                                                 │
│  慢动作 (子弹时间):                                               │
│  ──────────                                                      │
│  dt = Time.deltaTime * 0.5f;  // 半速                          │
│  → phaseTimer 半速增长，技能慢放                                 │
│                                                                 │
│  加速 (调试):                                                     │
│  ──────────                                                      │
│  dt = Time.deltaTime * 2f;  // 双速                             │
│  → phaseTimer 双速增长，技能快放                                 │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 核心类定义

### EnemySkill 抽象基类

```csharp
public enum SkillPhase
{
    Idle,        // 空闲，等待 AI 调用
    Windup,      // 前摇（玩家反应窗口）
    Active,      // 执行中（伤害判定窗口）
    Recovery,    // 后摇（反击窗口）
    Cooldown     // 冷却中
}

public abstract class EnemySkill
{
    // ── 配置 ──
    public SkillConfig Config { get; private set; }
    
    // ── 运行时状态 ──
    public SkillPhase Phase { get; private set; } = SkillPhase.Idle;
    public float PhaseElapsed { get; private set; }
    public float PhaseProgress => Phase == SkillPhase.Idle ? 0 
        : Phase == SkillPhase.Cooldown ? cooldownRemaining / Config.cooldown
        : PhaseElapsed / CurrentPhaseDuration;
    
    private float cooldownRemaining;
    
    // ── 持有者 ──
    protected Enemy Owner { get; private set; }
    protected Transform Player => Owner.Target;
    
    // ── 便捷属性 ──
    public bool IsRunning  => Phase != SkillPhase.Idle && Phase != SkillPhase.Cooldown;
    public bool IsReady    => Phase == SkillPhase.Idle && cooldownRemaining <= 0;
    public bool IsPlaying  => Phase == SkillPhase.Active;
    public bool IsWindup   => Phase == SkillPhase.Windup;
    public bool IsRecovery => Phase == SkillPhase.Recovery;
    public bool IsCooling  => Phase == SkillPhase.Cooldown;
    
    private float CurrentPhaseDuration => Phase switch
    {
        SkillPhase.Windup   => Config.windupTime,
        SkillPhase.Active   => Config.activeTime,
        SkillPhase.Recovery => Config.recoveryTime,
        _ => 0
    };
    
    // ── 初始化 ──
    public void Initialize(Enemy owner, SkillConfig config)
    {
        Owner = owner;
        Config = config;
    }
    
    // ── 核心 Tick ──
    public void Tick(float deltaTime)
    {
        switch (Phase)
        {
            case SkillPhase.Idle:
                TickIdle(deltaTime);
                break;
            case SkillPhase.Windup:
                PhaseElapsed += deltaTime;
                OnWindupTick(PhaseElapsed);
                if (PhaseElapsed >= Config.windupTime) TransitionTo(SkillPhase.Active);
                break;
            case SkillPhase.Active:
                PhaseElapsed += deltaTime;
                OnActiveTick(PhaseElapsed);
                if (PhaseElapsed >= Config.activeTime) TransitionTo(SkillPhase.Recovery);
                break;
            case SkillPhase.Recovery:
                PhaseElapsed += deltaTime;
                OnRecoveryTick(PhaseElapsed);
                if (PhaseElapsed >= Config.recoveryTime) TransitionTo(SkillPhase.Cooldown);
                break;
            case SkillPhase.Cooldown:
                TickCooldown(deltaTime);
                break;
        }
    }
    
    private void TickIdle(float dt)
    {
        if (cooldownRemaining > 0)
        {
            cooldownRemaining -= dt;
            if (cooldownRemaining <= 0)
            {
                cooldownRemaining = 0;
                OnReady();
            }
        }
    }
    
    private void TickCooldown(float dt)
    {
        cooldownRemaining -= dt;
        if (cooldownRemaining <= 0)
        {
            cooldownRemaining = 0;
            TransitionTo(SkillPhase.Idle);
        }
    }
    
    // ── 阶段切换 ──
    private void TransitionTo(SkillPhase nextPhase)
    {
        switch (Phase)
        {
            case SkillPhase.Windup:   OnWindupEnd();   break;
            case SkillPhase.Active:   OnActiveEnd();   break;
            case SkillPhase.Recovery: OnRecoveryEnd();  break;
        }
        
        Phase = nextPhase;
        PhaseElapsed = 0;
        
        switch (nextPhase)
        {
            case SkillPhase.Active:   OnActiveStart();  break;
            case SkillPhase.Recovery: OnRecoveryStart(); break;
            case SkillPhase.Cooldown:
                cooldownRemaining = Config.cooldown;
                OnCooldownStart();
                break;
        }
    }
    
    // ── 公共方法 ──
    public bool CanUse() => IsReady && OnCanUse();
    
    public void Activate()
    {
        if (!IsReady) return;
        TransitionTo(SkillPhase.Windup);
        OnWindupStart();
    }
    
    public void Interrupt()
    {
        OnInterrupt();
        Phase = SkillPhase.Cooldown;
        PhaseElapsed = 0;
        cooldownRemaining = Config.cooldown * 0.5f;
        OnCooldownStart();
    }
    
    public void ForceStop()
    {
        Phase = SkillPhase.Idle;
        PhaseElapsed = 0;
        cooldownRemaining = Config.cooldown;
    }
    
    // ── 子类钩子（全部可选覆盖）─
    protected virtual bool OnCanUse() => true;
    protected virtual void OnReady() { }
    
    protected virtual void OnWindupStart() { }
    protected virtual void OnWindupTick(float elapsed) { }
    protected virtual void OnWindupEnd() { }
    
    protected virtual void OnActiveStart() { }
    protected virtual void OnActiveTick(float elapsed) { }
    protected virtual void OnActiveEnd() { }
    
    protected virtual void OnRecoveryStart() { }
    protected virtual void OnRecoveryTick(float elapsed) { }
    protected virtual void OnRecoveryEnd() { }
    
    protected virtual void OnCooldownStart() { }
    protected virtual void OnInterrupt() { }
}
```

### SkillRunner

```csharp
public class SkillRunner
{
    private readonly List<EnemySkill> skills = new();
    private readonly Enemy owner;
    
    public EnemySkill CurrentSkill { get; private set; }
    public bool IsBusy => CurrentSkill != null && CurrentSkill.IsRunning;
    
    public SkillRunner(Enemy owner) => this.owner = owner;
    
    public void AddSkill(EnemySkill skill)
    {
        skill.Initialize(owner, skill.Config);
        skills.Add(skill);
    }
    
    public EnemySkill GetSkill(string skillId)
        => skills.Find(s => s.Config.id == skillId);
    
    public void Tick(float deltaTime)
    {
        CurrentSkill?.Tick(deltaTime);
        
        foreach (var skill in skills)
        {
            if (skill != CurrentSkill)
                skill.Tick(deltaTime);
        }
        
        if (CurrentSkill != null && !CurrentSkill.IsRunning)
            CurrentSkill = null;
    }
    
    public EnemySkill SelectBestSkill()
    {
        return skills
            .Where(s => s.IsReady)
            .Where(s => s.CanUse())
            .OrderByDescending(s => s.Config.priority)
            .ThenBy(s => Vector2.Distance(
                owner.transform.position, owner.Target.position))
            .FirstOrDefault();
    }
    
    public bool TryExecute(EnemySkill skill)
    {
        if (IsBusy || skill == null || !skill.IsReady) return false;
        CurrentSkill = skill;
        CurrentSkill.Activate();
        return true;
    }
    
    public bool TryExecute(string skillId) => TryExecute(GetSkill(skillId));
    
    public void Interrupt()
    {
        CurrentSkill?.Interrupt();
        CurrentSkill = null;
    }
    
    public void ForceStopAll()
    {
        CurrentSkill?.ForceStop();
        CurrentSkill = null;
        foreach (var skill in skills)
            skill.ForceStop();
    }
}
```

---

## 具体技能实现示例

### 近战挥砍

```csharp
public class MeleeSlashSkill : EnemySkill
{
    private Collider2D[] hitBuffer = new Collider2D[8];
    private bool dealtDamage;
    
    protected override bool OnCanUse()
    {
        float dist = Vector2.Distance(Owner.transform.position, Player.position);
        return dist <= Config.range && Owner.IsGrounded && !Owner.IsStunned;
    }
    
    protected override void OnWindupStart()
    {
        dealtDamage = false;
        Owner.Animator.SetTrigger(Config.animTrigger);
        Owner.FaceTarget();
    }
    
    protected override void OnActiveStart()
    {
        var count = Physics2D.OverlapCircleNonAlloc(
            Owner.attackPoint.position, Config.range, hitBuffer, Owner.playerLayer);
        
        for (int i = 0; i < count; i++)
        {
            if (hitBuffer[i].TryGetComponent<IDamageable>(out var target))
            {
                target.TakeDamage(Config.damage);
                dealtDamage = true;
            }
        }
        
        if (dealtDamage)
        {
            ObjectPool.Get<SimpleVFX>(Config.vfxPrefab, Owner.attackPoint.position);
            AudioManager.Instance.PlaySFX(Config.sfxId);
        }
    }
    
    protected override void OnRecoveryStart() => Owner.SetParryable(true);
    protected override void OnCooldownStart() => Owner.SetParryable(false);
    protected override void OnInterrupt() => Owner.SetParryable(false);
}
```

### 火球投射

```csharp
public class FireballSkill : EnemySkill
{
    protected override bool OnCanUse()
    {
        float dist = Vector2.Distance(Owner.transform.position, Player.position);
        return dist >= Config.minRange && dist <= Config.maxRange;
    }
    
    protected override void OnWindupStart()
    {
        Owner.Animator.SetTrigger("Cast");
        Owner.FaceTarget();
    }
    
    protected override void OnActiveStart()
    {
        var direction = (Player.position - Owner.firePoint.position).normalized;
        var projectile = ObjectPool.Get<Projectile>(Config.projectilePrefab);
        projectile.transform.position = Owner.firePoint.position;
        projectile.Launch(direction, Config.speed, Config.damage, Owner);
    }
}
```

### 范围地刺（Boss 技能）

```csharp
public class GroundSpikeSkill : EnemySkill
{
    private GameObject warningIndicator;
    private GameObject spikeInstance;
    
    protected override void OnWindupStart()
    {
        warningIndicator = ObjectPool.Get(Config.warningPrefab, Player.position);
    }
    
    protected override void OnWindupTick(float elapsed)
    {
        float flashRate = Mathf.Lerp(0.5f, 0.1f, elapsed / Config.windupTime);
        warningIndicator.SetActive(
            Mathf.PingPong(Time.unscaledTime * (1f / flashRate), 1f) > 0.5f);
    }
    
    protected override void OnActiveStart()
    {
        ObjectPool.Release(warningIndicator);
        spikeInstance = ObjectPool.Get(Config.spikePrefab, 
            warningIndicator.transform.position);
    }
    
    protected override void OnRecoveryTick(float elapsed)
    {
        spikeInstance.transform.localScale = Vector3.Lerp(
            Vector3.one, Vector3.zero, elapsed / Config.recoveryTime);
    }
    
    protected override void OnCooldownStart()
    {
        ObjectPool.Release(spikeInstance);
        spikeInstance = null;
    }
}
```

---

## Enemy 集成

```csharp
public class Enemy : MonoBehaviour, IDamageable
{
    [SerializeField] private SkillConfig[] initialSkills;
    
    public SkillRunner SkillRunner { get; private set; }
    public EnemyAI AI { get; private set; }
    public Transform Target { get; set; }
    public bool IsStunned { get; private set; }
    public bool IsGrounded { get; private set; }
    
    private bool isPaused;
    
    private void Awake()
    {
        SkillRunner = new SkillRunner(this);
        AI = new EnemyAI(this);
        
        foreach (var config in initialSkills)
        {
            var skill = SkillFactory.Create(config);
            skill.Initialize(this, config);
            SkillRunner.AddSkill(skill);
        }
    }
    
    private void Update()
    {
        if (isPaused) return;
        if (IsDead) return;
        
        float dt = Time.deltaTime;
        
        SkillRunner.Tick(dt);
        AI.Tick(dt);
        
        if (!SkillRunner.IsBusy)
        {
            var skill = SkillRunner.SelectBestSkill();
            SkillRunner.TryExecute(skill);
        }
    }
    
    public void Pause()  => isPaused = true;
    public void Resume() => isPaused = false;
}
```

---

## 批量暂停管理

```csharp
public class EnemyManager : Singleton<EnemyManager>
{
    private List<Enemy> activeEnemies = new();
    private bool globallyPaused;
    
    public void Register(Enemy enemy) => activeEnemies.Add(enemy);
    public void Unregister(Enemy enemy) => activeEnemies.Remove(enemy);
    
    public void PauseAll()
    {
        globallyPaused = true;
        foreach (var enemy in activeEnemies)
            enemy.Pause();
    }
    
    public void ResumeAll()
    {
        globallyPaused = false;
        foreach (var enemy in activeEnemies)
            enemy.Resume();
    }
    
    public void PauseOffscreen()
    {
        var camera = Camera.main;
        foreach (var enemy in activeEnemies)
        {
            var viewport = camera.WorldToViewportPoint(enemy.transform.position);
            if (viewport.x < -0.5f || viewport.x > 1.5f || 
                viewport.y < -0.5f || viewport.y > 1.5f)
                enemy.Pause();
        }
    }
    
    public bool IsGloballyPaused => globallyPaused;
}
```

---

## 暂停触发场景

```
┌─────────────────────────────────────────────────────────────────┐
│              暂停触发场景                                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  场景                      触发                    恢复          │
│  ────────────────────────────────────────────────────────────── │
│  玩家打开背包/地图          面板Open             面板Close       │
│  NPC 对话中                 对话Start             对话End        │
│  Cutscene 播放              Cutscene.Start        Cutscene.End  │
│  Boss 出场/转阶段           Boss.Cutscene         动画结束       │
│  玩家死亡                   Player.OnDeath         复活          │
│  场景加载中                 GameManager.Load      加载完成       │
│  获得新能力演出              Ability演出           演出结束       │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 完整调用链

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│  foreach Enemy (if !paused && !dead)                            │
│    │                                                            │
│    ├── dt = Time.deltaTime × timeScale                          │
│    │                                                            │
│    ├── SkillRunner.Tick(dt)                                     │
│    │     ├── CurrentSkill.Tick(dt)                              │
│    │     │     ├── Windup:  累积→钩子→够了→Active              │
│    │     │     ├── Active:  累积→钩子→够了→Recovery            │
│    │     │     ├── Recovery:累积→钩子→够了→Cooldown             │
│    │     │     └── Cooldown:倒计时→够了→Idle                   │
│    │     └── 其他技能.Tick(dt)  ← 冷却倒计时                    │
│    │                                                            │
│    └── AI.Tick(dt)                                              │
│          └── if !IsBusy → SelectBest → TryExecute              │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 目录结构

```
Assets/Scripts/Entities/Enemy/
├── Enemy.cs                     ← 集成 SkillRunner + 暂停
├── SkillRunner.cs               ← 技能调度器
├── SkillFactory.cs              ← 技能工厂（反射/查表创建）
├── Skills/
│   ├── EnemySkill.cs            ← 抽象基类 (Tick驱动)
│   ├── MeleeSlashSkill.cs       ← 近战
│   ├── FireballSkill.cs         ← 投射物
│   └── GroundSpikeSkill.cs      ← Boss地刺
├── Data/
│   ├── SkillConfig.cs           ← 技能配置数据类
│   └── SkillPhase.cs            ← 阶段枚举
└── Manager/
    └── EnemyManager.cs          ← 批量暂停管理
```

---

## 与 metroidvania-with-ai 架构的关系

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│  本系统属于 Layer 3 (实体层)，对应 Architecture.md 中的:         │
│  ────────────────────────────────────────────                   │
│  Enemy 实体的技能子模块                                          │
│                                                                 │
│  依赖:                                                          │
│  ──────                                                          │
│  Layer 1: 对象池 (投射物复用)                                   │
│  Layer 2: AudioManager (音效), EventBus (事件)                  │
│                                                                 │
│  Config 来源:                                                    │
│  ─────────────                                                   │
│  数值参数 → Luban TbSkillConfig                                 │
│  表现资源 → ScriptableObject (Boss专属)                         │
│                                                                 │
│  EnemyManager 暂停触发:                                          │
│  ────────────────────────                                        │
│  通过 EventBus 监听全局事件 (PlayerDied/SceneLoadStart等)        │
│  或由 GameManager / UIManager 直接调用                           │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```
