using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;
using TMPro;
using YoYo.Tools;

namespace YoYo.Tools.JsonUtil
{
    public static class YoYoJsonUtil
    {
        private static JsonSerializerSettings settings = new JsonSerializerSettings
        {
            ObjectCreationHandling = ObjectCreationHandling.Replace
        };
        public static void ObjectToJsonFile<T>(T obj, string path)
        {
            string json = JsonConvert.SerializeObject(obj, Formatting.Indented);
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(path, json);
        }

        public static async Task<T> JsonFileToObject<T>(string path)
        {
            if (!File.Exists(path))
                return default;

            string json = await YoYoTools.YoYoFileReader.ReadAllTextAsync(path);
            return JsonConvert.DeserializeObject<T>(json);
        }

        public static string ObjectToJsonString<T>(T obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }

        public static object JsonStringToObject(string json, System.Type type)
        {
            return JsonConvert.DeserializeObject(json, type, settings);
        }
    }
}