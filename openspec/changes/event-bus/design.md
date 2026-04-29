# EventBus 事件系统 —— 设计文档

> 父级: [metroidvania-with-ai 技术设计方案](../metroidvania-with-ai/design.md)

## 整体架构

EventBus 是全局事件发布/订阅系统，所有跨系统通信通过它解耦。

## 核心类

```csharp
public class EventBus : Singleton<EventBus>
{
    private Dictionary<string, List<Action<object>>> listeners = new();
    
    public void Publish(string eventName, object data)
    {
        if (listeners.TryGetValue(eventName, out var handlers))
        {
            foreach (var handler in handlers)
                handler?.Invoke(data);
        }
    }
    
    public void Subscribe(string eventName, Action<object> handler)
    {
        if (!listeners.ContainsKey(eventName))
            listeners[eventName] = new List<Action<object>>();
        listeners[eventName].Add(handler);
    }
    
    public void Unsubscribe(string eventName, Action<object> handler)
    {
        if (listeners.TryGetValue(eventName, out var handlers))
            handlers.Remove(handler);
    }
    
    #if UNITY_EDITOR
    public bool debugMode = true;
    #endif
}
```

## 事件列表（全局）

| 事件名 | 发布者 | 订阅者 | 参数 |
|--------|--------|--------|------|
| `EnemyKilled` | Enemy | QuestSystem, AchievementSystem | enemyId, position |
| `ItemCollected` | Item | GrowthSystem, QuestSystem | itemId, count |
| `AbilityUnlocked` | GrowthSystem | ExploreSystem, UIManager | abilityId |
| `BossDefeated` | Boss(Enemy) | QuestSystem, ExploreSystem | bossId, areaId |
| `PlayerDied` | Player | GameManager, UIManager | deathPosition |
| `PlayerRespawned` | GameManager | Player, UIManager | checkpointId |
| `CheckpointActivated` | InteractObj | SaveManager, ExploreSystem | checkpointId |
| `AreaEntered` | GameManager | AudioManager, ExploreSystem | areaId |
| `AreaExited` | GameManager | AudioManager | areaId |
| `QuestAccepted` | NPC | QuestSystem, UIManager | questId |
| `QuestCompleted` | QuestSystem | UIManager, GrowthSystem | questId |
| `LevelUp` | GrowthSystem | UIManager, VFX | newLevel |
| `SceneLoadStart` | GameManager | UIManager, AudioManager | sceneName |
| `SceneLoadComplete` | GameManager | UIManager, ExploreSystem | sceneName |

## 命名规范

- 事件名: 过去式动词 + 主语 + 动作 (如 `EnemyKilled`, `ItemCollected`)
- 禁止中文事件名
