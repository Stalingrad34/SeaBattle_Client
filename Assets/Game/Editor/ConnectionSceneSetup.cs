using System.IO;
using Game.Scripts.Infrastructure.Core.Network;
using Game.Scripts.Infrastructure.Implementations;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    public static class ConnectionSceneSetup
    {
        public const string ScenePath = "Assets/Scenes/SeaBattle.unity";
        public const string ConfigPath = "Assets/Game/Configs/ConnectionConfig.asset";

        [MenuItem("SeaBattle/Create connection scene")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath);
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            Directory.CreateDirectory("Assets/Game/Configs");
            AssetDatabase.Refresh();
            var config = AssetDatabase.LoadAssetAtPath<ConnectionConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ConnectionConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }
            var bootstrap = new GameObject("Connection Bootstrap").AddComponent<ConnectionBootstrap>();
            var serialized = new SerializedObject(bootstrap);
            serialized.FindProperty("config").objectReferenceValue = config;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }
    }
}
