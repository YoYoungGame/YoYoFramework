#if YoYo_UIModule
using UnityEngine;


namespace YoYo.UI
{
    public abstract class  YoYoUIControllerBase
    {
        public abstract void Init(YoYoUIType type, GameObject root);

        public abstract void Show();

        public abstract void Hide();

        public abstract void Destory();

        public abstract void SetLayer(YoYoUILayer layer);

        public abstract YoYoUILayer GetLayer();

        public abstract YoYoUIType GetUIType();
        public abstract bool GetActive();

        public abstract void SetOrder(int order);
#if !YoYo_AssetModule
        public abstract string GetPrefabPath();
#endif
    }

    public abstract class YoYoUIController<TView, TModel> : YoYoUIControllerBase
    where TView : YoYoUIViewBase, new()
    where TModel : YoYoUIModelBase, new()
    {
        protected YoYoUIType m_type;
        protected YoYoUILayer m_layer;

        protected TView m_view;
        protected TModel m_model;

        public override void Init(YoYoUIType type, GameObject root)
        {
            m_type = type;

            m_view = new TView();
            m_view.Init(root, this);

            m_model = new TModel();
            m_model.Init();
        }

        public override void Show()
        {
            m_view.Show();
            m_model.Show();
        }

        public override void Hide()
        {
            m_view.Hide();
            m_model.Hide();
        }

        public override void Destory()
        {
            m_view.Destory();
            m_model.Destory();
        }

        public override void SetLayer(YoYoUILayer layer)
        {
            m_layer = layer;
        }

        public override YoYoUILayer GetLayer()
        {
            return m_layer;
        }

        public override YoYoUIType GetUIType()
        {
            return m_type;
        }
        public override bool GetActive()
        {
            return m_view.GetActive();
        }

        public override void SetOrder(int order)
        {
            m_view.SetOrder(order);
        }
    }
}
#endif