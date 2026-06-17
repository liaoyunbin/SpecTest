using System;
using System.Collections.Generic;
using UnityEngine;

namespace UIFrameworkLib
{
    /// <summary>
    /// UI 状态枚举
    /// None：未加载或已重置
    /// Loading：异步加载预制体中
    /// AnimationEnter：加载完成，播放入场动画
    /// Opened：入场动画结束，UI 可见，交互启用
    /// AnimationExit：正在播放退场动画
    /// Closed：退场动画结束，已隐藏（可能缓存）
    /// </summary>
    public enum UIState
    {
        None,
        Loading,
        AnimationEnter,
        Opened,
        AnimationExit,
        Closed,
    }

    /// <summary>
    /// UI 状态机
    /// 管理状态转换合法性
    /// 状态转换图：
    ///   None → Loading → AnimationEnter → Opened → AnimationExit → Closed
    ///   Loading ──────────→ Closed（加载失败）
    ///   AnimationEnter ───→ Closed（中断关闭）
    ///   AnimationExit ────→ AnimationEnter（覆盖式重新打开）
    ///   Closed ───────────→ Loading（缓存复用）
    /// </summary>
    public class UIStateMachine
    {
        public UIState CurrentState { get; private set; } = UIState.None;
        public event Action<UIState, UIState> OnStateChanged;

        /// <summary>当前是否处于过渡状态（加载或动画中）</summary>
        public bool IsTransitioning =>
            CurrentState == UIState.Loading ||
            CurrentState == UIState.AnimationEnter ||
            CurrentState == UIState.AnimationExit;

        private static readonly Dictionary<UIState, HashSet<UIState>> ValidTransitions = new()
        {
            [UIState.None]           = new() { UIState.Loading },
            [UIState.Loading]        = new() { UIState.AnimationEnter, UIState.Closed },
            [UIState.AnimationEnter] = new() { UIState.Opened, UIState.Closed },
            [UIState.Opened]         = new() { UIState.AnimationExit },
            [UIState.AnimationExit]  = new() { UIState.Closed, UIState.AnimationEnter },
            [UIState.Closed]         = new() { UIState.Loading },
        };

        /// <summary>
        /// 尝试状态转换
        /// </summary>
        /// <returns>转换是否成功</returns>
        public bool TryTransitionTo(UIState newState)
        {
            if (!ValidTransitions.TryGetValue(CurrentState, out var allowed))
            {
                Debug.LogError($"[UIFrameworkLib] 状态 {CurrentState} 无转换定义");
                return false;
            }

            if (!allowed.Contains(newState))
            {
                Debug.LogWarning($"[UIFrameworkLib] 非法状态转换: {CurrentState} → {newState}");
                return false;
            }

            var oldState = CurrentState;
            CurrentState = newState;
            OnStateChanged?.Invoke(oldState, newState);
            return true;
        }
    }
}