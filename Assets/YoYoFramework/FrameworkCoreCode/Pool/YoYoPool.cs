#if YoYo_PoolModule

using System.Collections.Generic;
using UnityEngine;
using YoYo.Asset;

namespace YoYo.FrameWork.Pool
{
    /// <summary>
    /// 池对象接口（可选实现）
    /// </summary>
    public interface IPoolable
    {
        void OnGet();
        void OnRelease();
    }

    /// <summary>
    /// GameObject对象池（依赖资源系统）
    /// </summary>
    public class YoYoGameObjectPool
    {
        private Stack<GameObject> _free = new Stack<GameObject>();
        private HashSet<GameObject> _active = new HashSet<GameObject>();

        private YoYoResourceOwner _owner;
        private AssetToken _asset;

        private float _lastUseTime;

        private int _maxCapacity;
        private float _autoReleaseTime;

        public void Init(YoYoResourceOwner owner, AssetToken asset, int maxCapacity, float autoReleaseTime)
        {
            _owner = owner;
            _asset = asset;
            _maxCapacity = maxCapacity;
            _autoReleaseTime = autoReleaseTime;
            _lastUseTime = Time.time;
        }

        /// <summary>
        /// 获取对象
        /// </summary>
        public GameObject Get()
        {
            _lastUseTime = Time.time;

            GameObject obj;

            if (_free.Count > 0)
            {
                obj = _free.Pop();
                obj.SetActive(true);
            }
            else
            {
                obj = _owner.Instantiate(_asset);
            }

            _active.Add(obj);

            if (obj.TryGetComponent<IPoolable>(out var p))
                p.OnGet();

            return obj;
        }

        /// <summary>
        /// 回收对象
        /// </summary>
        public void Release(GameObject obj)
        {
            if (!_active.Contains(obj))
            {
                Debug.LogError("对象不属于该池");
                return;
            }

            _active.Remove(obj);

            if (obj.TryGetComponent<IPoolable>(out var p))
                p.OnRelease();

            if (_free.Count >= _maxCapacity)
            {
                _owner.Destory(obj);
                return;
            }

            obj.SetActive(false);
            _free.Push(obj);
        }

        /// <summary>
        /// 预热
        /// </summary>
        public void Preload(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var obj = _owner.Instantiate(_asset);
                obj.SetActive(false);
                _free.Push(obj);
            }
        }

        /// <summary>
        /// 每帧驱动
        /// </summary>
        public void Tick()
        {
            if (Time.time - _lastUseTime > _autoReleaseTime)
            {
                ClearFree();
            }
        }

        /// <summary>
        /// 清理空闲对象
        /// </summary>
        public void ClearFree()
        {
            foreach (var obj in _free)
            {
                _owner.Destory(obj);
            }
            _free.Clear();
        }

        /// <summary>
        /// 销毁池
        /// </summary>
        public void Destroy()
        {
            ClearFree();

            foreach (var obj in _active)
            {
                _owner.Destory(obj);
            }
            _active.Clear();
        }
    }

    /// <summary>
    /// 泛型对象池（纯C#对象）
    /// </summary>
    public class YoYoObjectPool<T> where T : class, new()
    {
        private readonly Stack<T> _free = new Stack<T>();
        private int _maxCapacity;

        public YoYoObjectPool(int maxCapacity = 1000)
        {
            _maxCapacity = maxCapacity;
        }

        /// <summary>
        /// 获取对象
        /// </summary>
        public T Get()
        {
            if (_free.Count > 0)
            {
                var obj = _free.Pop();

                if (obj is IPoolable p)
                    p.OnGet();

                return obj;
            }

            var newObj = new T();

            if (newObj is IPoolable p2)
                p2.OnGet();

            return newObj;
        }

        /// <summary>
        /// 回收对象
        /// </summary>
        public void Release(T obj)
        {
            if (obj == null)
                return;

            if (obj is IPoolable p)
                p.OnRelease();

            if (_free.Count >= _maxCapacity)
                return;

            _free.Push(obj);
        }

        /// <summary>
        /// 预热
        /// </summary>
        public void Preload(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _free.Push(new T());
            }
        }

        public void Clear()
        {
            _free.Clear();
        }
    }
}

#endif