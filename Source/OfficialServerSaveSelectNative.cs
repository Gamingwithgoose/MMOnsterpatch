using Mono.Cecil;
using Mono.Cecil.Cil;
using BepInEx;
using BepInEx.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Goose.Monsterpatch.GTSAllInOnePatcher;

namespace Goose.Monsterpatch.OfficialServer
{
    internal static class OfficialServerSaveSelectNativePatch
    {
        public static int Patch(AssemblyDefinition assembly)
        {
            int hooks = 0;
            ModuleDefinition module = assembly.MainModule;
            TypeDefinition menuScript = module.Types.FirstOrDefault(t => t.Name == "MenuScript");
            if (menuScript == null)
            {
                Console.WriteLine("[MMOnsterpatch Official Server] MenuScript not found; native save-select hooks were not injected.");
                return 0;
            }

            hooks += PatchRefreshSaveFiles(module, menuScript);
            hooks += PatchHoverSaveFile(module, menuScript);
            hooks += PatchUpdate(module, menuScript);
            hooks += PatchPlay(module, menuScript);
            hooks += PatchSelectSaveFile(module, menuScript);
            hooks += PatchSaveSystemDeleteSaveBlock(module);
            hooks += PatchCancel(module, menuScript);
            hooks += PatchSaveSystemSaveGameBlock(module);
            hooks += PatchGameScriptReturnToTitle(module);
            return hooks;
        }

        private static int PatchRefreshSaveFiles(ModuleDefinition module, TypeDefinition menuScript)
        {
            MethodDefinition m = menuScript.Methods.FirstOrDefault(x => x.Name == "RefreshSaveFiles" && x.HasBody && !x.IsStatic);
            if (m == null)
            {
                Console.WriteLine("[MMOnsterpatch Official Server] MenuScript.RefreshSaveFiles not found.");
                return 0;
            }
            MethodReference call = module.ImportReference(typeof(OfficialServerSaveSelectNativeRuntime).GetMethod(nameof(OfficialServerSaveSelectNativeRuntime.EnsureForMenuObject), BindingFlags.Public | BindingFlags.Static));
            ILProcessor il = m.Body.GetILProcessor();
            int count = 0;
            foreach (Instruction ret in m.Body.Instructions.Where(i => i.OpCode == OpCodes.Ret).ToArray())
            {
                il.InsertBefore(ret, il.Create(OpCodes.Ldarg_0));
                il.InsertBefore(ret, il.Create(OpCodes.Call, call));
                count++;
            }
            Console.WriteLine("[MMOnsterpatch Official Server] MenuScript.RefreshSaveFiles native save-select hook(s)=" + count);
            return count;
        }

        private static int PatchHoverSaveFile(ModuleDefinition module, TypeDefinition menuScript)
        {
            MethodDefinition m = menuScript.Methods.FirstOrDefault(x => x.Name == "HoverSaveFile" && x.HasBody && !x.IsStatic && x.Parameters.Count == 1);
            if (m == null)
            {
                Console.WriteLine("[MMOnsterpatch Official Server] MenuScript.HoverSaveFile(int) not found.");
                return 0;
            }

            ILProcessor il = m.Body.GetILProcessor();
            Instruction first = m.Body.Instructions.FirstOrDefault();
            int count = 0;

            // Pre-hook: while Official Server online slot mode is active, stop vanilla HoverSaveFile
            // before it shows local/offline save previews. This prevents the local-save flashing that
            // happened when moving between online slots.
            if (first != null)
            {
                MethodReference blockCall = module.ImportReference(typeof(OfficialServerSaveSelectNativeRuntime).GetMethod(nameof(OfficialServerSaveSelectNativeRuntime.TryBlockOnlineHoverSaveFile), BindingFlags.Public | BindingFlags.Static));
                il.InsertBefore(first, il.Create(OpCodes.Ldarg_0));
                il.InsertBefore(first, il.Create(OpCodes.Ldarg_1));
                il.InsertBefore(first, il.Create(OpCodes.Call, blockCall));
                il.InsertBefore(first, il.Create(OpCodes.Brfalse, first));
                il.InsertBefore(first, il.Create(OpCodes.Ret));
                count++;
            }

            MethodReference call = module.ImportReference(typeof(OfficialServerSaveSelectNativeRuntime).GetMethod(nameof(OfficialServerSaveSelectNativeRuntime.OnHoverSaveFile), BindingFlags.Public | BindingFlags.Static));
            foreach (Instruction ret in m.Body.Instructions.Where(i => i.OpCode == OpCodes.Ret).ToArray())
            {
                il.InsertBefore(ret, il.Create(OpCodes.Ldarg_0));
                il.InsertBefore(ret, il.Create(OpCodes.Ldarg_1));
                il.InsertBefore(ret, il.Create(OpCodes.Call, call));
                count++;
            }
            Console.WriteLine("[MMOnsterpatch Official Server] MenuScript.HoverSaveFile native save-select hook(s)=" + count + " (includes online pre-block).");
            return count;
        }

        private static int PatchUpdate(ModuleDefinition module, TypeDefinition menuScript)
        {
            MethodDefinition m = menuScript.Methods.FirstOrDefault(x => x.Name == "Update" && x.HasBody && !x.IsStatic && x.Parameters.Count == 0);
            if (m == null)
            {
                Console.WriteLine("[MMOnsterpatch Official Server] MenuScript.Update not found.");
                return 0;
            }
            MethodReference call = module.ImportReference(typeof(OfficialServerSaveSelectNativeRuntime).GetMethod(nameof(OfficialServerSaveSelectNativeRuntime.TickMenuObject), BindingFlags.Public | BindingFlags.Static));
            ILProcessor il = m.Body.GetILProcessor();
            Instruction first = m.Body.Instructions.FirstOrDefault();
            if (first == null)
                return 0;
            il.InsertBefore(first, il.Create(OpCodes.Ldarg_0));
            il.InsertBefore(first, il.Create(OpCodes.Call, call));
            Console.WriteLine("[MMOnsterpatch Official Server] MenuScript.Update native save-select tick hook(s)=1");
            return 1;
        }

        private static int PatchPlay(ModuleDefinition module, TypeDefinition menuScript)
        {
            MethodDefinition m = menuScript.Methods.FirstOrDefault(x => x.Name == "Play" && x.HasBody && !x.IsStatic && x.Parameters.Count == 0);
            if (m == null)
            {
                Console.WriteLine("[MMOnsterpatch Official Server] MenuScript.Play not found.");
                return 0;
            }
            MethodReference call = module.ImportReference(typeof(OfficialServerSaveSelectNativeRuntime).GetMethod(nameof(OfficialServerSaveSelectNativeRuntime.OnPlayButtonInvoked), BindingFlags.Public | BindingFlags.Static));
            ILProcessor il = m.Body.GetILProcessor();
            Instruction first = m.Body.Instructions.FirstOrDefault();
            if (first == null)
                return 0;
            il.InsertBefore(first, il.Create(OpCodes.Ldarg_0));
            il.InsertBefore(first, il.Create(OpCodes.Call, call));
            Console.WriteLine("[MMOnsterpatch Official Server] MenuScript.Play native save-select prehook(s)=1");
            return 1;
        }

        private static int PatchSelectSaveFile(ModuleDefinition module, TypeDefinition menuScript)
        {
            MethodDefinition m = menuScript.Methods.FirstOrDefault(x => x.Name == "SelectSaveFile" && x.HasBody && !x.IsStatic && x.Parameters.Count == 1);
            if (m == null)
            {
                Console.WriteLine("[MMOnsterpatch Official Server] MenuScript.SelectSaveFile(int) not found for online save-select intercept.");
                return 0;
            }

            MethodReference call = module.ImportReference(typeof(OfficialServerSaveSelectNativeRuntime).GetMethod(nameof(OfficialServerSaveSelectNativeRuntime.TryHandleSelectSaveFile), BindingFlags.Public | BindingFlags.Static));
            ILProcessor il = m.Body.GetILProcessor();
            Instruction first = m.Body.Instructions.FirstOrDefault();
            if (first == null)
                return 0;

            il.InsertBefore(first, il.Create(OpCodes.Ldarg_0));
            il.InsertBefore(first, il.Create(OpCodes.Ldarg_1));
            il.InsertBefore(first, il.Create(OpCodes.Call, call));
            il.InsertBefore(first, il.Create(OpCodes.Brfalse, first));
            il.InsertBefore(first, il.Create(OpCodes.Ret));
            Console.WriteLine("[MMOnsterpatch Official Server] MenuScript.SelectSaveFile online save-select intercept hook(s)=1");
            return 1;
        }

        private static int PatchSaveSystemDeleteSaveBlock(ModuleDefinition module)
        {
            TypeDefinition saveSystem = module.Types.FirstOrDefault(t => t.Name == "SaveSystem");
            if (saveSystem == null)
            {
                Console.WriteLine("[MMOnsterpatch Official Server] SaveSystem not found for online delete redirect.");
                return 0;
            }

            MethodDefinition m = saveSystem.Methods.FirstOrDefault(x => x.Name == "DeleteSave" && x.HasBody && x.Parameters.Count == 1);
            if (m == null)
            {
                Console.WriteLine("[MMOnsterpatch Official Server] SaveSystem.DeleteSave(int) not found for online delete redirect.");
                return 0;
            }

            MethodReference call = module.ImportReference(typeof(OfficialServerSaveSelectNativeRuntime).GetMethod(nameof(OfficialServerSaveSelectNativeRuntime.TryHandleOnlineDeleteSaveAndBlockLocal), BindingFlags.Public | BindingFlags.Static));
            ILProcessor il = m.Body.GetILProcessor();
            Instruction first = m.Body.Instructions.FirstOrDefault();
            if (first == null)
                return 0;

            // This keeps the official MenuScript delete flow intact:
            // DeleteASaveFile -> SelectSaveFile -> deleteSaveFile dialogue -> ActuallyDeleteSaveFile.
            // Only the disk-level SaveSystem.DeleteSave call is redirected while Official Server Online Mode is active.
            il.InsertBefore(first, il.Create(OpCodes.Ldarg_0));
            il.InsertBefore(first, il.Create(OpCodes.Call, call));
            il.InsertBefore(first, il.Create(OpCodes.Brfalse, first));
            il.InsertBefore(first, il.Create(OpCodes.Ret));

            Console.WriteLine("[MMOnsterpatch Official Server] SaveSystem.DeleteSave online server-delete/local-disk block hook(s)=1");
            return 1;
        }

        private static int PatchCancel(ModuleDefinition module, TypeDefinition menuScript)
        {
            MethodDefinition m = menuScript.Methods.FirstOrDefault(x => x.Name == "Cancel" && x.HasBody && !x.IsStatic && x.Parameters.Count == 0);
            if (m == null)
            {
                Console.WriteLine("[MMOnsterpatch Official Server] MenuScript.Cancel not found for online disconnect hook.");
                return 0;
            }
            MethodReference call = module.ImportReference(typeof(OfficialServerSaveSelectNativeRuntime).GetMethod(nameof(OfficialServerSaveSelectNativeRuntime.OnMenuCancelInvoked), BindingFlags.Public | BindingFlags.Static));
            ILProcessor il = m.Body.GetILProcessor();
            Instruction first = m.Body.Instructions.FirstOrDefault();
            if (first == null)
                return 0;
            il.InsertBefore(first, il.Create(OpCodes.Ldarg_0));
            il.InsertBefore(first, il.Create(OpCodes.Call, call));
            Console.WriteLine("[MMOnsterpatch Official Server] MenuScript.Cancel online disconnect hook(s)=1");
            return 1;
        }

        private static int PatchSaveSystemSaveGameBlock(ModuleDefinition module)
        {
            TypeDefinition saveSystem = module.Types.FirstOrDefault(t => t.Name == "SaveSystem");
            if (saveSystem == null)
            {
                Console.WriteLine("[MMOnsterpatch Official Server] SaveSystem not found for online local-save safety block.");
                return 0;
            }
            MethodDefinition m = saveSystem.Methods.FirstOrDefault(x => x.Name == "SaveGame" && x.HasBody && x.Parameters.Count == 2);
            if (m == null)
            {
                Console.WriteLine("[MMOnsterpatch Official Server] SaveSystem.SaveGame(SaveData,int) not found for online local-save safety block.");
                return 0;
            }
            MethodReference call = module.ImportReference(typeof(OfficialServerSaveSelectNativeRuntime).GetMethod(nameof(OfficialServerSaveSelectNativeRuntime.ShouldHandleOnlineSaveAndBlockLocalWrite), BindingFlags.Public | BindingFlags.Static));
            ILProcessor il = m.Body.GetILProcessor();
            Instruction first = m.Body.Instructions.FirstOrDefault();
            if (first == null)
                return 0;
            il.InsertBefore(first, il.Create(OpCodes.Ldarg_0));
            il.InsertBefore(first, il.Create(OpCodes.Ldarg_1));
            il.InsertBefore(first, il.Create(OpCodes.Call, call));
            il.InsertBefore(first, il.Create(OpCodes.Brfalse, first));
            il.InsertBefore(first, il.Create(OpCodes.Ret));
            Console.WriteLine("[MMOnsterpatch Official Server] SaveSystem.SaveGame online server-save/local-write block hook(s)=1");
            return 1;
        }

        private static int PatchGameScriptReturnToTitle(ModuleDefinition module)
        {
            TypeDefinition gameScript = module.Types.FirstOrDefault(t => t.Name == "GameScript");
            if (gameScript == null)
            {
                Console.WriteLine("[MMOnsterpatch Official Server] GameScript not found for ReturnToTitle disconnect hook.");
                return 0;
            }
            MethodDefinition m = gameScript.Methods.FirstOrDefault(x => x.Name == "ReturnToTitle" && x.HasBody && !x.IsStatic && x.Parameters.Count == 0);
            if (m == null)
            {
                Console.WriteLine("[MMOnsterpatch Official Server] GameScript.ReturnToTitle not found for online disconnect hook.");
                return 0;
            }
            MethodReference call = module.ImportReference(typeof(OfficialServerSaveSelectNativeRuntime).GetMethod(nameof(OfficialServerSaveSelectNativeRuntime.OnReturnToTitleInvoked), BindingFlags.Public | BindingFlags.Static));
            ILProcessor il = m.Body.GetILProcessor();
            Instruction first = m.Body.Instructions.FirstOrDefault();
            if (first == null)
                return 0;
            il.InsertBefore(first, il.Create(OpCodes.Call, call));
            Console.WriteLine("[MMOnsterpatch Official Server] GameScript.ReturnToTitle online disconnect hook(s)=1");
            return 1;
        }
    }

