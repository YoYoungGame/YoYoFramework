using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using YoYo.Config;
using YoYo.Tools.JsonUtil;

namespace YoYo.Editor.Config
{
    public class YoYoConfigEditorWindow : EditorWindow
    {
        private Type selectedConfigType;
        private YoYoConfigBase currentConfig;
        private Vector2 scrollPos;

        // 折叠状态缓存
        private Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();

        // 反射缓存
        private static Dictionary<Type, FieldInfo[]> fieldCache = new Dictionary<Type, FieldInfo[]>();

        [MenuItem("YoYo/Config Setting")]
        public static void OpenWindow()
        {
            GetWindow<YoYoConfigEditorWindow>("YoYo Config Editor");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();

            var configTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsSubclassOf(typeof(YoYoConfigBase)) && !t.IsAbstract)
                .ToArray();

            string[] typeNames = configTypes.Select(t => t.Name).ToArray();
            int selectedIndex = selectedConfigType == null ? -1 : Array.IndexOf(configTypes, selectedConfigType);
            selectedIndex = EditorGUILayout.Popup("Config Type:", selectedIndex, typeNames);

            if (selectedIndex >= 0 && selectedIndex < configTypes.Length)
            {
                if (selectedConfigType != configTypes[selectedIndex])
                {
                    selectedConfigType = configTypes[selectedIndex];
                    currentConfig = (YoYoConfigBase)Activator.CreateInstance(selectedConfigType);
                }
            }

            // ===== 显示路径（只读）=====
            if (currentConfig != null)
            {
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Config Path:");
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField(GetFullPath(currentConfig));
                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.Space();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            if (currentConfig != null)
            {
                currentConfig = (YoYoConfigBase)DrawObject(currentConfig.GetType(), currentConfig, "Root");
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("读取Json"))
                LoadJson();

            if (GUILayout.Button("生成Json"))
                SaveJson();
            EditorGUILayout.EndHorizontal();
        }

        // ================== 核心入口 ==================

        private object DrawValue(Type type, object value, string label)
        {
            if (type == typeof(int))
                return EditorGUILayout.IntField(label, value == null ? 0 : (int)value);

            if (type == typeof(float))
                return EditorGUILayout.FloatField(label, value == null ? 0f : (float)value);

            if (type == typeof(string))
                return EditorGUILayout.TextField(label, value == null ? "" : (string)value);

            if (type == typeof(bool))
                return EditorGUILayout.Toggle(label, value != null && (bool)value);

            if (type.IsEnum)
            {
                if (value == null)
                    value = Activator.CreateInstance(type);

                return EditorGUILayout.EnumPopup(label, (Enum)value);
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                return DrawList(type, value, label);

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                return DrawDictionary(type, value, label);

            return DrawObject(type, value, label);
        }

        // ================== Object ==================

        private object DrawObject(Type type, object value, string label)
        {
            if (value == null)
                value = Activator.CreateInstance(type);

            string key = type.FullName + label;

            if (!foldoutStates.ContainsKey(key))
                foldoutStates[key] = true;

            foldoutStates[key] = EditorGUILayout.Foldout(foldoutStates[key], label, true);

            if (!foldoutStates[key])
                return value;

            EditorGUI.indentLevel++;

            var fields = GetFields(type);

            foreach (var field in fields)
            {
                if (field.IsDefined(typeof(HideInInspector), true))
                    continue;

                var header = field.GetCustomAttribute<HeaderAttribute>();
                if (header != null)
                    EditorGUILayout.LabelField(header.header, EditorStyles.boldLabel);

                var tooltip = field.GetCustomAttribute<TooltipAttribute>();
                GUIContent labelContent = new GUIContent(field.Name, tooltip?.tooltip);

                object fieldValue = field.GetValue(value);

                object newValue = DrawValue(field.FieldType, fieldValue, labelContent.text);

                field.SetValue(value, newValue);
            }

            EditorGUI.indentLevel--;

            return value;
        }

        // ================== List ==================

        private object DrawList(Type listType, object value, string label)
        {
            var elementType = listType.GetGenericArguments()[0];
            var list = value as IList ?? (IList)Activator.CreateInstance(listType);

