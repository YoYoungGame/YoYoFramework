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
    public class AssetManager
    {
        private static AssetManager _instance;
        public static AssetManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new AssetManager();
                return _instance;
            }
        }

        private ResourcePackage _mainpackage;

        private ResourcePackage _rawpackage;

        private Dictionary<string, ResourcePackage> packages = new Dictionary<string, ResourcePackage>();

        /// <summary>
        /// 全局owner容器
        /// </summary>
        private List<ResourceOwner> _owners = new List<ResourceOwner>();

        private Dictionary<HandleToken, HandleBase> _handles  = new Dictionary<HandleToken, HandleBase>();

        private bool _initialized = false;

        private AssetManager() { }

        /// <summary>
        /// 初始化资源加载系统
        /// </summary>
        /// <param name="MainPackageName">默认资源包名</param>
        /// <param name="host">填写则启用远程热更新功能</param>
        /// <param name="host2">备用地址</param>
        /// <returns></returns>
        public async Task InitAsync(YooAssetAllConfig config)
        {
            await InitInternalAsync(config);
        }

        /// <summary>
        /// 热更新模式初始化
        /// </summary>
        private async Task InitInternalAsync(YooAssetAllConfig allConfig)
        {
            if (_initialized)
                return;

            YooAssets.Initialize();

            for (int i = 0; i < allConfig.configs.Count; ++i)
            {
                var name = allConfig.configs[i].PackageName;
                packages[name] = YooAssets.CreatePackage(name);
            }
            _mainpackage = packages[allConfig.configs[0].PackageName];
            _rawpackage = packages[allConfig.configs[1].PackageName];
            YooAssets.SetDefaultPackage(_mainpackage);
            
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
        internal async Task<HandleToken> LoadAssetAsync<T>(string location, ResourceOwner owner, string packageName = null) where T : UnityEngine.Object
        {
            if (owner == null)
            {
                return default;
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
                HandleToken token = new HandleToken(handle, true);
                _handles[token] = handle;
                return token;
            }
            else
            {
                handle.Release();
                return default;
            }
        }

        /// <summary>
        /// 异步加载原生资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="location"></param>
        /// <param name="owner"></param>
        /// <returns></returns>
        internal async Task<(HandleToken token,byte[] bytes)> LoadRawFileAsync(string location, ResourceOwner owner, string packageName = null)
        {
            if (_rawpackage == null)
            {
                Debug.LogError("未配置原生文件资源包");
                return default;
            }
            if (owner == null)
            {
                return default;
            }
            if (!_owners.Contains(owner))
            {
                _owners.Add(owner);
            }
            var package = packageName == null ? _rawpackage : packages[packageName];
            var handle = package.LoadRawFileAsync(location);
            await handle.Task;
            if (handle.Status == EOperationStatus.Succeed)
            {
                HandleToken token = new HandleToken(handle, true, true);
                _handles[token] = handle;
                return (token, handle.GetRawFileData());
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
        internal HandleToken LoadAssetSync<T>(string location, ResourceOwner owner, string packageName = null) where T : UnityEngine.Object
        {
            if (owner == null)
            {
                return default;
            }
            if (!_owners.Contains(owner))
            {
                _owners.Add(owner);
            }
            var package = packageName == null ? _mainpackage : packages[packageName];
            var handle = package.LoadAssetAsync<T>(location);
            if (handle.Status == EOperationStatus.Succeed)
            {
                HandleToken token = new HandleToken(handle, true);
                _handles[token] = handle;
                return token;
            }
            else
            {
                handle.Release();
                return default;
            }
        }

        /// <summary>
        /// 释放一个AssetHandle
        /// </summary>
        /// <param name="token">HandleToken</param>
        internal void ReleaseHandel(HandleToken token)
        {
            if(!_handles.TryGetValue(token, out var handle))
            {
                Debug.LogError("不存在的HandleToken");
                return;
            }
            handle.Release();
            _handles.Remove(token);
        }

        /// <summary>
        /// 创建一个资源副本实例
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        internal UnityEngine.Object Instantiate(HandleToken token)
        {
            if (token.GetIsRawFile())
            {
                Debug.LogError("原生资源无法实例化");
                return null;
            }
            if (!_handles.TryGetValue(token,out var handle))
            {
                Debug.LogError("不存在的token");
                return null;
            }
            return (handle as AssetHandle).InstantiateSync();
        }

        /// <summary>
        /// 释放指定owner
        /// </summary>
        /// <param name="owner">释放的owner对象</param>
        /// <param name="force">是否强制销毁该owner的所有资源实例</param>
        public void ReleaseOwner(ResourceOwner owner, bool force = false)
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