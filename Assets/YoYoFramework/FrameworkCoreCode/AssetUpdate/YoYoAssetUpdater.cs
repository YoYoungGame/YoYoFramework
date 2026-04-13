#if YoYo_AssetModule
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;
using YoYo.Config;
using YoYo.Tools;

public interface IUpdaterListener
{
    void InitializeFailCallBack();
    void GetRemoteInfoFailCallBack();
    void OnGetNewUpdateCallBack(float sizeMB);
    void OnDownloadUpdateCallBack(DownloadUpdateData data);
    void OnDownloadFailCallBack(string msg);
    void OnDownloadFinishCallBack(bool success);
    void OnDestoryYooAssetCallBack();
}

public class YoYoAssetUpdater : MonoBehaviour, IUpdaterListener
{
    public YooAssetAllConfig allConfig;
    public UpdateController controller;
    public bool updating = false;

    private void Start()
    {
        if (updating)
        {
            Debug.LogError("正在更新");
            return;
        }
        updating = true;
        _ = Init();
    }

    private async Task Init()
    {
        allConfig = await YoYoConfig.GetConfig<YooAssetAllConfig>(
            Path.Combine(Application.dataPath, "StreamingAssets", "Config", "YooAssetAllConfig.json"));

        if (controller == null)
        {
            controller = new UpdateController();
        }
        controller.SetListener(this);
        await controller.StartAsync();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _ = controller.StartDownloadAsync();
        }
    }

    public void InitializeFailCallBack() => Debug.Log("初始化失败");
    public void GetRemoteInfoFailCallBack() => Debug.Log("获取更新信息失败");

    public void OnGetNewUpdateCallBack(float sizeMB)
    {
        Debug.Log($"获取到新的资源本 {sizeMB}MB");
    }

    public void OnDownloadUpdateCallBack(DownloadUpdateData data)
    {
        Debug.Log($"下载进度：{(float)Math.Round((double)data.CurrentDownloadBytes / data.TotalDownloadBytes, 2) * 100} %");
    }

    public void OnDownloadFailCallBack(string msg)
    {
        Debug.Log("下载失败，检查网络状态/n" + msg);
    }

    public void OnDownloadFinishCallBack(bool success)
    {
        updating = false;
        Debug.Log(success ? "更新完成" : "更新失败");
    }

    public void OnDestoryYooAssetCallBack()
    {
        SceneManager.LoadScene(1);
    }
}

public class UpdateController
{
    private Dictionary<string, ResourcePackage> _packages = new Dictionary<string, ResourcePackage>();
    private List<ResourceDownloaderOperation> downLoaderList = new List<ResourceDownloaderOperation>();
    private YoYoAssetUpdater _updater;
    private bool hasError = false;

    public void SetListener(YoYoAssetUpdater updater)
    {
        _updater = updater;
    }

    public async Task StartAsync()
    {
        await CheckUpdateAsync();
    }

    private async Task CheckUpdateAsync()
    {
        if (_updater == null)
        {
            Debug.LogError("更新错误");
            return;
        }

        YooAssets.Initialize();
        var ops = new List<InitializationOperation>();

        for (int i = 0; i < _updater.allConfig.configs.Count; ++i)
        {
            YooAssetConfig config = _updater.allConfig.configs[i];
            if (config.Offline)
                continue;

            var package = YooAssets.CreatePackage(config.PackageName);
            _packages[config.PackageName] = package;

            IRemoteServices remoteServices = new RemoteServices(
                Path.Combine(config.HostServerURL, YoYoTools.PathTools.GetPlatformName(), config.AppVersion).Replace('\\', '/'),
                Path.Combine(config.FallbackHostServerURL, YoYoTools.PathTools.GetPlatformName(), config.AppVersion).Replace('\\', '/'));

            var cacheFileSystemParams = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices);
            var buildinFileSystemParams = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();

            var createParameters = new HostPlayModeParameters();
            createParameters.BuildinFileSystemParameters = buildinFileSystemParams;
            createParameters.CacheFileSystemParameters = cacheFileSystemParams;

            var initOperation = package.InitializeAsync(createParameters);
            ops.Add(initOperation);
        }

        foreach (var op in ops)
        {
            await op.Task;
            if (op.Status != EOperationStatus.Succeed)
            {
                Debug.LogError(op.Error);
                _updater?.InitializeFailCallBack();
                return;
            }
        }

        long totalUpdateBytes = 0;

