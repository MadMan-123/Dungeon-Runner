using System;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace Core
{
    public static class Loader
    {
        public enum Scene
        {
            MainMenu,
            Lobby,
            World
        }

        private static string GetSceneName(Scene scene)
        {
            return scene switch
            {
                Scene.Lobby => "Lobby",
                Scene.MainMenu => "Main Menu",
                Scene.World => "Main World",
                _ => ""
            };
        }
        
        private static Scene targetScene;



        public static void Load(Scene newScene)
        {
            Loader.targetScene = newScene;

            SceneManager.LoadScene(GetSceneName(targetScene));
        }

        public static void LoadNetwork(Scene newScene)
        {
            var nm = NetworkManager.Singleton;
            var sceneName = GetSceneName(newScene);

            UnityEngine.Debug.Log($"[Loader.LoadNetwork] Requested scene '{sceneName}'. " +
                                   $"IsServer={nm.IsServer}, IsHost={nm.IsHost}, IsClient={nm.IsClient}, " +
                                   $"IsConnectedClient={nm.IsConnectedClient}, IsListening={nm.IsListening}");

            if (nm == null)
            {
                UnityEngine.Debug.LogError("[Loader.LoadNetwork] NetworkManager.Singleton is null. Cannot load network scene.");
                return;
            }

            if (!nm.IsServer)
            {
                // Only the server may call NetworkSceneManager.LoadScene. Clients should not.
                UnityEngine.Debug.LogWarning($"[Loader.LoadNetwork] Ignoring client-side scene load request for '{sceneName}'. " +
                                             "Only the server/host may initiate network scene loads.");
                return;
            }

            try
            {
                UnityEngine.Debug.Log($"[Loader.LoadNetwork] Server initiating network load to '{sceneName}' (mode=Single).");
                nm.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[Loader.LoadNetwork] Exception while loading scene '{sceneName}': {ex}");
            }
        }
    }
}