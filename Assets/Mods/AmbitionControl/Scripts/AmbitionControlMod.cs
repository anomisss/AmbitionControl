using System;
using System.Threading.Tasks;
using BAModAPI;
using UnityEngine;

[assembly: RegisterModClass(typeof(AmbitionControl.AmbitionControlMod))]

namespace AmbitionControl
{
    // Exact lifecycle and interface are modeled on the SDK Example-Options mod.
    [ModEntryOnInitializationLoad]
    public sealed class AmbitionControlMod : IModBigAmbitions
    {
        public string[] RelativeAssetBundlePaths => Array.Empty<string>();

        public Task OnLoadAsync(ModContext context)
        {
            AmbitionControlHost.Install();
            context.Logger.Info("Ambition Control v0.2.0 loaded. Press F10.");
            return Task.CompletedTask;
        }

        public Task OnUnloadAsync()
        {
            AmbitionControlHost.Uninstall();
            return Task.CompletedTask;
        }
    }

    internal sealed class AmbitionControlHost : MonoBehaviour
    {
        private static AmbitionControlHost instance;
        private TrainerMenu menu;

        internal static void Install()
        {
            if (instance != null) return;
            var host = new GameObject("AmbitionControl.Host");
            DontDestroyOnLoad(host);
            instance = host.AddComponent<AmbitionControlHost>();
        }

        internal static void Uninstall()
        {
            if (instance == null) return;
            instance.menu?.Save();
            Destroy(instance.gameObject);
            instance = null;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            menu = new TrainerMenu();
        }

        private void OnGUI()
        {
            menu?.Render();
        }

        private void OnDestroy()
        {
            menu?.Save();
            if (instance == this) instance = null;
        }

        private void OnApplicationQuit()
        {
            menu?.Save();
        }
    }
}
