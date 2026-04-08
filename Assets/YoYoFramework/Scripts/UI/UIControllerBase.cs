using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace YoYo.UI
{
    public abstract class UIControllerBase
    {
        public abstract void Init(UIType type, GameObject root);

        public abstract void Show();

        public abstract void Hide();

        public abstract void SetLayer(UILayer layer);

        public abstract UILayer GetLayer();

        public abstract UIType GetUIType();
        public abstract bool GetActive();

        public abstract void SetOrder(int order);
    }

    public abstract class UIController<TView, TModel> : UIControllerBase
    where TView : UIViewBase, new()
    where TModel : UIModelBase, new()
    {
        protected UIType m_type;
        protected UILayer m_layer;

        protected TView m_view;
        protected TModel m_model;

        public override void Init(UIType type, GameObject root)
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

        public override void SetLayer(UILayer layer)
        {
            m_layer = layer;
        }

        public override UILayer GetLayer()
        {
            return m_layer;
        }

        public override UIType GetUIType()
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
