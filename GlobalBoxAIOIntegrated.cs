using System;
using System.IO;
using System.Collections;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Goose.Monsterpatch.GlobalBoxAccess
{
    /// <summary>
    /// AIO-integrated Global Box host.
    /// This is intentionally NOT a BepInEx plugin. The AIO preloader injects EnsureHost()
    /// from GameScript.Start so Global Box can live inside the AIO patcher DLL.
    /// </summary>
    public sealed class GlobalBoxRuntimeHost : MonoBehaviour
    {
        public const string Guid = "goose.monsterpatch.mmonsterpatchaio.globalbox";
        public const string Name = "Monsterpatch Global Box Access - AIO Integrated";
        public const string Version = "0.9.8-aio.native-buttons-uppercase-config";

        public static GlobalBoxRuntimeHost Instance;
        internal static ManualLogSource Log;
        internal static ConfigEntry<int> SelectButtonWindowsNumber;
        internal static ConfigEntry<bool> DebugSelectInput;
        internal static ConfigEntry<bool> AutoSaveOnGlobalBoxChanges;

        internal static ConfigEntry<float> NativeButtonRootX;
        internal static ConfigEntry<float> NativeButtonRootY;
        internal static ConfigEntry<float> NativeButtonWidth;
        internal static ConfigEntry<float> NativeButtonHeight;
        internal static ConfigEntry<float> NativeButtonGapY;
        internal static ConfigEntry<float> NativeLocalButtonX;
        internal static ConfigEntry<float> NativeLocalButtonY;
        internal static ConfigEntry<float> NativeGlobalButtonX;
        internal static ConfigEntry<float> NativeGlobalButtonY;
        internal static ConfigEntry<float> NativeLocalIconX;
        internal static ConfigEntry<float> NativeLocalIconY;
        internal static ConfigEntry<float> NativeGlobalIconX;
        internal static ConfigEntry<float> NativeGlobalIconY;
        internal static ConfigEntry<float> NativeIconWidth;
        internal static ConfigEntry<float> NativeIconHeight;
        internal static ConfigEntry<float> NativeTextOffsetX;
        internal static ConfigEntry<float> NativeTextOffsetYMin;
        internal static ConfigEntry<float> NativeTextOffsetYMax;
        internal static ConfigEntry<float> NativeCursorLocalX;
        internal static ConfigEntry<float> NativeCursorLocalY;
        internal static ConfigEntry<float> NativeCursorGlobalX;
        internal static ConfigEntry<float> NativeCursorGlobalY;

        internal static ConfigEntry<bool> NativeVirtualTabNavigation;
        internal static ConfigEntry<bool> SelectModeSwitchToBoxOneTab;
        internal static ConfigEntry<float> ModeArrowX;
        internal static ConfigEntry<float> ModeArrowY;

        private ConfigFile config;

        public static void EnsureHost()
        {
            try
            {
                if (Instance != null)
                    return;

                GameObject go = new GameObject("Monsterpatch_GlobalBox_AIO_Runtime");
                DontDestroyOnLoad(go);
                Instance = go.AddComponent<GlobalBoxRuntimeHost>();
                Debug.Log("[Monsterpatch Global Box AIO] runtime host loaded from GameScript.Start.");
            }
            catch (Exception ex)
            {
                Debug.Log("[Monsterpatch Global Box AIO] EnsureHost failed: " + ex);
            }
        }

        /// <summary>
        /// Used by the AIO chat host so the IMGUI chat cannot steal Tab/Select/click input
        /// while the Mon Box / Global Box UI is open.
        /// </summary>
        public static bool IsMonBoxOpenForChatGuard()
        {
            try
            {
                GameScript gs = UnityEngine.Object.FindObjectOfType<GameScript>();
                return IsMonBoxOpen(gs);
            }
            catch
            {
                return false;
            }
        }

        internal static bool IsMonBoxOpen(GameScript gs)
        {
            try
            {
                if (gs == null || gs.boxManager == null)
                    return false;

                BoxManager bm = gs.boxManager;
                return (gs.menuBox != null && gs.menuBox.activeInHierarchy)
                       || (GlobalBoxState.ButtonRoot != null && GlobalBoxState.ButtonRoot.activeInHierarchy)
                       || (bm.buttonBoxEntry != null && bm.buttonBoxEntry.Length > 0
                           && bm.buttonBoxEntry[0] != null
                           && bm.buttonBoxEntry[0].activeInHierarchy);
            }
            catch
            {
                return false;
            }
        }

        private void Awake()
        {
            Instance = this;

            try
            {
                Log = BepInEx.Logging.Logger.CreateLogSource("Monsterpatch Global Box AIO");
            }
            catch
            {
                Log = new ManualLogSource("Monsterpatch Global Box AIO");
            }

            try
            {
                string configPath = Path.Combine(Paths.ConfigPath, "goose.monsterpatch.globalbox.access.cfg");
                config = new ConfigFile(configPath, true);
                SelectButtonWindowsNumber = config.Bind("Controls", "SelectButtonWindowsNumber", 7, "Windows Game Controller Properties button number used to toggle Local/Global while the mon box is open. Example: Windows Button 7 maps to Unity JoystickButton6.");
                DebugSelectInput = config.Bind("Controls", "DebugSelectInput", false, "When true, logs joystick button presses while the mon box is open so the correct Select button can be identified.");
                AutoSaveOnGlobalBoxChanges = config.Bind("Saving", "AutoSaveOnGlobalBoxChanges", true, "When true, sending mons between Local and Global boxes immediately writes globalBox.txt and forces GameScript.SaveGame() so the local box change is saved too.");

                NativeButtonRootX = config.Bind("Global Box Native Buttons", "ButtonRootX", 225f, "Root X position for the Local/Global native button group. Adjusts both mode buttons together.");
                NativeButtonRootY = config.Bind("Global Box Native Buttons", "ButtonRootY", -6f, "Root Y position for the Local/Global native button group. Adjusts both mode buttons together.");
                NativeButtonWidth = config.Bind("Global Box Native Buttons", "ButtonWidth", 62f, "Clickable/selection width for each Local/Global native button.");
                NativeButtonHeight = config.Bind("Global Box Native Buttons", "ButtonHeight", 14f, "Clickable/selection height for each Local/Global native button.");
                NativeButtonGapY = config.Bind("Global Box Native Buttons", "ButtonGapY", -15f, "Vertical gap from Local button to Global button. Negative moves Global downward.");
                NativeLocalButtonX = config.Bind("Global Box Native Buttons", "LocalButtonX", 0f, "Local button X offset relative to ButtonRoot.");
                NativeLocalButtonY = config.Bind("Global Box Native Buttons", "LocalButtonY", -1f, "Local button Y offset relative to ButtonRoot.");
                NativeGlobalButtonX = config.Bind("Global Box Native Buttons", "GlobalButtonX", 0f, "Global button X offset relative to ButtonRoot.");
                NativeGlobalButtonY = config.Bind("Global Box Native Buttons", "GlobalButtonY", -15f, "Global button Y offset relative to ButtonRoot.");
                NativeLocalIconX = config.Bind("Global Box Native Buttons", "LocalIconX", 3f, "Local box image/icon X offset relative to Local button.");
                NativeLocalIconY = config.Bind("Global Box Native Buttons", "LocalIconY", 0f, "Local box image/icon Y offset relative to Local button.");
                NativeGlobalIconX = config.Bind("Global Box Native Buttons", "GlobalIconX", 3f, "Global box image/icon X offset relative to Global button.");
                NativeGlobalIconY = config.Bind("Global Box Native Buttons", "GlobalIconY", 0f, "Global box image/icon Y offset relative to Global button.");
                NativeIconWidth = config.Bind("Global Box Native Buttons", "IconWidth", 10f, "Width of the Local/Global box images/icons.");
                NativeIconHeight = config.Bind("Global Box Native Buttons", "IconHeight", 10f, "Height of the Local/Global box images/icons.");
                NativeTextOffsetX = config.Bind("Global Box Native Buttons", "TextOffsetX", 16f, "Text left offset inside each Local/Global button.");
                NativeTextOffsetYMin = config.Bind("Global Box Native Buttons", "TextOffsetYMin", -1f, "Text bottom offset inside each Local/Global button.");
                NativeTextOffsetYMax = config.Bind("Global Box Native Buttons", "TextOffsetYMax", 1f, "Text top offset inside each Local/Global button.");
                NativeCursorLocalX = config.Bind("Global Box Native Buttons", "CursorLocalX", -8f, "Arrow/cursor X landing point when Local is focused. This is local to the Local button.");
                NativeCursorLocalY = config.Bind("Global Box Native Buttons", "CursorLocalY", -7f, "Arrow/cursor Y landing point when Local is focused. This is local to the Local button.");
                NativeCursorGlobalX = config.Bind("Global Box Native Buttons", "CursorGlobalX", -8f, "Arrow/cursor X landing point when Global is focused. This is local to the Global button.");
                NativeCursorGlobalY = config.Bind("Global Box Native Buttons", "CursorGlobalY", -7f, "Arrow/cursor Y landing point when Global is focused. This is local to the Global button.");

                NativeVirtualTabNavigation = config.Bind("Native Cursor", "NativeVirtualTabNavigation", true, "When true, Global Box wires explicit native Selectable navigation for the Local/Global buttons.");
                SelectModeSwitchToBoxOneTab = config.Bind("Native Cursor", "SelectModeSwitchToBoxOneTab", true, "Safety setting retained for compatibility. Native Local/Global button confirm keeps focus on the button; Select/Tab fallback can still return to Box 1.");
                ModeArrowX = config.Bind("Native Cursor", "ModeArrowX", -8f, "Compatibility arrow X setting for native mode controls.");
                ModeArrowY = config.Bind("Native Cursor", "ModeArrowY", 0f, "Compatibility arrow Y setting for native mode controls.");

                config.Save();
                PruneGlobalBoxUserConfig(config.ConfigFilePath);
            }
            catch (Exception ex)
            {
                LogWarning("Config load failed: " + ex.Message);
            }

            // Harmony patches are installed by the existing AIO bootstrap PatchAll() call.
            // Do not PatchAll() here, or the existing MMO/PvP Harmony patches could be applied twice.
            LogInfo(Name + " " + Version + " loaded. Harmony patches are owned by the AIO bootstrap.");
        }

        private static void PruneGlobalBoxUserConfig(string path)
        {
            global::AIOConfigPruner.Prune(path, global::AIOVisibleConfigKeys.GlobalBoxUserConfigKeys());
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            try
            {
                GameScript gs = UnityEngine.Object.FindObjectOfType<GameScript>();
                if (gs != null)
                    GlobalBoxState.EnsureSendToButton(gs);
            }
            catch (Exception ex)
            {
                LogWarning("Global Box Send To update failed: " + ex.Message);
            }
        }

        internal static void LogInfo(string msg)
        {
            try { if (Log != null) Log.LogInfo(msg); else Debug.Log("[Monsterpatch Global Box AIO] " + msg); }
            catch { }
        }

        internal static void LogWarning(string msg)
        {
            try { if (Log != null) Log.LogWarning(msg); else Debug.LogWarning("[Monsterpatch Global Box AIO] " + msg); }
            catch { }
        }

        internal static void LogError(string msg)
        {
            try { if (Log != null) Log.LogError(msg); else Debug.LogError("[Monsterpatch Global Box AIO] " + msg); }
            catch { }
        }
    }

    internal static class GlobalBoxState
    {
        internal const int SlotCount = 648;
        internal static bool ViewingGlobal = false;
        internal static Mon[] LocalBoxRef;
        internal static Mon[] GlobalMons;
        internal static int LastLocalTab = 0;
        internal static int LastGlobalTab = 0;
        internal static GameObject ButtonRoot;
        internal static Button BtnLocal;
        internal static Button BtnGlobal;
        internal static TMP_Text TxtLocal;
        internal static TMP_Text TxtGlobal;
        internal static GameObject IconLocal;
        internal static GameObject IconGlobal;
        internal static GameObject HighlightLocal;
        internal static GameObject HighlightGlobal;
        internal static bool NativeNavigationWired;
        internal static Sprite CachedLocalBoxSprite;
        internal static Sprite CachedGlobalBoxSprite;
        internal static bool SuppressRefreshHook = false;
        internal static GameObject SendToExtraButton;
        internal static Vector3 SendToCancelOriginalLocalPos;
        internal static Vector3 SendToTeamOriginalLocalPos;
        internal static Vector3 SendToTownOriginalLocalPos;
        internal static bool SendToOriginalsCaptured = false;
        internal static Vector2 SendToMenuOriginalSize;
        internal static bool SendToCancelMoved = false;
        internal static GameObject SendToCancelButton;
        private const float SendToMenuYOffset = 12f;

        private static string Dir => Path.Combine(Paths.BepInExRootPath, "GlobalBox");
        private static string FilePath => Path.Combine(Dir, "globalBox.txt");
        private static string BackupPath => Path.Combine(Dir, "globalBox.backup.txt");

        private static float Cfg(ConfigEntry<float> entry, float fallback)
        {
            try { return entry != null ? entry.Value : fallback; }
            catch { return fallback; }
        }

        private static Vector2 NativeRootPos => new Vector2(Cfg(GlobalBoxRuntimeHost.NativeButtonRootX, 225f), Cfg(GlobalBoxRuntimeHost.NativeButtonRootY, -6f));
        private static Vector2 NativeLocalButtonPos => new Vector2(Cfg(GlobalBoxRuntimeHost.NativeLocalButtonX, 0f), Cfg(GlobalBoxRuntimeHost.NativeLocalButtonY, -1f));
        private static Vector2 NativeGlobalButtonPos => new Vector2(Cfg(GlobalBoxRuntimeHost.NativeGlobalButtonX, 0f), Cfg(GlobalBoxRuntimeHost.NativeGlobalButtonY, -15f));
        private static Vector2 NativeLocalIconPos => new Vector2(Cfg(GlobalBoxRuntimeHost.NativeLocalIconX, 3f), Cfg(GlobalBoxRuntimeHost.NativeLocalIconY, 0f));
        private static Vector2 NativeGlobalIconPos => new Vector2(Cfg(GlobalBoxRuntimeHost.NativeGlobalIconX, 3f), Cfg(GlobalBoxRuntimeHost.NativeGlobalIconY, 0f));
        private static Vector2 NativeIconSize => new Vector2(Cfg(GlobalBoxRuntimeHost.NativeIconWidth, 10f), Cfg(GlobalBoxRuntimeHost.NativeIconHeight, 10f));
        private static Vector2 NativeButtonSize => new Vector2(Cfg(GlobalBoxRuntimeHost.NativeButtonWidth, 62f), Cfg(GlobalBoxRuntimeHost.NativeButtonHeight, 14f));
        private static Vector3 NativeCursorLocalPos => new Vector3(Cfg(GlobalBoxRuntimeHost.NativeCursorLocalX, -8f), Cfg(GlobalBoxRuntimeHost.NativeCursorLocalY, -7f), 0f);
        private static Vector3 NativeCursorGlobalPos => new Vector3(Cfg(GlobalBoxRuntimeHost.NativeCursorGlobalX, -8f), Cfg(GlobalBoxRuntimeHost.NativeCursorGlobalY, -7f), 0f);

        internal static void EnsureInitialized(GameScript gs, BoxManager bm)
        {
            if (gs == null || bm == null) return;

            if (!ViewingGlobal && bm.boxMons != null)
                LocalBoxRef = bm.boxMons;

            if (GlobalMons == null || GlobalMons.Length != SlotCount)
                LoadGlobal(gs);

            EnsureButtons(gs, bm);
            EnsureModeButtonIcons(bm);
            UpdateButtonVisuals();
        }


        internal static void PrepareForSaveSessionBoundary(GameScript gs, string reason)
        {
            try
            {
                BoxManager bm = gs != null ? gs.boxManager : null;

                if (bm != null)
                {
                    if (ViewingGlobal)
                    {
                        bool visibleArrayIsGlobal = GlobalMons != null && object.ReferenceEquals(bm.boxMons, GlobalMons);
                        if (visibleArrayIsGlobal)
                        {
                            SaveGlobal(gs);
                        }
                        else if (GlobalMons != null)
                        {
                            // Save the tracked global array, but do not trust the currently visible BoxManager array.
                            SaveGlobal(gs);
                            GlobalBoxRuntimeHost.LogWarning("Global Box session boundary saw stale global view during " + reason + "; saved tracked global array only.");
                        }

                        // Before vanilla LoadGame/ReturnToTitle touches BoxManager.boxMons, make sure the visible
                        // array is never the shared global array. If the local reference is missing, give vanilla a
                        // fresh local-sized array instead of allowing it to write a save slot into GlobalMons.
                        bm.boxMons = LocalBoxRef ?? new Mon[SlotCount];
                    }
                    else if (bm.boxMons != null)
                    {
                        LocalBoxRef = bm.boxMons;
                    }
                }

                ResetForSaveSessionChange(reason);
            }
            catch (Exception ex)
            {
                GlobalBoxRuntimeHost.LogWarning("PrepareForSaveSessionBoundary failed during " + reason + ": " + ex.Message);
            }
        }

        internal static void ResetForSaveSessionChange(string reason)
        {
            try
            {
                ViewingGlobal = false;
                LocalBoxRef = null;
                GlobalMons = null;
                LastLocalTab = 0;
                LastGlobalTab = 0;
                SuppressRefreshHook = false;
                NativeNavigationWired = false;
                UpdateButtonVisuals();
                GlobalBoxRuntimeHost.LogInfo("Reset Global Box session state" + (string.IsNullOrEmpty(reason) ? "." : " for " + reason + "."));
            }
            catch { }
        }

        internal static void AttachFreshLocalBoxAfterLoad(GameScript gs)
        {
            try
            {
                BoxManager bm = gs != null ? gs.boxManager : null;
                ViewingGlobal = false;
                LocalBoxRef = bm != null ? bm.boxMons : null;
                GlobalMons = null;
                LastLocalTab = 0;
                LastGlobalTab = 0;
                SuppressRefreshHook = false;
                NativeNavigationWired = false;
                UpdateButtonVisuals();
                GlobalBoxRuntimeHost.LogInfo("Attached new save slot local box after LoadGame.");
            }
            catch (Exception ex)
            {
                GlobalBoxRuntimeHost.LogWarning("AttachFreshLocalBoxAfterLoad failed: " + ex.Message);
            }
        }

        internal static void RecoverStaleGlobalView(BoxManager bm, string reason)
        {
            try
            {
                ViewingGlobal = false;
                LocalBoxRef = bm != null ? bm.boxMons : null;
                GlobalMons = null;
                LastGlobalTab = 0;
                SuppressRefreshHook = false;
                UpdateButtonVisuals();
                GlobalBoxRuntimeHost.LogWarning("Recovered stale Global Box view without saving globalBox.txt" + (string.IsNullOrEmpty(reason) ? "." : ": " + reason));
            }
            catch { }
        }

        internal static void ToggleToLocal(GameScript gs, BoxManager bm)
        {
            if (gs == null || bm == null) return;
            if (!ViewingGlobal) return;
            if (bm.pickedUpMon != null)
            {
                GlobalBoxRuntimeHost.LogWarning("Cannot switch to My Box while holding a mon.");
                return;
            }

            SaveGlobal(gs);
            LastGlobalTab = Mathf.Clamp(bm.curBoxTab, 0, 23);
            ViewingGlobal = false;
            bm.boxMons = LocalBoxRef ?? bm.boxMons;
            bm.curBoxTab = 0;
            LastLocalTab = 0;
            RefreshSafe(bm);
            SelectBoxTabZero(gs, bm);
            UpdateButtonVisuals();
            GlobalBoxRuntimeHost.LogInfo("Switched to My Box.");
        }

        internal static void ToggleToGlobal(GameScript gs, BoxManager bm)
        {
            if (gs == null || bm == null) return;
            if (ViewingGlobal) return;
            if (bm.pickedUpMon != null)
            {
                GlobalBoxRuntimeHost.LogWarning("Cannot switch to Global Box while holding a mon.");
                return;
            }

            LocalBoxRef = bm.boxMons;
            LastLocalTab = Mathf.Clamp(bm.curBoxTab, 0, 23);
            LoadGlobal(gs);
            ViewingGlobal = true;
            bm.boxMons = GlobalMons;
            bm.curBoxTab = 0;
            LastGlobalTab = 0;
            RefreshSafe(bm);
            SelectBoxTabZero(gs, bm);
            UpdateButtonVisuals();
            GlobalBoxRuntimeHost.LogInfo("Switched to Global Box.");
        }

        internal static void HandleSelectButtonToggle(GameScript gs)
        {
            try
            {
                if (gs == null || gs.boxManager == null) return;

                BoxManager bm = gs.boxManager;
                bool boxLooksOpen = false;
                try
                {
                    boxLooksOpen = (gs.menuBox != null && gs.menuBox.activeInHierarchy)
                                   || (ButtonRoot != null && ButtonRoot.activeInHierarchy)
                                   || (bm.buttonBoxEntry != null && bm.buttonBoxEntry.Length > 0
                                       && bm.buttonBoxEntry[0] != null
                                       && bm.buttonBoxEntry[0].activeInHierarchy);
                }
                catch { }

                if (!boxLooksOpen) return;

                LogSelectInputIfEnabled();

                if (!GetSelectButtonDown()) return;
                if (Time.frameCount == LastSelectToggleFrame) return;
                LastSelectToggleFrame = Time.frameCount;

                EnsureInitialized(gs, bm);

                // Use the exact same onClick path as clicking the visible Local/Global buttons.
                if (ViewingGlobal)
                {
                    if (BtnLocal != null) BtnLocal.onClick.Invoke();
                    else ToggleToLocal(gs, bm);
                }
                else
                {
                    if (BtnGlobal != null) BtnGlobal.onClick.Invoke();
                    else ToggleToGlobal(gs, bm);
                }

                GlobalBoxRuntimeHost.LogInfo("Select/View toggle invoked Local/Global button click.");
            }
            catch (Exception ex)
            {
                GlobalBoxRuntimeHost.LogWarning("Select/View Local/Global toggle failed: " + ex.Message);
            }
        }

        private static int LastSelectToggleFrame = -1;
        private static int LastInputDebugFrame = -1;

        private static bool GetSelectButtonDown()
        {
            int windowsButtonNumber = 7;
            try
            {
                if (GlobalBoxRuntimeHost.SelectButtonWindowsNumber != null)
                    windowsButtonNumber = GlobalBoxRuntimeHost.SelectButtonWindowsNumber.Value;
            }
            catch { }

            // Windows Game Controller Properties is 1-based. Unity KeyCode.JoystickButton is 0-based.
            int unityButtonIndex = Mathf.Clamp(windowsButtonNumber - 1, 0, 19);
            KeyCode configuredButton = (KeyCode)((int)KeyCode.JoystickButton0 + unityButtonIndex);

            if (Input.GetKeyDown(configuredButton)) return true;
            if (Input.GetKeyDown(KeyCode.Tab)) return true; // keyboard sanity test
            if (SafeGetButtonDown("Select")) return true;
            if (SafeGetButtonDown("Back")) return true;
            if (SafeGetButtonDown("View")) return true;

            return false;
        }

        private static bool SafeGetButtonDown(string buttonName)
        {
            try { return Input.GetButtonDown(buttonName); }
            catch { return false; }
        }

        private static void LogSelectInputIfEnabled()
        {
            try
            {
                if (GlobalBoxRuntimeHost.DebugSelectInput == null || !GlobalBoxRuntimeHost.DebugSelectInput.Value) return;
                if (!Input.anyKeyDown) return;
                if (Time.frameCount == LastInputDebugFrame) return;
                LastInputDebugFrame = Time.frameCount;

                for (int i = 0; i <= 19; i++)
                {
                    KeyCode anyJoy = (KeyCode)((int)KeyCode.JoystickButton0 + i);
                    if (Input.GetKeyDown(anyJoy))
                    {
                        GlobalBoxRuntimeHost.LogInfo("[SelectInputDebug] Unity JoystickButton" + i + " / Windows Button " + (i + 1) + " pressed while mon box is open.");
                    }

                    KeyCode joy1 = (KeyCode)((int)KeyCode.Joystick1Button0 + i);
                    if (Input.GetKeyDown(joy1))
                    {
                        GlobalBoxRuntimeHost.LogInfo("[SelectInputDebug] Unity Joystick1Button" + i + " / Windows Button " + (i + 1) + " pressed while mon box is open.");
                    }
                }

                if (Input.GetKeyDown(KeyCode.Tab))
                    GlobalBoxRuntimeHost.LogInfo("[SelectInputDebug] Keyboard Tab pressed while mon box is open.");
            }
            catch (Exception ex)
            {
                GlobalBoxRuntimeHost.LogWarning("Select input debug failed: " + ex.Message);
            }
        }

        internal static void SaveGlobal(GameScript gs)
        {
            if (gs == null || GlobalMons == null) return;
            Directory.CreateDirectory(Dir);
            if (File.Exists(FilePath))
            {
                try { File.Copy(FilePath, BackupPath, true); } catch { }
            }

            string[] lines = new string[SlotCount];
            for (int i = 0; i < SlotCount; i++)
            {
                Mon mon = GlobalMons[i];
                lines[i] = mon == null ? "NULL" : gs.ConstructMonSaveStringFromMon(mon, false);
            }
            File.WriteAllLines(FilePath, lines);
        }

        internal static void ForceSaveAfterLocalGlobalTransfer(GameScript gs, string reason)
        {
            try
            {
                if (gs == null) return;
                if (GlobalBoxRuntimeHost.AutoSaveOnGlobalBoxChanges != null && !GlobalBoxRuntimeHost.AutoSaveOnGlobalBoxChanges.Value)
                    return;

                // SaveGlobal() persists globalBox.txt. GameScript.SaveGame() persists the local box side.
                // SaveGamePatch below safely swaps the visible Global Box out before vanilla saves,
                // then restores the Global Box view after the save completes.
                SaveGlobal(gs);
                gs.SaveGame();
                GlobalBoxRuntimeHost.LogInfo("Forced save after Global Box transfer" + (string.IsNullOrEmpty(reason) ? "." : ": " + reason));
            }
            catch (Exception ex)
            {
                GlobalBoxRuntimeHost.LogWarning("Force save after Global Box transfer failed: " + ex.Message);
            }
        }

        internal static void LoadGlobal(GameScript gs)
        {
            GlobalMons = new Mon[SlotCount];
            Directory.CreateDirectory(Dir);

            if (!File.Exists(FilePath))
            {
                string[] empty = new string[SlotCount];
                for (int i = 0; i < SlotCount; i++) empty[i] = "NULL";
                File.WriteAllLines(FilePath, empty);
                return;
            }

            string[] lines = File.ReadAllLines(FilePath);
            for (int i = 0; i < SlotCount; i++)
            {
                string s = i < lines.Length ? lines[i] : "NULL";
                if (string.IsNullOrWhiteSpace(s) || s == "NULL")
                    GlobalMons[i] = null;
                else
                    GlobalMons[i] = gs.GetMonFromSaveString(s);
            }
        }

        internal static void RefreshSafe(BoxManager bm)
        {
            try
            {
                SuppressRefreshHook = true;
                bm.RefreshBox();
            }
            finally { SuppressRefreshHook = false; }

            ApplyGlobalBoxLabel(bm);
        }

        internal static void ApplyGlobalBoxLabel(BoxManager bm)
        {
            try
            {
                if (!ViewingGlobal || bm == null || bm.txtBox == null) return;

                int monCount = 0;
                int start = Mathf.Clamp(bm.curBoxTab, 0, 23) * 27;
                for (int i = 0; i < 27; i++)
                {
                    int idx = start + i;
                    if (bm.boxMons != null && idx >= 0 && idx < bm.boxMons.Length && bm.boxMons[idx] != null)
                        monCount++;
                }
				bm.txtBox.fontSize = 7f;
                bm.txtBox.text = "GLOBAL " + (bm.curBoxTab + 1).ToString() + "\n" + monCount.ToString() + "/27";
            }
            catch { }
        }

        private static void SelectBoxTabZero(GameScript gs, BoxManager bm)
        {
            try
            {
                if (gs == null || gs.eventSystem == null || bm == null) return;

                // The standalone Global Box behavior Goose wants here is not the first mon slot.
                // After swapping Local <-> Global, force the active box page back to Box 1
                // and put Unity's selected UI object on the actual Box 1 tab/header control.
                bm.curBoxTab = 0;

                GameObject tab = FindBoxTabSelectableObject(bm, 0);
                if (tab != null)
                {
                    gs.eventSystem.SetSelectedGameObject(null);
                    gs.eventSystem.SetSelectedGameObject(tab);
                    GlobalBoxRuntimeHost.LogInfo("Selected Box tab 0 after Local/Global toggle: " + tab.name);
                }
                else
                {
                    GlobalBoxRuntimeHost.LogWarning("Could not find Box tab 0 selectable after Local/Global toggle; leaving current selection unchanged instead of forcing slot 0.");
                }

                if (GlobalBoxRuntimeHost.Instance != null)
                    GlobalBoxRuntimeHost.Instance.StartCoroutine(DelayedSelectBoxTabZero(gs, bm));
            }
            catch (Exception ex)
            {
                GlobalBoxRuntimeHost.LogWarning("SelectBoxTabZero failed: " + ex.Message);
            }
        }

        private static IEnumerator DelayedSelectBoxTabZero(GameScript gs, BoxManager bm)
        {
            yield return null;
            SelectBoxTabZeroImmediate(gs, bm);
            yield return null;
            SelectBoxTabZeroImmediate(gs, bm);
        }

        private static void SelectBoxTabZeroImmediate(GameScript gs, BoxManager bm)
        {
            try
            {
                if (gs == null || gs.eventSystem == null || bm == null) return;
                bm.curBoxTab = 0;

                GameObject tab = FindBoxTabSelectableObject(bm, 0);
                if (tab != null)
                {
                    gs.eventSystem.SetSelectedGameObject(null);
                    gs.eventSystem.SetSelectedGameObject(tab);
                }
            }
            catch { }
        }

        private static GameObject FindBoxTabSelectableObject(BoxManager bm, int tabIndex)
        {
            if (bm == null) return null;

            // Full Assembly-CSharp decompile confirms BoxManager exposes the real native tab array:
            // public GameObject[] buttonBoxTab. Prefer that over the older reflection/hierarchy guessing.
            GameObject direct = FindBoxTabFromDecompiledField(bm, tabIndex);
            if (direct != null) return direct;

            GameObject fromNamedMember = FindBoxTabFromKnownMembers(bm, tabIndex);
            if (fromNamedMember != null) return fromNamedMember;

            return FindBoxTabFromHierarchy(bm, tabIndex);
        }

        private static GameObject FindBoxTabFromDecompiledField(BoxManager bm, int tabIndex)
        {
            try
            {
                if (bm != null && bm.buttonBoxTab != null && tabIndex >= 0 && tabIndex < bm.buttonBoxTab.Length)
                {
                    GameObject go = bm.buttonBoxTab[tabIndex];
                    if (go != null)
                        return go;
                }
            }
            catch { }

            return null;
        }

        private static GameObject FindBoxTabFromKnownMembers(BoxManager bm, int tabIndex)
        {
            try
            {
                string[] preferredNames = new string[]
                {
                    "buttonBoxTab", "buttonBoxTabs", "buttonBoxTabButton", "buttonBoxTabButtons",
                    "boxTabButton", "boxTabButtons", "boxTabs", "buttonBoxPage", "buttonBoxPages",
                    "buttonBox", "buttonBoxes", "boxButtons"
                };

                Type t = bm.GetType();

                foreach (string name in preferredNames)
                {
                    GameObject go = GetIndexedGameObjectFromMember(t, bm, name, tabIndex);
                    if (IsUsableBoxTabCandidate(go, bm)) return go;
                }

                // Fallback: inspect every BoxManager field/property whose name looks like a tab control.
                foreach (var field in t.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
                {
                    string lower = (field.Name ?? string.Empty).ToLowerInvariant();
                    if (!lower.Contains("tab")) continue;
                    if (lower.Contains("entry") || lower.Contains("send") || lower.Contains("cancel")) continue;

                    GameObject go = GameObjectFromValue(field.GetValue(bm), tabIndex);
                    if (IsUsableBoxTabCandidate(go, bm)) return go;
                }

                foreach (var prop in t.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
                {
                    string lower = (prop.Name ?? string.Empty).ToLowerInvariant();
                    if (!lower.Contains("tab")) continue;
                    if (lower.Contains("entry") || lower.Contains("send") || lower.Contains("cancel")) continue;
                    if (!prop.CanRead) continue;

                    GameObject go = GameObjectFromValue(prop.GetValue(bm, null), tabIndex);
                    if (IsUsableBoxTabCandidate(go, bm)) return go;
                }
            }
            catch { }

            return null;
        }

        private static GameObject GetIndexedGameObjectFromMember(Type t, object instance, string memberName, int index)
        {
            try
            {
                var field = AccessTools.Field(t, memberName);
                if (field != null)
                {
                    GameObject go = GameObjectFromValue(field.GetValue(instance), index);
                    if (go != null) return go;
                }

                var prop = AccessTools.Property(t, memberName);
                if (prop != null && prop.CanRead)
                {
                    GameObject go = GameObjectFromValue(prop.GetValue(instance, null), index);
                    if (go != null) return go;
                }
            }
            catch { }

            return null;
        }

        private static GameObject GameObjectFromValue(object value, int index)
        {
            if (value == null) return null;

            GameObject singleGo = ValueToGameObject(value);
            if (singleGo != null && index == 0) return singleGo;

            try
            {
                System.Collections.IList list = value as System.Collections.IList;
                if (list != null && index >= 0 && index < list.Count)
                    return ValueToGameObject(list[index]);
            }
            catch { }

            try
            {
                Array arr = value as Array;
                if (arr != null && index >= 0 && index < arr.Length)
                    return ValueToGameObject(arr.GetValue(index));
            }
            catch { }

            return null;
        }

        private static GameObject ValueToGameObject(object value)
        {
            if (value == null) return null;
            GameObject go = value as GameObject;
            if (go != null) return go;

            Component c = value as Component;
            if (c != null) return c.gameObject;

            return null;
        }

        private static bool IsUsableBoxTabCandidate(GameObject go, BoxManager bm)
        {
            try
            {
                if (go == null || !go.activeInHierarchy) return false;
                if (BtnLocal != null && go == BtnLocal.gameObject) return false;
                if (BtnGlobal != null && go == BtnGlobal.gameObject) return false;
                if (bm != null && bm.buttonBoxEntry != null)
                {
                    for (int i = 0; i < bm.buttonBoxEntry.Length; i++)
                    {
                        if (bm.buttonBoxEntry[i] == go) return false;
                    }
                }

                Selectable sel = go.GetComponent<Selectable>();
                if (sel == null || !sel.IsInteractable()) return false;

                string name = (go.name ?? string.Empty).ToLowerInvariant();
                if (name.Contains("entry") || name.Contains("mon") || name.Contains("send") || name.Contains("cancel")) return false;

                return true;
            }
            catch { return false; }
        }

        private static GameObject FindBoxTabFromHierarchy(BoxManager bm, int tabIndex)
        {
            try
            {
                Transform root = null;
                if (bm.txtBox != null && bm.txtBox.transform != null && bm.txtBox.transform.parent != null)
                    root = bm.txtBox.transform.parent;
                if (root == null && bm.transform != null)
                    root = bm.transform;
                if (root == null) return null;

                Selectable[] selectables = root.GetComponentsInChildren<Selectable>(true);
                GameObject best = null;
                int bestScore = -9999;

                foreach (Selectable sel in selectables)
                {
                    if (sel == null) continue;
                    GameObject go = sel.gameObject;
                    if (!IsUsableBoxTabCandidate(go, bm)) continue;

                    int score = ScoreBoxTabCandidate(go, bm, tabIndex);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = go;
                    }
                }

                return bestScore > 0 ? best : null;
            }
            catch { }

            return null;
        }

        private static int ScoreBoxTabCandidate(GameObject go, BoxManager bm, int tabIndex)
        {
            int score = 0;
            try
            {
                string name = (go.name ?? string.Empty).ToLowerInvariant();
                string text = GetChildText(go).ToLowerInvariant();
                string combined = name + " " + text;

                if (combined.Contains("box")) score += 30;
                if (combined.Contains("tab")) score += 30;
                if (combined.Contains("page")) score += 10;
                if (combined.Contains("1") || combined.Contains("01")) score += 20;
                if (combined.Contains("global") || combined.Contains("local")) score -= 50;
                if (combined.Contains("entry") || combined.Contains("mon") || combined.Contains("send") || combined.Contains("cancel")) score -= 100;

                // Prefer controls visually above the first mon slot.
                if (bm.buttonBoxEntry != null && bm.buttonBoxEntry.Length > 0 && bm.buttonBoxEntry[0] != null)
                {
                    float candidateY = go.transform.position.y;
                    float firstSlotY = bm.buttonBoxEntry[0].transform.position.y;
                    if (candidateY > firstSlotY) score += 25;
                    else score -= 10;
                }

                // Prefer objects close to the box title/header label if present.
                if (bm.txtBox != null)
                {
                    float d = Vector3.Distance(go.transform.position, bm.txtBox.transform.position);
                    if (d < 2.5f) score += 10;
                }
            }
            catch { }

            return score;
        }

        private static string GetChildText(GameObject go)
        {
            try
            {
                TMP_Text[] tmps = go.GetComponentsInChildren<TMP_Text>(true);
                foreach (TMP_Text txt in tmps)
                {
                    if (txt != null && !string.IsNullOrEmpty(txt.text)) return txt.text;
                }

                Text[] texts = go.GetComponentsInChildren<Text>(true);
                foreach (Text txt in texts)
                {
                    if (txt != null && !string.IsNullOrEmpty(txt.text)) return txt.text;
                }
            }
            catch { }

            return string.Empty;
        }

        private static void EnsureButtons(GameScript gs, BoxManager bm)
        {
            if (gs == null || bm == null || gs.menuBox == null) return;

            Transform parent = bm.txtBox != null && bm.txtBox.transform != null && bm.txtBox.transform.parent != null
                ? bm.txtBox.transform.parent
                : gs.menuBox.transform;

            if (ButtonRoot == null)
            {
                ButtonRoot = new GameObject("GlobalBoxAccessNativeModeButtons", typeof(RectTransform));
                ButtonRoot.transform.SetParent(parent, false);
            }
            else if (ButtonRoot.transform.parent != parent)
            {
                ButtonRoot.transform.SetParent(parent, false);
            }

            RectTransform rootRt = ButtonRoot.GetComponent<RectTransform>();
            if (rootRt == null) rootRt = ButtonRoot.AddComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0f, 1f);
            rootRt.anchorMax = new Vector2(0f, 1f);
            rootRt.pivot = new Vector2(0f, 1f);
            rootRt.anchoredPosition = NativeRootPos;
            rootRt.sizeDelta = new Vector2(
                Mathf.Max(1f, Cfg(GlobalBoxRuntimeHost.NativeButtonWidth, 62f)),
                Mathf.Max(1f, Mathf.Abs(Cfg(GlobalBoxRuntimeHost.NativeButtonGapY, -15f)) + Cfg(GlobalBoxRuntimeHost.NativeButtonHeight, 14f) + 4f));

            if (BtnLocal == null)
                BtnLocal = CreateNativeModeButtonFromBoxTab(null, ButtonRoot.transform, "GlobalBoxAccessNative_LocalButton", false, NativeLocalButtonPos, out TxtLocal);

            if (BtnGlobal == null)
                BtnGlobal = CreateNativeModeButtonFromBoxTab(null, ButtonRoot.transform, "GlobalBoxAccessNative_GlobalButton", true, NativeGlobalButtonPos, out TxtGlobal);

            ApplyNativeButtonLayout(BtnLocal, false);
            ApplyNativeButtonLayout(BtnGlobal, true);
            ApplyNativeModeFont(bm);

            if (BtnLocal != null)
            {
                BtnLocal.onClick.RemoveAllListeners();
                BtnLocal.onClick.AddListener(() => ClickNativeModeButton(false));
            }

            if (BtnGlobal != null)
            {
                BtnGlobal.onClick.RemoveAllListeners();
                BtnGlobal.onClick.AddListener(() => ClickNativeModeButton(true));
            }

            WireNavigation(bm);
        }




        private static Button CreateButton(Transform parent, string name, string label, Vector2 pos, TMP_FontAsset font, Color accent, out TMP_Text text)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            // Keep the clickable area limited to the 10x10 box icon.
            rt.sizeDelta = new Vector2(10f, 10f);

            Image img = go.GetComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);
            img.raycastTarget = true;

            Button btn = go.GetComponent<Button>();

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            RectTransform trt = textGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            text = textGo.GetComponent<TextMeshProUGUI>();
            text.text = string.Empty;
            if (font != null) text.font = font;
            text.fontSize = 7.5f;
            text.fontStyle = FontStyles.Bold;
            text.enableAutoSizing = false;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.color = accent;

            return btn;
        }

        private static void EnsureModeButtonIcons(BoxManager bm)
        {
            try
            {
                if (bm == null || BtnLocal == null || BtnGlobal == null) return;

                CachedLocalBoxSprite = FindSpriteByName("boxColors_1") ?? CachedLocalBoxSprite;
                CachedGlobalBoxSprite = FindSpriteByName("boxColors_3") ?? CachedGlobalBoxSprite;

                Sprite fallbackSprite = null;
                try
                {
                    Image sourceImage = bm.inBoxIcon != null ? bm.inBoxIcon.GetComponent<Image>() : null;
                    if (sourceImage == null && bm.inBoxIcon != null) sourceImage = bm.inBoxIcon.GetComponentInChildren<Image>(true);
                    fallbackSprite = sourceImage != null ? sourceImage.sprite : null;
                }
                catch { }

                SetupModeButtonIconChild(BtnLocal, CachedLocalBoxSprite ?? fallbackSprite, Color.white);
                if (CachedGlobalBoxSprite != null)
                    SetupModeButtonIconChild(BtnGlobal, CachedGlobalBoxSprite, Color.white);
                else
                    SetupModeButtonIconChild(BtnGlobal, fallbackSprite, new Color(0.95f, 0.25f, 0.35f, 1f));

                MakeButtonBackgroundInvisible(BtnLocal);
                MakeButtonBackgroundInvisible(BtnGlobal);

                if (HighlightLocal != null) HighlightLocal.SetActive(false);
                if (HighlightGlobal != null) HighlightGlobal.SetActive(false);
            }
            catch (Exception ex)
            {
                GlobalBoxRuntimeHost.LogWarning("Mode button icon setup failed: " + ex.Message);
            }
        }

        private static void SetupModeButtonIconChild(Button button, Sprite sprite, Color color)
        {
            if (button == null) return;

            Image[] images = button.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image img = images[i];
                if (img == null || img.gameObject == button.gameObject) continue;
                if (img.gameObject.name.IndexOf("BoxIcon", StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (sprite != null) img.sprite = sprite;
                img.color = color;
                img.enabled = true;
                img.raycastTarget = false;

                RectTransform irt = img.GetComponent<RectTransform>();
                if (irt != null)
                {
                    bool isGlobal = button == BtnGlobal;
                    irt.anchoredPosition = isGlobal ? NativeGlobalIconPos : NativeLocalIconPos;
                    irt.sizeDelta = NativeIconSize;
                }
            }
        }


        private static Sprite FindSpriteByName(string spriteName)
        {
            try
            {
                Sprite[] sprites = Resources.FindObjectsOfTypeAll<Sprite>();
                foreach (Sprite sprite in sprites)
                {
                    if (sprite != null && sprite.name == spriteName)
                        return sprite;
                }
            }
            catch { }

            return null;
        }

        private static void EnsureModeHighlight(ref GameObject highlightObj, Button button, Sprite sprite, string name)
        {
            if (button == null) return;

            if (highlightObj == null)
            {
                highlightObj = new GameObject(name, typeof(RectTransform), typeof(Image));
                highlightObj.transform.SetParent(button.transform, false);
                highlightObj.transform.SetAsFirstSibling();
            }

            RectTransform rt = highlightObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;
                rt.sizeDelta = new Vector2(14f, 14f);
            }

            Image img = highlightObj.GetComponent<Image>();
            if (img != null)
            {
                if (sprite != null) img.sprite = sprite;
                img.color = new Color(1f, 1f, 1f, 0.55f);
                img.raycastTarget = false;
                img.enabled = true;
            }
        }

        private static void SetupModeIcon(GameObject iconObj, Sprite sprite, Color color)
        {
            if (iconObj == null) return;

            iconObj.SetActive(true);
            iconObj.transform.SetParent(iconObj.transform.parent, false);

            RectTransform rt = iconObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;
                rt.sizeDelta = new Vector2(10f, 10f);
            }
            else
            {
                iconObj.transform.localPosition = Vector3.zero;
                iconObj.transform.localScale = Vector3.one;
            }

            Image[] images = iconObj.GetComponentsInChildren<Image>(true);
            foreach (Image img in images)
            {
                if (img == null) continue;
                if (sprite != null) img.sprite = sprite;
                img.color = color;
                img.raycastTarget = false;
                img.enabled = true;
            }
        }

        private static void ClearButtonText(Button button)
        {
            if (button == null) return;

            TMP_Text[] tmps = button.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text txt in tmps)
            {
                if (txt != null) txt.text = string.Empty;
            }

            Text[] texts = button.GetComponentsInChildren<Text>(true);
            foreach (Text txt in texts)
            {
                if (txt != null) txt.text = string.Empty;
            }
        }

        private static void MakeButtonBackgroundInvisible(Button button)
        {
            if (button == null) return;

            Image img = button.GetComponent<Image>();
            if (img != null)
            {
                img.color = new Color(1f, 1f, 1f, 0f);
                img.raycastTarget = true;
            }
        }


        private static Button CreateNativeModeButtonFromBoxTab(GameObject template, Transform parent, string name, bool isGlobal, Vector2 pos, out TMP_Text text)
        {
            text = null;

            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(GlobalBoxNativeModeButtonRelay));
            go.transform.SetParent(parent, false);
            go.name = name;
            go.SetActive(true);
            go.transform.SetAsLastSibling();

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.localScale = Vector3.one;
            rt.sizeDelta = NativeButtonSize;

            Image bg = go.GetComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0f);
            bg.raycastTarget = true;

            Button btn = go.GetComponent<Button>();
            btn.targetGraphic = bg;

            GlobalBoxNativeModeButtonRelay relay = go.GetComponent<GlobalBoxNativeModeButtonRelay>();
            relay.IsGlobal = isGlobal;

            GameObject icon = new GameObject(isGlobal ? "GlobalBoxIcon" : "LocalBoxIcon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(go.transform, false);
            RectTransform irt = icon.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0f, 0.5f);
            irt.anchorMax = new Vector2(0f, 0.5f);
            irt.pivot = new Vector2(0f, 0.5f);
            irt.anchoredPosition = isGlobal ? NativeGlobalIconPos : NativeLocalIconPos;
            irt.sizeDelta = NativeIconSize;
            Image iconImg = icon.GetComponent<Image>();
            iconImg.raycastTarget = false;
            iconImg.color = Color.white;

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            RectTransform trt = textGo.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 0f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = new Vector2(Cfg(GlobalBoxRuntimeHost.NativeTextOffsetX, 16f), Cfg(GlobalBoxRuntimeHost.NativeTextOffsetYMin, -1f));
            trt.offsetMax = new Vector2(0f, Cfg(GlobalBoxRuntimeHost.NativeTextOffsetYMax, 1f));

            text = textGo.GetComponent<TextMeshProUGUI>();
            text.text = isGlobal ? "GLOBAL" : "LOCAL";
            text.fontSize = 7.5f;
            text.fontStyle = FontStyles.Normal;
            text.enableAutoSizing = false;
            text.alignment = TextAlignmentOptions.Left;
            text.raycastTarget = false;
            text.color = isGlobal ? new Color(0.95f, 0.25f, 0.35f, 1f) : new Color(0.25f, 0.78f, 0.45f, 1f);

            ApplyNativeButtonLayout(btn, isGlobal);
            return btn;
        }




        private static void ApplyNativeButtonLayout(Button btn, bool isGlobal)
        {
            if (btn == null) return;

            RectTransform rt = btn.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = isGlobal ? NativeGlobalButtonPos : NativeLocalButtonPos;
                rt.localScale = Vector3.one;
                rt.sizeDelta = NativeButtonSize;
            }

            Image[] images = btn.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image img = images[i];
                if (img == null || img.gameObject == btn.gameObject) continue;
                if (img.gameObject.name.IndexOf("BoxIcon", StringComparison.OrdinalIgnoreCase) < 0) continue;

                RectTransform irt = img.GetComponent<RectTransform>();
                if (irt != null)
                {
                    irt.anchorMin = new Vector2(0f, 0.5f);
                    irt.anchorMax = new Vector2(0f, 0.5f);
                    irt.pivot = new Vector2(0f, 0.5f);
                    irt.anchoredPosition = isGlobal ? NativeGlobalIconPos : NativeLocalIconPos;
                    irt.sizeDelta = NativeIconSize;
                }
            }

            TMP_Text[] tmps = btn.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < tmps.Length; i++)
            {
                TMP_Text tmp = tmps[i];
                if (tmp == null || tmp.gameObject.name != "Text") continue;
                RectTransform trt = tmp.GetComponent<RectTransform>();
                if (trt != null)
                {
                    trt.anchorMin = new Vector2(0f, 0f);
                    trt.anchorMax = new Vector2(1f, 1f);
                    trt.offsetMin = new Vector2(Cfg(GlobalBoxRuntimeHost.NativeTextOffsetX, 16f), Cfg(GlobalBoxRuntimeHost.NativeTextOffsetYMin, -1f));
                    trt.offsetMax = new Vector2(0f, Cfg(GlobalBoxRuntimeHost.NativeTextOffsetYMax, 1f));
                }
            }
        }


        private static void ApplyNativeModeFont(BoxManager bm)
        {
            try
            {
                TMP_FontAsset font = null;
                try
                {
                    if (bm != null && bm.txtBox != null)
                        font = bm.txtBox.font;
                }
                catch { }

                if (font == null)
                {
                    TMP_Text[] allText = UnityEngine.Object.FindObjectsOfType<TMP_Text>();
                    if (allText != null)
                    {
                        for (int i = 0; i < allText.Length; i++)
                        {
                            if (allText[i] != null && allText[i].font != null)
                            {
                                font = allText[i].font;
                                break;
                            }
                        }
                    }
                }

                if (font != null)
                {
                    if (TxtLocal != null) TxtLocal.font = font;
                    if (TxtGlobal != null) TxtGlobal.font = font;
                }
            }
            catch { }
        }

        private static void RestoreNativeModeButtonFocus(bool isGlobal)
        {
            try
            {
                Button btn = isGlobal ? BtnGlobal : BtnLocal;
                if (btn == null || btn.gameObject == null)
                    return;

                EventSystem es = EventSystem.current;
                if (es != null)
                    es.SetSelectedGameObject(btn.gameObject);

                HandleNativeModeButtonSelected(btn.gameObject);
            }
            catch { }
        }

        private static IEnumerator RestoreNativeModeButtonFocusNextFrame(bool isGlobal)
        {
            yield return null;
            RestoreNativeModeButtonFocus(isGlobal);
        }

        internal static void HandleNativeModeButtonSelected(GameObject selected)
        {
            try
            {
                if (selected == null) return;

                GameScript gs = UnityEngine.Object.FindObjectOfType<GameScript>();
                if (gs == null) return;

                if (gs.arrowRightObj != null)
                {
                    gs.arrowRightObj.transform.SetParent(selected.transform);

                    bool isGlobal = BtnGlobal != null && selected == BtnGlobal.gameObject;
                    gs.arrowRightObj.transform.localPosition = isGlobal ? NativeCursorGlobalPos : NativeCursorLocalPos;
                    gs.arrowRightObj.transform.localScale = Vector3.one;
                    gs.arrowRightObj.SetActive(true);
                }

                gs.lastButtonObj = selected;
            }
            catch { }
        }


        internal static void ClickNativeModeButton(bool isGlobal)
        {
            try
            {
                GameScript gs = UnityEngine.Object.FindObjectOfType<GameScript>();
                if (gs == null || gs.boxManager == null) return;

                EnsureInitialized(gs, gs.boxManager);

                if (isGlobal)
                {
                    if (!ViewingGlobal)
                        ToggleToGlobal(gs, gs.boxManager);
                    else
                        UpdateButtonVisuals();
                }
                else
                {
                    if (ViewingGlobal)
                        ToggleToLocal(gs, gs.boxManager);
                    else
                        UpdateButtonVisuals();
                }

                // The mode switch refreshes the box and vanilla code may move selection to Box tab 0.
                // For the native Local/Global buttons, keep focus on the button that was confirmed
                // so the player can continue moving around from that button after the switch.
                EnsureButtons(gs, gs.boxManager);
                EnsureModeButtonIcons(gs.boxManager);
                ApplyNativeModeFont(gs.boxManager);
                UpdateButtonVisuals();
                RestoreNativeModeButtonFocus(isGlobal);

                if (GlobalBoxRuntimeHost.Instance != null)
                    GlobalBoxRuntimeHost.Instance.StartCoroutine(RestoreNativeModeButtonFocusNextFrame(isGlobal));
            }
            catch (Exception ex)
            {
                GlobalBoxRuntimeHost.LogWarning("Native Local/Global mode click failed: " + ex.Message);
            }
        }



        private static void WireNavigation(BoxManager bm)
        {
            try
            {
                if (GlobalBoxRuntimeHost.NativeVirtualTabNavigation != null && !GlobalBoxRuntimeHost.NativeVirtualTabNavigation.Value)
                    return;

                if (bm == null || BtnLocal == null || BtnGlobal == null)
                    return;

                Selectable localSel = BtnLocal.GetComponent<Selectable>();
                Selectable globalSel = BtnGlobal.GetComponent<Selectable>();
                if (localSel == null || globalSel == null)
                    return;

                Selectable tabTopRight = null;
                Selectable tabBottomRight = null;
                Selectable tabZero = null;
                Selectable entryZero = null;

                GameObject tab0Obj = FindBoxTabFromDecompiledField(bm, 0);
                if (tab0Obj != null) tabZero = tab0Obj.GetComponent<Selectable>();

                GameObject tab11Obj = FindBoxTabFromDecompiledField(bm, 11);
                if (tab11Obj != null) tabTopRight = tab11Obj.GetComponent<Selectable>();

                GameObject tab23Obj = FindBoxTabFromDecompiledField(bm, 23);
                if (tab23Obj != null) tabBottomRight = tab23Obj.GetComponent<Selectable>();

                if (bm.buttonBoxEntry != null && bm.buttonBoxEntry.Length > 0 && bm.buttonBoxEntry[0] != null)
                    entryZero = bm.buttonBoxEntry[0].GetComponent<Selectable>();

                Navigation navLocal = new Navigation { mode = Navigation.Mode.Explicit };
                navLocal.selectOnDown = globalSel;
                navLocal.selectOnLeft = tabTopRight != null ? tabTopRight : tabZero;
                navLocal.selectOnRight = tabTopRight != null ? tabTopRight : tabZero;
                BtnLocal.navigation = navLocal;

                Navigation navGlobal = new Navigation { mode = Navigation.Mode.Explicit };
                navGlobal.selectOnUp = localSel;
                navGlobal.selectOnLeft = tabBottomRight != null ? tabBottomRight : tabZero;
                navGlobal.selectOnRight = tabBottomRight != null ? tabBottomRight : tabZero;
                navGlobal.selectOnDown = entryZero != null ? entryZero : tabZero;
                BtnGlobal.navigation = navGlobal;

                if (tabTopRight != null)
                {
                    Navigation nav = tabTopRight.navigation;
                    nav.mode = Navigation.Mode.Explicit;
                    nav.selectOnRight = localSel;
                    if (tabBottomRight != null && nav.selectOnDown == null) nav.selectOnDown = tabBottomRight;
                    tabTopRight.navigation = nav;
                }

                if (tabBottomRight != null)
                {
                    Navigation nav = tabBottomRight.navigation;
                    nav.mode = Navigation.Mode.Explicit;
                    nav.selectOnRight = globalSel;
                    if (tabTopRight != null && nav.selectOnUp == null) nav.selectOnUp = tabTopRight;
                    tabBottomRight.navigation = nav;
                }

                NativeNavigationWired = true;
            }
            catch (Exception ex)
            {
                GlobalBoxRuntimeHost.LogWarning("Native Local/Global navigation wiring failed: " + ex.Message);
            }
        }



        private static void UpdateButtonVisuals()
        {
            try
            {
                if (TxtLocal != null)
                {
                    TxtLocal.text = "LOCAL";
                    TxtLocal.color = ViewingGlobal ? new Color(0.45f, 0.65f, 0.48f, 1f) : new Color(0.15f, 0.85f, 0.35f, 1f);
                }

                if (TxtGlobal != null)
                {
                    TxtGlobal.text = "GLOBAL";
                    TxtGlobal.color = ViewingGlobal ? new Color(1f, 0.35f, 0.45f, 1f) : new Color(0.72f, 0.32f, 0.38f, 1f);
                }

                MakeButtonBackgroundInvisible(BtnLocal);
                MakeButtonBackgroundInvisible(BtnGlobal);

                if (IconLocal != null) IconLocal.SetActive(false);
                if (IconGlobal != null) IconGlobal.SetActive(false);
                if (HighlightLocal != null) HighlightLocal.SetActive(false);
                if (HighlightGlobal != null) HighlightGlobal.SetActive(false);
            }
            catch { }
        }


        internal static IEnumerator DelayedEnsureSendToButton(GameScript gs)
        {
            yield return null;
            EnsureSendToButton(gs);
            yield return null;
            EnsureSendToButton(gs);
        }

        internal static void EnsureSendToButton(GameScript gs)
        {
            try
            {
                if (gs == null || gs.boxManager == null || gs.menuBox == null)
                {
                    HideSendToExtraAndRestoreCancel();
                    return;
                }

                if (!gs.menuBox.activeSelf || gs.subMenuSendToTeamTown == null || !gs.subMenuSendToTeamTown.activeSelf)
                {
                    HideSendToExtraAndRestoreCancel();
                    return;
                }

                if (gs.buttonsubMenuSendToTeamTown == null || gs.buttonsubMenuSendToTeamTown.Length < 3)
                    return;

                // Make room for the extra Global/Local option without replacing Cancel.
                MoveCancelDownIfNeeded(gs);
                CreateSendToExtraButton(gs);

                if (SendToExtraButton == null) return;

                SendToExtraButton.SetActive(true);
                SetButtonLabel(SendToExtraButton, ViewingGlobal ? "Local" : "Global");

                Button btn = SendToExtraButton.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                    {
                        GameScript g = UnityEngine.Object.FindObjectOfType<GameScript>();
                        if (g != null) SendSelectedBetweenLocalAndGlobal(g);
                    });
                }

                WireSendToNavigation(gs);
            }
            catch (Exception ex)
            {
                GlobalBoxRuntimeHost.LogWarning("Ensure Send To Global/Local failed: " + ex.Message);
            }
        }

        private static void HideSendToExtraAndRestoreCancel()
        {
            try
            {
                if (SendToExtraButton != null) SendToExtraButton.SetActive(false);
                RestoreCancelIfNeeded();
            }
            catch { }
        }

        private static void ForceButtonLabel(GameObject button, string label, Color color, GameScript gs)
        {
            if (button == null) return;

            TMP_Text[] tmps = button.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text t in tmps)
            {
                if (t != null && t.gameObject.name != "GlobalBoxSendToLabel")
                    t.text = "";
            }
            Text[] texts = button.GetComponentsInChildren<Text>(true);
            foreach (Text t in texts)
            {
                if (t != null && t.gameObject.name != "GlobalBoxSendToLabel")
                    t.text = "";
            }

            Transform existing = button.transform.Find("GlobalBoxSendToLabel");
            TextMeshProUGUI tmp;
            if (existing == null)
            {
                GameObject go = new GameObject("GlobalBoxSendToLabel", typeof(RectTransform));
                go.transform.SetParent(button.transform, false);
                RectTransform rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                tmp = go.AddComponent<TextMeshProUGUI>();
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.enableWordWrapping = false;
                tmp.raycastTarget = false;
            }
            else
            {
                tmp = existing.GetComponent<TextMeshProUGUI>();
                if (tmp == null) tmp = existing.gameObject.AddComponent<TextMeshProUGUI>();
            }

            try
            {
                if (gs != null && gs.boxManager != null && gs.boxManager.txtBox != null && gs.boxManager.txtBox.font != null)
                    tmp.font = gs.boxManager.txtBox.font;
            }
            catch { }

            tmp.text = label;
            tmp.color = color;
            tmp.fontSize = 9f;
            tmp.fontStyle = FontStyles.Normal;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.gameObject.SetActive(true);
        }

        private static void WireThirdSendToNavigation(GameScript gs)
        {
            try
            {
                if (gs.buttonsubMenuSendToTeamTown == null || gs.buttonsubMenuSendToTeamTown.Length < 3) return;
                Selectable team = gs.buttonsubMenuSendToTeamTown[0].GetComponent<Selectable>();
                Selectable town = gs.buttonsubMenuSendToTeamTown[1].GetComponent<Selectable>();
                Selectable third = gs.buttonsubMenuSendToTeamTown[2].GetComponent<Selectable>();
                if (town == null || third == null) return;

                Navigation navTown = town.navigation;
                navTown.mode = Navigation.Mode.Explicit;
                navTown.selectOnDown = third;
                if (team != null) navTown.selectOnUp = team;
                town.navigation = navTown;

                Navigation navThird = third.navigation;
                navThird.mode = Navigation.Mode.Explicit;
                navThird.selectOnUp = town;
                third.navigation = navThird;
            }
            catch { }
        }

        private static void CreateSendToExtraButton(GameScript gs)
        {
            try
            {
                if (SendToExtraButton == null)
                {
                    GameObject template = null;
                    if (gs.buttonsubMenuSendToTeamTown != null)
                    {
                        // Clone Town if possible because it is a normal menu option, not the cancel option.
                        if (gs.buttonsubMenuSendToTeamTown.Length > 1 && gs.buttonsubMenuSendToTeamTown[1] != null)
                            template = gs.buttonsubMenuSendToTeamTown[1];
                        else if (gs.buttonsubMenuSendToTeamTown.Length > 0)
                            template = gs.buttonsubMenuSendToTeamTown[0];
                    }
                    if (template == null || gs.subMenuSendToTeamTown == null) return;

                    SendToExtraButton = UnityEngine.Object.Instantiate(template, template.transform.parent);
                    SendToExtraButton.name = "ButtonSendToGlobalOrLocal";

                    // Important: cloning the vanilla Town button also clones its persistent SendToTown listener.
                    // Replace the Button component so this option can only run our Global/Local transfer.
                    Button oldButton = SendToExtraButton.GetComponent<Button>();
                    Image targetImage = SendToExtraButton.GetComponent<Image>();
                    if (oldButton != null)
                    {
                        UnityEngine.Object.DestroyImmediate(oldButton);
                    }
                    Button freshButton = SendToExtraButton.AddComponent<Button>();
                    if (targetImage != null)
                        freshButton.targetGraphic = targetImage;
                }

                RectTransform extraRt = SendToExtraButton.GetComponent<RectTransform>();
                RectTransform t0 = gs.buttonsubMenuSendToTeamTown.Length > 0 && gs.buttonsubMenuSendToTeamTown[0] != null ? gs.buttonsubMenuSendToTeamTown[0].GetComponent<RectTransform>() : null;
                RectTransform t1 = gs.buttonsubMenuSendToTeamTown.Length > 1 && gs.buttonsubMenuSendToTeamTown[1] != null ? gs.buttonsubMenuSendToTeamTown[1].GetComponent<RectTransform>() : null;
                RectTransform t2 = gs.buttonsubMenuSendToTeamTown.Length > 2 && gs.buttonsubMenuSendToTeamTown[2] != null ? gs.buttonsubMenuSendToTeamTown[2].GetComponent<RectTransform>() : null;

                if (extraRt != null)
                {
                    // Force order/spacing every time the Send To menu opens:
                    // Team, Town, Global/Local, Cancel.
                    if (t0 != null && t1 != null)
                    {
                        Vector2 step = t1.anchoredPosition - t0.anchoredPosition;
                        if (Mathf.Abs(step.y) < 0.01f) step = new Vector2(0f, -24f);
                        extraRt.anchoredPosition = t1.anchoredPosition + step;
                    }
                    else if (t2 != null)
                    {
                        extraRt.anchoredPosition = t2.anchoredPosition;
                    }
                }

                // Keep the cloned text/style; only change the word.
                SetButtonLabel(SendToExtraButton, ViewingGlobal ? "Local" : "Global");
                SendToExtraButton.transform.SetAsLastSibling();
            }
            catch (Exception ex)
            {
                GlobalBoxRuntimeHost.LogWarning("Create Send To Global/Local button failed: " + ex.Message);
            }
        }

        private static void MoveCancelDownIfNeeded(GameScript gs)
        {
            try
            {
                if (gs.buttonsubMenuSendToTeamTown == null || gs.buttonsubMenuSendToTeamTown.Length < 3) return;
                SendToCancelButton = gs.buttonsubMenuSendToTeamTown[2];
                if (SendToCancelButton == null) return;

                RectTransform t0 = gs.buttonsubMenuSendToTeamTown[0].GetComponent<RectTransform>();
                RectTransform t1 = gs.buttonsubMenuSendToTeamTown[1].GetComponent<RectTransform>();
                RectTransform tc = SendToCancelButton.GetComponent<RectTransform>();
                if (t0 == null || t1 == null || tc == null) return;

                if (!SendToOriginalsCaptured)
                {
                    SendToTeamOriginalLocalPos = t0.anchoredPosition;
                    SendToTownOriginalLocalPos = t1.anchoredPosition;
                    SendToCancelOriginalLocalPos = tc.anchoredPosition;
                    RectTransform menuOriginalRt = gs.subMenuSendToTeamTown != null ? gs.subMenuSendToTeamTown.GetComponent<RectTransform>() : null;
                    SendToMenuOriginalSize = menuOriginalRt != null ? menuOriginalRt.sizeDelta : Vector2.zero;
                    SendToOriginalsCaptured = true;
                }

                t0.anchoredPosition = SendToTeamOriginalLocalPos + new Vector3(0f, SendToMenuYOffset, 0f);
                t1.anchoredPosition = SendToTownOriginalLocalPos + new Vector3(0f, SendToMenuYOffset, 0f);

                Vector2 step = t1.anchoredPosition - t0.anchoredPosition;
                if (Mathf.Abs(step.y) < 0.01f) step = new Vector2(0f, -24f);

                // Force the final order every time the menu opens:
                // Team, Town, Global/Local, Cancel.
                tc.anchoredPosition = t1.anchoredPosition + (step * 2f);

                RectTransform menuRt = gs.subMenuSendToTeamTown != null ? gs.subMenuSendToTeamTown.GetComponent<RectTransform>() : null;
                if (menuRt != null)
                {
                    // Do not keep growing this every time Send To opens.
                    // Set it to the original menu height plus one row for Global/Local.
                    Vector2 sz = SendToMenuOriginalSize != Vector2.zero ? SendToMenuOriginalSize : menuRt.sizeDelta;
                    sz.y = SendToMenuOriginalSize.y + Mathf.Abs(step.y);
                    menuRt.sizeDelta = sz;
                }

                SendToCancelMoved = true;
            }
            catch { }
        }

        private static void RestoreCancelIfNeeded()
        {
            try
            {
                if (!SendToCancelMoved || SendToCancelButton == null) return;
                RectTransform rt = SendToCancelButton.GetComponent<RectTransform>();
                if (rt != null) rt.anchoredPosition = SendToCancelOriginalLocalPos;
                try
                {
                    GameScript gsMenu = UnityEngine.Object.FindFirstObjectByType<GameScript>();
                    if (gsMenu != null && gsMenu.subMenuSendToTeamTown != null)
                    {
                        RectTransform menuRt = gsMenu.subMenuSendToTeamTown.GetComponent<RectTransform>();
                        if (menuRt != null && SendToMenuOriginalSize != Vector2.zero) menuRt.sizeDelta = SendToMenuOriginalSize;
                    }
                } catch { }
                // Restore vanilla Team/Town positions when the Send To menu closes.
                try
                {
                    GameScript gs = UnityEngine.Object.FindFirstObjectByType<GameScript>();
                    if (gs != null && gs.buttonsubMenuSendToTeamTown != null && gs.buttonsubMenuSendToTeamTown.Length > 1)
                    {
                        RectTransform t0 = gs.buttonsubMenuSendToTeamTown[0].GetComponent<RectTransform>();
                        RectTransform t1 = gs.buttonsubMenuSendToTeamTown[1].GetComponent<RectTransform>();
                        if (t0 != null) t0.anchoredPosition = SendToTeamOriginalLocalPos;
                        if (t1 != null) t1.anchoredPosition = SendToTownOriginalLocalPos;
                    }
                } catch { }
                SendToCancelMoved = false;
            }
            catch { }
        }

        private static void WireSendToNavigation(GameScript gs)
        {
            try
            {
                if (SendToExtraButton == null || gs.buttonsubMenuSendToTeamTown == null || gs.buttonsubMenuSendToTeamTown.Length < 3) return;
                Selectable team = gs.buttonsubMenuSendToTeamTown[0].GetComponent<Selectable>();
                Selectable town = gs.buttonsubMenuSendToTeamTown[1].GetComponent<Selectable>();
                Selectable cancel = gs.buttonsubMenuSendToTeamTown[2].GetComponent<Selectable>();
                Selectable extra = SendToExtraButton.GetComponent<Selectable>();
                if (town == null || cancel == null || extra == null) return;

                Navigation navTown = town.navigation;
                navTown.mode = Navigation.Mode.Explicit;
                navTown.selectOnDown = extra;
                if (team != null) navTown.selectOnUp = team;
                town.navigation = navTown;

                Navigation navExtra = new Navigation { mode = Navigation.Mode.Explicit };
                navExtra.selectOnUp = town;
                navExtra.selectOnDown = cancel;
                extra.navigation = navExtra;

                Navigation navCancel = cancel.navigation;
                navCancel.mode = Navigation.Mode.Explicit;
                navCancel.selectOnUp = extra;
                cancel.navigation = navCancel;
            }
            catch { }
        }

        private static void SetButtonLabel(GameObject button, string label)
        {
            if (button == null) return;
            TMP_Text tmp = button.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
            {
                tmp.text = label;
                return;
            }
            Text txt = button.GetComponentInChildren<Text>(true);
            if (txt != null) txt.text = label;
        }

        internal static void SendSelectedBetweenLocalAndGlobal(GameScript gs)
        {
            BoxManager bm = gs != null ? gs.boxManager : null;
            if (gs == null || bm == null) return;
            if (bm.pickedUpMon != null)
            {
                GlobalBoxRuntimeHost.LogWarning("Cannot send to Global/Local while holding a mon.");
                return;
            }

            if (LocalBoxRef == null) LocalBoxRef = ViewingGlobal ? LocalBoxRef : bm.boxMons;
            if (GlobalMons == null || GlobalMons.Length != SlotCount) LoadGlobal(gs);

            int slot = bm.curBoxEntry + bm.curBoxTab * 27;
            if (slot < 0 || slot >= SlotCount) return;

            if (ViewingGlobal)
                SendGlobalSlotToLocal(gs, bm, slot);
            else
                SendLocalSlotToGlobal(gs, bm, slot);

            try
            {
                if (gs.subMenuSendToTeamTown != null) gs.subMenuSendToTeamTown.SetActive(false);
                if (bm.subMenu != null) bm.subMenu.SetActive(false);
                if (gs.eventSystem != null && bm.buttonBoxEntry != null && bm.curBoxEntry >= 0 && bm.curBoxEntry < bm.buttonBoxEntry.Length)
                    gs.eventSystem.SetSelectedGameObject(bm.buttonBoxEntry[bm.curBoxEntry]);
            }
            catch { }
        }

        private static void SendLocalSlotToGlobal(GameScript gs, BoxManager bm, int slot)
        {
            Mon[] local = LocalBoxRef ?? bm.boxMons;
            Mon mon = local[slot];
            if (mon == null) return;

            int dest = FirstEmpty(GlobalMons);
            if (dest < 0)
            {
                GlobalBoxRuntimeHost.LogWarning("Global Box is full.");
                return;
            }

            GlobalMons[dest] = mon;
            local[slot] = null;
            bm.boxMons = local;
            SaveGlobal(gs);
            ForceSaveAfterLocalGlobalTransfer(gs, $"local slot {slot} -> global slot {dest}");
            RefreshSafe(bm);
            UpdateButtonVisuals();
            GlobalBoxRuntimeHost.LogInfo($"Sent local slot {slot} to Global Box slot {dest}.");
        }

        private static void SendGlobalSlotToLocal(GameScript gs, BoxManager bm, int slot)
        {
            Mon mon = GlobalMons[slot];
            if (mon == null) return;

            Mon[] local = LocalBoxRef;
            if (local == null)
            {
                GlobalBoxRuntimeHost.LogWarning("Local box reference was missing; cannot send Global to Local.");
                return;
            }

            int dest = FirstEmpty(local);
            if (dest < 0)
            {
                GlobalBoxRuntimeHost.LogWarning("Local Box is full.");
                return;
            }

            local[dest] = mon;
            GlobalMons[slot] = null;
            bm.boxMons = GlobalMons;
            SaveGlobal(gs);
            ForceSaveAfterLocalGlobalTransfer(gs, $"global slot {slot} -> local slot {dest}");
            RefreshSafe(bm);
            UpdateButtonVisuals();
            GlobalBoxRuntimeHost.LogInfo($"Sent Global Box slot {slot} to local slot {dest}.");
        }

        private static int FirstEmpty(Mon[] arr)
        {
            if (arr == null) return -1;
            for (int i = 0; i < arr.Length; i++)
                if (arr[i] == null) return i;
            return -1;
        }

    }



    internal sealed class GlobalBoxNativeModeButtonRelay : MonoBehaviour, ISelectHandler, IPointerEnterHandler
    {
        public bool IsGlobal;

        public void OnSelect(BaseEventData eventData)
        {
            GlobalBoxState.HandleNativeModeButtonSelected(gameObject);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            try
            {
                EventSystem es = EventSystem.current;
                if (es != null)
                    es.SetSelectedGameObject(gameObject);
            }
            catch { }

            GlobalBoxState.HandleNativeModeButtonSelected(gameObject);
        }
    }

    [HarmonyPatch(typeof(GameScript), "Update")]
    internal static class GameScriptUpdateSelectTogglePatch
    {
        private static void Postfix(GameScript __instance)
        {
            GlobalBoxState.HandleSelectButtonToggle(__instance);
        }
    }

    [HarmonyPatch(typeof(GameScript), nameof(GameScript.SendTo))]
    internal static class SendToOpenPatch
    {
        public static void Postfix(GameScript __instance)
        {
            try
            {
                GlobalBoxState.EnsureSendToButton(__instance);
                __instance.StartCoroutine(GlobalBoxState.DelayedEnsureSendToButton(__instance));
            }
            catch (Exception ex)
            {
                GlobalBoxRuntimeHost.LogWarning("SendTo postfix failed: " + ex.Message);
            }
        }
    }

    [HarmonyPatch(typeof(BoxManager), nameof(BoxManager.RefreshBox))]
    internal static class RefreshBoxPatch
    {
        private static void Postfix(BoxManager __instance)
        {
            if (GlobalBoxState.SuppressRefreshHook) return;
            GameScript gs = __instance != null ? __instance.gameScript : null;
            GlobalBoxState.EnsureInitialized(gs, __instance);

            if (GlobalBoxState.ViewingGlobal)
            {
                if (__instance == null || GlobalBoxState.GlobalMons == null || !object.ReferenceEquals(__instance.boxMons, GlobalBoxState.GlobalMons))
                {
                    // Safety guard for save-slot/title transitions: never treat an arbitrary BoxManager.boxMons
                    // array as global. This prevents a newly loaded local box from overwriting globalBox.txt.
                    GlobalBoxState.RecoverStaleGlobalView(__instance, "RefreshBox displayed array was not GlobalMons");
                    return;
                }

                // Keep global moves/rearranges persistent whenever the real GlobalMons array refreshes.
                GlobalBoxState.SaveGlobal(gs);
                GlobalBoxState.ApplyGlobalBoxLabel(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(GameScript), nameof(GameScript.LoadGame))]
    internal static class LoadGameGlobalBoxSessionPatch
    {
        private static void Prefix(GameScript __instance)
        {
            GlobalBoxState.PrepareForSaveSessionBoundary(__instance, "LoadGame");
        }

        private static void Postfix(GameScript __instance)
        {
            GlobalBoxState.AttachFreshLocalBoxAfterLoad(__instance);
        }
    }

    [HarmonyPatch(typeof(GameScript), nameof(GameScript.ReturnToTitle))]
    internal static class ReturnToTitleGlobalBoxSessionPatch
    {
        private static void Prefix(GameScript __instance)
        {
            GlobalBoxState.PrepareForSaveSessionBoundary(__instance, "ReturnToTitle");
        }
    }

    [HarmonyPatch(typeof(GameScript), nameof(GameScript.SaveGame))]
    internal static class SaveGamePatch
    {
        private static bool wasGlobal;
        private static BoxManager bm;
        private static Mon[] globalRef;

        private static bool Prefix(GameScript __instance)
        {
            bm = __instance != null ? __instance.boxManager : null;
            globalRef = null;
            wasGlobal = GlobalBoxState.ViewingGlobal && bm != null;
            if (!wasGlobal) return true;

            bool visibleArrayIsGlobal = GlobalBoxState.GlobalMons != null && object.ReferenceEquals(bm.boxMons, GlobalBoxState.GlobalMons);
            if (!visibleArrayIsGlobal)
            {
                // Do not let stale global mode save a local save slot into globalBox.txt.
                GlobalBoxState.RecoverStaleGlobalView(bm, "SaveGame displayed array was not GlobalMons");
                wasGlobal = false;
                return true;
            }

            GlobalBoxState.SaveGlobal(__instance);
            globalRef = bm.boxMons;

            if (GlobalBoxState.LocalBoxRef == null)
            {
                // Skipping vanilla SaveGame is safer than writing GlobalMons into the local save file.
                GlobalBoxRuntimeHost.LogWarning("Skipped vanilla SaveGame while viewing Global Box because LocalBoxRef was missing.");
                wasGlobal = false;
                return false;
            }

            bm.boxMons = GlobalBoxState.LocalBoxRef;
            return true;
        }

        private static void Postfix(GameScript __instance)
        {
            if (!wasGlobal || bm == null || globalRef == null) return;
            bm.boxMons = globalRef;
            GlobalBoxState.GlobalMons = globalRef;
            GlobalBoxState.RefreshSafe(bm);
            wasGlobal = false;
        }
    }
}
