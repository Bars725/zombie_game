using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core.Events
{
    /// <summary>Marker for global game events. Events are small structs (no allocations on raise).</summary>
    public interface IGameEvent { }

    /// <summary>
    /// Typed global event bus for cross-system events (EnemyKilled, WaveStarted, ScrapChanged...).
    /// Use it for "many systems care" events. For local wiring (a door listening to its lever)
    /// prefer direct references or C# events.
    ///
    /// Rules:
    /// - Handlers subscribed during Raise are not called until the next Raise.
    /// - Handlers unsubscribed during Raise are not called (if not yet reached).
    /// - An exception in one handler is logged and does not stop the others.
    /// - Subscribing the same handler twice has no effect.
    /// - All buses are cleared on play-mode start (works with Domain Reload disabled).
    /// </summary>
    public static class EventBus<T> where T : struct, IGameEvent
    {
        private static readonly List<Action<T>> s_Handlers = new List<Action<T>>();
        private static int s_RaiseDepth;
        private static bool s_NeedsCompaction;

        static EventBus()
        {
            EventBusRegistry.Register(Clear);
        }

        public static int HandlerCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < s_Handlers.Count; i++)
                {
                    if (s_Handlers[i] != null) count++;
                }
                return count;
            }
        }

        public static void Subscribe(Action<T> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (s_Handlers.Contains(handler)) return;
            s_Handlers.Add(handler);
        }

        public static void Unsubscribe(Action<T> handler)
        {
            if (handler == null) return;
            int index = s_Handlers.IndexOf(handler);
            if (index < 0) return;

            if (s_RaiseDepth > 0)
            {
                // Don't shift indices while Raise is iterating.
                s_Handlers[index] = null;
                s_NeedsCompaction = true;
            }
            else
            {
                s_Handlers.RemoveAt(index);
            }
        }

        public static void Raise(T evt)
        {
            s_RaiseDepth++;
            try
            {
                // Snapshot the count: handlers added during this raise wait for the next one.
                int count = s_Handlers.Count;
                for (int i = 0; i < count; i++)
                {
                    Action<T> handler = s_Handlers[i];
                    if (handler == null) continue;
                    try
                    {
                        handler(evt);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
            finally
            {
                s_RaiseDepth--;
                if (s_RaiseDepth == 0 && s_NeedsCompaction)
                {
                    s_Handlers.RemoveAll(h => h == null);
                    s_NeedsCompaction = false;
                }
            }
        }

        public static void Clear()
        {
            if (s_RaiseDepth > 0)
            {
                for (int i = 0; i < s_Handlers.Count; i++) s_Handlers[i] = null;
                s_NeedsCompaction = true;
            }
            else
            {
                s_Handlers.Clear();
            }
        }
    }

    /// <summary>Keeps track of every EventBus&lt;T&gt; in use so they can all be cleared at once.</summary>
    public static class EventBusRegistry
    {
        private static readonly List<Action> s_ClearActions = new List<Action>();

        internal static void Register(Action clear)
        {
            lock (s_ClearActions) s_ClearActions.Add(clear);
        }

        public static void ClearAll()
        {
            lock (s_ClearActions)
            {
                for (int i = 0; i < s_ClearActions.Count; i++) s_ClearActions[i]();
            }
        }

        // Runs before the first scene loads on every play-mode start, including when
        // Enter Play Mode Options disable Domain Reload (statics would otherwise survive).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayModeStart()
        {
            ClearAll();
        }
    }
}
