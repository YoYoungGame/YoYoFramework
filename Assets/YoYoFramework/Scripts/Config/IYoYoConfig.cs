using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using YoYo.Tools.JsonUtil;
using YoYo.Tools;

namespace YoYo.Config
{
    public interface IYoYoConfig
    {
        string ObjectToJson();
        void JsonToObject(string json);
    }

    [System.Serializable]
    public abstract class YoYoConfigBase : IYoYoConfig
    {
        /// <summary>
        /// 类型对象转Json字符串
        /// </summary>
        public virtual string ObjectToJson()
        {
            return YoYoJsonUtil.ObjectToJsonString(this);
        }

        /// <summary>
        /// Json字符串转类型对象
        /// </summary>
        public virtual void JsonToObject(string json)
        {
            if (string.IsNullOrEmpty(json)) return;

            // 1. 反序列化成新对象
            var newObj = (YoYoConfigBase)YoYoJsonUtil.JsonStringToObject(json, this.GetType());

            // 2. 将所有字段值覆盖到当前对象
            foreach (var field in this.GetType().GetFields())
            {
                field.SetValue(this, field.GetValue(newObj));
            }
        }
    }

    public static class YoYoConfig
    {
        public static async Task<T> GetConfig<T>(string configPath) where T : YoYoConfigBase, new() 
        {
            string json = await YoYoTools.FileReader.ReadAllTextAsync(configPath);
            if (json == string.Empty)
            {
                Debug.LogError("配置文件读取失败");
                return null;
            }
            T obj = new T();
            obj.JsonToObject(json);
            return obj;
        }
    }
}