# 存档系统

## 问题陈述

需要统一的存档/读档机制，支持多槽位管理、版本兼容和自动存档。

## 提议方案

基于 Newtonsoft.Json 实现存档系统，存档只存 ID 引用，具体数值从 Luban 配置表查询。

## 核心功能

- 多槽位存档/读档/删除
- Newtonsoft.Json 序列化/反序列化
- Vector2 自定义 Converter
- 存档版本校验（兼容旧档）
- 自动存档（检查点触发）

## 预期成果

- SaveManager 单例
- SaveData 数据结构
- 3 个存档槽位
- 版本升级兼容

## 相关约束

- 依赖 Newtonsoft.Json (com.unity.nuget.newtonsoft-json)
- 存档只存 ID 引用，数值从 Luban 表查询
- 依赖 config-manager, event-bus