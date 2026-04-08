using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


namespace YoYo.UI
{
    //UITYPE名必须与UI Prefab资源名保持一致
    public enum UIType
    {
        None,
        UI_Test1, UI_Test2, UI_Test3, UI_Test4, UI_Test5
    }
    public enum UILayer
    {
        None,
        Bottom,
        PopUp,
        Top
    }
    public class UIManager
    {
        private static readonly UIManager instance = new UIManager();
        public static UIManager Instance => instance;

        private Dictionary<UIType, UIControllerBase> m_uiMap = new();
        private Dictionary<UIType, Func<UIControllerBase>> m_factory = new();

        private Stack<UIControllerBase> m_popupStack = new();
        private UIControllerBase m_bottomLayer;
        private UIControllerBase m_topLayer;

        private GameObject m_maskObj;
        private Canvas m_maskCanvas;
        private ResourceOwner m_owner = new ResourceOwner();
        public void Init()
        {
            m_uiMap.Clear();
            m_factory.Clear();
            m_popupStack.Clear();
            m_bottomLayer = null;
            m_topLayer = null;
            m_owner.ReleaseAll(true);
        }

        public async void ShowAsync(UIType type, UILayer layer)
        {
            try
            {
                UIControllerBase controller = await CreateControllerAsync(type);
                controller.SetLayer(layer);
                switch (layer)
                {
                    case UILayer.Bottom:
                        ShowBottom(controller);
                        break;

                    case UILayer.PopUp:
                        PushPopup(controller);
                        break;

                    case UILayer.Top:
                        ShowTop(controller);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public void Hide(UIType type)
        {
            UIControllerBase controller = GetController(type);
            switch (controller.GetLayer())
            {
                case UILayer.Bottom:
                    HideBottom();
                    break;

                case UILayer.PopUp:
                    HidePopup(type);
                    break;

                case UILayer.Top:
                    HideTop();
                    break;
            }
        }

        public T GetController<T>(UIType type) where T : UIControllerBase
        {
            if(m_uiMap.TryGetValue(type, out var controller))
            {
                return controller as T;
            }
            return null;
        }

        private void ShowBottom(UIControllerBase controller)
        {
            if (m_bottomLayer != null)
            {
                m_bottomLayer.Hide();
            }
            HidePopup(UIType.None);
            m_bottomLayer = controller;
            controller.SetOrder(0);
            controller.Show();
        }

        private void PushPopup(UIControllerBase controller)
        {
            HideTop();
            m_popupStack.Push(controller);
            controller.SetOrder(m_popupStack.Count);
            controller.Show();
        }

        private void ShowTop(UIControllerBase controller)
        {
            if (m_maskObj == null)
            {
                CreateMask();
            }
            if (m_topLayer != null)
            {
                m_topLayer.Hide();
                m_maskObj.SetActive(false);
            }
            m_topLayer = controller;
            m_maskObj.SetActive(true);
            m_maskCanvas.sortingOrder = m_popupStack.Count + 1;
            controller.SetOrder(m_popupStack.Count + 2);
            controller.Show();
        }

        private void HideBottom()
        {
            HidePopup(UIType.None);
            m_bottomLayer.Hide();
            m_bottomLayer = null;
        }

        private void HidePopup(UIType type)
        {
            HideTop();
            if (m_popupStack.Count == 0)
            {
                return;
            }
            while(m_popupStack.Count > 0)
            {
                var top = m_popupStack.Pop();
                top.Hide();
                if(top.GetUIType() == type)
                {
                    break;
                }
            }
        }

        private void HideTop()
        {
            if (m_topLayer == null)
            {
                return ;
            }
            m_maskObj.SetActive(false);
            m_topLayer.Hide();
            m_topLayer = null;
        }

        public void Register<T>(UIType type) where T : UIControllerBase, new()
        {
            m_factory[type] = () => new T();
        }

        private async Task<UIControllerBase> CreateControllerAsync(UIType type)
        {
            if (m_uiMap.TryGetValue(type, out var controller))
                return controller;

            if (!m_factory.TryGetValue(type, out var factory))
            {
                Debug.LogError($"UI未注册: {type}");
                return null;
            }
            controller = factory();
            var asset = await m_owner.LoadAssetAsync<GameObject>(type.ToString());

            if (asset == null)
            {
                Debug.LogError($"Prefab不存在: {type}");
                return null;
            }

            GameObject obj = m_owner.Instantiate(asset);

            controller.Init(type, obj);

            m_uiMap[type] = controller;

            return controller;
        }

        private UIControllerBase GetController(UIType type)
        {
            if (m_uiMap.TryGetValue(type, out var controller))
                return controller;
            Debug.LogError($"{type} is not registered.");
            return null;
        }

        private void CreateMask()
        {
            if (m_maskObj != null)
                return;

            // 创建对象
            m_maskObj = new GameObject("UIMask");

            // RectTransform
            RectTransform rect = m_maskObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Canvas
            m_maskCanvas = m_maskObj.AddComponent<Canvas>();
            m_maskCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            m_maskCanvas.overrideSorting = true;

            // CanvasScaler（Unity默认UI都会有）
            var scaler = m_maskObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

            // Raycaster
            m_maskObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // 透明遮罩
            var image = m_maskObj.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0, 0, 0, 0);
            image.raycastTarget = true;

            m_maskObj.SetActive(false);
        }

        public bool GetUIActive(UIType type)
        {
            if(m_uiMap.TryGetValue(type, out var controller))
            {
                return controller.GetActive();
            }
            return false;
        }
    }
}
