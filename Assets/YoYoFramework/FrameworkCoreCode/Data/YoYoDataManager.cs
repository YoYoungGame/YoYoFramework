#if YoYo_DataModule
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEditor.Rendering;
using YoYo.Asset;

public class YoYoDataManager
{
    private static YoYoDataManager _instance;
    public static YoYoDataManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = new YoYoDataManager();
            return _instance;
        }
    }

    private Dictionary<Type,AssetToken> assets = new Dictionary<Type,AssetToken>();

    private YoYoResourceOwner owner = new YoYoResourceOwner();

    public async Task<byte[]> LoadData(string fileName,Type type)
    {
        var result = await owner.LoadRawFileASync(Path.Combine("Assets/StreamingAssets",fileName).Replace('\\','/'));
        assets[type] = result.asset;
        return result.bytes;
    }

    public void ReleaseData(Type type)
    {
        if(assets.TryGetValue(type, out var asset))
        {
            owner.ReleaseAsset(asset);
            return;
        }
        UnityEngine.Debug.LogError("无法释放未加载的数据");
    }
}
#endif