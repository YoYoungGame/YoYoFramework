using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace YoYo.Tools
{
    public static partial class YoYoTools
    {
        public class PathTools
        {
            public static string GetPlatformName()
            {
                switch (UnityEngine.Application.platform)
                {
                    case UnityEngine.RuntimePlatform.WindowsEditor:
                    case UnityEngine.RuntimePlatform.WindowsPlayer:
                        return "StandaloneWindows64";

                    case UnityEngine.RuntimePlatform.Android:
                        return "Android";

                    case UnityEngine.RuntimePlatform.IPhonePlayer:
                        return "iOS";

                    case UnityEngine.RuntimePlatform.WebGLPlayer:
                        return "WebGL";

                    case UnityEngine.RuntimePlatform.OSXEditor:
                    case UnityEngine.RuntimePlatform.OSXPlayer:
                        return "StandaloneOSX";

                    default:
                        throw new System.Exception($"Unsupported platform: {UnityEngine.Application.platform}");
                }
            }
        }
    }
}
