# 通用角色技能系统 —— 设计文档

> 从 `enemy-tick-skill-system` 抽离通用部分，供玩家技能和怪物技能共享。

## 整体架构

```
┌─────────────────────────────────────────────────────────────────┐
│              Character Skill System                              │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │                  CharacterSkill                        │   │
│   │                                                        │   │
│   │  SkillPhase: Windup→Active→Recovery→Cooldown→Idle      │   │
│   │  PhaseElapsed / PhaseProgress                          │   │
│   │  cooldownRemaining                                     │   │
│   │  CanUse() / Activate() / Interrupt() / ForceStop()     │   │
│   │  Tick(float deltaTime)                                 │   │
│   └──────────────┬──────────────────┬──────────────────────┘   │
│                  │                  │                           │
│                  ▼                  ▼                           │
│   ┌──────────────────────┐ ┌──────────────────────┐           │
│   │   EnemySkill         │ │   PlayerSkill        │           │
│   │                      │ │                      │           │
│   │ CanUse: AI判断       │ │ CanUse: 按键+MP+解锁  │           │
│   │ + 距离/视野检测      │ │ + 冷却就绪            │           │
│   └──────────────────────┘ └──────────────────────┘           │
│                                                                 │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │  SkillRunner .Tick(dt)                                  │   │
│   │  - CurrentSkill.Tick(dt)                                │   │
│   │  - 其他技能.Tick(dt)  ← 冷却倒计时                       │   │
│   │  - SelectBestSkill / TryExecute                         │   │
│   └─────────────────────────────────────────────────────────┘   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## deltaTime 传递规则

```
正常: dt = Time.deltaTime;        → 技能正常播放
暂停: dt = 0;                      → PhaseElapsed 不增长
慢动作: dt = Time.deltaTime × 0.5; → 技能半速播放
加速: dt = Time.deltaTime × 2.0;   → 技能快播(调试用)
```

---

## 核心类

### ISkillOwner 接口

```csharp
public interface ISkillOwner
{
    Transform transform { get; }
    Transform Target { get; }
    bool IsGrounded { get; }
    bool IsStunned { get; }
    bool IsDead { get; }
    BaseAttributes Attributes { get; }
    Animator Animator { get; }
    void FaceTarget();
    Transform attackPoint { get; }
    Transform firePoint { get; }
    LayerMask targetLayer { get; }
}
```

### SkillPhase 枚举

```csharp
public enum SkillPhase
{
    Idle,        // 空闲
    Windup,      // 前摇（反应窗口）
    Active,      // 执行中（判定窗口）
    Recovery,    // 后摇（反击窗口）
    Cooldown     // 冷却中
}
```

### CharacterSkill 基类

```csharp
public abstract class CharacterSkill
{
    public SkillConfig Config { get; private set; }
    public SkillPhase Phase { get; private set; } = SkillPhase.Idle;
    public float PhaseElapsed { get; private set; }
    public float PhaseProgress => Phase switch
    {
        SkillPhase.Idle or SkillPhase.Cooldown => 0,
        _ => PhaseElapsed / CurrentPhaseDuration
    };

    protected ISkillOwner Owner { get; private set; }
    private float cooldownRemaining;

    public bool IsRunning  => Phase != SkillPhase.Idle && Phase != SkillPhase.Cooldown;
    public bool IsReady    => Phase == SkillPhase.Idle && cooldownRemaining <= 0;
    public bool IsPlaying  => Phase == SkillPhase.Active;
    public bool IsWindup   => Phase == SkillPhase.Windup;
    public bool IsRecovery => Phase == SkillPhase.Recovery;

    private float CurrentPhaseDuration => Phase switch
    {
        SkillPhase.Windup   => Config.windupTime,
        SkillPhase.Active   => Config.activeTime,
        SkillPhase.Recovery => Config.recoveryTime,
        _ => 0
    };

    public void Initialize(ISkillOwner owner, SkillConfig config)
    {
        Owner = owner;
        Config = config;
    }

