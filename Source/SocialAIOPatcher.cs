using BepInEx;
using BepInEx.Configuration;
using System.Collections;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using UnityEngine;
using TMPro;
using Goose.Monsterpatch.GlobalBoxAccess;
using Goose.Monsterpatch.GTSAllInOnePatcher;

namespace Goose.Monsterpatch.SocialPatcher
{
    public static class SocialNativePatcher
    {
        public const string PatcherName = "MMOnsterpatch AIO Patcher";
        public const string PatcherVersion = "Final_GTSAIOBridge_RichListingTest_v1.1";

        public static IEnumerable<string> TargetDLLs
        {
            get { yield return "Assembly-CSharp.dll"; }
        }

        public static void Initialize()
        {
            Console.WriteLine("[MMOnsterpatch AIO Patcher] " + PatcherVersion + " loaded. Runtime chat host and integrated Global Box host will be injected from GameScript.Start. Trading Post is integrated lazily through the chat window so a GTS failure cannot block chat/follower startup. PlayerController input patching stays owned by the MMO runtime; integrated Global Box is guarded from chat input. Trading Post themed chat layout with delayed inactive transparency and guild creation/chat support, improved scrollback, active input indicator, quiet persisted guild state restore, and server-issued persistent per-save-slot character identity/public handles, manual chat connection button, and MenuScript.SelectSaveFile(slot) tracking, with SaveSystem slot hooks as fallback. No force-save on Connect. Adds silent /gleave removes the visible slot selector from the chat header, and adds a Birb icon picker button beside chat input with runtime game icon lookup for houses, shiny, and day/night icons, plus a scrollable Monsterpatch icon picker, static baked emoji/icon sizing, wrapped inline icon chat messages, a transparent Birb-only picker button, tighter chat message prefixes, and remote player sprite layering changed to follower-style world sorting.");
        }

        public static void Finish()
        {
            Console.WriteLine("[MMOnsterpatch AIO Patcher] Finish.");
        }

        public static void Patch(AssemblyDefinition assembly)
        {
            int injectedHost = 0;
            int saveLoadHooks = 0;
            int saveSaveHooks = 0;
            int menuSelectHooks = 0;

            try
            {
                injectedHost = PatchGameScriptStart(assembly);
                saveLoadHooks = PatchSaveSystemLoadGame(assembly);
                saveSaveHooks = PatchSaveSystemSaveGame(assembly);
                menuSelectHooks = PatchMenuScriptSelectSaveFile(assembly);
                int battleExposeHooks = AIOBattleExposure.ExposeBattleSystemMethods(assembly);
                Console.WriteLine("[MMOnsterpatch AIO Patcher] Patch complete. GameScript.Start host/globalbox injection(s)=" + injectedHost + ", SaveSystem.LoadGame hook(s)=" + saveLoadHooks + ", SaveSystem.SaveGame hook(s)=" + saveSaveHooks + ", MenuScript.SelectSaveFile hook(s)=" + menuSelectHooks + ", BattleSystem exposed method(s)=" + battleExposeHooks + ". PlayerController input guards remain chat/window safe.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[MMOnsterpatch AIO Patcher] Patch failed: " + ex);
            }
        }

        private static int PatchGameScriptStart(AssemblyDefinition assembly)
        {
            ModuleDefinition module = assembly.MainModule;
            TypeDefinition gameScript = module.Types.FirstOrDefault(t => t.Name == "GameScript");
            if (gameScript == null)
            {
                Console.WriteLine("[MMOnsterpatch AIO Patcher] GameScript not found; chat runtime host was not injected.");
                return 0;
            }

            MethodDefinition start = gameScript.Methods.FirstOrDefault(m => m.Name == "Start" && m.HasBody);
            if (start == null)
            {
                Console.WriteLine("[MMOnsterpatch AIO Patcher] GameScript.Start not found; chat runtime host was not injected.");
                return 0;
            }

            MethodInfo ensureHost = typeof(SocialRuntimeHost).GetMethod(nameof(SocialRuntimeHost.EnsureHost), BindingFlags.Public | BindingFlags.Static);
            MethodReference ensureHostRef = module.ImportReference(ensureHost);
            MethodInfo ensureGlobalBoxHost = typeof(GlobalBoxRuntimeHost).GetMethod(nameof(GlobalBoxRuntimeHost.EnsureHost), BindingFlags.Public | BindingFlags.Static);
            MethodReference ensureGlobalBoxHostRef = module.ImportReference(ensureGlobalBoxHost);

            ILProcessor il = start.Body.GetILProcessor();
            Instruction[] rets = start.Body.Instructions.Where(i => i.OpCode == OpCodes.Ret).ToArray();
            if (rets.Length == 0)
            {
                il.Append(il.Create(OpCodes.Call, ensureGlobalBoxHostRef));
                il.Append(il.Create(OpCodes.Call, ensureHostRef));
                il.Append(il.Create(OpCodes.Ret));
                return 1;
            }

            foreach (Instruction ret in rets)
            {
                // Global Box is created first so its config/static state exists before
                // the AIO bootstrap installs assembly-wide Harmony patches.
                il.InsertBefore(ret, il.Create(OpCodes.Call, ensureGlobalBoxHostRef));
                il.InsertBefore(ret, il.Create(OpCodes.Call, ensureHostRef));
            }

            return rets.Length;
        }

        private static int PatchMenuScriptSelectSaveFile(AssemblyDefinition assembly)
        {
            ModuleDefinition module = assembly.MainModule;
            TypeDefinition menuScript = module.Types.FirstOrDefault(t => t.Name == "MenuScript");
            if (menuScript == null)
            {
                Console.WriteLine("[MMOnsterpatch AIO Patcher] MenuScript not found; save-slot select tracking hook was not injected.");
                return 0;
            }

            MethodDefinition selectSaveFile = menuScript.Methods.FirstOrDefault(m => m.Name == "SelectSaveFile" && m.HasBody && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == module.TypeSystem.Int32.FullName);
            if (selectSaveFile == null)
            {
                Console.WriteLine("[MMOnsterpatch AIO Patcher] MenuScript.SelectSaveFile(int) not found; save-slot select tracking hook was not injected.");
                return 0;
            }

            MethodInfo onSelect = typeof(SocialRuntimeHost).GetMethod(nameof(SocialRuntimeHost.OnMenuScriptSelectSaveFile), BindingFlags.Public | BindingFlags.Static);
            MethodReference onSelectRef = module.ImportReference(onSelect);

            ILProcessor il = selectSaveFile.Body.GetILProcessor();
            Instruction first = selectSaveFile.Body.Instructions.FirstOrDefault();
            if (first == null)
                return 0;

            // SelectSaveFile is an instance method in current Monsterpatch, so the first parameter is ldarg.1.
            // If it ever becomes static, the first parameter would be ldarg.0.
            il.InsertBefore(first, il.Create(selectSaveFile.IsStatic ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1));
            il.InsertBefore(first, il.Create(OpCodes.Call, onSelectRef));
            return 1;
        }

        private static int PatchSaveSystemLoadGame(AssemblyDefinition assembly)
        {
            ModuleDefinition module = assembly.MainModule;
            TypeDefinition saveSystem = module.Types.FirstOrDefault(t => t.Name == "SaveSystem");
            if (saveSystem == null)
            {
                Console.WriteLine("[MMOnsterpatch AIO Patcher] SaveSystem not found; save-slot tracking hook was not injected.");
                return 0;
            }

            MethodDefinition loadGame = saveSystem.Methods.FirstOrDefault(m => m.Name == "LoadGame" && m.HasBody && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == module.TypeSystem.Int32.FullName);
            if (loadGame == null)
            {
                Console.WriteLine("[MMOnsterpatch AIO Patcher] SaveSystem.LoadGame(int) not found; save-slot tracking hook was not injected.");
                return 0;
            }

            MethodInfo onLoad = typeof(SocialRuntimeHost).GetMethod(nameof(SocialRuntimeHost.OnSaveSystemLoadGame), BindingFlags.Public | BindingFlags.Static);
            MethodReference onLoadRef = module.ImportReference(onLoad);

            loadGame.Body.InitLocals = true;
            VariableDefinition retLocal = new VariableDefinition(loadGame.ReturnType);
            loadGame.Body.Variables.Add(retLocal);

            ILProcessor il = loadGame.Body.GetILProcessor();
            Instruction[] rets = loadGame.Body.Instructions.Where(i => i.OpCode == OpCodes.Ret).ToArray();
            foreach (Instruction ret in rets)
            {
                il.InsertBefore(ret, il.Create(OpCodes.Stloc, retLocal));
                il.InsertBefore(ret, il.Create(OpCodes.Ldarg_0));
                il.InsertBefore(ret, il.Create(OpCodes.Ldloc, retLocal));
                il.InsertBefore(ret, il.Create(OpCodes.Call, onLoadRef));
                il.InsertBefore(ret, il.Create(OpCodes.Ldloc, retLocal));
            }

            return rets.Length;
        }

        private static int PatchSaveSystemSaveGame(AssemblyDefinition assembly)
        {
            ModuleDefinition module = assembly.MainModule;
            TypeDefinition saveSystem = module.Types.FirstOrDefault(t => t.Name == "SaveSystem");
            if (saveSystem == null)
            {
                Console.WriteLine("[MMOnsterpatch AIO Patcher] SaveSystem not found; save-slot save tracking hook was not injected.");
                return 0;
            }

            MethodDefinition saveGame = saveSystem.Methods.FirstOrDefault(m => m.Name == "SaveGame" && m.HasBody && m.Parameters.Count == 2 && m.Parameters[1].ParameterType.FullName == module.TypeSystem.Int32.FullName);
            if (saveGame == null)
            {
                Console.WriteLine("[MMOnsterpatch AIO Patcher] SaveSystem.SaveGame(SaveData,int) not found; save-slot save tracking hook was not injected.");
                return 0;
            }

            MethodInfo onSave = typeof(SocialRuntimeHost).GetMethod(nameof(SocialRuntimeHost.OnSaveSystemSaveGame), BindingFlags.Public | BindingFlags.Static);
            MethodReference onSaveRef = module.ImportReference(onSave);

            ILProcessor il = saveGame.Body.GetILProcessor();
            Instruction[] rets = saveGame.Body.Instructions.Where(i => i.OpCode == OpCodes.Ret).ToArray();
            foreach (Instruction ret in rets)
            {
                il.InsertBefore(ret, il.Create(OpCodes.Ldarg_1));
                il.InsertBefore(ret, il.Create(OpCodes.Ldarg_0));
                il.InsertBefore(ret, il.Create(OpCodes.Call, onSaveRef));
            }

            return rets.Length;
        }

    public sealed class SocialRuntimeHost : MonoBehaviour
    {
        public static SocialRuntimeHost Instance;
        public static volatile bool ChatInputActive;
        public static int LastSaveSystemLoadedSlot = -1;
        public static long LastSaveSystemLoadedTicks = 0L;
        public static int LastSaveSystemSavedSlot = -1;
        public static long LastSaveSystemSavedTicks = 0L;
        public static int LastMenuSelectedSlot = -1;
        public static long LastMenuSelectedTicks = 0L;

        public static void OnMenuScriptSelectSaveFile(int slot)
        {
            try
            {
                if (slot < 0 || slot > 5)
                    return;

                LastMenuSelectedSlot = slot;
                LastMenuSelectedTicks = DateTime.UtcNow.Ticks;

                SocialRuntimeHost inst = Instance;
                if (inst != null && inst.debugLogging != null && inst.debugLogging.Value)
                    inst.Log("MenuScript.SelectSaveFile tracked confirmed selected slot" + slot + ".");
            }
            catch { }
        }

        public static void OnSaveSystemLoadGame(int slot, object saveData)
        {
            try
            {
                if (slot < 0 || slot > 5 || saveData == null)
                    return;

                LastSaveSystemLoadedSlot = slot;
                LastSaveSystemLoadedTicks = DateTime.UtcNow.Ticks;

                SocialRuntimeHost inst = Instance;
                if (inst != null && inst.debugLogging != null && inst.debugLogging.Value)
                    inst.Log("SaveSystem.LoadGame tracked candidate active slot" + slot + ".");
            }
            catch { }
        }

        public static void OnSaveSystemSaveGame(int slot, object saveData)
        {
            try
            {
                if (slot < 0 || slot > 5 || saveData == null)
                    return;

                LastSaveSystemSavedSlot = slot;
                LastSaveSystemSavedTicks = DateTime.UtcNow.Ticks;

                SocialRuntimeHost inst = Instance;
                if (inst != null && inst.debugLogging != null && inst.debugLogging.Value)
                    inst.Log("SaveSystem.SaveGame tracked saved slot" + slot + ".");
            }
            catch { }
        }

        private ConfigFile config;
        private ConfigEntry<string> serverHost;
        private ConfigEntry<int> serverPort;
        private ConfigEntry<bool> autoConnect;
        private ConfigEntry<string> usernameOverride;
        private ConfigEntry<string> socialCharacterId;
        private ConfigEntry<string> socialSecretToken;
        private ConfigEntry<string> socialPublicHandle;
        private ConfigEntry<int> socialPublicSerial;
        private ConfigEntry<string> lastDisplayName;
        private ConfigEntry<string> socialAccountUuid;
        private ConfigEntry<int> activeSaveSlotOverride;
        private ConfigEntry<int> lastResolvedSaveSlot;
        private ConfigEntry<string>[] slotCharacterId = new ConfigEntry<string>[6];
        private ConfigEntry<string>[] slotSecretToken = new ConfigEntry<string>[6];
        private ConfigEntry<string>[] slotPublicHandle = new ConfigEntry<string>[6];
        private ConfigEntry<int>[] slotPublicSerial = new ConfigEntry<int>[6];
        private ConfigEntry<string>[] slotLastDisplayName = new ConfigEntry<string>[6];
        private ConfigEntry<string>[] slotSaveFingerprint = new ConfigEntry<string>[6];
        private ConfigEntry<KeyCode> openChatKey;
        private ConfigEntry<KeyCode> toggleChatKey;
        private ConfigEntry<int> maxHistory;
        private ConfigEntry<float> inactiveOpacity;
        private ConfigEntry<float> activeOpacity;
        private ConfigEntry<float> windowX;
        private ConfigEntry<float> windowY;
        private ConfigEntry<float> windowWidth;
        private ConfigEntry<float> windowHeight;
        private ConfigEntry<bool> lockWindow;
        private ConfigEntry<bool> autoScrollToBottom;
        private ConfigEntry<int> inactiveFadeDelayMs;
        private ConfigEntry<int> maxInputRows;
        private ConfigEntry<bool> debugLogging;
        private ConfigEntry<int> reconnectSeconds;
        private ConfigEntry<bool> disconnectOnNoActiveSave;
        private ConfigEntry<float> noActiveSaveDisconnectSeconds;
        private ConfigEntry<bool> clearHistoryOnNoActiveSaveDisconnect;
        private ConfigEntry<bool> forceSaveBeforeConnectForSlotDetection;

        private Rect windowRect;
        private Rect lastExpandedWindowRect;
        private Rect lastSavedRect;
        private bool visible = true;
        private bool minimized;
        private bool focused;
        private bool wantFocus;
        private bool wantClearGuiFocus;
        private int clearGuiFocusFrames;
        private bool pendingSubmit;
        private bool suppressNextSubmitGuiEvent;
        private bool ignoreSubmitUntilRelease;
        private bool resizing;
        private bool restoreExpandedWindowAfterGui;
        private float lastRectSaveTime;
        private float lastClickTime;
        private float inactiveSinceTime = -999f;
        private bool renderAsActive;
        private int activeTab;
        private bool inGuild;
        private string guildId = string.Empty;
        private string guildName = string.Empty;
        private string guildRank = string.Empty;
        private bool creatingGuild;
        private string newGuildName = string.Empty;
        private bool wantFocusGuildName;
        private bool invitePopupVisible;
        private string invitePopupGuildId = string.Empty;
        private string invitePopupGuildName = string.Empty;
        private string invitePopupInviter = string.Empty;
        private string inputText = string.Empty;
        private bool symbolPickerVisible;
        private Vector2 symbolPickerScroll;
        private bool chatIconScanComplete;
        private float nextChatIconScanTime;
        private readonly List<ChatIconEntry> chatIconEntries = new List<ChatIconEntry>();
        private ChatIconEntry emojiToggleIcon;
        private static readonly string[] BuiltInChatSymbols = new string[] { "★", "♥", "❤", "✓", "☠", "⚔", "⚡", "✦", "✧", "♪", "♫", "→", "←" };
        private Vector2 scrollGlobal;
        private Vector2 scrollGuild;
        private Vector2 scrollWhisper;
        private bool forceScrollGlobalToBottom = true;
        private bool forceScrollGuildToBottom = true;
        private bool forceScrollWhisperToBottom = true;
        private GUIStyle msgStyle;
        private GUIStyle inlineMsgStyle;
        private GUIStyle faintStyle;
        private GUIStyle inputStyle;
        private GUIStyle smallButtonStyle;
        private GUIStyle windowStyle;
        private GUIStyle titleStyle;
        private GUIStyle headerStyle;
        private GUIStyle cardStyle;
        private GUIStyle tabButtonStyle;
        private GUIStyle tabButtonActiveStyle;
        private GUIStyle resizeStyle;
        private Texture2D paperTex;
        private Texture2D cardTex;
        private Texture2D buttonTex;
        private Texture2D buttonHoverTex;
        private Texture2D buttonActiveTex;
        private Texture2D darkTex;
        private Texture2D textFieldTex;
        private Texture2D inactivePaperTex;
        private Texture2D inactiveCardTex;
        private Texture2D inactiveButtonTex;
        private Texture2D inactiveButtonHoverTex;
        private Texture2D inactiveButtonActiveTex;
        private Texture2D inactiveDarkTex;
        private Texture2D inactiveTextFieldTex;
        private bool stylesAppliedFocused;
        private float stylesAppliedInactiveOpacity = -1f;
        private float stylesAppliedActiveOpacity = -1f;

