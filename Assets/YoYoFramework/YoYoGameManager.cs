using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using YoYo;

#if YoYo_AssetModule
using YoYo.Asset;
#endif
using YoYo.Config;
using YoYo.Tools;
#if YoYo_UIModule
using YoYo.UI;
#endif
public class YoYoGameManager : MonoBehaviour
{
    private bool InitComplete = false;
    private IGameEntry gameEntry;

    private async void Start()
    {
        InitComplete = false;
        await Init();
        gameEntry.Start();
    }

    public async Task Init()
    {
        try
        {
            gameEntry = await YoYoHotfixLoader.Load();

#if YoYo_AssetModule
            await YoYoAssetManager.Instance.Init();
#endif
            
#if YoYo_UIModule
            YoYoUIManager.Instance.Init();
#endif
            InitComplete = true;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void Update()
    {
        if (InitComplete)
            gameEntry.Update();
    }

    private async void OnDestroy()
    {
        await Destory();
    }

    public async Task Destory()
    {
        try
        {
#if YoYo_AssetModule
            await YoYoAssetManager.Instance.Destroy();
#endif

#if YoYo_UIModule
            YoYoUIManager.Instance.Destory();
#endif
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
}