    public void Tick(float deltaTime)
    {
        switch (Phase)
        {
            case SkillPhase.Idle:
                if (cooldownRemaining > 0)
                {
                    cooldownRemaining -= deltaTime;
                    if (cooldownRemaining <= 0) { cooldownRemaining = 0; OnReady(); }
                }
                break;
            case SkillPhase.Windup:   TickPhase(deltaTime, Config.windupTime,   SkillPhase.Active,    OnWindupTick);  break;
            case SkillPhase.Active:   TickPhase(deltaTime, Config.activeTime,   SkillPhase.Recovery,  OnActiveTick);  break;
            case SkillPhase.Recovery: TickPhase(deltaTime, Config.recoveryTime, SkillPhase.Cooldown,  OnRecoveryTick); break;
            case SkillPhase.Cooldown:
                cooldownRemaining -= deltaTime;
                if (cooldownRemaining <= 0) TransitionTo(SkillPhase.Idle);
                break;
        }
    }

    private void TickPhase(float dt, float limit, SkillPhase next, Action<float> tickCallback)
    {
        PhaseElapsed += dt;
        tickCallback?.Invoke(PhaseElapsed);
        if (PhaseElapsed >= limit) TransitionTo(next);
    }

    private void TransitionTo(SkillPhase next)
    {
        switch (Phase)
        {
            case SkillPhase.Windup:   OnWindupEnd();   break;
            case SkillPhase.Active:   OnActiveEnd();   break;
            case SkillPhase.Recovery: OnRecoveryEnd();  break;
        }
        Phase = next;
        PhaseElapsed = 0;
        switch (next)
        {
            case SkillPhase.Active:   OnActiveStart();  break;
            case SkillPhase.Recovery: OnRecoveryStart(); break;
            case SkillPhase.Cooldown: cooldownRemaining = Config.cooldown; OnCooldownStart(); break;
        }
    }

    public virtual bool CanUse() => IsReady;
    public void Activate() { if (!IsReady) return; TransitionTo(SkillPhase.Windup); OnWindupStart(); }
    public void Interrupt() { OnInterrupt(); Phase = SkillPhase.Cooldown; cooldownRemaining = Config.cooldown * 0.5f; OnCooldownStart(); }
    public void ForceStop() { Phase = SkillPhase.Idle; cooldownRemaining = Config.cooldown; }

    // 子类钩子 (全部 virtual)
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
    private readonly List<CharacterSkill> skills = new();
    private readonly ISkillOwner owner;
    public CharacterSkill CurrentSkill { get; private set; }
    public bool IsBusy => CurrentSkill != null && CurrentSkill.IsRunning;

    public SkillRunner(ISkillOwner owner) => this.owner = owner;

    public void AddSkill(CharacterSkill skill) { skill.Initialize(owner, skill.Config); skills.Add(skill); }
    public CharacterSkill GetSkill(string skillId) => skills.Find(s => s.Config.id == skillId);

    public void Tick(float deltaTime)
    {
        CurrentSkill?.Tick(deltaTime);
        foreach (var s in skills) if (s != CurrentSkill) s.Tick(deltaTime);
        if (CurrentSkill != null && !CurrentSkill.IsRunning) CurrentSkill = null;
    }

    public bool TryExecute(CharacterSkill skill)
    {
        if (IsBusy || skill == null || !skill.IsReady) return false;
        CurrentSkill = skill; CurrentSkill.Activate(); return true;
    }
    public bool TryExecute(string id) => TryExecute(GetSkill(id));
    public void Interrupt() { CurrentSkill?.Interrupt(); CurrentSkill = null; }
    public void ForceStopAll() { CurrentSkill?.ForceStop(); CurrentSkill = null; foreach (var s in skills) s.ForceStop(); }
}
```

### SkillConfig

```csharp
public class SkillConfig
{
    public string id;
    public string name;
    public string className;
    public float cooldown;
    public float windupTime;
    public float activeTime;
    public float recoveryTime;
    public float damage;
    public float range;
    public float speed;
    public int priority;
    public string animTrigger;
    public string vfxPrefab;
    public string sfxId;
    // 玩家扩展字段
    public int mpCost;
    public int levelRequired;
    public string unlockConditionId;
}
```

---

## 目录结构

```
Assets/Scripts/Core/Skill/              ← Layer 2 共用层
├── CharacterSkill.cs
├── SkillPhase.cs
├── SkillConfig.cs
├── SkillRunner.cs
└── ISkillOwner.cs
```

---

## 依赖关系

```
character-skill-system (本 Change)
  依赖: Layer 1 (ObjectPool)
  被依赖: enemy-tick-skill-system, player-controller
  地位: Layer 2 与 Layer 3 之间的共用层
```
