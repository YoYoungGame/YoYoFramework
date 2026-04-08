using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


namespace YoYo.UI
{
    public abstract class UIViewBase
    {
        private GameObject m_root;
        protected UIControllerBase m_controller;
        protected Canvas m_canvas;

        public virtual void Init(GameObject root, UIControllerBase controller)
        {
            m_root = root;
            m_controller = controller;
            m_canvas = m_root.GetComponent<Canvas>();
            OnInit();
        }

        protected abstract void OnInit();

        public virtual void Show()
        {
            m_root.SetActive(true);
            OnShow();
        }

        protected abstract void OnShow();

        public virtual void Hide()
        {
            OnHide();
            m_root.SetActive(false);
        }

        protected abstract void OnHide();

        protected T Find<T>(string path) where T : Component
        {
            return m_root.transform.Find(path).GetComponent<T>();
        }

        public bool GetActive()
        {
            return m_root.activeSelf;
        }

        public void SetOrder(int order)
        {
            m_canvas.sortingOrder = order;
        }

        public int GetOrder()
        {
            return m_canvas.sortingOrder;
        }
    }
}
