#if YoYo_AssetModule
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using YooAsset;
using YoYo.Asset;

namespace YoYo.Asset
{
    public class YoYoResourceOwner
    {
        /// <summary>
        /// Asset -> 实例列表
        /// </summary>
        private Dictionary<AssetToken, List<UnityEngine.Object>> instanceList = new Dictionary<AssetToken, List<UnityEngine.Object>>();

        /// <summary>
        /// 实例 -> Asset
        /// </summary>
        private Dictionary<UnityEngine.Object, AssetToken> instances = new Dictionary<UnityEngine.Object, AssetToken>();

        /// <summary>
        /// Asset -> HandleToken
        /// </summary>
        private Dictionary<AssetToken, HandleBase> tokenMap = new Dictionary<AssetToken, HandleBase>();

        #region Load

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="location">资源地址</param>
        /// <returns></returns>
        public async Task<AssetToken> LoadAssetAsync<T>(string location, string packageName = null) where T : UnityEngine.Object
        {
            try
            {
                var handle = await YoYoAssetManager.Instance.LoadAssetAsync<T>(location, this, packageName);
                if (handle != null)
                {
                    var asset = new AssetToken(location, true);
                    tokenMap[asset] = handle;
                    instanceList[asset] = new List<UnityEngine.Object>();
                    return asset;
                }
                return default;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return default;
            }
        }

        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="location">资源地址</param>
        /// <returns></returns>
        public AssetToken LoadAssetSync<T>(string location, string packageName = null) where T : UnityEngine.Object
        {
            try
            {
                var handle = YoYoAssetManager.Instance.LoadAssetSync<T>(location, this, packageName);
                if (handle != null)
                {
                    var asset = new AssetToken(location, true);
                    tokenMap[asset] = handle;
                    instanceList[asset] = new List<UnityEngine.Object>();
                    return asset;
                }
                return default;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return default;
            }
        }

        /// <summary>
        /// 异步加载原生资源
        /// </summary>
        /// <param name="location"></param>
        /// <param name="packageName"></param>
        /// <returns></returns>
        public async Task<(AssetToken asset, byte[] bytes)> LoadRawFileASync(string location, string packageName = null)
        {
            try
            {
                var result = await YoYoAssetManager.Instance.LoadRawFileAsync(location, this, packageName);
                var handle = result.handle;
                if (handle != null)
                {
                    var asset = new AssetToken(location, true, true);
                    tokenMap[asset] = handle;
                    instanceList[asset] = new List<UnityEngine.Object>();
                    return (asset, result.bytes);
                }
                return default;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return default;
            }
        }

        #endregion

        #region Release Asset

        /// <summary>
        /// 释放指定资源
        /// </summary>
        /// <param name="asset">资源token</param>
        /// <param name="force">强制销毁资源实例(危险)</param>
        public void ReleaseAsset(AssetToken asset, bool force = false)
        {
            if (!asset.GetValid())
            {
                Debug.LogError("无效的资源");
                return;
            }
            if (!tokenMap.ContainsKey(asset))
            {
                Debug.LogError("禁止释放非本owner加载的资源");
                return;
            }

            if (instanceList[asset].Count > 0)
            {
                if (!force)
                {
                    Debug.LogError("禁止非强制模式释放持有实例的资源");
                    return;
                }

                DestoryAllInstanceInAsset(asset);
            }

            YoYoAssetManager.Instance.ReleaseHandel(tokenMap[asset]);

            tokenMap.Remove(asset);
            instanceList.Remove(asset);
        }

        /// <summary>
        /// 释放全部资源
        /// </summary>
        /// <param name="force">强制销毁资源实例(危险)</param>
        public void ReleaseAllAsset(bool force = false)
        {
            var list = new List<AssetToken>(tokenMap.Keys);
            foreach (var asset in list)
            {
                ReleaseAsset(asset, force);
            }
        }

        #endregion

        #region Instantiate

        /// <summary>
        /// 克隆一个资源副本，一般用于非prefab资源
        /// </summary>
        /// <typeparam name="T">副本类型</typeparam>
        /// <param name="asset">资源token</param>
        /// <returns></returns>
        public T Clone<T>(AssetToken asset, Vector3 position = default, Quaternion rotation = default, Transform parent = null) where T : UnityEngine.Object
        {
            if (!asset.GetValid())
            {
                Debug.LogError("无效的资源");
                return null;
            }
            if (asset.GetIsRawFile())
            {
                Debug.LogError("原生资源无法实例化");
                return null;
            }
            if (!tokenMap.TryGetValue(asset, out var handle))
            {
                Debug.LogError("禁止创建非本owner的资源副本");
                return null;
            }

            var obj = YoYoAssetManager.Instance.Instantiate(handle, position, rotation, parent);
            if (obj == null)
            {
                return null;
            }
            instances[obj] = asset;
            instanceList[asset].Add(obj);

            return obj as T;
        }

        /// <summary>
        /// 创建一个prefab资源实体
        /// </summary>
        /// <param name="asset">资源token</param>
        /// <returns></returns>
        public GameObject Instantiate(AssetToken asset, Vector3 position = default, Quaternion rotation = default, Transform parent = null)
        {
            return Clone<GameObject>(asset, position, rotation, parent);
        }

        #endregion

        #region Destroy Instance

        /// <summary>
        /// 销毁一个指定资源副本实例
        /// </summary>
        /// <param name="obj">实例对象</param>
        public void Destory(UnityEngine.Object obj)
        {
            if (!instances.TryGetValue(obj, out var asset))
            {
                Debug.LogError("禁止销毁不属于本owner的实例");
                return;
            }

            instances.Remove(obj);
            instanceList[asset].Remove(obj);

            UnityEngine.Object.Destroy(obj);
        }

        /// <summary>
        /// 销毁指定资源的全部副本实例
        /// </summary>
        /// <param name="asset"></param>
        public void DestoryAllInstanceInAsset(AssetToken asset)
        {
            if (!asset.GetValid())
            {
                Debug.LogError("无效的资源");
                return;
            }
            if (asset.GetIsRawFile())
            {
                Debug.LogError("原生资源无法实例化");
                return;
            }
            if (!instanceList.TryGetValue(asset, out var list))
            {
                Debug.LogError("非法AssetToken");
                return;
            }

            // 倒序删除，防止在遍历过程中如果触发了某些逻辑修改列表导致崩溃
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var obj = list[i];
                if (obj != null) // 这里 Unity 会检查原生对象是否还存在
                {
                    UnityEngine.Object.Destroy(obj);
                }
                instances.Remove(obj);
            }
            list.Clear();
        }

        /// <summary>
        /// 销毁该owner下全部副本实例
        /// </summary>
        public void DestoryAllInstance()
        {
            foreach (var pair in instanceList)
            {
                foreach (var obj in pair.Value)
                {
                    if (obj != null)
                        UnityEngine.Object.Destroy(obj);
                }
            }

            instances.Clear();

            foreach (var list in instanceList.Values)
                list.Clear();
        }

        #endregion

        #region Full Release

        /// <summary>
        /// 强制销毁该owner下全部资源和副本实例
        /// </summary>
        /// <param name="force"></param>
        public void ReleaseAll(bool force)
        {
            ReleaseAllAsset(force);
        }

        #endregion
    }
}
#endif