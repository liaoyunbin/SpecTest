# 存档系统 —— 设计文档

> 父级: [metroidvania-with-ai 技术设计方案](../metroidvania-with-ai/design.md)

## 数据管线

```
保存时:
各系统状态 → SaveData (聚合) → JsonConvert.SerializeObject
    → save_NNN.json → Application.persistentDataPath

读档时:
save_NNN.json → JsonConvert.DeserializeObject<SaveData>
    → 各系统恢复状态 → 查 Luban 表补全详细数据
```

## 存档数据结构

```csharp
[Serializable]
public class SaveData
{
    public int saveVersion = 1;
    public string saveTime;
    public float playTime;
    public PlayerData player;
    public ExplorationData exploration;
    public InventoryData inventory;
    public ProgressData progress;
}

[Serializable]
public class PlayerData
{
    public Vector2 position;
    public int currentHp, maxHp;
    public int currentMp, maxMp;
    public int level, exp;
}

[Serializable]
public class ExplorationData
{
    public string currentScene;
    public List<string> unlockedRooms;
    public float mapExplorationRate;
    public List<string> activatedCheckpoints;
}

[Serializable]
public class InventoryData
{
    public List<string> ownedItemIds;
    public List<string> unlockedAbilityIds;
    public Dictionary<string, int> itemCount;
    public Dictionary<string, string> equipmentSlots;
}

[Serializable]
public class ProgressData
{
    public List<string> defeatedBossIds;
    public List<string> completedQuestIds;
    public List<string> triggeredEventIds;
}
```

## 关键原则

- 存档只存 ID 引用，具体数值从 Luban 表查询
- 改表不影响已有存档
- 版本号机制处理存档格式升级
