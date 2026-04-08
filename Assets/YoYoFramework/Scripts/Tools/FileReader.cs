using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace YoYo.Tools
{
    public static partial class YoYoTools
    {
        public static class FileReader
        {
            /// <summary>
            /// 统一异步读取文本（唯一对外接口）
            /// 支持：PC磁盘 / 移动端APK / StreamingAssets / URL
            /// </summary>
            public static async Task<string> ReadAllTextAsync(string absolutePath)
            {
#if UNITY_EDITOR || UNITY_STANDALONE

                // PC / Editor：优先走本地文件系统
                if (File.Exists(absolutePath))
                {
                    return await Task.Run(() => File.ReadAllText(absolutePath));
                }

                // 如果不是普通文件路径，则尝试当作URL处理（兼容 file:// / http）
                return await ReadByUnityWebRequest(absolutePath);

#else

            // 移动端统一走 UnityWebRequest（兼容 APK / StreamingAssets / URL）
            return await ReadByUnityWebRequest(absolutePath);

#endif
            }

            /// <summary>
            /// UnityWebRequest读取（核心跨平台实现）
            /// </summary>
            private static async Task<string> ReadByUnityWebRequest(string path)
            {
                string url = NormalizePath(path);

                using (var request = UnityWebRequest.Get(url))
                {
                    var op = request.SendWebRequest();

                    while (!op.isDone)
                        await Task.Yield();

#if UNITY_2020_1_OR_NEWER
                    if (request.result != UnityWebRequest.Result.Success)
#else
                if (request.isHttpError || request.isNetworkError)
#endif
                    {
                        Debug.LogError($"[FileReader] 读取失败:\nURL: {url}\nError: {request.error}");
                        return null;
                    }

                    return request.downloadHandler.text;
                }
            }

            /// <summary>
            /// 路径规范化（核心：屏蔽平台差异）
            /// </summary>
            private static string NormalizePath(string path)
            {
                // 已经是URL（直接使用）
                if (path.StartsWith("http://") ||
                    path.StartsWith("https://") ||
                    path.StartsWith("file://") ||
                    path.StartsWith("jar:file://"))
                {
                    return path;
                }

#if UNITY_ANDROID && !UNITY_EDITOR

            // Android：StreamingAssets 在 APK 内（jar 协议）
            if (!Path.IsPathRooted(path))
                return Path.Combine(Application.streamingAssetsPath, path);

            return path;

#elif UNITY_IOS && !UNITY_EDITOR

            // iOS：需要 file:// 前缀
            if (!path.StartsWith("file://"))
                return "file://" + path;

            return path;

#else

                // PC：统一转成 file:// URL（给 UnityWebRequest 用）
                if (Path.IsPathRooted(path))
                    return "file://" + path;

                return "file://" + Path.Combine(Application.streamingAssetsPath, path);

#endif
            }
        }
    }
}
