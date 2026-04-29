# 怪物技能 — 设计文档

> **基类由 [character-skill-system](../character-skill-system/design.md) 提供** (CharacterSkill / SkillRunner / ISkillOwner / SkillPhase)
> 本设计仅含 EnemySkill 继承层 + 3 种技能。

## EnemySkill

```csharp
public class EnemySkill : CharacterSkill
{
    protected new Enemy Owner => (Enemy)base.Owner;

    public override bool CanUse()
    {
        if (!IsReady) return false;
        if (Owner.IsStunned || Owner.IsDead) return false;
        return OnCanUse();
    }

    protected virtual bool OnCanUse() => true;
}
```

## MeleeSlashSkill

```csharp
public class MeleeSlashSkill : EnemySkill
{
    private Collider2D[] hitBuffer = new Collider2D[8];

    protected override bool OnCanUse()
    {
        float dist = Vector2.Distance(Owner.transform.position, Owner.Target.position);
        return dist <= Config.range && Owner.IsGrounded;
    }

    protected override void OnWindupStart()
    {
        Owner.Animator.SetTrigger(Config.animTrigger);
        Owner.FaceTarget();
    }

    protected override void OnActiveStart()
    {
        var count = Physics2D.OverlapCircleNonAlloc(
            Owner.attackPoint.position, Config.range, hitBuffer, Owner.targetLayer);
        for (int i = 0; i < count; i++)
        {
            if (hitBuffer[i].TryGetComponent<IDamageable>(out var t))
                t.TakeDamage((int)Config.damage);
        }
        ObjectPool.Get<SimpleVFX>(Config.vfxPrefab, Owner.attackPoint.position);
        AudioManager.Instance.PlaySFX(Config.sfxId);
    }

    protected override void OnRecoveryStart() => Owner.SetParryable(true);
    protected override void OnCooldownStart() => Owner.SetParryable(false);
    protected override void OnInterrupt() => Owner.SetParryable(false);
}
```

## FireballSkill

```csharp
public class FireballSkill : EnemySkill
{
    protected override bool OnCanUse()
    {
        float dist = Vector2.Distance(Owner.transform.position, Owner.Target.position);
        return dist >= Config.minRange && dist <= Config.maxRange;
    }

    protected override void OnWindupStart()
    {
        Owner.Animator.SetTrigger("Cast");
        Owner.FaceTarget();
    }

    protected override void OnActiveStart()
    {
        var dir = (Owner.Target.position - Owner.firePoint.position).normalized;
        var proj = ObjectPool.Get<Projectile>(Config.projectilePrefab);
        proj.transform.position = Owner.firePoint.position;
        proj.Launch(dir, Config.speed, Config.damage, Owner);
    }
}
```

## GroundSpikeSkill

```csharp
public class GroundSpikeSkill : EnemySkill
{
    private GameObject warning, spike;

    protected override void OnWindupStart()
    {
        warning = ObjectPool.Get(Config.warningPrefab, Owner.Target.position);
    }

    protected override void OnWindupTick(float elapsed)
    {
        float rate = Mathf.Lerp(0.5f, 0.1f, elapsed / Config.windupTime);
        warning.SetActive(Mathf.PingPong(Time.unscaledTime * (1f / rate), 1f) > 0.5f);
    }

    protected override void OnActiveStart()
    {
        ObjectPool.Release(warning);
        spike = ObjectPool.Get(Config.spikePrefab, warning.transform.position);
    }

    protected override void OnRecoveryTick(float elapsed)
    {
        spike.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, elapsed / Config.recoveryTime);
    }

    protected override void OnCooldownStart()
    {
        ObjectPool.Release(spike); spike = null;
    }
}
```

## 目录结构

```
Assets/Scripts/Entities/Enemy/Skills/
├── EnemySkill.cs
├── MeleeSlashSkill.cs
├── FireballSkill.cs
├── GroundSpikeSkill.cs
└── SkillFactory.cs
```
