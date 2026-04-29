# 音频管理器

## 问题陈述

游戏需要统一的音频播放管理，包括 BGM、SFX、音量控制和音频对象池。

## 提议方案

实现 AudioManager 单例，支持 BGM 节奏切换、SFX 空间播放。

## 核心功能

- BGM 播放/切换/淡入淡出
- SFX 2D/3D 播放
- 音量分组控制 (Master/BGM/SFX)
- 音频对象池

## 预期成果

- AudioManager 单例
- BGM 淡入淡出
- 音量分组控制

## 相关约束

- 依赖 Layer 1 对象池