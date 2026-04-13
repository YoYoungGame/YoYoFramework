using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YoYo.UI;

public class GameEntry : IGameEntry
{
    public void Start()
    {
        YoYoUIManager.Instance.Register<Test1Controller>(YoYoUIType.UI_Test1);
        YoYoUIManager.Instance.Register<Test2Controller>(YoYoUIType.UI_Test2);
        YoYoUIManager.Instance.Register<Test3Controller>(YoYoUIType.UI_Test3);
    }

    public void Update()
    {

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if(YoYoUIManager.Instance.GetUIActive(YoYoUIType.UI_Test1))
            {
                YoYoUIManager.Instance.Hide(YoYoUIType.UI_Test1);
            }
            else
            {
                YoYoUIManager.Instance.ShowAsync(YoYoUIType.UI_Test1, YoYoUILayer.Bottom);
            }
        }
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (YoYoUIManager.Instance.GetUIActive(YoYoUIType.UI_Test2))
            {
                YoYoUIManager.Instance.Hide(YoYoUIType.UI_Test2);
            }
            else
            {
                YoYoUIManager.Instance.ShowAsync(YoYoUIType.UI_Test2, YoYoUILayer.PopUp);
            }
            
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (YoYoUIManager.Instance.GetUIActive(YoYoUIType.UI_Test3))
            {
                YoYoUIManager.Instance.Hide(YoYoUIType.UI_Test3);
            }
            else
            {
                YoYoUIManager.Instance.ShowAsync(YoYoUIType.UI_Test3, YoYoUILayer.Top);
            }
            
        }
    }
}
