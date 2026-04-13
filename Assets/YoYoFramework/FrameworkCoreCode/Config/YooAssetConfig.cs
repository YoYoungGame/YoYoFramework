using System.Collections.Generic;
using System.IO;
using UnityEngine;
using YooAsset;

namespace YoYo.Config
{
    [System.Serializable]
    public class YooAssetAllConfig : YoYoConfigBase
    {
        [Header("配置列表，一个元素对应一个package，第一个元素固定为YooAssets的默认包")]
        public List<YooAssetConfig> configs = new List<YooAssetConfig>();

        [Header("下载配置")]
        public int DownloadingMaxNum = 10;
        public int FailedTryAgain = 3;
        public int TimeOutSeconds = 10;

        [Header("缓存清理策略")]
        public EFileClearMode CacheClearMode = EFileClearMode.ClearUnusedBundleFiles;

        public override string GetConfigPath()
        {
            return Path.Combine(Application.streamingAssetsPath, "Config", "YooAssetAllConfig.json").Replace('\\', '/');
        }
    }

    public class YooAssetConfig
    {
        public string PackageName;
        public string HostServerURL;
        public string FallbackHostServerURL;
        public string AppVersion = "V1.0.1";
        public bool Offline = false;
    }
}