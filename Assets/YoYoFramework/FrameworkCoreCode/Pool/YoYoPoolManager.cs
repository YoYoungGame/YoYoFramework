#if YoYo_PoolModule

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using YoYo.Asset;

namespace YoYo.FrameWork.Pool
{
    /// <summary>
    /// 对象池总管理器（统一入口）
    /// </summary>
    public class YoYoPoolManager
    {
        private static YoYoPoolManager _instance;
        public static YoYoPoolManager Instance => _instance ??= new YoYoPoolManager();

        /// <summary>GameObject池</summary>
        private readonly Dictionary<string, YoYoGameObjectPool> _goPools = new();

        /// <summary>普通对象池</summary>
        private readonly Dictionary<Type, object> _objPools = new();

        /// <summary>共享资源Owner
        private readonly YoYoResourceOwner _owner = new YoYoResourceOwner();

        #region GameObject Pool

        /// <summary>
        /// 创建对象池
        /// </summary>
        public async Task<YoYoGameObjectPool> CreateGameObjectPool(string location, int preload = 0, int maxCapacity = 200, float autoReleaseTime = 60f, string package = null)
        {
            if (_goPools.ContainsKey(location))
                return _goPools[location];

            var asset = await _owner.LoadAssetAsync<GameObject>(location, package);

            var pool = new YoYoGameObjectPool();
            pool.Init(_owner, asset, maxCapacity, autoReleaseTime);

            _goPools[location] = pool;

            if (preload > 0)
                pool.Preload(preload);

            return pool;
        }

        public GameObject GetGameObject(string location)
        {
            if (!_goPools.TryGetValue(location, out var pool))
            {
                Debug.LogError($"对象池不存在: {location}");
                return null;
            }

            return pool.Get();
        }

        public void ReleaseGameObject(string location, GameObject obj)
        {
            if (!_goPools.TryGetValue(location, out var pool))
            {
                Debug.LogError($"对象池不存在: {location}");
                return;
            }

            pool.Release(obj);
        }

        public void DestroyGameObjectPool(string location)
        {
            if (!_goPools.TryGetValue(location, out var pool))
                return;

            pool.Destroy();
            _goPools.Remove(location);
        }

        #endregion

        #region Object Pool

        public YoYoObjectPool<T> GetObjectPool<T>() where T : class, new()
        {
            var type = typeof(T);

            if (_objPools.TryGetValue(type, out var pool))
                return pool as YoYoObjectPool<T>;

            var newPool = new YoYoObjectPool<T>();
            _objPools[type] = newPool;

            return newPool;
        }

        public T GetObj<T>() where T : class, new()
        {
            return GetObjectPool<T>().Get();
        }

        public void ReleaseObj<T>(T obj) where T : class, new()
        {
            GetObjectPool<T>().Release(obj);
        }

        public void PreloadObj<T>(int count) where T : class, new()
        {
            GetObjectPool<T>().Preload(count);
        }

        #endregion

        #region Lifecycle

        /// <summary>
        /// 每帧驱动（由GameManager调用）
        /// </summary>
        public void Update()
        {
            foreach (var pool in _goPools.Values)
            {
                pool.Tick();
            }
        }

        /// <summary>
        /// 销毁全部
        /// </summary>
        public void Destroy()
        {
            foreach (var pool in _goPools.Values)
            {
                pool.Destroy();
            }

            _goPools.Clear();
            _objPools.Clear();

            // 最后统一释放资源
            _owner.ReleaseAllAsset(true);
        }

        #endregion
    }
}

#endif