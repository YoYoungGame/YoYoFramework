using System;
using System.Collections.Generic;

namespace YoYo.Event
{
    /// <summary>
    /// 商业级事件系统（全局唯一）
    /// 特点：
    /// 1. 基于类型的事件系统（Type EventBus）
    /// 2. 支持泛型事件（强类型，无GC）
    /// 3. 支持自动解绑（防止内存泄漏）
    /// 4. 非MonoBehaviour，由外部驱动生命周期
    /// </summary>
    public class YoYoEventManager
    {
        private static YoYoEventManager _instance;
        public static YoYoEventManager Instance => _instance ??= new YoYoEventManager();

        /// <summary>
        /// 事件表：EventType -> ListenerList
        /// </summary>
        private readonly Dictionary<Type, IEventList> _eventTable = new(128);

        /// <summary>
        /// Owner -> 注册信息（用于自动解绑）
        /// </summary>
        private readonly Dictionary<object, List<IEventBinding>> _ownerBindings = new(128);


        #region 生命周期


        public void Destroy()
        {
            _eventTable.Clear();
            _ownerBindings.Clear();
        }

        #endregion

        #region 订阅

        /// <summary>
        /// 订阅事件（无Owner）
        /// </summary>
        public void Subscribe<T>(Action<T> callback)
        {
            Subscribe(null, callback);
        }

        /// <summary>
        /// 订阅事件（带Owner，用于自动解绑）
        /// </summary>
        public void Subscribe<T>(object owner, Action<T> callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            var type = typeof(T);

            if (!_eventTable.TryGetValue(type, out var list))
            {
                list = new EventList<T>();
                _eventTable[type] = list;
            }

            var binding = ((EventList<T>)list).Add(callback);

            // 记录owner绑定
            if (owner != null)
            {
                if (!_ownerBindings.TryGetValue(owner, out var bindings))
                {
                    bindings = new List<IEventBinding>(4);
                    _ownerBindings[owner] = bindings;
                }
                bindings.Add(binding);
            }
        }

        #endregion

        #region 取消订阅

        public void Unsubscribe<T>(Action<T> callback)
        {
            var type = typeof(T);

            if (_eventTable.TryGetValue(type, out var list))
            {
                ((EventList<T>)list).Remove(callback);
            }
        }

        /// <summary>
        /// 通过Owner自动解绑（推荐在OnDestroy调用）
        /// </summary>
        public void UnsubscribeByOwner(object owner)
        {
            if (owner == null) return;

            if (_ownerBindings.TryGetValue(owner, out var bindings))
            {
                foreach (var binding in bindings)
                {
                    binding.Unbind();
                }
                bindings.Clear();
                _ownerBindings.Remove(owner);
            }
        }

        #endregion

        #region 触发事件

        public void Publish<T>(T evt)
        {
            var type = typeof(T);

            if (_eventTable.TryGetValue(type, out var list))
            {
                ((EventList<T>)list).Invoke(evt);
            }
        }

        #endregion
    }
}