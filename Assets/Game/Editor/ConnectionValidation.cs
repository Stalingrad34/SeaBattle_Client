using System;
using System.IO;
using Game.Scripts.Infrastructure.Implementations;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>Menu-driven smoke check, also requestable by local automation through Library.</summary>
    [InitializeOnLoad]
    public static class ConnectionValidation
    {
        private const string RequestPath = "Library/SeaBattleConnection.request";
        private const string ResultPath = "Library/SeaBattleConnection.result";
        private const string RunningKey = "SeaBattle.ConnectionValidation.Running";
        private const string StartedKey = "SeaBattle.ConnectionValidation.Started";

        static ConnectionValidation() => EditorApplication.update += Update;

        [MenuItem("SeaBattle/Validate connection")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            ConnectionSceneSetup.Create();
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != ConnectionSceneSetup.ScenePath) return;
            File.WriteAllText(ResultPath, "RUNNING");
            SessionState.SetBool(RunningKey, true);
            SessionState.SetString(StartedKey, DateTime.UtcNow.Ticks.ToString());
            EditorApplication.isPlaying = true;
        }

        private static void Update()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (File.Exists(RequestPath) && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                File.Delete(RequestPath);
                Run();
                return;
            }
            if (!SessionState.GetBool(RunningKey, false)) return;
            var started = new DateTime(long.Parse(SessionState.GetString(StartedKey, "0")), DateTimeKind.Utc);
            var probe = UnityEngine.Object.FindAnyObjectByType<ConnectionBootstrap>();
            if (EditorApplication.isPlaying && probe != null && probe.Succeeded)
                Finish("PASS: Unity -> Colyseus -> Unity ping/pong");
            else if ((DateTime.UtcNow - started).TotalSeconds > 45)
                Finish("FAIL: " + (probe != null ? probe.Status : "Scene did not start"));
        }

        private static void Finish(string result)
        {
            SessionState.SetBool(RunningKey, false);
            File.WriteAllText(ResultPath, result);
            Debug.Log("[SeaBattle validation] " + result);
            EditorApplication.isPlaying = false;
        }
    }
}
