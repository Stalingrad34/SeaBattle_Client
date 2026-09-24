using System.IO;
using Game.Scripts.Infrastructure.Core.Configs;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class GameConfigExport
    {
        [MenuItem("SeaBattle/Export server configuration")]
        public static void Export()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConnectionSceneSetup.GameConfigPath);
            if (config == null)
                throw new System.InvalidOperationException("Game config is missing.");
            config.Validate();
            var directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Server/config"));
            if (!Directory.Exists(Path.GetDirectoryName(directory)))
                throw new System.InvalidOperationException("Place the server repository in Server first.");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "game.json"), JsonUtility.ToJson(config, true));
            Debug.Log("[SeaBattle] Exported Server/config/game.json. Restart server to apply supported rules.");
        }
    }
}
