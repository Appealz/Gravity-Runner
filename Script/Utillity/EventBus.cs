using UnityEngine;
using System.Collections.Generic;
using System;

public static class EventBus
{
    // 이벤트 타입별로 델리게이트 보관
    private static readonly  Dictionary<Type, Delegate> _table = new Dictionary<Type, Delegate>();

    /// <summary>
    /// 이벤트 구독 (payload 있는 타입)
    /// </summary>
    public static void Subscribe<T>(Action<T> handler)
    {
        if (handler == null) return;

        if (_table.TryGetValue(typeof(T), out Delegate existing))
        {
            _table[typeof(T)] = (Action<T>)existing + handler;
        }
        else
        {
            _table[typeof(T)] = handler;
        }
    }

    /// <summary>
    /// 이벤트 구독 해제
    /// </summary>
    public static void Unsubscribe<T>(Action<T> handler)
    {
        if (handler == null) return;

        if (_table.TryGetValue(typeof(T), out Delegate existing))
        {
            var current = (Action<T>)existing - handler;
            if (current == null) _table.Remove(typeof(T));
            else _table[typeof(T)] = current;
        }
    }

    /// <summary>
    /// 이벤트 발행 (payload 있는 타입)
    /// </summary>
    public static void Publish<T>(T evt)
    {
        if (_table.TryGetValue(typeof(T), out Delegate existing))
        {
            ((Action<T>)existing)?.Invoke(evt);
        }
    }

    /// <summary>
    /// payload 없는 신호(Signals)도 지원
    /// </summary>
    public static void Subscribe(Action handler) => Subscribe<Signal>(Wrap(handler));
    public static void Unsubscribe(Action handler) => Unsubscribe<Signal>(Wrap(handler));
    public static void Publish() => Publish<Signal>(default);

    // 내부: 무상태 신호용 래퍼
    private static Action<Signal> Wrap(Action a)
    {
        return a == null ? null : new Action<Signal>(_ => a());
    }

    private struct Signal { } // 빈 신호 타입
}