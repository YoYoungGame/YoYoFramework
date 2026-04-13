#if YoYo_HybridCLR
using System;
using System.Reflection;
using System.Threading.Tasks;
#if YoYo_AssetModule
using YoYo.Asset;
#endif

public static class YoYoHotfixLoader
{
#if YoYo_AssetModule
    private static YoYoResourceOwner owner = new YoYoResourceOwner();
#endif
    public static async Task<IGameEntry> Load()
    {
        // 1. 加载AOT元数据
        LoadMetadata();

#if UNITY_EDITOR
        var dllbytes = LoadHotfixDll();
        var pdbbytes = LoadHotfixPDB();
        await Task.WhenAll(dllbytes, pdbbytes);
        Assembly assembly = Assembly.Load(dllbytes.Result,pdbbytes.Result);
#else
        byte[] dllbytes = await LoadHotfixDll();
        Assembly assembly = Assembly.Load(dllbytes);
#endif

        // 4. 创建入口
        Type type = assembly.GetType("GameEntry");
        object instance = Activator.CreateInstance(type);

        // 5. 转接口
        return (IGameEntry)instance;
    }

    static void LoadMetadata()
    {
        
    }

    static async Task<byte[]> LoadHotfixDll()
    {
#if YoYo_AssetModule
        var result = await owner.LoadRawFileASync("Assets/HybridCLRHotUpdateDll/HotUpdate.dll.bytes");
        byte[] data = result.bytes;
        owner.ReleaseAsset(result.asset);
        return result.bytes;
#else
        byte[] data = await YoYoTools.FileReader.ReadAllBytesAsync("Assets/HybridCLRHotUpdateDll/HotUpdate.dll.bytes");
        return data;
#endif
    }

    static async Task<byte[]> LoadHotfixPDB()
    {
#if YoYo_AssetModule
        var result = await owner.LoadRawFileASync("Assets/HybridCLRHotUpdateDll/HotUpdate.pdb.bytes");
        byte[] data = result.bytes;
        owner.ReleaseAsset(result.asset);
        return result.bytes;
#else
        byte[] data = await YoYoTools.FileReader.ReadAllBytesAsync("Assets/HybridCLRHotUpdateDll/HotUpdate.pdb.bytes");
        return data;
#endif
    }
}
#endif