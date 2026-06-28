using System;
using UnityEngine;

namespace Goose.Monsterpatch.GTSAllInOnePatcher
{
    // Minimal helper used by the AIO-integrated Trading Post runtime.
    // This intentionally is not a BepInEx preloader patcher: no TargetDLLs/Patch methods.
    // The AIO patcher owns runtime injection so the old standalone GTS entry point does not double-install.
    public static class GTSNativePatcher
    {
        public const string PatcherName = "Monsterpatch GTS AIO Integrated";

        public static void RuntimeLog(string message)
        {
            Console.WriteLine("[" + PatcherName + "] " + message);
        }

        public static void RuntimeWarn(string message)
        {
            Console.WriteLine("[" + PatcherName + "] WARNING: " + message);
        }

        internal static GTSRuntimeHost EnsureRuntimeHost()
        {
            try
            {
                if (GTSRuntimeHost.Instance != null)
                    return GTSRuntimeHost.Instance;

                GameObject go = new GameObject("Monsterpatch_GTS_AIO_RuntimeHost");
                UnityEngine.Object.DontDestroyOnLoad(go);
                return go.AddComponent<GTSRuntimeHost>();
            }
            catch (Exception ex)
            {
                RuntimeWarn("EnsureRuntimeHost failed: " + ex.Message);
                return null;
            }
        }
    }
}