    public sealed class OfficialServerSaveSelectCoroutineHost : MonoBehaviour
    {
    }

    public sealed class OfficialServerSaveSelectNativeRelay : MonoBehaviour, IPointerEnterHandler, ISelectHandler, ISubmitHandler, IPointerClickHandler
    {
        public void OnPointerEnter(PointerEventData eventData)
        {
            OfficialServerSaveSelectNativeRuntime.OnModeButtonHovered();
        }

        public void OnSelect(BaseEventData eventData)
        {
            OfficialServerSaveSelectNativeRuntime.OnModeButtonHovered();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            OfficialServerSaveSelectNativeRuntime.OnModeButtonClicked();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OfficialServerSaveSelectNativeRuntime.OnModeButtonClicked();
        }
    }

    public sealed class OfficialServerSaveSelectDeleteRelay : MonoBehaviour, IPointerEnterHandler, ISelectHandler, ISubmitHandler, IPointerClickHandler
    {
        public void OnPointerEnter(PointerEventData eventData)
        {
            OfficialServerSaveSelectNativeRuntime.OnDeleteButtonHovered();
        }

        public void OnSelect(BaseEventData eventData)
        {
            OfficialServerSaveSelectNativeRuntime.OnDeleteButtonHovered();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            OfficialServerSaveSelectNativeRuntime.OnDeleteButtonClicked();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OfficialServerSaveSelectNativeRuntime.OnDeleteButtonClicked();
        }
    }

    public static class OfficialServerSaveSelectNativeRuntime
    {
        private static object _menuObj;
        private static GameObject _menuSaveFiles;
        private static GameObject _root;
        private static GameObject _modeButton;
        private static TMP_Text _modeButtonText;
        private static GameObject _deleteButton;
        private static TMP_Text _deleteButtonText;
        private static TMP_Text _statusText;
        private static bool _onlineMode;
        private static bool _authBusy;
        private static string _statusOverride;
        private static bool _officialOnlineSaveSessionActive;
        private static int _officialOnlineSaveSessionSlot = -1;
        private static OfficialServerSaveSelectCoroutineHost _host;
        private static bool _loggedMissingMenu;
        private static bool _loggedHidden;
        private static float _nextTick;
        private static int _lastHoverSlot = -1;
        private static bool _layoutLogged;
        private static Transform _visibleSaveSlotParent;
        private static bool _onlineHoverBlockLogged;
        private static bool _localSaveBlockLogged;
        private static float _modeClickGuardUntil;
        private static float _deleteClickGuardUntil;
        private static bool _deleteMode;
        private static int _pendingLocalDeleteSlot = -1;
        private static float _pendingLocalDeleteCheckUntil;
        private static float _pendingSelectFirstSlotAfter;
        private static float _pendingSelectFirstSlotUntil;
        private static bool _pendingSelectFirstSlotLoggedDeferral;
        private static bool _onlineSaveDataClearedLogged;
        private static bool _onlineSlotPayloadsLoaded;
        private static readonly object[] _onlineSaveSlotCache = new object[6];
        private static bool _refreshingOnlineSlotsFromCache;
        private static bool _returnToTitleFromOfficialDisconnect;
        private static float _lastOfficialSaveUploadAt;
        private static Image _onlineWallpaperTargetImage;
        private static Sprite _originalSaveSelectBackgroundSprite;
        private static Color _originalSaveSelectBackgroundColor = Color.white;
        private static Sprite _embeddedOnlineWallpaperSprite;
        private static Texture2D _embeddedOnlineWallpaperTexture;
        private static string _onlineWallpaperTargetPath;
        private static bool _onlineWallpaperSwapLogged;

        private const string ConfigFileName = "goose.monsterpatch.officialserver.cfg";
        private static ConfigFile _config;
        private static ConfigEntry<float> ButtonX;
        private static ConfigEntry<float> ButtonY;
        private static ConfigEntry<float> ButtonWidth;
        private static ConfigEntry<float> ButtonHeight;
        private static ConfigEntry<float> ButtonFontSize;
        private static ConfigEntry<float> DeleteButtonX;
        private static ConfigEntry<float> DeleteButtonY;
        private static ConfigEntry<float> DeleteButtonWidth;
        private static ConfigEntry<float> DeleteButtonHeight;
        private static ConfigEntry<float> DeleteButtonFontSize;
        private static ConfigEntry<float> DeleteButtonCursorX;
        private static ConfigEntry<float> DeleteButtonCursorY;
        private static ConfigEntry<float> StatusX;
        private static ConfigEntry<float> StatusY;
        private static ConfigEntry<float> StatusWidth;
        private static ConfigEntry<float> StatusHeight;
        private static ConfigEntry<float> StatusFontSize;
        private static ConfigEntry<string> ServerHost;
        private static ConfigEntry<int> SocialPort;
        private static ConfigEntry<int> NetworkTimeoutMs;
        private static ConfigEntry<bool> OnlineWallpaperTintEnabled;
        private static ConfigEntry<string> OnlineWallpaperTintColor;
        private static ConfigEntry<float> OnlineWallpaperMinWidth;
        private static ConfigEntry<float> OnlineWallpaperMinHeight;

        [Serializable]
        private sealed class OfficialOnlineSaveSlotInfo
        {
            public int slot;
            public int occupied;
            public string save_json;
            public string display_name;
            public long updated_at;
        }

        [Serializable]
        private sealed class OfficialOnlineSaveSlotsResponse
        {
            public OfficialOnlineSaveSlotInfo[] slots;
        }

        public static void OnPlayButtonInvoked(object menuObj)
        {
            // This fires before the game's Play2 coroutine fades into menuSaveFiles.
            // It is kept only to capture the MenuScript instance early; visible UI is created
            // by RefreshSaveFiles/Update once menuSaveFiles is actually active.
            _menuObj = menuObj;
            Log("MenuScript.Play observed; waiting for native Play2 to activate menuSaveFiles.");
        }

        public static void TickMenuObject(object menuObj)
        {
            try
            {
                if (Time.unscaledTime < _nextTick)
                    return;
                _nextTick = Time.unscaledTime + 0.20f;

                if (menuObj == null)
                    return;

                _menuObj = menuObj;
                GameObject saveMenu = GetGameObjectField(menuObj, "menuSaveFiles");
                if (saveMenu == null)
                {
                    if (!_loggedMissingMenu)
                    {
                        _loggedMissingMenu = true;
                        Log("MenuScript.Update tick could not read field menuSaveFiles. Save-select UI cannot attach.");
                    }
                    return;
                }

                bool visible = saveMenu.activeInHierarchy || saveMenu.activeSelf;
                if (!visible)
                {
                    if (_root != null && _root.activeSelf)
                        _root.SetActive(false);
                    if (!_loggedHidden)
                    {
                        _loggedHidden = true;
                        Log("menuSaveFiles found but not visible yet; native save-select UI remains hidden.");
                    }
                    return;
                }

                EnsureForMenuObject(menuObj);
                UpdateVisualState();
                HandlePendingSaveSlotSelection();
                HandlePendingLocalDeleteCompletion();
            }
            catch (Exception ex)
            {
                Log("TickMenuObject failed: " + ex.Message);
            }
        }

        public static void EnsureForMenuObject(object menuObj)
        {
            try
            {
                if (menuObj == null)
                    return;

                _menuObj = menuObj;
                GameObject saveMenu = GetGameObjectField(menuObj, "menuSaveFiles");
                if (saveMenu == null)
                {
                    if (!_loggedMissingMenu)
                    {
                        _loggedMissingMenu = true;
                        Log("EnsureForMenuObject could not read MenuScript.menuSaveFiles.");
                    }
                    return;
                }

                _menuSaveFiles = saveMenu;
                bool visible = saveMenu.activeInHierarchy || saveMenu.activeSelf;
                if (_root == null)
                {
                    CreateNativeRootAndControls(menuObj, saveMenu);
                }

                if (_root != null)
                {
                    _root.transform.SetAsLastSibling();
                    _root.SetActive(visible);
                    UpdateVisualState();
                }
            }
            catch (Exception ex)
            {
                Log("EnsureForMenuObject failed: " + ex);
            }
        }

        public static void OnHoverSaveFile(object menuObj, int slot)
        {
            try
            {
                _lastHoverSlot = slot;
                EnsureForMenuObject(menuObj);
                UpdateVisualState();
                LogOncePerSlot(slot, "HoverSaveFile observed on native save slot " + slot + "; official preview panel is active path.");
            }
            catch (Exception ex)
            {
                Log("OnHoverSaveFile failed: " + ex.Message);
            }
        }

