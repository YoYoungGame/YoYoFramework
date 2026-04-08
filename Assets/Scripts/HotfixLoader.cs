using System;
using System.Data.SqlTypes;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using YoYo.Asset;
using YoYo.Config;
using YoYo.Tools;

public static class HotfixLoader
{
    private static ResourceOwner owner = new ResourceOwner();
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
        var result = await owner.LoadRawFileASync("Assets/HybridCLRHotUpdateDll/HotUpdate.dll.bytes");
        byte[] data = result.bytes;
        owner.ReleaseAsset(result.asset);
        return result.bytes;
    }

    static async Task<byte[]> LoadHotfixPDB()
    {
        var result = await owner.LoadRawFileASync("Assets/HybridCLRHotUpdateDll/HotUpdate.pdb.bytes");
        byte[] data = result.bytes;
        owner.ReleaseAsset(result.asset);
        return result.bytes;
    }
}