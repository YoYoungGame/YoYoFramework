#if YoYo_UIModule

namespace YoYo.UI
{
    public abstract class YoYoUIModelBase
    {
        public virtual void Init() { }

        public virtual void Show() { }

        public virtual void Hide() { }

        public virtual void Destory() { }
    }
}
#endif