        public static void OnModeButtonHovered()
        {
            try
            {
                if (_menuObj != null)
                {
                    MethodInfo hover = _menuObj.GetType().GetMethod("HoverButtonLarge", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (hover != null && _modeButton != null)
                        hover.Invoke(_menuObj, new object[] { _modeButton });
                }
            }
            catch { }
        }

        public static void OnModeButtonClicked()
        {
            try
            {
                if (Time.unscaledTime < _modeClickGuardUntil)
                {
                    Log("Mode button click ignored during short input guard.");
                    return;
                }
                _modeClickGuardUntil = Time.unscaledTime + 0.35f;

                EnsureHostComponent();

                if (_onlineMode)
                {
                    SwitchToOfflineSaveSelect();
                    return;
                }

                if (_authBusy)
                {
                    Log("Switch to Online Mode ignored because Steam/server authentication is already in progress.");
                    return;
                }

                // If the GTS/AIO auth host is carrying a stale busy flag from a prior
                // title transition or closed game session, clear it before starting the
                // Official Server login. This prevents the save-select button from
                // sitting on Connecting forever without opening Steam auth.
                try
                {
                    if (GTSRuntimeHost.IsAuthBusyForAio() && !GTSRuntimeHost.IsLoggedInForAio())
                    {
                        Log("Detected stale Steam/AIO auth busy state before Official Server login; resetting auth state.");
                        GTSRuntimeHost.ResetStaleOfficialServerAuthForAio();
                    }
                }
                catch { }

                if (_host != null)
                    _host.StartCoroutine(AuthenticateThenShowOnlineSlots());
                else
                    Log("Switch to Online Mode could not start authentication coroutine because the save-select host component is missing.");
            }
            catch (Exception ex)
            {
                Log("OnModeButtonClicked failed: " + ex.Message);
            }
        }

        public static void OnDeleteButtonHovered()
        {
            try
            {
                if (_menuObj != null)
                {
                    MethodInfo hover = _menuObj.GetType().GetMethod("HoverButtonLarge", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (hover != null && _deleteButton != null)
                    {
                        hover.Invoke(_menuObj, new object[] { _deleteButton });
                        PositionDeleteButtonCursor();
                    }
                }
            }
            catch { }
        }

        public static void OnDeleteButtonClicked()
        {
            try
            {
                if (Time.unscaledTime < _deleteClickGuardUntil)
                {
                    Log("Delete button click ignored during short input guard.");
                    return;
                }
                _deleteClickGuardUntil = Time.unscaledTime + 0.30f;

                if (_authBusy || !_onlineMode)
                {
                    Log("Delete button ignored because delete is only available on the Official Server Online Mode save-select screen.");
                    return;
                }

                if (IsNativeDeleteModeActive(_menuObj))
                {
                    SetDeleteMode(false, "Cancel button");
                    return;
                }

                // Use the game's official Extras-menu delete entry point. It sets deletingSaveFile
                // and runs MenuScript.Play()/Play2(), which gives the native red delete screen and
                // confirmation dialogue path. Online save data is re-applied from the server cache
                // after Play2 reloads local saveData from disk.
                bool invokedOfficial = TryInvokeMenuMethod(_menuObj, "DeleteASaveFile");
                if (invokedOfficial)
                {
                    Log("Delete button invoked official MenuScript.DeleteASaveFile flow for Online Mode.");
                    ApplyOnlineSaveSlotVisuals();
                    RequestSelectFirstSaveSlot();
                    UpdateVisualState();
                }
                else
                {
                    SetDeleteMode(true, "Delete button fallback");
                }
            }
            catch (Exception ex)
            {
                Log("OnDeleteButtonClicked failed: " + ex.Message);
            }
        }

        private static IEnumerator AuthenticateThenShowOnlineSlots()
        {
            _authBusy = true;
            _statusOverride = "Server Status: Connecting";
            UpdateVisualState();
            Log("Switch to Online Mode clicked. Starting Steam authentication from the native save-select screen.");

            bool authenticated = false;
            bool authRoutineFailed = false;
            IEnumerator authRoutine = null;

            try
            {
                authRoutine = GTSRuntimeHost.EnsureSteamAuthForAioCoroutine();
            }
            catch (Exception ex)
            {
                authRoutineFailed = true;
                Log("Steam authentication routine could not be created from save-select: " + ex.Message);
            }

            if (authRoutine != null)
            {
                float authStart = Time.realtimeSinceStartup;
                const float officialAuthWatchdogSeconds = 90f;
                while (true)
                {
                    if (Time.realtimeSinceStartup - authStart > officialAuthWatchdogSeconds)
                    {
                        authRoutineFailed = true;
                        Log("Official Server Steam authentication timed out before completion; clearing stale auth state.");
                        try { GTSRuntimeHost.ResetStaleOfficialServerAuthForAio(); } catch { }
                        break;
                    }
                    object current = null;
                    bool moved = false;

                    try
                    {
                        moved = authRoutine.MoveNext();
                        if (moved)
                            current = authRoutine.Current;
                    }
                    catch (Exception ex)
                    {
                        authRoutineFailed = true;
                        Log("Steam authentication from save-select failed: " + ex.Message);
                        break;
                    }

                    if (!moved)
                        break;

                    yield return current;
                }
            }

            if (!authRoutineFailed)
            {
                try
                {
                    authenticated = GTSRuntimeHost.IsLoggedInForAio();
                }
                catch (Exception ex)
                {
                    Log("Steam authentication status check from save-select failed: " + ex.Message);
                }
            }

            _authBusy = false;
            if (authenticated)
            {
                _onlineMode = true;
                _statusOverride = "Server Status: Loading Saves";
                UpdateVisualState();
                Log("Steam authentication complete. Loading Official Server online save slots.");
                bool loaded = FetchOnlineSaveSlotsFromServer();
                _statusOverride = loaded ? null : "Server Status: Save Load Failed";
                InvokeMenuMethod(_menuObj, "RefreshSaveFiles");
                ApplyOnlineSaveSlotVisuals();
                RequestSelectFirstSaveSlot();
            }
            else
            {
                _onlineMode = false;
                _statusOverride = "Server Status: Auth Failed";
                Log("Steam authentication did not complete. Staying on local/offline save slots.");
            }

            UpdateVisualState();
        }

        private static void SwitchToOfflineSaveSelect()
        {
            _modeClickGuardUntil = Time.unscaledTime + 0.75f;
            DisconnectServerSession("Switch to Offline Mode");
            _onlineMode = false;
            _authBusy = false;
            _statusOverride = null;
            _onlineSaveDataClearedLogged = false;
            ClearOfficialOnlineSaveSession("Switch to Offline Mode");
            ClearOnlineSaveSlotCache();
            SetDeleteMode(false, "Switch to Offline Mode");
            RestoreOnlineWallpaperColors();
            Log("Switch to Offline Mode clicked. Restoring vanilla local save-slot screen.");
            RestoreLocalSaveDataFromDisk();
            InvokeMenuMethod(_menuObj, "RefreshSaveFiles");
            RepairLocalSaveSlotVisuals();
            RequestSelectFirstSaveSlot();
            UpdateVisualState();
        }

        private static void SetDeleteMode(bool active, string reason)
        {
            try
            {
                _deleteMode = active;
                if (!active)
                {
                    _pendingLocalDeleteSlot = -1;
                    _pendingLocalDeleteCheckUntil = 0f;
                }

                if (_menuObj != null)
                    SetObjectField(_menuObj, "deletingSaveFile", active);

                GameObject panelTopDelete = GetGameObjectField(_menuObj, "panelTopDeleteSave");
                if (panelTopDelete != null)
                    panelTopDelete.SetActive(active);

                GameObject preview = GetGameObjectField(_menuObj, "panelSaveDataPreview");
                if (preview != null)
                {
                    Image previewImg = preview.GetComponent<Image>();
                    Sprite sprite = GetObjectField(_menuObj, active ? "sliceMenuRed" : "sliceMenu") as Sprite;
                    if (previewImg != null && sprite != null)
                        previewImg.sprite = sprite;
                    preview.SetActive(false);
                }

                if (!active && _lastOfficialSaveUploadAt <= 0f)
                    _statusOverride = null;

                InvokeMenuMethod(_menuObj, "RefreshSaveFiles");
                if (_onlineMode)
                    ApplyOnlineSaveSlotVisuals();

                RequestSelectFirstSaveSlot();
                UpdateVisualState();
                Log((active ? "Delete Save mode enabled" : "Delete Save mode canceled") + " from " + reason + ".");
            }
            catch (Exception ex)
            {
                Log("SetDeleteMode failed: " + ex.Message);
            }
        }

        private static bool IsNativeDeleteModeActive(object menuObj)
        {
            try
            {
                object value = GetObjectField(menuObj, "deletingSaveFile");
                return value is bool b && b;
            }
            catch { return _deleteMode; }
        }

        private static void CreateNativeRootAndControls(object menuObj, GameObject saveMenu)
        {
            EnsureConfig();

            GameObject slot0 = GetGameObjectFromArrayField(menuObj, "saveFileObj", 0);
            GameObject slot5 = GetGameObjectFromArrayField(menuObj, "saveFileObj", 5);
            GameObject preview = GetGameObjectField(menuObj, "panelSaveDataPreview");

            // Important: do NOT parent to the save-slot Panel. The previous build used the visible slot
            // parent as the custom root and that disturbed the native save-slot layout. The real save
            // screen owns a top-level menuSaveSelect transform; vanilla slot panel and preview panel are
            // siblings under it. Custom controls belong there as siblings, not inside the slot grid.
            Transform parent = null;
            GameObject menuSaveSelect = GetGameObjectField(menuObj, "menuSaveSelect");
            if (menuSaveSelect != null && menuSaveSelect.transform != null)
                parent = menuSaveSelect.transform;
            else if (preview != null && preview.transform != null && preview.transform.parent != null)
                parent = preview.transform.parent;
            else if (saveMenu != null && saveMenu.transform != null)
                parent = saveMenu.transform;

            _visibleSaveSlotParent = parent;
            Log("Creating native save-select controls as siblings under '" + (parent != null ? parent.name : "null") + "' so vanilla slot layout is not moved.");

            if (_root == null)
            {
                _root = new GameObject("MMOnsterpatchOfficialServer_SaveSelectNativeControls", typeof(RectTransform), typeof(CanvasGroup));
                RectTransform rootRt = _root.GetComponent<RectTransform>();
                rootRt.SetParent(parent, false);
                rootRt.anchorMin = new Vector2(0.5f, 0.5f);
                rootRt.anchorMax = new Vector2(0.5f, 0.5f);
                rootRt.pivot = new Vector2(0.5f, 0.5f);
                rootRt.anchoredPosition = Vector2.zero;
                rootRt.localPosition = Vector3.zero;
                rootRt.localScale = Vector3.one;
                rootRt.sizeDelta = new Vector2(1f, 1f);
                CanvasGroup cg = _root.GetComponent<CanvasGroup>();
                _host = _root.GetComponent<OfficialServerSaveSelectCoroutineHost>();
                if (_host == null) _host = _root.AddComponent<OfficialServerSaveSelectCoroutineHost>();
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = false;
                _root.transform.SetAsLastSibling();
                _root.SetActive(true);
            }
            else if (_root.transform.parent != parent && parent != null)
            {
                _root.transform.SetParent(parent, false);
                _root.transform.SetAsLastSibling();
            }

            LogNativeLayout(menuObj, slot0, slot5, preview, parent);
            CreateStatusText(menuObj);
            CreateModeButton(menuObj);
            CreateDeleteButton(menuObj);
            ForceVisibleRecursive(_root);
            UpdateVisualState();
        }

        private static void CreateStatusText(object menuObj)
        {
            if (_statusText != null)
                return;

            Transform parent = _root != null ? _root.transform : _visibleSaveSlotParent;
            if (parent == null)
                return;

            GameObject go = new GameObject("MMOnsterpatchOfficialServer_ServerStatusText_NATIVE", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            go.transform.SetAsLastSibling();
            _statusText = go.GetComponent<TextMeshProUGUI>();

            TMP_Text txtPreview = GetObjectField(menuObj, "txtPreview") as TMP_Text;
            if (txtPreview != null)
            {
                _statusText.font = txtPreview.font;
                _statusText.fontMaterial = txtPreview.fontMaterial;
                _statusText.fontSharedMaterial = txtPreview.fontSharedMaterial;
            }

            RectTransform rt = _statusText.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.sizeDelta = new Vector2(Cfg(StatusWidth, 120f), Cfg(StatusHeight, 18f));
            // menuSaveSelect native coordinates from decompile/log: preview panel lives at x=44,y=0;
            // save-slot Panel lives at x=-74,y=0. Config lets this be tuned in-game-style
            // native coordinates without rebuilding.
            rt.anchoredPosition = new Vector2(Cfg(StatusX, -150f), Cfg(StatusY, 82f));
            rt.localPosition = new Vector3(rt.localPosition.x, rt.localPosition.y, 0f);

            _statusText.enableWordWrapping = false;
            _statusText.overflowMode = TextOverflowModes.Overflow;
            _statusText.alignment = TextAlignmentOptions.Left;
            _statusText.fontSize = Cfg(StatusFontSize, 4f);
            _statusText.raycastTarget = false;
            _statusText.color = new Color(0.45f, 0.10f, 0.10f, 1f);
            go.SetActive(true);
            Log("Native status text created under " + parent.name + "; local=" + rt.localPosition + "; anchored=" + rt.anchoredPosition + "; fontCopied=" + (txtPreview != null) + ".");
        }

        private static void CreateModeButton(object menuObj)
        {
            if (_modeButton != null)
                return;

            Transform parent = _root != null ? _root.transform : _visibleSaveSlotParent;
            if (parent == null)
                return;

            _modeButton = new GameObject("MMOnsterpatchOfficialServer_SwitchOnlineModeButton_NATIVE", typeof(RectTransform), typeof(Image), typeof(Button), typeof(OfficialServerSaveSelectNativeRelay));
            _modeButton.transform.SetParent(parent, false);
            _modeButton.transform.SetAsLastSibling();
            _modeButton.SetActive(true);

            RectTransform brt = _modeButton.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.5f, 0.5f);
            brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.localScale = Vector3.one;
            brt.localRotation = Quaternion.identity;
            brt.sizeDelta = new Vector2(Cfg(ButtonWidth, 110f), Cfg(ButtonHeight, 18f));
            // Under the native preview panel (panelSaveDataPreview local x=44,y=0,height=124).
            // Config values use the same menuSaveSelect native coordinate space.
            brt.anchoredPosition = new Vector2(Cfg(ButtonX, 44f), Cfg(ButtonY, -76f));
            brt.localPosition = new Vector3(brt.localPosition.x, brt.localPosition.y, 0f);

            Image img = _modeButton.GetComponent<Image>();
            img.raycastTarget = true;
            img.color = new Color(1f, 1f, 1f, 1f);
            Sprite buttonSprite = GetImageSprite(GetGameObjectField(menuObj, "buttonAlphaOk"));
            if (buttonSprite == null)
                buttonSprite = GetImageSprite(GetGameObjectField(menuObj, "panelSaveDataPreview"));
            if (buttonSprite != null)
            {
                img.sprite = buttonSprite;
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 1f;
            }

            Button btn = _modeButton.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnModeButtonClicked);

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(_modeButton.transform, false);
            RectTransform trt = textGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(2f, 0f);
            trt.offsetMax = new Vector2(-2f, 0f);
            trt.localScale = Vector3.one;
            trt.localRotation = Quaternion.identity;

            _modeButtonText = textGo.GetComponent<TextMeshProUGUI>();
            TMP_Text txtPreview = GetObjectField(menuObj, "txtPreview") as TMP_Text;
            if (txtPreview != null)
            {
                _modeButtonText.font = txtPreview.font;
                _modeButtonText.fontMaterial = txtPreview.fontMaterial;
                _modeButtonText.fontSharedMaterial = txtPreview.fontSharedMaterial;
            }
            _modeButtonText.alignment = TextAlignmentOptions.Center;
            _modeButtonText.enableWordWrapping = false;
            _modeButtonText.overflowMode = TextOverflowModes.Overflow;
            _modeButtonText.fontSize = Cfg(ButtonFontSize, 5.5f);
            _modeButtonText.raycastTarget = false;
            _modeButtonText.color = new Color(0.17f, 0.10f, 0.10f, 1f);

            Log("Native switch button created under " + parent.name + "; local=" + brt.localPosition + "; anchored=" + brt.anchoredPosition + "; size=" + brt.sizeDelta + "; sprite=" + (buttonSprite != null ? buttonSprite.name : "none") + "; fontCopied=" + (txtPreview != null) + ".");
        }

        private static void CreateDeleteButton(object menuObj)
        {
            if (_deleteButton != null)
                return;

            Transform parent = _root != null ? _root.transform : _visibleSaveSlotParent;
            if (parent == null)
                return;

            _deleteButton = new GameObject("MMOnsterpatchOfficialServer_DeleteSaveButton_NATIVE", typeof(RectTransform), typeof(Image), typeof(Button), typeof(OfficialServerSaveSelectDeleteRelay));
            _deleteButton.transform.SetParent(parent, false);
            _deleteButton.transform.SetAsLastSibling();
            _deleteButton.SetActive(true);

            RectTransform brt = _deleteButton.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.5f, 0.5f);
            brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.localScale = Vector3.one;
            brt.localRotation = Quaternion.identity;
            brt.sizeDelta = new Vector2(Cfg(DeleteButtonWidth, 72f), Cfg(DeleteButtonHeight, 18f));
            // Centered under the native 2x3 save-slot grid. The save-slot Panel lives at about x=-74,y=0 in menuSaveSelect space.
            brt.anchoredPosition = new Vector2(Cfg(DeleteButtonX, -74f), Cfg(DeleteButtonY, -76f));
            brt.localPosition = new Vector3(brt.localPosition.x, brt.localPosition.y, 0f);

            Image img = _deleteButton.GetComponent<Image>();
            img.raycastTarget = true;
            img.color = new Color(1f, 1f, 1f, 1f);
            Sprite buttonSprite = GetImageSprite(GetGameObjectField(menuObj, "buttonAlphaOk"));
            if (buttonSprite == null)
                buttonSprite = GetImageSprite(GetGameObjectField(menuObj, "panelSaveDataPreview"));
            if (buttonSprite != null)
            {
                img.sprite = buttonSprite;
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 1f;
            }

            Button btn = _deleteButton.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnDeleteButtonClicked);

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(_deleteButton.transform, false);
            RectTransform trt = textGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(2f, 0f);
            trt.offsetMax = new Vector2(-2f, 0f);
            trt.localScale = Vector3.one;
            trt.localRotation = Quaternion.identity;

            _deleteButtonText = textGo.GetComponent<TextMeshProUGUI>();
            TMP_Text txtPreview = GetObjectField(menuObj, "txtPreview") as TMP_Text;
            if (txtPreview != null)
            {
                _deleteButtonText.font = txtPreview.font;
                _deleteButtonText.fontMaterial = txtPreview.fontMaterial;
                _deleteButtonText.fontSharedMaterial = txtPreview.fontSharedMaterial;
            }
            _deleteButtonText.alignment = TextAlignmentOptions.Center;
            _deleteButtonText.enableWordWrapping = false;
            _deleteButtonText.overflowMode = TextOverflowModes.Overflow;
            _deleteButtonText.fontSize = Cfg(DeleteButtonFontSize, 5.5f);
            _deleteButtonText.raycastTarget = false;
            _deleteButtonText.color = new Color(0.17f, 0.10f, 0.10f, 1f);

