# 场景管理器

## 问题陈述

银河城游戏需要多区域无缝切换，单大场景内存不可控，需要 Additive Scene 方案。

## 提议方案

Persistent Scene 常驻 + Additive Scene 按需加载，GameManager 统一管理。

## 核心功能

- 多场景加载/卸载 (LoadSceneMode.Additive)
- 场景切换进度事件
- 场景配置读取 (Luban → SceneConfig)
- 场景切换 UI 联动 (关闭弹窗/普通面板，替换 HUD)
- 玩家出生点/SpawnPoint 管理
- 相邻场景预加载

## Persistent Scene 结构

```
Persistent Scene
├── [Manager] GameManager / UIManager / SaveManager
├── [Manager] InputManager / ConfigManager / AudioManager
├── UIRoot (Canvas)
├── Player (DontDestroyOnLoad)
└── EventSystem
```

## Additive Scene 结构

```
Additive Scene (如 Forest_01)
├── Grid + Tilemap
├── Enemies / NPCs / Items / InteractObjects
├── SpawnPoint
└── 场景边界触发器
```

## 场景切换流程

1. 玩家接触场景边界触发器
2. GameManager.LoadArea(targetScene)
3. 显示 LoadingMask (UIManager, isLoading=true)
4. 关闭所有 Dialog + Normal 面板
5. 卸载不需要的 Additive Scene
6. 加载目标 Additive Scene (异步)
7. 替换底层 HUD (新场景 HUD)
8. 传送 Player 到 SpawnPoint
9. 隐藏 LoadingMask (isLoading=false)
10. 触发 OnSceneChanged 事件

## 预期成果

- GameManager 单例
- 场景切换无忧
- 相邻预加载

## 相关约束

- 依赖 ui-manager, config-manager, audio-manager
- Manager 和 UIRoot 必须放在 Persistent Scene