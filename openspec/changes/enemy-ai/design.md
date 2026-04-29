# 敌人 AI —— 设计文档

> 父级: [metroidvania-with-ai 技术设计方案](../metroidvania-with-ai/design.md)

## AI 状态机

```
Patrol → (发现玩家) → Chase → (进入攻击范围) → Attack → (继续) → Chase
Patrol / Chase → (受伤) → Hurt
任一 → (HP<=0) → Death
```

## 暂停控制

```csharp
public class Enemy : MonoBehaviour
{
    private bool isPaused;
    
    private void Update()
    {
        if (isPaused) return; // 暂停 — 不 Tick
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
