using System;
using System.Collections.Generic;

namespace YoYo.Event
{
    /// <summary>
    /// 事件列表接口（用于统一管理）
    /// </summary>
    public interface IEventList { }

    /// <summary>
    /// 事件绑定（用于解绑）
    /// </summary>
    public interface IEventBinding
    {
        void Unbind();
    }

    /// <summary>
    /// 泛型事件列表
    /// </summary>
    public class EventList<T> : IEventList
    {
        private readonly List<Action<T>> _listeners = new(8);

        /// <summary>
        /// 添加监听
        /// </summary>
        public IEventBinding Add(Action<T> callback)
        {
            // 防止重复注册
            if (_listeners.Contains(callback))
                return new EventBinding<T>(this, callback);

            _listeners.Add(callback);
            return new EventBinding<T>(this, callback);
        }

        /// <summary>
        /// 移除监听
        /// </summary>
        public void Remove(Action<T> callback)
        {
            _listeners.Remove(callback);
        }

        /// <summary>
        /// 触发事件
        /// </summary>
        public void Invoke(T evt)
        {
            // 倒序遍历，避免Remove影响
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                var cb = _listeners[i];

                if (cb == null)
                {
                    _listeners.RemoveAt(i);
                    continue;
                }

                try
                {
                    cb.Invoke(evt);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"Event Invoke Error: {e}");
                }
            }
        }
    }

    /// <summary>
    /// 事件绑定实现
    /// </summary>
    public class EventBinding<T> : IEventBinding
    {
        private EventList<T> _list;
        private Action<T> _callback;

        public EventBinding(EventList<T> list, Action<T> callback)
        {
            _list = list;
            _callback = callback;
        }

        public void Unbind()
        {
            _list?.Remove(_callback);
            _list = null;
            _callback = null;
        }
    }
}