            Log("Native Delete Save button created under " + parent.name + "; local=" + brt.localPosition + "; anchored=" + brt.anchoredPosition + "; size=" + brt.sizeDelta + "; sprite=" + (buttonSprite != null ? buttonSprite.name : "none") + "; fontCopied=" + (txtPreview != null) + ".");
        }

        private static void EnsureConfig()
        {
            try
            {
                if (_config != null)
                    return;

                string configPath = Path.Combine(Paths.ConfigPath, ConfigFileName);
                _config = new ConfigFile(configPath, true);

                ButtonX = _config.Bind("Save Select Online Button", "ButtonX", 44f, "Native menuSaveSelect X position for the Switch to Online/Offline Mode button.");
                ButtonY = _config.Bind("Save Select Online Button", "ButtonY", -76f, "Native menuSaveSelect Y position for the Switch to Online/Offline Mode button.");
                ButtonWidth = _config.Bind("Save Select Online Button", "ButtonWidth", 110f, "Width of the Switch to Online/Offline Mode button.");
                ButtonHeight = _config.Bind("Save Select Online Button", "ButtonHeight", 18f, "Height of the Switch to Online/Offline Mode button.");
                ButtonFontSize = _config.Bind("Save Select Online Button", "ButtonFontSize", 5.5f, "Font size for the Switch to Online/Offline Mode button text.");

                DeleteButtonX = _config.Bind("Save Select Delete Button", "DeleteButtonX", -74f, "Native menuSaveSelect X position for the Delete/Cancel button centered under the save-slot grid.");
                DeleteButtonY = _config.Bind("Save Select Delete Button", "DeleteButtonY", -76f, "Native menuSaveSelect Y position for the Delete/Cancel button centered under the save-slot grid.");
                DeleteButtonWidth = _config.Bind("Save Select Delete Button", "DeleteButtonWidth", 72f, "Width of the Delete/Cancel button.");
                DeleteButtonHeight = _config.Bind("Save Select Delete Button", "DeleteButtonHeight", 18f, "Height of the Delete/Cancel button.");
                DeleteButtonFontSize = _config.Bind("Save Select Delete Button", "DeleteButtonFontSize", 5.5f, "Font size for the Delete/Cancel button text.");
                DeleteButtonCursorX = _config.Bind("Save Select Delete Button", "DeleteButtonCursorX", -42f, "Local X position for the native arrow cursor while it is parented to the Delete/Cancel button. More negative moves the cursor left; less negative moves it closer to the button.");
                DeleteButtonCursorY = _config.Bind("Save Select Delete Button", "DeleteButtonCursorY", 0f, "Local Y position for the native arrow cursor while it is parented to the Delete/Cancel button.");

                StatusX = _config.Bind("Save Select Server Status", "StatusX", -150f, "Native menuSaveSelect X position for the Server Status text.");
                StatusY = _config.Bind("Save Select Server Status", "StatusY", 82f, "Native menuSaveSelect Y position for the Server Status text.");
                StatusWidth = _config.Bind("Save Select Server Status", "StatusWidth", 120f, "Width of the Server Status text area.");
                StatusHeight = _config.Bind("Save Select Server Status", "StatusHeight", 18f, "Height of the Server Status text area.");
                StatusFontSize = _config.Bind("Save Select Server Status", "StatusFontSize", 4f, "Font size for the Server Status text.");

                ServerHost = _config.Bind("Official Server Network", "ServerHost", "mmo.gamingwithgoose.com", "Official Server social/save TCP host. This should match the Social server host.");
                SocialPort = _config.Bind("Official Server Network", "SocialPort", 61529, "Official Server social/save TCP port. This should match the Social server port.");
                NetworkTimeoutMs = _config.Bind("Official Server Network", "NetworkTimeoutMs", 5000, "Timeout in milliseconds for save-select online slot requests and server save uploads.");

                OnlineWallpaperTintEnabled = _config.Bind("Save Select Online Background", "OnlineWallpaperTintEnabled", true, "When true, the embedded Online Mode save-select wallpaper is swapped onto the native save-menu background while Online Mode is active and restored when Offline Mode is active.");
                OnlineWallpaperTintColor = _config.Bind("Save Select Online Background", "OnlineWallpaperTintColor", "#E886B8", "Legacy setting retained for compatibility. The embedded Online Mode wallpaper is now used instead of a tint swap.");
                OnlineWallpaperMinWidth = _config.Bind("Save Select Online Background", "OnlineWallpaperMinWidth", 180f, "Minimum RectTransform width for an Image to be considered the native save-select wallpaper/background target for Online Mode wallpaper swapping.");
                OnlineWallpaperMinHeight = _config.Bind("Save Select Online Background", "OnlineWallpaperMinHeight", 95f, "Minimum RectTransform height for an Image to be considered the native save-select wallpaper/background target for Online Mode wallpaper swapping.");

                Log("Official Server save-select config loaded from " + configPath + ".");
            }
            catch (Exception ex)
            {
                Log("Official Server save-select config load failed; using built-in defaults. " + ex.Message);
            }
        }

        private static float Cfg(ConfigEntry<float> entry, float fallback)
        {
            try { return entry != null ? entry.Value : fallback; } catch { return fallback; }
        }

        private static Sprite GetImageSprite(GameObject go)
        {
            if (go == null)
                return null;
            Image img = go.GetComponent<Image>();
            return img != null ? img.sprite : null;
        }

        private static void CopyRectBasis(RectTransform from, RectTransform to)
        {
            if (from == null || to == null)
                return;
            to.anchorMin = from.anchorMin;
            to.anchorMax = from.anchorMax;
            to.pivot = from.pivot;
            to.sizeDelta = from.sizeDelta;
            to.localRotation = from.localRotation;
            to.localScale = from.localScale;
        }

        private static void ForceVisibleRecursive(GameObject go)
        {
            if (go == null)
                return;
            go.SetActive(true);
            foreach (Transform child in go.GetComponentsInChildren<Transform>(true))
            {
                if (child != null)
                    child.gameObject.SetActive(true);
            }
            foreach (CanvasGroup cg in go.GetComponentsInChildren<CanvasGroup>(true))
            {
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
            foreach (Graphic g in go.GetComponentsInChildren<Graphic>(true))
            {
                Color c = g.color;
                if (c.a < 0.95f)
                    g.color = new Color(c.r, c.g, c.b, 1f);
                g.raycastTarget = g is Image;
            }
        }

        private static void LogNativeLayout(object menuObj, GameObject slot0, GameObject slot5, GameObject preview, Transform parent)
        {
            if (_layoutLogged)
                return;
            _layoutLogged = true;
            try
            {
                Log("Native layout anchor parent=" + (parent != null ? parent.name : "null") + "; parentType=" + (parent != null ? parent.GetType().Name : "null") + "; parentLocal=" + (parent != null ? parent.localPosition.ToString() : "null") + "; parentScale=" + (parent != null ? parent.localScale.ToString() : "null") + ".");
                LogObjectLayout("slot0/saveFileObj[0]", slot0);
                LogObjectLayout("slot5/saveFileObj[5]", slot5);
                LogObjectLayout("panelSaveDataPreview", preview);
                TMP_Text txtPreview = GetObjectField(menuObj, "txtPreview") as TMP_Text;
                if (txtPreview != null)
                    LogObjectLayout("txtPreview", txtPreview.gameObject);
            }
            catch (Exception ex)
            {
                Log("Native layout logging failed: " + ex.Message);
            }
        }

        private static void LogObjectLayout(string label, GameObject go)
        {
            if (go == null)
            {
                Log(label + "=null");
                return;
            }
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt != null)
                Log(label + " name=" + go.name + "; activeSelf=" + go.activeSelf + "; activeHierarchy=" + go.activeInHierarchy + "; parent=" + (go.transform.parent != null ? go.transform.parent.name : "null") + "; local=" + rt.localPosition + "; anchored=" + rt.anchoredPosition + "; size=" + rt.sizeDelta + "; rect=" + rt.rect + "; scale=" + rt.localScale + ".");
            else
                Log(label + " name=" + go.name + "; activeSelf=" + go.activeSelf + "; activeHierarchy=" + go.activeInHierarchy + "; parent=" + (go.transform.parent != null ? go.transform.parent.name : "null") + "; local=" + go.transform.localPosition + "; scale=" + go.transform.localScale + ".");
        }

        private static void UpdateVisualState()
        {
            ApplyConfiguredLayout();

            if (_onlineMode)
                ApplyOnlineSaveSlotVisuals();

            if (_statusText != null)
            {
                _statusText.text = !string.IsNullOrEmpty(_statusOverride) ? _statusOverride : (_onlineMode ? "Server Status: Connected" : "Server Status: Disconnected");
                bool warning = !string.IsNullOrEmpty(_statusOverride) && _statusOverride.IndexOf("Failed", StringComparison.OrdinalIgnoreCase) >= 0;
                _statusText.color = (_onlineMode && !warning) ? new Color(0.22f, 0.42f, 0.24f, 1f) : new Color(0.45f, 0.10f, 0.10f, 1f);
            }

            _deleteMode = IsNativeDeleteModeActive(_menuObj);
            if (_modeButtonText != null)
                _modeButtonText.text = _onlineMode ? "Switch to Offline Mode" : (_authBusy ? "Connecting..." : "Switch to Online Mode");

            bool showDeleteButton = _onlineMode && !_authBusy;
            if (_deleteButton != null && _deleteButton.activeSelf != showDeleteButton)
                _deleteButton.SetActive(showDeleteButton);
            if (!showDeleteButton && _deleteMode)
                SetDeleteMode(false, "left Online Mode");
            if (_deleteButtonText != null)
                _deleteButtonText.text = _deleteMode ? "Cancel" : "Delete";

            ApplyModeButtonNavigation();
            ApplyDeleteButtonNavigation();
            if (showDeleteButton && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == _deleteButton)
                PositionDeleteButtonCursor();
            ApplyOnlineWallpaperTheme();
        }

        private static void ApplyOnlineWallpaperTheme()
        {
            try
            {
                EnsureConfig();
                bool enabled = OnlineWallpaperTintEnabled == null || OnlineWallpaperTintEnabled.Value;
                if (!_onlineMode || !enabled)
                {
                    RestoreOnlineWallpaperColors();
                    return;
                }

                Image target = FindBestSaveSelectBackgroundImage();
                if (target == null)
                    return;

                if (_onlineWallpaperTargetImage != target)
                {
                    RestoreOnlineWallpaperColors();
                    _onlineWallpaperTargetImage = target;
                    _originalSaveSelectBackgroundSprite = target.sprite;
                    _originalSaveSelectBackgroundColor = target.color;
                    _onlineWallpaperTargetPath = GetTransformPath(target.transform);
                }

                Sprite onlineSprite = GetEmbeddedOnlineWallpaperSprite();
                if (onlineSprite == null)
                    return;

                if (_onlineWallpaperTargetImage.sprite != onlineSprite)
                    _onlineWallpaperTargetImage.sprite = onlineSprite;

                float alpha = _originalSaveSelectBackgroundColor.a > 0f ? _originalSaveSelectBackgroundColor.a : 1f;
                _onlineWallpaperTargetImage.color = new Color(1f, 1f, 1f, alpha);

                if (!_onlineWallpaperSwapLogged)
                {
                    _onlineWallpaperSwapLogged = true;
                    Log("Online Mode save-select wallpaper swap applied to native Image path='" + (_onlineWallpaperTargetPath ?? "unknown") + "' using embedded resource OnlinemenuBG.png. Offline Mode restores the original native sprite.");
                }
            }
            catch (Exception ex)
            {
                Log("ApplyOnlineWallpaperTheme failed: " + ex.Message);
            }
        }

        private static void RestoreOnlineWallpaperColors()
        {
            try
            {
                if (_onlineWallpaperTargetImage != null)
                {
                    _onlineWallpaperTargetImage.sprite = _originalSaveSelectBackgroundSprite;
                    _onlineWallpaperTargetImage.color = _originalSaveSelectBackgroundColor;
                }
            }
            catch { }
            finally
            {
                _onlineWallpaperTargetImage = null;
                _originalSaveSelectBackgroundSprite = null;
                _originalSaveSelectBackgroundColor = Color.white;
                _onlineWallpaperTargetPath = null;
                _onlineWallpaperSwapLogged = false;
            }
        }

        private static Image FindBestSaveSelectBackgroundImage()
        {
            Image best = null;
            float bestScore = float.MinValue;
            foreach (Image img in EnumerateSaveSelectBackgroundImages())
            {
                if (img == null)
                    continue;
                float score = ScoreSaveSelectBackgroundImage(img);
                if (best == null || score > bestScore)
                {
                    best = img;
                    bestScore = score;
                }
            }
            return best;
        }

        private static float ScoreSaveSelectBackgroundImage(Image img)
        {
            if (img == null)
                return float.MinValue;

            string path = GetTransformPath(img.transform).ToLowerInvariant();
            string spriteName = img.sprite != null ? (img.sprite.name ?? string.Empty).ToLowerInvariant() : string.Empty;

            float score = 0f;
            if (spriteName.Contains("menubg")) score += 1000f;
            if (spriteName.Contains("menu")) score += 250f;
            if (spriteName.Contains("bg")) score += 150f;
            if (path.Contains("background")) score += 400f;
            if (path.Contains("wallpaper")) score += 350f;
            if (path.Contains("back")) score += 150f;
            if (path.Contains("menu")) score += 75f;
            if (img.gameObject.activeInHierarchy) score += 10f;

            RectTransform rt = img.GetComponent<RectTransform>();
            if (rt != null)
                score += Mathf.Abs(rt.rect.width * rt.rect.height) / 100f;

            return score;
        }