        private const string InputControl = "MMONSTERPATCH_SOCIAL_PATCHER_INPUT";
        private const string GuildNameControl = "MMONSTERPATCH_SOCIAL_PATCHER_GUILD_NAME";
        private const int WindowId = 6152902;
        private const float MinWidth = 360f;
        private const float MinHeight = 160f;
        private const float MaxWidth = 1000f;
        private const float MaxHeight = 640f;
        private const float MinimizedHeight = 34f;
        private const float MinimizedWidthRatio = 0.50f;
        private const float MinimizedMinWidth = 220f;
        private const float InputLineHeight = 24f;
        private const float InputMinHeight = 28f;

        // Baked icon sizing defaults from EmojiTest tuning. These are intentionally
        // static now so normal user configs cannot change the Monsterpatch icon layout.
        private const float EmojiInlineIconSize = 25f;
        private const float EmojiInlineIconYOffset = -10f;
        private const float EmojiInlineIconSpacing = -1f;
        private const float EmojiPickerIconSize = 30f;
        private const float EmojiPickerButtonSize = 34f;
        private const float EmojiToggleButtonSize = 28f;
        private const float EmojiToggleIconSize = 50f;
        private const float EmojiToggleIconXOffset = 0f;
        private const float EmojiToggleIconYOffset = -10f;

        private readonly object historyLock = new object();
        private readonly List<ChatMessage> globalHistory = new List<ChatMessage>();
        private readonly List<ChatMessage> guildHistory = new List<ChatMessage>();
        private readonly List<ChatMessage> whisperHistory = new List<ChatMessage>();
        private readonly Queue<string> inboundLines = new Queue<string>();
        private readonly object inboundLock = new object();
        private readonly object sendLock = new object();

        private Thread networkThread;
        private volatile bool stopNetwork;
        private volatile bool connected;
        private TcpClient client;
        private StreamWriter writer;
        private string username;
        private string characterId = string.Empty;
        private string secretToken = string.Empty;
        private string publicHandle = string.Empty;
        private int publicSerial;
        private string accountUuid = string.Empty;
        private string currentSlotFingerprint = string.Empty;
        private int activeSaveSlot;
        private float noGameplaySince = -1f;
        private bool disconnectedBecauseNoActiveSave;
        private bool connectAllServicesBusy;

        public static void EnsureHost()
        {
            try
            {
                if (Instance != null)
                    return;

                GameObject go = new GameObject("Monsterpatch_MMOnsterpatch_AIO_Runtime");
                DontDestroyOnLoad(go);
                Instance = go.AddComponent<SocialRuntimeHost>();
                Debug.Log("[MMOnsterpatch AIO Patcher] runtime host loaded from GameScript.Start.");
            }
            catch (Exception ex)
            {
                Debug.Log("[MMOnsterpatch AIO Patcher] EnsureHost failed: " + ex);
            }
        }

        public static bool ShouldBlockGameInput()
        {
            try
            {
                return Instance != null && ChatInputActive;
            }
            catch
            {
                return false;
            }
        }

        private void Awake()
        {
            Instance = this;
            global::MMOnsterpatchAIOBootstrap.Ensure();
            LoadConfig();

            windowRect = new Rect(windowX.Value, windowY.Value, windowWidth.Value, windowHeight.Value);
            ClampWindowRect();
            RememberExpandedWindowRect();
            lastSavedRect = windowRect;
            username = ResolveUsername();

            activeSaveSlot = ResolveActiveSaveSlot(username);
            lastResolvedSaveSlot.Value = activeSaveSlot;
            LoadSavedIdentityFromConfig();

            AddLocalSystem(globalHistory, "Welcome to MMOnsterpatch. Please connect and Press Enter to chat.");

            if (autoConnect.Value)
                StartNetworkThread();

            Log("Loaded. Server=" + serverHost.Value + ":" + serverPort.Value + ", username=" + username + ", slot=slot" + activeSaveSlot + ", handle=" + (string.IsNullOrEmpty(publicHandle) ? "unregistered" : publicHandle));
        }

        private void OnDestroy()
        {
            StopNetworkThread();
            global::MMOnsterpatchAIOBootstrap.DisconnectMMO();
            SaveWindowConfig(true);
            ClearAllHistory();
            ChatInputActive = false;
            if (Instance == this)
                Instance = null;
        }

        private void LoadConfig()
        {
            string configPath = Path.Combine(Paths.ConfigPath, "goose.monsterpatch.mmonsterpatchaio.cfg");
            config = new ConfigFile(configPath, true);

            serverHost = config.Bind("Server", "ServerHost", "mmo.gamingwithgoose.com", "Social chat server host. Uses the same public MMO host by default; social stays on ServerPort 61529.");
            serverPort = config.Bind("Server", "ServerPort", 61529, "Social chat server TCP port.");
            autoConnect = config.Bind("Server", "AutoConnectOnStartup", false, "Connect to the local social server when the game starts. Default false because the chat window has a manual Connect button.");
            if (autoConnect.Value)
            {
                autoConnect.Value = false;
                config.Save();
            }
            reconnectSeconds = config.Bind("Server", "ReconnectSeconds", 3, "Seconds between reconnect attempts.");
            disconnectOnNoActiveSave = config.Bind("Server", "DisconnectOnNoActiveSave", true, "Disconnect social chat when the player exits to title or no loaded save/world is active.");
            noActiveSaveDisconnectSeconds = config.Bind("Server", "NoActiveSaveDisconnectSeconds", 1.25f, "How long no active save/world must be detected before social chat disconnects.");
            clearHistoryOnNoActiveSaveDisconnect = config.Bind("Server", "ClearHistoryOnNoActiveSaveDisconnect", true, "Clear local in-memory chat history when social chat auto-disconnects because the save/world is no longer active.");
            forceSaveBeforeConnectForSlotDetection = config.Bind("Server", "ForceSaveBeforeConnectForSlotDetection", false, "Legacy v0.2.8 option. v0.2.9 does not force-save on Connect; save slot is tracked from MenuScript.SelectSaveFile(slot).");

            usernameOverride = config.Bind("Player", "UsernameOverride", string.Empty, "Optional chat username override. If empty, this tries the in-game character name, then the configured MMOnsterpatch save/player name. It never uses the Windows account name.");

            activeSaveSlotOverride = config.Bind("Identity", "ActiveSaveSlotOverride", -1, "Advanced: force a save slot identity 0-5. Leave -1 for auto-detect.");
            lastResolvedSaveSlot = config.Bind("Identity", "LastResolvedSaveSlot", 0, "Last save slot identity section used by the social patcher.");
            socialCharacterId = config.Bind("Identity", "SocialCharacterId", string.Empty, "Legacy/global identity value from v0.2.0. v0.2.1 uses Identity.slot0 through Identity.slot5 instead.");
            socialSecretToken = config.Bind("Identity", "SocialSecretToken", string.Empty, "Legacy/global identity token from v0.2.0. v0.2.1 uses Identity.slot0 through Identity.slot5 instead.");
            socialPublicHandle = config.Bind("Identity", "PublicHandle", string.Empty, "Legacy/global public handle from v0.2.0. v0.2.1 uses Identity.slot0 through Identity.slot5 instead.");
            socialPublicSerial = config.Bind("Identity", "PublicSerial", 0, "Legacy/global public serial from v0.2.0. v0.2.1 uses Identity.slot0 through Identity.slot5 instead.");
            lastDisplayName = config.Bind("Identity", "LastDisplayName", string.Empty, "Legacy/global last display name from v0.2.0. v0.2.1 uses Identity.slot0 through Identity.slot5 instead.");
            socialAccountUuid = config.Bind("Identity", "AccountUUID", string.Empty, "Server-issued MMOnsterpatch account UUID tied to the authenticated Steam account. Stored for display/debug only; Steam session auth is still required.");
            for (int i = 0; i < 6; i++)
            {
                string section = "Identity.slot" + i;
                slotCharacterId[i] = config.Bind(section, "SocialCharacterId", string.Empty, "Server-issued hidden character UUID for save slot " + i + ". Leave blank to register this slot on next connect.");
                slotSecretToken[i] = config.Bind(section, "SocialSecretToken", string.Empty, "Server-issued secret proof for save slot " + i + ". Do not share this.");
                slotPublicHandle[i] = config.Bind(section, "PublicHandle", string.Empty, "Readable server handle for save slot " + i + ", like GOOSE#4827. The number is randomly generated per character name.");
                slotPublicSerial[i] = config.Bind(section, "PublicSerial", 0, "Four-digit server tag used in PublicHandle for save slot " + i + ". This is server-assigned.");
                slotLastDisplayName[i] = config.Bind(section, "LastDisplayName", string.Empty, "Last in-game character name sent for save slot " + i + ".");
                slotSaveFingerprint[i] = config.Bind(section, "SaveFingerprint", string.Empty, "Server/client fingerprint summary for the save currently occupying this slot. If the slot changes, the server archives the old character and creates a new one.");
            }

            openChatKey = config.Bind("Input", "OpenChatKey", KeyCode.Return, "Key used to focus chat. While chat is focused, this same key sends the message.");
            toggleChatKey = config.Bind("Input", "ToggleChatKey", KeyCode.F9, "Key used to show/minimize the chat window.");

            maxHistory = config.Bind("Window", "MaxHistoryMessages", 200, "Max local in-memory messages per tab. This is not written to disk.");
            inactiveOpacity = config.Bind("Window", "InactiveOpacity", 0.10f, "Chat window opacity when inactive.");
            activeOpacity = config.Bind("Window", "ActiveOpacity", 0.68f, "Chat window opacity when active/focused.");
            if (activeOpacity.Value <= 0.51f)
            {
                activeOpacity.Value = 0.68f;
                config.Save();
            }
            windowX = config.Bind("Window", "WindowX", 868f, "Chat window X position.");
            windowY = config.Bind("Window", "WindowY", 202f, "Chat window Y position.");
            windowWidth = config.Bind("Window", "WindowWidth", 412f, "Chat window width.");
            windowHeight = config.Bind("Window", "WindowHeight", 518f, "Chat window height.");
            lockWindow = config.Bind("Window", "LockWindow", false, "If true, disables moving/resizing the chat window.");
            autoScrollToBottom = config.Bind("Window", "AutoScrollToBottom", true, "If true, the active chat tab always jumps to the newest message.");
            inactiveFadeDelayMs = config.Bind("Window", "InactiveFadeDelayMs", 1000, "Milliseconds to keep the active/full-opacity theme after chat loses focus before fading to inactive transparency.");
            maxInputRows = config.Bind("Window", "MaxInputRows", 3, "Maximum visible wrapped rows in the chat input box before it stops growing taller.");

            debugLogging = config.Bind("Debug", "DebugLogging", true, "Write extra social/chat log lines.");

            config.Save();
            PruneSocialUserConfig(configPath);
            RemoveLegacyEmojiTestConfigSection(configPath);
        }

        private static void PruneSocialUserConfig(string path)
        {
            global::AIOConfigPruner.Prune(path, global::AIOVisibleConfigKeys.MmoSocialUserConfigKeys());
        }

        private void RemoveLegacyEmojiTestConfigSection(string configPath)
        {
            try
            {
                if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
                    return;

                string[] lines = File.ReadAllLines(configPath);
                List<string> kept = new List<string>();
                bool skipping = false;
                bool changed = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i] ?? string.Empty;
                    string trimmed = line.Trim();
                    bool isSection = trimmed.StartsWith("[", StringComparison.Ordinal) && trimmed.EndsWith("]", StringComparison.Ordinal);

                    if (isSection)
                    {
                        if (string.Equals(trimmed, "[EmojiTest]", StringComparison.OrdinalIgnoreCase))
                        {
                            skipping = true;
                            changed = true;
                            continue;
                        }

                        if (skipping)
                            skipping = false;
                    }

                    if (!skipping)
                        kept.Add(line);
                    else
                        changed = true;
                }

                if (changed)
                    File.WriteAllLines(configPath, kept.ToArray());
            }
            catch (Exception ex)
            {
                Log("Could not remove legacy EmojiTest config section: " + ex.Message);
            }
        }

        private void ReleaseChatInputForExternalMenu()
        {
            // Do not call Input.ResetInputAxes() here. The Mon Box / Global Box needs
            // Tab, controller Select/View, Submit, and mouse input to pass through cleanly.
            focused = false;
            symbolPickerVisible = false;
            pendingSubmit = false;
            suppressNextSubmitGuiEvent = false;
            ignoreSubmitUntilRelease = false;
            resizing = false;
            wantFocus = false;
            wantClearGuiFocus = true;
            clearGuiFocusFrames = 6;
            ChatInputActive = false;
            try { ClearGuiFocusHard(); } catch { }
        }

        private void Update()
        {
            DrainInboundLines();
            MonitorActiveSaveConnectionState();

            if (GlobalBoxRuntimeHost.IsMonBoxOpenForChatGuard())
            {
                ReleaseChatInputForExternalMenu();
                return;
            }

            if (focused && pendingSubmit)
            {
                // Submit one frame after IMGUI detects Enter/newline so GUILayout is not
                // modified during Layout/Repaint. This keeps Enter-to-send working with
                // the wrapped TextArea without the Unity control-count exception.
                pendingSubmit = false;
                SendCurrentInput(true);
                suppressNextSubmitGuiEvent = true;
                Input.ResetInputAxes();
                return;
            }

            if (focused)
            {
                ChatInputActive = true;
                Input.ResetInputAxes();

                bool submitHeld = Input.GetKey(openChatKey.Value) || Input.GetKey(KeyCode.KeypadEnter);
                if (ignoreSubmitUntilRelease && !submitHeld)
                    ignoreSubmitUntilRelease = false;

                if (!ignoreSubmitUntilRelease && (Input.GetKeyDown(openChatKey.Value) || Input.GetKeyDown(KeyCode.KeypadEnter)))
                {
                    pendingSubmit = true;
                    suppressNextSubmitGuiEvent = true;
                    Input.ResetInputAxes();
                    return;
                }
            }
            else
            {
                ChatInputActive = false;
                ignoreSubmitUntilRelease = false;
            }

            if (!focused && Input.GetKeyDown(toggleChatKey.Value))
            {
                visible = true;
                if (minimized)
                    FocusChat();
                else
                    MinimizeChat();
                Input.ResetInputAxes();
                return;
            }

            if (focused && Input.GetKeyDown(toggleChatKey.Value))
            {
                MinimizeChat();
                Input.ResetInputAxes();
                return;
            }

            if (!focused && visible && !minimized && Input.GetKeyDown(openChatKey.Value))
            {
                FocusChat(true);
                Input.ResetInputAxes();
                return;
            }

            if (Time.unscaledTime - lastRectSaveTime > 4f)
                SaveWindowConfig(false);
        }

        private void OnGUI()
        {
            if (GlobalBoxRuntimeHost.IsMonBoxOpenForChatGuard())
            {
                ReleaseChatInputForExternalMenu();
                return;
            }

            EnsureStyles();

            if (!visible)
                return;

            ClampWindowRect();
            Event e = Event.current;

            if (e != null && e.type == EventType.KeyDown && suppressNextSubmitGuiEvent && IsSubmitEvent(e))
            {
                suppressNextSubmitGuiEvent = false;
                wantClearGuiFocus = true;
                clearGuiFocusFrames = 6;
                ClearGuiFocusHard();
                Input.ResetInputAxes();
                e.Use();
                return;
            }

            if (e != null && e.type == EventType.MouseDown)
            {
                bool mouseOverTradingPost = false;
                try { mouseOverTradingPost = GTSRuntimeHost.IsMouseOverWindowForAio(e.mousePosition); } catch { }
                if (windowRect.Contains(e.mousePosition))
                {
                    focused = true;
                    inactiveSinceTime = -999f;
                    lastClickTime = Time.unscaledTime;
                }
                else if (mouseOverTradingPost)
                {
                    // Keep chat considered active while the Trading Post is being used.
                    // Do not consume the click here; the GTS window should receive it.
                    focused = true;
                    inactiveSinceTime = -999f;
                    lastClickTime = Time.unscaledTime;
                }
                else if (focused && Time.unscaledTime - lastClickTime > 0.05f)
                {
                    UnfocusChat();
                }
            }

            if (focused && e != null && e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Escape)
                {
                    UnfocusChat();
                    Input.ResetInputAxes();
                    e.Use();
                    return;
                }

                if (IsSubmitEvent(e))
                {
                    if (suppressNextSubmitGuiEvent || ignoreSubmitUntilRelease)
                    {
                        suppressNextSubmitGuiEvent = false;
                    }
                    else if (activeTab == 1 && creatingGuild && GUI.GetNameOfFocusedControl() == GuildNameControl)
                    {
                        SubmitCreateGuild();
                    }
                    else
                    {
                        pendingSubmit = true;
                    }
                    Input.ResetInputAxes();
                    e.Use();
                    return;
                }
            }

            renderAsActive = ShouldRenderActiveTheme();
            ApplyThemeForFocus(renderAsActive);

            float alpha = renderAsActive ? activeOpacity.Value : 1f;
            Color oldColor = GUI.color;
            GUI.color = new Color(oldColor.r, oldColor.g, oldColor.b, Mathf.Clamp01(alpha));
            DrawWindowBacking(windowRect);
            windowRect = GUI.Window(WindowId, windowRect, DrawWindow, GUIContent.none, windowStyle);
            if (restoreExpandedWindowAfterGui && !minimized)
            {
                restoreExpandedWindowAfterGui = false;
                RestoreExpandedWindowSize();
            }
            GUI.color = oldColor;

            if (invitePopupVisible)
            {
                ApplyThemeForFocus(true);
                DrawGuildInvitePopup();
                if (e != null && e.type != EventType.Layout && e.type != EventType.Repaint && (e.isKey || e.isMouse))
                    e.Use();
                return;
            }

            if (wantClearGuiFocus || (!focused && GUI.GetNameOfFocusedControl() == InputControl))
            {
                ClearGuiFocusHard();
                if (clearGuiFocusFrames > 0)
                    clearGuiFocusFrames--;
                else
                    wantClearGuiFocus = false;
            }

            if (wantFocus && !minimized)
            {
                GUI.FocusControl(InputControl);
                focused = true;
                wantFocus = false;
            }

