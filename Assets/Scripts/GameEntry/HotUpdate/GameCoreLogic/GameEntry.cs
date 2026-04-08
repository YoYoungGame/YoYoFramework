using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YoYo.UI;

public class GameEntry : IGameEntry
{
    public void Start()
    {
        UIManager.Instance.Register<Test1Controller>(UIType.UI_Test1);
        UIManager.Instance.Register<Test2Controller>(UIType.UI_Test2);
        UIManager.Instance.Register<Test3Controller>(UIType.UI_Test3);
    }

    public void Update()
    {

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if(UIManager.Instance.GetUIActive(UIType.UI_Test1))
            {
                UIManager.Instance.Hide(UIType.UI_Test1);
            }
            else
            {
                UIManager.Instance.ShowAsync(UIType.UI_Test1, UILayer.Bottom);
            }
        }
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (UIManager.Instance.GetUIActive(UIType.UI_Test2))
            {
                UIManager.Instance.Hide(UIType.UI_Test2);
            }
            else
            {
                UIManager.Instance.ShowAsync(UIType.UI_Test2, UILayer.PopUp);
            }
            
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (UIManager.Instance.GetUIActive(UIType.UI_Test3))
            {
                UIManager.Instance.Hide(UIType.UI_Test3);
            }
            else
            {
                UIManager.Instance.ShowAsync(UIType.UI_Test3, UILayer.Top);
            }
            
        }
    }
}