        private static bool LoadPngIntoTexture(Texture2D texture, byte[] bytes)
        {
            try
            {
                if (texture == null || bytes == null || bytes.Length == 0)
                    return false;

                Type imageConversionType = null;
                try
                {
                    imageConversionType = Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule");
                }
                catch { imageConversionType = null; }

                if (imageConversionType == null)
                {
                    foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            imageConversionType = asm.GetType("UnityEngine.ImageConversion", false);
                            if (imageConversionType != null)
                                break;
                        }
                        catch { }
                    }
                }

                if (imageConversionType == null)
                {
                    Log("UnityEngine.ImageConversion was not available, so the embedded Online Mode wallpaper PNG could not be decoded.");
                    return false;
                }

                MethodInfo loadImage = imageConversionType.GetMethod("LoadImage", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(Texture2D), typeof(byte[]) }, null);
                if (loadImage != null)
                    return (bool)loadImage.Invoke(null, new object[] { texture, bytes });

                loadImage = imageConversionType.GetMethod("LoadImage", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(Texture2D), typeof(byte[]), typeof(bool) }, null);
                if (loadImage != null)
                    return (bool)loadImage.Invoke(null, new object[] { texture, bytes, false });

                Log("UnityEngine.ImageConversion.LoadImage overload was not found, so the embedded Online Mode wallpaper PNG could not be decoded.");
                return false;
            }
            catch (Exception ex)
            {
                Log("LoadPngIntoTexture failed: " + ex.Message);
                return false;
            }
        }

        private static Sprite GetEmbeddedOnlineWallpaperSprite()
        {
            try
            {
                if (_embeddedOnlineWallpaperSprite != null)
                    return _embeddedOnlineWallpaperSprite;

                Assembly asm = typeof(OfficialServerSaveSelectNativeRuntime).Assembly;
                string resourceName = asm.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("OnlinemenuBG.png", StringComparison.OrdinalIgnoreCase));
                if (string.IsNullOrEmpty(resourceName))
                {
                    Log("Embedded Online Mode wallpaper resource OnlinemenuBG.png was not found in the patcher assembly.");
                    return null;
                }

                using (Stream stream = asm.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        Log("Embedded Online Mode wallpaper resource stream could not be opened: " + resourceName);
                        return null;
                    }

                    byte[] bytes = new byte[stream.Length];
                    int read = stream.Read(bytes, 0, bytes.Length);
                    if (read <= 0)
                    {
                        Log("Embedded Online Mode wallpaper resource was empty: " + resourceName);
                        return null;
                    }

                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    tex.name = "MMOnsterpatchOfficialServerOnlineMenuBG";
                    tex.filterMode = FilterMode.Point;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    if (!LoadPngIntoTexture(tex, bytes))
                    {
                        UnityEngine.Object.Destroy(tex);
                        Log("Embedded Online Mode wallpaper PNG failed to load from assembly resource: " + resourceName);
                        return null;
                    }

                    _embeddedOnlineWallpaperTexture = tex;
                    _embeddedOnlineWallpaperSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    _embeddedOnlineWallpaperSprite.name = "MMOnsterpatchOfficialServerOnlineMenuBG";
                    Log("Loaded embedded Online Mode save-select wallpaper from assembly resource " + resourceName + " (" + tex.width + "x" + tex.height + ").");
                    return _embeddedOnlineWallpaperSprite;
                }
            }
            catch (Exception ex)
            {
                Log("GetEmbeddedOnlineWallpaperSprite failed: " + ex.Message);
                return null;
            }
        }

        private static IEnumerable<Image> EnumerateSaveSelectBackgroundImages()
        {
            HashSet<Image> yielded = new HashSet<Image>();
            foreach (GameObject root in EnumerateSaveSelectRoots())
            {
                if (root == null)
                    continue;
                foreach (Image img in root.GetComponentsInChildren<Image>(true))
                {
                    if (img == null || yielded.Contains(img))
                        continue;
                    if (!IsSaveSelectWallpaperCandidate(img))
                        continue;
                    yielded.Add(img);
                    yield return img;
                }
            }
        }

        private static IEnumerable<GameObject> EnumerateSaveSelectRoots()
        {
            GameObject menuSaveSelect = GetGameObjectField(_menuObj, "menuSaveSelect");
            GameObject menuSaveFiles = GetGameObjectField(_menuObj, "menuSaveFiles");
            if (menuSaveSelect != null) yield return menuSaveSelect;
            if (menuSaveFiles != null && menuSaveFiles != menuSaveSelect) yield return menuSaveFiles;
        }

        private static bool IsSaveSelectWallpaperCandidate(Image img)
        {
            if (img == null || img.gameObject == null)
                return false;
            if (_root != null && img.transform.IsChildOf(_root.transform))
                return false;
            string path = GetTransformPath(img.transform).ToLowerInvariant();
            if (path.Contains("savefile") || path.Contains("saveplus") || path.Contains("playerpreview") || path.Contains("preview") || path.Contains("button") || path.Contains("version") || path.Contains("arrow") || path.Contains("text"))
                return false;
            if (img.GetComponent<Button>() != null || img.GetComponent<Selectable>() != null)
                return false;
            RectTransform rt = img.GetComponent<RectTransform>();
            if (rt == null)
                return false;
            float w = Mathf.Abs(rt.rect.width);
            float h = Mathf.Abs(rt.rect.height);
            bool bigEnough = w >= Cfg(OnlineWallpaperMinWidth, 180f) && h >= Cfg(OnlineWallpaperMinHeight, 95f);
            bool namedLikeBackground = path.Contains("bg") || path.Contains("back") || path.Contains("wall") || path.Contains("paper") || path.Contains("menu") || path.Contains("background");
            bool greenTinted = img.color.g > img.color.r + 0.05f && img.color.g > img.color.b + 0.05f;
            bool hasSprite = img.sprite != null;
            return bigEnough && (namedLikeBackground || greenTinted || hasSprite);
        }

        private static string GetTransformPath(Transform t)
        {
            try
            {
                if (t == null)
                    return string.Empty;
                List<string> parts = new List<string>();
                Transform cur = t;
                while (cur != null)
                {
                    parts.Add(cur.name ?? string.Empty);
                    cur = cur.parent;
                }
                parts.Reverse();
                return string.Join("/", parts.ToArray());
            }
            catch { return string.Empty; }
        }

        private static void ApplyConfiguredLayout()
        {
            EnsureConfig();

            try
            {
                if (_statusText != null)
                {
                    RectTransform rt = _statusText.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.sizeDelta = new Vector2(Cfg(StatusWidth, 120f), Cfg(StatusHeight, 18f));
                        rt.anchoredPosition = new Vector2(Cfg(StatusX, -150f), Cfg(StatusY, 82f));
                    }
                    _statusText.fontSize = Cfg(StatusFontSize, 4f);
                }

                if (_modeButton != null)
                {
                    RectTransform brt = _modeButton.GetComponent<RectTransform>();
                    if (brt != null)
                    {
                        brt.sizeDelta = new Vector2(Cfg(ButtonWidth, 110f), Cfg(ButtonHeight, 18f));
                        brt.anchoredPosition = new Vector2(Cfg(ButtonX, 44f), Cfg(ButtonY, -76f));
                    }
                }

                if (_modeButtonText != null)
                    _modeButtonText.fontSize = Cfg(ButtonFontSize, 5.5f);

                if (_deleteButton != null)
                {
                    RectTransform drt = _deleteButton.GetComponent<RectTransform>();
                    if (drt != null)
                    {
                        drt.sizeDelta = new Vector2(Cfg(DeleteButtonWidth, 72f), Cfg(DeleteButtonHeight, 18f));
                        drt.anchoredPosition = new Vector2(Cfg(DeleteButtonX, -74f), Cfg(DeleteButtonY, -76f));
                    }
                }

                if (_deleteButtonText != null)
                    _deleteButtonText.fontSize = Cfg(DeleteButtonFontSize, 5.5f);
            }
            catch (Exception ex)
            {
                Log("ApplyConfiguredLayout failed: " + ex.Message);
            }
        }

        public static bool TryHandleSelectSaveFile(object menuObj, int slot)
        {
            try
            {
                _menuObj = menuObj;
                int safeSlot = Mathf.Clamp(slot, 0, 5);
                if (!_onlineMode)
                    return false;

                if (_authBusy)
                {
                    Log("Online save slot " + slot + " ignored because authentication is still in progress.");
                    return true;
                }

                if (IsNativeDeleteModeActive(menuObj))
                {
                    SetObjectField(menuObj, "curSaveSlot", safeSlot);
                    SetObjectField(menuObj, "lastButtonObj", GetGameObjectFromArrayField(menuObj, "saveFileObj", safeSlot));
                    ApplyOnlineSaveSlotVisuals();
                    UpdateVisualState();
                    Log("Online save slot " + safeSlot + " selected while Delete Save mode is active. Native confirmation dialogue is allowed; the confirmed delete is redirected to the Official Server.");
                    return false;
                }

                // Empty online slots intentionally use the game's native character-creation flow.
                // The moment an online slot is selected, arm a gameplay-session guard that survives the
                // title-to-game scene transition. This is critical for new-character creation, because the
                // first GameScript.SaveGame can happen after MenuScript is gone; the guard still redirects
                // SaveSystem.SaveGame to the Official Server and blocks local disk writes.
                SetObjectField(menuObj, "curSaveSlot", safeSlot);
                SetObjectField(menuObj, "lastButtonObj", GetGameObjectFromArrayField(menuObj, "saveFileObj", safeSlot));
                object existing = GetSaveDataForSlot(safeSlot);
                BeginOfficialOnlineSaveSession(safeSlot, existing == null ? "creating online save slot" : "loading online save slot");
                _statusOverride = existing == null ? "Server Status: Creating Online Slot" : null;
                ApplyOnlineSaveSlotVisuals();
                UpdateVisualState();
                Log((existing == null ? "Online empty save slot " : "Online occupied save slot ") + safeSlot + " selected. Native save flow is allowed; SaveSystem writes are redirected to the server while Official Online Save Session guard is active.");
                return false;
            }
            catch (Exception ex)
            {
                Log("TryHandleSelectSaveFile failed: " + ex.Message);
                return true;
            }
        }

        public static bool TryHandleOnlineDeleteSaveAndBlockLocal(int slot)
        {
            try
            {
                if (!IsOfficialOnlineSaveProtectionActive())
                    return false;

                int safeSlot = (_officialOnlineSaveSessionActive && _officialOnlineSaveSessionSlot >= 0) ? Mathf.Clamp(_officialOnlineSaveSessionSlot, 0, 5) : Mathf.Clamp(slot, 0, 5);
                bool deleted = DeleteOnlineSaveSlotOnServer(safeSlot);
                if (deleted)
                {
                    SetSaveDataForSlot(safeSlot, null);
                    _onlineSaveSlotCache[safeSlot] = null;
                    _statusOverride = "Server Status: Save Deleted";
                    Log("Official MenuScript delete confirmed for Online Mode slot" + safeSlot + ". SaveSystem.DeleteSave was redirected to the server and the local disk delete was blocked.");
                }
                else
                {
                    _statusOverride = "Server Status: Delete Failed";
                    Log("Official MenuScript delete for Online Mode slot" + safeSlot + " failed. Local disk delete was blocked.");
                }

                // Let MenuScript.ActuallyDeleteSaveFile continue after this blocked disk call. The official
                // method will clear MenuScript.saveData[curSaveSlot], refresh the slot grid, hide preview,
                // play the click SFX, and restore menu state exactly like vanilla.
                return true;
            }
            catch (Exception ex)
            {
                Log("TryHandleOnlineDeleteSaveAndBlockLocal failed: " + ex.Message + ". Blocking local disk delete while Online Mode is active.");
                _statusOverride = "Server Status: Delete Failed";
                return true;
            }
        }

        public static void OnActuallyDeleteSaveFileCompleted(object menuObj)
        {
            // Intentionally unused. MenuScript.ActuallyDeleteSaveFile is no longer patched.
            // Online deletion is redirected at SaveSystem.DeleteSave so the official MenuScript
            // delete confirmation/cleanup flow remains untouched.
        }

        private static int GetCurrentSaveSlot(object menuObj)
        {
            try
            {
                object value = GetObjectField(menuObj, "curSaveSlot");
                if (value is int i)
                    return Mathf.Clamp(i, 0, 5);
            }
            catch { }
            return Mathf.Clamp(_lastHoverSlot, 0, 5);
        }

        private static void BeginOfficialOnlineSaveSession(int slot, string reason)
        {
            try
            {
                int safeSlot = Mathf.Clamp(slot, 0, 5);
                _officialOnlineSaveSessionActive = true;
                _officialOnlineSaveSessionSlot = safeSlot;
                _onlineMode = true;
                Log("Official Online Save Session armed for slot" + safeSlot + " (" + reason + "). Local SaveSystem writes are now server-redirected until return-to-title/disconnect.");
            }
            catch { }
        }

        private static void ClearOfficialOnlineSaveSession(string reason)
        {
            try
            {
                if (_officialOnlineSaveSessionActive || _officialOnlineSaveSessionSlot >= 0)
                    Log("Official Online Save Session cleared from " + reason + ".");
            }
            catch { }
            _officialOnlineSaveSessionActive = false;
            _officialOnlineSaveSessionSlot = -1;
        }

        private static bool IsOfficialOnlineSaveProtectionActive()
        {
            try { return _onlineMode || _officialOnlineSaveSessionActive; } catch { return false; }
        }


        private static bool DeleteOnlineSaveSlotOnServer(int slot)
        {
            try
            {
                string token = GTSRuntimeHost.GetAioSessionTokenForSocial();
                if (string.IsNullOrEmpty(token))
                {
                    Log("Official Server save delete failed for slot" + slot + ": Steam session token is empty.");
                    return false;
                }

                string line = SendOfficialServerRequest("OFFICIAL_SAVE_DELETE|" + B64(token) + "|" + Mathf.Clamp(slot, 0, 5).ToString(), "OFFICIAL_SAVE_DELETE_OK", "OFFICIAL_SAVE_ERROR");
                if (!string.IsNullOrEmpty(line) && line.StartsWith("OFFICIAL_SAVE_DELETE_OK|", StringComparison.OrdinalIgnoreCase))
                    return true;

                Log("Official Server save delete failed for slot" + slot + ": " + DecodeServerError(line));
                return false;
            }
            catch (Exception ex)
            {
                Log("DeleteOnlineSaveSlotOnServer failed: " + ex.Message);
                return false;
            }
        }

        public static bool ShouldHandleOnlineSaveAndBlockLocalWrite(object saveDataObj, int slot)
        {
            try
            {
                if (!IsOfficialOnlineSaveProtectionActive())
                    return false;

                int safeSlot = (_officialOnlineSaveSessionActive && _officialOnlineSaveSessionSlot >= 0) ? Mathf.Clamp(_officialOnlineSaveSessionSlot, 0, 5) : Mathf.Clamp(slot, 0, 5);
                SanitizeSaveDataForMenuPreview(saveDataObj, "server upload slot" + safeSlot);
                bool uploaded = UploadOnlineSaveSlotToServer(saveDataObj, safeSlot);
                if (uploaded)
                {
                    SetSaveDataForSlot(safeSlot, saveDataObj);
                    _statusOverride = null;
                    _lastOfficialSaveUploadAt = Time.unscaledTime;
                    Log("Redirected SaveSystem.SaveGame(slot" + safeSlot + ") to Official Server and blocked local disk write.");
                }
                else
                {
                    _statusOverride = "Server Status: Save Failed";
                    Log("Blocked local SaveSystem.SaveGame(slot" + safeSlot + ") but server upload failed. Local save file was not touched.");
                }

                if (!_localSaveBlockLogged)
                {
                    _localSaveBlockLogged = true;
                    Log("Official Server Online Mode is redirecting SaveSystem.SaveGame to the server. Local disk writes are blocked while online.");
                }
                return true;
            }
            catch (Exception ex)
            {
                Log("Online save redirect failed: " + ex.Message + ". Blocking local write to protect local saves.");
                return true;
            }
        }

        public static bool TryBlockOnlineHoverSaveFile(object menuObj, int slot)
        {
            try
            {
                _menuObj = menuObj;
                if (!_onlineMode)
                    return false;

                _lastHoverSlot = slot;
                if (_lastOfficialSaveUploadAt <= 0f || Time.unscaledTime - _lastOfficialSaveUploadAt > 2.0f)
                    _statusOverride = null;
                ApplyOnlineSaveSlotVisuals();
                UpdateVisualState();
                if (!_onlineHoverBlockLogged)
                {
                    _onlineHoverBlockLogged = true;
                    Log("Online save-select hover is using server-loaded MenuScript.saveData, so vanilla preview movement stays native without local slot flashes.");
                }
                return false;
            }
            catch (Exception ex)
            {
                Log("TryBlockOnlineHoverSaveFile failed: " + ex.Message);
                return true;
            }
        }

        public static void OnMenuCancelInvoked(object menuObj)
        {
            try
            {
                _menuObj = menuObj;
                if (_onlineMode || _authBusy || IsAnyServerSessionActive())
                    DisconnectServerSession("MenuScript.Cancel/title navigation");
            }
            catch (Exception ex)
            {
                Log("OnMenuCancelInvoked failed: " + ex.Message);
            }
        }

        public static void OnReturnToTitleInvoked()
        {
            try
            {
                if (_returnToTitleFromOfficialDisconnect)
                {
                    HideOfficialChatWindow();
                    return;
                }
                if (_onlineMode || _officialOnlineSaveSessionActive || _authBusy || IsAnyServerSessionActive())
                    ForceSaveAndDisconnectOnly("GameScript.ReturnToTitle");
            }
            catch (Exception ex)
            {
                Log("OnReturnToTitleInvoked failed: " + ex.Message);
            }
        }

        private static bool IsAnyServerSessionActive()
        {
            try { if (GTSRuntimeHost.IsLoggedInForAio() || GTSRuntimeHost.IsAuthBusyForAio()) return true; } catch { }
            try { if (global::MMOnsterpatchAIOBootstrap.IsMMOBusy() || global::MMOnsterpatchAIOBootstrap.IsMMOConnected()) return true; } catch { }
            return false;
        }

        private static void DisconnectServerSession(string reason)
        {
            try
            {
                Log("Disconnecting Official Server session from " + reason + ".");
                _authBusy = false;
                _onlineMode = false;
                _statusOverride = null;
                _onlineHoverBlockLogged = false;
                ClearOfficialOnlineSaveSession(reason);

                try { global::MMOnsterpatchAIOBootstrap.DisconnectMMO(); } catch (Exception ex) { Log("MMO disconnect failed: " + ex.Message); }
                // Keep the Steam/AIO session token in memory for this game process so switching
                // Online/Offline during testing can restore with SESSION_LOGIN instead of opening Steam auth again.
                // GTSRuntimeHost.OnApplicationQuit still revokes/clears this memory-only token when the game closes.
                try { GTSRuntimeHost.DisconnectNetworkOnlyForAio(); } catch (Exception ex) { Log("Steam/GTS network disconnect failed: " + ex.Message); }

                UpdateVisualState();
            }
            catch (Exception ex)
            {
                Log("DisconnectServerSession failed: " + ex.Message);
            }
        }

        private static void ApplyOnlineSaveSlotVisuals()
        {
            try
            {
                object menuObj = _menuObj;
                if (menuObj == null || !_onlineMode)
                    return;

                // Keep the vanilla save-slot controls intact. The only data source we replace is
                // MenuScript.saveData, which is loaded from the server while Online Mode is active.
                // Official MenuScript.DeleteASaveFile calls Play2(), which reloads local saveData from disk;
                // re-apply the server cache so Online Mode never displays/deletes local saves.
                bool cacheChanged = ApplyOnlineSaveSlotCacheToMenuData();
                if (cacheChanged && !_refreshingOnlineSlotsFromCache)
                {
                    try
                    {
                        _refreshingOnlineSlotsFromCache = true;
                        InvokeMenuMethod(menuObj, "RefreshSaveFiles");
                    }
                    finally
                    {
                        _refreshingOnlineSlotsFromCache = false;
                    }
                }

                for (int i = 0; i < 6; i++)
                {
                    object save = GetSaveDataForSlot(i);
                    GameObject plus = GetGameObjectFromArrayField(menuObj, "savePlusObj", i);
                    GameObject preview = GetGameObjectFromArrayField(menuObj, "savePlayerPreviewObj", i);
                    GameObject secondary = GetGameObjectFromArrayField(menuObj, "savePlayerPreviewSecondaryColor", i);
                    if (save == null)
                    {
                        if (plus != null) plus.SetActive(true);
                        if (preview != null) preview.SetActive(false);
                    }
                    else
                    {
                        if (plus != null) plus.SetActive(false);
                        if (preview != null) preview.SetActive(true);
                        if (secondary != null) secondary.SetActive(true);
                    }
                }
            }
            catch (Exception ex)
            {
                Log("ApplyOnlineSaveSlotVisuals failed: " + ex.Message);
            }
        }

        private static bool ApplyOnlineSaveSlotCacheToMenuData()
        {
            try
            {
                if (!_onlineMode)
                    return false;

                Type menuType = FindGameType("MenuScript");
                FieldInfo f = menuType != null ? menuType.GetField("saveData", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) : null;
                Array arr = f != null ? f.GetValue(null) as Array : null;
                if (arr == null)
                    return false;

                bool changed = false;
                int count = Math.Min(6, arr.Length);
                for (int i = 0; i < count; i++)
                {
                    object cached = _onlineSaveSlotCache[i];
                    object current = arr.GetValue(i);
                    if (!object.ReferenceEquals(current, cached))
                    {
                        arr.SetValue(cached, i);
                        changed = true;
                    }
                }
                return changed;
            }
            catch
            {
                return false;
            }
        }

        private static void ClearOnlineSaveSlotCache()
        {
            try
            {
                for (int i = 0; i < _onlineSaveSlotCache.Length; i++)
                    _onlineSaveSlotCache[i] = null;
            }
            catch { }
        }

        private static void RequestSelectFirstSaveSlot()
        {
            _pendingSelectFirstSlotAfter = Time.unscaledTime + 0.08f;
            _pendingSelectFirstSlotUntil = Time.unscaledTime + 1.00f;
            _pendingSelectFirstSlotLoggedDeferral = false;
        }

        private static void HandlePendingLocalDeleteCompletion()
        {
            try
            {
                if (_pendingLocalDeleteSlot < 0)
                    return;

                if (_onlineMode || _menuObj == null || !IsNativeDeleteModeActive(_menuObj))
                {
                    _pendingLocalDeleteSlot = -1;
                    _pendingLocalDeleteCheckUntil = 0f;
                    return;
                }

                if (Time.unscaledTime > _pendingLocalDeleteCheckUntil)
                {
                    _pendingLocalDeleteSlot = -1;
                    _pendingLocalDeleteCheckUntil = 0f;
                    return;
                }

                int slot = Mathf.Clamp(_pendingLocalDeleteSlot, 0, 5);
                if (GetSaveDataForSlot(slot) == null)
                {
                    SetDeleteMode(false, "confirmed local delete");
                    Log("Delete Save mode auto-canceled after confirmed local save deletion for slot" + slot + ".");
                }
            }
            catch (Exception ex)
            {
                Log("HandlePendingLocalDeleteCompletion failed: " + ex.Message);
                _pendingLocalDeleteSlot = -1;
                _pendingLocalDeleteCheckUntil = 0f;
            }
        }

        private static void HandlePendingSaveSlotSelection()
        {
            try
            {
                if (_pendingSelectFirstSlotUntil <= 0f)
                    return;
                if (Time.unscaledTime < _pendingSelectFirstSlotAfter)
                    return;
                if (Time.unscaledTime > _pendingSelectFirstSlotUntil)
                {
                    _pendingSelectFirstSlotUntil = 0f;
                    return;
                }
                GameObject first = GetBestSaveSlotSelectable(_menuObj, 0);
                EventSystem es = GetMenuEventSystem(_menuObj);
                if (es != null && first != null)
                {
                    if (IsEventSystemAlreadySelecting(es))
                    {
                        if (!_pendingSelectFirstSlotLoggedDeferral)
                        {
                            _pendingSelectFirstSlotLoggedDeferral = true;
                            Log("Save slot focus handoff deferred because Unity EventSystem is already inside a selection event.");
                        }
                        _pendingSelectFirstSlotAfter = Time.unscaledTime + 0.05f;
                        return;
                    }
                    if (es.currentSelectedGameObject == first)
                    {
                        SetObjectField(_menuObj, "lastButtonObj", first);
                        _pendingSelectFirstSlotUntil = 0f;
                        return;
                    }
                    es.SetSelectedGameObject(first);
                    SetObjectField(_menuObj, "lastButtonObj", first);
                    _pendingSelectFirstSlotUntil = 0f;
                    _pendingSelectFirstSlotLoggedDeferral = false;
                    Log("Selected first save slot after mode switch so controller/keyboard focus leaves the Online/Offline button.");
                }
            }
            catch { }
        }


        private static bool IsEventSystemAlreadySelecting(EventSystem es)
        {
            try
            {
                if (es == null)
                    return false;
                PropertyInfo prop = typeof(EventSystem).GetProperty("alreadySelecting", BindingFlags.Public | BindingFlags.Instance);
                if (prop != null)
                {
                    object value = prop.GetValue(es, null);
                    if (value is bool b)
                        return b;
                }
            }
            catch { }
            return false;
        }

        private static EventSystem GetMenuEventSystem(object menuObj)
        {
            try
            {
                object esObj = GetObjectField(menuObj, "eventSystem");
                if (esObj is EventSystem es)
                    return es;
            }
            catch { }
            try { return EventSystem.current; } catch { return null; }
        }

        private static GameObject GetBestSaveSlotSelectable(object menuObj, int slot)
        {
            GameObject go = GetGameObjectFromArrayField(menuObj, "saveFileObj", slot);
            if (go != null && go.GetComponent<Selectable>() != null)
                return go;
            go = GetGameObjectFromArrayField(menuObj, "bSaveFile", slot);
            if (go != null && go.GetComponent<Selectable>() != null)
                return go;
            go = GetGameObjectFromArrayField(menuObj, "saveFileObj", slot);
            if (go != null)
                return go;
            return GetGameObjectFromArrayField(menuObj, "bSaveFile", slot);
        }

        private static Selectable GetSaveSlotSelectable(object menuObj, int slot)
        {
            GameObject go = GetBestSaveSlotSelectable(menuObj, slot);
            return go != null ? go.GetComponent<Selectable>() : null;
        }

        private static void ApplyModeButtonNavigation()
        {
            try
            {
                if (_modeButton == null || _menuObj == null)
                    return;
                Button btn = _modeButton.GetComponent<Button>();
                if (btn == null)
                    return;
                Selectable slot0 = GetSaveSlotSelectable(_menuObj, 0);
                Selectable slot4 = GetSaveSlotSelectable(_menuObj, 4);
                Selectable slot5 = GetSaveSlotSelectable(_menuObj, 5);
                Selectable target = slot5 ?? slot4 ?? slot0;
                Navigation nav = btn.navigation;
                nav.mode = Navigation.Mode.Explicit;
                nav.selectOnUp = target;
                nav.selectOnDown = target;
                nav.selectOnLeft = target;
                nav.selectOnRight = target;
                btn.navigation = nav;
            }
            catch { }
        }


        private static void PositionDeleteButtonCursor()
        {
            try
            {
                if (_menuObj == null || _deleteButton == null)
                    return;

                GameObject arrow = GetGameObjectField(_menuObj, "versionArrowObj");
                if (arrow == null || arrow.transform == null)
                    return;

                if (arrow.transform.parent != _deleteButton.transform)
                    return;

                arrow.transform.localPosition = new Vector3(Cfg(DeleteButtonCursorX, -42f), Cfg(DeleteButtonCursorY, 0f), 0f);
            }
            catch { }
        }

        private static void ApplyDeleteButtonNavigation()
        {
            try
            {
                if (_deleteButton == null || _menuObj == null)
                    return;
                Button btn = _deleteButton.GetComponent<Button>();
                if (btn == null)
                    return;
                Selectable slot0 = GetSaveSlotSelectable(_menuObj, 0);
                Selectable slot4 = GetSaveSlotSelectable(_menuObj, 4);
                Selectable slot5 = GetSaveSlotSelectable(_menuObj, 5);
                Selectable mode = _modeButton != null ? _modeButton.GetComponent<Selectable>() : null;
                Selectable target = slot5 ?? slot4 ?? slot0;
                Navigation nav = btn.navigation;
                nav.mode = Navigation.Mode.Explicit;
                nav.selectOnUp = target;
                nav.selectOnDown = target;
                nav.selectOnLeft = target;
                nav.selectOnRight = mode ?? target;
                btn.navigation = nav;
            }
            catch { }
        }


        public static bool IsOfficialOnlineModeActive()
        {
            try { return _onlineMode || _authBusy || _officialOnlineSaveSessionActive; } catch { return false; }
        }

        public static void ForceSaveDisconnectAndReturnToTitleFromChat()
        {
            try
            {
                ForceSaveAndDisconnectOnly("chat Disconnect button");
                _returnToTitleFromOfficialDisconnect = true;
                try
                {
                    Type gsType = FindGameType("GameScript");
                    UnityEngine.Object gsObj = gsType != null ? UnityEngine.Object.FindObjectOfType(gsType) : null;
                    MethodInfo rt = gsType != null ? gsType.GetMethod("ReturnToTitle", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static) : null;
                    if (rt != null)
                    {
                        rt.Invoke(rt.IsStatic ? null : gsObj, null);
                        Log("Chat Disconnect force-saved, disconnected, and invoked GameScript.ReturnToTitle.");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Log("GameScript.ReturnToTitle invoke failed after chat disconnect: " + ex.Message);
                }
                try { UnityEngine.SceneManagement.SceneManager.LoadScene(0); } catch { }
            }
            finally
            {
                _returnToTitleFromOfficialDisconnect = false;
            }
        }

        private static void ForceSaveAndDisconnectOnly(string reason)
        {
            try
            {
                if (_onlineMode || _officialOnlineSaveSessionActive)
                    ForceCurrentGameSaveToOfficialServer(reason);
                DisconnectServerSession(reason);
                HideOfficialChatWindow();
            }
            catch (Exception ex)
            {
                Log("ForceSaveAndDisconnectOnly failed: " + ex.Message);
            }
        }

        private static void ForceCurrentGameSaveToOfficialServer(string reason)
        {
            try
            {
                Type gsType = FindGameType("GameScript");
                UnityEngine.Object gsObj = gsType != null ? UnityEngine.Object.FindObjectOfType(gsType) : null;
                MethodInfo save = gsType != null ? gsType.GetMethod("SaveGame", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static) : null;
                if (save == null || (!save.IsStatic && gsObj == null))
                {
                    Log("No active GameScript.SaveGame found for Official Server force-save from " + reason + ".");
                    return;
                }
                save.Invoke(save.IsStatic ? null : gsObj, null);
                Log("Requested GameScript.SaveGame for Official Server before disconnect from " + reason + ".");
            }
            catch (Exception ex)
            {
                Log("Official Server force-save failed from " + reason + ": " + ex.Message);
            }
        }

        private static void HideOfficialChatWindow()
        {
            try
            {
                Goose.Monsterpatch.SocialPatcher.SocialNativePatcher.SocialRuntimeHost.HideChatForOfficialServerTitle();
            }
            catch { }
        }

        private static bool FetchOnlineSaveSlotsFromServer()
        {
            // Use the v2 line protocol first. It avoids a nested JSON response with a huge escaped save_json
            // string and is much easier to diagnose from logs. The old JSON payload remains as fallback for
            // compatibility with older server files.
            bool v2Ok = FetchOnlineSaveSlotsFromServerV2();
            if (v2Ok)
                return true;
            return FetchOnlineSaveSlotsFromServerLegacyJson();
        }

        private static bool FetchOnlineSaveSlotsFromServerV2()
        {
            try
            {
                string token = GTSRuntimeHost.GetAioSessionTokenForSocial();
                if (string.IsNullOrEmpty(token))
                {
                    Log("Cannot load Official Server save slots: Steam session token is empty.");
                    SetAllMenuSaveDataNull();
                    return false;
                }

                List<string> lines = SendOfficialServerRequestLines("OFFICIAL_SAVE_SLOTS2_REQ|" + B64(token), "OFFICIAL_SAVE_SLOTS_DONE", "OFFICIAL_SAVE_ERROR");
                if (lines == null || lines.Count == 0)
                {
                    Log("Official Server v2 save slot request returned no lines.");
                    SetAllMenuSaveDataNull();
                    return false;
                }

                string error = lines.FirstOrDefault(x => x != null && x.StartsWith("OFFICIAL_SAVE_ERROR|", StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(error))
                {
                    Log("Official Server v2 save slot request failed: " + DecodeServerError(error));
                    SetAllMenuSaveDataNull();
                    return false;
                }

                Type saveType = FindGameType("SaveData");
                if (saveType == null)
                {
                    Log("Official Server v2 save slot request failed: SaveData type not found.");
                    SetAllMenuSaveDataNull();
                    return false;
                }

                SetAllMenuSaveDataNull();
                int received = 0;
                int occupied = 0;
                int materialized = 0;
                foreach (string line in lines)
                {
                    if (string.IsNullOrEmpty(line) || !line.StartsWith("OFFICIAL_SAVE_SLOT|", StringComparison.OrdinalIgnoreCase))
                        continue;
                    received++;
                    string[] parts = line.Split('|');
                    if (parts.Length < 6)
                        continue;
                    int safeSlot = 0;
                    int.TryParse(parts[1], out safeSlot);
                    safeSlot = Mathf.Clamp(safeSlot, 0, 5);
                    int occ = 0;
                    int.TryParse(parts[2], out occ);
                    string display = FromB64(parts[3]);
                    string saveJson = FromB64(parts[5]);
                    if (occ != 0 && !string.IsNullOrEmpty(saveJson))
                    {
                        occupied++;
                        object saveObj = FromJsonCompat(saveJson, saveType);
                        if (saveObj != null)
                        {
                            SetSaveDataForSlot(safeSlot, saveObj);
                            materialized++;
                            Log("Loaded Official Server slot" + safeSlot + " from server: display=" + display + ", bytes=" + Encoding.UTF8.GetByteCount(saveJson) + ".");
                        }
                        else
                        {
                            Log("Official Server slot" + safeSlot + " was occupied but SaveData deserialize returned null. display=" + display + ", bytes=" + Encoding.UTF8.GetByteCount(saveJson) + ".");
                        }
                    }
                }

                string done = lines.FirstOrDefault(x => x != null && x.StartsWith("OFFICIAL_SAVE_SLOTS_DONE|", StringComparison.OrdinalIgnoreCase));
                _onlineSlotPayloadsLoaded = true;
                Log("Loaded Official Server online save slots via v2. received=" + received + ", occupied=" + occupied + ", materialized=" + materialized + ". done=" + (done ?? "missing") + ".");
                return true;
            }
            catch (Exception ex)
            {
                Log("FetchOnlineSaveSlotsFromServerV2 failed: " + ex.Message);
                SetAllMenuSaveDataNull();
                return false;
            }
        }

        private static bool FetchOnlineSaveSlotsFromServerLegacyJson()
        {
            try
            {
                string token = GTSRuntimeHost.GetAioSessionTokenForSocial();
                if (string.IsNullOrEmpty(token))
                {
                    Log("Cannot load Official Server save slots: Steam session token is empty.");
                    SetAllMenuSaveDataNull();
                    return false;
                }

                string line = SendOfficialServerRequest("OFFICIAL_SAVE_SLOTS_REQ|" + B64(token), "OFFICIAL_SAVE_SLOTS", "OFFICIAL_SAVE_ERROR");
                if (string.IsNullOrEmpty(line) || line.StartsWith("OFFICIAL_SAVE_ERROR|", StringComparison.OrdinalIgnoreCase))
                {
                    Log("Official Server legacy save slot request failed: " + DecodeServerError(line));
                    SetAllMenuSaveDataNull();
                    return false;
                }

                string[] parts = line.Split(new char[] { '|' }, 2);
                if (parts.Length < 2)
                {
                    SetAllMenuSaveDataNull();
                    return false;
                }

                string json = FromB64(parts[1]);
                OfficialOnlineSaveSlotsResponse resp = FromJsonCompat(json, typeof(OfficialOnlineSaveSlotsResponse)) as OfficialOnlineSaveSlotsResponse;
                SetAllMenuSaveDataNull();
                int occupied = 0;
                int materialized = 0;
                if (resp != null && resp.slots != null)
                {
                    Type saveType = FindGameType("SaveData");
                    foreach (OfficialOnlineSaveSlotInfo slot in resp.slots)
                    {
                        if (slot == null) continue;
                        int safeSlot = Mathf.Clamp(slot.slot, 0, 5);
                        if (slot.occupied != 0 && !string.IsNullOrEmpty(slot.save_json) && saveType != null)
                        {
                            occupied++;
                            object saveObj = FromJsonCompat(slot.save_json, saveType);
                            if (saveObj != null)
                            {
                                SetSaveDataForSlot(safeSlot, saveObj);
                                materialized++;
                            }
                        }
                    }
                }
                _onlineSlotPayloadsLoaded = true;
                Log("Loaded Official Server online save slots via legacy JSON. Occupied=" + occupied + "/6, materialized=" + materialized + "/6.");
                return true;
            }
            catch (Exception ex)
            {
                Log("FetchOnlineSaveSlotsFromServerLegacyJson failed: " + ex.Message);
                SetAllMenuSaveDataNull();
                return false;
            }
        }

        private static Type GetJsonUtilityTypeCompat()
        {
            try
            {
                Type t = Type.GetType("UnityEngine.JsonUtility, UnityEngine.JSONSerializeModule");
                if (t != null)
                    return t;

                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        t = asm.GetType("UnityEngine.JsonUtility");
                        if (t != null)
                            return t;
                    }
                    catch { }
                }
            }
            catch { }
            return null;
        }

        private static object FromJsonCompat(string json, Type targetType)
        {
            try
            {
                if (string.IsNullOrEmpty(json) || targetType == null)
                    return null;

                Type jsonUtilityType = GetJsonUtilityTypeCompat();
                if (jsonUtilityType == null)
                {
                    Log("UnityEngine.JsonUtility type was not found for Official Server JSON load.");
                    return null;
                }

                MethodInfo typedFromJson = jsonUtilityType.GetMethod("FromJson", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string), typeof(Type) }, null);
                if (typedFromJson != null)
                    return typedFromJson.Invoke(null, new object[] { json, targetType });

                MethodInfo genericFromJson = jsonUtilityType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m => m.Name == "FromJson" && m.IsGenericMethodDefinition && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(string));
                if (genericFromJson != null)
                    return genericFromJson.MakeGenericMethod(targetType).Invoke(null, new object[] { json });
            }
            catch (Exception ex)
            {
                Log("Official Server JSON load failed: " + ex.Message);
            }
            return null;
        }

        private static string ToJsonCompat(object obj, bool prettyPrint)
        {
            try
            {
                if (obj == null)
                    return null;

                Type jsonUtilityType = GetJsonUtilityTypeCompat();
                if (jsonUtilityType == null)
                {
                    Log("UnityEngine.JsonUtility type was not found for Official Server JSON save.");
                    return null;
                }

                MethodInfo toJson = jsonUtilityType.GetMethod("ToJson", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(object), typeof(bool) }, null);
                if (toJson != null)
                    return toJson.Invoke(null, new object[] { obj, prettyPrint }) as string;

                toJson = jsonUtilityType.GetMethod("ToJson", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(object) }, null);
                if (toJson != null)
                    return toJson.Invoke(null, new object[] { obj }) as string;
            }
            catch (Exception ex)
            {
                Log("Official Server JSON save failed: " + ex.Message);
            }
            return null;
        }

        private static bool UploadOnlineSaveSlotToServer(object saveDataObj, int slot)
        {
            try
            {
                if (saveDataObj == null)
                {
                    Log("Official Server save upload skipped for slot" + slot + ": saveData is null.");
                    return false;
                }
                string token = GTSRuntimeHost.GetAioSessionTokenForSocial();
                if (string.IsNullOrEmpty(token))
                {
                    Log("Official Server save upload failed for slot" + slot + ": Steam session token is empty.");
                    return false;
                }
                string json = ToJsonCompat(saveDataObj, true);
                if (string.IsNullOrEmpty(json) || json == "{}")
                {
                    Log("Official Server save upload failed for slot" + slot + ": SaveData serialized empty.");
                    return false;
                }
                string line = SendOfficialServerRequest("OFFICIAL_SAVE_WRITE|" + B64(token) + "|" + Mathf.Clamp(slot, 0, 5).ToString() + "|" + B64(json), "OFFICIAL_SAVE_WRITE_OK", "OFFICIAL_SAVE_ERROR");
                if (!string.IsNullOrEmpty(line) && line.StartsWith("OFFICIAL_SAVE_WRITE_OK|", StringComparison.OrdinalIgnoreCase))
                    return true;
                Log("Official Server save upload failed for slot" + slot + ": " + DecodeServerError(line));
                return false;
            }
            catch (Exception ex)
            {
                Log("UploadOnlineSaveSlotToServer failed: " + ex.Message);
                return false;
            }
        }

        private static List<string> SendOfficialServerRequestLines(string request, string donePrefix, string errorPrefix)
        {
            EnsureConfig();
            string host = ServerHost != null ? (ServerHost.Value ?? "").Trim() : "mmo.gamingwithgoose.com";
            int port = SocialPort != null ? SocialPort.Value : 61529;
            int timeout = NetworkTimeoutMs != null ? Mathf.Clamp(NetworkTimeoutMs.Value, 1000, 20000) : 5000;
            List<string> lines = new List<string>();
            using (TcpClient c = new TcpClient())
            {
                c.NoDelay = true;
                c.ReceiveTimeout = timeout;
                c.SendTimeout = timeout;
                IAsyncResult ar = c.BeginConnect(host, port, null, null);
                if (!ar.AsyncWaitHandle.WaitOne(timeout))
                    throw new TimeoutException("Timed out connecting to " + host + ":" + port);
                c.EndConnect(ar);
                using (NetworkStream ns = c.GetStream())
                using (StreamReader reader = new StreamReader(ns, Encoding.UTF8))
                using (StreamWriter writer = new StreamWriter(ns, new UTF8Encoding(false)))
                {
                    writer.AutoFlush = true;
                    try { reader.ReadLine(); } catch { } // initial WELCOME|server-ready
                    writer.WriteLine(request);
                    DateTime until = DateTime.UtcNow.AddMilliseconds(timeout);
                    while (DateTime.UtcNow < until)
                    {
                        string line = reader.ReadLine();
                        if (line == null)
                            break;
                        lines.Add(line);
                        if ((!string.IsNullOrEmpty(donePrefix) && line.StartsWith(donePrefix, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrEmpty(errorPrefix) && line.StartsWith(errorPrefix, StringComparison.OrdinalIgnoreCase)))
                            break;
                    }
                }
            }
            return lines;
        }

        private static string SendOfficialServerRequest(string request, params string[] acceptedPrefixes)
        {
            EnsureConfig();
            string host = ServerHost != null ? (ServerHost.Value ?? "").Trim() : "mmo.gamingwithgoose.com";
            int port = SocialPort != null ? SocialPort.Value : 61529;
            int timeout = NetworkTimeoutMs != null ? Mathf.Clamp(NetworkTimeoutMs.Value, 1000, 20000) : 5000;
            using (TcpClient c = new TcpClient())
            {
                c.NoDelay = true;
                c.ReceiveTimeout = timeout;
                c.SendTimeout = timeout;
                IAsyncResult ar = c.BeginConnect(host, port, null, null);
                if (!ar.AsyncWaitHandle.WaitOne(timeout))
                    throw new TimeoutException("Timed out connecting to " + host + ":" + port);
                c.EndConnect(ar);
                using (NetworkStream ns = c.GetStream())
                using (StreamReader reader = new StreamReader(ns, Encoding.UTF8))
                using (StreamWriter writer = new StreamWriter(ns, new UTF8Encoding(false)))
                {
                    writer.AutoFlush = true;
                    try { reader.ReadLine(); } catch { } // initial WELCOME|server-ready
                    writer.WriteLine(request);
                    DateTime until = DateTime.UtcNow.AddMilliseconds(timeout);
                    while (DateTime.UtcNow < until)
                    {
                        string line = reader.ReadLine();
                        if (line == null)
                            break;
                        foreach (string prefix in acceptedPrefixes)
                        {
                            if (!string.IsNullOrEmpty(prefix) && line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                                return line;
                        }
                    }
                }
            }
            return string.Empty;
        }

        private static string DecodeServerError(string line)
        {
            try
            {
                if (string.IsNullOrEmpty(line)) return "no response";
                string[] p = line.Split('|');
                if (p.Length >= 2) return FromB64(p[1]);
                return line;
            }
            catch { return line ?? "unknown error"; }
        }

        private static string B64(string value)
        {
            try { return Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty)); } catch { return string.Empty; }
        }

        private static string FromB64(string value)
        {
            try { return Encoding.UTF8.GetString(Convert.FromBase64String(value ?? string.Empty)); } catch { return value ?? string.Empty; }
        }

        private static object GetSaveDataForSlot(int slot)
        {
            try
            {
                Type menuType = FindGameType("MenuScript");
                FieldInfo f = menuType != null ? menuType.GetField("saveData", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) : null;
                Array arr = f != null ? f.GetValue(null) as Array : null;
                if (arr == null || slot < 0 || slot >= arr.Length) return null;
                return arr.GetValue(slot);
            }
            catch { return null; }
        }

        private static void SetSaveDataForSlot(int slot, object saveDataObj)
        {
            try
            {
                Type menuType = FindGameType("MenuScript");
                FieldInfo f = menuType != null ? menuType.GetField("saveData", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) : null;
                Array arr = f != null ? f.GetValue(null) as Array : null;
                if (arr == null || slot < 0 || slot >= arr.Length) return;
                SanitizeSaveDataForMenuPreview(saveDataObj, "slot" + slot);
                arr.SetValue(saveDataObj, slot);
                if (_onlineMode && slot >= 0 && slot < _onlineSaveSlotCache.Length)
                    _onlineSaveSlotCache[slot] = saveDataObj;
            }
            catch { }
        }

        private static void SanitizeSaveDataForMenuPreview(object saveDataObj, string context)
        {
            try
            {
                if (saveDataObj == null)
                    return;

                // MenuScript.HoverSaveFile assumes teamMons entries are either null or a full pipe-delimited
                // Mon save string. Some saves contain the literal placeholder "NULL" in the team array.
                // Vanilla then calls GetMonFromSaveString("NULL") and spams warnings/stack traces while
                // moving between slots. Normalize those placeholders to null before the native preview code runs.
                int cleaned = 0;
                cleaned += SanitizeStringArrayField(saveDataObj, "teamMons", 4, requireFullMonSave: true);
                cleaned += SanitizeStringArrayField(saveDataObj, "lastEncounterMons", 4, requireFullMonSave: false);

                if (cleaned > 0)
                    Log("Sanitized " + cleaned + " placeholder save-string value(s) for native save preview in " + context + ".");
            }
            catch (Exception ex)
            {
                Log("SanitizeSaveDataForMenuPreview failed for " + context + ": " + ex.Message);
            }
        }

        private static int SanitizeStringArrayField(object obj, string fieldName, int minimumLength, bool requireFullMonSave)
        {
            try
            {
                if (obj == null)
                    return 0;
                FieldInfo f = obj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (f == null || f.FieldType != typeof(string[]))
                    return 0;

                string[] arr = f.GetValue(obj) as string[];
                int changed = 0;
                if (arr == null)
                {
                    f.SetValue(obj, new string[Math.Max(0, minimumLength)]);
                    return Math.Max(0, minimumLength);
                }

                for (int i = 0; i < arr.Length; i++)
                {
                    string v = arr[i];
                    if (string.IsNullOrWhiteSpace(v) ||
                        string.Equals(v, "NULL", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(v, "null", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(v, "None", StringComparison.OrdinalIgnoreCase))
                    {
                        if (v != null)
                        {
                            arr[i] = null;
                            changed++;
                        }
                        continue;
                    }

                    // For teamMons, the native preview parser requires at least 10 pipe fields.
                    // If a malformed placeholder slips through, treat it as an empty team slot so
                    // HoverSaveFile can keep using the official preview flow without warning spam.
                    if (requireFullMonSave && v.IndexOf('|') < 0)
                    {
                        arr[i] = null;
                        changed++;
                    }
                }
                return changed;
            }
            catch
            {
                return 0;
            }
        }

        private static void SetAllMenuSaveDataNull()
        {
            try
            {
                Type menuType = FindGameType("MenuScript");
                FieldInfo f = menuType != null ? menuType.GetField("saveData", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) : null;
                Array arr = f != null ? f.GetValue(null) as Array : null;
                if (arr == null) return;
                for (int i = 0; i < Math.Min(6, arr.Length); i++)
                    arr.SetValue(null, i);
            }
            catch { }
        }

        private static void RepairLocalSaveSlotVisuals()
        {
            try
            {
                if (_menuObj == null || _onlineMode)
                    return;
                for (int i = 0; i < 6; i++)
                {
                    object save = GetSaveDataForSlot(i);
                    GameObject plus = GetGameObjectFromArrayField(_menuObj, "savePlusObj", i);
                    GameObject preview = GetGameObjectFromArrayField(_menuObj, "savePlayerPreviewObj", i);
                    GameObject secondary = GetGameObjectFromArrayField(_menuObj, "savePlayerPreviewSecondaryColor", i);
                    if (save == null)
                    {
                        if (plus != null) plus.SetActive(true);
                        if (preview != null) preview.SetActive(false);
                    }
                    else
                    {
                        if (plus != null) plus.SetActive(false);
                        if (preview != null) preview.SetActive(true);
                        if (secondary != null) secondary.SetActive(true);
                    }
                }
                Log("Repaired local save slot preview/secondary-color visibility after leaving Online Mode.");
            }
            catch (Exception ex)
            {
                Log("RepairLocalSaveSlotVisuals failed: " + ex.Message);
            }
        }

        private static Type FindGameType(string name)
        {
            try
            {
                Type t = Type.GetType(name + ", Assembly-CSharp");
                if (t != null)
                    return t;
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        t = asm.GetType(name);
                        if (t != null)
                            return t;
                    }
                    catch { }
                }
            }
            catch { }
            return null;
        }

        private static void ClearMenuSaveDataForOnline()
        {
            try
            {
                if (!_onlineMode)
                    return;
                Type menuType = FindGameType("MenuScript");
                FieldInfo f = menuType != null ? menuType.GetField("saveData", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) : null;
                Array arr = f != null ? f.GetValue(null) as Array : null;
                if (arr == null)
                    return;
                for (int i = 0; i < Math.Min(6, arr.Length); i++)
                    arr.SetValue(null, i);
                if (!_onlineSaveDataClearedLogged)
                {
                    _onlineSaveDataClearedLogged = true;
                    Log("Cleared in-memory MenuScript.saveData while in Online Mode so native save slots stay empty and local saves cannot flash/load.");
                }
            }
            catch (Exception ex)
            {
                Log("ClearMenuSaveDataForOnline failed: " + ex.Message);
            }
        }

        private static void RestoreLocalSaveDataFromDisk()
        {
            try
            {
                Type menuType = FindGameType("MenuScript");
                Type saveSystemType = FindGameType("SaveSystem");
                FieldInfo f = menuType != null ? menuType.GetField("saveData", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) : null;
                MethodInfo load = saveSystemType != null ? saveSystemType.GetMethod("LoadGame", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) : null;
                Array arr = f != null ? f.GetValue(null) as Array : null;
                if (arr == null || load == null)
                    return;
                for (int i = 0; i < Math.Min(6, arr.Length); i++)
                {
                    object save = null;
                    try { save = load.Invoke(null, new object[] { i }); } catch { save = null; }
                    SanitizeSaveDataForMenuPreview(save, "local slot" + i);
                    arr.SetValue(save, i);
                }
                Log("Restored local MenuScript.saveData from disk after leaving Online Mode.");
            }
            catch (Exception ex)
            {
                Log("RestoreLocalSaveDataFromDisk failed: " + ex.Message);
            }
        }

        private static void EnsureHostComponent()
        {
            try
            {
                if (_host != null)
                    return;
                if (_root != null)
                {
                    _host = _root.GetComponent<OfficialServerSaveSelectCoroutineHost>();
                    if (_host == null)
                        _host = _root.AddComponent<OfficialServerSaveSelectCoroutineHost>();
                }
            }
            catch { }
        }

        private static void InvokeMenuMethod(object menuObj, string methodName)
        {
            try
            {
                MethodInfo m = menuObj != null ? menuObj.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) : null;
                if (m != null)
                    m.Invoke(menuObj, null);
            }
            catch { }
        }

        private static bool TryInvokeMenuMethod(object menuObj, string methodName)
        {
            try
            {
                MethodInfo m = menuObj != null ? menuObj.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) : null;
                if (m == null)
                    return false;
                m.Invoke(menuObj, null);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void SetObjectField(object obj, string field, object value)
        {
            try
            {
                if (obj == null)
                    return;
                FieldInfo f = obj.GetType().GetField(field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                if (f != null)
                    f.SetValue(obj, value);
            }
            catch { }
        }

        private static void ResetClonedButtonComponents(GameObject go)
        {
            try
            {
                foreach (EventTrigger trig in go.GetComponents<EventTrigger>())
                    UnityEngine.Object.DestroyImmediate(trig);
                foreach (Button b in go.GetComponents<Button>())
                    UnityEngine.Object.DestroyImmediate(b);
                foreach (Selectable s in go.GetComponents<Selectable>())
                {
                    if (!(s is Button))
                        UnityEngine.Object.DestroyImmediate(s);
                }
                if (go.GetComponent<Button>() == null)
                    go.AddComponent<Button>();
            }
            catch { }
        }

        private static void StripNonTextComponents(GameObject go)
        {
            try
            {
                foreach (Button b in go.GetComponents<Button>()) UnityEngine.Object.DestroyImmediate(b);
                foreach (Image img in go.GetComponents<Image>()) UnityEngine.Object.DestroyImmediate(img);
                foreach (EventTrigger e in go.GetComponents<EventTrigger>()) UnityEngine.Object.DestroyImmediate(e);
            }
            catch { }
        }

        private static TMP_Text FindText(GameObject go)
        {
            if (go == null)
                return null;
            TMP_Text t = go.GetComponent<TMP_Text>();
            if (t != null)
                return t;
            return go.GetComponentInChildren<TMP_Text>(true);
        }

        private static RectTransform GetRectField(object obj, string field)
        {
            GameObject go = GetGameObjectField(obj, field);
            if (go == null)
                return null;
            return go.GetComponent<RectTransform>();
        }

        private static GameObject GetGameObjectField(object obj, string field)
        {
            object value = GetObjectField(obj, field);
            if (value is GameObject go)
                return go;
            Component comp = value as Component;
            return comp != null ? comp.gameObject : null;
        }

        private static GameObject GetGameObjectFromArrayField(object obj, string field, int index)
        {
            try
            {
                object arrObj = GetObjectField(obj, field);
                Array arr = arrObj as Array;
                if (arr == null || index < 0 || index >= arr.Length)
                    return null;
                object value = arr.GetValue(index);
                if (value is GameObject go)
                    return go;
                Component c = value as Component;
                return c != null ? c.gameObject : null;
            }
            catch { return null; }
        }

        private static object GetObjectField(object obj, string field)
        {
            if (obj == null)
                return null;
            FieldInfo f = obj.GetType().GetField(field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            return f != null ? f.GetValue(obj) : null;
        }

        private static void LogOncePerSlot(int slot, string message)
        {
            // Keep hover logging finite so BepInEx does not get spammed during menu navigation.
            if (slot == _lastHoverSlot)
                Log(message);
        }

        private static void Log(string msg)
        {
            try { Console.WriteLine("[MMOnsterpatch Official Server] " + msg); } catch { }
            try { Debug.Log("[MMOnsterpatch Official Server] " + msg); } catch { }
        }
    }
}