            if (focused && e != null && (e.isKey || e.isMouse))
            {
                bool mouseOverTradingPost = false;
                try { mouseOverTradingPost = e.isMouse && GTSRuntimeHost.IsMouseOverWindowForAio(e.mousePosition); } catch { }
                if (!mouseOverTradingPost)
                    e.Use();
            }
        }

        private void DrawWindow(int id)
        {
            if (minimized)
            {
                // Minimized mode is intentionally only the title/menu bar.
                // Do not draw the preview line or a full "Open" button here, because
                // that makes the collapsed chat look like it is still rendering inside
                // the old expanded body. The same top-right control restores the last
                // expanded size remembered by FocusChat/RestoreExpandedWindowSize.
                GUI.Label(new Rect(0f, 5f, windowRect.width, 24f), "MMOnsterpatch", titleStyle);
                if (GUI.Button(new Rect(windowRect.width - 34f, 5f, 26f, 24f), "+", smallButtonStyle))
                    FocusChat(false);

                if (!lockWindow.Value)
                    GUI.DragWindow(new Rect(0f, 0f, windowRect.width - 36f, MinimizedHeight));
                return;
            }

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("MMOnsterpatch", titleStyle, GUILayout.Height(26f), GUILayout.ExpandWidth(true));
            if (GUILayout.Button("—", smallButtonStyle, GUILayout.Width(28f), GUILayout.Height(24f)))
            {
                MinimizeChat();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical(cardStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label(GetConnectionStatusLabel(), faintStyle, GUILayout.ExpandWidth(true));
            bool netBusy = IsNetworkThreadAlive() || connected || connectAllServicesBusy || GTSRuntimeHost.IsLoggedInForAio() || GTSRuntimeHost.IsAuthBusyForAio();
            if (GUILayout.Button(netBusy ? "Disconnect" : "Connect", smallButtonStyle, GUILayout.Width(104f), GUILayout.Height(24f)))
                ToggleConnectionFromWindow();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(4f);
            DrawTabRow();
            GUILayout.Space(4f);
            DrawActiveHistory();
            GUILayout.Space(4f);

            if (focused && symbolPickerVisible)
            {
                DrawSymbolPicker();
                GUILayout.Space(3f);
            }

            GUILayout.BeginHorizontal();
            float toggleIconSizeForLayout = Mathf.Clamp(EmojiToggleIconSize, 10f, 128f);
            float toggleButtonWidth = Mathf.Clamp(EmojiToggleButtonSize, 10f, 128f);
            float toggleLayoutWidth = focused ? Mathf.Max(toggleButtonWidth, toggleIconSizeForLayout) + 2f : 0f;
            float sendButtonWidth = 62f;
            float inputWidth = Mathf.Max(120f, windowRect.width - toggleLayoutWidth - sendButtonWidth - 42f);
            float inputHeight = CalculateInputHeight(inputWidth);
            if (focused)
            {
                if (DrawEmojiToggleButton(toggleLayoutWidth, inputHeight))
                {
                    symbolPickerVisible = !symbolPickerVisible;
                    FocusChat(false);
                }
            }
            if (focused)
            {
                GUI.SetNextControlName(InputControl);
                ApplyTextCursorSettings();
                inputText = GUILayout.TextArea(inputText ?? string.Empty, inputStyle, GUILayout.Width(inputWidth), GUILayout.MinHeight(inputHeight), GUILayout.MaxHeight(inputHeight));
                StripLineBreaksAndSubmitIfNeeded();
            }
            else
            {
                // Do not draw an interactive TextArea while inactive.
                // Unity IMGUI can otherwise leave keyboardControl on the old text box,
                // making the chat look inactive while still eating keyboard input.
                GUILayout.Label(inputText ?? string.Empty, inputStyle, GUILayout.Width(inputWidth), GUILayout.MinHeight(inputHeight), GUILayout.MaxHeight(inputHeight));
            }

            if (GUILayout.Button(focused ? "Send ✓" : "Send", smallButtonStyle, GUILayout.Width(sendButtonWidth), GUILayout.Height(inputHeight)))
            {
                if (focused)
                    SendCurrentInput();
                else
                    FocusChat(false);
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            DrawResizeHandle();

            if (!lockWindow.Value)
                GUI.DragWindow(new Rect(0f, 0f, windowRect.width - 70f, 28f));
        }

        private void DrawNativeChatTitleBand(float y, float width)
        {
            // Intentionally empty. Earlier test builds drew a colored title band
            // here, but the native Alpha Version Warning window uses a cream paper
            // face with text directly on the panel.
        }

        private void DrawGuildInvitePopup()
        {
            Rect popupRect = new Rect(
                Mathf.Max(12f, (Screen.width - 420f) * 0.5f),
                Mathf.Max(12f, (Screen.height - 190f) * 0.5f),
                420f,
                190f);

            Color oldColor = GUI.color;
            GUI.color = new Color(oldColor.r, oldColor.g, oldColor.b, Mathf.Clamp01(activeOpacity != null ? activeOpacity.Value : 0.68f));
            DrawWindowBacking(popupRect);
            GUI.color = oldColor;

            GUI.Window(WindowId + 1, popupRect, DrawGuildInvitePopupWindow, GUIContent.none, windowStyle);
        }

        private void DrawGuildInvitePopupWindow(int id)
        {
            GUILayout.BeginVertical();
            GUILayout.Label("Guild Invite", titleStyle, GUILayout.Height(26f));
            GUILayout.BeginVertical(cardStyle, GUILayout.ExpandHeight(true));
            GUILayout.Space(4f);
            GUILayout.Label(invitePopupInviter + " invited you to join:", faintStyle, GUILayout.Height(22f));
            GUILayout.Label(invitePopupGuildName, headerStyle, GUILayout.Height(30f));
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Accept", smallButtonStyle, GUILayout.Width(110f), GUILayout.Height(32f)))
                AcceptGuildInvitePopup();
            GUILayout.Space(20f);
            if (GUILayout.Button("Decline", smallButtonStyle, GUILayout.Width(110f), GUILayout.Height(32f)))
                DeclineGuildInvitePopup();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndVertical();
        }

        private void AcceptGuildInvitePopup()
        {
            string gid = invitePopupGuildId;
            string gname = invitePopupGuildName;
            invitePopupVisible = false;
            invitePopupGuildId = string.Empty;
            invitePopupGuildName = string.Empty;
            invitePopupInviter = string.Empty;
            activeTab = 1;
            if (!connected)
                AddLocalSystem(guildHistory, "Not connected to social server yet. Guild invite was not accepted.");
            else if (!string.IsNullOrEmpty(gid))
            {
                SendLine("GUILD_ACCEPT|" + gid);
                AddLocalSystem(guildHistory, "Accepting guild invite to " + gname + "...");
            }
            UnfocusChat(true);
            ClearGuiFocusHard();
        }

        private void DeclineGuildInvitePopup()
        {
            string gid = invitePopupGuildId;
            string gname = invitePopupGuildName;
            invitePopupVisible = false;
            invitePopupGuildId = string.Empty;
            invitePopupGuildName = string.Empty;
            invitePopupInviter = string.Empty;
            activeTab = 1;
            if (connected && !string.IsNullOrEmpty(gid))
                SendLine("GUILD_DECLINE|" + gid);
            AddLocalSystem(guildHistory, "Declined guild invite" + (string.IsNullOrEmpty(gname) ? "." : " to " + gname + "."));
            UnfocusChat(true);
            ClearGuiFocusHard();
        }

        private bool DrawEmojiToggleButton(float buttonWidth, float inputHeight)
        {
            EnsureChatIconsLoaded(false);

            float layoutWidth = Mathf.Max(10f, buttonWidth);
            Rect layoutRect = GUILayoutUtility.GetRect(layoutWidth, inputHeight, GUILayout.Width(layoutWidth), GUILayout.Height(inputHeight));
            float iconSize = Mathf.Clamp(EmojiToggleIconSize, 10f, 128f);
            float xOffset = Mathf.Clamp(EmojiToggleIconXOffset, -128f, 128f);
            float yOffset = Mathf.Clamp(EmojiToggleIconYOffset, -128f, 128f);

            Rect iconRect = new Rect(
                layoutRect.x + (layoutWidth - iconSize) * 0.5f + xOffset,
                layoutRect.y + (inputHeight - iconSize) * 0.5f + yOffset,
                iconSize,
                iconSize);

            bool clicked = GUI.Button(iconRect, GUIContent.none, GUIStyle.none);

            if (emojiToggleIcon != null && emojiToggleIcon.IsUsable)
            {
                DrawChatIcon(emojiToggleIcon, iconRect);
            }
            else
            {
                GUI.Label(iconRect, "❤", titleStyle);
            }

            return clicked;
        }

        private void DrawSymbolPicker()
        {
            EnsureChatIconsLoaded(false);

            GUILayout.BeginVertical(cardStyle);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", smallButtonStyle, GUILayout.Width(58f), GUILayout.Height(22f)))
            {
                symbolPickerVisible = false;
                FocusChat(false);
            }
            GUILayout.EndHorizontal();

            // The icon list can be taller than the chat window once game sprites load.
            // Keep the Close button visible and make the contents scrollable.
            float pickerHeight = Mathf.Clamp(windowRect.height * 0.34f, 120f, 210f);
            symbolPickerScroll = GUILayout.BeginScrollView(symbolPickerScroll, false, true, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none, GUILayout.Height(pickerHeight));

            DrawIconPickerSection("Monsterpatch", "monsterpatch", 13);

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawSymbolPickerSection(string label, string[] symbols)
        {
            if (symbols == null || symbols.Length == 0)
                return;

            GUILayout.Label(label, faintStyle, GUILayout.Height(18f));
            int perRow = 7;
            for (int start = 0; start < symbols.Length; start += perRow)
            {
                GUILayout.BeginHorizontal();
                int end = Math.Min(start + perRow, symbols.Length);
                for (int i = start; i < end; i++)
                {
                    string symbol = symbols[i];
                    if (GUILayout.Button(symbol, smallButtonStyle, GUILayout.Width(32f), GUILayout.Height(28f)))
                        InsertChatSymbol(symbol);
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
        }

        private void DrawIconPickerSection(string label, string group, int expectedCount)
        {
            List<ChatIconEntry> groupIcons = GetIconGroup(group);
            if (groupIcons.Count == 0)
            {
                GUILayout.Label(label + " (not loaded yet)", faintStyle, GUILayout.Height(18f));
                return;
            }

            GUILayout.Label(label, faintStyle, GUILayout.Height(18f));
            float configuredPickerButtonSize = Mathf.Clamp(EmojiPickerButtonSize, 24f, 54f);
            int perRow = Math.Max(1, Mathf.FloorToInt((windowRect.width - 52f) / (configuredPickerButtonSize + 4f)));
            int drawn = 0;
            for (int start = 0; start < groupIcons.Count; start += perRow)
            {
                GUILayout.BeginHorizontal();
                int end = Math.Min(start + perRow, groupIcons.Count);
                for (int i = start; i < end; i++)
                {
                    ChatIconEntry entry = groupIcons[i];
                    if (DrawChatIconButton(entry))
                        InsertChatToken(entry.Token);
                    drawn++;
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            if (drawn < expectedCount)
                GUILayout.Label("Loaded " + drawn + "/" + expectedCount + ". Open the related in-game UI once if an icon is missing.", faintStyle, GUILayout.Height(18f));
        }

        private List<ChatIconEntry> GetIconGroup(string group)
        {
            List<ChatIconEntry> result = new List<ChatIconEntry>();
            for (int i = 0; i < chatIconEntries.Count; i++)
            {
                if (chatIconEntries[i] != null && chatIconEntries[i].Group == group && chatIconEntries[i].IsUsable)
                    result.Add(chatIconEntries[i]);
            }
            result.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            return result;
        }

        private bool DrawChatIconButton(ChatIconEntry entry)
        {
            float buttonSize = Mathf.Clamp(EmojiPickerButtonSize, 24f, 54f);
            float iconSize = Mathf.Clamp(EmojiPickerIconSize, 10f, buttonSize - 8f);

            Rect r = GUILayoutUtility.GetRect(buttonSize, buttonSize, GUILayout.Width(buttonSize), GUILayout.Height(buttonSize));
            bool clicked = GUI.Button(r, GUIContent.none, smallButtonStyle);

            if (entry != null && entry.IsUsable)
            {
                Rect iconRect = new Rect(
                    r.x + (buttonSize - iconSize) * 0.5f,
                    r.y + (buttonSize - iconSize) * 0.5f,
                    iconSize,
                    iconSize);
                DrawChatIcon(entry, iconRect);
            }
            else
            {
                GUI.Label(r, entry != null ? entry.FallbackLabel : "?", smallButtonStyle);
            }

            return clicked;
        }

        private void InsertChatSymbol(string symbol)
        {
            InsertChatText(symbol);
        }

        private void InsertChatToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                return;

            // Icon tokens are text commands like :house0:. Add one trailing space
            // when inserted from the picker so players can keep typing naturally.
            InsertChatText(token.EndsWith(":", StringComparison.Ordinal) ? token + " " : token);
        }

        private void InsertChatText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            inputText = (inputText ?? string.Empty) + text;
            focused = true;
            ChatInputActive = true;
            wantFocus = true;
            inactiveSinceTime = -999f;
            Input.ResetInputAxes();
        }

        private void DrawChatMessageLine(string rendered)
        {
            if (string.IsNullOrEmpty(rendered) || !ContainsChatIconToken(rendered))
            {
                GUILayout.Label(rendered ?? string.Empty, msgStyle);
                return;
            }

            // Icon-rich chat lines are drawn manually so Unity IMGUI can mix small
            // game sprites with text. GUILayout.Label handles normal word wrap, but
            // our custom icon renderer has to reserve enough height and wrap tokens
            // itself based on the current chat window width.
            float estimatedWidth = Mathf.Max(120f, windowRect.width - 58f);
            float h = CalculateIconRichTextHeight(rendered, estimatedWidth);
            Rect r = GUILayoutUtility.GetRect(20f, h, GUILayout.ExpandWidth(true));
            DrawIconRichText(r, rendered);
        }

        private bool ContainsChatIconToken(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            EnsureChatIconsLoaded(false);
            for (int i = 0; i < chatIconEntries.Count; i++)
            {
                ChatIconEntry e = chatIconEntries[i];
                if (e != null && e.IsUsable && text.IndexOf(e.Token, StringComparison.Ordinal) >= 0)
                    return true;
            }
            return false;
        }

        private GUIStyle GetInlineMessageStyle()
        {
            if (inlineMsgStyle == null)
            {
                inlineMsgStyle = msgStyle != null ? new GUIStyle(msgStyle) : new GUIStyle(GUI.skin.label);
                inlineMsgStyle.wordWrap = false;
                inlineMsgStyle.padding = new RectOffset(0, 0, 0, 0);
                inlineMsgStyle.margin = new RectOffset(0, 0, 0, 0);
            }
            return inlineMsgStyle;
        }

        private float GetIconRichTextLineHeight()
        {
            float inlineIconSize = Mathf.Clamp(EmojiInlineIconSize, 10f, 34f);
            float textLine = msgStyle != null ? msgStyle.lineHeight + 7f : 22f;
            return Mathf.Max(22f, Mathf.Max(textLine, inlineIconSize + 6f));
        }

        private float CalculateIconRichTextHeight(string text, float width)
        {
            GUIStyle style = GetInlineMessageStyle();

            if (string.IsNullOrEmpty(text))
                return GetIconRichTextLineHeight();

            float inlineIconSize = Mathf.Clamp(EmojiInlineIconSize, 10f, 34f);
            float inlineSpacing = Mathf.Clamp(EmojiInlineIconSpacing, -4f, 10f);
            float lineHeight = GetIconRichTextLineHeight();
            float maxWidth = Mathf.Max(60f, width - 7f);
            float x = 0f;
            int lines = 1;
            int pos = 0;

            while (pos < text.Length)
            {
                ChatIconEntry icon = FindIconTokenAt(text, pos);
                if (icon != null)
                {
                    float iconTokenWidth = inlineIconSize + inlineSpacing;
                    if (x > 0f && x + iconTokenWidth > maxWidth)
                    {
                        lines++;
                        x = 0f;
                    }
                    x += iconTokenWidth;
                    pos += icon.Token.Length;
                    continue;
                }

                int nextIcon = FindNextIconTokenIndex(text, pos);
                int end = nextIcon >= 0 ? nextIcon : text.Length;
                MeasurePlainTextSegment(text, pos, end, style, maxWidth, ref x, ref lines);
                pos = end;
            }

            return Mathf.Max(lineHeight, lines * lineHeight + 2f);
        }

        private void MeasurePlainTextSegment(string text, int start, int end, GUIStyle style, float maxWidth, ref float x, ref int lines)
        {
            int pos = start;
            while (pos < end)
            {
                char ch = text[pos];
                if (ch == '\r')
                {
                    pos++;
                    continue;
                }
                if (ch == '\n')
                {
                    lines++;
                    x = 0f;
                    pos++;
                    continue;
                }

                int chunkStart = pos;
                bool isSpace = ch == ' ' || ch == '\t';
                while (pos < end)
                {
                    char c = text[pos];
                    if (c == '\r' || c == '\n')
                        break;
                    bool cSpace = c == ' ' || c == '\t';
                    if (cSpace != isSpace)
                        break;
                    pos++;
                }

                string chunk = text.Substring(chunkStart, pos - chunkStart);
                if (string.IsNullOrEmpty(chunk))
                    continue;

                float chunkWidth = Mathf.Max(1f, style.CalcSize(new GUIContent(chunk)).x);
                if (chunkWidth > maxWidth)
                {
                    for (int i = 0; i < chunk.Length; i++)
                    {
                        string single = chunk[i].ToString();
                        float charWidth = Mathf.Max(1f, style.CalcSize(new GUIContent(single)).x);
                        if (x > 0f && x + charWidth > maxWidth)
                        {
                            lines++;
                            x = 0f;
                            if (chunk[i] == ' ' || chunk[i] == '\t')
                                continue;
                        }
                        x += charWidth;
                    }
                    continue;
                }

                if (x > 0f && x + chunkWidth > maxWidth)
                {
                    lines++;
                    x = 0f;
                    if (isSpace)
                        continue;
                }
                x += chunkWidth;
            }
        }

        private void DrawIconRichText(Rect rect, string text)
        {
            GUIStyle style = GetInlineMessageStyle();

            float inlineIconSize = Mathf.Clamp(EmojiInlineIconSize, 10f, 34f);
            float inlineYOffset = Mathf.Clamp(EmojiInlineIconYOffset, -18f, 18f);
            float inlineSpacing = Mathf.Clamp(EmojiInlineIconSpacing, -4f, 10f);
            float lineHeight = GetIconRichTextLineHeight();

            float startX = rect.x + 3f;
            float x = startX;
            float y = rect.y;
            float maxX = rect.xMax - 4f;
            int pos = 0;

            while (pos < text.Length && y < rect.yMax)
            {
                ChatIconEntry icon = FindIconTokenAt(text, pos);
                if (icon != null)
                {
                    float iconTokenWidth = inlineIconSize + inlineSpacing;
                    if (x > startX && x + iconTokenWidth > maxX)
                    {
                        x = startX;
                        y += lineHeight;
                        if (y >= rect.yMax)
                            break;
                    }

                    float iconY = y + Mathf.Max(0f, (lineHeight - inlineIconSize) * 0.5f) + inlineYOffset;
                    Rect iconRect = new Rect(x, iconY, inlineIconSize, inlineIconSize);
                    DrawChatIcon(icon, iconRect);
                    x += iconTokenWidth;
                    pos += icon.Token.Length;
                    continue;
                }

                int nextIcon = FindNextIconTokenIndex(text, pos);
                int end = nextIcon >= 0 ? nextIcon : text.Length;
                DrawPlainTextSegment(text, pos, end, style, lineHeight, startX, maxX, ref x, ref y, rect.yMax);
                pos = end;
            }
        }

        private void DrawPlainTextSegment(string text, int start, int end, GUIStyle style, float lineHeight, float startX, float maxX, ref float x, ref float y, float maxY)
        {
            int pos = start;
            while (pos < end && y < maxY)
            {
                char ch = text[pos];
                if (ch == '\r')
                {
                    pos++;
                    continue;
                }
                if (ch == '\n')
                {
                    x = startX;
                    y += lineHeight;
                    pos++;
                    continue;
                }

                int chunkStart = pos;
                bool isSpace = ch == ' ' || ch == '\t';
                while (pos < end)
                {
                    char c = text[pos];
                    if (c == '\r' || c == '\n')
                        break;
                    bool cSpace = c == ' ' || c == '\t';
                    if (cSpace != isSpace)
                        break;
                    pos++;
                }

                string chunk = text.Substring(chunkStart, pos - chunkStart);
                if (string.IsNullOrEmpty(chunk))
                    continue;

                float availableWidth = Mathf.Max(10f, maxX - startX);
                float chunkWidth = Mathf.Max(1f, style.CalcSize(new GUIContent(chunk)).x);
                if (chunkWidth > availableWidth)
                {
                    for (int i = 0; i < chunk.Length && y < maxY; i++)
                    {
                        string single = chunk[i].ToString();
                        GUIContent singleContent = new GUIContent(single);
                        float charWidth = Mathf.Max(1f, style.CalcSize(singleContent).x);
                        if (x > startX && x + charWidth > maxX)
                        {
                            x = startX;
                            y += lineHeight;
                            if (y >= maxY)
                                break;
                            if (chunk[i] == ' ' || chunk[i] == '\t')
                                continue;
                        }
                        GUI.Label(new Rect(x, y, charWidth + 2f, lineHeight), singleContent, style);
                        x += charWidth;
                    }
                    continue;
                }

                if (x > startX && x + chunkWidth > maxX)
                {
                    x = startX;
                    y += lineHeight;
                    if (y >= maxY)
                        break;
                    if (isSpace)
                        continue;
                }

                GUI.Label(new Rect(x, y, chunkWidth + 2f, lineHeight), new GUIContent(chunk), style);
                x += chunkWidth;
            }
        }

        private ChatIconEntry FindIconTokenAt(string text, int index)
        {
            if (string.IsNullOrEmpty(text) || index < 0 || index >= text.Length)
                return null;

            for (int i = 0; i < chatIconEntries.Count; i++)
            {
                ChatIconEntry e = chatIconEntries[i];
                if (e == null || !e.IsUsable || string.IsNullOrEmpty(e.Token))
                    continue;

                if (index + e.Token.Length <= text.Length && string.CompareOrdinal(text, index, e.Token, 0, e.Token.Length) == 0)
                    return e;
            }
            return null;
        }

        private int FindNextIconTokenIndex(string text, int start)
        {
            int best = -1;
            for (int i = 0; i < chatIconEntries.Count; i++)
            {
                ChatIconEntry e = chatIconEntries[i];
                if (e == null || !e.IsUsable || string.IsNullOrEmpty(e.Token))
                    continue;

                int found = text.IndexOf(e.Token, start, StringComparison.Ordinal);
                if (found >= 0 && (best < 0 || found < best))
                    best = found;
            }
            return best;
        }

        private void EnsureChatIconsLoaded(bool force)
        {
            if (chatIconScanComplete && !force)
                return;

            if (!force && Time.unscaledTime < nextChatIconScanTime)
                return;

            nextChatIconScanTime = Time.unscaledTime + 2f;
            try
            {
                Sprite[] sprites = Resources.FindObjectsOfTypeAll<Sprite>();
                Dictionary<string, Sprite> byName = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
                if (sprites != null)
                {
                    for (int i = 0; i < sprites.Length; i++)
                    {
                        Sprite sp = sprites[i];
                        if (sp != null && !string.IsNullOrEmpty(sp.name) && !byName.ContainsKey(sp.name))
                            byName.Add(sp.name, sp);
                    }
                }

                chatIconEntries.Clear();
                emojiToggleIcon = BuildChatIcon(byName, "button", 0, string.Empty, new string[] { "0_Birb", "0_BIRB", "0_Birb(Original)", "Birb", "_monsters_0" }, "❤");

                for (int i = 0; i <= 9; i++)
                    AddChatIcon(byName, "monsterpatch", i, ":house" + i + ":", new string[] { "spriteHouses_" + i }, "H" + i);

                AddChatIcon(byName, "monsterpatch", 10, ":shiny:", new string[] { "sparklyIcon" }, "★");
                AddChatIcon(byName, "monsterpatch", 11, ":day:", new string[] { "dayNightIcons_0" }, "D");
                AddChatIcon(byName, "monsterpatch", 12, ":night:", new string[] { "dayNightIcons_1" }, "N");

                int loadedIcons = GetLoadedChatIconCount();
                bool birbLoaded = emojiToggleIcon != null && emojiToggleIcon.IsUsable;
                chatIconScanComplete = loadedIcons >= 13 && birbLoaded;
                Log("Chat icon scan " + (chatIconScanComplete ? "complete" : "partial") + ". Loaded " + loadedIcons + "/13 chat icon sprites. Birb button: " + (birbLoaded ? "loaded" : "not loaded") + ".");
            }
            catch (Exception ex)
            {
                Log("Chat icon scan failed: " + ex.Message);
                chatIconScanComplete = false;
            }
        }

        private void AddChatIcon(Dictionary<string, Sprite> byName, string group, int sortOrder, string token, string[] spriteNames, string fallbackLabel)
        {
            chatIconEntries.Add(BuildChatIcon(byName, group, sortOrder, token, spriteNames, fallbackLabel));
        }

        private ChatIconEntry BuildChatIcon(Dictionary<string, Sprite> byName, string group, int sortOrder, string token, string[] spriteNames, string fallbackLabel)
        {
            Sprite sp = null;
            string matchedName = string.Empty;
            if (byName != null && spriteNames != null)
            {
                for (int i = 0; i < spriteNames.Length; i++)
                {
                    string candidate = spriteNames[i];
                    if (string.IsNullOrEmpty(candidate))
                        continue;
                    if (byName.TryGetValue(candidate, out sp) && sp != null)
                    {
                        matchedName = candidate;
                        break;
                    }
                }
            }

            return new ChatIconEntry
            {
                Group = group,
                SortOrder = sortOrder,
                Token = token,
                SpriteName = matchedName,
                FallbackLabel = fallbackLabel,
                Sprite = sp
            };
        }

        private int GetLoadedChatIconCount()
        {
            int count = 0;
            for (int i = 0; i < chatIconEntries.Count; i++)
            {
                if (chatIconEntries[i] != null && chatIconEntries[i].IsUsable)
                    count++;
            }
            return count;
        }

        private void DrawChatIcon(ChatIconEntry entry, Rect rect)
        {
            if (entry == null || entry.Sprite == null || entry.Sprite.texture == null)
                return;

            Texture2D tex = entry.Sprite.texture;
            Rect tr = entry.Sprite.textureRect;
            Rect uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
            Rect fitted = FitRectPreserveAspect(rect, Mathf.Max(1f, tr.width), Mathf.Max(1f, tr.height));
            GUI.DrawTextureWithTexCoords(fitted, tex, uv, true);
        }

        private Rect FitRectPreserveAspect(Rect bounds, float sourceWidth, float sourceHeight)
        {
            if (sourceWidth <= 0f || sourceHeight <= 0f || bounds.width <= 0f || bounds.height <= 0f)
                return bounds;

            float sourceAspect = sourceWidth / sourceHeight;
            float boundsAspect = bounds.width / bounds.height;

            float drawWidth = bounds.width;
            float drawHeight = bounds.height;
            if (sourceAspect > boundsAspect)
                drawHeight = bounds.width / sourceAspect;
            else
                drawWidth = bounds.height * sourceAspect;

            return new Rect(
                bounds.x + (bounds.width - drawWidth) * 0.5f,
                bounds.y + (bounds.height - drawHeight) * 0.5f,
                drawWidth,
                drawHeight);
        }

        private float CalculateInputHeight(float inputWidth)
        {
            try
            {
                int rows = Mathf.Clamp(maxInputRows != null ? maxInputRows.Value : 3, 1, 6);
                float maxHeight = Mathf.Max(InputMinHeight, InputLineHeight * rows + 8f);
                string preview = string.IsNullOrEmpty(inputText) ? " " : inputText;
                float calc = inputStyle != null ? inputStyle.CalcHeight(new GUIContent(preview), Mathf.Max(80f, inputWidth)) + 2f : InputMinHeight;
                return Mathf.Clamp(calc, InputMinHeight, maxHeight);
            }
            catch
            {
                return InputMinHeight;
            }
        }

        private void StripLineBreaksAndSubmitIfNeeded()
        {
            string value = inputText ?? string.Empty;
            if (value.IndexOf('\n') < 0 && value.IndexOf('\r') < 0)
                return;

            // TextArea is used for word wrap, but Enter should send.
            // If Unity inserts a newline before our submit handler sees it, strip it and
            // queue the send for Update instead of sending during IMGUI Layout/Repaint.
            inputText = value.Replace("\r", string.Empty).Replace("\n", string.Empty);
            pendingSubmit = true;
            suppressNextSubmitGuiEvent = true;
            Input.ResetInputAxes();
        }

        private void ApplyTextCursorSettings()
        {
            try
            {
                GUI.skin.settings.cursorColor = new Color(0.16f, 0.10f, 0.08f, 1f);
                GUI.skin.settings.selectionColor = new Color(0.25f, 0.20f, 0.15f, 0.35f);
                GUI.skin.settings.cursorFlashSpeed = 1.1f;
            }
            catch
            {
            }
        }

        private void DrawTabRow()
        {
            GUILayout.BeginHorizontal();
            DrawTabButton(0, "Global");
            DrawTabButton(1, "Guild");
            DrawTradingPostButton();
            GUILayout.EndHorizontal();
        }

        private void DrawTradingPostButton()
        {
            GUIStyle style = GTSRuntimeHost.IsOpenForAio() ? tabButtonActiveStyle : tabButtonStyle;
            if (GUILayout.Button("Trade Post", style, GUILayout.Height(28f)))
            {
                GTSRuntimeHost.OpenFromAioChatWindow();
                FocusChat(false);
            }
        }

        private void DrawTabButton(int tab, string label)
        {
            GUIStyle style = activeTab == tab ? tabButtonActiveStyle : tabButtonStyle;
            if (GUILayout.Button(label, style, GUILayout.Height(28f)))
            {
                if (activeTab != tab)
                {
                    activeTab = tab;
                    MarkActiveTabForScrollBottom();
                }
            }
        }

        private void DrawActiveHistory()
        {
            if (activeTab == 1)
            {
                DrawGuildTab();
                return;
            }

            List<ChatMessage> list = GetActiveHistory();
            Vector2 scroll = activeTab == 0 ? scrollGlobal : scrollWhisper;
            bool forceBottom = (autoScrollToBottom == null || autoScrollToBottom.Value) && (activeTab == 0 ? forceScrollGlobalToBottom : forceScrollWhisperToBottom);

            if (forceBottom)
                scroll.y = float.MaxValue;

            GUILayout.BeginVertical(cardStyle, GUILayout.ExpandHeight(true));
            scroll = GUILayout.BeginScrollView(scroll, false, true, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none, GUILayout.ExpandHeight(true));

            lock (historyLock)
            {
                for (int i = 0; i < list.Count; i++)
                    DrawChatMessageLine(list[i].Render());
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            if (forceBottom)
            {
                scroll.y = float.MaxValue;
                if (Event.current != null && Event.current.type == EventType.Repaint)
                {
                    if (activeTab == 0) forceScrollGlobalToBottom = false;
                    else forceScrollWhisperToBottom = false;
                }
            }

            if (activeTab == 0) scrollGlobal = scroll;
            else scrollWhisper = scroll;
        }

        private void DrawGuildTab()
        {
            if (!inGuild)
            {
                GUILayout.BeginVertical(cardStyle, GUILayout.ExpandHeight(true));
                GUILayout.Label("[" + DateTime.Now.ToString("HH:mm") + "] You are not in a Guild.", msgStyle);
                GUILayout.Space(18f);

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Create Guild", smallButtonStyle, GUILayout.Width(170f), GUILayout.Height(30f)))
                {
                    creatingGuild = true;
                    wantFocusGuildName = true;
                    FocusChat(false);
                }
                GUILayout.Space(36f);
                if (GUILayout.Button("Browse Guilds", smallButtonStyle, GUILayout.Width(170f), GUILayout.Height(30f)))
                {
                    AddLocalSystem(guildHistory, "Browse Guilds is not wired yet. First pass supports creating a guild and guild-only chat.");
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                if (creatingGuild)
                {
                    GUILayout.Space(14f);
                    GUILayout.BeginVertical(cardStyle);
                    GUILayout.Label("Guild Name", faintStyle);
                    GUI.SetNextControlName(GuildNameControl);
                    newGuildName = GUILayout.TextField(newGuildName ?? string.Empty, inputStyle, GUILayout.Height(28f));
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Create", smallButtonStyle, GUILayout.Width(90f), GUILayout.Height(26f)))
                        SubmitCreateGuild();
                    if (GUILayout.Button("Cancel", smallButtonStyle, GUILayout.Width(90f), GUILayout.Height(26f)))
                    {
                        creatingGuild = false;
                        newGuildName = string.Empty;
                        GUI.FocusControl(InputControl);
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();

                if (wantFocusGuildName)
                {
                    GUI.FocusControl(GuildNameControl);
                    wantFocusGuildName = false;
                }
                return;
            }

            GUILayout.BeginVertical(cardStyle, GUILayout.ExpandHeight(true));
            GUILayout.BeginHorizontal();
            GUILayout.Label("Guild: " + guildName, headerStyle, GUILayout.ExpandWidth(true));
            GUILayout.Label("Rank: " + guildRank, faintStyle, GUILayout.Width(110f));
            GUILayout.EndHorizontal();

            bool forceGuildBottom = (autoScrollToBottom == null || autoScrollToBottom.Value) && forceScrollGuildToBottom;
            if (forceGuildBottom)
                scrollGuild.y = float.MaxValue;

            scrollGuild = GUILayout.BeginScrollView(scrollGuild, false, true, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none, GUILayout.ExpandHeight(true));
            lock (historyLock)
            {
                for (int i = 0; i < guildHistory.Count; i++)
                    DrawChatMessageLine(guildHistory[i].Render());
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            if (forceGuildBottom)
            {
                scrollGuild.y = float.MaxValue;
                if (Event.current != null && Event.current.type == EventType.Repaint)
                    forceScrollGuildToBottom = false;
            }
        }

        private void SubmitCreateGuild()
        {
            string name = (newGuildName ?? string.Empty).Trim();
            if (name.Length < 3)
            {
                AddLocalSystem(guildHistory, "Guild name must be at least 3 characters.");
                return;
            }

            if (!IsValidGuildName(name))
            {
                AddLocalSystem(guildHistory, "Guild name can only use letters, numbers, and spaces.");
                return;
            }

            if (!connected)
            {
                AddLocalSystem(guildHistory, "Not connected to social server yet. Guild was not created.");
                return;
            }

            SendLine("GUILD_CREATE|" + B64(name));
            AddLocalSystem(guildHistory, "Creating guild: " + name + "...");
            creatingGuild = false;
            newGuildName = string.Empty;
            GUI.FocusControl(InputControl);
        }

        private bool IsValidGuildName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (!(char.IsLetterOrDigit(c) || c == ' '))
                    return false;
            }
            return true;
        }

        private List<ChatMessage> GetActiveHistory()
        {
            if (activeTab == 1) return guildHistory;
            if (activeTab == 2) return whisperHistory;
            return globalHistory;
        }

        private void RememberExpandedWindowRect()
        {
            if (minimized)
                return;

            float maxW = Mathf.Min(MaxWidth, Screen.width > 0 ? Screen.width : MaxWidth);
            float maxH = Mathf.Min(MaxHeight, Screen.height > 0 ? Screen.height : MaxHeight);
            lastExpandedWindowRect = new Rect(
                windowRect.x,
                windowRect.y,
                Mathf.Clamp(windowRect.width, MinWidth, maxW),
                Mathf.Clamp(windowRect.height, MinHeight, maxH));
        }

        private bool HasUsableExpandedWindowRect()
        {
            return lastExpandedWindowRect.width >= MinWidth && lastExpandedWindowRect.height >= MinHeight;
        }

        private void RestoreExpandedWindowSize()
        {
            Rect expanded = lastExpandedWindowRect;

            if (!HasUsableExpandedWindowRect())
            {
                expanded = new Rect(
                    windowX != null ? windowX.Value : windowRect.x,
                    windowY != null ? windowY.Value : windowRect.y,
                    windowWidth != null ? windowWidth.Value : windowRect.width,
                    windowHeight != null ? windowHeight.Value : windowRect.height);
            }

            float screenW = Screen.width > 0 ? Screen.width : MaxWidth;
            float screenH = Screen.height > 0 ? Screen.height : MaxHeight;
            windowRect = new Rect(
                expanded.x,
                expanded.y,
                Mathf.Clamp(expanded.width, MinWidth, Mathf.Min(MaxWidth, screenW)),
                Mathf.Clamp(expanded.height, MinHeight, Mathf.Min(MaxHeight, screenH)));
            ClampWindowRect();
        }

        private Rect BuildMinimizedWindowRect()
        {
            Rect expanded = HasUsableExpandedWindowRect() ? lastExpandedWindowRect : windowRect;
            float screenW = Screen.width > 0 ? Screen.width : MaxWidth;
            float screenH = Screen.height > 0 ? Screen.height : MaxHeight;
            float maxMiniW = Mathf.Min(expanded.width, screenW);
            float minMiniW = Mathf.Min(MinimizedMinWidth, maxMiniW);
            float miniW = Mathf.Clamp(expanded.width * MinimizedWidthRatio, minMiniW, maxMiniW);
            float miniH = MinimizedHeight;

            // Anchor the collapsed title bar to the old expanded bottom-left corner.
            // This makes minimize feel like the body collapsed downward instead of
            // the whole chat being pulled upward into the title bar.
            float x = expanded.x;
            float y = expanded.y + expanded.height - miniH;

            x = Mathf.Clamp(x, 0f, Mathf.Max(0f, screenW - miniW));
            y = Mathf.Clamp(y, 0f, Mathf.Max(0f, screenH - miniH));
            return new Rect(x, y, miniW, miniH);
        }

        private void MinimizeChat()
        {
            if (!minimized)
            {
                RememberExpandedWindowRect();
                SaveWindowConfig(true);
            }

            minimized = true;
            windowRect = BuildMinimizedWindowRect();
            UnfocusChat();
            ClampWindowRect();
        }

        private void DrawResizeHandle()
        {
            if (lockWindow.Value)
                return;

            Rect r = new Rect(windowRect.width - 18f, windowRect.height - 18f, 18f, 18f);
            GUI.Label(r, "◢", resizeStyle);
            Event e = Event.current;
            if (e == null)
                return;

            if (e.type == EventType.MouseDown && r.Contains(e.mousePosition))
            {
                resizing = true;
                focused = true;
                e.Use();
            }
            else if (resizing && e.type == EventType.MouseDrag)
            {
                float newW = Mathf.Clamp(e.mousePosition.x + 8f, MinWidth, Mathf.Min(MaxWidth, Screen.width - windowRect.x));
                float newH = Mathf.Clamp(e.mousePosition.y + 8f, MinHeight, Mathf.Min(MaxHeight, Screen.height - windowRect.y));
                windowRect.width = newW;
                windowRect.height = newH;
                e.Use();
            }
            else if (resizing && (e.rawType == EventType.MouseUp || e.type == EventType.MouseUp))
            {
                resizing = false;
                RememberExpandedWindowRect();
                SaveWindowConfig(true);
                e.Use();
            }
        }

        private bool IsSubmitEvent(Event e)
        {
            if (e == null)
                return false;
            if (e.keyCode == openChatKey.Value || e.keyCode == KeyCode.KeypadEnter)
                return true;
            return e.character == '\n' || e.character == '\r';
        }

        private void FocusChat(bool openedBySubmitKey = false)
        {
            visible = true;
            bool wasMinimized = minimized;
            minimized = false;
            if (wasMinimized)
            {
                RestoreExpandedWindowSize();
                restoreExpandedWindowAfterGui = true;
            }
            focused = true;
            inactiveSinceTime = -999f;
            wantFocus = true;
            wantClearGuiFocus = false;
            clearGuiFocusFrames = 0;
            ChatInputActive = true;
            pendingSubmit = false;
            suppressNextSubmitGuiEvent = false;
            ignoreSubmitUntilRelease = openedBySubmitKey;
            MarkActiveTabForScrollBottom();
            Input.ResetInputAxes();
        }

        private void UnfocusChat(bool clearGuiFocus = true)
        {
            focused = false;
            symbolPickerVisible = false;
            inactiveSinceTime = Time.unscaledTime;
            ChatInputActive = false;
            pendingSubmit = false;
            suppressNextSubmitGuiEvent = false;
            ignoreSubmitUntilRelease = false;
            if (clearGuiFocus)
            {
                wantClearGuiFocus = true;
                clearGuiFocusFrames = 6;
                ClearGuiFocusHard();
            }
            Input.ResetInputAxes();
        }

        private void ClearGuiFocusHard()
        {
            try { GUI.FocusControl(null); } catch { }
            try { GUIUtility.keyboardControl = 0; } catch { }
            try
            {
                if (!resizing)
                    GUIUtility.hotControl = 0;
            }
            catch { }
        }

        private bool ShouldRenderActiveTheme()
        {
            if (focused)
                return true;

            float delayMs = Mathf.Max(0f, inactiveFadeDelayMs != null ? inactiveFadeDelayMs.Value : 1000);
            if (delayMs <= 0f)
                return false;

            if (inactiveSinceTime <= 0f)
                return false;

            return (Time.unscaledTime - inactiveSinceTime) < (delayMs / 1000f);
        }

        private void SendCurrentInput(bool clearGuiFocus = true)
        {
            string msg = (inputText ?? string.Empty).Trim();
            inputText = string.Empty;

            if (msg.Length == 0)
            {
                UnfocusChat(clearGuiFocus);
                return;
            }

            if (msg.Equals("/clear", StringComparison.OrdinalIgnoreCase))
            {
                ClearAllHistory();
                AddLocalSystem(globalHistory, "Local chat history cleared.");
                UnfocusChat(clearGuiFocus);
                return;
            }

            if (msg.Equals("/help", StringComparison.OrdinalIgnoreCase))
            {
                AddLocalSystem(GetActiveHistory(), "v0.3.0 commands: /help, /clear, /ginvite PublicHandle, /gaccept GuildId, /gleave. Guild invites show an Accept/Decline popup and social chat disconnects when no active save/world is loaded.");
                UnfocusChat(clearGuiFocus);
                return;
            }

            if (msg.StartsWith("/ginvite ", StringComparison.OrdinalIgnoreCase))
            {
                string target = msg.Substring(9).Trim();
                if (target.Length == 0)
                    AddLocalSystem(guildHistory, "Usage: /ginvite PublicHandle, for example /ginvite Goose#0001");
                else if (!connected)
                    AddLocalSystem(guildHistory, "Not connected to social server yet. Invite was not sent.");
                else
                {
                    SendLine("GUILD_INVITE|" + B64(target));
                    AddLocalSystem(guildHistory, "Invite request sent for " + target + ". If names are duplicated, use the full Name#0001 handle.");
                }
                UnfocusChat(clearGuiFocus);
                return;
            }

            if (msg.StartsWith("/gaccept ", StringComparison.OrdinalIgnoreCase))
            {
                string targetGuild = msg.Substring(9).Trim();
                if (targetGuild.Length == 0)
                    AddLocalSystem(guildHistory, "Usage: /gaccept GuildId");
                else if (!connected)
                    AddLocalSystem(guildHistory, "Not connected to social server yet. Guild invite was not accepted.");
                else
                {
                    SendLine("GUILD_ACCEPT|" + targetGuild);
                    AddLocalSystem(guildHistory, "Accepting guild invite " + targetGuild + "...");
                }
                UnfocusChat(clearGuiFocus);
                return;
            }

            if (msg.Equals("/gleave", StringComparison.OrdinalIgnoreCase))
            {
                if (connected)
                    SendLine("GUILD_LEAVE");
                else if (!inGuild)
                    AddLocalSystem(guildHistory, "You are not in a Guild.");
                else
                    AddLocalSystem(guildHistory, "Not connected to social server yet. Guild leave was not sent.");
                UnfocusChat(clearGuiFocus);
                return;
            }

            if (msg.StartsWith("/", StringComparison.Ordinal))
            {
                AddLocalSystem(GetActiveHistory(), "Unknown command. Type /help for commands.");
                UnfocusChat(clearGuiFocus);
                return;
            }

            if (activeTab == 1)
            {
                if (!inGuild)
                {
                    AddLocalSystem(guildHistory, "You are not in a Guild. Create a guild first or wait for an invite in a later build.");
                    UnfocusChat(clearGuiFocus);
                    return;
                }

                if (!connected)
                {
                    AddLocalSystem(guildHistory, "Not connected to social server yet. Message was not sent.");
                    UnfocusChat(clearGuiFocus);
                    return;
                }

                SendLine("CHAT|GUILD|" + B64(msg));
                UnfocusChat(clearGuiFocus);
                return;
            }

            if (activeTab == 2)
            {
                AddLocalSystem(whisperHistory, "DMs are not wired yet in v0.2.3. Use Global for now.");
                UnfocusChat(clearGuiFocus);
                return;
            }

            if (!connected)
            {
                AddLocalSystem(globalHistory, "Not connected to social server yet. Message was not sent.");
                UnfocusChat(clearGuiFocus);
                return;
            }

            SendLine("CHAT|GLOBAL|" + B64(msg));
            UnfocusChat(clearGuiFocus);
        }

        private bool IsNetworkThreadAlive()
        {
            try
            {
                return networkThread != null && networkThread.IsAlive;
            }
            catch
            {
                return false;
            }
        }


        private void MonitorActiveSaveConnectionState()
        {
            if (disconnectOnNoActiveSave == null || !disconnectOnNoActiveSave.Value)
                return;

            bool gameplayActive = IsLoadedSaveWorldActive();
            if (gameplayActive)
            {
                noGameplaySince = -1f;
                disconnectedBecauseNoActiveSave = false;
                return;
            }

            if (!connected && !IsNetworkThreadAlive())
            {
                noGameplaySince = -1f;
                return;
            }

            if (noGameplaySince < 0f)
                noGameplaySince = Time.unscaledTime;

            float delay = noActiveSaveDisconnectSeconds != null ? Mathf.Max(0.25f, noActiveSaveDisconnectSeconds.Value) : 1.25f;
            if (Time.unscaledTime - noGameplaySince < delay)
                return;

            if (!disconnectedBecauseNoActiveSave)
            {
                disconnectedBecauseNoActiveSave = true;
                StopNetworkThread();
                global::MMOnsterpatchAIOBootstrap.DisconnectMMO();
                ResetSessionStateForNoActiveSave();
                if (clearHistoryOnNoActiveSaveDisconnect == null || clearHistoryOnNoActiveSaveDisconnect.Value)
                {
                    ClearAllHistory();
                    AddLocalSystem(globalHistory, "MMO/social disconnected because no active save/world is loaded.");
                }
                else
                {
                    AddLocalSystem(globalHistory, "MMO/social disconnected because no active save/world is loaded.");
                }
                UnfocusChat(true);
            }
        }

        private void ResetSessionStateForNoActiveSave()
        {
            try
            {
                inGuild = false;
                guildId = string.Empty;
                guildName = string.Empty;
                guildRank = string.Empty;
                invitePopupVisible = false;
                invitePopupGuildId = string.Empty;
                invitePopupGuildName = string.Empty;
                invitePopupInviter = string.Empty;
                creatingGuild = false;
                inputText = string.Empty;
            }
            catch
            {
            }
        }

        private bool IsLoadedSaveWorldActive()
        {
            try
            {
                // GameScript can still exist on/after the title screen because this host is
                // DontDestroyOnLoad. For disconnect-on-title we need to know whether the
                // actual controllable player/world is active, so key off PlayerController.
                Type pc = FindLoadedType("PlayerController");
                if (pc == null)
                    return false;

                UnityEngine.Object obj = UnityEngine.Object.FindObjectOfType(pc);
                if (obj == null)
                    return false;

                Component c = obj as Component;
                if (c == null || c.gameObject == null)
                    return true;

                Behaviour b = c as Behaviour;
                return c.gameObject.activeInHierarchy && (b == null || b.enabled);
            }
            catch
            {
            }
            return false;
        }

        private string GetSlotSelectorLabel()
        {
            try
            {
                int forced = activeSaveSlotOverride != null ? activeSaveSlotOverride.Value : -1;
                if (forced >= 0 && forced <= 5)
                    return "Slot " + forced;
                return "Auto→" + Mathf.Clamp(activeSaveSlot, 0, 5);
            }
            catch
            {
                return "Auto";
            }
        }

        private void CycleActiveSaveSlotOverride()
        {
            try
            {
                if (activeSaveSlotOverride == null)
                    return;

                int v = activeSaveSlotOverride.Value;
                // Cycle: Auto -> slot0 -> slot1 -> ... -> slot5 -> Auto.
                if (v < 0)
                    v = 0;
                else if (v >= 5)
                    v = -1;
                else
                    v++;

                activeSaveSlotOverride.Value = v;
                activeSaveSlot = ResolveActiveSaveSlot(username);
                lastResolvedSaveSlot.Value = activeSaveSlot;
                LoadSavedIdentityFromConfig();
                config.Save();
                PruneSocialUserConfig(config.ConfigFilePath);
                AddLocalSystem(globalHistory, "Social save slot set to " + GetSlotSelectorLabel() + ".");
            }
            catch (Exception ex)
            {
                Log("Could not cycle social save slot: " + ex.Message);
            }
        }

        private string GetConnectionStatusLabel()
        {
            string id = string.IsNullOrEmpty(publicHandle) ? "unregistered" : publicHandle;
            bool serverConnected = connected || global::MMOnsterpatchAIOBootstrap.IsMMOConnected() || GTSRuntimeHost.IsLoggedInForAio();
            bool serverBusy = IsNetworkThreadAlive() || connectAllServicesBusy || global::MMOnsterpatchAIOBootstrap.IsMMOBusy() || GTSRuntimeHost.IsAuthBusyForAio();

            if (serverConnected)
                return "Server Status: <color=#A2BA9C>Connected</color> [" + id + "]";

            if (serverBusy)
                return "Server Status: <color=#A2BA9C>Connecting</color>";

            return "Server Status: <color=#D07D7D>Disconnected</color>";
        }



        private bool ForceSaveForSlotDetection()
        {
            try
            {
                LastSaveSystemSavedSlot = -1;
                LastSaveSystemSavedTicks = 0L;

                Type gsType = FindLoadedType("GameScript");
                if (gsType == null)
                {
                    Log("Could not force save for slot detection: GameScript type not found.");
                    return false;
                }

                UnityEngine.Object gsObj = UnityEngine.Object.FindObjectOfType(gsType);
                if (gsObj == null)
                {
                    Log("Could not force save for slot detection: GameScript instance not found.");
                    return false;
                }

                MethodInfo saveMethod = gsType.GetMethod("SaveGame", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                if (saveMethod == null)
                {
                    Log("Could not force save for slot detection: GameScript.SaveGame method not found.");
                    return false;
                }

                object target = saveMethod.IsStatic ? null : gsObj;
                saveMethod.Invoke(target, null);

                int slot = TryGetRecentSaveSystemSavedSlot();
                if (slot >= 0 && slot <= 5)
                {
                    if (debugLogging != null && debugLogging.Value)
                        Log("Force-save slot detection resolved slot" + slot + ".");
                    return true;
                }

                Log("GameScript.SaveGame was called, but SaveSystem.SaveGame(slot) did not report a slot.");
            }
            catch (Exception ex)
            {
                Log("Could not force save for slot detection: " + ex.Message);
            }

            return false;
        }

        private void ToggleConnectionFromWindow()
        {
            bool socialBusy = IsNetworkThreadAlive() || connected;
            bool mmoBusy = global::MMOnsterpatchAIOBootstrap.IsMMOBusy();
            bool gtsBusy = GTSRuntimeHost.IsLoggedInForAio() || GTSRuntimeHost.IsAuthBusyForAio();

            if (connectAllServicesBusy)
            {
                AddLocalSystem(globalHistory, "Connection/auth is already in progress...");
                return;
            }

            if (socialBusy || mmoBusy || gtsBusy)
            {
                AddLocalSystem(globalHistory, "Disconnecting MMO, social, and Trading Post services...");
                StopNetworkThread();
                global::MMOnsterpatchAIOBootstrap.DisconnectMMO();
                GTSRuntimeHost.DisconnectNetworkOnlyForAio();
                AddLocalSystem(globalHistory, "MMO, social, and Trading Post services disconnected. Steam auth token kept for this game session.");
            }
            else
            {
                StartCoroutine(ConnectAllServicesCoroutine());
            }
        }

        private IEnumerator ConnectAllServicesCoroutine()
        {
            if (connectAllServicesBusy)
                yield break;

            connectAllServicesBusy = true;
            try
            {
                if (!IsLoadedSaveWorldActive())
                {
                    AddLocalSystem(globalHistory, "Load a save/world before connecting MMO/social/Trading Post services.");
                    yield break;
                }

                username = ResolveUsername();
                activeSaveSlot = ResolveActiveSaveSlot(username);
                try { lastResolvedSaveSlot.Value = activeSaveSlot; config.Save(); PruneSocialUserConfig(config.ConfigFilePath); } catch { }
                LoadSavedIdentityFromConfig();

                AddLocalSystem(globalHistory, "Authenticating MMOnsterpatch account with Steam...");
                yield return GTSRuntimeHost.EnsureSteamAuthForAioCoroutine();
                if (!GTSRuntimeHost.IsLoggedInForAio())
                {
                    AddLocalSystem(globalHistory, "Steam authentication did not complete. MMO/social services were not connected.");
                    yield break;
                }

                AddLocalSystem(globalHistory, "Connecting MMO and social services as " + username + "...");
                global::MMOnsterpatchAIOBootstrap.ConnectMMO();
                StartNetworkThread();
            }
            finally
            {
                connectAllServicesBusy = false;
            }
        }


        private void StartNetworkThread()
        {
            if (networkThread != null && networkThread.IsAlive)
                return;

            stopNetwork = false;
            networkThread = new Thread(NetworkLoop);
            networkThread.IsBackground = true;
            networkThread.Name = "MMOnsterpatchSocialPatcherNetwork";
            networkThread.Start();
        }

        private void StopNetworkThread()
        {
            stopNetwork = true;
            connected = false;
            try { if (client != null) client.Close(); } catch { }
            try { if (networkThread != null && networkThread.IsAlive) networkThread.Join(500); } catch { }
        }

        private void NetworkLoop()
        {
            while (!stopNetwork)
            {
                try
                {
                    EnqueueInbound("SYSTEM|Connecting to " + serverHost.Value + ":" + serverPort.Value + "...");
                    using (TcpClient c = new TcpClient())
                    {
                        c.NoDelay = true;
                        c.Connect(serverHost.Value, serverPort.Value);
                        client = c;
                        using (NetworkStream ns = c.GetStream())
                        using (StreamReader reader = new StreamReader(ns, Encoding.UTF8))
                        using (StreamWriter w = new StreamWriter(ns, new UTF8Encoding(false)))
                        {
                            writer = w;
                            writer.AutoFlush = true;
                            connected = true;
                            username = ResolveUsername();
                            // Do not re-guess the slot inside the network thread. The Connect button already resolved
                            // the active slot after an immediate force-save, and re-resolving here could fall back to
                            // stale LoadGame/title-screen data and accidentally reuse slot0.
                            try { lastResolvedSaveSlot.Value = activeSaveSlot; config.Save(); PruneSocialUserConfig(config.ConfigFilePath); } catch { }
                            LoadSavedIdentityFromConfig();
                            currentSlotFingerprint = BuildCurrentSlotFingerprint();
                            string accountSessionToken = GTSRuntimeHost.GetAioSessionTokenForSocial();
                            string slotSnapshot = BuildCurrentSlotRecoverySnapshot();
                            if (!string.IsNullOrEmpty(accountSessionToken))
                            {
                                SendLine("ACCOUNT_SLOT_HELLO|" +
                                    B64(accountSessionToken) + "|" +
                                    Mathf.Clamp(activeSaveSlot, 0, 5).ToString() + "|" +
                                    B64(characterId) + "|" +
                                    B64(secretToken) + "|" +
                                    B64(username) + "|" +
                                    B64(currentSlotFingerprint) + "|" +
                                    B64(slotSnapshot));
                            }
                            else if (!string.IsNullOrEmpty(characterId) && !string.IsNullOrEmpty(secretToken))
                                SendLine("HELLO_ID|" + B64(characterId) + "|" + B64(secretToken) + "|" + B64(username));
                            else
                                SendLine("REGISTER|" + B64(username));
                            SendLine("GUILD_STATE_REQ");

                            string line;
                            while (!stopNetwork && c.Connected && (line = reader.ReadLine()) != null)
                                EnqueueInbound(line);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (!stopNetwork)
                        EnqueueInbound("SYSTEM|Social network error: " + ex.Message);
                }
                finally
                {
                    connected = false;
                    writer = null;
                    try { if (client != null) client.Close(); } catch { }
                    client = null;
                }

                int wait = Mathf.Max(1, reconnectSeconds.Value);
                for (int i = 0; i < wait * 10 && !stopNetwork; i++)
                    Thread.Sleep(100);
            }
        }

        private void SendLine(string line)
        {
            try
            {
                lock (sendLock)
                {
                    if (writer != null)
                    {
                        writer.WriteLine(line);
                        writer.Flush();
                    }
                }
            }
            catch (Exception ex)
            {
                EnqueueInbound("SYSTEM|Send failed: " + ex.Message);
            }
        }

        private void EnqueueInbound(string line)
        {
            lock (inboundLock)
                inboundLines.Enqueue(line);
        }

        private void DrainInboundLines()
        {
            while (true)
            {
                string line = null;
                lock (inboundLock)
                {
                    if (inboundLines.Count > 0)
                        line = inboundLines.Dequeue();
                }

                if (line == null)
                    break;

                HandleServerLine(line);
            }
        }

        private void HandleServerLine(string line)
        {
            try
            {
                if (string.IsNullOrEmpty(line))
                    return;

                string[] parts = line.Split('|');
                string type = parts[0];

                if (type == "SYSTEM")
                {
                    AddLocalSystem(globalHistory, line.Length > 7 ? line.Substring(7) : "System message.");
                    return;
                }

                if (type == "IDENTITY")
                {
                    ApplyIdentity(parts);
                    return;
                }

                if (type == "IDENTITY_ERROR" && parts.Length >= 2)
                {
                    AddLocalSystem(globalHistory, "Identity error: " + FromB64(parts[1]));
                    return;
                }

                if (type == "WELCOME")
                {
                    string msg = parts.Length > 1 ? FromB64(parts[1]) : "server-ready";
                    AddLocalSystem(globalHistory, "Server says: " + msg);
                    return;
                }

                if (type == "GUILD_INVITE" && parts.Length >= 4)
                {
                    string inviteGuildId = parts[1];
                    string inviteGuildName = FromB64(parts[2]);
                    string inviter = FromB64(parts[3]);
                    invitePopupGuildId = inviteGuildId;
                    invitePopupGuildName = inviteGuildName;
                    invitePopupInviter = inviter;
                    invitePopupVisible = true;
                    visible = true;
                    if (minimized)
                    {
                        minimized = false;
                        RestoreExpandedWindowSize();
                    }
                    activeTab = 1;
                    UnfocusChat(true);
                    AddLocalSystem(guildHistory, inviter + " invited you to join " + inviteGuildName + ". Use the popup to Accept or Decline.");
                    AddLocalSystem(globalHistory, "Guild invite received from " + inviter + " for " + inviteGuildName + ".");
                    return;
                }

                if (type == "GUILD_STATE")
                {
                    ApplyGuildState(parts);
                    return;
                }

                if (type == "GUILD_CREATED" && parts.Length >= 4)
                {
                    inGuild = true;
                    guildId = parts[1];
                    guildName = FromB64(parts[2]);
                    guildRank = parts[3];
                    AddLocalSystem(guildHistory, "Guild created: " + guildName + ". You are the Leader.");
                    return;
                }

                if (type == "GUILD_JOINED" && parts.Length >= 4)
                {
                    string newGuildId = parts[1];
                    string newGuildName = FromB64(parts[2]);
                    string newGuildRank = parts[3];
                    bool alreadyInSameGuild = inGuild && string.Equals(guildId, newGuildId, StringComparison.OrdinalIgnoreCase) && string.Equals(guildName, newGuildName, StringComparison.OrdinalIgnoreCase) && string.Equals(guildRank, newGuildRank, StringComparison.OrdinalIgnoreCase);
                    inGuild = true;
                    guildId = newGuildId;
                    guildName = newGuildName;
                    guildRank = newGuildRank;
                    if (!alreadyInSameGuild)
                        AddLocalSystem(guildHistory, "Joined guild: " + guildName + " as " + guildRank + ".");
                    return;
                }

                if (type == "GUILD_LEFT")
                {
                    inGuild = false;
                    guildId = string.Empty;
                    guildName = string.Empty;
                    guildRank = string.Empty;
                    invitePopupVisible = false;
                    invitePopupGuildId = string.Empty;
                    invitePopupGuildName = string.Empty;
                    invitePopupInviter = string.Empty;
                    return;
                }

                if (type == "GUILD_ERROR" && parts.Length >= 2)
                {
                    AddLocalSystem(guildHistory, FromB64(parts[1]));
                    return;
                }

                if (type == "CHAT" && parts.Length >= 4)
                {
                    string channel = parts[1];
                    string from = FromB64(parts[2]);
                    string msg = FromB64(parts[3]);
                    AddMessage(channel, from, msg);
                    return;
                }

                if (debugLogging.Value)
                    AddLocalSystem(globalHistory, "Unknown server line: " + line);
            }
            catch (Exception ex)
            {
                AddLocalSystem(globalHistory, "Could not parse server message: " + ex.Message);
            }
        }

        private void LoadSavedIdentityFromConfig()
        {
            try
            {
                int slot = Mathf.Clamp(activeSaveSlot, 0, 5);
                characterId = slotCharacterId != null && slotCharacterId[slot] != null ? (slotCharacterId[slot].Value ?? string.Empty).Trim() : string.Empty;
                secretToken = slotSecretToken != null && slotSecretToken[slot] != null ? (slotSecretToken[slot].Value ?? string.Empty).Trim() : string.Empty;
                publicHandle = slotPublicHandle != null && slotPublicHandle[slot] != null ? (slotPublicHandle[slot].Value ?? string.Empty).Trim() : string.Empty;
                publicSerial = slotPublicSerial != null && slotPublicSerial[slot] != null ? slotPublicSerial[slot].Value : 0;
                currentSlotFingerprint = slotSaveFingerprint != null && slotSaveFingerprint[slot] != null ? (slotSaveFingerprint[slot].Value ?? string.Empty).Trim() : string.Empty;
                accountUuid = socialAccountUuid != null ? (socialAccountUuid.Value ?? string.Empty).Trim() : string.Empty;
            }
            catch
            {
                characterId = string.Empty;
                secretToken = string.Empty;
                publicHandle = string.Empty;
                publicSerial = 0;
                currentSlotFingerprint = string.Empty;
            }
        }

        private void ApplyIdentity(string[] parts)
        {
            if (parts.Length < 6)
                return;

            string newCharacterId = FromB64(parts[1]).Trim();
            string newSecretToken = FromB64(parts[2]).Trim();
            int newSerial = 0;
            int.TryParse(parts[3], out newSerial);
            string newPublicHandle = FromB64(parts[4]).Trim();
            string newDisplayName = FromB64(parts[5]).Trim();
            string newAccountUuid = parts.Length >= 7 ? FromB64(parts[6]).Trim() : string.Empty;
            string newSlotFingerprint = parts.Length >= 9 ? FromB64(parts[8]).Trim() : string.Empty;

            if (string.IsNullOrEmpty(newCharacterId) || string.IsNullOrEmpty(newSecretToken))
                return;

            bool wasNew = string.IsNullOrEmpty(characterId) || !string.Equals(characterId, newCharacterId, StringComparison.OrdinalIgnoreCase);

            characterId = newCharacterId;
            secretToken = newSecretToken;
            publicSerial = newSerial;
            publicHandle = newPublicHandle;
            if (!string.IsNullOrEmpty(newAccountUuid))
                accountUuid = newAccountUuid;
            if (!string.IsNullOrEmpty(newSlotFingerprint))
                currentSlotFingerprint = newSlotFingerprint;
            else if (string.IsNullOrEmpty(currentSlotFingerprint))
                currentSlotFingerprint = BuildCurrentSlotFingerprint();
            if (!string.IsNullOrEmpty(newDisplayName))
                username = SanitizeName(newDisplayName);

            try
            {
                int slot = Mathf.Clamp(activeSaveSlot, 0, 5);
                slotCharacterId[slot].Value = characterId;
                slotSecretToken[slot].Value = secretToken;
                slotPublicSerial[slot].Value = publicSerial;
                slotPublicHandle[slot].Value = publicHandle;
                slotLastDisplayName[slot].Value = username;
                if (slotSaveFingerprint != null && slotSaveFingerprint[slot] != null)
                    slotSaveFingerprint[slot].Value = currentSlotFingerprint ?? string.Empty;
                if (socialAccountUuid != null && !string.IsNullOrEmpty(accountUuid))
                    socialAccountUuid.Value = accountUuid;
                lastResolvedSaveSlot.Value = slot;
                config.Save();
                PruneSocialUserConfig(config.ConfigFilePath);
            }
            catch (Exception ex)
            {
                AddLocalSystem(globalHistory, "Could not save social identity: " + ex.Message);
            }

            if (wasNew)
                AddLocalSystem(globalHistory, "Registered social character as " + publicHandle + " for slot" + activeSaveSlot + ".");
            else if (!string.IsNullOrEmpty(publicHandle) && debugLogging.Value)
                AddLocalSystem(globalHistory, "Social identity confirmed: " + publicHandle + " for slot" + activeSaveSlot + ".");
        }

        private void ApplyGuildState(string[] parts)
        {
            if (parts.Length >= 2 && string.Equals(parts[1], "NONE", StringComparison.OrdinalIgnoreCase))
            {
                inGuild = false;
                guildId = string.Empty;
                guildName = string.Empty;
                guildRank = string.Empty;
                return;
            }

            if (parts.Length >= 5 && string.Equals(parts[1], "IN", StringComparison.OrdinalIgnoreCase))
            {
                inGuild = true;
                guildId = parts[2];
                guildName = FromB64(parts[3]);
                guildRank = parts[4];
                return;
            }
        }

        private void MarkActiveTabForScrollBottom()
        {
            if (activeTab == 1) forceScrollGuildToBottom = true;
            else if (activeTab == 2) forceScrollWhisperToBottom = true;
            else forceScrollGlobalToBottom = true;
        }

        private void MarkHistoryForScrollBottom(List<ChatMessage> target)
        {
            if (object.ReferenceEquals(target, guildHistory)) forceScrollGuildToBottom = true;
            else if (object.ReferenceEquals(target, whisperHistory)) forceScrollWhisperToBottom = true;
            else forceScrollGlobalToBottom = true;
        }

        private void AddMessage(string channel, string from, string message)
        {
            List<ChatMessage> target = globalHistory;
            if (string.Equals(channel, "GUILD", StringComparison.OrdinalIgnoreCase)) target = guildHistory;
            else if (string.Equals(channel, "DM", StringComparison.OrdinalIgnoreCase)) target = whisperHistory;

            lock (historyLock)
            {
                target.Add(new ChatMessage { Time = DateTime.Now, Channel = channel, From = from, Text = message, IsSystem = false });
                Trim(target);
                MarkHistoryForScrollBottom(target);
            }
        }

        private void AddLocalSystem(List<ChatMessage> target, string message)
        {
            lock (historyLock)
            {
                target.Add(new ChatMessage { Time = DateTime.Now, Channel = "SYSTEM", From = "System", Text = message, IsSystem = true });
                Trim(target);
                MarkHistoryForScrollBottom(target);
            }
        }

        public static void AddTradingPostAcceptedNotification()
        {
            try
            {
                if (Instance != null)
                    Instance.AddTradingPostAcceptedNotificationInstance();
            }
            catch (Exception ex)
            {
                Debug.Log("[MMOnsterpatch AIO Patcher] Could not add Trading Post accepted notification: " + ex.Message);
            }
        }

        private void AddTradingPostAcceptedNotificationInstance()
        {
            visible = true;
            if (minimized)
            {
                minimized = false;
                RestoreExpandedWindowSize();
            }

            activeTab = 0;
            AddLocalSystem(globalHistory, "<color=#D07D7D><b>System:</b> Your Trading Post offer has been accepted.</color>");
        }

        private void Trim(List<ChatMessage> target)
        {
            int max = Mathf.Max(20, maxHistory != null ? maxHistory.Value : 200);
            while (target.Count > max)
                target.RemoveAt(0);
        }

        private void ClearAllHistory()
        {
            lock (historyLock)
            {
                globalHistory.Clear();
                guildHistory.Clear();
                whisperHistory.Clear();
            }
        }

        private string GetLatestLinePreview()
        {
            lock (historyLock)
            {
                if (globalHistory.Count > 0)
                    return globalHistory[globalHistory.Count - 1].Render();
            }
            return "Chat minimized";
        }

        private int ResolveActiveSaveSlot(string resolvedName)
        {
            try
            {
                if (activeSaveSlotOverride != null && activeSaveSlotOverride.Value >= 0 && activeSaveSlotOverride.Value <= 5)
                    return activeSaveSlotOverride.Value;

                int menuSelected = TryGetMenuScriptSelectedSlot();
                if (menuSelected >= 0 && menuSelected <= 5)
                    return menuSelected;

                int saveSystemSaved = TryGetRecentSaveSystemSavedSlot();
                if (saveSystemSaved >= 0 && saveSystemSaved <= 5)
                    return saveSystemSaved;

                int saveSystemTracked = TryGetSaveSystemLoadedSlot();
                if (saveSystemTracked >= 0 && saveSystemTracked <= 5)
                    return saveSystemTracked;

                int reflected = TryGetGameScriptSaveSlot();
                if (reflected >= 0 && reflected <= 5)
                    return reflected;

                // Important: do NOT use MMOnsterpatch SaveFilePath as the active slot.
                // That config is only an optional appearance/name source and is often left
                // pointed at slot0.json, which caused slot1/slot2 characters to reuse slot0.
                int byLoadedState = TryGuessSaveSlotFromLoadedSaveState(resolvedName);
                if (byLoadedState >= 0 && byLoadedState <= 5)
                    return byLoadedState;

                int bySaveName = TryGuessSaveSlotFromPlayerName(resolvedName);
                if (bySaveName >= 0 && bySaveName <= 5)
                    return bySaveName;

                // LastResolvedSaveSlot is intentionally not used as a fallback anymore.
                // A stale last slot is worse than asking the user to fix detection, because it
                // can accidentally connect slot1 as slot0 and reuse guild membership.
                AddLocalSystem(globalHistory, "Could not auto-detect the active save slot. Defaulting to slot0. If this is wrong, exit to title and reselect the save slot, or temporarily set ActiveSaveSlotOverride in the config.");
            }
            catch (Exception ex)
            {
                Log("Could not resolve save slot identity: " + ex.Message);
            }

            return 0;
        }



        private int TryGetMenuScriptSelectedSlot()
        {
            try
            {
                int slot = LastMenuSelectedSlot;
                if (slot < 0 || slot > 5)
                    return -1;

                if (LastMenuSelectedTicks <= 0L)
                    return -1;

                if (debugLogging != null && debugLogging.Value)
                    Log("Using MenuScript.SelectSaveFile tracked slot" + slot + " as active social identity slot.");
                return slot;
            }
            catch { }
            return -1;
        }

        private int TryGetRecentSaveSystemSavedSlot()
        {
            try
            {
                int slot = LastSaveSystemSavedSlot;
                if (slot < 0 || slot > 5)
                    return -1;

                if (LastSaveSystemSavedTicks <= 0L)
                    return -1;

                double ageSeconds = (DateTime.UtcNow.Ticks - LastSaveSystemSavedTicks) / (double)TimeSpan.TicksPerSecond;
                if (ageSeconds > 10.0)
                    return -1;

                if (debugLogging != null && debugLogging.Value)
                    Log("Using recent SaveSystem.SaveGame tracked slot" + slot + " as active social identity slot. age=" + ageSeconds.ToString("0.00") + "s");
                return slot;
            }
            catch { }
            return -1;
        }

        private int TryGetSaveSystemLoadedSlot()
        {
            try
            {
                int slot = LastSaveSystemLoadedSlot;
                if (slot >= 0 && slot <= 5)
                {
                    if (debugLogging != null && debugLogging.Value)
                        Log("Using SaveSystem.LoadGame tracked slot" + slot + " as active social identity slot.");
                    return slot;
                }
            }
            catch { }
            return -1;
        }

        private int TryGuessSaveSlotFromLoadedSaveState(string resolvedName)
        {
            try
            {
                string saveRoot = TryGetLikelySaveFolder();
                if (string.IsNullOrWhiteSpace(saveRoot) || !Directory.Exists(saveRoot))
                    return -1;

                string curName = SanitizeName(resolvedName);
                int curDesign = TryGetGameScriptInt("playerDesign", "PlayerDesign");
                int curColor1 = TryGetGameScriptInt("playerColor1", "PlayerColor1");
                int curColor2 = TryGetGameScriptInt("playerColor2", "PlayerColor2");
                int curWorldBlock = TryGetGameScriptInt("worldBlock", "WorldBlock", "curWorldBlock", "CurWorldBlock");
                int curStarterCount = TryGetGameScriptInt("starterCount", "StarterCount");
                int curDayCount = TryGetGameScriptInt("dayCount", "DayCount");
                string curBestFriend = SanitizeName(TryGetGameScriptString("bestFriendName", "BestFriendName"));
                string curLastInterior = TryGetGameScriptString("lastInteriorName", "LastInteriorName");
                string curLocation = TryGetGameScriptString("curLocation", "CurLocation");

                int bestSlot = -1;
                int bestScore = -1;
                int secondScore = -1;

                for (int i = 0; i <= 5; i++)
                {
                    string path = Path.Combine(saveRoot, "slot" + i + ".json");
                    if (!File.Exists(path))
                        continue;

                    int score = 0;
                    string saveName = SanitizeName(TryReadSaveString(path, "playerName"));
                    if (!string.IsNullOrWhiteSpace(curName) && string.Equals(saveName, curName, StringComparison.OrdinalIgnoreCase))
                        score += 50;

                    if (curDesign >= 0 && TryReadSaveInt(path, "playerDesign") == curDesign) score += 10;
                    if (curColor1 >= 0 && TryReadSaveInt(path, "playerColor1") == curColor1) score += 10;
                    if (curColor2 >= 0 && TryReadSaveInt(path, "playerColor2") == curColor2) score += 10;

                    string saveBestFriend = SanitizeName(TryReadSaveString(path, "bestFriendName"));
                    if (!string.IsNullOrWhiteSpace(curBestFriend) && string.Equals(saveBestFriend, curBestFriend, StringComparison.OrdinalIgnoreCase))
                        score += 12;

                    string saveLastInterior = TryReadSaveString(path, "lastInteriorName");
                    if (!string.IsNullOrWhiteSpace(curLastInterior) && string.Equals(saveLastInterior, curLastInterior, StringComparison.OrdinalIgnoreCase))
                        score += 20;

                    string saveLocation = TryReadSaveString(path, "curLocation");
                    if (!string.IsNullOrWhiteSpace(curLocation) && string.Equals(saveLocation, curLocation, StringComparison.OrdinalIgnoreCase))
                        score += 16;

                    if (curWorldBlock >= 0 && TryReadSaveInt(path, "worldBlock") == curWorldBlock) score += 10;
                    if (curStarterCount >= 0 && TryReadSaveInt(path, "starterCount") == curStarterCount) score += 8;
                    if (curDayCount >= 0 && TryReadSaveInt(path, "dayCount") == curDayCount) score += 4;

                    if (score > bestScore)
                    {
                        secondScore = bestScore;
                        bestScore = score;
                        bestSlot = i;
                    }
                    else if (score > secondScore)
                    {
                        secondScore = score;
                    }
                }

                // Require a strong, unique match. This prevents two same-name saves from
                // silently choosing whichever file happens to be checked first.
                if (bestSlot >= 0 && bestScore >= 60 && bestScore - secondScore >= 8)
                {
                    if (debugLogging != null && debugLogging.Value)
                        Log("Auto-detected active save slot" + bestSlot + " by loaded save state. score=" + bestScore + ", runnerUp=" + secondScore);
                    return bestSlot;
                }

                if (debugLogging != null && debugLogging.Value)
                    Log("Loaded-save-state slot detection was not unique enough. bestSlot=" + bestSlot + ", score=" + bestScore + ", runnerUp=" + secondScore);
            }
            catch (Exception ex)
            {
                Log("Could not guess save slot from loaded save state: " + ex.Message);
            }
            return -1;
        }

        private string BuildCurrentSlotFingerprint()
        {
            try
            {
                string summary = BuildCurrentSlotFingerprintSummary();
                if (string.IsNullOrWhiteSpace(summary))
                    summary = "slot=" + Mathf.Clamp(activeSaveSlot, 0, 5) + "|name=" + SanitizeName(username);

                using (SHA256 sha = SHA256.Create())
                {
                    byte[] data = Encoding.UTF8.GetBytes(summary);
                    byte[] hash = sha.ComputeHash(data);
                    StringBuilder sb = new StringBuilder(hash.Length * 2);
                    for (int i = 0; i < hash.Length; i++)
                        sb.Append(hash[i].ToString("x2"));
                    return sb.ToString();
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        private string BuildCurrentSlotFingerprintSummary()
        {
            try
            {
                int slot = Mathf.Clamp(activeSaveSlot, 0, 5);
                string path = TryGetSavePathForSlot(slot);
                List<string> parts = new List<string>();
                parts.Add("slot=" + slot);

                string name = null;
                int design = -1;
                int color1 = -1;
                int color2 = -1;
                string bestFriend = null;
                int starterCount = -1;

                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                {
                    name = TryReadSaveString(path, "playerName");
                    design = TryReadSaveInt(path, "playerDesign");
                    color1 = TryReadSaveInt(path, "playerColor1");
                    color2 = TryReadSaveInt(path, "playerColor2");
                    bestFriend = TryReadSaveString(path, "bestFriendName");
                    starterCount = TryReadSaveInt(path, "starterCount");
                }

                if (string.IsNullOrWhiteSpace(name)) name = TryGetGameScriptCharacterName();
                if (design < 0) design = TryGetGameScriptInt("playerDesign", "PlayerDesign");
                if (color1 < 0) color1 = TryGetGameScriptInt("playerColor1", "PlayerColor1");
                if (color2 < 0) color2 = TryGetGameScriptInt("playerColor2", "PlayerColor2");
                if (string.IsNullOrWhiteSpace(bestFriend)) bestFriend = TryGetGameScriptString("bestFriendName", "BestFriendName");
                if (starterCount < 0) starterCount = TryGetGameScriptInt("starterCount", "StarterCount");

                parts.Add("name=" + SanitizeName(name));
                parts.Add("design=" + design);
                parts.Add("color1=" + color1);
                parts.Add("color2=" + color2);
                parts.Add("friend=" + SanitizeName(bestFriend));
                parts.Add("starter=" + starterCount);
                return string.Join("|", parts.ToArray());
            }
            catch
            {
                return string.Empty;
            }
        }

        private string BuildCurrentSlotRecoverySnapshot()
        {
            try
            {
                int slot = Mathf.Clamp(activeSaveSlot, 0, 5);
                string path = TryGetSavePathForSlot(slot);
                string summary = BuildCurrentSlotFingerprintSummary();
                string name = SanitizeName(username);
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                {
                    string saveName = TryReadSaveString(path, "playerName");
                    if (!string.IsNullOrWhiteSpace(saveName))
                        name = SanitizeName(saveName);
                }
                return "slot=" + slot + "\nname=" + name + "\nfingerprint=" + BuildCurrentSlotFingerprint() + "\nsummary=" + summary;
            }
            catch
            {
                return string.Empty;
            }
        }

        private string TryGetSavePathForSlot(int slot)
        {
            try
            {
                string saveRoot = TryGetLikelySaveFolder();
                if (string.IsNullOrWhiteSpace(saveRoot))
                    return null;
                string path = Path.Combine(saveRoot, "slot" + Mathf.Clamp(slot, 0, 5) + ".json");
                return File.Exists(path) ? path : null;
            }
            catch { }
            return null;
        }

        private string TryGetGameScriptString(params string[] names)
        {
            try
            {
                Type t = FindLoadedType("GameScript");
                if (t == null)
                    return null;

                foreach (string name in names)
                {
                    FieldInfo f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                    if (f != null && f.FieldType == typeof(string))
                    {
                        object target = null;
                        if (!f.IsStatic)
                            target = UnityEngine.Object.FindObjectOfType(t);
                        if (f.IsStatic || target != null)
                        {
                            string v = f.GetValue(target) as string;
                            if (!string.IsNullOrWhiteSpace(v))
                                return v;
                        }
                    }

                    PropertyInfo p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                    if (p != null && p.PropertyType == typeof(string) && p.GetIndexParameters().Length == 0)
                    {
                        MethodInfo getter = p.GetGetMethod(true);
                        if (getter == null)
                            continue;
                        object target = null;
                        if (!getter.IsStatic)
                            target = UnityEngine.Object.FindObjectOfType(t);
                        if (getter.IsStatic || target != null)
                        {
                            string v = p.GetValue(target, null) as string;
                            if (!string.IsNullOrWhiteSpace(v))
                                return v;
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        private int TryGetGameScriptInt(params string[] names)
        {
            try
            {
                Type t = FindLoadedType("GameScript");
                if (t == null)
                    return -1;

                foreach (string name in names)
                {
                    FieldInfo f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                    if (f != null && (f.FieldType == typeof(int) || f.FieldType == typeof(short) || f.FieldType == typeof(byte)))
                    {
                        object target = null;
                        if (!f.IsStatic)
                            target = UnityEngine.Object.FindObjectOfType(t);
                        if (f.IsStatic || target != null)
                            return Convert.ToInt32(f.GetValue(target));
                    }

                    PropertyInfo p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                    if (p != null && (p.PropertyType == typeof(int) || p.PropertyType == typeof(short) || p.PropertyType == typeof(byte)) && p.GetIndexParameters().Length == 0)
                    {
                        MethodInfo getter = p.GetGetMethod(true);
                        if (getter == null)
                            continue;
                        object target = null;
                        if (!getter.IsStatic)
                            target = UnityEngine.Object.FindObjectOfType(t);
                        if (getter.IsStatic || target != null)
                            return Convert.ToInt32(p.GetValue(target, null));
                    }
                }
            }
            catch { }
            return -1;
        }

        private string TryReadSaveString(string path, string key)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    return null;
                foreach (string raw in File.ReadAllLines(path))
                {
                    string value = TryParseLooseKeyValue(raw, key);
                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }
            }
            catch { }
            return null;
        }

        private int TryReadSaveInt(string path, string key)
        {
            try
            {
                string value = TryReadSaveString(path, key);
                if (string.IsNullOrWhiteSpace(value))
                    return -1;
                int parsed;
                if (int.TryParse(value, out parsed))
                    return parsed;
            }
            catch { }
            return -1;
        }

        private int TryGetGameScriptSaveSlot()
        {
            try
            {
                Type t = FindLoadedType("GameScript");
                if (t == null)
                    return -1;

                string[] names = { "saveSlot", "SaveSlot", "curSaveSlot", "CurSaveSlot", "currentSaveSlot", "CurrentSaveSlot", "saveFileSlot", "SaveFileSlot", "saveIndex", "SaveIndex", "curSaveIndex", "CurSaveIndex", "currentSaveIndex", "CurrentSaveIndex", "selectedSaveSlot", "SelectedSaveSlot" };
                foreach (string name in names)
                {
                    FieldInfo f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                    if (f != null && (f.FieldType == typeof(int) || f.FieldType == typeof(short) || f.FieldType == typeof(byte)))
                    {
                        object target = null;
                        if (!f.IsStatic)
                            target = UnityEngine.Object.FindObjectOfType(t);
                        if (f.IsStatic || target != null)
                        {
                            int v = Convert.ToInt32(f.GetValue(target));
                            if (v >= 0 && v <= 5)
                                return v;
                        }
                    }

                    PropertyInfo p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                    if (p != null && (p.PropertyType == typeof(int) || p.PropertyType == typeof(short) || p.PropertyType == typeof(byte)) && p.GetIndexParameters().Length == 0)
                    {
                        MethodInfo getter = p.GetGetMethod(true);
                        if (getter == null)
                            continue;
                        object target = null;
                        if (!getter.IsStatic)
                            target = UnityEngine.Object.FindObjectOfType(t);
                        if (getter.IsStatic || target != null)
                        {
                            int v = Convert.ToInt32(p.GetValue(target, null));
                            if (v >= 0 && v <= 5)
                                return v;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Could not read GameScript save slot: " + ex.Message);
            }
            return -1;
        }

        private int TryGetSaveSlotFromMmoSaveFilePath()
        {
            try
            {
                string savePath = TryReadMmoConfigValue("SaveFilePath");
                return ParseSlotIndexFromPath(savePath);
            }
            catch { }
            return -1;
        }

        private int TryGuessSaveSlotFromPlayerName(string resolvedName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(resolvedName))
                    return -1;

                string saveRoot = TryGetLikelySaveFolder();
                if (string.IsNullOrWhiteSpace(saveRoot) || !Directory.Exists(saveRoot))
                    return -1;

                string target = SanitizeName(resolvedName);
                int match = -1;
                int count = 0;
                for (int i = 0; i <= 5; i++)
                {
                    string path = Path.Combine(saveRoot, "slot" + i + ".json");
                    if (!File.Exists(path))
                        continue;
                    string name = TryReadPlayerNameFromSavePath(path);
                    if (!string.IsNullOrWhiteSpace(name) && string.Equals(SanitizeName(name), target, StringComparison.OrdinalIgnoreCase))
                    {
                        match = i;
                        count++;
                    }
                }

                if (count == 1)
                    return match;

                if (count > 1 && debugLogging != null && debugLogging.Value)
                    Log("Player-name slot detection found multiple saves named " + target + "; refusing to pick one by name only.");
            }
            catch (Exception ex)
            {
                Log("Could not guess save slot from player name: " + ex.Message);
            }
            return -1;
        }


        private string TryGetLikelySaveFolder()
        {
            try
            {
                string savePath = TryReadMmoConfigValue("SaveFilePath");
                if (!string.IsNullOrWhiteSpace(savePath))
                {
                    string full = Environment.ExpandEnvironmentVariables(savePath.Trim().Trim('"'));
                    if (File.Exists(full))
                        return Path.GetDirectoryName(full);
                    if (Directory.Exists(full))
                        return full;
                }
            }
            catch { }

            try
            {
                string localLow = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (!string.IsNullOrWhiteSpace(localLow))
                {
                    string appRoot = Path.Combine(localLow.Replace("AppData\\Local", "AppData\\LocalLow"), "CoolJosh3k", "Monsterpatch");
                    if (Directory.Exists(appRoot))
                        return appRoot;
                }
            }
            catch { }
            return null;
        }

        private int ParseSlotIndexFromPath(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    return -1;
                string file = Path.GetFileName(path.Trim().Trim('"'));
                if (string.IsNullOrWhiteSpace(file))
                    return -1;
                for (int i = 0; i <= 5; i++)
                {
                    if (file.IndexOf("slot" + i, StringComparison.OrdinalIgnoreCase) >= 0)
                        return i;
                }
            }
            catch { }
            return -1;
        }

        private string ResolveUsername()
        {
            string u = (usernameOverride.Value ?? string.Empty).Trim();
            if (!string.IsNullOrEmpty(u))
                return SanitizeName(u);

            u = TryGetGameScriptCharacterName();
            if (!string.IsNullOrEmpty(u))
                return SanitizeName(u);

            u = TryReadPlayerNameFromConfiguredSave();
            if (!string.IsNullOrEmpty(u))
                return SanitizeName(u);

            u = TryReadMmoConfigValue("PlayerName");
            if (!string.IsNullOrEmpty(u))
                return SanitizeName(u);

            return "Player" + UnityEngine.Random.Range(1000, 9999);
        }

        private string TryGetGameScriptCharacterName()
        {
            try
            {
                Type t = FindLoadedType("GameScript");
                if (t == null)
                    return null;

                string[] names = { "playerName", "PlayerName", "characterName", "CharacterName", "trainerName", "TrainerName" };

                foreach (string name in names)
                {
                    FieldInfo f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                    if (f != null && f.FieldType == typeof(string))
                    {
                        object target = null;
                        if (!f.IsStatic)
                            target = UnityEngine.Object.FindObjectOfType(t);
                        if (f.IsStatic || target != null)
                        {
                            string v = f.GetValue(target) as string;
                            if (!string.IsNullOrWhiteSpace(v))
                                return v;
                        }
                    }

                    PropertyInfo p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                    if (p != null && p.PropertyType == typeof(string) && p.GetIndexParameters().Length == 0)
                    {
                        object target = null;
                        MethodInfo getter = p.GetGetMethod(true);
                        if (getter == null)
                            continue;
                        if (!getter.IsStatic)
                            target = UnityEngine.Object.FindObjectOfType(t);
                        if (getter.IsStatic || target != null)
                        {
                            string v = p.GetValue(target, null) as string;
                            if (!string.IsNullOrWhiteSpace(v))
                                return v;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Could not read GameScript character name: " + ex.Message);
            }

            return null;
        }

        private Type FindLoadedType(string typeName)
        {
            try
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length; i++)
                {
                    Type t = assemblies[i].GetType(typeName, false);
                    if (t != null)
                        return t;

                    try
                    {
                        Type[] types = assemblies[i].GetTypes();
                        for (int j = 0; j < types.Length; j++)
                        {
                            if (types[j].Name == typeName)
                                return types[j];
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return null;
        }

        private string TryReadPlayerNameFromConfiguredSave()
        {
            try
            {
                string savePath = TryReadMmoConfigValue("SaveFilePath");
                if (string.IsNullOrWhiteSpace(savePath) || !File.Exists(savePath))
                    return null;

                return TryReadPlayerNameFromSavePath(savePath);
            }
            catch (Exception ex)
            {
                Log("Could not read playerName from save file: " + ex.Message);
            }

            return null;
        }

        private string TryReadPlayerNameFromSavePath(string savePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(savePath) || !File.Exists(savePath))
                    return null;

                foreach (string raw in File.ReadAllLines(savePath))
                {
                    string value = TryParseLooseKeyValue(raw, "playerName");
                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }
            }
            catch (Exception ex)
            {
                Log("Could not read playerName from save path: " + ex.Message);
            }

            return null;
        }

        private string TryReadMmoConfigValue(string key)
        {
            try
            {
                string cfg = Path.Combine(Paths.ConfigPath, "goose.monsterpatch.mmonsterpatchaio.cfg");
                if (!File.Exists(cfg))
                    return null;

                foreach (string raw in File.ReadAllLines(cfg))
                {
                    string value = TryParseLooseKeyValue(raw, key);
                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }
            }
            catch { }

            return null;
        }

        private string TryParseLooseKeyValue(string raw, string key)
        {
            if (raw == null || key == null)
                return null;

            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#") || line.StartsWith("["))
                return null;

            int idx = line.IndexOf('=');
            if (idx < 0) idx = line.IndexOf(':');
            if (idx < 0) idx = line.IndexOf('|');
            if (idx < 0) idx = line.IndexOf('	');
            if (idx < 0)
                return null;

            string left = line.Substring(0, idx).Trim().Trim('"');
            if (!string.Equals(left, key, StringComparison.OrdinalIgnoreCase))
                return null;

            string value = line.Substring(idx + 1).Trim();
            while (value.EndsWith(",", StringComparison.Ordinal))
                value = value.Substring(0, value.Length - 1).Trim();
            return value.Trim().Trim('"');
        }


        private string SanitizeName(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "Player";
            value = value.Replace("|", "").Replace("\r", "").Replace("\n", "").Trim();
            if (value.Length > 24)
                value = value.Substring(0, 24);
            return value.Length == 0 ? "Player" : value;
        }

        private static string B64(string s)
        {
            if (s == null) s = string.Empty;
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(s));
        }

        private static string FromB64(string s)
        {
            try { return Encoding.UTF8.GetString(Convert.FromBase64String(s)); }
            catch { return s ?? string.Empty; }
        }

        private void EnsureStyles()
        {
            if (msgStyle != null)
                return;

            Color32 paper = new Color32(239, 229, 194, 255);
            Color32 card = new Color32(239, 229, 194, 255);
            Color32 button = new Color32(239, 229, 194, 255);
            Color32 buttonHover = new Color32(247, 239, 210, 255);
            Color32 buttonActive = new Color32(220, 210, 178, 255);
            Color32 dark = new Color32(44, 30, 25, 255);
            Color32 cream = new Color32(255, 248, 220, 255);
            Color32 text = new Color32(53, 34, 34, 255);
            Color32 faint = new Color32(86, 63, 56, 255);
            Font nativeGuiFont = FindNativeImGuiFont();

            paperTex = MakeNativeDoubleBorderTex(paper, dark, paper, 2, 3, 2);
            cardTex = MakeBorderedTex(card, dark, 2);
            buttonTex = MakeBorderedTex(button, dark, 2);
            buttonHoverTex = MakeBorderedTex(buttonHover, dark, 2);
            buttonActiveTex = MakeBorderedTex(buttonActive, dark, 2);
            darkTex = MakeSolidTex(dark);
            textFieldTex = MakeBorderedTex(cream, dark, 2);

            RebuildInactiveThemeTextures();

            windowStyle = new GUIStyle(GUI.skin.window)
            {
                padding = new RectOffset(16, 16, 18, 16),
                margin = new RectOffset(0, 0, 0, 0),
                border = new RectOffset(8, 8, 8, 8),
                overflow = new RectOffset(0, 0, 0, 0)
            };
            SetAllStyleStates(windowStyle, paperTex, text);

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
                padding = new RectOffset(4, 4, 2, 2)
            };
            titleStyle.normal.textColor = text;

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false,
                padding = new RectOffset(4, 4, 2, 2)
            };
            headerStyle.normal.textColor = text;

            msgStyle = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true,
                fontSize = 15,
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(3, 3, 1, 1)
            };
            msgStyle.normal.textColor = text;
            msgStyle.richText = true;

            inlineMsgStyle = new GUIStyle(msgStyle)
            {
                wordWrap = false
            };

            faintStyle = new GUIStyle(GUI.skin.label)
            {
                wordWrap = false,
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(2, 2, 1, 1)
            };
            faintStyle.normal.textColor = faint;
            faintStyle.richText = true;

            inputStyle = new GUIStyle(GUI.skin.textArea)
            {
                wordWrap = true,
                stretchWidth = false,
                stretchHeight = false,
                fontSize = 15,
                border = new RectOffset(2, 2, 2, 2),
                padding = new RectOffset(7, 7, 5, 5),
                alignment = TextAnchor.UpperLeft
            };
            SetAllStyleStates(inputStyle, textFieldTex, text);

            smallButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                border = new RectOffset(2, 2, 2, 2),
                padding = new RectOffset(8, 8, 5, 5),
                margin = new RectOffset(2, 2, 2, 2)
            };
            SetAllStyleStates(smallButtonStyle, buttonTex, text);
            smallButtonStyle.hover.background = buttonHoverTex;
            smallButtonStyle.focused.background = buttonHoverTex;
            smallButtonStyle.active.background = buttonActiveTex;
            smallButtonStyle.onHover.background = buttonHoverTex;
            smallButtonStyle.onFocused.background = buttonHoverTex;
            smallButtonStyle.onActive.background = buttonActiveTex;

            tabButtonStyle = new GUIStyle(smallButtonStyle)
            {
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(8, 8, 4, 4)
            };

            tabButtonActiveStyle = new GUIStyle(tabButtonStyle);
            SetAllStyleStates(tabButtonActiveStyle, buttonActiveTex, text);
            tabButtonActiveStyle.hover.background = buttonActiveTex;
            tabButtonActiveStyle.focused.background = buttonActiveTex;
            tabButtonActiveStyle.active.background = buttonActiveTex;

            cardStyle = new GUIStyle(GUI.skin.box)
            {
                border = new RectOffset(2, 2, 2, 2),
                padding = new RectOffset(10, 10, 8, 9),
                margin = new RectOffset(0, 0, 4, 5)
            };
            SetAllStyleStates(cardStyle, cardTex, text);

            resizeStyle = new GUIStyle(faintStyle)
            {
                alignment = TextAnchor.LowerRight,
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };

            ApplyNativeFont(nativeGuiFont, windowStyle, titleStyle, headerStyle, msgStyle, inlineMsgStyle, faintStyle, inputStyle, smallButtonStyle, tabButtonStyle, tabButtonActiveStyle, cardStyle, resizeStyle);
        }

        private static void ApplyNativeFont(Font font, params GUIStyle[] styles)
        {
            if (font == null || styles == null)
                return;

            for (int i = 0; i < styles.Length; i++)
            {
                if (styles[i] != null)
                    styles[i].font = font;
            }
        }

        private static Font FindNativeImGuiFont()
        {
            try
            {
                TMP_Text[] texts = Resources.FindObjectsOfTypeAll<TMP_Text>();
                if (texts != null)
                {
                    for (int i = 0; i < texts.Length; i++)
                    {
                        Font f = ExtractSourceFont(texts[i] != null ? texts[i].font : null);
                        if (f != null) return f;
                    }
                }
            }
            catch { }

            try
            {
                TMP_FontAsset[] assets = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                if (assets != null)
                {
                    for (int i = 0; i < assets.Length; i++)
                    {
                        Font f = ExtractSourceFont(assets[i]);
                        if (f != null) return f;
                    }
                }
            }
            catch { }

            return null;
        }

        private static Font ExtractSourceFont(TMP_FontAsset asset)
        {
            if (asset == null)
                return null;

            try
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                PropertyInfo prop = asset.GetType().GetProperty("sourceFontFile", flags);
                if (prop != null)
                {
                    Font f = prop.GetValue(asset, null) as Font;
                    if (f != null) return f;
                }

                FieldInfo field = asset.GetType().GetField("m_SourceFontFile", flags) ?? asset.GetType().GetField("sourceFontFile", flags);
                if (field != null)
                {
                    Font f = field.GetValue(asset) as Font;
                    if (f != null) return f;
                }
            }
            catch { }

            return null;
        }

        private void ApplyThemeForFocus(bool isFocused)
        {
            if (msgStyle == null)
                return;

            float inactiveAlpha = Mathf.Clamp01(inactiveOpacity != null ? inactiveOpacity.Value : 0.10f);
            if (!isFocused && (inactivePaperTex == null || Mathf.Abs(stylesAppliedInactiveOpacity - inactiveAlpha) > 0.001f))
                RebuildInactiveThemeTextures();

            if (stylesAppliedFocused == isFocused && (isFocused || Mathf.Abs(stylesAppliedInactiveOpacity - inactiveAlpha) <= 0.001f))
                return;

            Color32 text = new Color32(53, 34, 34, 255);
            Color32 faint = new Color32(86, 63, 56, 255);

            Texture2D usePaper = isFocused ? paperTex : inactivePaperTex;
            Texture2D useCard = isFocused ? cardTex : inactiveCardTex;
            Texture2D useButton = isFocused ? buttonTex : inactiveButtonTex;
            Texture2D useButtonHover = isFocused ? buttonHoverTex : inactiveButtonHoverTex;
            Texture2D useButtonActive = isFocused ? buttonActiveTex : inactiveButtonActiveTex;
            Texture2D useInput = isFocused ? textFieldTex : inactiveTextFieldTex;

            SetAllStyleStates(windowStyle, usePaper, text);
            SetAllStyleStates(cardStyle, useCard, text);
            SetAllStyleStates(inputStyle, useInput, text);
            SetAllStyleStates(smallButtonStyle, useButton, text);
            smallButtonStyle.hover.background = useButtonHover;
            smallButtonStyle.focused.background = useButtonHover;
            smallButtonStyle.active.background = useButtonActive;
            smallButtonStyle.onHover.background = useButtonHover;
            smallButtonStyle.onFocused.background = useButtonHover;
            smallButtonStyle.onActive.background = useButtonActive;

            SetAllStyleStates(tabButtonStyle, useButton, text);
            tabButtonStyle.hover.background = useButtonHover;
            tabButtonStyle.focused.background = useButtonHover;
            tabButtonStyle.active.background = useButtonActive;
            tabButtonStyle.onHover.background = useButtonHover;
            tabButtonStyle.onFocused.background = useButtonHover;
            tabButtonStyle.onActive.background = useButtonActive;

            SetAllStyleStates(tabButtonActiveStyle, useButtonActive, text);
            tabButtonActiveStyle.hover.background = useButtonActive;
            tabButtonActiveStyle.focused.background = useButtonActive;
            tabButtonActiveStyle.active.background = useButtonActive;

            titleStyle.normal.textColor = text;
            headerStyle.normal.textColor = text;
            msgStyle.normal.textColor = text;
            if (inlineMsgStyle != null) inlineMsgStyle.normal.textColor = text;
            faintStyle.normal.textColor = faint;
            faintStyle.richText = true;
            resizeStyle.normal.textColor = faint;

            stylesAppliedFocused = isFocused;
        }

        private void RebuildInactiveThemeTextures()
        {
            float a = Mathf.Clamp01(inactiveOpacity != null ? inactiveOpacity.Value : 0.10f);

            Color32 paper = new Color32(239, 229, 194, 255);
            Color32 card = new Color32(239, 229, 194, 255);
            Color32 button = new Color32(239, 229, 194, 255);
            Color32 buttonHover = new Color32(247, 239, 210, 255);
            Color32 buttonActive = new Color32(220, 210, 178, 255);
            Color32 dark = new Color32(44, 30, 25, 255);
            Color32 cream = new Color32(255, 248, 220, 255);

            inactivePaperTex = MakeNativeDoubleBorderTex(WithAlpha(paper, a), WithAlpha(dark, a), WithAlpha(paper, a), 2, 3, 2);
            inactiveCardTex = MakeBorderedTex(WithAlpha(card, a), WithAlpha(dark, a), 2);
            inactiveButtonTex = MakeBorderedTex(WithAlpha(button, a), WithAlpha(dark, a), 2);
            inactiveButtonHoverTex = MakeBorderedTex(WithAlpha(buttonHover, a), WithAlpha(dark, a), 2);
            inactiveButtonActiveTex = MakeBorderedTex(WithAlpha(buttonActive, a), WithAlpha(dark, a), 2);
            inactiveDarkTex = MakeSolidTex(WithAlpha(dark, a));
            inactiveTextFieldTex = MakeBorderedTex(WithAlpha(cream, a), WithAlpha(dark, a), 2);
            stylesAppliedInactiveOpacity = a;
            stylesAppliedFocused = true;
        }

        private Color WithAlpha(Color32 color, float alpha)
        {
            return new Color(color.r / 255f, color.g / 255f, color.b / 255f, Mathf.Clamp01(alpha));
        }

        private Texture2D MakeSolidTex(Color color)
        {
            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.SetPixel(0, 0, color);
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            return tex;
        }

        private Texture2D MakeNativeDoubleBorderTex(Color fill, Color darkBorder, Color gap, int outerThickness, int gapThickness, int innerThickness)
        {
            Texture2D tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;

            int outerEnd = Mathf.Max(0, outerThickness);
            int gapEnd = outerEnd + Mathf.Max(0, gapThickness);
            int innerEnd = gapEnd + Mathf.Max(0, innerThickness);

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    int edge = Math.Min(Math.Min(x, y), Math.Min(31 - x, 31 - y));
                    Color c = fill;
                    if (edge < outerEnd)
                        c = darkBorder;
                    else if (edge < gapEnd)
                        c = gap;
                    else if (edge < innerEnd)
                        c = darkBorder;
                    tex.SetPixel(x, y, c);
                }
            }

            tex.filterMode = FilterMode.Point;
            tex.Apply();
            return tex;
        }

        private Texture2D MakeBorderedTex(Color fill, Color border, int thickness)
        {
            Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    bool isBorder = x < thickness || y < thickness || x >= 16 - thickness || y >= 16 - thickness;
                    tex.SetPixel(x, y, isBorder ? border : fill);
                }
            }
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            return tex;
        }

        private void SetAllStyleStates(GUIStyle style, Texture2D background, Color textColor)
        {
            SetState(style.normal, background, textColor);
            SetState(style.hover, background, textColor);
            SetState(style.active, background, textColor);
            SetState(style.focused, background, textColor);
            SetState(style.onNormal, background, textColor);
            SetState(style.onHover, background, textColor);
            SetState(style.onActive, background, textColor);
            SetState(style.onFocused, background, textColor);
        }

        private void SetState(GUIStyleState state, Texture2D background, Color textColor)
        {
            state.background = background;
            state.textColor = textColor;
        }

        private void DrawWindowBacking(Rect rect)
        {
            // The window style itself now draws the native double-line frame.
            // Avoid an extra dark slab behind the chat window because it made the
            // title area look like a broken black title bar.
        }

        private void ClampWindowRect()
        {
            float screenW = Screen.width > 0 ? Screen.width : MaxWidth;
            float screenH = Screen.height > 0 ? Screen.height : MaxHeight;

            if (minimized)
            {
                float maxMiniW = Mathf.Min(MaxWidth, screenW);
                float minMiniW = Mathf.Min(MinimizedMinWidth, maxMiniW);
                windowRect.width = Mathf.Clamp(windowRect.width, minMiniW, maxMiniW);
                windowRect.height = MinimizedHeight;
            }
            else
            {
                windowRect.width = Mathf.Clamp(windowRect.width, MinWidth, Mathf.Min(MaxWidth, screenW));
                windowRect.height = Mathf.Clamp(windowRect.height, MinHeight, Mathf.Min(MaxHeight, screenH));
            }

            windowRect.x = Mathf.Clamp(windowRect.x, 0f, Mathf.Max(0f, screenW - windowRect.width));
            windowRect.y = Mathf.Clamp(windowRect.y, 0f, Mathf.Max(0f, screenH - windowRect.height));
        }

        private void SaveWindowConfig(bool force)
        {
            if (config == null || windowX == null)
                return;

            Rect saveRect = windowRect;
            if (minimized && HasUsableExpandedWindowRect())
                saveRect = lastExpandedWindowRect;

            if (!force && RectClose(saveRect, lastSavedRect))
                return;

            windowX.Value = saveRect.x;
            windowY.Value = saveRect.y;
            windowWidth.Value = saveRect.width;
            windowHeight.Value = saveRect.height;
            config.Save();
            PruneSocialUserConfig(config.ConfigFilePath);
            lastSavedRect = saveRect;
            lastRectSaveTime = Time.unscaledTime;
        }

        private bool RectClose(Rect a, Rect b)
        {
            return Mathf.Abs(a.x - b.x) < 1f && Mathf.Abs(a.y - b.y) < 1f && Mathf.Abs(a.width - b.width) < 1f && Mathf.Abs(a.height - b.height) < 1f;
        }

        private void Log(string msg)
        {
            if (debugLogging != null && debugLogging.Value)
                Debug.Log("[MMOnsterpatch AIO Patcher] " + msg);
        }

        private sealed class ChatIconEntry
        {
            public string Group;
            public int SortOrder;
            public string Token;
            public string SpriteName;
            public string FallbackLabel;
            public Sprite Sprite;

            public bool IsUsable
            {
                get { return Sprite != null && Sprite.texture != null; }
            }
        }

        private sealed class ChatMessage
        {
            public DateTime Time;
            public string Channel;
            public string From;
            public string Text;
            public bool IsSystem;

            public string Render()
            {
                if (IsSystem)
                    return "[" + Time.ToString("HH:mm") + "] " + Text;
                return "[" + Time.ToString("HH:mm") + "][" + Channel + "] " + From + ": " + Text;
            }
        }
    }
}

}
