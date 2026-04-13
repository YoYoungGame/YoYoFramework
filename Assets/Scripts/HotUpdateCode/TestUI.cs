using UnityEngine.UI;
using YoYo.UI;

public class Test1View : YoYoUIViewBase
{
    private Button btn;
    private Text txt;
    protected override void OnInit()
    {
        btn = Find<Button>("Panel/Button (Legacy)");
        txt = Find<Text>("Panel/Text (Legacy)");
        if(btn != null)
        {
            btn.onClick.AddListener(OnClickBtn);
        }
    }

    private void OnClickBtn()
    {
        txt.text = "代码热更新成功！！！";
    }
    protected override void OnShow() { }

    protected override void OnHide() { }

    protected override void OnDestory() { }
}
public class Test1Model : YoYoUIModelBase
{

}
public class Test1Controller : YoYoUIController<Test1View, Test1Model>
{

}

public class Test2View : YoYoUIViewBase
{
    protected override void OnInit() { }

    protected override void OnShow() { }

    protected override void OnHide() { }

    protected override void OnDestory() { }
}
public class Test2Model : YoYoUIModelBase
{

}
public class Test2Controller : YoYoUIController<Test2View, Test2Model>
{

}

public class Test3View : YoYoUIViewBase
{
    protected override void OnInit() { }

    protected override void OnShow() { }

    protected override void OnHide() { }

    protected override void OnDestory() { }
}
public class Test3Model : YoYoUIModelBase
{

}
public class Test3Controller : YoYoUIController<Test3View, Test3Model>
{

}