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
        public static class YoYoFileReader
        {
            #region 文本读取接口 (String)

            /// <summary>
            /// 统一异步读取文本
            /// </summary>
            public static async Task<string> ReadAllTextAsync(string absolutePath)
            {
#if UNITY_EDITOR || UNITY_STANDALONE
                if (File.Exists(absolutePath))
                {
                    return await Task.Run(() => File.ReadAllText(absolutePath));
                }
                return await ReadByUnityWebRequest(absolutePath, true) as string;
#else
        return await ReadByUnityWebRequest(absolutePath, true) as string;
#endif
            }

            #endregion

            #region 数据流读取接口 (Byte Array)

            /// <summary>
            /// 统一异步读取二进制数据流
            /// </summary>
            public static async Task<byte[]> ReadAllBytesAsync(string absolutePath)
            {
#if UNITY_EDITOR || UNITY_STANDALONE
                if (File.Exists(absolutePath))
                {
                    // 使用 Task.Run 避免在大文件读取时卡住主线程
                    return await Task.Run(() => File.ReadAllBytes(absolutePath));
                }
                return await ReadByUnityWebRequest(absolutePath, false) as byte[];
#else
        return await ReadByUnityWebRequest(absolutePath, false) as byte[];
#endif
            }

            #endregion

            #region 私有核心实现

            /// <summary>
            /// UnityWebRequest核心跨平台实现
            /// </summary>
            /// <param name="asText">true 返回 string, false 返回 byte[]</param>
            private static async Task<object> ReadByUnityWebRequest(string path, bool asText)
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

                    if (asText)
                        return request.downloadHandler.text;
                    else
                        return request.downloadHandler.data;
                }
            }

            /// <summary>
            /// 路径规范化
            /// </summary>
            private static string NormalizePath(string path)
            {
                if (path.StartsWith("http://") ||
                    path.StartsWith("https://") ||
                    path.StartsWith("file://") ||
                    path.StartsWith("jar:file://"))
                {
                    return path;
                }

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Path.IsPathRooted(path))
            return Path.Combine(Application.streamingAssetsPath, path);
        return path;
#elif UNITY_IOS && !UNITY_EDITOR
        if (!path.StartsWith("file://"))
            return "file://" + path;
        return path;
#else
                if (Path.IsPathRooted(path))
                    return "file://" + path;

                return "file://" + Path.Combine(Application.streamingAssetsPath, path);
#endif
            }

            #endregion
        }
    }
}