        for (int i = 0; i < _updater.allConfig.configs.Count; ++i)
        {
            var config = _updater.allConfig.configs[i];
            var package = _packages[config.PackageName];

            if (!config.Offline)
            {
                var versionOp = package.RequestPackageVersionAsync();
                await versionOp.Task;

                if (versionOp.Status != EOperationStatus.Succeed)
                {
                    Debug.LogError(versionOp.Error);
                    _updater?.GetRemoteInfoFailCallBack();
                    return;
                }

                var manifestOp = package.UpdatePackageManifestAsync(versionOp.PackageVersion);
                await manifestOp.Task;

                if (manifestOp.Status != EOperationStatus.Succeed)
                {
                    Debug.LogError(manifestOp.Error);
                    _updater?.GetRemoteInfoFailCallBack();
                    return;
                }

                var downloader = package.CreateResourceDownloader(5, 3);
                if (downloader.TotalDownloadBytes > 0)
                {
                    totalUpdateBytes += downloader.TotalDownloadBytes;
                    downLoaderList.Add(downloader);
                }
            }
        }

        if (totalUpdateBytes == 0)
        {
            Debug.Log("无需更新");

            await ClearCacheAsync();

            _updater?.OnDownloadFinishCallBack(true);
            return;
        }

        float sizeMB = totalUpdateBytes / 1024f / 1024f;
        _updater?.OnGetNewUpdateCallBack(sizeMB);
    }

    public async Task StartDownloadAsync()
    {
        hasError = false;

        if (downLoaderList.Count == 0)
        {
            Debug.LogError("下载器丢失");
            hasError = true;
            return;
        }

        foreach (var downloader in downLoaderList)
        {
            downloader.DownloadErrorCallback = OnDownloadError;
            downloader.DownloadUpdateCallback = OnDownloadUpdate;
        }

        downLoaderList[^1].DownloadFinishCallback = OnDownloadFinish;

        await DownloadAsync();
    }

    private async Task DownloadAsync()
    {
        for (int i = 0; i < downLoaderList.Count; ++i)
        {
            var downloader = downLoaderList[i];
            downloader.BeginDownload();

            float lastTime = Time.time;
            long lastBytes = 0;
            bool timeout = false;

            while (!downloader.IsDone)
            {
                if (downloader.CurrentDownloadBytes != lastBytes)
                {
                    lastBytes = downloader.CurrentDownloadBytes;
                    lastTime = Time.time;
                }

                if (Time.time - lastTime > _updater.allConfig.TimeOutSeconds)
                {
                    timeout = true;
                    break;
                }

                await Task.Yield();
            }

            if (downloader.Status != EOperationStatus.Succeed)
                hasError = true;

            if (timeout)
            {
                downloader.CancelDownload();
                hasError = true;
                _updater?.OnDownloadFailCallBack("超时");
                return;
            }
        }

        await ClearCacheAsync();
    }

    private void OnDownloadFinish(DownloaderFinishData data)
    {
        if (_updater != null && !hasError)
            _updater.OnDownloadFinishCallBack(data.Succeed);
        else
            Debug.LogError("下载出现错误");
    }

    private void OnDownloadError(DownloadErrorData data)
    {
        _updater?.OnDownloadFailCallBack(data.FileName + " : " + data.ErrorInfo);
    }

    private void OnDownloadUpdate(DownloadUpdateData data)
    {
        _updater?.OnDownloadUpdateCallBack(data);
    }

    private async Task ClearCacheAsync()
    {
        foreach (var downloader in downLoaderList)
        {
            if (!downloader.IsDone)
                downloader.CancelDownload();
        }

        downLoaderList.Clear();

        foreach (var package in _packages.Values)
        {
            var op = package.ClearCacheFilesAsync(_updater.allConfig.CacheClearMode);
            await op.Task;

            if (op.Status != EOperationStatus.Succeed)
                Debug.LogError(op.Error);

            if (package != null)
            {
                await package.DestroyAsync().Task;
                YooAssets.RemovePackage(package);
            }
        }

        _packages.Clear();
        YooAssets.SetDefaultPackage(null);

        var op2 = Resources.UnloadUnusedAssets();
        while (!op2.isDone)
        {
            await Task.Yield();
        }
        GC.Collect();

        Debug.Log("YooAsset 已彻底销毁");

        _updater?.OnDestoryYooAssetCallBack();
    }

    private class RemoteServices : IRemoteServices
    {
        private string _main;
        private string _fallback;

        public RemoteServices(string main, string fallback)
        {
            _main = main;
            _fallback = fallback;
        }

        public string GetRemoteMainURL(string fileName) => _main + "/" + fileName;
        public string GetRemoteFallbackURL(string fileName) => _fallback + "/" + fileName;
    }
}
#endif