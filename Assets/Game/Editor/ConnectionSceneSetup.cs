using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    public static class ConnectionSceneSetup
    {
        public const string ScenePath = "Assets/Scenes/SeaBattle.unity";
        public const string ConfigPath = "Assets/Game/Configs/ConnectionConfig.asset";
        public const string GameConfigPath = "Assets/Game/Configs/GameConfig.asset";
        [MenuItem("SeaBattle/Open startup scene")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().path == ScenePath)
                return;
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }
    }
}
