#if UNITY_EDITOR
using ExcelDataReader;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace YoYo.Editor.DataTool
{
    public class YoYoDataGeneratorWindow : EditorWindow
    {
        private string excelFolderPath = "E:/Project/Unity/YoYoFramework/Sheet";
        private string jsonOutputPath = "E:/Project/Unity/YoYoFramework/SheetJson";
        private string bytesOutputPath = "E:/Project/Unity/YoYoFramework/Assets/StreamingAssets/Data";
        private string codeOutputPath = "E:/Project/Unity/YoYoFramework/Assets/Scripts/YoYoDataReader";

        private List<string> supportTypes = new List<string> { "bool", "byte", "short", "ushort", "int", "uint", "long", "ulong", "float", "double", "string",
        "List<bool>","List<byte>","List<short>","List<ushort>","List<int>","List<uint>","List<long>","List<ulong>","List<float>","List<double>","List<string>","~"};

        [MenuItem("YoYo/Data Generator")]
        public static void ShowWindow()
        {
            GetWindow<YoYoDataGeneratorWindow>("YoYo Data Generator");
        }

        private void OnGUI()
        {
            GUILayout.Label("Excel 表格存放路径", EditorStyles.boldLabel);
            excelFolderPath = EditorGUILayout.TextField(excelFolderPath);

            GUILayout.Space(10);
            GUILayout.Label("JSON 中间文件输出路径", EditorStyles.boldLabel);
            jsonOutputPath = EditorGUILayout.TextField(jsonOutputPath);

            GUILayout.Space(10);
            GUILayout.Label("二进制文件 (.bytes) 输出路径", EditorStyles.boldLabel);
            bytesOutputPath = EditorGUILayout.TextField(bytesOutputPath);

            GUILayout.Space(10);
            GUILayout.Label("运行时数据读取脚本输出路径", EditorStyles.boldLabel);
            codeOutputPath = EditorGUILayout.TextField(codeOutputPath);

            GUILayout.Space(20);

            if (GUILayout.Button("生成 JSON", GUILayout.Height(30)))
            {
                GenerateJsonFiles();
            }

            if (GUILayout.Button("生成二进制文件", GUILayout.Height(30)))
            {
                
            }

            if (GUILayout.Button("生成脚本", GUILayout.Height(30)))
            {
                
            }
        }

        private void GenerateJsonFiles()
        {
            if (!Directory.Exists(excelFolderPath))
            {
                EditorUtility.DisplayDialog("错误", $"Excel 文件夹不存在: {excelFolderPath}", "确定");
                return;
            }

            if (!Directory.Exists(jsonOutputPath))
                Directory.CreateDirectory(jsonOutputPath);

            string[] excelFiles = Directory.GetFiles(excelFolderPath, "*.xlsx");
            if (excelFiles.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", "在指定路径下未找到 .xlsx 文件", "确定");
                return;
            }

            foreach (string filePath in excelFiles)
            {
                if (Path.GetFileName(filePath).StartsWith("~$"))
                {
                    EditorUtility.DisplayDialog("提示", "请先关闭所有表格文件", "确定");
                    return;
                }
            }

            foreach (string filePath in excelFiles)
            {
                GenerateDataTypeJson(filePath);
            }

            AssetDatabase.Refresh();
        }

        public void GenerateDataTypeJson(string filePath)
        {
            List<string> types = new List<string>();
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    var result = reader.AsDataSet();
                    foreach(DataTable table in result.Tables)
                    {
                        types.Clear();
                        string tableName = table.ToString();
                        JObject main = new JObject();
                        JArray t = new JArray();
                        main["types"] = t;
                        for (int i = 0; i < table.Columns.Count; ++i)
                        {
                            string str = table.Rows[0][i]?.ToString() ?? "";
                            if(str == string.Empty)
                            {
                                Debug.LogError($"类型数据不允许为空{fileName}_{tableName}_{i + 1}列");
                                return;
                            }
                            JObject obj = new JObject();
                            string name = str.Split(':')[0];
                            string typestr = str.Split(':')[1];
                            if (!supportTypes.Contains(typestr))
                            {
                                if (!typestr.StartsWith("List<") || !typestr.EndsWith(">"))
                                {
                                    Debug.LogError($"错误的数据类型{typestr}");
                                    return;
                                }
                            }
                            obj["index"] = i;
                            obj["name"] = name;
                            obj["type"] = typestr;
                            t.Add(obj);
                        }
                        CreateFile(jsonOutputPath, fileName + '_' + tableName + "_type.json", main.ToString());
                    }
                }
            }
        }

        public void GenerateDataJson(string filePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    var result = reader.AsDataSet();
                    foreach (DataTable table in result.Tables)
                    {
                        string tableName = table.ToString();
                        List<string> names = new List<string>();
                        List<string> types = new List<string>();
                        JObject main = new JObject();
                        JArray t = new JArray();
                        main["datas"] = t;
                        for (int i = 0; i < table.Rows.Count; ++i)
                        {
                            DataRow row = table.Rows[i];
                            for (int j = 0; j < table.Columns.Count; ++j)
                            {
                                if(i==0)
                                {
                                    names.Add(row[j]?.ToString().Split(':')[0]);
                                    types.Add(row[j]?.ToString().Split(':')[1]);
                                    continue;
                                }
                                string data = row[j]?.ToString() ?? "";
                                if(data == string.Empty)
                                {
                                    continue;
                                }
                                string type = types[j];
                                if(CheckType(type,data))
                                {
                                    JObject obj = new JObject();
                                    obj["name"] = names[j];
                                    obj["data"] = data;
                                    obj["type"] = type;
                                }

                            }
                                
                        }
                        CreateFile(jsonOutputPath, fileName + '_' + tableName + ".json", main.ToString());
                    }
                }
            }
        }

        public bool CheckType(string type, string data)
        {
            if (data == null) return false;

            // 处理基础类型
            if (!type.StartsWith("List<"))
            {
                return ValidateBaseType(type, data);
            }

            string innerType = type.Replace("List<", "").Replace(">", "");
            string[] elements = data.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            // 如果是空字符串且类型是 List，视具体业务逻辑而定，这里返回 true (空列表合法)
            if (string.IsNullOrWhiteSpace(data)) return true;

            return elements.All(e => ValidateBaseType(innerType, e.Trim()));
        }

        private bool ValidateBaseType(string type, string value)
        {
            switch (type.ToLower())
            {
                case "bool": return bool.TryParse(value, out _);
                case "byte": return byte.TryParse(value, out _);
                case "short": return short.TryParse(value, out _);
                case "ushort": return ushort.TryParse(value, out _);
                case "int": return int.TryParse(value, out _);
                case "uint": return uint.TryParse(value, out _);
                case "long": return long.TryParse(value, out _);
                case "ulong": return ulong.TryParse(value, out _);
                case "float": return float.TryParse(value, out _);
                case "double": return double.TryParse(value, out _);
                case "string": return true; // 任何字符串对 string 类型都是合法的
                default: return false; // 不支持的类型
            }
        }

        public static void CreateFile(string directoryPath, string fileName, string content)
        {
            try
            {
                // 1. 如果目录不存在，则创建目录
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // 2. 组合完整的路径（自动处理斜杠问题）
                string fullPath = Path.Combine(directoryPath, fileName);

                // 3. 写入文件（默认使用 UTF-8 编码，如果文件已存在则覆盖）
                File.WriteAllText(fullPath, content, Encoding.UTF8);

                Debug.Log($"文件已成功生成：{fullPath}");
            }
            catch (Exception ex)
            {
                Debug.Log($"生成文件时出错: {ex.Message}");
            }
        }
    }

}
#endif