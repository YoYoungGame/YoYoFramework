using UnityEngine.UI;
using YoYo.UI;

public class Test1View : UIViewBase
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
}
public class Test1Model : UIModelBase
{

}
public class Test1Controller : UIController<Test1View, Test1Model>
{

}

public class Test2View : UIViewBase
{
    protected override void OnInit() { }

    protected override void OnShow() { }

    protected override void OnHide() { }
}
public class Test2Model : UIModelBase
{

}
public class Test2Controller : UIController<Test2View, Test2Model>
{

}

public class Test3View : UIViewBase
{
    protected override void OnInit() { }

    protected override void OnShow() { }

    protected override void OnHide() { }
}
public class Test3Model : UIModelBase
{

}
public class Test3Controller : UIController<Test3View, Test3Model>
{

}