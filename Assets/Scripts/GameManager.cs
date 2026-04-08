using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using YoYo.Asset;
using YoYo.Config;
using YoYo.Tools;
using YoYo.UI;
public class GameManager : MonoBehaviour
{
    private bool InitComplete = false;
    private YooAssetAllConfig yooassetConfig;
    private IGameEntry gameEntry;

    private void Start()
    {
        InitComplete = false;
        _ = StartAsync();
        
    }

    async Task StartAsync()
    {
        try
        {
            yooassetConfig = await YoYoConfig.GetConfig<YooAssetAllConfig>(Path.Combine(Application.dataPath, "StreamingAssets", "Config", "YooAssetAllConfig.json"));

            await AssetManager.Instance.InitAsync(yooassetConfig);

            gameEntry = await HotfixLoader.Load();

            UIManager.Instance.Init();
            gameEntry.Start();
            InitComplete = true;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(InitComplete)
            gameEntry.Update();
    }
}