            string key = "List_" + label;

            if (!foldoutStates.ContainsKey(key))
                foldoutStates[key] = true;

            foldoutStates[key] = EditorGUILayout.Foldout(foldoutStates[key], label, true);

            if (!foldoutStates[key])
                return list;

            EditorGUI.indentLevel++;

            int count = Mathf.Max(0, EditorGUILayout.IntField("Size", list.Count));

            while (list.Count < count)
                list.Add(elementType.IsValueType ? Activator.CreateInstance(elementType) : null);

            while (list.Count > count)
                list.RemoveAt(list.Count - 1);

            for (int i = 0; i < list.Count; i++)
            {
                list[i] = DrawValue(elementType, list[i], $"Element {i}");
            }

            EditorGUI.indentLevel--;

            return list;
        }

        // ================== Dictionary ==================

        private object DrawDictionary(Type dictType, object value, string label)
        {
            var dict = value as IDictionary ?? (IDictionary)Activator.CreateInstance(dictType);

            Type keyType = dictType.GetGenericArguments()[0];
            Type valueType = dictType.GetGenericArguments()[1];

            if (keyType != typeof(string))
            {
                EditorGUILayout.HelpBox($"{label} 仅支持 string key", MessageType.Warning);
                return dict;
            }

            string foldKey = "Dict_" + label;

            if (!foldoutStates.ContainsKey(foldKey))
                foldoutStates[foldKey] = true;

            foldoutStates[foldKey] = EditorGUILayout.Foldout(foldoutStates[foldKey], label, true);

            if (!foldoutStates[foldKey])
                return dict;

            EditorGUI.indentLevel++;

            List<string> keys = dict.Keys.Cast<string>().ToList();
            int removeIndex = -1;

            for (int i = 0; i < keys.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                string oldKey = keys[i];
                string newKey = EditorGUILayout.TextField(oldKey, GUILayout.Width(150));

                object val = dict[oldKey];
                val = DrawValue(valueType, val, "");

                dict.Remove(oldKey);
                if (!string.IsNullOrEmpty(newKey))
                    dict[newKey] = val;

                if (GUILayout.Button("X", GUILayout.Width(20)))
                    removeIndex = i;

                EditorGUILayout.EndHorizontal();
            }

            if (removeIndex >= 0)
                dict.Remove(keys[removeIndex]);

            if (GUILayout.Button("添加条目"))
            {
                string newKey = "NewKey";
                int suffix = 1;
                while (dict.Contains(newKey))
                    newKey = $"NewKey{suffix++}";

                object newVal = valueType.IsValueType ? Activator.CreateInstance(valueType) : null;
                dict[newKey] = newVal;
            }

            EditorGUI.indentLevel--;

            return dict;
        }

        // ================== 工具 ==================

        private static FieldInfo[] GetFields(Type type)
        {
            if (!fieldCache.TryGetValue(type, out var fields))
            {
                fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                fieldCache[type] = fields;
            }
            return fields;
        }

        private string GetFullPath(YoYoConfigBase config)
        {
            string relativePath = config.GetConfigPath();
            return Path.Combine(Application.dataPath, relativePath);
        }

        private void LoadJson()
        {
            if (selectedConfigType == null || currentConfig == null) return;

            string fullPath = GetFullPath(currentConfig);

            if (File.Exists(fullPath))
            {
                string json = File.ReadAllText(fullPath);

                var newConfig = (YoYoConfigBase)Activator.CreateInstance(selectedConfigType);
                newConfig.JsonToObject(json);

                currentConfig = newConfig;

                Debug.Log($"Config loaded: {fullPath}");
            }
            else
            {
                Debug.LogWarning($"Json not found: {fullPath}");
            }
        }

        private void SaveJson()
        {
            if (currentConfig == null) return;

            string fullPath = GetFullPath(currentConfig);
            string dir = Path.GetDirectoryName(fullPath);

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            YoYoJsonUtil.ObjectToJsonFile(currentConfig, fullPath);

            Debug.Log($"Config saved: {fullPath}");
            AssetDatabase.Refresh();
        }
    }
}