# YoYoFramework

依赖：
YooAsset
com.unity.nuget.newtonsoft-json



UI管理系统：

每个UI界面需要一个Canvas根节点的UI预制体
在UIManager的UIType枚举中加入一个对应的UI枚举值
写好UI的MVC类
再手动注册一个UIController工厂



资源管理系统：
直接在需要加载资源的地方创建一个ResourcesOwner，然后利用ResourcesOwner的接口进行资源加载和实例化


工具类：

json字符串与类型对象转换器
磁盘文本文件读取器，根据平台自动编译

编辑器功能：