// YoYo Module Setting Editor Tool
// 放到 Assets/Editor/YoYoModuleSettingWindow.cs

using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

[Serializable]
public class YoYoModuleData
{
    public string defineName;
    public string asmdefPath; // 相对于 Assets，不包含 Assets
    public bool enable;
}

public class YoYoModuleConfig : ScriptableObject
{
    public List<YoYoModuleData> modules = new List<YoYoModuleData>();
}

public class YoYoModuleSettingWindow : EditorWindow
{
    private YoYoModuleConfig config;
    private Vector2 scroll;
    private const string CONFIG_PATH = "Assets/YoYoModuleConfig.asset";

    [MenuItem("YoYo/YoYo Module Setting")]
    public static void Open()
    {
        var window = GetWindow<YoYoModuleSettingWindow>();
        window.titleContent = new GUIContent("YoYo Module Setting");
        window.Show();
    }

    private void OnEnable()
    {
        LoadOrCreateConfig();
    }

    void LoadOrCreateConfig()
    {
        config = AssetDatabase.LoadAssetAtPath<YoYoModuleConfig>(CONFIG_PATH);
        if (config == null)
        {
            config = CreateInstance<YoYoModuleConfig>();
            AssetDatabase.CreateAsset(config, CONFIG_PATH);
            AssetDatabase.SaveAssets();
        }
    }

    private void OnGUI()
    {
        if (config == null)
            return;

        EditorGUILayout.Space();

        scroll = EditorGUILayout.BeginScrollView(scroll);

        for (int i = 0; i < config.modules.Count; i++)
        {
            var m = config.modules[i];

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Module {i}", EditorStyles.boldLabel);
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                config.modules.RemoveAt(i);
                break;
            }
            EditorGUILayout.EndHorizontal();

            m.defineName = EditorGUILayout.TextField("Define Name", m.defineName);
            m.asmdefPath = EditorGUILayout.TextField("Asmdef Path", m.asmdefPath);
            m.enable = EditorGUILayout.Toggle("Enable", m.enable);

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        if (GUILayout.Button("+ Add Module"))
        {
            config.modules.Add(new YoYoModuleData());
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Save"))
        {
            ApplyConfig();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }
    }

    void ApplyConfig()
    {
        var group = BuildTargetGroup.Standalone;

        // 1. 获取当前宏
        var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group)
            .Split(';')
            .Where(d => !string.IsNullOrEmpty(d))
            .ToList();

        // 2. 删除所有 YoYo 开头的宏
        defines.RemoveAll(d => d.StartsWith("YoYo"));

        // 3. 添加配置中的宏（只添加 enable = true）
        foreach (var m in config.modules)
        {
            if (!m.defineName.StartsWith("YoYo"))
            {
                Debug.LogError($"宏名必须以 YoYo 开头: {m.defineName}");
                continue;
            }

            if (m.enable)
            {
                if (!defines.Contains(m.defineName))
                    defines.Add(m.defineName);
            }
        }

        // 4. 应用宏
        var result = string.Join(";", defines);
        PlayerSettings.SetScriptingDefineSymbolsForGroup(group, result);

        // 5. 处理 asmdef
        foreach (var m in config.modules)
        {
            if (string.IsNullOrEmpty(m.asmdefPath))
                continue;

            string fullPath = Path.Combine("Assets", m.asmdefPath);

            if (!File.Exists(fullPath))
            {
                Debug.LogError($"asmdef 不存在: {fullPath}");
                continue;
            }

            string json = File.ReadAllText(fullPath);
            var asm = JsonUtility.FromJson<AsmdefData>(json);

            if (asm.defineConstraints == null)
                asm.defineConstraints = new List<string>();

            // 清理旧的 YoYo 宏
            asm.defineConstraints.RemoveAll(d => d.StartsWith("YoYo"));

            // 添加当前宏
            if (!string.IsNullOrEmpty(m.defineName))
            {
                asm.defineConstraints.Add(m.defineName);
            }

            string newJson = JsonUtility.ToJson(asm, true);
            File.WriteAllText(fullPath, newJson);
        }

        AssetDatabase.Refresh();

        Debug.Log("YoYo Module Apply Done (will recompile)");
    }

    [Serializable]
    class AsmdefData
    {
        public string name;
        public List<string> references;
        public List<string> defineConstraints;
    }
}
