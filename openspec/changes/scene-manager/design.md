# 场景管理器 —— 设计文档

> 父级: [metroidvania-with-ai 技术设计方案](../metroidvania-with-ai/design.md)

## Persistent Scene 结构

```
Persistent Scene
├── [Manager] GameManager        ← 场景加载/卸载/切换
├── [Manager] UIManager          ← UI 面板栈管理
├── [Manager] SaveManager        ← 存档/读档
├── [Manager] InputManager       ← 输入映射
├── [Manager] ConfigManager      ← Luban 配置查询
├── [Manager] AudioManager       ← 音频播放
├── UIRoot (Canvas)
│   ├── ToastLayer
│   ├── DialogLayer
│   └── PanelLayer
├── Player (DontDestroyOnLoad)
└── EventSystem
```

## Additive Scene 结构

```
Additive Scene (如 Forest_01)
├── Grid + Tilemap (地面/平台/墙壁)
├── 装饰层 Tilemap (背景/细节)
├── Enemies
├── NPCs
├── Items/Pickups
├── InteractObjects (存档点/传送门/开关/能力门)
├── SpawnPoint
└── 场景边界触发器
```

## 场景切换流程

```
1. 玩家接触场景边界触发器
2. GameManager.LoadArea(targetScene)
3. 显示 LoadingMask (UIManager, 设置 isLoading=true)
4. 关闭所有 Dialog + Normal 面板
5. 卸载不再需要的 Additive Scene
6. 加载目标 Additive Scene (异步, LoadSceneMode.Additive)
7. 替换底层 HUD (新场景 HUD)
8. 传送 Player 到 SpawnPoint
9. 隐藏 LoadingMask (设置 isLoading=false)
10. 触发 OnSceneChanged 事件
```

## 场景配置表 (Luban)

| scene_id | scene_name | display_name | hud_id | bgm_id | neighbors |
|----------|------------|-------------|--------|--------|-----------|
| 1001 | Forest_01 | 遗忘森林 | 1001 | 2001 | [1002,2001] |
| 1002 | Forest_02 | 密林深处 | 1001 | 2001 | [1001] |
| 2001 | Cave_01 | 幽暗洞穴 | 2001 | 2002 | [1001,2002] |
