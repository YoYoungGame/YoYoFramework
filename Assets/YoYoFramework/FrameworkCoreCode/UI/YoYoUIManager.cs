#if YoYo_UIModule
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using YoYo.Asset;


namespace YoYo.UI
{
    //UITYPE名必须与UI Prefab资源名保持一致
    public enum YoYoUIType
    {
        None,
        UI_Test1, UI_Test2, UI_Test3, UI_Test4, UI_Test5
    }
    public enum YoYoUILayer
    {
        None,
        Bottom,
        PopUp,
        Top
    }
    public class YoYoUIManager
    {
        private static YoYoUIManager _instance;
        public static YoYoUIManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new YoYoUIManager();
                return _instance;
            }
        }

        private Dictionary<YoYoUIType, YoYoUIControllerBase> m_uiMap = new();
        private Dictionary<YoYoUIType, Func<YoYoUIControllerBase>> m_factory = new();

        private Stack<YoYoUIControllerBase> m_popupStack = new();
        private YoYoUIControllerBase m_bottomLayer;
        private YoYoUIControllerBase m_topLayer;

        private GameObject m_maskObj;
        private GameObject m_root;
        private Canvas m_maskCanvas;
#if YoYo_AssetModule
        private YoYoResourceOwner m_owner = new YoYoResourceOwner();
#endif
        public void Init()
        {
            m_uiMap.Clear();
            m_factory.Clear();
            m_popupStack.Clear();
            m_bottomLayer = null;
            m_topLayer = null;
#if YoYo_AssetModule
            m_owner.ReleaseAll(true);
#endif
        }

        public async void ShowAsync(YoYoUIType type, YoYoUILayer layer)
        {
            try
            {
                YoYoUIControllerBase controller = await CreateControllerAsync(type);
                controller.SetLayer(layer);
                switch (layer)
                {
                    case YoYoUILayer.Bottom:
                        ShowBottom(controller);
                        break;

                    case YoYoUILayer.PopUp:
                        PushPopup(controller);
                        break;

                    case YoYoUILayer.Top:
                        ShowTop(controller);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public void Hide(YoYoUIType type)
        {
            YoYoUIControllerBase controller = GetController(type);
            switch (controller.GetLayer())
            {
                case YoYoUILayer.Bottom:
                    HideBottom();
                    break;

                case YoYoUILayer.PopUp:
                    HidePopup(type);
                    break;

                case YoYoUILayer.Top:
                    HideTop();
                    break;
            }
        }

        public T GetController<T>(YoYoUIType type) where T : YoYoUIControllerBase
        {
            if(m_uiMap.TryGetValue(type, out var controller))
            {
                return controller as T;
            }
            return null;
        }

        private void ShowBottom(YoYoUIControllerBase controller)
        {
            if (m_bottomLayer != null)
            {
                m_bottomLayer.Hide();
            }
            HidePopup(YoYoUIType.None);
            m_bottomLayer = controller;
            controller.SetOrder(0);
            controller.Show();
        }

        private void PushPopup(YoYoUIControllerBase controller)
        {
            HideTop();
            m_popupStack.Push(controller);
            controller.SetOrder(m_popupStack.Count);
            controller.Show();
        }

        private void ShowTop(YoYoUIControllerBase controller)
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
            HidePopup(YoYoUIType.None);
            m_bottomLayer.Hide();
            m_bottomLayer = null;
        }

        private void HidePopup(YoYoUIType type)
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

        public void Register<T>(YoYoUIType type) where T : YoYoUIControllerBase, new()
        {
            m_factory[type] = () => new T();
        }

        private async Task<YoYoUIControllerBase> CreateControllerAsync(YoYoUIType type)
        {
            if(m_root == null)
            {
                m_root = new GameObject("UIRoot");
            }
            if (m_uiMap.TryGetValue(type, out var controller))
                return controller;

            if (!m_factory.TryGetValue(type, out var factory))
            {
                Debug.LogError($"UI未注册: {type}");
                return null;
            }
            controller = factory();
#if YoYo_AssetModule
            var asset = await m_owner.LoadAssetAsync<GameObject>(type.ToString());
#else
            var asset = Resources.Load<GameObject>(controller.GetPrefabPath());
            if (asset == null)
            {
                Debug.LogError($"Prefab不存在: {type}");
                return null;
            }
#endif
#if YoYo_AssetModule
            GameObject obj = m_owner.Instantiate(asset, Vector3.zero, Quaternion.identity, m_root.transform);
#else
            GameObject obj = GameObject.Instantiate(asset,m_root);
#endif
            controller.Init(type, obj);

            m_uiMap[type] = controller;

            return controller;
        }

        private YoYoUIControllerBase GetController(YoYoUIType type)
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

        public bool GetUIActive(YoYoUIType type)
        {
            if(m_uiMap.TryGetValue(type, out var controller))
            {
                return controller.GetActive();
            }
            return false;
        }

        public void HideAll()
        {
            m_root.SetActive(false);
        }

        public void ShowAll()
        {
            m_root.SetActive(true);
        }
        public void Destory()
        {
            foreach(var controller in m_uiMap.Values)
            {
                controller.Hide();
                controller.Destory();
            }
            m_popupStack.Clear();
            m_uiMap.Clear();
            m_factory.Clear();
        }
    }
}
#endif