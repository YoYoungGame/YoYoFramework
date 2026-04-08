using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace YoYo.UI
{
    public abstract class UIModelBase
    {
        public virtual void Init() { }

        public virtual void Show() { }

        public virtual void Hide() { }
    }
}
