#if YoYo_AssetModule
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using YooAsset;
using YoYo.Config;
using YoYo.Tools;

namespace YoYo.Asset
{
    public class YoYoAssetManager
    {
        private static YoYoAssetManager _instance;
        public static YoYoAssetManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new YoYoAssetManager();
                return _instance;
            }
        }

        private ResourcePackage _mainpackage;

        private Dictionary<string, ResourcePackage> packages = new Dictionary<string, ResourcePackage>();

        private List<YoYoResourceOwner> _owners = new List<YoYoResourceOwner>();

        private List<HandleBase> _handles  = new List<HandleBase>();

        private bool _initialized = false;

        private YooAssetAllConfig allConfig;

        private YoYoAssetManager() { }

        public async Task Init()
        {
            await InitInternalAsync();
        }

        /// <summary>
        /// 热更新模式初始化
        /// </summary>
        private async Task InitInternalAsync()
        {
            if (_initialized)
                return;

            allConfig = await YoYoConfig.GetConfig<YooAssetAllConfig>(Path.Combine(Application.dataPath, "StreamingAssets", "Config", "YooAssetAllConfig.json"));
            YooAssets.Initialize();

            for (int i = 0; i < allConfig.configs.Count; ++i)
            {
                var name = allConfig.configs[i].PackageName;
                packages[name] = YooAssets.CreatePackage(name);
                if(i==0)
                {
                    _mainpackage = packages[allConfig.configs[0].PackageName];
                    YooAssets.SetDefaultPackage(_mainpackage);
                }
            }
            
            
            // 初始化
            var ops = new List<InitializationOperation>();

            for (int i = 0; i < allConfig.configs.Count; ++i)
            {
                var config = allConfig.configs[i];
                var package = packages[config.PackageName];
                if(config.Offline)
                {
                    var fileSystemParams = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
                    var createParameters = new OfflinePlayModeParameters();
                    createParameters.BuildinFileSystemParameters = fileSystemParams;
                    var initOperation = package.InitializeAsync(createParameters);
                    ops.Add(initOperation);
                }
                else
                {
                    IRemoteServices remoteServices = new RemoteServices(Path.Combine(config.HostServerURL, YoYoTools.PathTools.GetPlatformName(), config.AppVersion).Replace('\\', '/'),
                        Path.Combine(config.FallbackHostServerURL, YoYoTools.PathTools.GetPlatformName(), config.AppVersion).Replace('\\', '/'));
                    var cacheFileSystemParams = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices);
                    var buildinFileSystemParams = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();

                    var createParameters = new HostPlayModeParameters();
                    createParameters.BuildinFileSystemParameters = buildinFileSystemParams;
                    createParameters.CacheFileSystemParameters = cacheFileSystemParams;
                    var initOperation = package.InitializeAsync(createParameters);
                    ops.Add(initOperation);
                }
            }

            // 并发执行（已经开始了）
            // 然后统一等待
            foreach (var op in ops)
            {
                await op.Task;   // YooAsset 提供的 Task 属性
                if (op.Status != EOperationStatus.Succeed)
                    throw new Exception(op.Error);
            }

            for (int i = 0; i < allConfig.configs.Count; ++i)
            {
                var config = allConfig.configs[i];
                var package = packages[config.PackageName];
                if(!config.Offline)
                {
                    // 获取版本
                    var versionOp = package.RequestPackageVersionAsync();
                    await versionOp.Task;

                    if (versionOp.Status != EOperationStatus.Succeed)
                        throw new Exception(versionOp.Error);

                    // 更新清单
                    var manifestOp = package.UpdatePackageManifestAsync(versionOp.PackageVersion);
                    await manifestOp.Task;

                    if (manifestOp.Status != EOperationStatus.Succeed)
                        throw new Exception(manifestOp.Error);

                    ResourceDownloaderOperation _downloader = package.CreateResourceDownloader(5, 3);
                    if (_downloader.TotalDownloadBytes > 0)
                    {
                        Debug.LogError("未达到最新版本");
                        _downloader.CancelDownload();
                        return;
                    }
                    _downloader.CancelDownload();
                }
            }
            
            _initialized = true;
        }

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="location">资源地址</param>
        /// <param name="owner">owner对象</param>
        /// <returns></returns>
        internal async Task<AssetHandle> LoadAssetAsync<T>(string location, YoYoResourceOwner owner, string packageName = null) where T : UnityEngine.Object
        {
            if (owner == null)
            {
                return null;
            }
            if(_mainpackage == null)
            {
                Debug.LogError("未配置主资源包");
                return null;
            }
            if (!_owners.Contains(owner))
            {
                _owners.Add(owner);
            }
            var package = packageName==null? _mainpackage:packages[packageName];
            var handle = package.LoadAssetAsync<T>(location);
            await handle.Task;
            
            if (handle.Status == EOperationStatus.Succeed)
            {
                _handles.Add(handle);
                return handle;
            }
            else
            {
                handle.Release();
                return null;
            }
        }

        /// <summary>
        /// 异步加载原生资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="location"></param>
        /// <param name="owner"></param>
        /// <returns></returns>
        internal async Task<(RawFileHandle handle,byte[] bytes)> LoadRawFileAsync(string location, YoYoResourceOwner owner, string packageName)
        {
            if (owner == null)
            {
                return default;
            }
            if (!_owners.Contains(owner))
            {
                _owners.Add(owner);
            }
            ResourcePackage package = null;
            if (packageName == string.Empty)
            {
                Debug.LogError("需要指定原生资源包名");
                return default;
            }
            else
            {
                package = packages[packageName];
            }
            var handle = package.LoadRawFileAsync(location);
            await handle.Task;
            if (handle.Status == EOperationStatus.Succeed)
            {
                _handles.Add(handle);
                return (handle, handle.GetRawFileData());
            }
            else
            {
                handle.Release();
                return default;
            }
        }

        /// <summary>
        /// 同步加载资源，返回一个资源token
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="location"></param>
        /// <param name="owner"></param>
        /// <returns></returns>
        internal AssetHandle LoadAssetSync<T>(string location, YoYoResourceOwner owner, string packageName = null) where T : UnityEngine.Object
        {
            if (owner == null)
            {
                return default;
            }
            if (_mainpackage == null)
            {
                Debug.LogError("未配置主资源包");
                return null;
            }
            if (!_owners.Contains(owner))
            {
                _owners.Add(owner);
            }
            var package = packageName == null ? _mainpackage : packages[packageName];
            var handle = package.LoadAssetAsync<T>(location);
            if (handle.Status == EOperationStatus.Succeed)
            {
                _handles.Add(handle);
                return handle;
            }
            else
            {
                handle.Release();
                return null;
            }
        }

        /// <summary>
        /// 释放一个AssetHandle
        /// </summary>
        /// <param name="token">HandleToken</param>
        internal void ReleaseHandel(HandleBase handle)
        {
            if(!_handles.Contains(handle))
            {
                Debug.LogError("不存在的handle");
                return;
            }
            handle.Release();
            _handles.Remove(handle);
        }

        /// <summary>
        /// 创建一个资源副本实例
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        internal UnityEngine.Object Instantiate(HandleBase handle, Vector3 position = default, Quaternion rotation = default, Transform parent = null)
        {
            if (!_handles.Contains(handle))
            {
                Debug.LogError("handle");
                return null;
            }
            return (handle as AssetHandle).InstantiateSync(position, rotation, parent);
        }

        /// <summary>
        /// 释放指定owner
        /// </summary>
        /// <param name="owner">释放的owner对象</param>
        /// <param name="force">是否强制销毁该owner的所有资源实例</param>
        public void ReleaseOwner(YoYoResourceOwner owner, bool force = false)
        {
            if (!_owners.Contains(owner))
            {
                Debug.LogError("未记录的Owner");
                return;
            }
            owner.ReleaseAllAsset(force);
            _owners.Remove(owner);
        }

        /// <summary>
        /// 释放当前环境全部资源
        /// </summary>
        /// <param name="force"></param>
        public void ReleaseAllOwner(bool force = false)
        {
            for (int i = _owners.Count - 1; i >= 0; --i)
            {
                _owners[i].ReleaseAllAsset(force);
            }
            _owners.Clear();
        }

        /// <summary>
        /// 完全卸载无引用的资源
        /// </summary>
        /// <returns></returns>
        public async Task UnloadUnusedAssetsAsync()
        {
            var op = _mainpackage.UnloadUnusedAssetsAsync();
            await op.Task;
        }

        public async Task Destroy()
        {
            ReleaseAllOwner(true);
            await UnloadUnusedAssetsAsync();
        }

        /// <summary>
        /// 热更新资源地址桥接类
        /// </summary>
        private class RemoteServices : IRemoteServices
        {
            private string _host;
            private string _fallback;

            public RemoteServices(string host, string fallback)
            {
                _host = host;
                _fallback = fallback;
            }

            public string GetRemoteMainURL(string fileName)
            {
                return _host + "/" + fileName;
            }

            public string GetRemoteFallbackURL(string fileName)
            {
                return _fallback + "/" + fileName;
            }
        }
    }
}
#endif