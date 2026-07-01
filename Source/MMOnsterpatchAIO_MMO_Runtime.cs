using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using Goose.Monsterpatch.GTSAllInOnePatcher;

public static class MMOnsterpatchAIOBootstrap
{
    private static bool initialized;
    private static ManualLogSource log;
    private static ConfigFile config;

    public static void Ensure()
    {
        if (initialized)
            return;

        initialized = true;
        try
        {
            log = BepInEx.Logging.Logger.CreateLogSource("MMOnsterpatch AIO");
        }
        catch
        {
            log = new ManualLogSource("MMOnsterpatch AIO");
        }

        try
        {
            log.LogInfo("[MMOnsterpatch AIO] Patcher-driven bootstrap entered. Social remains native patcher host; MMO runtime starts from injected host.");
            string configPath = Path.Combine(Paths.ConfigPath, "goose.monsterpatch.mmonsterpatchaio.cfg");
            config = new ConfigFile(configPath, true);

            var settings = new MMOnsterpatchSettings
            {
            ServerHost = config.Bind("Server", "Host", "mmo.gamingwithgoose.com", "Multiplayer remotePlayer server hostname/IP.").Value,
            ServerPort = config.Bind("Server", "Port", 61528, "Multiplayer remotePlayer server TCP port.").Value,
            AutoConnectOnStartup = config.Bind("Server", "AutoConnectOnStartup", false, "When false, MMOnsterpatch starts offline and only connects from the AIO chat window.").Value,            ClusterId = config.Bind("Server", "ClusterId", "1", "Only players in the same cluster see each other.").Value,
            PlayerName = config.Bind("Player", "PlayerName", "GOOSE", "Displayed player name sent to the server.").Value,
            PlayerId = config.Bind("Player", "PlayerId", "", "Optional stable player ID. Leave blank for a unique runtime ID.").Value,
            RemotePlayerSpriteName = config.Bind("Visual", "RemotePlayerSpriteName", "boxColors_2", "Sprite name to use for remote player remote player marker.").Value,
            RemotePlayerScale = config.Bind("Visual", "RemotePlayerScale", 1.0f, "Manual scale. Used for icon fallback, or for player visuals if UseGameDefaultVisualScale is false.").Value,
            UsePlayerSprite = config.Bind("Visual", "UsePlayerSprite", true, "Clone the local player visual and apply remote design/colors instead of using the icon fallback.").Value,
            UseGameDefaultVisualScale = config.Bind("Visual", "UseGameDefaultVisualScale", true, "For player visuals, keep the game clone/default scale instead of applying RemotePlayerScale.").Value,
            ShowNameplates = config.Bind("Nameplates", "ShowNameplates", true, "Show remote player names above remote players.").Value,
            NameplateOffsetY = config.Bind("Nameplates", "NameplateOffsetY", 0.2f, "Vertical world offset for the nameplate above the remotePlayer.").Value,
            NameplateCharacterSize = config.Bind("Nameplates", "NameplateCharacterSize", 0.012f, "TextMesh character size for nameplates.").Value,
            NameplateFontSize = config.Bind("Nameplates", "NameplateFontSize", 32, "Font size for nameplates.").Value,
            NameplateFontColorHex = config.Bind("Nameplates", "NameplateFontColorHex", "#FFFFFF", "Nameplate font color. Supports #RRGGBB or #RRGGBBAA.").Value,
            NameplateShadowColorHex = config.Bind("Nameplates", "NameplateShadowColorHex", "#000000CC", "Nameplate shadow color. Supports #RRGGBB or #RRGGBBAA.").Value,
            ShowNameplateBackground = config.Bind("Nameplates", "ShowNameplateBackground", true, "Show a rounded rectangle behind the nameplate.").Value,
            NameplateBackgroundColorHex = config.Bind("Nameplates", "NameplateBackgroundColorHex", "#00000099", "Rounded rectangle background color. Supports #RRGGBB or #RRGGBBAA.").Value,
            NameplateBackgroundPaddingX = config.Bind("Nameplates", "NameplateBackgroundPaddingX", 0.018f, "Horizontal padding around the nameplate text.").Value,
            NameplateBackgroundPaddingY = config.Bind("Nameplates", "NameplateBackgroundPaddingY", 0.007f, "Vertical padding around the nameplate text.").Value,
            NameplateBackgroundMinWidth = config.Bind("Nameplates", "NameplateBackgroundMinWidth", 0.11f, "Minimum rounded rectangle background width when autosizing is enabled.").Value,
            NameplateBackgroundAutoSize = config.Bind("Nameplates", "NameplateBackgroundAutoSize", true, "Automatically resize the rounded rectangle background to match the player name length. False uses fixed size.").Value,
            NameplateBackgroundFixedWidth = config.Bind("Nameplates", "NameplateBackgroundFixedWidth", 0.16f, "Fixed rounded rectangle background width when autosizing is disabled.").Value,
            NameplateBackgroundFixedHeight = config.Bind("Nameplates", "NameplateBackgroundFixedHeight", 0.04f, "Fixed rounded rectangle background height when autosizing is disabled.").Value,
            InterpolationSpeed = config.Bind("Movement", "InterpolationSpeed", 28f, "How quickly remote remotePlayers interpolate toward their latest server position. Higher is tighter, lower is smoother.").Value,
            SnapDistance = config.Bind("Movement", "SnapDistance", 1.25f, "If a remote remotePlayer is farther than this from the latest position, snap instead of smoothing. Good for map changes/teleports.").Value,
            RemoteMovingDistanceThreshold = config.Bind("Movement", "RemoteMovingDistanceThreshold", 0.015f, "Distance-to-target threshold used to keep walking animation active even if the remote moving flag misses a frame.").Value,
            AnimationRefreshSeconds = config.Bind("Movement", "AnimationRefreshSeconds", 0.20f, "How often to re-assert remote moving/idle animation state.").Value,
            WalkCycleSeconds = config.Bind("Movement", "WalkCycleSeconds", 0.8f, "Fallback walking animation cycle time. Step packets normally drive animation progress directly.").Value,
            UseStepMovementPackets = false,
            UseMoveCoroutinePatch = false,
            StepDistance = 0.2f,
            StepDurationSeconds = 0.8f,
            StepSendLeadFactor = 0.82f,
            StepOriginSnapDistance = 0.35f,
            BattleRequestsEnabled = config.Bind("Battles", "BattleRequestsEnabled", true, "Allow pressing Submit near/facing a remote player to send a PvP battle request.").Value,
            BattleRequestDistance = config.Bind("Battles", "BattleRequestDistance", 0.45f, "Maximum distance from a remote player to send a battle request.").Value,
            BattleRequestForwardDot = config.Bind("Battles", "BattleRequestForwardDot", 0.35f, "How closely the remote player must be in front of you. Lower is more forgiving.").Value,
            BattleRequestTimeoutSeconds = config.Bind("Battles", "BattleRequestTimeoutSeconds", 20f, "How long incoming battle requests stay pending.").Value,
            BattleOverlayCharacterSize = config.Bind("Battles", "BattleOverlayCharacterSize", 0.055f, "World TextMesh character size for older battle toast popups.").Value,
            BattleRequestWindowScale = config.Bind("Battles", "BattleRequestWindowScale", 1.0f, "Scale for the centered Accept / Decline battle request window.").Value,
            DisableRunInPvP = config.Bind("Battles", "DisableRunInPvP", true, "Disable the Run option during RemotePlayer PvP battles.").Value,
            RealBattlesEnabled = config.Bind("Battles", "RealBattlesEnabled", true, "Use VMS-style real PvP battles: local player commands are sent to the other client and enemy turns wait for remote commands.").Value,
            BattleCommandTimeoutSeconds = config.Bind("Battles", "BattleCommandTimeoutSeconds", 60f, "How long an enemy-side PvP turn waits for the remote player's command before falling back to a local available move.").Value,
            BattleStateSyncEnabled = config.Bind("Battles", "BattleStateSyncEnabled", true, "Sync HP, shields, buffs, debuffs, cooldown-like fields, and other battle state as a safety net.").Value,
            BattleStateSendRateSeconds = config.Bind("Battles", "BattleStateSendRateSeconds", 0.35f, "How often to send local team battle-state reports during PvP.").Value,
            BattleHitTimeoutSeconds = config.Bind("Battles", "BattleHitTimeoutSeconds", 12f, "How long the receiving side waits for authoritative per-hit packets before ending the remote turn.").Value,
            UseOpponentSpriteInBattleSplash = config.Bind("Battles", "UseOpponentSpriteInBattleSplash", true, "Use the remote player's current overworld sprite in the trainer battle splash.").Value,
            ForceLocalFirstTurn = false,
            WorldSpawnsEnabled = config.Bind("MMO World Spawns", "Enabled", true, "Render and interact with server-owned visible overworld spawns sent by the AIO/MMO server. Spawn rates are controlled only by the server.").Value,
            BlockVanillaEncountersWhileConnected = config.Bind("MMO World Spawns", "BlockVanillaEncountersWhileConnected", false, "Legacy flag. Random grass/cave battles are allowed online; only vanilla visible spawnZone rolls are suppressed because the server owns those spawns.").Value,
            WorldSpawnInteractDistance = config.Bind("MMO World Spawns", "InteractDistance", 0.55f, "Maximum distance from the local player to press Submit/Interact on a server-owned world spawn.").Value,
            WorldSpawnScale = config.Bind("MMO World Spawns", "Scale", 1.00f, "Fallback-only scale for server-owned placeholder sprites. Real server spawns use the vanilla monPrefab scale.").Value,
            WorldSpawnOffsetX = config.Bind("MMO World Spawns", "OffsetX", 0.0f, "Fallback-only horizontal render offset for placeholder server spawns. Real server spawns use vanilla placement.").Value,
            WorldSpawnOffsetY = config.Bind("MMO World Spawns", "OffsetY", 0.0f, "Fallback-only vertical render offset for placeholder server spawns. Real server spawns use vanilla placement.").Value,
            WorldSpawnFrameSeconds = config.Bind("MMO World Spawns", "FrameSeconds", 0.35f, "Seconds per overworld animation frame for server-owned spawn sprites.").Value,
            ShowWorldSpawnLabels = config.Bind("MMO World Spawns", "ShowLabels", false, "Show a tiny label over server-owned spawn sprites.").Value,
            WorldSpawnLabelOffsetY = config.Bind("MMO World Spawns", "LabelOffsetY", 0.22f, "Vertical offset for server spawn labels.").Value,
            WorldSpawnLabelCharacterSize = config.Bind("MMO World Spawns", "LabelCharacterSize", 0.010f, "TextMesh character size for server spawn labels.").Value,
            SendRateSeconds = config.Bind("Network", "SendRateSeconds", 0.033f, "How often to send local real-position snapshots. 0.033 is about 30 updates/sec like the VMS server tick.").Value,
            SaveFilePath = config.Bind("Save", "SaveFilePath", "", "Optional path to slot0.json. Used only to pull player name/design/colors.").Value,
            ReadAppearanceFromSave = config.Bind("Save", "ReadAppearanceFromSave", true, "Read playerName/playerDesign/playerColor1/playerColor2 from configured save file.").Value,
            SystemMessagePopupWidth = config.Bind("System Message Popup", "PopupWidth", 560f, "Native system message popup width before the root scale is applied.").Value,
            SystemMessagePopupHeight = config.Bind("System Message Popup", "PopupHeight", 180f, "Native system message popup height before the root scale is applied.").Value,
            SystemMessagePopupScale = config.Bind("System Message Popup", "PopupScale", 0.32f, "Native system message popup root scale.").Value,
            SystemMessageTitleOffsetX = config.Bind("System Message Popup", "TitleOffsetX", 0f, "Horizontal title offset.").Value,
            SystemMessageTitleOffsetY = config.Bind("System Message Popup", "TitleOffsetY", 55f, "Vertical title offset.").Value,
            SystemMessageTitleWidth = config.Bind("System Message Popup", "TitleWidth", 500f, "Title text box width.").Value,
            SystemMessageTitleHeight = config.Bind("System Message Popup", "TitleHeight", 44f, "Title text box height.").Value,
            SystemMessageTitleFontSize = config.Bind("System Message Popup", "TitleFontSize", 32, "Title font size.").Value,
            SystemMessageBodyOffsetX = config.Bind("System Message Popup", "BodyOffsetX", 0f, "Horizontal body text offset.").Value,
            SystemMessageBodyOffsetY = config.Bind("System Message Popup", "BodyOffsetY", -10f, "Vertical body text offset.").Value,
            SystemMessageBodyWidth = config.Bind("System Message Popup", "BodyWidth", 430f, "Body text box width.").Value,
            SystemMessageBodyHeight = config.Bind("System Message Popup", "BodyHeight", 32f, "Body text box height.").Value,
            SystemMessageBodyFontSize = config.Bind("System Message Popup", "BodyFontSize", 22, "Body text font size.").Value,
            SystemMessageBirbOffsetX = config.Bind("System Message Popup", "BirbOffsetX", -50f, "Horizontal Birb icon offset from the lower-right anchor.").Value,
            SystemMessageBirbOffsetY = config.Bind("System Message Popup", "BirbOffsetY", 76f, "Vertical Birb icon offset from the lower-right anchor.").Value,
            SystemMessageBirbSize = config.Bind("System Message Popup", "BirbSize", 120f, "Birb icon size. 120 is 2.5x the original 48 size.").Value,
            SystemMessageButtonOffsetX = config.Bind("System Message Popup", "ButtonOffsetX", 0f, "Horizontal OK button offset.").Value,
            SystemMessageButtonOffsetY = config.Bind("System Message Popup", "ButtonOffsetY", -4f, "Vertical OK button offset.").Value,
            SystemMessageButtonWidth = config.Bind("System Message Popup", "ButtonWidth", 134f, "OK button body width.").Value,
            SystemMessageButtonHeight = config.Bind("System Message Popup", "ButtonHeight", 24f, "OK button body height.").Value
        };

            try
            {
                config.Save();
                PruneMmoUserConfig(config.ConfigFilePath);
                log.LogInfo("[MMOnsterpatch AIO] MMO config loaded/saved from " + config.ConfigFilePath);
            }
            catch (Exception ex)
            {
                log.LogWarning("[MMOnsterpatch AIO] MMO config.Save failed: " + ex.Message);
            }

            MMOnsterpatchRunner.Ensure(settings, log, Paths.BepInExRootPath);
            MMOnsterpatchMovementHarmony.Init(log);
            log.LogInfo("[MMOnsterpatch AIO] MMO runtime ready. FollowerPacketTest v0.1: remote followers use MPFOL monID/shiny/frame payload through the existing v0.1 server relay.");
        }
        catch (Exception ex)
        {
            try { log.LogError("[MMOnsterpatch AIO] Bootstrap failed: " + ex); } catch { }
        }
    }

    public static bool IsMMOConnected()
    {
        try { return MMOnsterpatchRunner.Current != null && MMOnsterpatchRunner.Current.IsConnectedForAio; } catch { return false; }
    }

    // Optional public API for separate patchers such as Monsterpatch_ExtraOptions_Patcher.
    // Extra Options remains fully standalone: if this AIO patcher is not installed or not connected,
    // its local/offline settings continue to work normally. While connected, the AIO owns the online
    // gameplay-rate policy so client-side shiny/EXP multipliers cannot affect the server session.
    public static bool ShouldUseOnlineExtraOptionRules()
    {
        try
        {
            return MMOnsterpatchRunner.Current != null
                && MMOnsterpatchRunner.Current.IsConnectedForAio
                && MMOnsterpatchRunner.Current.HasServerHandshakeForAio;
        }
        catch { return false; }
    }

    public static int GetOnlineShinyOddsDenominator()
    {
        // Server-owned denominator-style online shiny odds. 1000 = 1/1000, 1 = guaranteed.
        try
        {
            if (OfficialServerWorldRatesRuntime.IsOnlineRatesActive)
                return Math.Max(1, OfficialServerWorldRatesRuntime.ShinyOddsDenominator);
        }
        catch { }
        return 1000;
    }

    public static float GetOnlineExpGlobalMultiplier()
    {
        // Online EXP is server/AIO controlled and defaults to vanilla 1x.
        return 1.0f;
    }

    public static float GetOnlineBaseExpScale()
    {
        return 0.3f;
    }

    public static float GetOnlineWildBattleMultiplier()
    {
        return 1.0f;
    }

    public static float GetOnlineWizardBattleMultiplier()
    {
        return 1.35f;
    }

    public static bool IsMMOWorldModeActive()
    {
        try { return MMOnsterpatchRunner.Current != null && MMOnsterpatchRunner.Current.IsMmoWorldModeActiveForAio; } catch { return false; }
    }

    public static bool IsMMOBusy()
    {
        try { return MMOnsterpatchRunner.Current != null && MMOnsterpatchRunner.Current.IsNetworkBusyForAio; } catch { return false; }
    }

    public static void ConnectMMO()
    {
        Ensure();
        try
        {
            if (MMOnsterpatchRunner.Current != null)
                MMOnsterpatchRunner.Current.ConnectFromAioChatWindow();
        }
        catch (Exception ex)
        {
            try { log.LogWarning("[MMOnsterpatch AIO] ConnectMMO failed: " + ex.Message); } catch { }
        }
    }

    public static void DisconnectMMO()
    {
        try
        {
            if (MMOnsterpatchRunner.Current != null)
                MMOnsterpatchRunner.Current.DisconnectFromAioChatWindow();
        }
        catch (Exception ex)
        {
            try { log.LogWarning("[MMOnsterpatch AIO] DisconnectMMO failed: " + ex.Message); } catch { }
        }
    }

    private static void PruneMmoUserConfig(string path)
    {
        AIOConfigPruner.Prune(path, AIOVisibleConfigKeys.MmoSocialUserConfigKeys());
    }
}

public static class MMOnsterpatchMovementHarmony
{
    private static Harmony harmony;
    private static ManualLogSource log;

    public static void Init(ManualLogSource logger)
    {
        log = logger;
        if (harmony != null)
            return;

        try
        {
            harmony = new Harmony("goose.monsterpatch.mmonsterpatch.movehook");
            harmony.PatchAll();
            if (log != null)
                log.LogInfo("[MMOnsterpatch] PlayerController.Move hook installed for exact STEP packets.");
        }
        catch (Exception ex)
        {
            if (log != null)
                log.LogWarning("[MMOnsterpatch] Failed to install PlayerController.Move hook: " + ex.Message);
        }
    }
}

[HarmonyPatch(typeof(PlayerController), "Move", new Type[] { typeof(Vector3) })]
public static class MMOnsterpatch_PlayerController_Move_Patch
{
    public static void Prefix(PlayerController __instance, Vector3 direction)
    {
        MMOnsterpatchRunner runner = MMOnsterpatchRunner.Current;
        if (runner != null)
            runner.SendExactLocalMoveStep(__instance, direction);
    }
}


public static class MMOnsterpatchPvPState
{
    public static bool Active;
    public static bool DisableRun;
    public static bool RealBattlesEnabled;
    public static float BattleCommandTimeoutSeconds;
    public static float BattleHitTimeoutSeconds;
    public static bool ForceLocalFirstTurn;
    public static bool FirstTurnPending;
    public static string OpponentId = "";
    public static string OpponentName = "";
    public static string BattleId = "";
    public static Sprite OpponentBattleSprite;
    private static GameScript gameScript;
    private static readonly Dictionary<int, int> originalExpByUniqueId = new Dictionary<int, int>();

    public static void Begin(GameScript gs, string opponentId, string opponentName, string battleId, bool disableRun, bool realBattlesEnabled, float commandTimeoutSeconds, float hitTimeoutSeconds)
    {
        Active = true;
        DisableRun = disableRun;
        RealBattlesEnabled = realBattlesEnabled;
        BattleCommandTimeoutSeconds = Mathf.Max(5f, commandTimeoutSeconds);
        BattleHitTimeoutSeconds = Mathf.Max(2f, hitTimeoutSeconds);
        ForceLocalFirstTurn = false;
        FirstTurnPending = false;
        OpponentId = opponentId ?? "";
        OpponentName = opponentName ?? "";
        BattleId = battleId ?? "";
        gameScript = gs;
        originalExpByUniqueId.Clear();

        try
        {
            if (gs != null && gs.teamMon != null)
            {
                for (int i = 0; i < gs.teamMon.Length; i++)
                {
                    Mon m = gs.teamMon[i];
                    if (m != null)
                        originalExpByUniqueId[m.uniqueID] = m.curExp;
                }
            }
        }
        catch { }
    }

    public static void ClearPvPEncounterResidue()
    {
        try
        {
            if (gameScript == null)
                return;

            try { gameScript.ClearLastEncounterMons(); } catch { }

            try
            {
                if (gameScript.lastEncounterMons != null)
                {
                    for (int i = 0; i < gameScript.lastEncounterMons.Length; i++)
                        gameScript.lastEncounterMons[i] = null;
                }
            }
            catch { }

            try { gameScript.spawnedEncounterCrystal = false; } catch { }

            try
            {
                if (gameScript.encounterCrystalObj != null)
                    gameScript.encounterCrystalObj.SetActive(false);
            }
            catch { }

            try { SetStaticOrInstanceField(gameScript, "lastEncounterWasTrainer", false); } catch { }
            try { GameScript.aboutToBattleEnemyTrainer = false; } catch { }
            try { GameScript.battlingEnemyTrainer = false; } catch { }
        }
        catch { }
    }

    public static void EndAndRestoreExp()
    {
        try
        {
            if (gameScript != null && gameScript.teamMon != null)
            {
                for (int i = 0; i < gameScript.teamMon.Length; i++)
                {
                    Mon m = gameScript.teamMon[i];
                    if (m != null && originalExpByUniqueId.ContainsKey(m.uniqueID))
                    {
                        m.curExp = originalExpByUniqueId[m.uniqueID];
                        m.curLevel = gameScript.GetLevelFromTotalExp(m.curExp);
                        try { m.RefreshStatsWithLevelAndStuff(false); } catch { }
                    }
                }
            }
        }
        catch { }

        ClearPvPEncounterResidue();
        RestoreRunButton();

        Active = false;
        DisableRun = false;
        RealBattlesEnabled = false;
        BattleCommandTimeoutSeconds = 60f;
        BattleHitTimeoutSeconds = 12f;
        ForceLocalFirstTurn = false;
        FirstTurnPending = false;
        OpponentId = "";
        OpponentName = "";
        BattleId = "";
        OpponentBattleSprite = null;
        MMOnsterpatchRealPvP.ClearQueuedCommands();
        gameScript = null;
        originalExpByUniqueId.Clear();
    }

    private static void RestoreRunButton()
    {
        try
        {
            BattleSystem bs = UnityEngine.Object.FindObjectOfType<BattleSystem>();
            if (bs != null && bs.bRun != null)
                bs.bRun.SetActive(true);
        }
        catch { }
    }

    private static void SetStaticOrInstanceField(object obj, string name, object value)
    {
        if (obj == null) return;
        FieldInfo f = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        if (f == null) return;
        object target = f.IsStatic ? null : obj;
        f.SetValue(target, value);
    }
}

[HarmonyPatch(typeof(GameScript), "GetExpGain")]
public static class MMOnsterpatch_GameScript_GetExpGain_NoPvPExp_Patch
{
    public static bool Prefix(ref int __result)
    {
        if (MMOnsterpatchPvPState.Active)
        {
            __result = 0;
            return false;
        }

        return true;
    }
}


[HarmonyPatch(typeof(BattleSystem), "Run")]
public static class MMOnsterpatch_BattleSystem_Run_BlockPvPRun_Patch
{
    public static bool Prefix(BattleSystem __instance)
    {
        if (!MMOnsterpatchPvPState.Active || !MMOnsterpatchPvPState.DisableRun)
            return true;

        try
        {
            if (__instance != null)
            {
                MMOnsterpatchBattleUiPatchHelpers.TryPlaySfx(__instance, "uiCancel");

                if (__instance.menuBattleOrRun != null)
                    __instance.menuBattleOrRun.SetActive(true);

                MMOnsterpatchBattleUiPatchHelpers.TrySelectBattleButton(__instance);
            }
        }
        catch { }

        return false;
    }
}

[HarmonyPatch(typeof(BattleSystem), "PlayerTurn")]
public static class MMOnsterpatch_BattleSystem_PlayerTurn_HidePvPRun_Patch
{
    public static void Postfix(BattleSystem __instance)
    {
        MMOnsterpatchBattleUiPatchHelpers.ApplyPvPBattleMenuRules(__instance);
    }
}

[HarmonyPatch(typeof(BattleSystem), "MenuBattleOrRun")]
public static class MMOnsterpatch_BattleSystem_MenuBattleOrRun_HidePvPRun_Patch
{
    public static void Postfix(BattleSystem __instance)
    {
        MMOnsterpatchBattleUiPatchHelpers.ApplyPvPBattleMenuRules(__instance);
    }
}
public static class MMOnsterpatchBattleUiPatchHelpers
{
    public static void ApplyPvPBattleMenuRules(BattleSystem bs)
    {
        if (!MMOnsterpatchPvPState.Active || !MMOnsterpatchPvPState.DisableRun || bs == null)
            return;

        try
        {
            if (bs.bRun != null)
                bs.bRun.SetActive(false);

            TrySelectBattleButton(bs);
        }
        catch { }
    }

    public static void TrySelectBattleButton(BattleSystem bs)
    {
        if (bs == null || bs.bBattle == null)
            return;

        try
        {
            FieldInfo f = typeof(BattleSystem).GetField("eventSystem", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            object eventSystem = f != null ? f.GetValue(bs) : null;
            if (eventSystem == null)
                return;

            MethodInfo m = eventSystem.GetType().GetMethod("SetSelectedGameObject", new Type[] { typeof(GameObject) });
            if (m != null)
                m.Invoke(eventSystem, new object[] { bs.bBattle });
        }
        catch { }
    }

    public static void TryPlaySfx(BattleSystem bs, string sfx)
    {
        if (bs == null || bs.gameScript == null || string.IsNullOrEmpty(sfx))
            return;

        try
        {
            FieldInfo f = typeof(GameScript).GetField("audioSystem", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            object audioSystem = f != null ? f.GetValue(bs.gameScript) : null;
            if (audioSystem == null)
                return;

            MethodInfo m = audioSystem.GetType().GetMethod("PlaySFX", new Type[] { typeof(string) });
            if (m != null)
                m.Invoke(audioSystem, new object[] { sfx });
        }
        catch { }
    }
}


public sealed class MMOnsterpatchBattleCommand
{
    public string fromId;
    public string battleId;
    public int actorSlot;
    public int moveSlot;
    public int targetSlot;
    public bool targetAlly;
    public float receivedAt;
}

public sealed class MMOnsterpatchBattleHit
{
    public string fromId;
    public string battleId;
    public int actorSlot;
    public int moveSlot;
    public string targetSide;
    public int targetSlot;
    public int amount;
    public int hpAfter;
    public int shieldAfter;
    public bool crit;
    public float receivedAt;
}

public sealed class MMOnsterpatchBattleDone
{
    public string fromId;
    public string battleId;
    public int actorSlot;
    public int moveSlot;
    public float receivedAt;
}


[HarmonyPatch(typeof(BattleSystem), "ShowDamageText")]
public static class MMOnsterpatch_BattleSystem_ShowDamageText_PvPHitVisual_Patch
{
    public static void Prefix(BattleSystem __instance, int num, GameObject g, bool crit)
    {
        MMOnsterpatchRealPvP.NoteLocalDamagePopup(__instance, num, g, crit);
    }
}

[HarmonyPatch(typeof(BattleSystem), "ApplyRawDamageToTarget")]
public static class MMOnsterpatch_BattleSystem_ApplyRawDamageToTarget_PvPHit_Patch
{
    public static void Postfix(BattleSystem __instance, MonObject targetMonObj, int damage)
    {
        MMOnsterpatchRealPvP.SendAuthoritativeHitAfterDamage(__instance, targetMonObj, damage);
    }
}

[HarmonyPatch(typeof(BattleSystem), "SelectTarget")]
public static class MMOnsterpatch_BattleSystem_SelectTarget_SendPvPCommand_Patch
{
    public static void Prefix(BattleSystem __instance, int a, ref bool __state)
    {
        __state = MMOnsterpatchRealPvP.ShouldSendLocalCommand(__instance, a, false);
    }

    public static void Postfix(BattleSystem __instance, int a, bool __state)
    {
        if (__state)
            MMOnsterpatchRealPvP.SendLocalCommandFromBattleSelection(__instance, a, false);
    }
}

[HarmonyPatch(typeof(BattleSystem), "SelectTargetAlly")]
public static class MMOnsterpatch_BattleSystem_SelectTargetAlly_SendPvPCommand_Patch
{
    public static void Prefix(BattleSystem __instance, int a, ref bool __state)
    {
        __state = MMOnsterpatchRealPvP.ShouldSendLocalCommand(__instance, a, true);
    }

    public static void Postfix(BattleSystem __instance, int a, bool __state)
    {
        if (__state)
            MMOnsterpatchRealPvP.SendLocalCommandFromBattleSelection(__instance, a, true);
    }
}

[HarmonyPatch(typeof(BattleSystem), "BattleStepStartOfTurn")]
public static class MMOnsterpatch_BattleSystem_BattleStepStartOfTurn_RealPvPEnemyTurn_Patch
{
    public static bool Prefix(BattleSystem __instance, BattleBattler battler, ref IEnumerator __result)
    {
        if (!MMOnsterpatchPvPState.Active || !MMOnsterpatchPvPState.RealBattlesEnabled || __instance == null || battler == null || !battler.isEnemy)
            return true;

        __result = MMOnsterpatchRealPvP.NetworkEnemyTurn(__instance, battler);
        return false;
    }
}

public static class MMOnsterpatchRealPvP
{
    private static readonly object commandLock = new object();
    private static readonly List<MMOnsterpatchBattleCommand> queuedCommands = new List<MMOnsterpatchBattleCommand>();
    private static readonly object hitLock = new object();
    private static readonly List<MMOnsterpatchBattleHit> queuedHits = new List<MMOnsterpatchBattleHit>();
    private static readonly List<MMOnsterpatchBattleDone> queuedDone = new List<MMOnsterpatchBattleDone>();
    private static bool recordingLocalMove;
    private static int recordingActorSlot = -1;
    private static int recordingMoveSlot = -1;
    private static int recordingTargetSlot = -1;
    private static readonly Dictionary<int, bool> pendingCritByTargetId = new Dictionary<int, bool>();

    public static void ClearQueuedCommands()
    {
        lock (commandLock)
        {
            queuedCommands.Clear();
        }
        lock (hitLock)
        {
            queuedHits.Clear();
            queuedDone.Clear();
        }
        recordingLocalMove = false;
        recordingActorSlot = -1;
        recordingMoveSlot = -1;
        recordingTargetSlot = -1;
        pendingCritByTargetId.Clear();
    }

    public static bool ShouldSendLocalCommand(BattleSystem bs, int selectedSlot, bool targetAlly)
    {
        if (!MMOnsterpatchPvPState.Active || !MMOnsterpatchPvPState.RealBattlesEnabled || bs == null || bs.gameScript == null)
            return false;

        try
        {
            if (MenuScript.gameState != MenuScript.GameState.Battle)
                return false;

            int curMon = GetIntField(bs, "curMon", -1);
            int curMove = GetIntField(bs, "curMove", -1);
            if (curMon < 0 || curMon >= 4 || curMove < 0 || curMove >= 4)
                return false;

            Mon actingMon = bs.gameScript.teamMon[curMon];
            if (actingMon == null || actingMon.hp <= 0 || actingMon.moveIDs == null || actingMon.moveIDs[curMove] < 0)
                return false;

            MoveScriptableObject move = bs.gameScript.moveScriptableObject[actingMon.moveIDs[curMove]];
            if (move == null)
                return false;

            bool manualEnemy = (bool)InvokePrivate(bs, "IsManualEnemyTarget", new object[] { move.target }, false);
            bool manualAlly = (bool)InvokePrivate(bs, "IsManualAllyTarget", new object[] { move.target }, false);

            if (targetAlly)
            {
                if (manualAlly)
                {
                    if (selectedSlot < 0 || selectedSlot >= bs.playerWhiteCircle.Length)
                        return false;
                    return bs.playerWhiteCircle[selectedSlot] != null && bs.playerWhiteCircle[selectedSlot].activeSelf;
                }

                return false;
            }

            if (manualEnemy)
            {
                if (selectedSlot < 0 || selectedSlot >= bs.enemyWhiteCircle.Length)
                    return false;
                return bs.enemyWhiteCircle[selectedSlot] != null && bs.enemyWhiteCircle[selectedSlot].activeSelf;
            }

            return !manualAlly;
        }
        catch
        {
            return false;
        }
    }

    public static void SendLocalCommandFromBattleSelection(BattleSystem bs, int selectedSlot, bool targetAlly)
    {
        if (!MMOnsterpatchPvPState.Active || !MMOnsterpatchPvPState.RealBattlesEnabled || bs == null)
            return;

        try
        {
            int actorSlot = GetIntField(bs, "curMon", -1);
            int moveSlot = GetIntField(bs, "curMove", -1);
            int targetSlot = GetIntField(bs, "selectedMonIndex", -1);

            if (targetAlly && targetSlot < 0)
                targetSlot = selectedSlot;

            MMOnsterpatchRunner runner = MMOnsterpatchRunner.Current;
            if (runner != null)
                runner.SendBattleCommand(actorSlot, moveSlot, targetSlot, targetAlly);
        }
        catch { }
    }

    public static void QueueRemoteCommand(string fromId, string battleId, int actorSlot, int moveSlot, int targetSlot, bool targetAlly)
    {
        if (!MMOnsterpatchPvPState.Active || !MMOnsterpatchPvPState.RealBattlesEnabled)
            return;

        if (!string.IsNullOrEmpty(MMOnsterpatchPvPState.BattleId) && battleId != MMOnsterpatchPvPState.BattleId)
            return;

        lock (commandLock)
        {
            queuedCommands.Add(new MMOnsterpatchBattleCommand
            {
                fromId = fromId ?? "",
                battleId = battleId ?? "",
                actorSlot = actorSlot,
                moveSlot = moveSlot,
                targetSlot = targetSlot,
                targetAlly = targetAlly,
                receivedAt = Time.time
            });
        }
    }


    public static void QueueRemoteHit(string fromId, string battleId, int actorSlot, int moveSlot, string targetSide, int targetSlot, int amount, int hpAfter, int shieldAfter, bool crit)
    {
        if (!MMOnsterpatchPvPState.Active || !MMOnsterpatchPvPState.RealBattlesEnabled)
            return;

        if (!string.IsNullOrEmpty(MMOnsterpatchPvPState.BattleId) && battleId != MMOnsterpatchPvPState.BattleId)
            return;

        lock (hitLock)
        {
            queuedHits.Add(new MMOnsterpatchBattleHit
            {
                fromId = fromId ?? "",
                battleId = battleId ?? "",
                actorSlot = actorSlot,
                moveSlot = moveSlot,
                targetSide = targetSide ?? "E",
                targetSlot = targetSlot,
                amount = Mathf.Max(0, amount),
                hpAfter = Mathf.Max(0, hpAfter),
                shieldAfter = Mathf.Max(0, shieldAfter),
                crit = crit,
                receivedAt = Time.time
            });
        }
    }

    public static void QueueRemoteDone(string fromId, string battleId, int actorSlot, int moveSlot)
    {
        if (!MMOnsterpatchPvPState.Active || !MMOnsterpatchPvPState.RealBattlesEnabled)
            return;

        if (!string.IsNullOrEmpty(MMOnsterpatchPvPState.BattleId) && battleId != MMOnsterpatchPvPState.BattleId)
            return;

        lock (hitLock)
        {
            queuedDone.Add(new MMOnsterpatchBattleDone
            {
                fromId = fromId ?? "",
                battleId = battleId ?? "",
                actorSlot = actorSlot,
                moveSlot = moveSlot,
                receivedAt = Time.time
            });
        }
    }

    private static bool TryDequeueHitForCommand(MMOnsterpatchBattleCommand cmd, out MMOnsterpatchBattleHit hit)
    {
        lock (hitLock)
        {
            for (int i = 0; i < queuedHits.Count; i++)
            {
                MMOnsterpatchBattleHit h = queuedHits[i];
                if (h == null)
                    continue;

                bool actorMatches = cmd == null || h.actorSlot == cmd.actorSlot;
                bool moveMatches = cmd == null || h.moveSlot == cmd.moveSlot;

                if (actorMatches && moveMatches)
                {
                    queuedHits.RemoveAt(i);
                    hit = h;
                    return true;
                }
            }
        }

        hit = null;
        return false;
    }

    private static bool HasDoneForCommand(MMOnsterpatchBattleCommand cmd)
    {
        lock (hitLock)
        {
            for (int i = 0; i < queuedDone.Count; i++)
            {
                MMOnsterpatchBattleDone d = queuedDone[i];
                if (d == null)
                    continue;

                bool actorMatches = cmd == null || d.actorSlot == cmd.actorSlot;
                bool moveMatches = cmd == null || d.moveSlot == cmd.moveSlot;

                if (actorMatches && moveMatches)
                {
                    queuedDone.RemoveAt(i);
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryDequeueCommandForSlot(int actorSlot, out MMOnsterpatchBattleCommand cmd)
    {
        lock (commandLock)
        {
            for (int i = 0; i < queuedCommands.Count; i++)
            {
                MMOnsterpatchBattleCommand c = queuedCommands[i];
                if (c != null && c.actorSlot == actorSlot)
                {
                    queuedCommands.RemoveAt(i);
                    cmd = c;
                    return true;
                }
            }
        }

        cmd = null;
        return false;
    }

    public static IEnumerator NetworkEnemyTurn(BattleSystem bs, BattleBattler battler)
    {
        if (bs == null || battler == null || battler.monObject == null || battler.monObject.mon == null)
        {
            InvokePrivate(bs, "BattleStep", new object[0], null);
            yield break;
        }

        int actorSlot = battler.monObject.teamIndex;
        SetField(bs, "curMon", actorSlot);
        SetField(bs, "currentBattler", battler);

        try
        {
            if (bs.enemyWhiteCircle != null && actorSlot >= 0 && actorSlot < bs.enemyWhiteCircle.Length && bs.enemyWhiteCircle[actorSlot] != null)
                bs.enemyWhiteCircle[actorSlot].SetActive(true);

            if (Camera.main != null && battler.obj != null)
                Camera.main.gameObject.GetComponent<CameraFollow>().SetTarget(battler.obj);
        }
        catch { }

        yield return StartPrivateCoroutine(bs, "ShowAndWaitTurn", new object[] { true });
        yield return StartPrivateCoroutine(bs, "ResolveStartOfTurnStatusTicks", new object[] { battler });

        if (battler.monObject.mon.hp <= 0)
        {
            FinishSkippedTurn(bs, battler, battler.monObject.mon, false);
            yield break;
        }

        if (battler.monObject.mon.HasDebuff(Mon.Debuff.Stun))
        {
            yield return StartPrivateCoroutine(bs, "ShowBattleTextAndWait", new object[] { "The enemy " + battler.monObject.mon.monScriptableObject.monName + " is stunned!", 1f });
            yield return new WaitForSeconds(0.2f);
            yield return PassEnemyTurnAfterNoMove(bs, battler, battler.monObject.mon, -1);
            yield break;
        }

        yield return StartPrivateCoroutine(bs, "HandleTurnStartPassives", new object[] { battler });
        yield return StartPrivateCoroutine(bs, "HandleLeafToken", new object[] { battler });

        Mon actingMon = battler.monObject.mon;
        List<int> availableMoveSlots = InvokePrivate(bs, "GetAvailableMoveSlots", new object[] { actingMon }, null) as List<int>;
        if (availableMoveSlots == null)
            availableMoveSlots = new List<int>();

        if (availableMoveSlots.Count == 0)
        {
            string message = actingMon.HasDebuff(Mon.Debuff.Silence)
                ? "The enemy " + actingMon.monScriptableObject.monName + " is silenced and cannot use moves with cooldowns."
                : "The enemy " + actingMon.monScriptableObject.monName + " doesn't have any available moves...";
            yield return StartPrivateCoroutine(bs, "ShowBattleTextAndWait", new object[] { message, 1f });
            yield return new WaitForSeconds(0.2f);
            yield return PassEnemyTurnAfterNoMove(bs, battler, actingMon, -1);
            yield break;
        }

        MMOnsterpatchBattleCommand cmd = null;
        float waitStart = Time.time;
        float timeout = Mathf.Max(5f, MMOnsterpatchPvPState.BattleCommandTimeoutSeconds);

        while (!TryDequeueCommandForSlot(actorSlot, out cmd))
        {
            if (!MMOnsterpatchPvPState.Active || !MMOnsterpatchPvPState.RealBattlesEnabled)
                yield break;

            if (Time.time - waitStart > timeout)
                break;

            yield return null;
        }

        int chosenMoveSlot;
        int selectedTarget;
        if (cmd != null)
        {
            chosenMoveSlot = cmd.moveSlot;
            selectedTarget = cmd.targetSlot;
        }
        else
        {
            chosenMoveSlot = availableMoveSlots[0];
            selectedTarget = -1;
            Debug.LogWarning("[MMOnsterpatch] Timed out waiting for remote PvP command for enemy slot " + actorSlot + ". Falling back to first available move.");
        }

        if (!availableMoveSlots.Contains(chosenMoveSlot))
            chosenMoveSlot = availableMoveSlots[0];

        if (actingMon.moveIDs == null || chosenMoveSlot < 0 || chosenMoveSlot >= actingMon.moveIDs.Length || actingMon.moveIDs[chosenMoveSlot] < 0)
        {
            yield return PassEnemyTurnAfterNoMove(bs, battler, actingMon, -1);
            yield break;
        }

        int chosenMoveId = actingMon.moveIDs[chosenMoveSlot];
        MoveScriptableObject move = bs.gameScript.moveScriptableObject[chosenMoveId];
        GameObject enemyCasterObj = InvokePrivate(bs, "GetEnemySlotObject", new object[] { actorSlot }, null) as GameObject;
        if (enemyCasterObj == null || move == null)
        {
            InvokePrivate(bs, "BattleStep", new object[0], null);
            yield break;
        }

        SetField(bs, "curMove", chosenMoveSlot);
        SetField(bs, "selectedMonIndex", selectedTarget);

        if (cmd != null)
        {
            yield return ReplayAuthoritativeHitsForRemoteTurn(bs, battler, actingMon, enemyCasterObj, move, chosenMoveSlot, cmd);
            yield break;
        }

        Debug.LogWarning("[MMOnsterpatch] Missing remote PvP command for enemy slot " + actorSlot + ". Falling back to local ResolveMove.");
        IEnumerator resolve = InvokePrivate(bs, "ResolveMove", new object[] { enemyCasterObj, move, true }, null) as IEnumerator;
        if (resolve != null)
            bs.StartCoroutine(resolve);
        else
            InvokePrivate(bs, "BattleStep", new object[0], null);

        yield break;
    }

    private static IEnumerator PassEnemyTurnAfterNoMove(BattleSystem bs, BattleBattler battler, Mon actingMon, int usedMoveSlot)
    {
        if (battler != null)
        {
            battler.turnMeter -= 100f;
            if (battler.turnMeter < 0f)
                battler.turnMeter = 0f;
        }

        if (actingMon != null)
            InvokePrivate(bs, "AdvanceCooldownsAfterUsingMove", new object[] { actingMon, usedMoveSlot }, null);

        if (battler != null)
        {
            yield return StartPrivateCoroutine(bs, "ResolveEndOfTurnStatusTicks", new object[] { battler });
            yield return StartPrivateCoroutine(bs, "HandleTurnEndPassives", new object[] { battler });

            try { InvokePrivate(bs, "TickStatusDurations", new object[] { actingMon, battler.monObject }, null); }
            catch { }
        }

        SetField(bs, "currentBattler", null);
        InvokePrivate(bs, "BattleStep", new object[0], null);
    }

    private static void FinishSkippedTurn(BattleSystem bs, BattleBattler battler, Mon actingMon, bool advanceCooldown)
    {
        if (battler != null)
        {
            battler.turnMeter -= 100f;
            if (battler.turnMeter < 0f)
                battler.turnMeter = 0f;
        }

        if (advanceCooldown && actingMon != null)
            InvokePrivate(bs, "AdvanceCooldownsAfterUsingMove", new object[] { actingMon, -1 }, null);

        SetField(bs, "currentBattler", null);
        InvokePrivate(bs, "BattleStep", new object[0], null);
    }


    public static IEnumerator WrapResolveMoveAndReportState(BattleSystem bs, GameObject moveUserObj, MoveScriptableObject move, bool isEnemyUsingMove, IEnumerator inner)
    {
        bool shouldRecord = false;

        try
        {
            MonObject mo = moveUserObj != null ? moveUserObj.GetComponent<MonObject>() : null;
            shouldRecord =
                MMOnsterpatchPvPState.Active &&
                MMOnsterpatchPvPState.RealBattlesEnabled &&
                !isEnemyUsingMove &&
                mo != null &&
                !mo.CompareTag("Enemy");

            if (shouldRecord)
            {
                recordingLocalMove = true;
                recordingActorSlot = mo.teamIndex;
                recordingMoveSlot = GetIntField(bs, "curMove", -1);
                recordingTargetSlot = GetIntField(bs, "selectedMonIndex", -1);
                pendingCritByTargetId.Clear();
            }
        }
        catch { }

        if (inner != null)
        {
            while (inner.MoveNext())
                yield return inner.Current;
        }

        MMOnsterpatchRunner runner = MMOnsterpatchRunner.Current;
        if (shouldRecord && runner != null)
        {
            runner.SendBattleDone(recordingActorSlot, recordingMoveSlot);
            recordingLocalMove = false;
            recordingActorSlot = -1;
            recordingMoveSlot = -1;
            recordingTargetSlot = -1;
            pendingCritByTargetId.Clear();
        }

        if (runner != null)
            runner.SendLocalBattleState();
    }

    public static void NoteLocalDamagePopup(BattleSystem bs, int amount, GameObject targetObj, bool crit)
    {
        if (!recordingLocalMove || targetObj == null)
            return;

        try
        {
            pendingCritByTargetId[targetObj.GetInstanceID()] = crit;
        }
        catch { }
    }

    public static void SendAuthoritativeHitAfterDamage(BattleSystem bs, MonObject targetMonObj, int damage)
    {
        if (!recordingLocalMove || bs == null || targetMonObj == null || targetMonObj.mon == null)
            return;

        try
        {
            GameObject targetObj = targetMonObj.gameObject;
            if (targetObj == null || !targetObj.CompareTag("Enemy"))
                return;

            bool crit = false;
            if (targetObj != null)
                pendingCritByTargetId.TryGetValue(targetObj.GetInstanceID(), out crit);

            MMOnsterpatchRunner runner = MMOnsterpatchRunner.Current;
            if (runner == null)
                return;

            runner.SendBattleHit(
                recordingActorSlot,
                recordingMoveSlot,
                "E",
                targetMonObj.teamIndex,
                Mathf.Max(0, damage),
                Mathf.Max(0, targetMonObj.mon.hp),
                GetMonIntField(targetMonObj.mon, "shield", 0),
                crit);
        }
        catch { }
    }

    private static IEnumerator ReplayAuthoritativeHitsForRemoteTurn(BattleSystem bs, BattleBattler battler, Mon actingMon, GameObject enemyCasterObj, MoveScriptableObject move, int usedMoveSlot, MMOnsterpatchBattleCommand cmd)
    {
        if (bs == null || battler == null || actingMon == null || enemyCasterObj == null || move == null)
        {
            InvokePrivate(bs, "BattleStep", new object[0], null);
            yield break;
        }

        try
        {
            if (Camera.main != null)
                Camera.main.gameObject.GetComponent<CameraFollow>().SetTarget(enemyCasterObj);
        }
        catch { }

        try { bs.gameScript.audioSystem.PlaySFX("turn"); } catch { }

        string monName = actingMon.monScriptableObject != null ? actingMon.monScriptableObject.monName : "opponent";
        yield return StartPrivateCoroutine(bs, "ShowBattleTextAndWait", new object[] { "The enemy " + monName + " used " + move.moveName + "!", 0.25f });

        float waitStart = Time.time;
        float timeout = Mathf.Max(2f, MMOnsterpatchPvPState.BattleHitTimeoutSeconds);
        bool done = false;

        while (!done || HasQueuedHitsForCommand(cmd))
        {
            MMOnsterpatchBattleHit hit;
            if (TryDequeueHitForCommand(cmd, out hit))
            {
                waitStart = Time.time;
                yield return ReplayOneAuthoritativeHit(bs, move, hit);
                continue;
            }

            if (HasDoneForCommand(cmd))
            {
                done = true;
                if (!HasQueuedHitsForCommand(cmd))
                    break;
            }

            if (Time.time - waitStart > timeout)
            {
                Debug.LogWarning("[MMOnsterpatch] Timed out waiting for authoritative PvP hits for enemy slot " + cmd.actorSlot + ".");
                break;
            }

            yield return null;
        }

        if (battler != null)
        {
            battler.turnMeter -= 100f;
            if (battler.turnMeter < 0f)
                battler.turnMeter = 0f;
        }

        if (actingMon != null)
            InvokePrivate(bs, "AdvanceCooldownsAfterUsingMove", new object[] { actingMon, usedMoveSlot }, null);

        yield return StartPrivateCoroutine(bs, "ResolveEndOfTurnStatusTicks", new object[] { battler });
        yield return StartPrivateCoroutine(bs, "HandleTurnEndPassives", new object[] { battler });

        SetField(bs, "selectedMonIndex", -1);
        SetField(bs, "currentBattler", null);

        MMOnsterpatchRunner runner = MMOnsterpatchRunner.Current;
        if (runner != null)
            runner.SendLocalBattleState();

        InvokePrivate(bs, "BattleStep", new object[0], null);
    }

    private static bool HasQueuedHitsForCommand(MMOnsterpatchBattleCommand cmd)
    {
        lock (hitLock)
        {
            for (int i = 0; i < queuedHits.Count; i++)
            {
                MMOnsterpatchBattleHit h = queuedHits[i];
                if (h == null)
                    continue;

                if ((cmd == null || h.actorSlot == cmd.actorSlot) && (cmd == null || h.moveSlot == cmd.moveSlot))
                    return true;
            }
        }

        return false;
    }

    private static IEnumerator ReplayOneAuthoritativeHit(BattleSystem bs, MoveScriptableObject move, MMOnsterpatchBattleHit hit)
    {
        if (bs == null || move == null || hit == null)
            yield break;

        // Sender's enemy side is the receiver's local/player side.
        bool receiverEnemySide = hit.targetSide == "P";
        GameObject targetObj = GetSlotObject(bs, receiverEnemySide, hit.targetSlot);
        Mon targetMon = GetSlotMon(bs, receiverEnemySide, hit.targetSlot);
        if (targetObj == null || targetMon == null)
            yield break;

        targetObj.SetActive(true);
        try
        {
            if (Camera.main != null)
                Camera.main.gameObject.GetComponent<CameraFollow>().SetTarget(targetObj);
        }
        catch { }

        yield return new WaitForSeconds(0.05f);
        SpawnMoveEffect(bs, move, targetObj, true);
        if (move.isFrameDelayed)
            yield return new WaitForSeconds(0.2f);
        yield return new WaitForSeconds(0.2f);

        try { bs.ShowDamageText(hit.amount, targetObj, hit.crit); } catch { }

        targetMon.hp = Mathf.Clamp(hit.hpAfter, 0, targetMon.maxhp);
        SetMonIntField(targetMon, "shield", hit.shieldAfter);
        RefreshSlotHpBar(bs, receiverEnemySide, hit.targetSlot, targetMon, true);

        if (targetMon.hp <= 0)
        {
            yield return new WaitForSeconds(0.35f);
            SpawnPoof(bs, targetObj);
            targetObj.SetActive(false);
            RefreshSlotHpBar(bs, receiverEnemySide, hit.targetSlot, targetMon, false);
        }
        else if (!move.isFrameDelayed)
        {
            yield return new WaitForSeconds(0.15f);
        }
    }

    private static void SpawnMoveEffect(BattleSystem bs, MoveScriptableObject move, GameObject targetObj, bool enemyUsingMove)
    {
        try
        {
            GameObject prefab = GetFieldObject(bs, "effectPrefab") as GameObject;
            if (move != null && move.sprites != null && move.sprites.Length == 10)
                prefab = GetFieldObject(bs, "effectPrefabLarge") as GameObject ?? prefab;
            else if (move != null && move.sprites != null && move.sprites.Length == 9)
                prefab = GetFieldObject(bs, "effectPrefab9") as GameObject ?? prefab;

            if (prefab == null || targetObj == null)
                return;

            GameObject g = UnityEngine.Object.Instantiate<GameObject>(prefab, targetObj.transform.position - new Vector3(0f, 0.1f, 0f), Quaternion.identity);
            SpriteOverride so = g.GetComponent<SpriteOverride>();
            if (so != null && move != null && move.sprites != null)
                so.SetSpriteSheet(move.sprites);

            if (enemyUsingMove)
                g.transform.localScale = new Vector3(-1f, 1f, 1f);
        }
        catch { }
    }

    private static void SpawnPoof(BattleSystem bs, GameObject targetObj)
    {
        try
        {
            GameObject prefab = GetFieldObject(bs, "poofWhitePrefab") as GameObject;
            if (prefab != null && targetObj != null)
                UnityEngine.Object.Instantiate<GameObject>(prefab, targetObj.transform.position, Quaternion.identity);
        }
        catch { }
    }

    private static GameObject GetSlotObject(BattleSystem bs, bool enemySide, int slot)
    {
        if (bs == null || slot < 0 || slot >= 4)
            return null;

        object arrObj = GetFieldObject(bs, enemySide ? "enemySlotMonObj" : "playerSlotMonObj");
        Array arr = arrObj as Array;
        if (arr != null && slot < arr.Length)
        {
            object entry = arr.GetValue(slot);
            if (entry is MonObject)
                return ((MonObject)entry).gameObject;
            if (entry is GameObject)
                return (GameObject)entry;
        }

        arrObj = GetFieldObject(bs, enemySide ? "enemyMonObj" : "playerMonObj");
        arr = arrObj as Array;
        if (arr != null && slot < arr.Length)
        {
            object entry = arr.GetValue(slot);
            if (entry is GameObject)
                return (GameObject)entry;
            if (entry is MonObject)
                return ((MonObject)entry).gameObject;
        }

        return null;
    }

    private static Mon GetSlotMon(BattleSystem bs, bool enemySide, int slot)
    {
        if (bs == null || slot < 0 || slot >= 4)
            return null;

        if (enemySide)
        {
            Mon[] mons = GetFieldObject(bs, "enemyMon") as Mon[];
            return mons != null && slot < mons.Length ? mons[slot] : null;
        }

        return bs.gameScript != null && bs.gameScript.teamMon != null && slot < bs.gameScript.teamMon.Length ? bs.gameScript.teamMon[slot] : null;
    }

    private static void RefreshSlotHpBar(BattleSystem bs, bool enemySide, int slot, Mon mon, bool keepVisible)
    {
        try
        {
            GameObject targetObj = GetSlotObject(bs, enemySide, slot);
            if (targetObj != null)
            {
                MonObject mo = targetObj.GetComponent<MonObject>();
                if (mo != null && mon != null)
                    mo.mon = mon;
            }

            object barsObj = GetFieldObject(bs, enemySide ? "enemyHealthBar" : "playerHealthBar");
            Array bars = barsObj as Array;
            if (bars == null || slot < 0 || slot >= bars.Length)
                return;

            GameObject hbObj = bars.GetValue(slot) as GameObject;
            if (hbObj == null || mon == null)
                return;

            hbObj.SetActive(keepVisible || mon.hp > 0);
            HealthBar hb = hbObj.GetComponent<HealthBar>();
            if (hb == null)
                return;

            InvokePrivate(hb, "BindMon", new object[] { mon }, null);
            InvokePrivate(hb, "SetNewHP", new object[] { mon.hp }, null);
            InvokePrivate(hb, "SetShield", new object[] { GetMonIntField(mon, "shield", 0) }, null);
        }
        catch { }
    }

    private static int GetMonIntField(Mon mon, string fieldName, int fallback)
    {
        try
        {
            if (mon == null)
                return fallback;
            FieldInfo f = typeof(Mon).GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f == null)
                return fallback;
            object v = f.GetValue(mon);
            if (v is int)
                return (int)v;
            int parsed;
            if (v != null && int.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                return parsed;
        }
        catch { }
        return fallback;
    }

    private static void SetMonIntField(Mon mon, string fieldName, int value)
    {
        try
        {
            if (mon == null)
                return;
            FieldInfo f = typeof(Mon).GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null)
                f.SetValue(mon, value);
        }
        catch { }
    }

    private static object GetFieldObject(object obj, string name)
    {
        try
        {
            if (obj == null)
                return null;
            FieldInfo f = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (f == null)
                return null;
            return f.GetValue(f.IsStatic ? null : obj);
        }
        catch { return null; }
    }

    private static IEnumerator StartPrivateCoroutine(BattleSystem bs, string methodName, object[] args)
    {
        IEnumerator routine = InvokePrivate(bs, methodName, args, null) as IEnumerator;
        if (routine != null)
            yield return bs.StartCoroutine(routine);
    }

    private static object InvokePrivate(object obj, string methodName, object[] args, object fallback)
    {
        if (obj == null)
            return fallback;

        try
        {
            Type t = obj.GetType();
            MethodInfo[] methods = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            int argCount = args == null ? 0 : args.Length;
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo m = methods[i];
                if (m.Name != methodName)
                    continue;

                ParameterInfo[] ps = m.GetParameters();
                if (ps.Length != argCount)
                    continue;

                return m.Invoke(obj, args);
            }
        }
        catch { }

        return fallback;
    }

    private static int GetIntField(object obj, string name, int fallback)
    {
        try
        {
            FieldInfo f = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (f == null)
                return fallback;
            object v = f.GetValue(f.IsStatic ? null : obj);
            if (v is int)
                return (int)v;
        }
        catch { }
        return fallback;
    }

    private static void SetField(object obj, string name, object value)
    {
        try
        {
            FieldInfo f = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (f != null)
                f.SetValue(f.IsStatic ? null : obj, value);
        }
        catch { }
    }
}


[HarmonyPatch(typeof(BattleSystem), "ResolveMove")]
public static class MMOnsterpatch_BattleSystem_ResolveMove_SendState_Patch
{
    public static void Postfix(BattleSystem __instance, GameObject moveUserObj, MoveScriptableObject move, bool isEnemyUsingMove, ref IEnumerator __result)
    {
        if (!MMOnsterpatchPvPState.Active || !MMOnsterpatchPvPState.RealBattlesEnabled || __result == null)
            return;

        __result = MMOnsterpatchRealPvP.WrapResolveMoveAndReportState(__instance, moveUserObj, move, isEnemyUsingMove, __result);
    }
}

[HarmonyPatch(typeof(BattleSystem), "Victory2")]
public static class MMOnsterpatch_BattleSystem_Victory2_ClearPvPEncounter_Patch
{
    public static void Prefix()
    {
        if (MMOnsterpatchPvPState.Active)
            MMOnsterpatchPvPState.ClearPvPEncounterResidue();
    }
}

[HarmonyPatch(typeof(BattleSystem), "ExitBattle")]
public static class MMOnsterpatch_BattleSystem_ExitBattle_ClearPvP_Patch
{
    public static void Postfix()
    {
        if (MMOnsterpatchPvPState.Active)
            MMOnsterpatchPvPState.EndAndRestoreExp();
    }
}


public static class MMOnsterpatchPersonalEncounterSpawnRuntime
{
    public static bool ShouldHandle(GameScript gs)
    {
        try
        {
            if (gs == null)
                return false;
            if (!MMOnsterpatchAIOBootstrap.IsMMOWorldModeActive())
                return false;
            if (MMOnsterpatchPvPState.Active)
                return false;
            bool wasTrainer = false;
            try { wasTrainer = (bool)(typeof(GameScript).GetField("lastEncounterWasTrainer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(gs) ?? false); } catch { }
            try { wasTrainer = wasTrainer || GameScript.battlingEnemyTrainer || GameScript.aboutToBattleEnemyTrainer; } catch { }
            if (wasTrainer)
                return false;
            if (gs.lastEncounterMons == null)
                return false;
            for (int i = 0; i < gs.lastEncounterMons.Length; i++)
                if (gs.lastEncounterMons[i] != null)
                    return true;
        }
        catch { }
        return false;
    }

    public static IEnumerator OpenEncounterCrystalPersonalRoutine(GameScript gs)
    {
        if (gs == null)
            yield break;

        try { Debug.Log("[MMOnsterpatch MMO World] personal encounter crystal open -> server personal spawns"); } catch { }
        try { gs.audioSystem.PlaySFX("cast"); } catch { }
        try { gs.playerController.PlayAnimation(PlayerController.facingDir); } catch { }
        yield return new WaitForSeconds(0.2f);
        try
        {
            if (gs.encounterCrystalObj != null)
                gs.encounterCrystalObj.GetComponent<Animator>().Play("crystalDisappear");
        }
        catch { }

        List<Vector3> spots = BuildOfficialSpawnSpots(gs);
        try
        {
            if (MMOnsterpatchRunner.Current != null)
                MMOnsterpatchRunner.Current.ReportPersonalEncounterSpawnsFromPatch(gs, spots, "OpenEncounterCrystal-personal");
        }
        catch { }

        yield return new WaitForSeconds(0.2f);
        try { if (gs.encounterCrystalObj != null) gs.encounterCrystalObj.SetActive(false); } catch { }
        try { gs.spawnedEncounterCrystal = false; } catch { }
        try { gs.ClearLastEncounterMons(); } catch { }
        try { GameScript.interacting = false; } catch { }
    }

    private static List<Vector3> BuildOfficialSpawnSpots(GameScript gs)
    {
        List<Vector3> spots = new List<Vector3>();
        try
        {
            Vector3 center = gs.encounterCrystalObj != null ? gs.encounterCrystalObj.transform.position : gs.lastEncounterPos;
            List<Vector2Int> usedTiles = new List<Vector2Int>();
            MethodInfo trySpotMethod = typeof(GameScript).GetMethod("TryGetClosestOpenSpotAroundPos", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo tileKeyMethod = typeof(GameScript).GetMethod("WorldPosToTileKey", BindingFlags.Instance | BindingFlags.NonPublic);
            for (int i = 0; i < 4; i++)
            {
                if (gs.lastEncounterMons == null || i >= gs.lastEncounterMons.Length || gs.lastEncounterMons[i] == null)
                {
                    spots.Add(Vector3.zero);
                    continue;
                }

                Vector3 spot = Vector3.zero;
                bool ok = false;
                if (trySpotMethod != null)
                {
                    object[] args = new object[] { center, 3, usedTiles, Vector3.zero };
                    try { ok = (bool)trySpotMethod.Invoke(gs, args); spot = (Vector3)args[3]; } catch { ok = false; }
                }
                if (!ok)
                {
                    try
                    {
                        Vector2 v = gs.GetRandomOpenSpotAroundPos(new Vector2(center.x, center.y), 3);
                        if (v.x > -90f && v.y > -90f)
                        {
                            spot = new Vector3(v.x, v.y, v.y * 0.01f);
                            ok = true;
                        }
                    }
                    catch { }
                }
                if (!ok)
                    spot = center;
                spots.Add(spot);
                try
                {
                    if (tileKeyMethod != null)
                        usedTiles.Add((Vector2Int)tileKeyMethod.Invoke(gs, new object[] { spot }));
                }
                catch { }
            }
        }
        catch { }
        return spots;
    }
}

public static class MMOnsterpatchMmoWorldEncounterSuppressor
{
    private static float lastClearTime;
    private static float lastLogTime;

    public static bool Active
    {
        get
        {
            try { return MMOnsterpatchAIOBootstrap.IsMMOWorldModeActive(); }
            catch { return false; }
        }
    }

    public static IEnumerator EmptyRoutine()
    {
        yield break;
    }

    public static void ClearEncounterObjects()
    {
        if (!Active)
            return;

        try
        {
            // Do not hammer Destroy() every frame, but keep local vanilla spawns cleaned up.
            if (Time.time - lastClearTime < 0.35f)
                return;
            lastClearTime = Time.time;
        }
        catch { }

        try
        {
            GameScript gs = UnityEngine.Object.FindObjectOfType<GameScript>();
            if (gs == null)
                return;

            // Keep random grass/cave encounters and post-battle encounter crystals intact.
            // Only clear non-network vanilla visible overworld Mons so the server-owned spawn layer remains authoritative.
            ClearLocalOverworldMons(gs);
        }
        catch { }
    }

    private static void SetFieldValueForEncounterSuppressor(object obj, string name, object value)
    {
        try
        {
            if (obj == null || string.IsNullOrEmpty(name))
                return;

            System.Reflection.FieldInfo f = obj.GetType().GetField(
                name,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Static);

            if (f == null)
                return;

            f.SetValue(f.IsStatic ? null : obj, value);
        }
        catch { }
    }

    private static void ClearLocalOverworldMons(GameScript gs)
    {
        try
        {
            if (gs == null || gs._OverworldMons == null)
                return;

            int removed = 0;
            for (int i = gs._OverworldMons.childCount - 1; i >= 0; i--)
            {
                Transform child = gs._OverworldMons.GetChild(i);
                if (child == null || child.gameObject == null)
                    continue;

                string nm = child.gameObject.name ?? "";
                if (nm.StartsWith("MMOnsterpatch", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Starter hatch/claim sequence is vanilla-owned. HatchStarterMon creates
                // temporary starter overworld mons under GameScript._OverworldMons named
                // 0starter..5starter. The server-owned overworld-spawn cleaner must not
                // delete those, or starter eggs appear to hatch and then immediately vanish.
                if (IsStarterSequenceObject(child.gameObject))
                    continue;

                bool isServerWorldSpawn = false;
                try { isServerWorldSpawn = child.GetComponent<MMOWorldSpawnMarker>() != null || child.GetComponentInChildren<MMOWorldSpawnMarker>() != null; } catch { }
                if (isServerWorldSpawn)
                    continue;

                bool looksLikeVanillaMon = false;
                try { looksLikeVanillaMon = child.GetComponent<MonObject>() != null || child.GetComponentInChildren<MonObject>() != null; } catch { }
                if (!looksLikeVanillaMon)
                    continue;

                removed++;
                UnityEngine.Object.Destroy(child.gameObject);
            }

            if (removed > 0)
                LogSuppressed("cleared " + removed.ToString() + " local vanilla overworld spawn(s)");
        }
        catch { }
    }

    private static bool IsStarterSequenceObject(GameObject go)
    {
        try
        {
            if (go == null)
                return false;

            string nm = go.name ?? "";
            if (IsStarterSequenceName(nm))
                return true;

            Transform t = go.transform;
            while (t != null)
            {
                if (IsStarterSequenceName(t.name ?? ""))
                    return true;
                t = t.parent;
            }
        }
        catch { }
        return false;
    }

    private static bool IsStarterSequenceName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        // Vanilla starter eggs and the hatched starter mons use the exact names
        // 0starter through 5starter. Keep the match narrow so normal overworld
        // server-spawn suppression still removes regular vanilla visible spawns.
        if (name.Length == 8 && name.EndsWith("starter", StringComparison.OrdinalIgnoreCase))
        {
            char c = name[0];
            return c >= '0' && c <= '5';
        }

        return false;
    }

    public static bool ShouldSuppressAndClear(string source = null)
    {
        if (!Active)
            return false;

        ClearEncounterObjects();
        bool blockVanillaVisibleSpawnZone = !string.IsNullOrEmpty(source)
            && source.StartsWith("WildMonSpawnManager.", StringComparison.OrdinalIgnoreCase);
        if (blockVanillaVisibleSpawnZone)
        {
            LogSuppressed("blocked " + source);
            return true;
        }

        // Random grass/cave battles and the encounter-crystal flow are allowed online.
        // Personal encounter reward spawns are intercepted separately and sent to the server.
        return false;
    }

    public static void LogSuppressed(string msg)
    {
        try
        {
            if (Time.time - lastLogTime < 0.5f)
                return;
            lastLogTime = Time.time;
        }
        catch { }

        try { Debug.Log("[MMOnsterpatch MMO World] " + msg); } catch { }
    }
}

[HarmonyPatch(typeof(GameScript), "WildEncounterCheck")]
public static class MMOnsterpatch_GameScript_WildEncounterCheck_BlockMmoWorld_Patch
{
    public static bool Prefix()
    {
        return !MMOnsterpatchMmoWorldEncounterSuppressor.ShouldSuppressAndClear("GameScript.WildEncounterCheck");
    }
}

[HarmonyPatch(typeof(GameScript), "StartWildEncounter")]
public static class MMOnsterpatch_GameScript_StartWildEncounter_BlockMmoWorld_Patch
{
    public static bool Prefix(ref IEnumerator __result)
    {
        if (!MMOnsterpatchMmoWorldEncounterSuppressor.ShouldSuppressAndClear("GameScript.StartWildEncounter"))
            return true;

        __result = MMOnsterpatchMmoWorldEncounterSuppressor.EmptyRoutine();
        return false;
    }
}

[HarmonyPatch(typeof(GameScript), "OpenEncounterCrystal")]
public static class MMOnsterpatch_GameScript_OpenEncounterCrystal_BlockMmoWorld_Patch
{
    public static bool Prefix(GameScript __instance, ref IEnumerator __result)
    {
        if (MMOnsterpatchPersonalEncounterSpawnRuntime.ShouldHandle(__instance))
        {
            __result = MMOnsterpatchPersonalEncounterSpawnRuntime.OpenEncounterCrystalPersonalRoutine(__instance);
            return false;
        }

        if (!MMOnsterpatchMmoWorldEncounterSuppressor.ShouldSuppressAndClear("GameScript.OpenEncounterCrystal"))
            return true;

        __result = MMOnsterpatchMmoWorldEncounterSuppressor.EmptyRoutine();
        return false;
    }
}

[HarmonyPatch(typeof(GameScript), "MonPopsOutOfEncounterCrystal")]
public static class MMOnsterpatch_GameScript_MonPopsOutOfEncounterCrystal_BlockMmoWorld_Patch
{
    public static bool Prefix(ref IEnumerator __result)
    {
        if (!MMOnsterpatchMmoWorldEncounterSuppressor.ShouldSuppressAndClear("GameScript.MonPopsOutOfEncounterCrystal"))
            return true;

        __result = MMOnsterpatchMmoWorldEncounterSuppressor.EmptyRoutine();
        return false;
    }
}

[HarmonyPatch(typeof(WildMonSpawnManager), "RollForWildMonsInAZone")]
public static class MMOnsterpatch_WildMonSpawnManager_RollForWildMonsInAZone_BlockMmoWorld_Patch
{
    public static bool Prefix()
    {
        return !MMOnsterpatchMmoWorldEncounterSuppressor.ShouldSuppressAndClear("WildMonSpawnManager.RollForWildMonsInAZone");
    }
}

[HarmonyPatch(typeof(WildMonSpawnManager), "RollForWildMonsInAZone2")]
public static class MMOnsterpatch_WildMonSpawnManager_RollForWildMonsInAZone2_BlockMmoWorld_Patch
{
    public static bool Prefix(ref IEnumerator __result)
    {
        if (!MMOnsterpatchMmoWorldEncounterSuppressor.ShouldSuppressAndClear("WildMonSpawnManager.RollForWildMonsInAZone2"))
            return true;

        __result = MMOnsterpatchMmoWorldEncounterSuppressor.EmptyRoutine();
        return false;
    }
}


[HarmonyPatch(typeof(PlayerController), "EnterBroom")]
public static class MMOnsterpatch_PlayerController_EnterBroom_RemoteVisual_Patch
{
    public static void Postfix()
    {
        try
        {
            if (MMOnsterpatchRunner.Current != null)
            {
                MMOnsterpatchRunner.Current.SendRemoteVisualEventFromPatch("BROOM_ENTER");
                MMOnsterpatchRunner.Current.SendImmediateLocalStateFromPatch("EnterBroom");
            }
        }
        catch { }
    }
}

[HarmonyPatch(typeof(PlayerController), "ExitBroom")]
public static class MMOnsterpatch_PlayerController_ExitBroom_RemoteVisual_Patch
{
    public static void Postfix()
    {
        try
        {
            if (MMOnsterpatchRunner.Current != null)
            {
                MMOnsterpatchRunner.Current.SendRemoteVisualEventFromPatch("BROOM_EXIT");
                MMOnsterpatchRunner.Current.SendImmediateLocalStateFromPatch("ExitBroom");
            }
        }
        catch { }
    }
}

[HarmonyPatch(typeof(PlayerController), "PlayAnimation", new Type[] { typeof(int) })]
public static class MMOnsterpatch_PlayerController_PlayAnimation_RemoteVisual_Patch
{
    public static void Prefix(int dir)
    {
        try
        {
            if (MMOnsterpatchRunner.Current != null)
                MMOnsterpatchRunner.Current.SendRemoteVisualEventFromPatch("CAST", dir);
        }
        catch { }
    }
}


[HarmonyPatch(typeof(GameScript), "Chop", new Type[] { typeof(int), typeof(GameObject) })]
public static class MMOnsterpatch_GameScript_Chop_RemoteToolVisual_Patch
{
    public static void Prefix(int dir)
    {
        try
        {
            if (MMOnsterpatchRunner.Current != null)
                MMOnsterpatchRunner.Current.SendRemoteVisualEventFromPatch("SPELL_AXE", dir);
        }
        catch { }
    }
}

[HarmonyPatch(typeof(GameScript), "Mine", new Type[] { typeof(int), typeof(GameObject) })]
public static class MMOnsterpatch_GameScript_Mine_RemoteToolVisual_Patch
{
    public static void Prefix(int dir)
    {
        try
        {
            if (MMOnsterpatchRunner.Current != null)
                MMOnsterpatchRunner.Current.SendRemoteVisualEventFromPatch("SPELL_PICKAXE", dir);
        }
        catch { }
    }
}

[HarmonyPatch(typeof(GameScript), "GenericHarvestItem", new Type[] { typeof(int), typeof(GameObject) })]
public static class MMOnsterpatch_GameScript_GenericHarvestItem_RemoteToolVisual_Patch
{
    public static void Prefix(int dir)
    {
        try
        {
            if (MMOnsterpatchRunner.Current != null)
                MMOnsterpatchRunner.Current.SendRemoteVisualEventFromPatch("SPELL_GLOVE", dir);
        }
        catch { }
    }
}

[HarmonyPatch(typeof(GameScript), "Harvest", new Type[] { typeof(int), typeof(GameObject) })]
public static class MMOnsterpatch_GameScript_Harvest_RemoteToolVisual_Patch
{
    public static void Prefix(int dir)
    {
        try
        {
            if (MMOnsterpatchRunner.Current != null)
                MMOnsterpatchRunner.Current.SendRemoteVisualEventFromPatch("SPELL_GLOVE", dir);
        }
        catch { }
    }
}

[HarmonyPatch(typeof(GameScript), "Bloom", new Type[] { typeof(int) })]
public static class MMOnsterpatch_GameScript_Bloom_RemoteToolVisual_Patch
{
    public static void Prefix(int dir)
    {
        try
        {
            if (MMOnsterpatchRunner.Current != null)
                MMOnsterpatchRunner.Current.SendRemoteVisualEventFromPatch("SPELL_BLOOM", dir);
        }
        catch { }
    }
}

[HarmonyPatch(typeof(PlayerController), "WaterRipple", new Type[] { typeof(Vector3) })]
public static class MMOnsterpatch_PlayerController_WaterRipple_RemoteVisual_Patch
{
    public static void Prefix(Vector3 pos)
    {
        try
        {
            if (MMOnsterpatchRunner.Current != null)
                MMOnsterpatchRunner.Current.SendRemoteVisualEventFromPatch("WATER_RIPPLE", -999, pos);
        }
        catch { }
    }
}

[HarmonyPatch(typeof(PlayerController), "WaterWalkCheck")]
public static class MMOnsterpatch_PlayerController_WaterWalkCheck_RemoteVisual_Patch
{
    private static bool lastWaterWalking;

    public static void Postfix(PlayerController __instance)
    {
        try
        {
            bool now = __instance != null && __instance.waterWalking;
            if (now == lastWaterWalking)
                return;

            lastWaterWalking = now;
            if (MMOnsterpatchRunner.Current != null)
                MMOnsterpatchRunner.Current.SendRemoteVisualEventFromPatch(now ? "WATER_ON" : "WATER_OFF");
        }
        catch { }
    }
}

[HarmonyPatch(typeof(PlayerController), "Jump", new Type[] { typeof(string) })]
public static class MMOnsterpatch_PlayerController_Jump_RemoteVisual_Patch
{
    public static void Postfix()
    {
        try
        {
            if (MMOnsterpatchRunner.Current != null)
                MMOnsterpatchRunner.Current.SendRemoteVisualEventFromPatch("JUMP");
        }
        catch { }
    }
}

[HarmonyPatch(typeof(PlayerController), "PlayerJump")]
public static class MMOnsterpatch_PlayerController_PlayerJump_RemoteVisual_Patch
{
    public static void Prefix()
    {
        try
        {
            if (MMOnsterpatchRunner.Current != null)
                MMOnsterpatchRunner.Current.SendRemoteVisualEventFromPatch("JUMP");
        }
        catch { }
    }
}

[HarmonyPatch(typeof(PlayerController), "PlayerBounce")]
public static class MMOnsterpatch_PlayerController_PlayerBounce_RemoteVisual_Patch
{
    public static void Prefix()
    {
        try
        {
            if (MMOnsterpatchRunner.Current != null)
                MMOnsterpatchRunner.Current.SendRemoteVisualEventFromPatch("BOUNCE");
        }
        catch { }
    }
}

public sealed class MMOWorldSpawnMarker : MonoBehaviour
{
    public string SpawnId;
    public string Scene;
    public int MonId;
    public string MonKey;
    public bool Shiny;
    public int Level;
    public bool ClaimPending;
    public bool CatchResultSent;
    public bool CaughtAndHidden;
    public string LockState = "public";
    public string PersonalOwnerId = "";
    public string MonSaveB64 = "";
}

public static class MMOWorldSpawnCatchRuntime
{
    private static string activeSpawnId = "";
    private static bool activeResultSent;
    private static float activeAt;

    public static void NoteAttempt(string spawnId)
    {
        if (string.IsNullOrWhiteSpace(spawnId))
            return;

        activeSpawnId = spawnId;
        activeResultSent = false;
        try { activeAt = Time.time; } catch { activeAt = 0f; }
        MarkClaimPending(spawnId, true);
        ResetCatchFlags(spawnId);
        try { Debug.Log("[MMOnsterpatch MMO World] active server spawn attempt = " + spawnId); } catch { }
    }


    public static void NoteAttemptFromGameScript(GameScript gs, string source = null, bool sendClaim = true)
    {
        try
        {
            GameObject obj = GetCurInteractingMonObj(gs);
            string spawnId = FindSpawnIdFromObject(obj);
            try
            {
                Debug.Log("[MMOnsterpatch MMO World] NoteAttemptFromGameScript source=" + source +
                    " obj=" + (obj != null ? obj.name : "<null>") +
                    " spawnId=" + spawnId);
            }
            catch { }

            if (string.IsNullOrWhiteSpace(spawnId))
                return;

            MMOWorldSpawnMarker marker = FindMarkerFromObject(obj);
            if (IsLockedForLocalPlayer(marker))
            {
                CloseWildMonMenuAndReset(gs);
                try
                {
                    if (MMOnsterpatchRunner.Current != null)
                        MMOnsterpatchRunner.Current.ShowWorldSpawnNoPermissionFromPatch();
                }
                catch { }
                return;
            }

            NoteAttempt(spawnId);

            if (sendClaim && MMOnsterpatchRunner.Current != null)
                MMOnsterpatchRunner.Current.ReportWorldSpawnClaimFromPatch(spawnId, source ?? "GameScript");
        }
        catch (Exception ex)
        {
            try { Debug.Log("[MMOnsterpatch MMO World] NoteAttemptFromGameScript failed: " + ex.Message); } catch { }
        }
    }

    public static void NotifyCatchAnimationStartFromGameScript(GameScript gs, string source = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(GetActiveSpawnId()))
                NoteAttemptFromGameScript(gs, (source ?? "CatchStart") + "-ensure", true);
            else
                NoteAttemptFromGameScript(gs, (source ?? "CatchStart") + "-refresh", false);
        }
        catch { }

        NotifyCatchAnimationStart(source);
    }

    private static GameObject GetCurInteractingMonObj(GameScript gs)
    {
        try
        {
            if (gs == null)
                return null;
            FieldInfo f = typeof(GameScript).GetField("curInteractingMonObj", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f == null)
                return null;
            return f.GetValue(gs) as GameObject;
        }
        catch { return null; }
    }

    public static string FindSpawnIdFromObject(GameObject obj)
    {
        if (obj == null)
            return "";

        try
        {
            MMOWorldSpawnMarker marker = obj.GetComponent<MMOWorldSpawnMarker>();
            if (marker != null && !string.IsNullOrWhiteSpace(marker.SpawnId))
                return marker.SpawnId;
        }
        catch { }

        try
        {
            MMOWorldSpawnMarker marker = obj.GetComponentInParent<MMOWorldSpawnMarker>();
            if (marker != null && !string.IsNullOrWhiteSpace(marker.SpawnId))
                return marker.SpawnId;
        }
        catch { }

        try
        {
            MMOWorldSpawnMarker marker = obj.GetComponentInChildren<MMOWorldSpawnMarker>(true);
            if (marker != null && !string.IsNullOrWhiteSpace(marker.SpawnId))
                return marker.SpawnId;
        }
        catch { }

        return "";
    }

    private static MMOWorldSpawnMarker FindMarkerFromObject(GameObject obj)
    {
        if (obj == null)
            return null;
        try
        {
            MMOWorldSpawnMarker marker = obj.GetComponent<MMOWorldSpawnMarker>();
            if (marker != null)
                return marker;
        }
        catch { }
        try
        {
            MMOWorldSpawnMarker marker = obj.GetComponentInParent<MMOWorldSpawnMarker>();
            if (marker != null)
                return marker;
        }
        catch { }
        try
        {
            MMOWorldSpawnMarker marker = obj.GetComponentInChildren<MMOWorldSpawnMarker>(true);
            if (marker != null)
                return marker;
        }
        catch { }
        return null;
    }

    private static bool IsLockedForLocalPlayer(MMOWorldSpawnMarker marker)
    {
        try
        {
            if (marker == null)
                return false;
            string lockState = (marker.LockState ?? "").Trim().ToLowerInvariant();
            if (lockState != "owner_only")
                return false;
            string owner = marker.PersonalOwnerId ?? "";
            string me = MMOnsterpatchRunner.Current != null ? MMOnsterpatchRunner.Current.LocalPlayerIdForAio : "";
            return !string.IsNullOrWhiteSpace(owner) && !string.Equals(owner, me, StringComparison.Ordinal);
        }
        catch { return false; }
    }

    private static void CloseWildMonMenuAndReset(GameScript gs)
    {
        try { if (gs != null && gs.subMenuWildMon != null) gs.subMenuWildMon.SetActive(false); } catch { }
        try { if (gs != null && gs.panelMonNamePreview != null) gs.panelMonNamePreview.SetActive(false); } catch { }
        try { if (gs != null && gs.eventSystem != null) gs.eventSystem.SetSelectedGameObject(null); } catch { }
        try { GameScript.interacting = false; } catch { }
        try { MenuScript.gameState = MenuScript.GameState.Open; } catch { }
    }

    public static void NotifyCatchAnimationStart(string source = null)
    {
        string spawnId = GetActiveSpawnId();
        try { Debug.Log("[MMOnsterpatch MMO World] NotifyCatchAnimationStart source=" + source + " spawnId=" + spawnId); } catch { }
        if (string.IsNullOrWhiteSpace(spawnId))
            return;

        try { Debug.Log("[MMOnsterpatch MMO World] server spawn catch animation started" + (string.IsNullOrEmpty(source) ? "" : " via " + source)); } catch { }

        try
        {
            if (MMOnsterpatchRunner.Current != null)
                MMOnsterpatchRunner.Current.ReportWorldSpawnCatchStartFromPatch(spawnId);
        }
        catch (Exception ex)
        {
            try { Debug.Log("[MMOnsterpatch MMO World] ReportWorldSpawnCatchStartFromPatch failed: " + ex.Message); } catch { }
        }
    }


    public static bool ShouldDeferRemove(string spawnId)
    {
        if (string.IsNullOrWhiteSpace(spawnId))
            return false;
        if (!string.Equals(GetActiveSpawnId(), spawnId, StringComparison.Ordinal))
            return false;
        return !activeResultSent;
    }

    public static void HandleCaught(string source = null)
    {
        string spawnId = GetActiveSpawnId();
        if (string.IsNullOrWhiteSpace(spawnId) || activeResultSent)
            return;

        activeResultSent = true;
        HideOrShowSpawn(spawnId, false, true);
        try { Debug.Log("[MMOnsterpatch MMO World] server spawn caught; hiding before caught message" + (string.IsNullOrEmpty(source) ? "" : " via " + source)); } catch { }
        try
        {
            if (MMOnsterpatchRunner.Current != null)
                MMOnsterpatchRunner.Current.ReportWorldSpawnCatchResultFromPatch(spawnId, true);
        }
        catch { }

        try
        {
            if (MMOnsterpatchRunner.Current != null)
                MMOnsterpatchRunner.Current.ForceLocalWorldSpawnCaughtFromPatch(spawnId);
        }
        catch { }

        activeSpawnId = "";
    }

    public static void HandleFailed(string source = null)
    {
        string spawnId = GetActiveSpawnId();
        if (string.IsNullOrWhiteSpace(spawnId) || activeResultSent)
            return;

        activeResultSent = true;
        HideOrShowSpawn(spawnId, true, false);
        MarkClaimPending(spawnId, false);
        try { Debug.Log("[MMOnsterpatch MMO World] server spawn catch failed; showing spawn again" + (string.IsNullOrEmpty(source) ? "" : " via " + source)); } catch { }
        try
        {
            if (MMOnsterpatchRunner.Current != null)
                MMOnsterpatchRunner.Current.ReportWorldSpawnCatchResultFromPatch(spawnId, false);
        }
        catch { }
        activeSpawnId = "";
    }

    public static IEnumerator WrapCatchCoroutine(GameScript gs, IEnumerator inner)
    {
        if (inner == null)
            yield break;

        while (true)
        {
            bool moved = false;
            object current = null;
            try
            {
                moved = inner.MoveNext();
                if (moved)
                    current = inner.Current;
            }
            catch
            {
                throw;
            }

            if (!moved)
                break;

            yield return current;

            if (!activeResultSent && IsActiveSpawnObjectGone())
                HandleCaught("TryToCatchWildMon2-destroyed-object");
            else if (!activeResultSent && IsCatchSuccess(gs))
                HandleCaught("TryToCatchWildMon2");
        }

        if (!activeResultSent && IsActiveSpawnObjectGone())
            HandleCaught("TryToCatchWildMon2-end-destroyed-object");
        else if (!activeResultSent && IsCatchSuccess(gs))
            HandleCaught("TryToCatchWildMon2-end");
        else if (!activeResultSent && !string.IsNullOrWhiteSpace(GetActiveSpawnId()))
            HandleFailed("TryToCatchWildMon2-end");
    }

    private static bool IsActiveSpawnObjectGone()
    {
        string spawnId = GetActiveSpawnId();
        if (string.IsNullOrWhiteSpace(spawnId))
            return false;

        try
        {
            MMOWorldSpawnMarker[] markers = UnityEngine.Object.FindObjectsOfType<MMOWorldSpawnMarker>(true);
            for (int i = 0; i < markers.Length; i++)
            {
                MMOWorldSpawnMarker m = markers[i];
                if (m != null && string.Equals(m.SpawnId, spawnId, StringComparison.Ordinal))
                    return false;
            }
        }
        catch { }

        return true;
    }

    private static bool IsCatchSuccess(GameScript gs)
    {
        try
        {
            if (gs == null)
                return false;
            System.Reflection.FieldInfo f = gs.GetType().GetField(
                "catchSuccess",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Static);
            if (f == null)
                return false;
            object v = f.GetValue(f.IsStatic ? null : gs);
            return v is bool && (bool)v;
        }
        catch { return false; }
    }

    private static string GetActiveSpawnId()
    {
        if (!string.IsNullOrWhiteSpace(activeSpawnId))
            return activeSpawnId;

        try
        {
            MMOWorldSpawnMarker[] markers = UnityEngine.Object.FindObjectsOfType<MMOWorldSpawnMarker>();
            for (int i = 0; i < markers.Length; i++)
            {
                MMOWorldSpawnMarker m = markers[i];
                if (m != null && m.ClaimPending && !string.IsNullOrWhiteSpace(m.SpawnId))
                {
                    activeSpawnId = m.SpawnId;
                    return activeSpawnId;
                }
            }
        }
        catch { }

        return "";
    }

    private static void MarkClaimPending(string spawnId, bool pending)
    {
        try
        {
            MMOWorldSpawnMarker[] markers = UnityEngine.Object.FindObjectsOfType<MMOWorldSpawnMarker>();
            for (int i = 0; i < markers.Length; i++)
            {
                MMOWorldSpawnMarker m = markers[i];
                if (m == null || !string.Equals(m.SpawnId, spawnId, StringComparison.Ordinal))
                    continue;
                m.ClaimPending = pending;
            }
        }
        catch { }
    }

    private static void ResetCatchFlags(string spawnId)
    {
        try
        {
            MMOWorldSpawnMarker[] markers = UnityEngine.Object.FindObjectsOfType<MMOWorldSpawnMarker>(true);
            for (int i = 0; i < markers.Length; i++)
            {
                MMOWorldSpawnMarker m = markers[i];
                if (m == null || !string.Equals(m.SpawnId, spawnId, StringComparison.Ordinal))
                    continue;
                m.CatchResultSent = false;
                m.CaughtAndHidden = false;
                if (m.gameObject != null)
                    m.gameObject.SetActive(true);
            }
        }
        catch { }
    }

    private static void HideOrShowSpawn(string spawnId, bool active, bool caught)
    {
        try
        {
            MMOWorldSpawnMarker[] markers = UnityEngine.Object.FindObjectsOfType<MMOWorldSpawnMarker>(true);
            for (int i = 0; i < markers.Length; i++)
            {
                MMOWorldSpawnMarker m = markers[i];
                if (m == null || !string.Equals(m.SpawnId, spawnId, StringComparison.Ordinal))
                    continue;

                m.CatchResultSent = true;
                m.CaughtAndHidden = caught;
                if (m.gameObject != null)
                    m.gameObject.SetActive(active);
            }
        }
        catch { }
    }
}

[HarmonyPatch]
public static class MMOnsterpatch_GameScript_ShowDialogue_ServerWorldSpawnResult_Patch
{
    public static MethodBase TargetMethod()
    {
        try
        {
            return typeof(GameScript).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(m => m.Name == "ShowDialogue" && m.GetParameters().Length >= 1 && m.GetParameters()[0].ParameterType == typeof(string));
        }
        catch { return null; }
    }

    public static void Prefix(object[] __args)
    {
        string key = "";
        try { if (__args != null && __args.Length > 0) key = __args[0] as string; } catch { }
        if (string.Equals(key, "caughtMonInBottle", StringComparison.Ordinal))
            MMOWorldSpawnCatchRuntime.HandleCaught("ShowDialogue-caughtMonInBottle-prefix");
        else if (string.Equals(key, "failToCatchMon", StringComparison.Ordinal))
            MMOWorldSpawnCatchRuntime.HandleFailed("ShowDialogue-failToCatchMon-prefix");
    }
}

[HarmonyPatch(typeof(GameScript), "AfterCaughtMonInBottle")]
public static class MMOnsterpatch_GameScript_AfterCaughtMonInBottle_ServerWorldSpawn_Patch
{
    public static void Prefix()
    {
        // Prefix is intentional: hide/remove the server spawn before the caught message box appears.
        MMOWorldSpawnCatchRuntime.HandleCaught("AfterCaughtMonInBottle-prefix");
    }
}

[HarmonyPatch(typeof(GameScript), "FailedToCatchMon")]
public static class MMOnsterpatch_GameScript_FailedToCatchMon_ServerWorldSpawn_Patch
{
    public static void Prefix()
    {
        // Prefix is intentional: restore the server spawn before the failed-catch message box appears.
        MMOWorldSpawnCatchRuntime.HandleFailed("FailedToCatchMon-prefix");
    }
}

[HarmonyPatch(typeof(GameScript), "Interact")]
public static class MMOnsterpatch_GameScript_Interact_ServerWorldSpawnClaim_Patch
{
    public static void Postfix(GameScript __instance)
    {
        // Decompile shows Interact sets private curInteractingMonObj to the exact raycast hit object
        // before opening the wild-mon submenu. Claim here instead of polling input after GameScript
        // may have already changed MenuScript.gameState to Menu.
        MMOWorldSpawnCatchRuntime.NoteAttemptFromGameScript(__instance, "Interact-postfix", true);
    }
}

[HarmonyPatch(typeof(GameScript), "TryToCatchWildMon")]
public static class MMOnsterpatch_GameScript_TryToCatchWildMon_ServerWorldSpawnStart_Patch
{
    public static void Prefix(GameScript __instance)
    {
        // This is the real public Catch button entry shown by the full Assembly-CSharp decompile.
        MMOWorldSpawnCatchRuntime.NotifyCatchAnimationStartFromGameScript(__instance, "TryToCatchWildMon-prefix");
    }
}


[HarmonyPatch(typeof(GameScript), "TryToCatchWildMon2")]
public static class MMOnsterpatch_GameScript_TryToCatchWildMon2_ServerWorldSpawn_Patch
{
    public static void Prefix(GameScript __instance)
    {
        // Private coroutine fallback. This should fire immediately before vanilla PlayCastLong/catchDiamondLock flow.
        MMOWorldSpawnCatchRuntime.NotifyCatchAnimationStartFromGameScript(__instance, "TryToCatchWildMon2-prefix");
    }

    public static void Postfix(GameScript __instance, ref IEnumerator __result)
    {
        if (__result != null)
            __result = MMOWorldSpawnCatchRuntime.WrapCatchCoroutine(__instance, __result);
    }
}


public sealed class MMOnsterpatchSettings
{
    public string ServerHost;
    public int ServerPort;
    public bool AutoConnectOnStartup;
    public string ClusterId;
    public string PlayerName;
    public string PlayerId;
    public string RemotePlayerSpriteName;
    public float RemotePlayerScale;
    public bool UsePlayerSprite;
    public bool UseGameDefaultVisualScale;
    public bool ShowNameplates;
    public float NameplateOffsetY;
    public float NameplateCharacterSize;
    public int NameplateFontSize;
    public string NameplateFontColorHex;
    public string NameplateShadowColorHex;
    public bool ShowNameplateBackground;
    public string NameplateBackgroundColorHex;
    public float NameplateBackgroundPaddingX;
    public float NameplateBackgroundPaddingY;
    public float NameplateBackgroundMinWidth;
    public bool NameplateBackgroundAutoSize;
    public float NameplateBackgroundFixedWidth;
    public float NameplateBackgroundFixedHeight;
    public float InterpolationSpeed;
    public float SnapDistance;
    public float RemoteMovingDistanceThreshold;
    public float AnimationRefreshSeconds;
    public float WalkCycleSeconds;
    public bool UseStepMovementPackets;
    public bool UseMoveCoroutinePatch;
    public float StepDistance;
    public float StepDurationSeconds;
    public float StepSendLeadFactor;
    public float StepOriginSnapDistance;
    public bool BattleRequestsEnabled;
    public float BattleRequestDistance;
    public float BattleRequestForwardDot;
    public float BattleRequestTimeoutSeconds;
    public float BattleOverlayCharacterSize;
    public float BattleRequestWindowScale;
    public bool DisableRunInPvP;
    public bool RealBattlesEnabled;
    public float BattleCommandTimeoutSeconds;
    public bool BattleStateSyncEnabled;
    public float BattleStateSendRateSeconds;
    public float BattleHitTimeoutSeconds;
    public bool UseOpponentSpriteInBattleSplash;
    public bool ForceLocalFirstTurn;
    public bool WorldSpawnsEnabled;
    public bool BlockVanillaEncountersWhileConnected;
    public float WorldSpawnInteractDistance;
    public float WorldSpawnScale;
    public float WorldSpawnOffsetX;
    public float WorldSpawnOffsetY;
    public float WorldSpawnFrameSeconds;
    public bool ShowWorldSpawnLabels;
    public float WorldSpawnLabelOffsetY;
    public float WorldSpawnLabelCharacterSize;
    public float SendRateSeconds;
    public string SaveFilePath;
    public bool ReadAppearanceFromSave;
    public float SystemMessagePopupWidth;
    public float SystemMessagePopupHeight;
    public float SystemMessagePopupScale;
    public float SystemMessageTitleOffsetX;
    public float SystemMessageTitleOffsetY;
    public float SystemMessageTitleWidth;
    public float SystemMessageTitleHeight;
    public int SystemMessageTitleFontSize;
    public float SystemMessageBodyOffsetX;
    public float SystemMessageBodyOffsetY;
    public float SystemMessageBodyWidth;
    public float SystemMessageBodyHeight;
    public int SystemMessageBodyFontSize;
    public float SystemMessageBirbOffsetX;
    public float SystemMessageBirbOffsetY;
    public float SystemMessageBirbSize;
    public float SystemMessageButtonOffsetX;
    public float SystemMessageButtonOffsetY;
    public float SystemMessageButtonWidth;
    public float SystemMessageButtonHeight;
}

public sealed class MMOnsterpatchRunner : MonoBehaviour
{
    public static MMOnsterpatchRunner Current { get { return instance; } }
    public string LocalPlayerIdForAio { get { return runtimePlayerId ?? ""; } }
    public bool IsConnectedForAio { get { return connected; } }
    public bool HasServerHandshakeForAio { get { return serverHandshakeComplete; } }
    public bool IsMmoWorldModeActiveForAio { get { return connected && settings != null && settings.WorldSpawnsEnabled; } }
    public bool IsNetworkBusyForAio { get { return connected || wantsConnection || (netThread != null && netThread.IsAlive); } }

    public void ConnectFromAioChatWindow()
    {
        Dbg("AIO chat window requested MMO connect.");
        wantsConnection = true;
        running = true;
        reconnectTimer = 0f;
        sendTimer = 0f;
        StartNetworkThread();
    }

    public void DisconnectFromAioChatWindow()
    {
        Dbg("AIO chat window requested MMO disconnect.");
        wantsConnection = false;
        ShutdownNetwork();
        ClearRemotePlayers();
        ShowServerStatusWindow("Disconnected from Server");
    }


    private static MMOnsterpatchRunner instance;
    private static bool applicationQuitting;

    private MMOnsterpatchSettings settings;
    private ManualLogSource log;
    private string bepInExRoot;
    private string debugLogPath;

    private TcpClient client;
    private StreamReader reader;
    private StreamWriter writer;
    private Thread netThread;
    private volatile bool running;
    private volatile bool connected;
    private volatile bool serverHandshakeComplete;
    private volatile bool wantsConnection;
    private readonly ConcurrentQueue<string> inbound = new ConcurrentQueue<string>();

    private PlayerController player;
    private float sendTimer;
    private float reconnectTimer;
    private float playerFindTimer;
    private string runtimePlayerId;

    private bool localStepStateInitialized;
    private bool localWasMoving;
    private Vector3 localPrevPos;
    private Vector3 localLastStepOrigin;
    private Vector3 localLastStepTarget;
    private float localLastStepSentTime;
    private int localStepSeq;

    private int playerDesign = 0;
    private int playerColor1 = 0;
    private int playerColor2 = 0;

    private readonly Dictionary<string, RemotePlayer> remotePlayers = new Dictionary<string, RemotePlayer>();
    private readonly Dictionary<string, WorldSpawnVisual> worldSpawns = new Dictionary<string, WorldSpawnVisual>();
    private readonly Dictionary<string, RemoteWorldCatchFx> remoteWorldCatchFx = new Dictionary<string, RemoteWorldCatchFx>();
    private float worldSpawnZoneReportTimer;
    private float lastWorldSpawnZoneReportAt;
    private string lastWorldSpawnZoneSignature = "";
    private string lastWorldSpawnZoneScene = "";
    private readonly Dictionary<string, Sprite> followerSpriteCache = new Dictionary<string, Sprite>();
    private Sprite cachedRemotePlayerSprite;
    private Sprite cachedWorldSpawnFallbackSprite;
    private Sprite cachedNameplateBackgroundSprite;

    private string pendingBattleFromId = "";
    private string pendingBattleFromName = "";
    private string pendingBattleTeamPayload = "";
    private float pendingBattleExpiresAt;
    private string outgoingBattleTargetId = "";
    private RemotePlayer pendingOutgoingBattleTarget;
    private int battleDialogMode; // 0 none, 1 incoming request, 2 outgoing confirm, 3 info/ok, 4 outgoing battle type select
    private float battleInfoInputBlockedUntil;
    private string battleDialogTitle = "";
    private string battleDialogMessage = "";
    private string battleDialogHint = "";
    private float battleRequestCooldownUntil;
    private float battleStateSendTimer;
    private GameObject battleOverlayRoot;
    private TextMesh battleOverlayText;
    private TextMesh battleOverlayShadow;
    private string battleOverlayMessage = "";
    private float battleOverlayHideAt;
    private Rect battleRequestWindowRect = new Rect(0f, 0f, 420f, 170f);
    private bool battleRequestWindowStylesReady;
    private Texture2D battleWindowPaperTex;
    private Texture2D battleWindowCardTex;
    private Texture2D battleWindowButtonTex;
    private Texture2D battleWindowButtonHoverTex;
    private Texture2D battleWindowButtonActiveTex;
    private Texture2D battleWindowDarkTex;
    private GUIStyle battleWindowStyle;
    private GUIStyle battleWindowTitleStyle;
    private GUIStyle battleWindowLabelStyle;
    private GUIStyle battleWindowTinyLabelStyle;
    private GUIStyle battleWindowButtonStyle;
    private GUIStyle battleWindowCardStyle;

    // Native-styled server status popup. This is used only for Connect/Disconnect
    // info windows so battle request/player-busy dialog behavior stays untouched.
    private bool serverStatusUseNativePopup;
    private GameObject serverStatusNativeRoot;
    private Image serverStatusNativePanelImage;
    private Image serverStatusNativeTitleImage;
    private Image serverStatusNativeButtonImage;
    private Button serverStatusNativeButton;
    private TextMeshProUGUI serverStatusNativeTitleText;
    private TextMeshProUGUI serverStatusNativeMessageText;
    private TextMeshProUGUI serverStatusNativeButtonText;
    private Image serverStatusNativeBirbImage;
    private TMP_FontAsset serverStatusNativeFont;
    private Sprite serverStatusNativePanelSprite;
    private Sprite serverStatusNativeButtonSprite;
    private Sprite serverStatusNativeBirbSprite;

    private class RemotePlayer
    {
        public string id;
        public string name;
        public GameObject obj;
        public GameObject visualRoot;
        public SpriteRenderer fallbackRenderer;
        public GameObject nameplateRoot;
        public SpriteRenderer nameplateBackgroundRenderer;
        public TextMesh nameplateText;
        public TextMesh nameplateShadow;
        public SpriteAnimator spriteAnimator;
        public Animation visualAnimation;
        public Vector3 targetPos;
        public Vector3 lastRenderedPos;
        public float lastSeen;
        public float animationRefreshTimer;
        public float walkCycleTimer;
        public bool stepActive;
        public Vector3 stepOrigin;
        public Vector3 stepTarget;
        public float stepTimer;
        public float stepDuration;
        public bool animCurrentlyMoving;
        public int animLastFacing = -999;
        public bool usingFallback;
        public int design = -999;
        public int color1 = -999;
        public int color2 = -999;
        public int facing = -999;
        public bool moving;
        public bool ridingBroom;
        public bool lastRidingBroom;
        public bool waterWalking;
        public bool lastWaterWalking;
        public bool jumpingOrBouncing;
        public bool lastJumpingOrBouncing;
        public string bouncingName = "";
        public GameObject remoteShadowObj;
        public GameObject remoteWaterWalkObj;
        public GameObject remoteSpellCastObj;
        public GameObject followerObj;
        public SpriteRenderer followerRenderer;
        public string followerSpriteName = "";
        public bool followerActive;
        public Vector3 followerTargetPos;
        public int followerFacing;
        public bool followerMoving;
        public bool followerFlipX;
    }

    private class WorldSpawnVisual
    {
        public string id;
        public string scene;
        public GameObject obj;
        public SpriteRenderer renderer;
        public TextMesh label;
        public Vector3 pos;
        public MonObject monObject;
        public MMOWorldSpawnMarker marker;
        public bool usesVanillaObject;
        public Sprite[] frames;
        public int frameIndex;
        public float frameTimer;
        public int monId;
        public string monKey = "";
        public bool shiny;
        public int level;
        public string state = "available";
        public string lockState = "public";
        public string personalOwnerId = "";
        public string monSaveB64 = "";
        public float lastSeen;
        public bool claimPending;
    }

    private class RemoteWorldCatchFx
    {
        public string spawnId;
        public string ownerId;
        public GameObject diamond;
        public float startedAt;
    }

    private class LocalFollowerNetState
    {
        public bool enabled;
        public Vector3 pos;
        public int facing;
        public bool moving;
        public string spriteName = "";
        public bool flipX;
    }

    public static void Ensure(MMOnsterpatchSettings settings, ManualLogSource logger, string bepInExRootPath)
    {
        if (instance != null)
        {
            instance.Dbg("Ensure called but runner already exists.");
            return;
        }

        GameObject go = new GameObject("Monsterpatch_MMOnsterpatch_Runtime");
        go.hideFlags = HideFlags.HideAndDontSave;
        DontDestroyOnLoad(go);

        instance = go.AddComponent<MMOnsterpatchRunner>();
        instance.Init(settings, logger, bepInExRootPath);
    }

    private void Init(MMOnsterpatchSettings s, ManualLogSource logger, string root)
    {
        settings = s;
        log = logger;
        bepInExRoot = root;
        debugLogPath = Path.Combine(bepInExRoot, "mmonsterpatch_debug.log");

        File.AppendAllText(debugLogPath, "\n==== RemotePlayer RUNTIME Init " + DateTime.Now + " ====\n");

        runtimePlayerId = string.IsNullOrWhiteSpace(settings.PlayerId)
            ? "mpg-" + System.Diagnostics.Process.GetCurrentProcess().Id + "-" + Guid.NewGuid().ToString("N").Substring(0, 8)
            : settings.PlayerId.Trim();

        TryReadSaveAppearance();

        Dbg("Loaded runtime runner MMO World Spawn Test v0.3");
        Dbg("Runtime GameObject: " + gameObject.name);
        Dbg("Local ID: " + runtimePlayerId);
        Dbg("Server: " + settings.ServerHost + ":" + settings.ServerPort + ", Cluster: " + settings.ClusterId);

        wantsConnection = settings.AutoConnectOnStartup;
        running = wantsConnection;
        if (wantsConnection)
            StartNetworkThread();
        else
            Dbg("AutoConnectOnStartup is false; starting offline until the AIO chat window Connect button is pressed.");
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        Dbg("Runner OnEnable");
        running = wantsConnection;
    }

    private void OnDisable()
    {
        Dbg("Runner OnDisable ignored; keeping network alive.");
    }

    private void OnDestroy()
    {
        Dbg("Runner OnDestroy. applicationQuitting=" + applicationQuitting);
        if (instance == this)
            instance = null;

        if (applicationQuitting)
            ShutdownNetwork();
        else
            Dbg("Runner destroyed unexpectedly; network shutdown skipped for diagnosis.");
    }

    private void OnApplicationQuit()
    {
        Dbg("Runner OnApplicationQuit");
        applicationQuitting = true;
        wantsConnection = false;
        ShutdownNetwork();
    }

    private void Update()
    {
        if (applicationQuitting) return;

        TryFindPlayer();
        if (!settings.UseMoveCoroutinePatch)
            DetectAndSendLocalStep();

        if (wantsConnection && !connected)
        {
            reconnectTimer -= Time.deltaTime;
            if (reconnectTimer <= 0f)
            {
                reconnectTimer = 3f;
                Dbg("Connection wanted but not connected; starting/retrying network thread.");
                StartNetworkThread();
            }
        }

        string line;
        while (inbound.TryDequeue(out line))
            HandleServerLine(line);

        if (!UpdateWorldSpawnInput())
            UpdateBattleRequestInput();
        MaybeSendWorldSpawnZones();
        UpdateWorldSpawnVisuals();
        UpdateRemoteWorldCatchFxTimeouts();
        if (IsMmoWorldModeActiveForAio)
            MMOnsterpatchMmoWorldEncounterSuppressor.ClearEncounterObjects();
        UpdateBattleOverlay();
        UpdateBattleStateSync();

        foreach (var g in remotePlayers.Values.ToList())
        {
            if (g.obj == null) continue;

            UpdateRemotePlayerMovementAndAnimation(g);
            UpdateRemoteFollowerVisual(g);
            RefreshNameplate(g);

            if (Time.time - g.lastSeen > 10f)
            {
                Destroy(g.obj);
                remotePlayers.Remove(g.id);
            }
        }

        if (connected)
        {
            sendTimer += Time.deltaTime;
            if (sendTimer >= Mathf.Max(0.02f, settings.SendRateSeconds))
            {
                sendTimer = 0f;
                SendLocalStateOrHeartbeat();
            }
        }
    }

    


    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    

    
    
    
    
    
    
    
    
    private void ClearRemotePlayers()
    {
        try
        {
            foreach (RemotePlayer rp in remotePlayers.Values.ToList())
            {
                if (rp != null && rp.obj != null)
                    Destroy(rp.obj);
            }
            remotePlayers.Clear();
            ClearWorldSpawns();
            pendingBattleFromId = "";
            pendingBattleFromName = "";
            pendingBattleTeamPayload = "";
            outgoingBattleTargetId = "";
            pendingOutgoingBattleTarget = null;
            battleDialogMode = 0;
        }
        catch { }
    }

    
    private void UpdateRemotePlayerMovementAndAnimation(RemotePlayer remotePlayer)
    {
        if (remotePlayer == null || remotePlayer.obj == null) return;

        if (settings.UseStepMovementPackets && remotePlayer.stepActive)
        {
            remotePlayer.stepTimer += Time.deltaTime;
            float duration = Mathf.Max(0.05f, remotePlayer.stepDuration);
            float progress = Mathf.Clamp01(remotePlayer.stepTimer / duration);

            Vector3 next = Vector3.Lerp(remotePlayer.stepOrigin, remotePlayer.stepTarget, progress);
            next.z = next.y * 0.01f;
            remotePlayer.obj.transform.position = WithRemoteSortZ(next);
            ApplyRemotePlayerRenderPriority(remotePlayer);

            DriveRemotePlayerAnimation(remotePlayer, true, progress);

            if (progress >= 1f)
            {
                remotePlayer.stepActive = false;
                remotePlayer.targetPos = remotePlayer.stepTarget;
                remotePlayer.obj.transform.position = WithRemoteSortZ(remotePlayer.stepTarget);
                remotePlayer.walkCycleTimer = 0f;
            }

            remotePlayer.lastRenderedPos = remotePlayer.obj.transform.position;
            return;
        }

        Vector3 current = remotePlayer.obj.transform.position;
        Vector3 target = remotePlayer.targetPos;
        target.z = target.y * 0.01f;

        float distance = Vector2.Distance(new Vector2(current.x, current.y), new Vector2(target.x, target.y));

        if (distance > Mathf.Max(0.01f, settings.SnapDistance))
        {
            remotePlayer.obj.transform.position = WithRemoteSortZ(target);
        }
        else
        {
            float t = 1f - Mathf.Exp(-Mathf.Max(0.01f, settings.InterpolationSpeed) * Time.deltaTime);
            Vector3 next = Vector3.Lerp(current, target, t);
            next.z = next.y * 0.01f;
            remotePlayer.obj.transform.position = WithRemoteSortZ(next);
        }

        ApplyRemotePlayerRenderPriority(remotePlayer);

        bool visuallyMoving =
            remotePlayer.moving ||
            distance > Mathf.Max(0.0001f, settings.RemoteMovingDistanceThreshold);

        DriveRemotePlayerAnimation(remotePlayer, visuallyMoving, -1f);
        remotePlayer.lastRenderedPos = remotePlayer.obj.transform.position;
    }

    private void DriveRemotePlayerAnimation(RemotePlayer remotePlayer, bool movingNow, float forcedProgress)
    {
        if (remotePlayer == null) return;

        if (remotePlayer.spriteAnimator == null && remotePlayer.visualRoot != null)
            remotePlayer.spriteAnimator = remotePlayer.visualRoot.GetComponentInChildren<SpriteAnimator>(true);

        if (remotePlayer.spriteAnimator == null)
            return;

        bool dirChanged = remotePlayer.animLastFacing != remotePlayer.facing;
        bool stateChanged = remotePlayer.animCurrentlyMoving != movingNow;
        bool broomChanged = remotePlayer.lastRidingBroom != remotePlayer.ridingBroom;

        try
        {
            remotePlayer.spriteAnimator.curDir = remotePlayer.facing;
            remotePlayer.spriteAnimator.primaryColor = remotePlayer.color1;
            remotePlayer.spriteAnimator.secondaryColor = remotePlayer.color2;
            remotePlayer.spriteAnimator.isMoving = movingNow;

            if (broomChanged)
            {
                remotePlayer.walkCycleTimer = 0f;
                ApplyRemoteVisualState(remotePlayer, remotePlayer.ridingBroom, remotePlayer.waterWalking, remotePlayer.jumpingOrBouncing, remotePlayer.bouncingName, "drive-broom");
                remotePlayer.animCurrentlyMoving = false;
                remotePlayer.animLastFacing = -999;
                Dbg("Remote broom state changed for " + remotePlayer.name + " riding=" + remotePlayer.ridingBroom);
            }

            if (remotePlayer.ridingBroom)
            {
                ApplyRemotePlayerFrame(remotePlayer, true, 0f);
                remotePlayer.animCurrentlyMoving = movingNow;
                remotePlayer.animLastFacing = remotePlayer.facing;
                remotePlayer.lastRidingBroom = true;
                return;
            }

            // Do not call SpriteAnimator.PlayIdle/PlayMoving/ShowMoveProgress for remote normal frames.
            // Those methods check the local client's static PlayerController.ridingBroom, which can make
            // a remote player stay on broom frames after that remote dismounts. Drive the sheet indices directly.
            float progress = 0f;
            if (movingNow)
            {
                if (forcedProgress >= 0f)
                {
                    progress = Mathf.Clamp01(forcedProgress);
                }
                else
                {
                    if (stateChanged || dirChanged || broomChanged)
                        remotePlayer.walkCycleTimer = 0f;

                    remotePlayer.walkCycleTimer += Time.deltaTime;
                    float cycleSeconds = Mathf.Max(0.05f, settings.WalkCycleSeconds);
                    progress = remotePlayer.walkCycleTimer / cycleSeconds;

                    if (progress >= 1f)
                    {
                        remotePlayer.walkCycleTimer = remotePlayer.walkCycleTimer % cycleSeconds;
                        progress = remotePlayer.walkCycleTimer / cycleSeconds;
                    }
                }
            }
            else
            {
                remotePlayer.walkCycleTimer = 0f;
                progress = 0f;
            }

            ApplyRemotePlayerFrame(remotePlayer, movingNow, progress);

            remotePlayer.animCurrentlyMoving = movingNow;
            remotePlayer.animLastFacing = remotePlayer.facing;
            remotePlayer.lastRidingBroom = false;
        }
        catch (Exception ex)
        {
            Dbg("DriveRemotePlayerAnimation SpriteAnimator failed: " + ex.Message);
        }
    }

    private void ApplyRemotePlayerFrame(RemotePlayer remotePlayer, bool movingNow, float progress)
    {
        if (remotePlayer == null || remotePlayer.spriteAnimator == null)
            return;

        try
        {
            SpriteAnimator sa = remotePlayer.spriteAnimator;
            if (sa.spriteScriptableObject == null)
                return;

            int dir = Mathf.Clamp(remotePlayer.facing, 0, 3);
            int primary = Mathf.Max(0, remotePlayer.color1);
            int secondary = Mathf.Max(0, remotePlayer.color2);

            if (remotePlayer.ridingBroom)
            {
                if (sa.spriteScriptableObject.spriteSheetBroom == null)
                    return;

                int primaryIndex = primary * 2 + dir * 18;
                int accentIndex = secondary * 2 + dir * 18 + 72;

                if (sa.spriteRenderer != null && primaryIndex >= 0 && primaryIndex < sa.spriteScriptableObject.spriteSheetBroom.Length)
                    sa.spriteRenderer.sprite = sa.spriteScriptableObject.spriteSheetBroom[primaryIndex];

                if (sa.spriteRendererAccent != null && accentIndex >= 0 && accentIndex < sa.spriteScriptableObject.spriteSheetBroom.Length)
                    sa.spriteRendererAccent.sprite = sa.spriteScriptableObject.spriteSheetBroom[accentIndex];

                return;
            }

            if (sa.spriteScriptableObject.spriteSheet == null)
                return;

            int frame = 0;
            if (movingNow)
            {
                progress = Mathf.Clamp01(progress);
                frame = (progress < 0.25f) ? 1 : ((progress < 0.5f) ? 0 : ((progress < 0.75f) ? 2 : 0));
            }

            int normalIndex = primary * 3 + dir * 27 + frame;
            int accentNormalIndex = secondary * 3 + dir * 27 + 108 + frame;

            if (sa.spriteRenderer != null && normalIndex >= 0 && normalIndex < sa.spriteScriptableObject.spriteSheet.Length)
                sa.spriteRenderer.sprite = sa.spriteScriptableObject.spriteSheet[normalIndex];

            if (sa.spriteRendererAccent != null && accentNormalIndex >= 0 && accentNormalIndex < sa.spriteScriptableObject.spriteSheet.Length)
                sa.spriteRendererAccent.sprite = sa.spriteScriptableObject.spriteSheet[accentNormalIndex];
        }
        catch (Exception ex)
        {
            Dbg("ApplyRemotePlayerFrame failed: " + ex.Message);
        }
    }

    private void ApplyRemoteBroomFrame(RemotePlayer remotePlayer)
    {
        if (remotePlayer == null)
            return;
        bool old = remotePlayer.ridingBroom;
        try
        {
            remotePlayer.ridingBroom = true;
            ApplyRemotePlayerFrame(remotePlayer, true, 0f);
        }
        finally
        {
            remotePlayer.ridingBroom = old;
        }
    }

    private void TryPlayRemotePlayerRootAnimation(RemotePlayer remotePlayer, string animationName)
    {
        if (remotePlayer == null || string.IsNullOrWhiteSpace(animationName))
            return;

        try
        {
            Animation anim = remotePlayer.visualAnimation;
            if (anim == null && remotePlayer.visualRoot != null)
                anim = remotePlayer.visualRoot.GetComponent<Animation>();

            if (anim != null)
            {
                anim.Play(animationName);
                remotePlayer.visualAnimation = anim;
            }
        }
        catch { }
    }



    private void DetectAndSendLocalStep()
    {
        if (!settings.UseStepMovementPackets || !connected || player == null)
            return;

        Vector3 cur = player.transform.position;
        cur.z = cur.y * 0.01f;

        if (!localStepStateInitialized)
        {
            localStepStateInitialized = true;
            localPrevPos = cur;
            localLastStepOrigin = cur;
            localLastStepTarget = cur;
            localLastStepSentTime = -999f;
            localWasMoving = PlayerController.isMoving;
            return;
        }

        bool moving = PlayerController.isMoving;

        bool shouldSend = false;
        Vector3 origin = localPrevPos;

        if (moving && !localWasMoving)
        {
            shouldSend = true;
            origin = localPrevPos;
        }
        else if (moving && localWasMoving)
        {
            float duration = Mathf.Max(0.05f, settings.StepDurationSeconds);
            float lead = Mathf.Clamp(settings.StepSendLeadFactor, 0.25f, 1.25f);
            bool enoughTime = Time.time - localLastStepSentTime >= duration * lead;

            Vector2 cur2 = new Vector2(cur.x, cur.y);
            Vector2 lastTarget2 = new Vector2(localLastStepTarget.x, localLastStepTarget.y);
            bool nearLastTarget = Vector2.Distance(cur2, lastTarget2) <= Mathf.Max(0.02f, settings.StepDistance * 0.35f);

            if (enoughTime && nearLastTarget)
            {
                shouldSend = true;
                origin = localLastStepTarget;
            }
        }

        if (shouldSend)
        {
            int facing = PlayerController.facingDir;
            Vector3 dir = FacingToVector(facing);
            float step = Mathf.Max(0.01f, settings.StepDistance);

            Vector3 target = origin + dir * step;
            origin.z = origin.y * 0.01f;
            target.z = target.y * 0.01f;

            SendLocalStep(origin, target, facing, Mathf.Max(0.05f, settings.StepDurationSeconds));
            localLastStepOrigin = origin;
            localLastStepTarget = target;
            localLastStepSentTime = Time.time;
        }

        localWasMoving = moving;
        localPrevPos = cur;
    }

    private void SendLocalStep(Vector3 origin, Vector3 target, int facing, float duration)
    {
        if (player == null)
            return;

        RefreshLocalAppearanceFromRuntime();

        string scene = GetLocalSceneName();

        localStepSeq++;

        SendLine("STEP|" +
            Escape(runtimePlayerId) + "|" +
            Escape(settings.PlayerName) + "|" +
            Escape(settings.ClusterId) + "|" +
            Escape(scene) + "|" +
            F(origin.x) + "|" +
            F(origin.y) + "|" +
            F(target.x) + "|" +
            F(target.y) + "|" +
            facing + "|" +
            F(duration) + "|" +
            playerDesign + "|" +
            playerColor1 + "|" +
            playerColor2 + "|" +
            localStepSeq + "|" +
            (PlayerController.ridingBroom ? "1" : "0"));
    }

    public void SendExactLocalMoveStep(PlayerController pc, Vector3 direction)
    {
        if (!settings.UseStepMovementPackets || !settings.UseMoveCoroutinePatch || !connected || pc == null)
            return;

        if (direction == Vector3.zero)
            return;

        try
        {
            bool canMove = false;
            try
            {
                canMove = pc.CanMove(direction);
            }
            catch
            {
                canMove = true;
            }

            bool skyBalloonMove = false;
            try
            {
                skyBalloonMove = GameScript.inCutscene && pc.gameScript != null && pc.gameScript.ridingSkyBalloon;
            }
            catch { }

            if (!canMove && !skyBalloonMove)
                return;

            Vector3 origin = pc.transform.position;
            origin.z = origin.y * 0.01f;

            float stepDistance = Mathf.Max(0.01f, settings.StepDistance);
            try
            {
                stepDistance *= Mathf.Max(1, pc.moveMod + 1);
            }
            catch { }

            Vector3 target = origin + direction.normalized * stepDistance;
            target.z = target.y * 0.01f;

            int facing = DirectionToFacing(direction);
            float duration = ReadPlayerMoveDuration(pc);
            if (duration <= 0.01f)
                duration = Mathf.Max(0.05f, settings.StepDurationSeconds);

            SendLocalStep(origin, target, facing, duration);
        }
        catch (Exception ex)
        {
            Dbg("SendExactLocalMoveStep failed: " + ex.Message);
        }
    }

    private float ReadPlayerMoveDuration(PlayerController pc)
    {
        try
        {
            FieldInfo f = typeof(PlayerController).GetField("timeTo", BindingFlags.Instance | BindingFlags.NonPublic);
            if (f != null)
            {
                object value = f.GetValue(pc);
                if (value is float)
                    return (float)value;
            }
        }
        catch { }

        try
        {
            if (PlayerController.ridingBroom)
            {
                FieldInfo f = typeof(PlayerController).GetField("timeToBroom", BindingFlags.Instance | BindingFlags.NonPublic);
                if (f != null && f.GetValue(pc) is float) return (float)f.GetValue(pc);
            }
            if (PlayerController.isRunning)
            {
                FieldInfo f = typeof(PlayerController).GetField("timeToRun", BindingFlags.Instance | BindingFlags.NonPublic);
                if (f != null && f.GetValue(pc) is float) return (float)f.GetValue(pc);
            }
            else
            {
                FieldInfo f = typeof(PlayerController).GetField("timeToMove", BindingFlags.Instance | BindingFlags.NonPublic);
                if (f != null && f.GetValue(pc) is float) return (float)f.GetValue(pc);
            }
        }
        catch { }

        return Mathf.Max(0.05f, settings.StepDurationSeconds);
    }

    private static int DirectionToFacing(Vector3 direction)
    {
        if (Mathf.Abs(direction.y) > Mathf.Abs(direction.x))
            return direction.y > 0f ? 2 : 0;
        return direction.x > 0f ? 3 : 1;
    }

    private static Vector3 FacingToVector(int facing)
    {
        switch (facing)
        {
            case 0: return new Vector3(0f, -1f, 0f);
            case 1: return new Vector3(-1f, 0f, 0f);
            case 2: return new Vector3(0f, 1f, 0f);
            case 3: return new Vector3(1f, 0f, 0f);
            default: return Vector3.zero;
        }
    }


    private void OnGUI()
    {
        if (!IsBattleDialogOpen())
            return;

        try
        {
            if (battleDialogMode == 1 && Time.time > pendingBattleExpiresAt)
            {
                ClearPendingBattleRequest();
                return;
            }

            if (battleDialogMode == 3 && serverStatusUseNativePopup && EnsureNativeServerStatusPopup())
            {
                RefreshNativeServerStatusPopup();
                return;
            }

            EnsureBattleRequestGuiStyles();

            float s = Mathf.Max(0.5f, settings.BattleRequestWindowScale);
            float w = 420f;
            float h = 174f;
            battleRequestWindowRect.width = w;
            battleRequestWindowRect.height = h;
            battleRequestWindowRect.x = Mathf.Round(((Screen.width / s) - w) * 0.5f);
            battleRequestWindowRect.y = Mathf.Round(((Screen.height / s) - h) * 0.5f);

            Matrix4x4 oldMatrix = GUI.matrix;
            if (Math.Abs(s - 1f) > 0.001f)
                GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(s, s, 1f));

            DrawBattleRequestWindowBacking(battleRequestWindowRect);
            battleRequestWindowRect = GUI.Window(6152805, battleRequestWindowRect, DrawBattleRequestWindow, GUIContent.none, battleWindowStyle);
            ConsumeBattleRequestWindowEvent();

            GUI.matrix = oldMatrix;
        }
        catch (Exception ex)
        {
            Dbg("Battle request OnGUI failed: " + ex.Message);
        }
    }

    private void DrawBattleRequestWindow(int id)
    {
        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal();
        GUILayout.Label(string.IsNullOrWhiteSpace(battleDialogTitle) ? "Battle Request" : battleDialogTitle, battleWindowTitleStyle, GUILayout.Height(24f));
        GUILayout.EndHorizontal();

        DrawBattleRequestHorizontalRule();

        GUILayout.BeginVertical(battleWindowCardStyle);
        GUILayout.Label(string.IsNullOrWhiteSpace(battleDialogMessage) ? "Battle request." : battleDialogMessage, battleWindowLabelStyle);
        GUILayout.Space(3f);
        if (!string.IsNullOrWhiteSpace(battleDialogHint))
            GUILayout.Label(battleDialogHint, battleWindowTinyLabelStyle);
        GUILayout.EndVertical();

        GUILayout.Space(4f);

        if (battleDialogMode == 1)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Accept", battleWindowButtonStyle, GUILayout.Height(32f)))
                AcceptPendingBattleRequest();

            if (GUILayout.Button("Decline", battleWindowButtonStyle, GUILayout.Height(32f)))
                DeclinePendingBattleRequest();
            GUILayout.EndHorizontal();
        }
        else if (battleDialogMode == 2)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Yes", battleWindowButtonStyle, GUILayout.Height(32f)))
                ConfirmOutgoingBattleRequest();

            if (GUILayout.Button("No", battleWindowButtonStyle, GUILayout.Height(32f)))
                ClearOutgoingBattleConfirm();
            GUILayout.EndHorizontal();
        }
        else if (battleDialogMode == 4)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Normal", battleWindowButtonStyle, GUILayout.Height(32f)))
                ConfirmOutgoingBattleTypeNormal();

            if (GUILayout.Button("Ranked", battleWindowButtonStyle, GUILayout.Height(32f)))
                ConfirmOutgoingBattleTypeRanked();
            GUILayout.EndHorizontal();

            GUILayout.Space(5f);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Cancel", battleWindowButtonStyle, GUILayout.Width(150f), GUILayout.Height(32f)))
                ClearOutgoingBattleConfirm();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
        else
        {
            if (GUILayout.Button("Okay", battleWindowButtonStyle, GUILayout.Height(32f)))
                ClearBattleInfoWindow();
        }

        GUILayout.EndVertical();
    }

    private void EnsureBattleRequestGuiStyles()
    {
        if (battleRequestWindowStylesReady)
            return;

        Color32 paper = new Color32(239, 229, 194, 255);
        Color32 card = new Color32(247, 239, 210, 255);
        Color32 button = new Color32(239, 229, 194, 255);
        Color32 buttonHover = new Color32(247, 239, 210, 255);
        Color32 buttonActive = new Color32(225, 215, 181, 255);
        Color32 dark = new Color32(44, 30, 25, 255);
        Color darkText = new Color32(53, 34, 34, 255);
        Font nativeGuiFont = FindNativeBattleImGuiFont();

        battleWindowPaperTex = MakeBattleWindowDoubleBorderedTex(paper, dark);
        battleWindowCardTex = MakeBattleWindowBorderedTex(card, dark, 2);
        battleWindowButtonTex = MakeBattleWindowBorderedTex(button, dark, 2);
        battleWindowButtonHoverTex = MakeBattleWindowBorderedTex(buttonHover, dark, 2);
        battleWindowButtonActiveTex = MakeBattleWindowBorderedTex(buttonActive, dark, 2);
        battleWindowDarkTex = MakeBattleWindowSolidTex(dark);

        battleWindowStyle = new GUIStyle(GUI.skin.window);
        SetBattleWindowStyleStates(battleWindowStyle, battleWindowPaperTex, darkText);
        battleWindowStyle.padding = new RectOffset(14, 14, 12, 12);
        battleWindowStyle.margin = new RectOffset(0, 0, 0, 0);
        battleWindowStyle.border = new RectOffset(6, 6, 6, 6);
        battleWindowStyle.overflow = new RectOffset(0, 0, 0, 0);

        battleWindowTitleStyle = new GUIStyle(GUI.skin.label);
        battleWindowTitleStyle.fontSize = 20;
        battleWindowTitleStyle.fontStyle = FontStyle.Bold;
        battleWindowTitleStyle.alignment = TextAnchor.MiddleCenter;
        battleWindowTitleStyle.normal.textColor = darkText;

        battleWindowLabelStyle = new GUIStyle(GUI.skin.label);
        battleWindowLabelStyle.fontSize = 17;
        battleWindowLabelStyle.fontStyle = FontStyle.Bold;
        battleWindowLabelStyle.wordWrap = true;
        battleWindowLabelStyle.alignment = TextAnchor.MiddleCenter;
        battleWindowLabelStyle.normal.textColor = darkText;

        battleWindowTinyLabelStyle = new GUIStyle(GUI.skin.label);
        battleWindowTinyLabelStyle.fontSize = 13;
        battleWindowTinyLabelStyle.wordWrap = true;
        battleWindowTinyLabelStyle.alignment = TextAnchor.MiddleCenter;
        battleWindowTinyLabelStyle.normal.textColor = darkText;

        battleWindowButtonStyle = new GUIStyle(GUI.skin.button);
        SetBattleWindowStyleStates(battleWindowButtonStyle, battleWindowButtonTex, darkText);
        battleWindowButtonStyle.hover.background = battleWindowButtonHoverTex;
        battleWindowButtonStyle.onHover.background = battleWindowButtonHoverTex;
        battleWindowButtonStyle.focused.background = battleWindowButtonHoverTex;
        battleWindowButtonStyle.onFocused.background = battleWindowButtonHoverTex;
        battleWindowButtonStyle.active.background = battleWindowButtonActiveTex;
        battleWindowButtonStyle.onActive.background = battleWindowButtonActiveTex;
        battleWindowButtonStyle.fontSize = 16;
        battleWindowButtonStyle.fontStyle = FontStyle.Bold;
        battleWindowButtonStyle.alignment = TextAnchor.MiddleCenter;
        battleWindowButtonStyle.padding = new RectOffset(8, 8, 5, 5);
        battleWindowButtonStyle.margin = new RectOffset(4, 4, 2, 2);

        battleWindowCardStyle = new GUIStyle(GUI.skin.box);
        SetBattleWindowStyleStates(battleWindowCardStyle, battleWindowCardTex, darkText);
        battleWindowCardStyle.border = new RectOffset(2, 2, 2, 2);
        battleWindowCardStyle.padding = new RectOffset(10, 10, 10, 10);
        battleWindowCardStyle.margin = new RectOffset(0, 0, 4, 5);

        ApplyNativeBattleFont(nativeGuiFont, battleWindowStyle, battleWindowTitleStyle, battleWindowLabelStyle, battleWindowTinyLabelStyle, battleWindowButtonStyle, battleWindowCardStyle);

        battleRequestWindowStylesReady = true;
    }

    private static void ApplyNativeBattleFont(Font font, params GUIStyle[] styles)
    {
        if (font == null || styles == null)
            return;

        for (int i = 0; i < styles.Length; i++)
        {
            if (styles[i] != null)
                styles[i].font = font;
        }
    }

    private static Font FindNativeBattleImGuiFont()
    {
        try
        {
            TMP_Text[] texts = Resources.FindObjectsOfTypeAll<TMP_Text>();
            if (texts != null)
            {
                for (int i = 0; i < texts.Length; i++)
                {
                    Font f = ExtractBattleSourceFont(texts[i] != null ? texts[i].font : null);
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
                    Font f = ExtractBattleSourceFont(assets[i]);
                    if (f != null) return f;
                }
            }
        }
        catch { }

        return null;
    }

    private static Font ExtractBattleSourceFont(TMP_FontAsset asset)
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

    private static Texture2D MakeBattleWindowSolidTex(Color color)
    {
        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, color);
        tex.Apply(false, true);
        return tex;
    }

    private static Texture2D MakeBattleWindowBorderedTex(Color fill, Color border, int borderPx)
    {
        const int size = 16;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool edge = x < borderPx || y < borderPx || x >= size - borderPx || y >= size - borderPx;
                tex.SetPixel(x, y, edge ? border : fill);
            }
        }

        tex.filterMode = FilterMode.Point;
        tex.Apply(false, true);
        return tex;
    }

    private static Texture2D MakeBattleWindowDoubleBorderedTex(Color fill, Color border)
    {
        const int size = 16;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color gap = fill;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool outer = x < 2 || y < 2 || x >= size - 2 || y >= size - 2;
                bool inner = x >= 4 && x < 6 || y >= 4 && y < 6 || x < size - 4 && x >= size - 6 || y < size - 4 && y >= size - 6;
                tex.SetPixel(x, y, outer || inner ? border : gap);
            }
        }

        tex.filterMode = FilterMode.Point;
        tex.Apply(false, true);
        return tex;
    }

    private static void SetBattleWindowStyleStates(GUIStyle style, Texture2D background, Color textColor)
    {
        SetBattleWindowStyleState(style.normal, background, textColor);
        SetBattleWindowStyleState(style.hover, background, textColor);
        SetBattleWindowStyleState(style.active, background, textColor);
        SetBattleWindowStyleState(style.focused, background, textColor);
        SetBattleWindowStyleState(style.onNormal, background, textColor);
        SetBattleWindowStyleState(style.onHover, background, textColor);
        SetBattleWindowStyleState(style.onActive, background, textColor);
        SetBattleWindowStyleState(style.onFocused, background, textColor);
    }

    private static void SetBattleWindowStyleState(GUIStyleState state, Texture2D background, Color textColor)
    {
        state.background = background;
        state.textColor = textColor;
    }

    private void DrawBattleRequestWindowBacking(Rect r)
    {
        if (battleWindowDarkTex == null || battleWindowPaperTex == null)
            return;

        GUI.DrawTexture(new Rect(r.x, r.y, r.width, r.height), battleWindowPaperTex);
    }

    private void DrawBattleRequestHorizontalRule()
    {
        Rect r = GUILayoutUtility.GetRect(1f, 2f, GUILayout.ExpandWidth(true));
        GUI.DrawTexture(new Rect(r.x + 6f, r.y, Mathf.Max(0f, r.width - 12f), 2f), battleWindowDarkTex);
    }

    private void ConsumeBattleRequestWindowEvent()
    {
        try
        {
            Event e = Event.current;
            if (e == null || !IsBattleDialogOpen())
                return;

            float s = Mathf.Max(0.5f, settings.BattleRequestWindowScale);
            Vector2 p = e.mousePosition;
            if (Math.Abs(s - 1f) > 0.001f)
                p = new Vector2(p.x / s, p.y / s);

            if (!battleRequestWindowRect.Contains(p))
                return;

            if (e.type == EventType.MouseDown ||
                e.type == EventType.MouseUp ||
                e.type == EventType.MouseDrag ||
                e.type == EventType.ScrollWheel)
            {
                e.Use();
            }
        }
        catch { }
    }

    private void TryFindPlayer()
    {
        if (player != null) return;

        playerFindTimer -= Time.deltaTime;
        if (playerFindTimer > 0f) return;
        playerFindTimer = 2f;

        try
        {
            player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                Dbg("Found PlayerController active: " + GetPath(player.gameObject));
                return;
            }

            PlayerController[] all = Resources.FindObjectsOfTypeAll<PlayerController>();
            if (all != null && all.Length > 0)
            {
                Dbg("Resources found PlayerController count: " + all.Length);
                foreach (var pc in all)
                {
                    if (pc == null || pc.gameObject == null) continue;
                    Dbg("Candidate PlayerController: activeInHierarchy=" + pc.gameObject.activeInHierarchy + " path=" + GetPath(pc.gameObject));
                    if (pc.gameObject.activeInHierarchy)
                    {
                        player = pc;
                        Dbg("Selected active Resource PlayerController: " + GetPath(player.gameObject));
                        return;
                    }
                }
            }
            else
            {
                Dbg("No PlayerController found yet.");
            }
        }
        catch (Exception ex)
        {
            Dbg("TryFindPlayer error: " + ex);
        }
    }

    private void StartNetworkThread()
    {
        if (applicationQuitting) return;
        if (!wantsConnection)
        {
            Dbg("StartNetworkThread ignored because wantsConnection=false.");
            return;
        }

        if (netThread != null && netThread.IsAlive)
        {
            Dbg("Network thread already alive.");
            return;
        }

        running = true;
        netThread = new Thread(NetworkLoop);
        netThread.IsBackground = true;
        netThread.Start();
        Dbg("Network thread started.");
    }

    private void NetworkLoop()
    {
        try
        {
            connected = false;
            serverHandshakeComplete = false;
            Dbg("Connecting to " + settings.ServerHost + ":" + settings.ServerPort + "...");
            client = new TcpClient();
            client.NoDelay = true;
            client.Connect(settings.ServerHost, settings.ServerPort);

            var stream = client.GetStream();
            reader = new StreamReader(stream, Encoding.UTF8);
            writer = new StreamWriter(stream, new UTF8Encoding(false));
            writer.AutoFlush = true;
            writer.NewLine = "\n";

            connected = true;
            Dbg("Connected.");
            inbound.Enqueue("LOCALCONNECTED|");
            SendHello();

            while (running && !applicationQuitting && client != null && client.Connected)
            {
                string line = reader.ReadLine();
                if (line == null) break;
                inbound.Enqueue(line);
            }
        }
        catch (Exception ex)
        {
            if (!applicationQuitting)
            {
                Dbg("Network error: " + ex.ToString());
                if (wantsConnection)
                    inbound.Enqueue("LOCALERR|" + Escape(ex.Message));
            }
        }
        finally
        {
            connected = false;
            serverHandshakeComplete = false;
            try { if (reader != null) reader.Close(); } catch { }
            try { if (writer != null) writer.Close(); } catch { }
            try { if (client != null) client.Close(); } catch { }
            reader = null;
            writer = null;
            client = null;
            Dbg("Network loop ended.");
        }
    }

    private void ShutdownNetwork()
    {
        running = false;
        connected = false;
        serverHandshakeComplete = false;

        try { SendLine("BYE|" + Escape(runtimePlayerId)); } catch { }
        try { if (client != null) client.Close(); } catch { }

        try
        {
            if (netThread != null && netThread.IsAlive)
                netThread.Join(300);
        }
        catch { }
    }

    private void SendHello()
    {
        RefreshLocalAppearanceFromRuntime();

        SendLine("HELLO|" +
            Escape(runtimePlayerId) + "|" +
            Escape(settings.PlayerName) + "|" +
            Escape(settings.ClusterId) + "|" +
            playerDesign + "|" +
            playerColor1 + "|" +
            playerColor2);
        Dbg("Sent HELLO with appearance design=" + playerDesign + " colors=" + playerColor1 + "," + playerColor2 + ".");
    }

    private void SendLocalStateOrHeartbeat()
    {
        if (player == null)
        {
            SendLine("PING");
            return;
        }

        RefreshLocalAppearanceFromRuntime();

        Vector3 p = player.transform.position;
        string scene = GetLocalSceneName();

        int facing = PlayerController.facingDir;
        bool moving = PlayerController.isMoving;

        LocalFollowerNetState followerState = GetLocalFollowerNetState();

        SendLine("POS|" +
            Escape(runtimePlayerId) + "|" +
            Escape(settings.PlayerName) + "|" +
            Escape(settings.ClusterId) + "|" +
            Escape(scene) + "|" +
            F(p.x) + "|" +
            F(p.y) + "|" +
            F(p.z) + "|" +
            facing + "|" +
            (moving ? "1" : "0") + "|" +
            playerDesign + "|" +
            playerColor1 + "|" +
            playerColor2 + "|" +
            Escape(GetLocalAvailabilityStatus()) + "|" +
            (followerState.enabled ? "1" : "0") + "|" +
            F(followerState.pos.x) + "|" +
            F(followerState.pos.y) + "|" +
            followerState.facing + "|" +
            (followerState.moving ? "1" : "0") + "|" +
            Escape(followerState.spriteName) + "|" +
            (followerState.flipX ? "1" : "0") + "|" +
            (PlayerController.ridingBroom ? "1" : "0") + "|" +
            (GetLocalWaterWalkingState() ? "1" : "0") + "|" +
            (GetLocalJumpOrBounceState() ? "1" : "0") + "|" +
            Escape(GetLocalBouncingName()));
    }
    public void SendImmediateLocalStateFromPatch(string reason)
    {
        if (!connected)
            return;
        try
        {
            Dbg("Immediate local visual state send: " + (reason ?? ""));
            SendLocalStateOrHeartbeat();
        }
        catch (Exception ex)
        {
            Dbg("SendImmediateLocalStateFromPatch failed: " + ex.Message);
        }
    }





    private static string GetPrivateBouncingName(PlayerController pc)
    {
        try
        {
            if (pc == null)
                return "";

            FieldInfo f = AccessTools.Field(typeof(PlayerController), "bouncing");
            if (f == null)
                return "";

            object value = f.GetValue(pc);
            return value as string ?? "";
        }
        catch { return ""; }
    }

    private string GetLocalBouncingName()
    {
        return GetPrivateBouncingName(player);
    }

    private bool GetLocalWaterWalkingState()
    {
        try { return player != null && player.waterWalking; } catch { return false; }
    }

    private bool GetLocalJumpOrBounceState()
    {
        try
        {
            if (player == null)
                return false;

            if (player.jumpingOverCliff)
                return true;

            string bouncing = GetPrivateBouncingName(player);
            return !string.IsNullOrEmpty(bouncing);
        }
        catch { return false; }
    }

    public void SendRemoteVisualEventFromPatch(string eventType)
    {
        SendRemoteVisualEventFromPatch(eventType, -999, false, Vector3.zero);
    }

    public void SendRemoteVisualEventFromPatch(string eventType, int dir)
    {
        SendRemoteVisualEventFromPatch(eventType, dir, false, Vector3.zero);
    }

    public void SendRemoteVisualEventFromPatch(string eventType, int dir, Vector3 eventPosition)
    {
        SendRemoteVisualEventFromPatch(eventType, dir, true, eventPosition);
    }

    private void SendRemoteVisualEventFromPatch(string eventType, int dir, bool hasEventPosition, Vector3 eventPosition)
    {
        if (!connected || player == null || string.IsNullOrWhiteSpace(eventType))
            return;

        try
        {
            RefreshLocalAppearanceFromRuntime();

            Vector3 p = hasEventPosition ? eventPosition : player.transform.position;
            string scene = GetLocalSceneName();

            int facing = PlayerController.facingDir;
            int eventDir = dir >= 0 ? dir : facing;

            SendLine("VIS_EVENT|" +
                Escape(runtimePlayerId) + "|" +
                Escape(settings.PlayerName) + "|" +
                Escape(settings.ClusterId) + "|" +
                Escape(scene) + "|" +
                Escape(eventType) + "|" +
                F(p.x) + "|" +
                F(p.y) + "|" +
                F(p.z) + "|" +
                facing + "|" +
                eventDir + "|" +
                (PlayerController.ridingBroom ? "1" : "0") + "|" +
                (GetLocalWaterWalkingState() ? "1" : "0") + "|" +
                (GetLocalJumpOrBounceState() ? "1" : "0") + "|" +
                Escape(GetLocalBouncingName()));

            // Also push the authoritative state immediately so late/packet-loss cases still converge.
            SendLocalStateOrHeartbeat();
        }
        catch (Exception ex)
        {
            Dbg("SendRemoteVisualEventFromPatch failed: " + ex.Message);
        }
    }


    private void UpdateBattleStateSync()
    {
        if (!settings.BattleStateSyncEnabled || !MMOnsterpatchPvPState.Active || !MMOnsterpatchPvPState.RealBattlesEnabled)
            return;

        try
        {
            if (MenuScript.gameState != MenuScript.GameState.Battle)
                return;
        }
        catch
        {
            return;
        }

        battleStateSendTimer += Time.deltaTime;
        if (battleStateSendTimer < Mathf.Max(0.1f, settings.BattleStateSendRateSeconds))
            return;

        battleStateSendTimer = 0f;
        SendLocalBattleState();
    }

    public void SendLocalBattleState()
    {
        if (!settings.BattleStateSyncEnabled || !MMOnsterpatchPvPState.Active || !MMOnsterpatchPvPState.RealBattlesEnabled)
            return;

        if (string.IsNullOrEmpty(MMOnsterpatchPvPState.OpponentId))
            return;

        string payload = BuildLocalBattleStatePayload();
        if (string.IsNullOrEmpty(payload))
            return;

        SendLine("BATTLE_STATE|" +
            Escape(runtimePlayerId) + "|" +
            Escape(MMOnsterpatchPvPState.OpponentId) + "|" +
            Escape(MMOnsterpatchPvPState.BattleId) + "|" +
            payload);
    }

    private string BuildLocalBattleStatePayload()
    {
        try
        {
            GameScript gs = GetGameScript();
            if (gs == null || gs.teamMon == null)
                return "";

            string[] slots = new string[4];

            for (int i = 0; i < 4; i++)
            {
                Mon m = (i < gs.teamMon.Length) ? gs.teamMon[i] : null;
                if (m == null)
                {
                    slots[i] = "_";
                    continue;
                }

                string saveString = gs.ConstructMonSaveStringFromMon(m, false) ?? "";
                string extra = BuildBattleStateExtraString(m);
                slots[i] =
                    Convert.ToBase64String(Encoding.UTF8.GetBytes(saveString)) +
                    "," +
                    Convert.ToBase64String(Encoding.UTF8.GetBytes(extra ?? ""));
            }

            return string.Join("~", slots);
        }
        catch (Exception ex)
        {
            Dbg("BuildLocalBattleStatePayload failed: " + ex.Message);
            return "";
        }
    }

    private void ApplyRemoteBattleState(string payload)
    {
        try
        {
            GameScript gs = GetGameScript();
            BattleSystem bs = FindObjectOfType<BattleSystem>();
            if (gs == null || bs == null || string.IsNullOrWhiteSpace(payload))
                return;

            Mon[] enemyMon = GetFieldValue<Mon[]>(bs, "enemyMon");
            if (enemyMon == null || enemyMon.Length < 4)
                return;

            string[] slots = payload.Split('~');
            for (int i = 0; i < 4 && i < slots.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(slots[i]) || slots[i] == "_")
                {
                    enemyMon[i] = null;
                    RefreshBattleSlotFromMon(bs, true, i);
                    continue;
                }

                string[] pair = slots[i].Split(',');
                string saveB64 = pair.Length > 0 ? pair[0] : "";
                string extraB64 = pair.Length > 1 ? pair[1] : "";

                Mon decoded = null;
                try
                {
                    string saveString = Encoding.UTF8.GetString(Convert.FromBase64String(saveB64));
                    decoded = gs.GetMonFromSaveString(saveString);
                }
                catch { }

                if (decoded == null)
                    continue;

                if (enemyMon[i] == null || enemyMon[i].monScriptableObject == null || enemyMon[i].monScriptableObject.id != decoded.monScriptableObject.id)
                    enemyMon[i] = decoded;
                else
                    CopySupportedBattleFields(decoded, enemyMon[i]);

                try
                {
                    string extra = string.IsNullOrEmpty(extraB64) ? "" : Encoding.UTF8.GetString(Convert.FromBase64String(extraB64));
                    ApplyBattleStateExtraString(enemyMon[i], extra);
                }
                catch { }

                RefreshBattleSlotFromMon(bs, true, i);
            }
        }
        catch (Exception ex)
        {
            Dbg("ApplyRemoteBattleState failed: " + ex.Message);
        }
    }

    private static string BuildBattleStateExtraString(Mon mon)
    {
        if (mon == null)
            return "";

        List<string> parts = new List<string>();
        FieldInfo[] fields = typeof(Mon).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (FieldInfo f in fields)
        {
            if (!ShouldSyncBattleField(f))
                continue;

            try
            {
                object value = f.GetValue(mon);
                string encoded = SerializeBattleFieldValue(value, f.FieldType);
                if (encoded != null)
                    parts.Add(f.Name + "=" + encoded);
            }
            catch { }
        }

        return string.Join("^", parts.ToArray());
    }

    private static void ApplyBattleStateExtraString(Mon mon, string extra)
    {
        if (mon == null || string.IsNullOrEmpty(extra))
            return;

        string[] entries = extra.Split('^');
        Type t = typeof(Mon);

        for (int i = 0; i < entries.Length; i++)
        {
            string entry = entries[i];
            int eq = entry.IndexOf('=');
            if (eq <= 0)
                continue;

            string name = entry.Substring(0, eq);
            string value = entry.Substring(eq + 1);

            FieldInfo f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f == null || !ShouldSyncBattleField(f))
                continue;

            try
            {
                object parsed = DeserializeBattleFieldValue(value, f.FieldType);
                if (parsed != null || !f.FieldType.IsValueType)
                    f.SetValue(mon, parsed);
            }
            catch { }
        }
    }

    private static bool ShouldSyncBattleField(FieldInfo f)
    {
        if (f == null || f.IsStatic)
            return false;

        string n = f.Name.ToLowerInvariant();
        bool important =
            n == "hp" ||
            n == "shield" ||
            n.Contains("buff") ||
            n.Contains("debuff") ||
            n.Contains("cooldown") ||
            n.Contains("duration") ||
            n.Contains("stack") ||
            n.Contains("status") ||
            n.Contains("statboost");

        if (!important)
            return false;

        Type ft = f.FieldType;
        if (ft == typeof(int) || ft == typeof(float) || ft == typeof(bool) || ft == typeof(string) || ft.IsEnum)
            return true;

        if (ft.IsArray)
        {
            Type e = ft.GetElementType();
            return e == typeof(int) || e == typeof(float) || e == typeof(bool) || e == typeof(string) || e.IsEnum;
        }

        return false;
    }

    private static void CopySupportedBattleFields(Mon source, Mon target)
    {
        if (source == null || target == null)
            return;

        FieldInfo[] fields = typeof(Mon).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (FieldInfo f in fields)
        {
            if (!ShouldCopyBaseBattleField(f))
                continue;

            try
            {
                object value = f.GetValue(source);
                if (value != null && f.FieldType.IsArray)
                {
                    Array arr = (Array)value;
                    Array clone = Array.CreateInstance(f.FieldType.GetElementType(), arr.Length);
                    Array.Copy(arr, clone, arr.Length);
                    f.SetValue(target, clone);
                }
                else
                {
                    f.SetValue(target, value);
                }
            }
            catch { }
        }
    }

    private static bool ShouldCopyBaseBattleField(FieldInfo f)
    {
        if (f == null || f.IsStatic)
            return false;

        string n = f.Name.ToLowerInvariant();
        if (n == "mon" || n.Contains("scriptableobject"))
            return false;

        Type ft = f.FieldType;
        if (ft == typeof(int) || ft == typeof(float) || ft == typeof(bool) || ft == typeof(string) || ft.IsEnum)
            return true;

        if (ft.IsArray)
        {
            Type e = ft.GetElementType();
            return e == typeof(int) || e == typeof(float) || e == typeof(bool) || e == typeof(string) || e.IsEnum;
        }

        return false;
    }

    private static string SerializeBattleFieldValue(object value, Type type)
    {
        if (value == null)
            return "";

        if (type == typeof(int) || type == typeof(float) || type == typeof(bool) || type == typeof(string) || type.IsEnum)
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(Convert.ToString(value, CultureInfo.InvariantCulture) ?? ""));

        if (type.IsArray)
        {
            Array arr = value as Array;
            if (arr == null)
                return "";

            List<string> vals = new List<string>();
            for (int i = 0; i < arr.Length; i++)
                vals.Add(Convert.ToString(arr.GetValue(i), CultureInfo.InvariantCulture) ?? "");

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Join(";", vals.ToArray())));
        }

        return null;
    }

    private static object DeserializeBattleFieldValue(string encoded, Type type)
    {
        string s = "";
        try { s = Encoding.UTF8.GetString(Convert.FromBase64String(encoded)); } catch { s = encoded ?? ""; }

        if (type == typeof(string))
            return s;
        if (type == typeof(int))
        {
            int v;
            int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v);
            return v;
        }
        if (type == typeof(float))
        {
            float v;
            float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v);
            return v;
        }
        if (type == typeof(bool))
            return s == "True" || s == "true" || s == "1";
        if (type.IsEnum)
        {
            try { return Enum.Parse(type, s); } catch { return Activator.CreateInstance(type); }
        }

        if (type.IsArray)
        {
            Type e = type.GetElementType();
            string[] parts = string.IsNullOrEmpty(s) ? new string[0] : s.Split(';');
            Array arr = Array.CreateInstance(e, parts.Length);

            for (int i = 0; i < parts.Length; i++)
            {
                object val = null;
                if (e == typeof(int))
                {
                    int v; int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out v); val = v;
                }
                else if (e == typeof(float))
                {
                    float v; float.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out v); val = v;
                }
                else if (e == typeof(bool))
                {
                    val = parts[i] == "True" || parts[i] == "true" || parts[i] == "1";
                }
                else if (e == typeof(string))
                {
                    val = parts[i];
                }
                else if (e.IsEnum)
                {
                    try { val = Enum.Parse(e, parts[i]); } catch { val = Activator.CreateInstance(e); }
                }

                arr.SetValue(val, i);
            }

            return arr;
        }

        return null;
    }

    private void RefreshBattleSlotFromMon(BattleSystem bs, bool enemySide, int slot)
    {
        try
        {
            if (bs == null || slot < 0 || slot >= 4)
                return;

            Mon[] mons = enemySide ? GetFieldValue<Mon[]>(bs, "enemyMon") : (bs.gameScript != null ? bs.gameScript.teamMon : null);
            Mon mon = (mons != null && slot < mons.Length) ? mons[slot] : null;

            object slotObjArrayObj = GetFieldObject(bs, enemySide ? "enemySlotMonObj" : "playerSlotMonObj");
            MonObject mo = null;

            Array slotObjArray = slotObjArrayObj as Array;
            if (slotObjArray != null && slot < slotObjArray.Length)
            {
                object slotObj = slotObjArray.GetValue(slot);
                if (slotObj is MonObject)
                    mo = (MonObject)slotObj;
                else if (slotObj is GameObject)
                    mo = ((GameObject)slotObj).GetComponent<MonObject>();
            }

            if (mo == null)
            {
                object monObjArrayObj = GetFieldObject(bs, enemySide ? "enemyMonObj" : "playerMonObj");
                Array monObjArray = monObjArrayObj as Array;
                if (monObjArray != null && slot < monObjArray.Length)
                {
                    object slotObj = monObjArray.GetValue(slot);
                    if (slotObj is GameObject)
                        mo = ((GameObject)slotObj).GetComponent<MonObject>();
                    else if (slotObj is MonObject)
                        mo = (MonObject)slotObj;
                }
            }

            if (mo != null && mon != null)
                mo.mon = mon;

            RefreshBattleHpBar(bs, enemySide, slot, mon);
            if (mo != null)
                TryInvoke(bs, "RefreshStatusIcons", new object[] { mo });
        }
        catch { }
    }

    private void RefreshBattleHpBar(BattleSystem bs, bool enemySide, int slot, Mon mon)
    {
        try
        {
            object barsObj = GetFieldObject(bs, enemySide ? "enemyHealthBar" : "playerHealthBar");
            Array bars = barsObj as Array;
            if (bars == null || slot < 0 || slot >= bars.Length)
                return;

            GameObject hbObj = bars.GetValue(slot) as GameObject;
            if (hbObj == null || mon == null)
                return;

            HealthBar hb = hbObj.GetComponent<HealthBar>();
            if (hb == null)
                return;

            TryInvoke(hb, "BindMon", new object[] { mon });
            TryInvoke(hb, "SetNewHP", new object[] { mon.hp });
        }
        catch { }
    }

    public void SendBattleCommand(int actorSlot, int moveSlot, int targetSlot, bool targetAlly)
    {
        if (!MMOnsterpatchPvPState.Active || !MMOnsterpatchPvPState.RealBattlesEnabled)
            return;

        if (string.IsNullOrEmpty(MMOnsterpatchPvPState.OpponentId))
            return;

        SendLine("BATTLE_CMD|" +
            Escape(runtimePlayerId) + "|" +
            Escape(MMOnsterpatchPvPState.OpponentId) + "|" +
            Escape(MMOnsterpatchPvPState.BattleId) + "|" +
            actorSlot + "|" +
            moveSlot + "|" +
            targetSlot + "|" +
            (targetAlly ? "1" : "0"));

        Dbg("Sent PvP command battle=" + MMOnsterpatchPvPState.BattleId + " to=" + MMOnsterpatchPvPState.OpponentId + " actor=" + actorSlot + " move=" + moveSlot + " target=" + targetSlot + " ally=" + targetAlly);
    }

    public void SendBattleHit(int actorSlot, int moveSlot, string targetSide, int targetSlot, int amount, int hpAfter, int shieldAfter, bool crit)
    {
        if (!MMOnsterpatchPvPState.Active || !MMOnsterpatchPvPState.RealBattlesEnabled)
            return;

        if (string.IsNullOrEmpty(MMOnsterpatchPvPState.OpponentId))
            return;

        SendLine("BATTLE_HIT|" +
            Escape(runtimePlayerId) + "|" +
            Escape(MMOnsterpatchPvPState.OpponentId) + "|" +
            Escape(MMOnsterpatchPvPState.BattleId) + "|" +
            actorSlot + "|" +
            moveSlot + "|" +
            Escape(targetSide ?? "E") + "|" +
            targetSlot + "|" +
            amount + "|" +
            hpAfter + "|" +
            shieldAfter + "|" +
            (crit ? "1" : "0"));

        Dbg("Sent PvP hit battle=" + MMOnsterpatchPvPState.BattleId + " actor=" + actorSlot + " move=" + moveSlot + " target=" + targetSide + targetSlot + " amount=" + amount + " hpAfter=" + hpAfter + " crit=" + crit);
    }

    public void SendBattleDone(int actorSlot, int moveSlot)
    {
        if (!MMOnsterpatchPvPState.Active || !MMOnsterpatchPvPState.RealBattlesEnabled)
            return;

        if (string.IsNullOrEmpty(MMOnsterpatchPvPState.OpponentId))
            return;

        SendLine("BATTLE_DONE|" +
            Escape(runtimePlayerId) + "|" +
            Escape(MMOnsterpatchPvPState.OpponentId) + "|" +
            Escape(MMOnsterpatchPvPState.BattleId) + "|" +
            actorSlot + "|" +
            moveSlot);

        Dbg("Sent PvP done battle=" + MMOnsterpatchPvPState.BattleId + " actor=" + actorSlot + " move=" + moveSlot);
    }

    private static string MakeBattleId(string a, string b)
    {
        string aa = a ?? "";
        string bb = b ?? "";
        int cmp = string.CompareOrdinal(aa, bb);
        return cmp <= 0 ? (aa + "_vs_" + bb) : (bb + "_vs_" + aa);
    }

    public void ReportWorldSpawnClaimFromPatch(string spawnId, string source)
    {
        if (string.IsNullOrWhiteSpace(spawnId))
            return;

        try
        {
            SendLine("WORLD_SPAWN_CLAIM|" + Escape(runtimePlayerId) + "|" + Escape(spawnId));
            Dbg("Sent WORLD_SPAWN_CLAIM from " + (source ?? "patch") + " for " + spawnId);
        }
        catch (Exception ex)
        {
            Dbg("ReportWorldSpawnClaimFromPatch failed: " + ex.Message);
        }
    }

    public void ReportWorldSpawnCatchStartFromPatch(string spawnId)
    {
        if (string.IsNullOrWhiteSpace(spawnId))
            return;

        try
        {
            SendLine("WORLD_SPAWN_CATCH_START|" + Escape(runtimePlayerId) + "|" + Escape(spawnId));
            Dbg("Sent WORLD_SPAWN_CATCH_START " + spawnId);
        }
        catch (Exception ex)
        {
            Dbg("ReportWorldSpawnCatchStartFromPatch failed: " + ex.Message);
        }
    }


    public void ReportWorldSpawnCatchResultFromPatch(string spawnId, bool caught)
    {
        if (string.IsNullOrWhiteSpace(spawnId))
            return;

        try
        {
            WorldSpawnVisual spawn;
            if (worldSpawns.TryGetValue(spawnId, out spawn) && spawn != null)
            {
                spawn.claimPending = caught;
                spawn.lastSeen = Time.time;
                if (spawn.marker != null)
                {
                    spawn.marker.CatchResultSent = true;
                    spawn.marker.CaughtAndHidden = caught;
                    spawn.marker.ClaimPending = caught;
                }

                if (caught)
                {
                    // Successful catch: match the requested flow.
                    // Catch animation finishes, then the overworld spawn is hidden/removed before the caught message box.
                    if (spawn.obj != null)
                    {
                        try { spawn.obj.SetActive(false); } catch { }
                        try { Destroy(spawn.obj); } catch { }
                    }
                    worldSpawns.Remove(spawnId);
                }
                else
                {
                    // Failed catch/shoo-style result: make the same vanilla object visible/available again.
                    spawn.claimPending = false;
                    if (spawn.marker != null)
                    {
                        spawn.marker.ClaimPending = false;
                        spawn.marker.CaughtAndHidden = false;
                    }
                    if (spawn.obj != null)
                        spawn.obj.SetActive(true);
                }
            }
        }
        catch { }

        SendLine("WORLD_SPAWN_RESULT|" + Escape(runtimePlayerId) + "|" + Escape(spawnId) + "|" + (caught ? "caught" : "failed"));
        Dbg("Sent WORLD_SPAWN_RESULT " + spawnId + " result=" + (caught ? "caught" : "failed"));
    }
    public void ShowWorldSpawnNoPermissionFromPatch()
    {
        try { ShowBattleInfoWindow("Capture Locked", "You don't have permission to capture this MoN."); } catch { }
    }

    public void ReportPersonalEncounterSpawnsFromPatch(GameScript gs, List<Vector3> spots, string source)
    {
        if (gs == null || !connected || settings == null || !settings.WorldSpawnsEnabled)
            return;
        try
        {
            string scene = GetLocalSceneName();
            if (string.IsNullOrWhiteSpace(scene) || scene.Equals("unknown", StringComparison.OrdinalIgnoreCase))
                return;
            List<string> records = new List<string>();
            for (int i = 0; i < 4; i++)
            {
                Mon m = (gs.lastEncounterMons != null && i < gs.lastEncounterMons.Length) ? gs.lastEncounterMons[i] : null;
                if (m == null)
                    continue;
                Vector3 pos = (spots != null && i < spots.Count && spots[i] != Vector3.zero) ? spots[i] : gs.lastEncounterPos;
                int monId = -1;
                string monKey = "";
                bool shiny = false;
                int level = 1;
                try { monId = m.monScriptableObject != null ? m.monScriptableObject.id : m.monID; } catch { try { monId = m.monID; } catch { } }
                try { monKey = m.monScriptableObject != null ? (m.monScriptableObject.name ?? "") : ""; } catch { }
                if (string.IsNullOrWhiteSpace(monKey))
                {
                    try { monKey = m.monScriptableObject != null ? (m.monScriptableObject.monName ?? "") : ""; } catch { }
                }
                try { shiny = m.isShiny; } catch { }
                try { level = Mathf.Max(1, m.curLevel); } catch { }
                string versionReq = GetEncounterVersionRequirementLabel(gs, m);
                string saveB64 = "";
                try
                {
                    string saveString = gs.ConstructMonSaveStringFromMon(m, false) ?? "";
                    saveB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(saveString));
                }
                catch { }
                records.Add(i.ToString(CultureInfo.InvariantCulture) + "," +
                    F(pos.x) + "," + F(pos.y) + "," + F(pos.z) + "," +
                    monId.ToString(CultureInfo.InvariantCulture) + "," +
                    (shiny ? "1" : "0") + "," +
                    level.ToString(CultureInfo.InvariantCulture) + "," +
                    Escape(monKey) + "," + Escape(saveB64) + "," + Escape(versionReq));
            }
            if (records.Count <= 0)
                return;
            SendLine("WORLD_SPAWN_PERSONAL_ADD|" +
                Escape(runtimePlayerId) + "|" +
                Escape(settings.ClusterId) + "|" +
                Escape(scene) + "|" +
                string.Join(";", records.ToArray()));
            Dbg("Sent personal encounter reward spawn packet count=" + records.Count + " source=" + (source ?? "") + " scene=" + scene);
        }
        catch (Exception ex)
        {
            Dbg("ReportPersonalEncounterSpawnsFromPatch failed: " + ex.Message);
        }
    }

    public void ForceLocalWorldSpawnCaughtFromPatch(string spawnId)
    {
        if (string.IsNullOrWhiteSpace(spawnId))
            return;

        try
        {
            Dbg("ForceLocalWorldSpawnCaughtFromPatch enter " + spawnId);

            // First hit the exact private GameScript.curInteractingMonObj because the decompile
            // shows Interact uses this exact raycast object for the Catch/Shoo menu.
            try
            {
                GameScript gs = GetGameScript();
                if (gs != null)
                {
                    FieldInfo f = typeof(GameScript).GetField("curInteractingMonObj", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    GameObject curObj = f != null ? f.GetValue(gs) as GameObject : null;
                    string curSpawnId = MMOWorldSpawnCatchRuntime.FindSpawnIdFromObject(curObj);
                    if (curObj != null && string.Equals(curSpawnId, spawnId, StringComparison.Ordinal))
                    {
                        Dbg("Force hiding exact curInteractingMonObj for caught spawn " + spawnId + " obj=" + curObj.name);
                        HardDisableWorldSpawnObject(curObj);
                        if (f != null)
                            f.SetValue(gs, null);
                    }

                    try
                    {
                        if (gs.subMenuWildMon != null)
                            gs.subMenuWildMon.SetActive(false);
                        if (gs.panelMonNamePreview != null)
                            gs.panelMonNamePreview.SetActive(false);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Dbg("Force curInteractingMonObj cleanup failed: " + ex.Message);
            }

            WorldSpawnVisual spawn;
            if (worldSpawns.TryGetValue(spawnId, out spawn) && spawn != null)
            {
                if (spawn.marker != null)
                {
                    spawn.marker.ClaimPending = true;
                    spawn.marker.CaughtAndHidden = true;
                    spawn.marker.CatchResultSent = true;
                }

                if (spawn.obj != null)
                    HardDisableWorldSpawnObject(spawn.obj);

                worldSpawns.Remove(spawnId);
            }

            // Also scan all active/inactive markers in case the interacted child/root is not the same
            // object stored in worldSpawns.
            try
            {
                MMOWorldSpawnMarker[] markers = UnityEngine.Object.FindObjectsOfType<MMOWorldSpawnMarker>(true);
                for (int i = 0; i < markers.Length; i++)
                {
                    MMOWorldSpawnMarker marker = markers[i];
                    if (marker == null || !string.Equals(marker.SpawnId, spawnId, StringComparison.Ordinal))
                        continue;
                    marker.ClaimPending = true;
                    marker.CaughtAndHidden = true;
                    marker.CatchResultSent = true;
                    if (marker.gameObject != null)
                        HardDisableWorldSpawnObject(marker.gameObject);
                }
            }
            catch { }

            Dbg("Force removed local caught server spawn " + spawnId);
        }
        catch (Exception ex)
        {
            Dbg("ForceLocalWorldSpawnCaughtFromPatch failed: " + ex.Message);
        }
    }

    private void HardDisableWorldSpawnObject(GameObject obj)
    {
        if (obj == null)
            return;

        try { obj.tag = "Null"; } catch { }
        try
        {
            Collider2D[] cols = obj.GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < cols.Length; i++)
                if (cols[i] != null)
                    cols[i].enabled = false;
        }
        catch { }
        try { obj.SetActive(false); } catch { }
        try { Destroy(obj); } catch { }
    }




    private void SendLine(string line)
    {
        try
        {
            StreamWriter w = writer;
            if (w != null)
                w.WriteLine(line);
        }
        catch (Exception ex)
        {
            Dbg("SendLine error: " + ex.Message);
            connected = false;
        }
    }

    private void HandleServerLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return;

        if (line.StartsWith("LOCALCONNECTED|"))
        {
            ShowServerStatusWindow("Connected to Server");
            return;
        }

        if (line.StartsWith("LOCALERR|"))
        {
            Dbg("Local network error from queue: " + Unescape(line.Substring("LOCALERR|".Length)));
            return;
        }

        if (line.StartsWith("WELCOME|"))
        {
            serverHandshakeComplete = true;
            Dbg("Server says: " + line);
            return;
        }

        if (line.StartsWith("PONG"))
        {
            return;
        }

        if (line.StartsWith("BATTLE_REQ|"))
        {
            HandleBattleRequestPacket(line.Substring("BATTLE_REQ|".Length));
            return;
        }

        if (line.StartsWith("BATTLE_ACCEPT|"))
        {
            HandleBattleAcceptPacket(line.Substring("BATTLE_ACCEPT|".Length));
            return;
        }

        if (line.StartsWith("BATTLE_DECLINE|"))
        {
            HandleBattleDeclinePacket(line.Substring("BATTLE_DECLINE|".Length));
            return;
        }

        if (line.StartsWith("BATTLE_BUSY|"))
        {
            HandleBattleBusyPacket(line.Substring("BATTLE_BUSY|".Length));
            return;
        }

        if (line.StartsWith("BATTLE_CMD|"))
        {
            HandleBattleCommandPacket(line.Substring("BATTLE_CMD|".Length));
            return;
        }

        if (line.StartsWith("BATTLE_HIT|"))
        {
            HandleBattleHitPacket(line.Substring("BATTLE_HIT|".Length));
            return;
        }

        if (line.StartsWith("BATTLE_DONE|"))
        {
            HandleBattleDonePacket(line.Substring("BATTLE_DONE|".Length));
            return;
        }

        if (line.StartsWith("BATTLE_STATE|"))
        {
            HandleBattleStatePacket(line.Substring("BATTLE_STATE|".Length));
            return;
        }

        if (line.StartsWith("WORLD_SPAWN_GEN_REQ|"))
        {
            HandleWorldSpawnGenerateRequest(line.Substring("WORLD_SPAWN_GEN_REQ|".Length));
            return;
        }

        if (line.StartsWith("WORLD_SPAWNS|"))
        {
            HandleWorldSpawnsSnapshot(line.Substring("WORLD_SPAWNS|".Length));
            return;
        }

        if (line.StartsWith("WORLD_SPAWN_ADD|"))
        {
            HandleWorldSpawnAdd(line.Substring("WORLD_SPAWN_ADD|".Length));
            return;
        }

        if (line.StartsWith("WORLD_SPAWN_LOCK_UPDATE|"))
        {
            HandleWorldSpawnLockUpdate(line.Substring("WORLD_SPAWN_LOCK_UPDATE|".Length));
            return;
        }

        if (line.StartsWith("WORLD_SPAWN_REMOVE|"))
        {
            HandleWorldSpawnRemove(line.Substring("WORLD_SPAWN_REMOVE|".Length));
            return;
        }

        if (line.StartsWith("WORLD_SPAWN_BUSY|"))
        {
            HandleWorldSpawnBusy(line.Substring("WORLD_SPAWN_BUSY|".Length));
            return;
        }

        if (line.StartsWith("WORLD_SPAWN_CATCH_START|"))
        {
            HandleWorldSpawnCatchStart(line.Substring("WORLD_SPAWN_CATCH_START|".Length));
            return;
        }

        if (line.StartsWith("WORLD_SPAWN_CATCH_END|"))
        {
            HandleWorldSpawnCatchEnd(line.Substring("WORLD_SPAWN_CATCH_END|".Length));
            return;
        }

        if (line.StartsWith("WORLD_SPAWN_CLAIM_OK|"))
        {
            HandleWorldSpawnClaimOk(line.Substring("WORLD_SPAWN_CLAIM_OK|".Length));
            return;
        }

        if (line.StartsWith("WORLD_SPAWN_CLAIM_FAIL|"))
        {
            HandleWorldSpawnClaimFail(line.Substring("WORLD_SPAWN_CLAIM_FAIL|".Length));
            return;
        }

        if (line.StartsWith("VIS_EVENT|"))
        {
            HandleRemoteVisualEvent(line.Substring("VIS_EVENT|".Length));
            return;
        }

        if (line.StartsWith("STEP|"))
        {
            HandleRemoteStep(line.Substring("STEP|".Length));
            return;
        }

        if (!line.StartsWith("SNAP|")) return;

        string body = line.Substring("SNAP|".Length);

        if (!string.IsNullOrWhiteSpace(body))
        {
            string[] records = body.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string rec in records)
            {
                string[] p = rec.Split('|');
                if (p.Length < 12) continue;

                string id = Unescape(p[0]);
                if (id == runtimePlayerId) continue;

                string name = Unescape(p[1]);
                float x = ParseFloat(p[4]);
                float y = ParseFloat(p[5]);
                int facing = ParseInt(p[7]);
                bool moving = p[8] == "1" || p[8].Equals("true", StringComparison.OrdinalIgnoreCase);
                int design = ParseInt(p[9]);
                int color1 = ParseInt(p[10]);
                int color2 = ParseInt(p[11]);

                bool followerEnabled = false;
                float followerX = 0f;
                float followerY = 0f;
                int followerFacing = facing;
                bool followerMoving = false;
                string followerSpriteName = "";
                bool followerFlipX = false;
                bool ridingBroom = false;
                bool waterWalking = false;
                bool jumpingOrBouncing = false;
                string bouncingName = "";
                if (p.Length >= 20)
                {
                    followerEnabled = p[13] == "1" || p[13].Equals("true", StringComparison.OrdinalIgnoreCase);
                    followerX = ParseFloat(p[14]);
                    followerY = ParseFloat(p[15]);
                    followerFacing = ParseInt(p[16]);
                    followerMoving = p[17] == "1" || p[17].Equals("true", StringComparison.OrdinalIgnoreCase);
                    followerSpriteName = Unescape(p[18]);
                    followerFlipX = p[19] == "1" || p[19].Equals("true", StringComparison.OrdinalIgnoreCase);
                }
                if (p.Length >= 21)
                    ridingBroom = p[20] == "1" || p[20].Equals("true", StringComparison.OrdinalIgnoreCase);
                if (p.Length >= 22)
                    waterWalking = p[21] == "1" || p[21].Equals("true", StringComparison.OrdinalIgnoreCase);
                if (p.Length >= 23)
                    jumpingOrBouncing = p[22] == "1" || p[22].Equals("true", StringComparison.OrdinalIgnoreCase);
                if (p.Length >= 24)
                    bouncingName = Unescape(p[23]);

                Vector3 target = new Vector3(x, y, y * 0.01f);
                Vector3 followerTarget = new Vector3(followerX, followerY, followerY * 0.01f);
                UpdateRemotePlayer(id, name, target, facing, moving, design, color1, color2, followerEnabled, followerTarget, followerFacing, followerMoving, followerSpriteName, followerFlipX, ridingBroom, waterWalking, jumpingOrBouncing, bouncingName);
            }
        }
    }

    private void MaybeSendWorldSpawnZones()
    {
        if (!connected || settings == null || !settings.WorldSpawnsEnabled || player == null)
            return;

        try
        {
            worldSpawnZoneReportTimer -= Time.deltaTime;
            if (worldSpawnZoneReportTimer > 0f)
                return;
            worldSpawnZoneReportTimer = 2.0f;

            string scene = GetLocalSceneName();
            if (string.IsNullOrWhiteSpace(scene) || scene.Equals("unknown", StringComparison.OrdinalIgnoreCase))
                return;

            string signature;
            string zoneBody;
            int count = BuildWorldSpawnZoneReport(out signature, out zoneBody);
            if (count <= 0 || string.IsNullOrWhiteSpace(zoneBody))
                return;

            bool changed = !string.Equals(signature, lastWorldSpawnZoneSignature, StringComparison.Ordinal)
                || !string.Equals(scene, lastWorldSpawnZoneScene, StringComparison.Ordinal);
            bool periodic = Time.time - lastWorldSpawnZoneReportAt >= 15f;
            if (!changed && !periodic)
                return;

            lastWorldSpawnZoneSignature = signature;
            lastWorldSpawnZoneScene = scene;
            lastWorldSpawnZoneReportAt = Time.time;

            SendLine("WORLD_SPAWNZONES|" +
                Escape(runtimePlayerId) + "|" +
                Escape(settings.ClusterId) + "|" +
                Escape(scene) + "|" +
                Escape(signature) + "|" +
                zoneBody);
            Dbg("Reported " + count.ToString() + " deduped vanilla spawn tile(s) for server-owned overworld spawns scene=" + scene + " sig=" + signature);
        }
        catch (Exception ex)
        {
            Dbg("MaybeSendWorldSpawnZones failed: " + ex.Message);
        }
    }

    private int BuildWorldSpawnZoneReport(out string signature, out string zoneBody)
    {
        signature = "";
        zoneBody = "";

        try
        {
            WildMonSpawnManager manager = UnityEngine.Object.FindObjectOfType<WildMonSpawnManager>();
            if (manager == null)
                return 0;

            Transform parent = FindBestSpawnZoneParent(manager);
            if (parent == null)
                return 0;

            List<string> records = new List<string>();
            HashSet<string> seenTiles = new HashSet<string>(StringComparer.Ordinal);
            int index = 0;
            foreach (Transform child in parent)
            {
                if (child == null)
                    continue;
                string nm = child.name ?? "";
                if (!string.Equals(nm, "spawnZone", StringComparison.Ordinal))
                    continue;

                Vector3 pos = child.position;
                // v0.9.8: zone identity is based on tile/position, not raw child index.
                // Some updated maps can expose duplicate spawnZone objects at the same tile;
                // deduping here ensures Visible Spawn Rate changes chance only, not quantity.
                string zoneId = Mathf.RoundToInt(pos.x * 100f).ToString(CultureInfo.InvariantCulture) + ":" + Mathf.RoundToInt(pos.y * 100f).ToString(CultureInfo.InvariantCulture);
                if (seenTiles.Contains(zoneId))
                    continue;
                seenTiles.Add(zoneId);
                records.Add(Escape(zoneId) + "," + F(pos.x) + "," + F(pos.y) + "," + F(pos.z));
                index++;
                if (index >= 128)
                    break;
            }

            if (records.Count == 0)
                return 0;

            string parentName = parent.name ?? "unknown";
            signature = parentName + ":" + records.Count.ToString(CultureInfo.InvariantCulture) + ":" + string.Join(";", records.ToArray());
            zoneBody = string.Join(";", records.ToArray());
            return records.Count;
        }
        catch (Exception ex)
        {
            Dbg("BuildWorldSpawnZoneReport failed: " + ex.Message);
            return 0;
        }
    }

    private Transform FindBestSpawnZoneParent(WildMonSpawnManager manager)
    {
        if (manager == null)
            return null;

        try
        {
            GameScript gs = GetGameScript();
            string curLocation = "";
            try { if (gs != null) curLocation = gs.curLocation ?? ""; } catch { }

            if (!string.IsNullOrWhiteSpace(curLocation))
            {
                Transform byLocation = manager.transform.Find(curLocation);
                if (byLocation != null && CountDirectSpawnZones(byLocation) > 0)
                    return byLocation;
            }

            string localScene = GetLocalSceneName();
            if (!string.IsNullOrWhiteSpace(localScene))
            {
                Transform byScene = manager.transform.Find(localScene);
                if (byScene != null && CountDirectSpawnZones(byScene) > 0)
                    return byScene;
            }

            Transform best = null;
            float bestScore = float.MaxValue;
            Vector3 playerPos = player != null ? player.transform.position : Vector3.zero;

            foreach (Transform candidate in manager.transform)
            {
                if (candidate == null)
                    continue;

                int count = CountDirectSpawnZones(candidate);
                if (count <= 0)
                    continue;

                float closest = 99999f;
                foreach (Transform child in candidate)
                {
                    if (child == null || !string.Equals(child.name ?? "", "spawnZone", StringComparison.Ordinal))
                        continue;
                    float dist = Vector2.Distance(new Vector2(playerPos.x, playerPos.y), new Vector2(child.position.x, child.position.y));
                    if (dist < closest)
                        closest = dist;
                }

                // Prefer active parents, then closest spawnZone to the player.
                float score = closest;
                try { if (!candidate.gameObject.activeInHierarchy) score += 10000f; } catch { }
                if (score < bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }
        catch { return null; }
    }

    private int CountDirectSpawnZones(Transform parent)
    {
        if (parent == null)
            return 0;
        int count = 0;
        try
        {
            foreach (Transform child in parent)
            {
                if (child != null && string.Equals(child.name ?? "", "spawnZone", StringComparison.Ordinal))
                    count++;
            }
        }
        catch { }
        return count;
    }

    private void HandleWorldSpawnGenerateRequest(string body)
    {
        try
        {
            string[] p = (body ?? "").Split('|');
            if (p.Length < 7)
                return;

            string requestId = Unescape(p[0]);
            string scene = Unescape(p[1]);
            string rarity = Unescape(p[2]);
            string zoneId = Unescape(p[3]);
            float x = ParseFloat(p[4]);
            float y = ParseFloat(p[5]);
            float z = ParseFloat(p[6]);

            string localScene = GetLocalSceneName();
            if (!string.Equals(scene, localScene, StringComparison.Ordinal))
            {
                Dbg("Ignoring WORLD_SPAWN_GEN_REQ for scene=" + scene + " while local scene=" + localScene);
                return;
            }

            GameScript gs = GetGameScript();
            if (gs == null)
                return;

            Mon m = null;
            bool waterMonOnly = false;
            try { waterMonOnly = gs.playerController != null && gs.playerController.waterWalking; } catch { waterMonOnly = false; }

            // Official Server overworld spawns are shared-world spawns. Do not let
            // a generator client choose version-exclusive encounter entries here.
            // Random encounters remain vanilla/client-owned and still use the
            // character's saved GameScript.curVersion selection.
            m = GetOfficialServerSharedEncounterMon(gs, rarity, waterMonOnly);

            if (m == null || m.monScriptableObject == null)
            {
                Dbg("No shared-version overworld spawn candidate for req=" + requestId + " rarity=" + rarity + " water=" + waterMonOnly + "; leaving zone empty.");
                return;
            }

            int level = 1;
            try
            {
                if (gs.encounterPoolScriptableObject != null
                    && gs.curEncounterPool >= 0
                    && gs.curEncounterPool < gs.encounterPoolScriptableObject.Length
                    && gs.encounterPoolScriptableObject[gs.curEncounterPool] != null)
                {
                    level = Mathf.Max(1, gs.encounterPoolScriptableObject[gs.curEncounterPool].encounterLevel);
                }
            }
            catch { level = 1; }

            try { m.curLevel = Mathf.Max(1, level); } catch { }
            try { m.curExp = gs.GetTotalExpForLevel(m.curLevel); } catch { }
            try { gs.SetUniqueIDAndInitializeMon(m); } catch { }
            try { level = Mathf.Max(1, m.curLevel); } catch { }

            int monId = -1;
            string monKey = "";
            bool shiny = false;
            try { monId = m.monScriptableObject.id; } catch { try { monId = m.monID; } catch { } }
            try { monKey = m.monScriptableObject.name ?? ""; } catch { }
            if (string.IsNullOrWhiteSpace(monKey))
            {
                try { monKey = m.monScriptableObject.monName ?? ""; } catch { }
            }
            try { shiny = m.isShiny; } catch { }

            SendLine("WORLD_SPAWN_GEN_RESULT|" +
                Escape(runtimePlayerId) + "|" +
                Escape(requestId) + "|" +
                monId.ToString(CultureInfo.InvariantCulture) + "|" +
                Escape(monKey) + "|" +
                (shiny ? "1" : "0") + "|" +
                level.ToString(CultureInfo.InvariantCulture) + "|" +
                Escape(GetEncounterVersionRequirementLabel(gs, m)));

            Dbg("Generated official server-owned shared-version overworld spawn req=" + requestId + " zone=" + zoneId + " rarity=" + rarity + " mon=" + monKey + " id=" + monId + " shiny=" + shiny + " level=" + level + " versionRequirement=" + GetEncounterVersionRequirementLabel(gs, m) + " pos=(" + F(x) + "," + F(y) + "," + F(z) + ")");
        }
        catch (Exception ex)
        {
            Dbg("HandleWorldSpawnGenerateRequest failed: " + ex.Message);
        }
    }

    private Mon GetOfficialServerSharedEncounterMon(GameScript gs, string rarity, bool waterMonOnly)
    {
        try
        {
            EncounterPoolScriptableObject pool = GetCurrentEncounterPool(gs);
            if (pool == null)
                return null;

            string r = (rarity ?? "").Trim().ToLowerInvariant();
            if (r == "rare")
            {
                Mon rare = ChooseSharedEncounterFromList(pool.rareMons, waterMonOnly);
                if (rare != null) return rare;
                Mon uncommonFallback = ChooseSharedEncounterFromList(pool.uncommonMons, waterMonOnly);
                if (uncommonFallback != null) return uncommonFallback;
                return ChooseSharedEncounterFromList(pool.commonMons, waterMonOnly);
            }
            if (r == "uncommon")
            {
                Mon uncommon = ChooseSharedEncounterFromList(pool.uncommonMons, waterMonOnly);
                if (uncommon != null) return uncommon;
                return ChooseSharedEncounterFromList(pool.commonMons, waterMonOnly);
            }
            return ChooseSharedEncounterFromList(pool.commonMons, waterMonOnly);
        }
        catch (Exception ex)
        {
            Dbg("GetOfficialServerSharedEncounterMon failed: " + ex.Message);
            return null;
        }
    }

    private EncounterPoolScriptableObject GetCurrentEncounterPool(GameScript gs)
    {
        try
        {
            if (gs == null || gs.encounterPoolScriptableObject == null)
                return null;
            int idx = gs.curEncounterPool;
            if (idx < 0 || idx >= gs.encounterPoolScriptableObject.Length)
                return null;
            return gs.encounterPoolScriptableObject[idx];
        }
        catch { return null; }
    }

    private Mon ChooseSharedEncounterFromList(List<EncounterEntry> entries, bool waterMonOnly)
    {
        try
        {
            if (entries == null || entries.Count <= 0)
                return null;
            List<MonScriptableObject> candidates = new List<MonScriptableObject>();
            for (int i = 0; i < entries.Count; i++)
            {
                EncounterEntry e = entries[i];
                if (!IsSharedEncounterEntryAllowed(e, waterMonOnly))
                    continue;
                if (e.mon != null)
                    candidates.Add(e.mon);
            }
            if (candidates.Count <= 0)
                return null;
            MonScriptableObject chosen = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            return chosen != null ? new Mon(chosen) : null;
        }
        catch { return null; }
    }

    private bool IsSharedEncounterEntryAllowed(EncounterEntry e, bool waterMonOnly)
    {
        try
        {
            if (e == null || e.mon == null)
                return false;
            if (e.versionRequirement != VersionRequirement.None)
                return false;

            if (e.timeRequirement == TimeRequirement.Day && GameScript.nightTime)
                return false;
            if (e.timeRequirement == TimeRequirement.Night && !GameScript.nightTime)
                return false;

            if (waterMonOnly)
            {
                return e.waterRequirement == WaterRequirement.Waterwalk || e.waterRequirement == WaterRequirement.Both;
            }
            return e.waterRequirement == WaterRequirement.None;
        }
        catch { return false; }
    }

    private string GetEncounterVersionRequirementLabel(GameScript gs, Mon mon)
    {
        try
        {
            if (mon == null || mon.monScriptableObject == null)
                return "unknown";
            EncounterPoolScriptableObject pool = GetCurrentEncounterPool(gs);
            if (pool == null)
                return "unknown";
            MonScriptableObject mso = mon.monScriptableObject;
            string found = null;
            string shared = FindVersionRequirementInEntries(pool.commonMons, mso);
            if (IsSharedVersionRequirementLabel(shared)) return "None";
            if (!string.IsNullOrEmpty(shared)) found = shared;
            shared = FindVersionRequirementInEntries(pool.uncommonMons, mso);
            if (IsSharedVersionRequirementLabel(shared)) return "None";
            if (!string.IsNullOrEmpty(shared)) found = found ?? shared;
            shared = FindVersionRequirementInEntries(pool.rareMons, mso);
            if (IsSharedVersionRequirementLabel(shared)) return "None";
            if (!string.IsNullOrEmpty(shared)) found = found ?? shared;
            shared = FindVersionRequirementInEntries(pool.veryRareMons, mso);
            if (IsSharedVersionRequirementLabel(shared)) return "None";
            if (!string.IsNullOrEmpty(shared)) found = found ?? shared;
            return string.IsNullOrEmpty(found) ? "unknown" : found;
        }
        catch { return "unknown"; }
    }

    private string FindVersionRequirementInEntries(List<EncounterEntry> entries, MonScriptableObject mso)
    {
        try
        {
            if (entries == null || mso == null)
                return null;
            int id = -999999;
            try { id = mso.id; } catch { }
            string name = "";
            try { name = mso.name ?? ""; } catch { }
            for (int i = 0; i < entries.Count; i++)
            {
                EncounterEntry e = entries[i];
                if (e == null || e.mon == null)
                    continue;
                bool match = object.ReferenceEquals(e.mon, mso);
                if (!match)
                {
                    try { match = e.mon.id == id && id >= 0; } catch { }
                }
                if (!match && !string.IsNullOrEmpty(name))
                {
                    try { match = string.Equals(e.mon.name ?? "", name, StringComparison.OrdinalIgnoreCase); } catch { }
                }
                if (match)
                    return e.versionRequirement.ToString();
            }
        }
        catch { }
        return null;
    }

    private bool IsSharedVersionRequirementLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            return false;
        return string.Equals(label.Trim(), "None", StringComparison.OrdinalIgnoreCase);
    }

    private void HandleWorldSpawnsSnapshot(string body)
    {
        if (settings == null || !settings.WorldSpawnsEnabled)
            return;

        if (string.IsNullOrWhiteSpace(body))
            return;

        string[] records = body.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string rec in records)
        {
            string[] p = rec.Split('|');
            ApplyWorldSpawnRecord(p);
        }
    }

    private void HandleWorldSpawnAdd(string body)
    {
        if (settings == null || !settings.WorldSpawnsEnabled)
            return;

        string[] p = (body ?? "").Split('|');
        string spawnId = p.Length > 0 ? Unescape(p[0]) : "";
        try
        {
            if (!string.IsNullOrWhiteSpace(spawnId) && remoteWorldCatchFx.ContainsKey(spawnId))
                FinishRemoteWorldSpawnCatchFx(spawnId, false);
        }
        catch { }

        ApplyWorldSpawnRecord(p);
    }

    private void HandleWorldSpawnRemove(string body)
    {
        string spawnId = Unescape((body ?? "").Split('|')[0]);
        if (MMOWorldSpawnCatchRuntime.ShouldDeferRemove(spawnId))
        {
            Dbg("Deferring local WORLD_SPAWN_REMOVE for active catch attempt " + spawnId);
            return;
        }
        FinishRemoteWorldSpawnCatchFx(spawnId, true);
        RemoveWorldSpawn(spawnId);
    }

    private void HandleWorldSpawnBusy(string body)
    {
        string[] p = (body ?? "").Split('|');
        string spawnId = p.Length > 0 ? Unescape(p[0]) : "";
        WorldSpawnVisual spawn;
        if (worldSpawns.TryGetValue(spawnId, out spawn) && spawn != null)
        {
            spawn.claimPending = true;
            spawn.lastSeen = Time.time;
            if (spawn.marker != null)
                spawn.marker.ClaimPending = true;
        }
    }

    private void HandleWorldSpawnLockUpdate(string body)
    {
        string[] p = (body ?? "").Split('|');
        string spawnId = p.Length > 0 ? Unescape(p[0]) : "";
        string lockState = p.Length > 1 ? Unescape(p[1]) : "public";
        string personalOwnerId = p.Length > 2 ? Unescape(p[2]) : "";
        string reason = p.Length > 3 ? Unescape(p[3]) : "";
        if (string.IsNullOrWhiteSpace(spawnId))
            return;

        try
        {
            WorldSpawnVisual spawn;
            if (!worldSpawns.TryGetValue(spawnId, out spawn) || spawn == null)
            {
                Dbg("WORLD_SPAWN_LOCK_UPDATE for missing local spawn " + spawnId + "; waiting for add/snapshot repair.");
                return;
            }

            spawn.lockState = string.IsNullOrWhiteSpace(lockState) ? "public" : lockState;
            spawn.personalOwnerId = personalOwnerId ?? "";
            spawn.claimPending = false;
            spawn.lastSeen = Time.time;

            if (spawn.marker != null)
            {
                spawn.marker.LockState = spawn.lockState;
                spawn.marker.PersonalOwnerId = spawn.personalOwnerId;
                spawn.marker.ClaimPending = false;
                spawn.marker.CaughtAndHidden = false;
                spawn.marker.CatchResultSent = false;
            }

            if (spawn.obj != null)
                spawn.obj.SetActive(true);
            if (spawn.renderer != null)
            {
                spawn.renderer.enabled = true;
                spawn.renderer.color = Color.white;
            }

            Dbg("Updated world spawn lock without despawn " + spawnId + " lock=" + spawn.lockState + " owner=" + spawn.personalOwnerId + " reason=" + reason);
        }
        catch (Exception ex)
        {
            Dbg("HandleWorldSpawnLockUpdate failed: " + ex.Message);
        }
    }

    private void HandleWorldSpawnCatchStart(string body)
    {
        string[] p = (body ?? "").Split('|');
        string ownerId = p.Length > 0 ? Unescape(p[0]) : "";
        string spawnId = p.Length > 1 ? Unescape(p[1]) : "";
        if (string.IsNullOrWhiteSpace(spawnId))
            return;

        try
        {
            if (remoteWorldCatchFx.ContainsKey(spawnId))
            {
                Dbg("Ignoring duplicate WORLD_SPAWN_CATCH_START for " + spawnId);
                return;
            }

            WorldSpawnVisual spawn;
            if (worldSpawns.TryGetValue(spawnId, out spawn) && spawn != null)
            {
                spawn.claimPending = true;
                spawn.lastSeen = Time.time;
                if (spawn.marker != null)
                    spawn.marker.ClaimPending = true;
            }

            StartCoroutine(PlayRemoteWorldSpawnCatchFx(ownerId, spawnId));
            Dbg("Started remote catch FX for " + spawnId + " owner=" + ownerId);
        }
        catch (Exception ex)
        {
            Dbg("HandleWorldSpawnCatchStart failed: " + ex.Message);
        }
    }


    private void HandleWorldSpawnCatchEnd(string body)
    {
        string[] p = (body ?? "").Split('|');
        string ownerId = p.Length > 0 ? Unescape(p[0]) : "";
        string spawnId = p.Length > 1 ? Unescape(p[1]) : "";
        string result = p.Length > 2 ? Unescape(p[2]) : "";
        bool caught = result.Equals("caught", StringComparison.OrdinalIgnoreCase)
            || result.Equals("success", StringComparison.OrdinalIgnoreCase)
            || result == "1"
            || result.Equals("true", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(spawnId))
            return;

        try
        {
            Dbg("HandleWorldSpawnCatchEnd spawn=" + spawnId + " owner=" + ownerId + " result=" + result);
            FinishRemoteWorldSpawnCatchFx(spawnId, caught);

            if (caught)
                RemoveWorldSpawn(spawnId);
            else
            {
                WorldSpawnVisual spawn;
                if (worldSpawns.TryGetValue(spawnId, out spawn) && spawn != null)
                {
                    spawn.claimPending = false;
                    spawn.lastSeen = Time.time;
                    if (spawn.marker != null)
                    {
                        spawn.marker.ClaimPending = false;
                        spawn.marker.CaughtAndHidden = false;
                        spawn.marker.CatchResultSent = false;
                    }

                    if (spawn.obj != null)
                        spawn.obj.SetActive(true);
                }
            }
        }
        catch (Exception ex)
        {
            Dbg("HandleWorldSpawnCatchEnd failed: " + ex.Message);
        }
    }



    private void HandleWorldSpawnClaimOk(string body)
    {
        string spawnId = Unescape((body ?? "").Split('|')[0]);
        try
        {
            WorldSpawnVisual spawn;
            if (worldSpawns.TryGetValue(spawnId, out spawn) && spawn != null)
            {
                spawn.claimPending = true;
                spawn.lastSeen = Time.time;
                if (spawn.marker != null)
                    spawn.marker.ClaimPending = true;
            }
            MMOWorldSpawnCatchRuntime.NoteAttempt(spawnId);
            Dbg("Server accepted world spawn claim " + spawnId + "; keeping local vanilla encounter object until catch result.");
        }
        catch { }
    }

    private void HandleWorldSpawnClaimFail(string body)
    {
        string[] p = (body ?? "").Split('|');
        string spawnId = p.Length > 0 ? Unescape(p[0]) : "";
        string reason = p.Length > 1 ? Unescape(p[1]) : "Claim failed";
        WorldSpawnVisual spawn;
        if (worldSpawns.TryGetValue(spawnId, out spawn) && spawn != null)
        {
            spawn.claimPending = false;
            if (spawn.marker != null)
            {
                spawn.marker.ClaimPending = false;
                spawn.marker.CaughtAndHidden = false;
                spawn.marker.CatchResultSent = false;
            }
            if (spawn.obj != null)
                spawn.obj.SetActive(true);
        }
        if (!string.IsNullOrWhiteSpace(reason) && reason.IndexOf("permission", StringComparison.OrdinalIgnoreCase) >= 0)
            ShowBattleInfoWindow("Capture Locked", reason);
        else
            ShowBattleInfoWindow("World Spawn", reason);
    }

    private void ApplyWorldSpawnRecord(string[] p)
    {
        try
        {
            if (p == null || p.Length < 11)
                return;

            string id = Unescape(p[0]);
            string cluster = Unescape(p[1]);
            string scene = Unescape(p[2]);
            float x = ParseFloat(p[3]);
            float y = ParseFloat(p[4]);
            float z = ParseFloat(p[5]);
            int monId = ParseInt(p[6]);
            bool shiny = p[7] == "1" || p[7].Equals("true", StringComparison.OrdinalIgnoreCase);
            int level = ParseInt(p[8]);
            string state = Unescape(p[9]);
            string monKey = p.Length > 11 ? Unescape(p[11]) : "";
            string lockState = p.Length > 12 ? Unescape(p[12]) : "public";
            string personalOwnerId = p.Length > 13 ? Unescape(p[13]) : "";
            string monSaveB64 = p.Length > 14 ? Unescape(p[14]) : "";

            if (string.IsNullOrWhiteSpace(id))
                return;

            if (!string.Equals(state, "available", StringComparison.OrdinalIgnoreCase))
            {
                RemoveWorldSpawn(id);
                return;
            }

            WorldSpawnVisual existingForRelease;
            if (worldSpawns.TryGetValue(id, out existingForRelease) && existingForRelease != null && existingForRelease.claimPending)
                FinishRemoteWorldSpawnCatchFx(id, false);

            string localScene = GetLocalSceneName();
            if (!string.IsNullOrWhiteSpace(scene)
                && !string.Equals(scene, "unknown", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(localScene, scene, StringComparison.Ordinal))
            {
                RemoveWorldSpawn(id);
                return;
            }

            Vector3 pos = new Vector3(x, y, Mathf.Abs(z) > 0.0001f ? z : y * 0.01f);
            WorldSpawnVisual spawn;
            if (!worldSpawns.TryGetValue(id, out spawn) || spawn == null || spawn.obj == null)
            {
                spawn = CreateWorldSpawnVisual(id, scene, pos, monId, monKey, shiny, level, monSaveB64);
                worldSpawns[id] = spawn;
                Dbg("Created server world spawn " + id + " monId=" + monId + " monKey=" + monKey + " shiny=" + shiny + " scene=" + scene + " resolved=" + DescribeWorldSpawnResolvedMon(monId, monKey));
            }

            spawn.scene = scene;
            spawn.pos = pos;
            spawn.monId = monId;
            spawn.monKey = monKey;
            spawn.shiny = shiny;
            spawn.level = level;
            spawn.state = state;
            spawn.lockState = string.IsNullOrWhiteSpace(lockState) ? "public" : lockState;
            spawn.personalOwnerId = personalOwnerId ?? "";
            spawn.monSaveB64 = monSaveB64 ?? "";
            spawn.claimPending = false;
            if (spawn.marker != null)
                spawn.marker.ClaimPending = false;
            spawn.lastSeen = Time.time;

            if (spawn.obj != null)
            {
                if (!spawn.usesVanillaObject)
                {
                    spawn.obj.transform.position = GetWorldSpawnRenderPosition(pos);
                    spawn.obj.transform.localScale = Vector3.one * Mathf.Max(0.05f, settings.WorldSpawnScale);
                }
                // Real server spawns are created by GameScript.SpawnMonAtPos.
                // Do not touch their scale/offset here or we stop matching vanilla overworld mons.
            }

            if (spawn.renderer != null)
            {
                spawn.renderer.enabled = true;
                spawn.renderer.sortingOrder = 1;
            }
            if (spawn.marker != null)
            {
                spawn.marker.ClaimPending = spawn.claimPending;
                spawn.marker.LockState = spawn.lockState;
                spawn.marker.PersonalOwnerId = spawn.personalOwnerId;
                spawn.marker.MonSaveB64 = spawn.monSaveB64;
                if (!spawn.claimPending && string.Equals(spawn.lockState, "public", StringComparison.OrdinalIgnoreCase))
                {
                    spawn.marker.CaughtAndHidden = false;
                    spawn.marker.CatchResultSent = false;
                }
            }

            if (spawn.obj != null && !spawn.claimPending)
                spawn.obj.SetActive(true);

            RefreshWorldSpawnLabel(spawn);
        }
        catch (Exception ex)
        {
            Dbg("ApplyWorldSpawnRecord failed: " + ex.Message);
        }
    }

    private Vector3 GetWorldSpawnRenderPosition(Vector3 pos)
    {
        float ox = 0f;
        float oy = 0f;
        try
        {
            if (settings != null)
            {
                ox = settings.WorldSpawnOffsetX;
                oy = settings.WorldSpawnOffsetY;
            }
        }
        catch { }

        float x = pos.x + ox;
        float y = pos.y + oy;
        return new Vector3(x, y, y * 0.01f);
    }

    private WorldSpawnVisual CreateWorldSpawnVisual(string id, string scene, Vector3 pos, int monId, string monKey, bool shiny, int level, string monSaveB64)
    {
        GameScript gs = GetGameScript();
        if (gs == null || gs._OverworldMons == null)
        {
            Dbg("CreateWorldSpawnVisual: no GameScript/_OverworldMons; falling back to placeholder object.");
            return CreatePlaceholderWorldSpawnVisual(id, scene, pos, monId, monKey, shiny, level);
        }

        Mon mon = CreateServerWorldMon(gs, id, monId, monKey, shiny, level, pos, monSaveB64);
        if (mon == null)
        {
            Dbg("CreateWorldSpawnVisual: could not create Mon for monId=" + monId + " monKey=" + monKey + "; falling back to placeholder object.");
            return CreatePlaceholderWorldSpawnVisual(id, scene, pos, monId, monKey, shiny, level);
        }

        string objName = mon.objName;
        Vector2 spawnPos = new Vector2(pos.x, pos.y);

        try
        {
            // Use the game's exact overworld spawn function/prefab path.
            // false preserves the server-chosen shiny/species fields already placed on the Mon.
            gs.SpawnMonAtPos(mon, spawnPos, gs._OverworldMons, false, "grass");
        }
        catch (Exception ex)
        {
            Dbg("GameScript.SpawnMonAtPos failed for server spawn: " + ex.Message);
            return CreatePlaceholderWorldSpawnVisual(id, scene, pos, monId, monKey, shiny, level);
        }

        GameObject go = FindSpawnedWorldMonObject(gs, objName, pos);
        if (go == null)
        {
            Dbg("CreateWorldSpawnVisual: vanilla SpawnMonAtPos ran but object was not found; falling back to placeholder object.");
            return CreatePlaceholderWorldSpawnVisual(id, scene, pos, monId, monKey, shiny, level);
        }

        go.name = "MMOnsterpatchWorldSpawn_" + SafeObjectName(id);
        // Keep vanilla SpawnMonAtPos transform/scale exactly as the game created it.

        MMOWorldSpawnMarker marker = go.GetComponent<MMOWorldSpawnMarker>();
        if (marker == null)
            marker = go.AddComponent<MMOWorldSpawnMarker>();
        marker.SpawnId = id;
        marker.Scene = scene;
        marker.MonId = monId;
        marker.MonKey = monKey ?? "";
        marker.Shiny = shiny;
        marker.Level = level;
        marker.ClaimPending = false;
        marker.CatchResultSent = false;
        marker.CaughtAndHidden = false;
        marker.LockState = "public";
        marker.PersonalOwnerId = "";
        marker.MonSaveB64 = monSaveB64 ?? "";

        MonObject mo = null;
        SpriteRenderer renderer = null;
        try { mo = go.GetComponent<MonObject>() ?? go.GetComponentInChildren<MonObject>(); } catch { }
        try
        {
            if (mo != null && mo.sr != null)
                renderer = mo.sr;
            else
                renderer = go.GetComponent<SpriteRenderer>() ?? go.GetComponentInChildren<SpriteRenderer>();
        }
        catch { }

        WorldSpawnVisual spawn = new WorldSpawnVisual
        {
            id = id,
            scene = scene,
            obj = go,
            renderer = renderer,
            label = null,
            pos = pos,
            monObject = mo,
            marker = marker,
            usesVanillaObject = true,
            frames = null,
            frameIndex = 0,
            frameTimer = 0f,
            monId = monId,
            monKey = monKey,
            shiny = shiny,
            level = level,
            state = "available",
            lockState = "public",
            personalOwnerId = "",
            monSaveB64 = monSaveB64 ?? "",
            lastSeen = Time.time
        };

        if (settings != null && settings.ShowWorldSpawnLabels)
            EnsureWorldSpawnLabel(spawn);

        return spawn;
    }

    private WorldSpawnVisual CreatePlaceholderWorldSpawnVisual(string id, string scene, Vector3 pos, int monId, string monKey, bool shiny, int level)
    {
        GameObject go = new GameObject("MMOnsterpatchWorldSpawn_" + SafeObjectName(id));
        DontDestroyOnLoad(go);
        go.transform.position = GetWorldSpawnRenderPosition(pos);
        go.transform.localScale = Vector3.one * Mathf.Max(0.05f, settings != null ? settings.WorldSpawnScale : 0.7f);

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = GetWorldSpawnSprite(monId, monKey, shiny);
        renderer.sortingOrder = 1;

        MMOWorldSpawnMarker marker = go.AddComponent<MMOWorldSpawnMarker>();
        marker.SpawnId = id;
        marker.Scene = scene;
        marker.MonId = monId;
        marker.MonKey = monKey ?? "";
        marker.Shiny = shiny;
        marker.Level = level;
        marker.ClaimPending = false;
        marker.CatchResultSent = false;
        marker.CaughtAndHidden = false;

        WorldSpawnVisual spawn = new WorldSpawnVisual
        {
            id = id,
            scene = scene,
            obj = go,
            renderer = renderer,
            label = null,
            pos = pos,
            monObject = null,
            marker = marker,
            usesVanillaObject = false,
            frames = null,
            frameIndex = 0,
            frameTimer = 0f,
            monId = monId,
            monKey = monKey,
            shiny = shiny,
            level = level,
            state = "available",
            lastSeen = Time.time
        };

        if (settings != null && settings.ShowWorldSpawnLabels)
            EnsureWorldSpawnLabel(spawn);

        return spawn;
    }

    private Mon CreateServerWorldMon(GameScript gs, string spawnId, int monId, string monKey, bool shiny, int level, Vector3 pos, string monSaveB64)
    {
        try
        {
            if (gs == null)
                return null;

            if (!string.IsNullOrWhiteSpace(monSaveB64))
            {
                try
                {
                    string saveString = Encoding.UTF8.GetString(Convert.FromBase64String(monSaveB64));
                    Mon exact = gs.GetMonFromSaveString(saveString);
                    if (exact != null)
                    {
                        try { exact.worldPosX = pos.x; exact.worldPosY = pos.y; } catch { }
                        return exact;
                    }
                }
                catch (Exception ex)
                {
                    Dbg("CreateServerWorldMon exact save decode failed: " + ex.Message);
                }
            }

            MonScriptableObject mso = null;
            if (!string.IsNullOrWhiteSpace(monKey))
                mso = FindMonScriptableObjectByKey(gs, monKey);
            if (mso == null && monId >= 0 && gs.monScriptableObject != null && monId < gs.monScriptableObject.Length)
                mso = gs.monScriptableObject[monId];
            if (mso == null)
                return null;

            Mon m = new Mon();
            m.monScriptableObject = mso;
            try { m.monID = mso.id; } catch { m.monID = monId; }
            if (m.monID < 0) m.monID = monId;
            m.objName = "MMOnsterpatchWorldSpawn_" + SafeObjectName(spawnId);
            m.nickName = "";
            m.gender = UnityEngine.Random.Range(0, 2);
            m.isShiny = shiny;
            m.curLevel = Mathf.Max(1, level > 0 ? level : 1);
            try { m.curExp = gs.GetTotalExpForLevel(m.curLevel); } catch { m.curExp = 0; }
            m.worldPosX = pos.x;
            m.worldPosY = pos.y;
            m.metLevel = m.curLevel;
            try { m.metLocation = gs.curLocation; } catch { m.metLocation = "Server World"; }
            if (string.IsNullOrWhiteSpace(m.metLocation)) m.metLocation = "Server World";
            if (m.passiveAbilityIDs == null || m.passiveAbilityIDs.Length != 4)
                m.passiveAbilityIDs = new int[4];
            for (int i = 0; i < m.passiveAbilityIDs.Length; i++)
                m.passiveAbilityIDs[i] = -1;
            if (m.moveIDs == null || m.moveIDs.Length != 4)
                m.moveIDs = new int[4];
            for (int i = 0; i < m.moveIDs.Length; i++)
                m.moveIDs[i] = -1;
            if (m.statGrades == null || m.statGrades.Length != 6)
                m.statGrades = new int[6];
            for (int i = 0; i < m.statGrades.Length; i++)
                m.statGrades[i] = UnityEngine.Random.Range(0, 5);
            if (m.statBoostLevels == null || m.statBoostLevels.Length != 6)
                m.statBoostLevels = new int[6];
            if (m.upgradeChoice == null || m.upgradeChoice.Length != 13)
                m.upgradeChoice = new int[13];
            for (int i = 0; i < m.upgradeChoice.Length; i++)
                m.upgradeChoice[i] = -1;
            m.vibe = UnityEngine.Random.Range(0, 20);
            try { m.SetGrowthBasedOnLevel(m.curLevel, gs, false, true); } catch { }
            try { m.RefreshStatsWithLevelAndStuff(true); } catch { }
            return m;
        }
        catch (Exception ex)
        {
            Dbg("CreateServerWorldMon failed: " + ex.Message);
            return null;
        }
    }

    private GameObject FindSpawnedWorldMonObject(GameScript gs, string objName, Vector3 pos)
    {
        try
        {
            if (gs == null || gs._OverworldMons == null)
                return null;

            GameObject best = null;
            float bestDist = 9999f;
            for (int i = gs._OverworldMons.childCount - 1; i >= 0; i--)
            {
                Transform child = gs._OverworldMons.GetChild(i);
                if (child == null || child.gameObject == null)
                    continue;
                if (!string.IsNullOrEmpty(objName) && !string.Equals(child.gameObject.name, objName, StringComparison.Ordinal))
                    continue;
                float dist = Vector2.Distance(new Vector2(child.position.x, child.position.y), new Vector2(pos.x, pos.y));
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = child.gameObject;
                }
            }
            return best;
        }
        catch { return null; }
    }

    private string DescribeWorldSpawnResolvedMon(int monId, string monKey)
    {
        try
        {
            GameScript gs = GetGameScript();
            if (gs == null || gs.monScriptableObject == null)
                return "<no GameScript/mon table>";

            MonScriptableObject mso = null;
            if (!string.IsNullOrWhiteSpace(monKey))
                mso = FindMonScriptableObjectByKey(gs, monKey);
            if (mso == null && monId >= 0 && monId < gs.monScriptableObject.Length)
                mso = gs.monScriptableObject[monId];
            if (mso == null)
                return "<not found>";

            string monName = "";
            string objName = "";
            try { monName = mso.monName; } catch { }
            try { objName = mso.name; } catch { }
            return monName + " / " + objName;
        }
        catch (Exception ex)
        {
            return "<resolve error: " + ex.Message + ">";
        }
    }

    private Sprite[] GetWorldSpawnFrames(int monId, string monKey, bool shiny)
    {
        try
        {
            GameScript gs = GetGameScript();
            if (gs != null && gs.monScriptableObject != null)
            {
                MonScriptableObject mso = null;

                if (!string.IsNullOrWhiteSpace(monKey))
                    mso = FindMonScriptableObjectByKey(gs, monKey);

                if (mso == null && monId >= 0 && monId < gs.monScriptableObject.Length)
                    mso = gs.monScriptableObject[monId];

                Sprite[] frames = GetPreferredMonWorldFrames(mso, shiny);
                if (frames != null && frames.Length > 0)
                    return frames;
            }
        }
        catch { }

        Sprite fallback = GetWorldSpawnSprite(monId, monKey, shiny);
        return fallback != null ? new Sprite[] { fallback } : new Sprite[0];
    }

    private Sprite[] GetPreferredMonWorldFrames(MonScriptableObject mso, bool shiny)
    {
        if (mso == null)
            return null;

        try
        {
            Sprite[] frames = shiny ? mso.spritesShiny : mso.sprites;
            if ((frames == null || frames.Length == 0) && shiny)
                frames = mso.sprites;

            if (frames != null && frames.Length > 0)
            {
                List<Sprite> clean = new List<Sprite>();
                for (int i = 0; i < frames.Length; i++)
                    if (frames[i] != null)
                        clean.Add(frames[i]);

                if (clean.Count > 0)
                    return clean.ToArray();
            }
        }
        catch { }

        try
        {
            Sprite direct = shiny ? mso.spriteShiny : mso.sprite;
            if (direct != null)
                return new Sprite[] { direct };
        }
        catch { }

        try
        {
            if (shiny && mso.sprite != null)
                return new Sprite[] { mso.sprite };
        }
        catch { }

        return null;
    }

    private Sprite GetWorldSpawnSprite(int monId, string monKey, bool shiny)
    {
        Sprite sprite = null;

        // v0.2: prefer the MonScriptableObject summary sprite. The v0.1 test used
        // follower-animation frames first, which proved the server object pipeline worked
        // but could show the wrong-looking icon for some species.
        try
        {
            GameScript gs = GetGameScript();
            if (gs != null && gs.monScriptableObject != null)
            {
                MonScriptableObject mso = null;

                if (!string.IsNullOrWhiteSpace(monKey))
                    mso = FindMonScriptableObjectByKey(gs, monKey);

                if (mso == null && monId >= 0 && monId < gs.monScriptableObject.Length)
                    mso = gs.monScriptableObject[monId];

                sprite = GetPreferredMonSprite(mso, shiny);
                if (sprite != null)
                    return sprite;
            }
        }
        catch { }

        try
        {
            sprite = GetSpriteFromFollowerPayload("MPFOL:" + monId.ToString() + ":" + (shiny ? "1" : "0") + ":0");
        }
        catch { }

        if (sprite != null)
            return sprite;

        return GetWorldSpawnFallbackSprite();
    }

    private MonScriptableObject FindMonScriptableObjectByKey(GameScript gs, string monKey)
    {
        if (gs == null || gs.monScriptableObject == null || string.IsNullOrWhiteSpace(monKey))
            return null;

        string want = NormalizeMonLookupKey(monKey);
        if (string.IsNullOrEmpty(want))
            return null;

        for (int i = 0; i < gs.monScriptableObject.Length; i++)
        {
            MonScriptableObject mso = gs.monScriptableObject[i];
            if (mso == null)
                continue;

            try
            {
                if (NormalizeMonLookupKey(mso.monName) == want)
                    return mso;
            }
            catch { }

            try
            {
                if (NormalizeMonLookupKey(mso.name) == want)
                    return mso;
            }
            catch { }
        }

        return null;
    }

    private static string NormalizeMonLookupKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";
        string s = value.Trim().ToLowerInvariant();
        StringBuilder sb = new StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                sb.Append(c);
        }
        return sb.ToString();
    }

    private Sprite GetPreferredMonSprite(MonScriptableObject mso, bool shiny)
    {
        if (mso == null)
            return null;

        try
        {
            Sprite[] frames = GetPreferredMonWorldFrames(mso, shiny);
            if (frames != null && frames.Length > 0 && frames[0] != null)
                return frames[0];
        }
        catch { }

        try
        {
            Sprite direct = shiny ? mso.spriteShiny : mso.sprite;
            if (direct != null)
                return direct;
        }
        catch { }

        try
        {
            if (shiny && mso.sprite != null)
                return mso.sprite;
        }
        catch { }

        return null;
    }

    private Sprite GetWorldSpawnFallbackSprite()
    {
        if (cachedWorldSpawnFallbackSprite != null)
            return cachedWorldSpawnFallbackSprite;

        Texture2D tex = new Texture2D(16, 16, TextureFormat.ARGB32, false);
        Color fill = new Color(0.95f, 0.65f, 0.15f, 1f);
        Color edge = new Color(0.25f, 0.14f, 0.05f, 1f);
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                bool border = x == 0 || y == 0 || x == 15 || y == 15;
                tex.SetPixel(x, y, border ? edge : fill);
            }
        }
        tex.Apply();

        cachedWorldSpawnFallbackSprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
        cachedWorldSpawnFallbackSprite.name = "MMOnsterpatchWorldSpawnFallback";
        return cachedWorldSpawnFallbackSprite;
    }

    private void EnsureWorldSpawnLabel(WorldSpawnVisual spawn)
    {
        if (spawn == null || spawn.obj == null || spawn.label != null)
            return;

        GameObject go = new GameObject("WorldSpawnLabel");
        go.transform.SetParent(spawn.obj.transform, false);
        go.transform.localPosition = new Vector3(0f, settings != null ? settings.WorldSpawnLabelOffsetY : 0.22f, -0.01f);
        TextMesh tm = go.AddComponent<TextMesh>();
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.characterSize = settings != null ? settings.WorldSpawnLabelCharacterSize : 0.010f;
        tm.fontSize = 32;
        tm.color = Color.white;
        tm.text = "";
        spawn.label = tm;
    }

    private void RefreshWorldSpawnLabel(WorldSpawnVisual spawn)
    {
        if (spawn == null)
            return;

        if (settings != null && !settings.ShowWorldSpawnLabels)
        {
            if (spawn.label != null)
                spawn.label.gameObject.SetActive(false);
            return;
        }

        EnsureWorldSpawnLabel(spawn);
        if (spawn.label == null)
            return;

        spawn.label.gameObject.SetActive(true);
        spawn.label.transform.localPosition = new Vector3(0f, settings != null ? settings.WorldSpawnLabelOffsetY : 0.22f, -0.01f);
        spawn.label.characterSize = settings != null ? settings.WorldSpawnLabelCharacterSize : 0.010f;
        string shinyText = spawn.shiny ? " ★" : "";
        string nameText = string.IsNullOrWhiteSpace(spawn.monKey) ? "Spawn" : spawn.monKey.Trim();
        spawn.label.text = nameText + " Lv." + Mathf.Max(1, spawn.level).ToString() + shinyText;
    }

    private void UpdateWorldSpawnVisuals()
    {
        try
        {
            if (settings == null || !settings.WorldSpawnsEnabled)
            {
                ClearWorldSpawns();
                return;
            }

            string localScene = GetLocalSceneName();
            foreach (var kv in worldSpawns.ToList())
            {
                WorldSpawnVisual spawn = kv.Value;
                if (spawn == null || spawn.obj == null)
                {
                    worldSpawns.Remove(kv.Key);
                    continue;
                }

                // Server snapshots refresh lastSeen, but personal encounter rewards can flip
                // owner_only -> public while clients are changing map. Keep the local vanilla
                // object alive longer so an unlock update/add can update metadata instead of
                // looking like a despawn. Actual removals still use WORLD_SPAWN_REMOVE.
                if (Time.time - spawn.lastSeen > 20f && !spawn.claimPending && !remoteWorldCatchFx.ContainsKey(spawn.id))
                {
                    Destroy(spawn.obj);
                    worldSpawns.Remove(kv.Key);
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(spawn.scene)
                    && !string.Equals(spawn.scene, "unknown", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(spawn.scene, localScene, StringComparison.Ordinal))
                {
                    Destroy(spawn.obj);
                    worldSpawns.Remove(kv.Key);
                    continue;
                }

                if (spawn.marker != null)
                {
                    spawn.marker.ClaimPending = spawn.claimPending;
                    if (spawn.marker.CaughtAndHidden)
                    {
                        try { spawn.obj.SetActive(false); } catch { }
                        continue;
                    }
                }

                if (!spawn.usesVanillaObject)
                    spawn.obj.transform.position = GetWorldSpawnRenderPosition(spawn.pos);

                if (spawn.renderer != null)
                    spawn.renderer.color = spawn.claimPending ? new Color(1f, 1f, 1f, 0.45f) : Color.white;
            }
        }
        catch { }
    }

    private GameObject GetGameScriptGameObjectField(GameScript gs, string fieldName)
    {
        try
        {
            if (gs == null || string.IsNullOrWhiteSpace(fieldName))
                return null;
            FieldInfo f = typeof(GameScript).GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f == null)
                return null;
            return f.GetValue(gs) as GameObject;
        }
        catch { return null; }
    }

    private void InvokeGameScriptVector3Method(GameScript gs, string methodName, Vector3 value)
    {
        try
        {
            if (gs == null || string.IsNullOrWhiteSpace(methodName))
                return;
            MethodInfo m = typeof(GameScript).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(Vector3) }, null);
            if (m == null)
                return;
            m.Invoke(gs, new object[] { value });
        }
        catch { }
    }

    private void UpdateRemoteWorldCatchFxTimeouts()
    {
        try
        {
            foreach (RemoteWorldCatchFx fx in remoteWorldCatchFx.Values.ToList())
            {
                if (fx == null || string.IsNullOrWhiteSpace(fx.spawnId))
                    continue;

                if (Time.time - fx.startedAt > 6.5f)
                {
                    Dbg("Cleaning stale remote catch FX for " + fx.spawnId);
                    FinishRemoteWorldSpawnCatchFx(fx.spawnId, false);
                }
            }
        }
        catch { }
    }

    private IEnumerator PlayRemoteWorldSpawnCatchFx(string ownerId, string spawnId)
    {
        WorldSpawnVisual spawn = null;
        try { worldSpawns.TryGetValue(spawnId, out spawn); } catch { }
        if (spawn == null || spawn.obj == null)
            yield break;

        try
        {
            RemotePlayer rp;
            if (!string.IsNullOrWhiteSpace(ownerId) && remotePlayers.TryGetValue(ownerId, out rp) && rp != null && rp.spriteAnimator != null)
                rp.spriteAnimator.PlayCastLong();
        }
        catch { }

        GameObject diamond = null;
        try
        {
            GameScript gs = GetGameScript();
            GameObject prefab = GetGameScriptGameObjectField(gs, "catchDiamondPrefab");
            if (prefab != null)
            {
                diamond = Instantiate(prefab, spawn.obj.transform.position, Quaternion.identity);
                Animator a = diamond.GetComponent<Animator>();
                if (a != null)
                    a.Play("catchDiamondLock");
            }
        }
        catch { }

        try
        {
            remoteWorldCatchFx[spawnId] = new RemoteWorldCatchFx
            {
                spawnId = spawnId,
                ownerId = ownerId,
                diamond = diamond,
                startedAt = Time.time
            };
            spawn.claimPending = true;
            if (spawn.marker != null)
                spawn.marker.ClaimPending = true;
            if (spawn.obj != null)
                spawn.obj.SetActive(false);
        }
        catch { }

        yield return new WaitForSeconds(0.3f);
        while (true)
        {
            yield return new WaitForSeconds(0.6f);
            try
            {
                RemoteWorldCatchFx fx;
                if (!remoteWorldCatchFx.TryGetValue(spawnId, out fx))
                    yield break;

                if (Time.time - fx.startedAt > 6.0f)
                {
                    FinishRemoteWorldSpawnCatchFx(spawnId, false);
                    yield break;
                }

                if (fx != null && fx.diamond != null)
                {
                    Animator a = fx.diamond.GetComponent<Animator>();
                    if (a != null)
                        a.Play("catchDiamondShake");
                }
            }
            catch { }
        }
    }



    private void FinishRemoteWorldSpawnCatchFx(string spawnId, bool caught)
    {
        if (string.IsNullOrWhiteSpace(spawnId))
            return;

        RemoteWorldCatchFx fx = null;
        try { remoteWorldCatchFx.TryGetValue(spawnId, out fx); } catch { }
        WorldSpawnVisual spawn = null;
        try { worldSpawns.TryGetValue(spawnId, out spawn); } catch { }

        if (caught)
        {
            try
            {
                if (fx != null && fx.diamond != null)
                {
                    Transform target = null;
                    try
                    {
                        RemotePlayer rp;
                        if (!string.IsNullOrWhiteSpace(fx.ownerId) && remotePlayers.TryGetValue(fx.ownerId, out rp) && rp != null)
                            target = rp.visualRoot != null ? rp.visualRoot.transform : (rp.obj != null ? rp.obj.transform : null);
                    }
                    catch { }
                    CatchDiamondScript cds = fx.diamond.GetComponent<CatchDiamondScript>();
                    if (cds != null && target != null)
                        cds.Init(target);
                    Animator a = fx.diamond.GetComponent<Animator>();
                    if (a != null)
                        a.Play("catchDiamondEnd");
                    StartCoroutine(DestroyRemoteCatchDiamondAfterSeconds(fx.diamond, 1.25f));
                }
            }
            catch { }

            try
            {
                if (spawn != null && spawn.obj != null)
                    spawn.obj.SetActive(false);
            }
            catch { }
        }
        else
        {
            try
            {
                if (fx != null && fx.diamond != null)
                    Destroy(fx.diamond);
            }
            catch { }

            try
            {
                if (spawn != null && spawn.obj != null)
                {
                    spawn.obj.SetActive(true);
                    try
                    {
                        GameScript gs = GetGameScript();
                        InvokeGameScriptVector3Method(gs, "WhitePopExplode", spawn.obj.transform.position);
                    }
                    catch { }
                    try { spawn.obj.transform.GetChild(0).GetComponent<Animation>().Play("monFailToCatch"); } catch { }
                }
            }
            catch { }
        }

        try { TryPlayRemotePlayerIdle(fx != null ? fx.ownerId : ""); } catch { }
        try { remoteWorldCatchFx.Remove(spawnId); } catch { }

        try
        {
            if (spawn != null)
            {
                spawn.claimPending = caught;
                if (spawn.marker != null)
                {
                    spawn.marker.ClaimPending = caught;
                    spawn.marker.CaughtAndHidden = caught;
                }
            }
        }
        catch { }
    }

    private IEnumerator DestroyRemoteCatchDiamondAfterSeconds(GameObject diamond, float seconds)
    {
        yield return new WaitForSeconds(Mathf.Max(0.05f, seconds));
        try
        {
            if (diamond != null)
                Destroy(diamond);
        }
        catch { }
    }

    private void TryPlayRemotePlayerIdle(string ownerId)
    {
        try
        {
            RemotePlayer rp;
            if (!string.IsNullOrWhiteSpace(ownerId) && remotePlayers.TryGetValue(ownerId, out rp) && rp != null && rp.spriteAnimator != null)
                rp.spriteAnimator.PlayIdle();
        }
        catch { }
    }







    private bool UpdateWorldSpawnInput()
    {
        try
        {
            if (settings == null || !settings.WorldSpawnsEnabled || !connected || player == null)
                return false;

            bool submitDown = false;
            try { submitDown = Input.GetButtonDown("Submit") || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space); } catch { }
            if (!submitDown)
                return false;

            if (IsBattleRequestInputBlockedByBattleContext())
                return false;

            try
            {
                if (GTSRuntimeHost.IsOpenForAio())
                    return false;
            }
            catch { }

            try
            {
                if (MenuScript.gameState != MenuScript.GameState.Open)
                    return false;
                if (PlayerController.isMoving || GameScript.inCutscene || GameScript.interacting)
                    return false;
            }
            catch { }

            WorldSpawnVisual target = FindNearestWorldSpawn();
            if (target == null)
                return false;

            if (IsWorldSpawnLockedForLocalPlayer(target))
            {
                ShowWorldSpawnNoPermissionFromPatch();
                return true;
            }

            target.claimPending = true;
            if (target.marker != null) target.marker.ClaimPending = true;
            MMOWorldSpawnCatchRuntime.NoteAttempt(target.id);
            SendLine("WORLD_SPAWN_CLAIM|" + Escape(runtimePlayerId) + "|" + Escape(target.id));
            Dbg("Sent WORLD_SPAWN_CLAIM for " + target.id);
            return true;
        }
        catch (Exception ex)
        {
            Dbg("UpdateWorldSpawnInput failed: " + ex.Message);
            return false;
        }
    }

    private bool IsWorldSpawnLockedForLocalPlayer(WorldSpawnVisual spawn)
    {
        try
        {
            if (spawn == null)
                return false;
            string lockState = (spawn.lockState ?? "").Trim().ToLowerInvariant();
            if (lockState != "owner_only")
                return false;
            string owner = spawn.personalOwnerId ?? "";
            return !string.IsNullOrWhiteSpace(owner) && !string.Equals(owner, runtimePlayerId, StringComparison.Ordinal);
        }
        catch { return false; }
    }

    private WorldSpawnVisual FindNearestWorldSpawn()
    {
        if (player == null)
            return null;

        Vector2 origin = new Vector2(player.transform.position.x, player.transform.position.y);
        float maxDist = Mathf.Max(0.05f, settings != null ? settings.WorldSpawnInteractDistance : 0.55f);

        WorldSpawnVisual best = null;
        float bestDist = maxDist;
        foreach (WorldSpawnVisual spawn in worldSpawns.Values)
        {
            if (spawn == null || spawn.obj == null || spawn.claimPending)
                continue;

            Vector2 pos = new Vector2(spawn.obj.transform.position.x, spawn.obj.transform.position.y);
            float dist = Vector2.Distance(origin, pos);
            if (dist <= bestDist)
            {
                bestDist = dist;
                best = spawn;
            }
        }

        return best;
    }

    private void RemoveWorldSpawn(string spawnId)
    {
        if (string.IsNullOrWhiteSpace(spawnId))
            return;

        try
        {
            WorldSpawnVisual spawn;
            if (worldSpawns.TryGetValue(spawnId, out spawn))
            {
                if (spawn != null && spawn.obj != null)
                    Destroy(spawn.obj);
                worldSpawns.Remove(spawnId);
            }
        }
        catch { }
    }

    private void ClearWorldSpawns()
    {
        try
        {
            foreach (WorldSpawnVisual spawn in worldSpawns.Values.ToList())
            {
                if (spawn != null && spawn.obj != null)
                    Destroy(spawn.obj);
            }
            worldSpawns.Clear();
            try
            {
                foreach (RemoteWorldCatchFx fx in remoteWorldCatchFx.Values.ToList())
                    if (fx != null && fx.diamond != null)
                        Destroy(fx.diamond);
                remoteWorldCatchFx.Clear();
            }
            catch { }
        }
        catch { }
    }


    private void HandleRemoteVisualEvent(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return;

        try
        {
            string[] p = body.Split('|');
            if (p.Length < 10)
                return;

            string id = Unescape(p[0]);
            if (id == runtimePlayerId)
                return;

            string name = p.Length > 1 ? Unescape(p[1]) : "";
            string eventType = p.Length > 4 ? Unescape(p[4]) : "";
            float x = p.Length > 5 ? ParseFloat(p[5]) : 0f;
            float y = p.Length > 6 ? ParseFloat(p[6]) : 0f;
            float z = p.Length > 7 ? ParseFloat(p[7]) : 0f;
            int facing = p.Length > 8 ? ParseInt(p[8]) : 0;
            int dir = p.Length > 9 ? ParseInt(p[9]) : facing;
            bool ridingBroom = p.Length > 10 && (p[10] == "1" || p[10].Equals("true", StringComparison.OrdinalIgnoreCase));
            bool waterWalking = p.Length > 11 && (p[11] == "1" || p[11].Equals("true", StringComparison.OrdinalIgnoreCase));
            bool jumpingOrBouncing = p.Length > 12 && (p[12] == "1" || p[12].Equals("true", StringComparison.OrdinalIgnoreCase));
            string bouncingName = p.Length > 13 ? Unescape(p[13]) : "";

            Vector3 eventPos = new Vector3(x, y, Mathf.Abs(z) > 0.0001f ? z : y * 0.01f);

            RemotePlayer remotePlayer;
            if (!remotePlayers.TryGetValue(id, out remotePlayer) || remotePlayer == null || remotePlayer.obj == null)
                return;

            remotePlayer.name = string.IsNullOrWhiteSpace(name) ? remotePlayer.name : name;
            remotePlayer.facing = facing;
            remotePlayer.lastSeen = Time.time;

            ApplyRemoteVisualState(remotePlayer, ridingBroom, waterWalking, jumpingOrBouncing, bouncingName, "event:" + eventType);

            if (eventType.Equals("BROOM_ENTER", StringComparison.OrdinalIgnoreCase))
            {
                remotePlayer.ridingBroom = true;
                ApplyRemoteVisualState(remotePlayer, true, waterWalking, jumpingOrBouncing, bouncingName, "broom-enter");
                TryPlayRemotePlayerRootAnimation(remotePlayer, "playerBroom");
                SpawnRemotePoof(eventPos);
                ApplyRemotePlayerFrame(remotePlayer, true, 0f);
            }
            else if (eventType.Equals("BROOM_EXIT", StringComparison.OrdinalIgnoreCase))
            {
                remotePlayer.ridingBroom = false;
                ApplyRemoteVisualState(remotePlayer, false, waterWalking, jumpingOrBouncing, bouncingName, "broom-exit");
                TryPlayRemotePlayerRootAnimation(remotePlayer, waterWalking ? "playerBouncing" : "playerIdle");
                ApplyRemotePlayerFrame(remotePlayer, remotePlayer.moving, 0f);
            }
            else if (eventType.Equals("CAST", StringComparison.OrdinalIgnoreCase))
            {
                StartCoroutine(PlayRemoteCastFx(remotePlayer, dir, eventPos));
            }
            else if (eventType.Equals("SPELL_AXE", StringComparison.OrdinalIgnoreCase)
                     || eventType.Equals("SPELL_PICKAXE", StringComparison.OrdinalIgnoreCase)
                     || eventType.Equals("SPELL_GLOVE", StringComparison.OrdinalIgnoreCase)
                     || eventType.Equals("SPELL_BLOOM", StringComparison.OrdinalIgnoreCase))
            {
                StartCoroutine(PlayRemoteToolSpellFx(remotePlayer, eventType, dir, eventPos));
            }
            else if (eventType.Equals("WATER_RIPPLE", StringComparison.OrdinalIgnoreCase))
            {
                SpawnRemoteWaterRipple(eventPos);
            }
            else if (eventType.Equals("WATER_ON", StringComparison.OrdinalIgnoreCase))
            {
                ApplyRemoteVisualState(remotePlayer, ridingBroom, true, jumpingOrBouncing, bouncingName, "water-on");
            }
            else if (eventType.Equals("WATER_OFF", StringComparison.OrdinalIgnoreCase))
            {
                ApplyRemoteVisualState(remotePlayer, ridingBroom, false, jumpingOrBouncing, bouncingName, "water-off");
            }
            else if (eventType.Equals("JUMP", StringComparison.OrdinalIgnoreCase) || eventType.Equals("BOUNCE", StringComparison.OrdinalIgnoreCase))
            {
                ApplyRemoteVisualState(remotePlayer, ridingBroom, waterWalking, true, bouncingName, eventType.ToLowerInvariant());
                TryPlayRemotePlayerRootAnimation(remotePlayer, "playerSmallJump");
                StartCoroutine(ClearRemoteJumpOrBounceAfter(id, 0.75f));
            }
        }
        catch (Exception ex)
        {
            Dbg("HandleRemoteVisualEvent failed: " + ex.Message);
        }
    }

    private void HandleRemoteStep(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return;

        string[] p = body.Split('|');
        if (p.Length < 15)
            return;

        string id = Unescape(p[0]);
        if (id == runtimePlayerId)
            return;

        string name = Unescape(p[1]);
        // p[2] cluster, p[3] scene already filtered by server
        float ox = ParseFloat(p[4]);
        float oy = ParseFloat(p[5]);
        float tx = ParseFloat(p[6]);
        float ty = ParseFloat(p[7]);
        int facing = ParseInt(p[8]);
        float duration = ParseFloat(p[9]);
        int design = ParseInt(p[10]);
        int color1 = ParseInt(p[11]);
        int color2 = ParseInt(p[12]);
        bool ridingBroom = p.Length >= 15 && (p[14] == "1" || p[14].Equals("true", StringComparison.OrdinalIgnoreCase));

        Vector3 origin = new Vector3(ox, oy, oy * 0.01f);
        Vector3 target = new Vector3(tx, ty, ty * 0.01f);

        RemotePlayer remotePlayer;
        if (!remotePlayers.TryGetValue(id, out remotePlayer) || remotePlayer.obj == null)
        {
            remotePlayer = CreateRemotePlayer(id, name, origin, design, color1, color2, facing, true, ridingBroom);
            remotePlayers[id] = remotePlayer;
            Dbg("Created step remotePlayer for " + name + " / " + id + " design=" + design + " colors=" + color1 + "," + color2);
        }

        remotePlayer.name = name;
        remotePlayer.lastSeen = Time.time;

        if (remotePlayer.usingFallback && settings.UsePlayerSprite && player != null && player.playerAnimObj != null)
        {
            Dbg("Upgrading fallback remotePlayer to player visual for " + name);
            DestroyFallbackRenderer(remotePlayer);
            TryAttachPlayerVisual(remotePlayer, design, color1, color2, facing, true);
        }

        Vector3 current = remotePlayer.obj.transform.position;
        float originDist = Vector2.Distance(new Vector2(current.x, current.y), new Vector2(origin.x, origin.y));
        if (originDist > Mathf.Max(0.01f, settings.StepOriginSnapDistance))
            remotePlayer.obj.transform.position = WithRemoteSortZ(origin);
        else
            origin = current;

        remotePlayer.stepOrigin = origin;
        remotePlayer.stepTarget = target;
        remotePlayer.stepTimer = 0f;
        remotePlayer.stepDuration = Mathf.Max(0.05f, duration);
        remotePlayer.stepActive = true;
        remotePlayer.targetPos = target;
        remotePlayer.walkCycleTimer = 0f;
        remotePlayer.animCurrentlyMoving = false;
        remotePlayer.animLastFacing = -999;

        ApplyRemotePlayerAppearance(remotePlayer, design, color1, color2, facing, true, ridingBroom);
        EnsureNameplate(remotePlayer);
    }

    private void UpdateRemotePlayer(string id, string name, Vector3 target, int facing, bool moving, int design, int color1, int color2, bool followerEnabled, Vector3 followerTarget, int followerFacing, bool followerMoving, string followerSpriteName, bool followerFlipX, bool ridingBroom, bool waterWalking, bool jumpingOrBouncing, string bouncingName)
    {
        RemotePlayer remotePlayer;
        if (!remotePlayers.TryGetValue(id, out remotePlayer) || remotePlayer.obj == null)
        {
            remotePlayer = CreateRemotePlayer(id, name, target, design, color1, color2, facing, moving, ridingBroom);
            remotePlayers[id] = remotePlayer;
            Dbg("Created remotePlayer for " + name + " / " + id + " design=" + design + " colors=" + color1 + "," + color2);
        }

        remotePlayer.name = name;
        if (!remotePlayer.stepActive)
        {
            if (Vector2.Distance(new Vector2(remotePlayer.targetPos.x, remotePlayer.targetPos.y), new Vector2(target.x, target.y)) > Mathf.Max(0.0001f, settings.RemoteMovingDistanceThreshold))
                remotePlayer.animationRefreshTimer = 0f;
            remotePlayer.targetPos = target;
        }
        remotePlayer.lastSeen = Time.time;
        EnsureNameplate(remotePlayer);

        if (remotePlayer.usingFallback && settings.UsePlayerSprite && player != null && player.playerAnimObj != null)
        {
            Dbg("Upgrading fallback remotePlayer to player visual for " + name);
            DestroyFallbackRenderer(remotePlayer);
            TryAttachPlayerVisual(remotePlayer, design, color1, color2, facing, moving);
        }

        ApplyRemoteFollowerState(remotePlayer, followerEnabled, followerTarget, followerFacing, followerMoving, followerSpriteName, followerFlipX);
        ApplyRemotePlayerAppearance(remotePlayer, design, color1, color2, facing, moving, ridingBroom);
        ApplyRemoteVisualState(remotePlayer, ridingBroom, waterWalking, jumpingOrBouncing, bouncingName, "snapshot");
    }

    private RemotePlayer CreateRemotePlayer(string id, string name, Vector3 pos, int design, int color1, int color2, int facing, bool moving, bool ridingBroom)
    {
        pos = WithRemoteSortZ(pos);

        GameObject go = new GameObject("MMOnsterpatchRemotePlayer_" + SafeObjectName(id));
        DontDestroyOnLoad(go);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one;

        RemotePlayer remotePlayer = new RemotePlayer
        {
            id = id,
            name = name,
            obj = go,
            targetPos = pos,
            lastRenderedPos = pos,
            lastSeen = Time.time
        };

        bool attachedPlayerVisual = TryAttachPlayerVisual(remotePlayer, design, color1, color2, facing, moving);
        if (!attachedPlayerVisual)
            AttachFallbackRenderer(remotePlayer);

        ApplyRemotePlayerAppearance(remotePlayer, design, color1, color2, facing, moving, ridingBroom);
        EnsureNameplate(remotePlayer);
        return remotePlayer;
    }

    private bool TryAttachPlayerVisual(RemotePlayer remotePlayer, int design, int color1, int color2, int facing, bool moving)
    {
        if (!settings.UsePlayerSprite)
            return false;

        if (player == null || player.playerAnimObj == null)
        {
            Dbg("Player visual not ready; using fallback until PlayerController/playerAnimObj exists.");
            return false;
        }

        try
        {
            if (remotePlayer.visualRoot != null)
                Destroy(remotePlayer.visualRoot);

            GameObject visual = Instantiate(player.playerAnimObj);
            visual.name = "MMOnsterpatchRemotePlayerVisual";
            visual.transform.SetParent(remotePlayer.obj.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            if (!settings.UseGameDefaultVisualScale)
                visual.transform.localScale = Vector3.one * Mathf.Max(0.01f, settings.RemotePlayerScale);
            visual.SetActive(true);

            // Keep the clone purely visual. No physics, no triggers, no accidental player logic.
            foreach (Collider2D c in visual.GetComponentsInChildren<Collider2D>(true))
                c.enabled = false;

            foreach (Rigidbody2D rb in visual.GetComponentsInChildren<Rigidbody2D>(true))
            {
                rb.simulated = false;
                rb.isKinematic = true;
            }

            foreach (PlayerController pc in visual.GetComponentsInChildren<PlayerController>(true))
                pc.enabled = false;

            remotePlayer.visualRoot = visual;
            remotePlayer.spriteAnimator = visual.GetComponentInChildren<SpriteAnimator>(true);
            remotePlayer.visualAnimation = visual.GetComponent<Animation>();
            remotePlayer.usingFallback = false;
            remotePlayer.animCurrentlyMoving = false;
            remotePlayer.animLastFacing = -999;
            remotePlayer.animationRefreshTimer = 0f;
            remotePlayer.walkCycleTimer = 0f;
            remotePlayer.obj.transform.localScale = Vector3.one;

            EnsureRemoteAuxVisuals(remotePlayer);

            ApplyRemotePlayerRenderPriority(remotePlayer);
            DestroyFallbackRenderer(remotePlayer);

            Dbg("Attached cloned player visual with vanilla clone renderer ordering + follower-style world z. SpriteAnimator=" + (remotePlayer.spriteAnimator != null ? "yes" : "no"));
            return true;
        }
        catch (Exception ex)
        {
            Dbg("TryAttachPlayerVisual failed: " + ex);
            return false;
        }
    }

    private static Vector3 WithRemoteSortZ(Vector3 v)
    {
        return new Vector3(v.x, v.y, v.y * 0.01f);
    }

    private void ApplyRemotePlayerRenderPriority(RemotePlayer remotePlayer)
    {
        if (remotePlayer == null)
            return;

        SpriteRenderer[] remoteRenderers = null;

        try
        {
            if (remotePlayer.visualRoot != null)
            {
                remoteRenderers = remotePlayer.visualRoot.GetComponentsInChildren<SpriteRenderer>(true)
                    .Where(r => r != null)
                    .ToArray();
            }
            else if (remotePlayer.fallbackRenderer != null)
            {
                remoteRenderers = new SpriteRenderer[] { remotePlayer.fallbackRenderer };
            }
        }
        catch
        {
            return;
        }

        if (remoteRenderers == null || remoteRenderers.Length == 0)
            return;

        SpriteRenderer playerPrimary = GetLocalPlayerPrimaryRenderer();
        if (playerPrimary == null)
        {
            // No local player renderer yet. Leave the clone/fallback on normal world sorting
            // instead of forcing the old always-on-top 9999 layer.
            for (int i = 0; i < remoteRenderers.Length; i++)
            {
                SpriteRenderer r = remoteRenderers[i];
                if (r == null)
                    continue;
                if (r.sortingOrder >= 9000)
                    r.sortingOrder = 0;
            }
            return;
        }

        bool isClonedPlayerVisual = remotePlayer.visualRoot != null;

        if (isClonedPlayerVisual)
        {
            // v0.3: keep the cloned player visual's own SpriteRenderer layer/order values.
            // The clone already comes from player.playerAnimObj, so flattening every child
            // renderer to the same order can make edge/object clipping worse. The only
            // thing we must prevent is the old debug/overlay behavior where remote
            // renderers were forced to very high sorting orders.
            for (int i = 0; i < remoteRenderers.Length; i++)
            {
                SpriteRenderer r = remoteRenderers[i];
                if (r == null)
                    continue;
                if (r.sortingOrder >= 9000)
                    r.sortingOrder = playerPrimary.sortingOrder;
            }
            return;
        }

        // Fallback square/sprite path is not cloned from player.playerAnimObj, so give it
        // the same normal world render layer/order as the local player instead of 9999.
        int targetLayerId = playerPrimary.sortingLayerID;
        int targetOrder = playerPrimary.sortingOrder;

        for (int i = 0; i < remoteRenderers.Length; i++)
        {
            SpriteRenderer r = remoteRenderers[i];
            if (r == null)
                continue;

            r.sortingLayerID = targetLayerId;
            r.sortingOrder = targetOrder;
        }
    }

    private SpriteRenderer GetLocalPlayerPrimaryRenderer()
    {
        try
        {
            if (player == null)
                player = FindObjectOfType<PlayerController>();

            if (player == null)
                return null;

            SpriteRenderer[] playerRenderers = player.GetComponentsInChildren<SpriteRenderer>(true)
                .Where(r => r != null)
                .ToArray();

            if (playerRenderers == null || playerRenderers.Length == 0)
                return null;

            return playerRenderers
                .OrderByDescending(r => r.sortingOrder)
                .FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private void AttachFallbackRenderer(RemotePlayer remotePlayer)
    {
        if (remotePlayer.fallbackRenderer == null)
        {
            SpriteRenderer sr = remotePlayer.obj.AddComponent<SpriteRenderer>();
            sr.sprite = GetRemotePlayerSprite();
            remotePlayer.fallbackRenderer = sr;
            ApplyRemotePlayerRenderPriority(remotePlayer);
        }
        remotePlayer.usingFallback = true;
        remotePlayer.obj.transform.localScale = Vector3.one * Mathf.Max(0.01f, settings.RemotePlayerScale);
    }

    private void DestroyFallbackRenderer(RemotePlayer remotePlayer)
    {
        if (remotePlayer.fallbackRenderer != null)
        {
            Destroy(remotePlayer.fallbackRenderer);
            remotePlayer.fallbackRenderer = null;
        }
    }


    private void EnsureRemoteAuxVisuals(RemotePlayer remotePlayer)
    {
        if (remotePlayer == null || remotePlayer.obj == null || player == null)
            return;

        try
        {
            if (remotePlayer.remoteShadowObj == null && player.playerShadowObj != null)
                remotePlayer.remoteShadowObj = CloneLocalPlayerAuxVisual(player.playerShadowObj, remotePlayer, "RemotePlayerShadow");

            if (remotePlayer.remoteWaterWalkObj == null && player.waterWalkObj != null)
                remotePlayer.remoteWaterWalkObj = CloneLocalPlayerAuxVisual(player.waterWalkObj, remotePlayer, "RemoteWaterWalk");

            if (remotePlayer.remoteSpellCastObj == null && player.spellCastObj != null)
                remotePlayer.remoteSpellCastObj = CloneLocalPlayerAuxVisual(player.spellCastObj, remotePlayer, "RemoteSpellCast");

            if (remotePlayer.remoteShadowObj != null && !remotePlayer.remoteShadowObj.activeSelf)
                remotePlayer.remoteShadowObj.SetActive(false);
            if (remotePlayer.remoteWaterWalkObj != null && !remotePlayer.remoteWaterWalkObj.activeSelf)
                remotePlayer.remoteWaterWalkObj.SetActive(false);
            if (remotePlayer.remoteSpellCastObj != null)
                remotePlayer.remoteSpellCastObj.SetActive(false);
        }
        catch (Exception ex)
        {
            Dbg("EnsureRemoteAuxVisuals failed: " + ex.Message);
        }
    }

    private GameObject CloneLocalPlayerAuxVisual(GameObject source, RemotePlayer remotePlayer, string name)
    {
        if (source == null || remotePlayer == null || remotePlayer.obj == null)
            return null;

        GameObject clone = Instantiate(source);
        clone.name = "MMOnsterpatch" + name;
        clone.transform.SetParent(remotePlayer.obj.transform, false);

        try
        {
            if (player != null)
                clone.transform.localPosition = player.transform.InverseTransformPoint(source.transform.position);
            else
                clone.transform.localPosition = source.transform.localPosition;
        }
        catch { clone.transform.localPosition = source.transform.localPosition; }

        clone.transform.localRotation = source.transform.localRotation;
        clone.transform.localScale = source.transform.localScale;

        foreach (Collider2D c in clone.GetComponentsInChildren<Collider2D>(true))
            c.enabled = false;
        foreach (Rigidbody2D rb in clone.GetComponentsInChildren<Rigidbody2D>(true))
        {
            rb.simulated = false;
            rb.isKinematic = true;
        }
        foreach (PlayerController pc in clone.GetComponentsInChildren<PlayerController>(true))
            pc.enabled = false;

        clone.SetActive(false);
        return clone;
    }

    private void ApplyRemoteVisualState(RemotePlayer remotePlayer, bool ridingBroom, bool waterWalking, bool jumpingOrBouncing, string bouncingName, string reason)
    {
        if (remotePlayer == null)
            return;

        bool waterChanged = remotePlayer.waterWalking != waterWalking;
        bool jumpChanged = remotePlayer.jumpingOrBouncing != jumpingOrBouncing;
        bool broomChanged = remotePlayer.ridingBroom != ridingBroom;

        remotePlayer.ridingBroom = ridingBroom;
        remotePlayer.waterWalking = waterWalking;
        remotePlayer.jumpingOrBouncing = jumpingOrBouncing;
        remotePlayer.bouncingName = bouncingName ?? "";

        EnsureRemoteAuxVisuals(remotePlayer);

        try
        {
            bool shadowActive = ridingBroom || jumpingOrBouncing;
            if (remotePlayer.remoteShadowObj != null && remotePlayer.remoteShadowObj.activeSelf != shadowActive)
                remotePlayer.remoteShadowObj.SetActive(shadowActive);

            if (remotePlayer.remoteWaterWalkObj != null && remotePlayer.remoteWaterWalkObj.activeSelf != waterWalking)
                remotePlayer.remoteWaterWalkObj.SetActive(waterWalking);
        }
        catch { }

        if (jumpingOrBouncing && jumpChanged)
        {
            TryPlayRemotePlayerRootAnimation(remotePlayer, "playerSmallJump");
        }
        else if (waterWalking && (waterChanged || broomChanged || jumpChanged))
        {
            TryPlayRemotePlayerRootAnimation(remotePlayer, "playerBouncing");
        }
        else if (ridingBroom && (broomChanged || waterChanged || jumpChanged))
        {
            TryPlayRemotePlayerRootAnimation(remotePlayer, "playerBroom");
        }
        else if ((waterChanged || jumpChanged || broomChanged) && !ridingBroom && !waterWalking && !jumpingOrBouncing)
        {
            TryPlayRemotePlayerRootAnimation(remotePlayer, "playerIdle");
        }

        remotePlayer.lastWaterWalking = waterWalking;
        remotePlayer.lastJumpingOrBouncing = jumpingOrBouncing;
    }

    private IEnumerator ClearRemoteJumpOrBounceAfter(string id, float seconds)
    {
        yield return new WaitForSeconds(Mathf.Max(0.05f, seconds));

        RemotePlayer rp;
        if (!remotePlayers.TryGetValue(id, out rp) || rp == null)
            yield break;

        if (rp.jumpingOrBouncing)
            ApplyRemoteVisualState(rp, rp.ridingBroom, rp.waterWalking, false, "", "jump-timeout");
    }

    private void SpawnRemotePoof(Vector3 pos)
    {
        try
        {
            if (player == null || player.poofWhitePrefab == null)
                return;

            Vector3 spawn = new Vector3(pos.x, pos.y, pos.z + 0.001f);
            Instantiate(player.poofWhitePrefab, spawn, Quaternion.identity);
        }
        catch (Exception ex)
        {
            Dbg("SpawnRemotePoof failed: " + ex.Message);
        }
    }

    private void SpawnRemoteWaterRipple(Vector3 pos)
    {
        try
        {
            if (player == null || player.waterRipplePrefab == null)
                return;

            Instantiate(player.waterRipplePrefab, pos, Quaternion.identity);
        }
        catch (Exception ex)
        {
            Dbg("SpawnRemoteWaterRipple failed: " + ex.Message);
        }
    }

    private IEnumerator PlayRemoteCastFx(RemotePlayer remotePlayer, int dir, Vector3 eventPos)
    {
        if (remotePlayer == null)
            yield break;

        EnsureRemoteAuxVisuals(remotePlayer);

        try
        {
            if (remotePlayer.spriteAnimator == null && remotePlayer.visualRoot != null)
                remotePlayer.spriteAnimator = remotePlayer.visualRoot.GetComponentInChildren<SpriteAnimator>(true);

            if (remotePlayer.spriteAnimator != null)
            {
                remotePlayer.spriteAnimator.curDir = Mathf.Clamp(dir, 0, 3);
                remotePlayer.spriteAnimator.PlayCast();
            }
        }
        catch (Exception ex)
        {
            Dbg("Remote SpriteAnimator.PlayCast failed: " + ex.Message);
        }

        GameObject template = null;
        try
        {
            if (player != null && player.spellCastObj != null)
                template = player.spellCastObj;
            else if (remotePlayer.remoteSpellCastObj != null)
                template = remotePlayer.remoteSpellCastObj;
        }
        catch { }

        if (template == null)
            yield break;

        Vector3 basePos = remotePlayer.obj != null ? remotePlayer.obj.transform.position : eventPos;
        Vector3 offset = DirectionalSpellOffset(dir, 0.15f);
        Vector3 pos = basePos + offset;

        GameObject fx = null;
        try
        {
            // Decompiled PlayerController.PlayAnimation2 uses the player's official spellCastObj
            // and enables it after 0.15 seconds. Instantiate from that object instead of relying
            // only on a child clone so one-shot particles/sprites restart cleanly for remotes.
            fx = Instantiate(template, pos, Quaternion.identity);
            fx.name = "MMOnsterpatchRemoteSpellCastObj";
            fx.transform.position = pos;
            fx.SetActive(false);

            foreach (Collider2D c in fx.GetComponentsInChildren<Collider2D>(true))
                c.enabled = false;
            foreach (Rigidbody2D rb in fx.GetComponentsInChildren<Rigidbody2D>(true))
            {
                rb.simulated = false;
                rb.isKinematic = true;
            }
        }
        catch (Exception ex)
        {
            Dbg("Instantiate remote spellCastObj failed: " + ex.Message);
            fx = remotePlayer.remoteSpellCastObj;
            if (fx == null)
                yield break;

            try
            {
                fx.transform.position = pos;
                fx.SetActive(false);
            }
            catch { }
        }

        yield return new WaitForSeconds(0.15f);

        try { if (fx != null) fx.SetActive(true); } catch { }

        yield return new WaitForSeconds(0.45f);

        try { if (fx != null) Destroy(fx); } catch { }
    }

    private Vector3 DirectionalSpellOffset(int dir, float amount)
    {
        switch (Mathf.Clamp(dir, 0, 3))
        {
            case 0: return new Vector3(0f, -amount, 0f);
            case 1: return new Vector3(-amount, 0f, 0f);
            case 2: return new Vector3(0f, amount, 0f);
            case 3: return new Vector3(amount, 0f, 0f);
        }

        return Vector3.zero;
    }

    private IEnumerator PlayRemoteToolSpellFx(RemotePlayer remotePlayer, string eventType, int dir, Vector3 eventPos)
    {
        if (remotePlayer == null)
            yield break;

        // Decompiled GameScript tool methods call WandGeneric(dir), wait 0.25s, then instantiate
        // spellAxeObj/spellPickaxeObj/spellGloveObj/spellBloomObj at player position + 0.2 directional offset.
        yield return new WaitForSeconds(0.25f);

        GameScript gs = null;
        try
        {
            if (player != null && player.gameScript != null)
                gs = player.gameScript;
            if (gs == null)
                gs = FindObjectOfType<GameScript>();
        }
        catch { }

        if (gs == null)
            yield break;

        GameObject prefab = null;
        try
        {
            if (eventType.Equals("SPELL_AXE", StringComparison.OrdinalIgnoreCase))
                prefab = gs.spellAxeObj;
            else if (eventType.Equals("SPELL_PICKAXE", StringComparison.OrdinalIgnoreCase))
                prefab = gs.spellPickaxeObj;
            else if (eventType.Equals("SPELL_GLOVE", StringComparison.OrdinalIgnoreCase))
                prefab = gs.spellGloveObj;
            else if (eventType.Equals("SPELL_BLOOM", StringComparison.OrdinalIgnoreCase))
                prefab = gs.spellBloomObj;
        }
        catch { }

        if (prefab == null)
            yield break;

        Vector3 basePos = remotePlayer.obj != null ? remotePlayer.obj.transform.position : eventPos;
        Vector3 pos = basePos + DirectionalSpellOffset(dir, 0.2f);

        Quaternion rot = (Mathf.Clamp(dir, 0, 3) == 1 || Mathf.Clamp(dir, 0, 3) == 2)
            ? Quaternion.identity
            : Quaternion.Euler(0f, 180f, 0f);

        try
        {
            GameObject fx = Instantiate(prefab, pos, rot);
            fx.name = "MMOnsterpatchRemote" + eventType;

            foreach (Collider2D c in fx.GetComponentsInChildren<Collider2D>(true))
                c.enabled = false;
            foreach (Rigidbody2D rb in fx.GetComponentsInChildren<Rigidbody2D>(true))
            {
                rb.simulated = false;
                rb.isKinematic = true;
            }
        }
        catch (Exception ex)
        {
            Dbg("PlayRemoteToolSpellFx failed for " + eventType + ": " + ex.Message);
        }
    }



    private void ApplyRemotePlayerAppearance(RemotePlayer remotePlayer, int design, int color1, int color2, int facing, bool moving, bool ridingBroom)
    {
        if (remotePlayer == null) return;

        bool appearanceChanged = remotePlayer.design != design || remotePlayer.color1 != color1 || remotePlayer.color2 != color2;
        bool directionChanged = remotePlayer.facing != facing;
        bool movingChanged = remotePlayer.moving != moving;
        bool broomChanged = remotePlayer.ridingBroom != ridingBroom;

        remotePlayer.design = design;
        remotePlayer.color1 = color1;
        remotePlayer.color2 = color2;
        remotePlayer.facing = facing;
        remotePlayer.moving = moving;
        remotePlayer.ridingBroom = ridingBroom;

        if (remotePlayer.spriteAnimator == null && remotePlayer.visualRoot != null)
            remotePlayer.spriteAnimator = remotePlayer.visualRoot.GetComponentInChildren<SpriteAnimator>(true);

        if (remotePlayer.spriteAnimator != null)
        {
            try
            {
                GameScript gs = null;
                if (player != null)
                    gs = player.gameScript;
                if (gs == null)
                    gs = FindObjectOfType<GameScript>();

                if (appearanceChanged && gs != null && gs.playerSpriteScriptableObject != null && design >= 0 && design < gs.playerSpriteScriptableObject.Length)
                    remotePlayer.spriteAnimator.spriteScriptableObject = gs.playerSpriteScriptableObject[design];

                remotePlayer.spriteAnimator.primaryColor = color1;
                remotePlayer.spriteAnimator.secondaryColor = color2;
                remotePlayer.spriteAnimator.curDir = facing;
                remotePlayer.spriteAnimator.isMoving = moving;

                if (broomChanged)
                {
                    remotePlayer.animCurrentlyMoving = false;
                    remotePlayer.animLastFacing = -999;
                    remotePlayer.walkCycleTimer = 0f;
                    TryPlayRemotePlayerRootAnimation(remotePlayer, ridingBroom ? "playerBroom" : "playerIdle");
                    Dbg("ApplyRemotePlayerAppearance broom refresh for " + remotePlayer.name + " riding=" + ridingBroom);
                }

                if (appearanceChanged || directionChanged || movingChanged || broomChanged)
                    remotePlayer.animationRefreshTimer = 0f;

                // Force an immediate visible refresh on packet receipt. This fixes dismounts where
                // the remote stays on the last broom frame until another movement animation happens.
                ApplyRemotePlayerFrame(remotePlayer, moving, 0f);
            }
            catch (Exception ex)
            {
                Dbg("ApplyRemotePlayerAppearance SpriteAnimator failed: " + ex.Message);
            }
        }
    }



    private void RefreshLocalAppearanceFromRuntime()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(GameScript.playerName))
                settings.PlayerName = GameScript.playerName;

            playerDesign = GameScript.curDesign;
            playerColor1 = GameScript.curPrimaryColor;
            playerColor2 = GameScript.curSecondaryColor;
        }
        catch
        {
            // Static fields may not be ready during very early startup. Keep existing/save values.
        }
    }


    private LocalFollowerNetState GetLocalFollowerNetState()
    {
        LocalFollowerNetState st = new LocalFollowerNetState();
        try
        {
            SpriteRenderer sr = FindLocalFollowerSpriteRenderer();
            if (sr == null || sr.sprite == null || sr.gameObject == null || !sr.gameObject.activeInHierarchy)
                return st;

            st.enabled = true;
            st.pos = sr.transform.position;
            st.facing = PlayerController.facingDir;
            st.moving = PlayerController.isMoving;
            st.spriteName = BuildFollowerSpritePayload(sr);
            if (string.IsNullOrWhiteSpace(st.spriteName))
                st.spriteName = sr.sprite.name ?? "";
            st.flipX = sr.flipX;
        }
        catch { }
        return st;
    }

    private SpriteRenderer FindLocalFollowerSpriteRenderer()
    {
        try
        {
            SpriteRenderer[] renderers = Resources.FindObjectsOfTypeAll<SpriteRenderer>();
            if (renderers == null)
                return null;

            SpriteRenderer best = null;
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer sr = renderers[i];
                if (sr == null || sr.sprite == null || sr.gameObject == null)
                    continue;
                if (!sr.gameObject.activeInHierarchy)
                    continue;

                string path = GetPath(sr.gameObject);
                if (path.IndexOf("Monsterpatch_Follower", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    path.IndexOf("BattleSpriteVisual", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    best = sr;
                    if (sr.gameObject.name.IndexOf("BattleSpriteVisual", StringComparison.OrdinalIgnoreCase) >= 0)
                        return sr;
                }
            }
            return best;
        }
        catch { return null; }
    }

    private string BuildFollowerSpritePayload(SpriteRenderer sr)
    {
        try
        {
            if (sr == null || sr.sprite == null)
                return "";

            GameScript gs = GetGameScript();
            Mon lead = GetLeadFollowerMon(gs);
            if (lead == null || lead.monScriptableObject == null)
                return "";

            int monId = GetMonScriptableId(lead);
            if (monId < 0)
                return "";

            bool shiny = false;
            try { shiny = lead.isShiny; } catch { shiny = false; }

            int frameIndex = GetCurrentFollowerIdleFrameIndex(lead, sr.sprite, shiny);
            if (frameIndex < 0)
                frameIndex = 0;

            // Reuse the existing v0.1 follower_sprite text field. The v0.1 server only relays this string.
            // Remote AIO resolves this deterministically through GameScript.monScriptableObject[monId].sprites/spritesShiny.
            return "MPFOL:" + monId.ToString(CultureInfo.InvariantCulture) + ":" + (shiny ? "1" : "0") + ":" + frameIndex.ToString(CultureInfo.InvariantCulture);
        }
        catch { return ""; }
    }

    private Mon GetLeadFollowerMon(GameScript gs)
    {
        try
        {
            if (gs != null && gs.teamMon != null && gs.teamMon.Length > 0)
                return gs.teamMon[0];
        }
        catch { }
        return null;
    }

    private int GetMonScriptableId(Mon mon)
    {
        try
        {
            if (mon != null && mon.monScriptableObject != null && mon.monScriptableObject.id >= 0)
                return mon.monScriptableObject.id;
        }
        catch { }
        try
        {
            if (mon != null && mon.monID >= 0)
                return mon.monID;
        }
        catch { }
        return -1;
    }

    private int GetCurrentFollowerIdleFrameIndex(Mon mon, Sprite current, bool shiny)
    {
        try
        {
            Sprite[] idle = GetFollowerIdleFrames(mon, shiny);
            if (idle == null || idle.Length == 0 || current == null)
                return 0;

            for (int i = 0; i < idle.Length && i < 2; i++)
            {
                if (SpritesMatchForFollowerFrame(idle[i], current))
                    return i;
            }

            // If the visible follower briefly has the opposite sheet, use the frame position but keep the lead mon's shiny bool.
            Sprite[] opposite = GetFollowerIdleFrames(mon, !shiny);
            if (opposite != null)
            {
                for (int i = 0; i < opposite.Length && i < 2; i++)
                {
                    if (SpritesMatchForFollowerFrame(opposite[i], current))
                        return i;
                }
            }
        }
        catch { }
        return 0;
    }

    private Sprite[] GetFollowerIdleFrames(Mon mon, bool shiny)
    {
        try
        {
            if (mon == null || mon.monScriptableObject == null)
                return null;

            Sprite[] source = shiny ? mon.monScriptableObject.spritesShiny : mon.monScriptableObject.sprites;
            if ((source == null || source.Length == 0) && shiny)
                source = mon.monScriptableObject.sprites;
            if (source == null || source.Length == 0)
                return null;

            List<Sprite> cleaned = new List<Sprite>();
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] != null)
                    cleaned.Add(source[i]);
                if (cleaned.Count >= 2)
                    break;
            }
            return cleaned.ToArray();
        }
        catch { return null; }
    }

    private bool SpritesMatchForFollowerFrame(Sprite a, Sprite b)
    {
        if (a == null || b == null)
            return false;
        if (object.ReferenceEquals(a, b))
            return true;
        try
        {
            string an = a.name ?? "";
            string bn = b.name ?? "";
            if (!string.Equals(an, bn, StringComparison.Ordinal))
                return false;

            Texture2D at = a.texture;
            Texture2D bt = b.texture;
            string atn = at != null ? (at.name ?? "") : "";
            string btn = bt != null ? (bt.name ?? "") : "";
            if (!string.Equals(atn, btn, StringComparison.Ordinal))
                return false;

            Rect ar = a.rect;
            Rect br = b.rect;
            return Mathf.Abs(ar.x - br.x) < 0.01f &&
                   Mathf.Abs(ar.y - br.y) < 0.01f &&
                   Mathf.Abs(ar.width - br.width) < 0.01f &&
                   Mathf.Abs(ar.height - br.height) < 0.01f;
        }
        catch { return false; }
    }

    private void ApplyRemoteFollowerState(RemotePlayer remotePlayer, bool enabled, Vector3 target, int facing, bool moving, string spriteName, bool flipX)
    {
        if (remotePlayer == null)
            return;

        remotePlayer.followerActive = enabled && !string.IsNullOrWhiteSpace(spriteName);
        remotePlayer.followerTargetPos = WithRemoteSortZ(target);
        remotePlayer.followerFacing = facing;
        remotePlayer.followerMoving = moving;
        remotePlayer.followerSpriteName = spriteName ?? "";
        remotePlayer.followerFlipX = flipX;

        if (!remotePlayer.followerActive && remotePlayer.followerObj != null)
        {
            try { remotePlayer.followerObj.SetActive(false); } catch { }
        }
    }

    private void UpdateRemoteFollowerVisual(RemotePlayer remotePlayer)
    {
        if (remotePlayer == null || remotePlayer.obj == null)
            return;

        if (!remotePlayer.followerActive)
        {
            if (remotePlayer.followerObj != null)
                remotePlayer.followerObj.SetActive(false);
            return;
        }

        Sprite sprite = GetSpriteByName(remotePlayer.followerSpriteName);
        if (sprite == null)
        {
            if (remotePlayer.followerObj != null)
                remotePlayer.followerObj.SetActive(false);
            return;
        }

        if (remotePlayer.followerObj == null)
        {
            GameObject go = new GameObject("MMOnsterpatchRemoteFollower_" + SafeObjectName(remotePlayer.id));
            go.transform.SetParent(remotePlayer.obj.transform, true);
            go.transform.position = WithRemoteSortZ(remotePlayer.followerTargetPos);
            go.transform.localScale = Vector3.one;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            remotePlayer.followerObj = go;
            remotePlayer.followerRenderer = sr;
            Dbg("Created remote follower for " + remotePlayer.name + " sprite=" + remotePlayer.followerSpriteName);
        }

        remotePlayer.followerObj.SetActive(true);
        remotePlayer.followerObj.transform.position = Vector3.Lerp(remotePlayer.followerObj.transform.position, WithRemoteSortZ(remotePlayer.followerTargetPos), Mathf.Clamp01(Time.deltaTime * Mathf.Max(1f, settings.InterpolationSpeed)));

        if (remotePlayer.followerRenderer == null)
            remotePlayer.followerRenderer = remotePlayer.followerObj.GetComponent<SpriteRenderer>();
        if (remotePlayer.followerRenderer != null)
        {
            remotePlayer.followerRenderer.sprite = sprite;
            remotePlayer.followerRenderer.flipX = remotePlayer.followerFlipX;
            if (remotePlayer.followerRenderer.sortingOrder >= 9000)
                remotePlayer.followerRenderer.sortingOrder = 0;
        }
    }

    private Sprite GetSpriteByName(string spriteName)
    {
        if (string.IsNullOrWhiteSpace(spriteName))
            return null;

        if (spriteName.StartsWith("MPFOL:", StringComparison.Ordinal))
            return GetSpriteFromFollowerPayload(spriteName);

        Sprite cached;
        if (followerSpriteCache.TryGetValue(spriteName, out cached) && cached != null)
            return cached;
        try
        {
            Sprite found = Resources.FindObjectsOfTypeAll<Sprite>()
                .FirstOrDefault(s => s != null && s.name == spriteName);
            if (found != null)
            {
                followerSpriteCache[spriteName] = found;
                return found;
            }
        }
        catch { }
        return null;
    }

    private Sprite GetSpriteFromFollowerPayload(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return null;

        Sprite cached;
        if (followerSpriteCache.TryGetValue(payload, out cached) && cached != null)
            return cached;

        try
        {
            string[] parts = payload.Split(':');
            if (parts == null || parts.Length < 4 || !string.Equals(parts[0], "MPFOL", StringComparison.Ordinal))
                return null;

            int monId = ParseInt(parts[1]);
            bool shiny = parts[2] == "1" || parts[2].Equals("true", StringComparison.OrdinalIgnoreCase);
            int frameIndex = ParseInt(parts[3]);

            GameScript gs = GetGameScript();
            if (gs == null || gs.monScriptableObject == null || monId < 0 || monId >= gs.monScriptableObject.Length)
                return null;

            MonScriptableObject mso = gs.monScriptableObject[monId];
            if (mso == null)
                return null;

            Sprite[] frames = shiny ? mso.spritesShiny : mso.sprites;
            if ((frames == null || frames.Length == 0) && shiny)
                frames = mso.sprites;
            if (frames == null || frames.Length == 0)
                return null;

            List<Sprite> idle = new List<Sprite>();
            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i] != null)
                    idle.Add(frames[i]);
                if (idle.Count >= 2)
                    break;
            }
            if (idle.Count == 0)
                return null;

            if (frameIndex < 0)
                frameIndex = 0;
            if (frameIndex >= idle.Count)
                frameIndex = idle.Count - 1;

            Sprite result = idle[frameIndex];
            if (result != null)
                followerSpriteCache[payload] = result;
            return result;
        }
        catch { return null; }
    }

    private Sprite GetRemotePlayerSprite()
    {
        if (cachedRemotePlayerSprite != null) return cachedRemotePlayerSprite;

        string spriteName = settings.RemotePlayerSpriteName.Trim();
        try
        {
            cachedRemotePlayerSprite = Resources.FindObjectsOfTypeAll<Sprite>()
                .FirstOrDefault(s => s != null && s.name == spriteName);
        }
        catch { }

        if (cachedRemotePlayerSprite != null)
        {
            Dbg("Found remote player sprite: " + cachedRemotePlayerSprite.name);
            return cachedRemotePlayerSprite;
        }

        Dbg("Could not find sprite '" + spriteName + "'. Using generated fallback sprite.");

        Texture2D tex = new Texture2D(16, 16, TextureFormat.ARGB32, false);
        Color c = new Color(1f, 0.72f, 0.1f, 1f);
        Color edge = new Color(0.25f, 0.15f, 0.02f, 1f);
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
                tex.SetPixel(x, y, (x == 0 || y == 0 || x == 15 || y == 15) ? edge : c);
        }
        tex.Apply();

        cachedRemotePlayerSprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
        cachedRemotePlayerSprite.name = "MMOnsterpatchRemoteFallbackGoldBox";
        return cachedRemotePlayerSprite;
    }


    private void EnsureNameplate(RemotePlayer remotePlayer)
    {
        if (remotePlayer == null || remotePlayer.obj == null) return;

        if (!settings.ShowNameplates)
        {
            if (remotePlayer.nameplateRoot != null)
            {
                Destroy(remotePlayer.nameplateRoot);
                remotePlayer.nameplateRoot = null;
                remotePlayer.nameplateBackgroundRenderer = null;
                remotePlayer.nameplateText = null;
                remotePlayer.nameplateShadow = null;
            }
            return;
        }

        if (remotePlayer.nameplateRoot == null)
        {
            GameObject root = new GameObject("Nameplate");
            root.transform.SetParent(remotePlayer.obj.transform, false);
            root.transform.localPosition = new Vector3(0f, settings.NameplateOffsetY, 0f);
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            remotePlayer.nameplateRoot = root;

            GameObject bgObj = new GameObject("NameplateBackground");
            bgObj.transform.SetParent(root.transform, false);
            bgObj.transform.localPosition = new Vector3(0f, 0f, 0f);
            bgObj.transform.localRotation = Quaternion.identity;
            bgObj.transform.localScale = Vector3.one;
            remotePlayer.nameplateBackgroundRenderer = bgObj.AddComponent<SpriteRenderer>();
            remotePlayer.nameplateBackgroundRenderer.sprite = GetNameplateBackgroundSprite();
            remotePlayer.nameplateBackgroundRenderer.sortingOrder = 9998;

            GameObject shadowObj = new GameObject("NameplateShadow");
            shadowObj.transform.SetParent(root.transform, false);
            shadowObj.transform.localPosition = new Vector3(0.006f, -0.006f, 0f);
            shadowObj.transform.localRotation = Quaternion.identity;
            shadowObj.transform.localScale = Vector3.one;
            remotePlayer.nameplateShadow = shadowObj.AddComponent<TextMesh>();

            GameObject textObj = new GameObject("NameplateText");
            textObj.transform.SetParent(root.transform, false);
            textObj.transform.localPosition = Vector3.zero;
            textObj.transform.localRotation = Quaternion.identity;
            textObj.transform.localScale = Vector3.one;
            remotePlayer.nameplateText = textObj.AddComponent<TextMesh>();

            ConfigureNameplateText(remotePlayer.nameplateShadow, true);
            ConfigureNameplateText(remotePlayer.nameplateText, false);
        }

        RefreshNameplate(remotePlayer);
    }

    private void ConfigureNameplateText(TextMesh tm, bool shadow)
    {
        if (tm == null) return;

        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontSize = Mathf.Max(1, settings.NameplateFontSize);
        tm.characterSize = Mathf.Max(0.001f, settings.NameplateCharacterSize);
        tm.richText = false;
        tm.color = shadow
            ? ParseHexColor(settings.NameplateShadowColorHex, new Color(0f, 0f, 0f, 0.85f))
            : ParseHexColor(settings.NameplateFontColorHex, new Color(1f, 1f, 1f, 1f));

        MeshRenderer mr = tm.GetComponent<MeshRenderer>();
        if (mr != null)
            mr.sortingOrder = shadow ? 10000 : 10001;
    }

    private void RefreshNameplate(RemotePlayer remotePlayer)
    {
        if (remotePlayer == null) return;

        if (!settings.ShowNameplates)
            return;

        if (remotePlayer.nameplateRoot == null)
            return;

        remotePlayer.nameplateRoot.transform.localPosition = new Vector3(0f, settings.NameplateOffsetY, 0f);

        string displayName = string.IsNullOrWhiteSpace(remotePlayer.name) ? "Player" : remotePlayer.name;
        if (remotePlayer.nameplateText != null && remotePlayer.nameplateText.text != displayName)
            remotePlayer.nameplateText.text = displayName;
        if (remotePlayer.nameplateShadow != null && remotePlayer.nameplateShadow.text != displayName)
            remotePlayer.nameplateShadow.text = displayName;

        if (remotePlayer.nameplateText != null)
            ConfigureNameplateText(remotePlayer.nameplateText, false);
        if (remotePlayer.nameplateShadow != null)
            ConfigureNameplateText(remotePlayer.nameplateShadow, true);

        ConfigureNameplateBackground(remotePlayer, displayName);
    }

    private void ConfigureNameplateBackground(RemotePlayer remotePlayer, string displayName)
    {
        if (remotePlayer == null || remotePlayer.nameplateBackgroundRenderer == null) return;

        remotePlayer.nameplateBackgroundRenderer.enabled = settings.ShowNameplateBackground;
        if (!settings.ShowNameplateBackground)
            return;

        remotePlayer.nameplateBackgroundRenderer.color = ParseHexColor(settings.NameplateBackgroundColorHex, new Color(0f, 0f, 0f, 0.6f));
        remotePlayer.nameplateBackgroundRenderer.sortingOrder = 9998;
        remotePlayer.nameplateBackgroundRenderer.sprite = GetNameplateBackgroundSprite();

        float charSize = Mathf.Max(0.001f, settings.NameplateCharacterSize);
        float width;
        float height;

        if (settings.NameplateBackgroundAutoSize)
        {
            float textWidth = MeasureNameplateTextWidth(remotePlayer, displayName, charSize);
            float textHeight = MeasureNameplateTextHeight(remotePlayer, charSize);

            width = Mathf.Max(
                Mathf.Max(0.01f, settings.NameplateBackgroundMinWidth),
                textWidth + Mathf.Max(0f, settings.NameplateBackgroundPaddingX) * 2f
            );

            height = Mathf.Max(
                textHeight + Mathf.Max(0f, settings.NameplateBackgroundPaddingY) * 2f,
                0.02f
            );
        }
        else
        {
            width = Mathf.Max(0.01f, settings.NameplateBackgroundFixedWidth);
            height = Mathf.Max(0.01f, settings.NameplateBackgroundFixedHeight);
        }

        Sprite s = remotePlayer.nameplateBackgroundRenderer.sprite;
        Vector2 baseSize = (s != null) ? s.bounds.size : new Vector2(1f, 0.32f);
        float baseW = Mathf.Max(0.001f, baseSize.x);
        float baseH = Mathf.Max(0.001f, baseSize.y);

        remotePlayer.nameplateBackgroundRenderer.transform.localScale = new Vector3(width / baseW, height / baseH, 1f);
    }

    private float MeasureNameplateTextWidth(RemotePlayer remotePlayer, string displayName, float charSize)
    {
        if (remotePlayer != null && remotePlayer.nameplateText != null)
        {
            MeshRenderer mr = remotePlayer.nameplateText.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                float rootScale = 1f;
                if (remotePlayer.nameplateRoot != null)
                    rootScale = Mathf.Max(0.0001f, Mathf.Abs(remotePlayer.nameplateRoot.transform.lossyScale.x));

                float measured = mr.bounds.size.x / rootScale;
                if (measured > 0.0001f)
                    return measured;
            }
        }

        // Fallback only used for the first frame before Unity updates TextMesh bounds.
        return Mathf.Max(1, displayName.Length) * charSize * 0.62f;
    }

    private float MeasureNameplateTextHeight(RemotePlayer remotePlayer, float charSize)
    {
        if (remotePlayer != null && remotePlayer.nameplateText != null)
        {
            MeshRenderer mr = remotePlayer.nameplateText.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                float rootScale = 1f;
                if (remotePlayer.nameplateRoot != null)
                    rootScale = Mathf.Max(0.0001f, Mathf.Abs(remotePlayer.nameplateRoot.transform.lossyScale.y));

                float measured = mr.bounds.size.y / rootScale;
                if (measured > 0.0001f)
                    return measured;
            }
        }

        return charSize * 1.6f;
    }

    private Sprite GetNameplateBackgroundSprite()
    {
        if (cachedNameplateBackgroundSprite != null)
            return cachedNameplateBackgroundSprite;

        int w = 96;
        int h = 32;
        int r = 8;

        Texture2D tex = new Texture2D(w, h, TextureFormat.ARGB32, false);
        tex.filterMode = FilterMode.Point;

        Color clear = new Color(1f, 1f, 1f, 0f);
        Color solid = new Color(1f, 1f, 1f, 1f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool inside = true;

                if (x < r && y < r)
                    inside = DistanceSq(x, y, r, r) <= r * r;
                else if (x >= w - r && y < r)
                    inside = DistanceSq(x, y, w - r - 1, r) <= r * r;
                else if (x < r && y >= h - r)
                    inside = DistanceSq(x, y, r, h - r - 1) <= r * r;
                else if (x >= w - r && y >= h - r)
                    inside = DistanceSq(x, y, w - r - 1, h - r - 1) <= r * r;

                tex.SetPixel(x, y, inside ? solid : clear);
            }
        }

        tex.Apply();

        cachedNameplateBackgroundSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        cachedNameplateBackgroundSprite.name = "MMOnsterpatchRemotePlayerNameplateRoundedBackground";
        return cachedNameplateBackgroundSprite;
    }

    private static int DistanceSq(int x, int y, int cx, int cy)
    {
        int dx = x - cx;
        int dy = y - cy;
        return dx * dx + dy * dy;
    }

    private static Color ParseHexColor(string hex, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return fallback;

        string s = hex.Trim();
        if (s.StartsWith("#"))
            s = s.Substring(1);

        if (s.Length == 3)
        {
            s = new string(new[] { s[0], s[0], s[1], s[1], s[2], s[2] });
        }

        try
        {
            if (s.Length == 6 || s.Length == 8)
            {
                byte r = byte.Parse(s.Substring(0, 2), NumberStyles.HexNumber);
                byte g = byte.Parse(s.Substring(2, 2), NumberStyles.HexNumber);
                byte b = byte.Parse(s.Substring(4, 2), NumberStyles.HexNumber);
                byte a = 255;
                if (s.Length == 8)
                    a = byte.Parse(s.Substring(6, 2), NumberStyles.HexNumber);

                return new Color32(r, g, b, a);
            }
        }
        catch { }

        return fallback;
    }


    private void UpdateBattleRequestInput()
    {
        if (!settings.BattleRequestsEnabled || player == null)
            return;

        bool submitDown = false;
        bool cancelDown = false;

        try { submitDown = Input.GetButtonDown("Submit") || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space); } catch { }
        try { cancelDown = Input.GetButtonDown("Cancel") || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace); } catch { }

        if (battleDialogMode == 1)
        {
            if (Time.time > pendingBattleExpiresAt)
            {
                ClearPendingBattleRequest();
                return;
            }

            if (submitDown)
            {
                AcceptPendingBattleRequest();
                return;
            }

            if (cancelDown)
            {
                DeclinePendingBattleRequest();
                return;
            }

            return;
        }

        if (battleDialogMode == 2)
        {
            if (submitDown)
            {
                ConfirmOutgoingBattleRequest();
                return;
            }

            if (cancelDown)
            {
                ClearOutgoingBattleConfirm();
                return;
            }

            return;
        }

        if (battleDialogMode == 3)
        {
            // When an info window is opened from the same Submit press that activated
            // a UI button, the original input can still be "down" for this frame.
            // Ignore input briefly so Server status popups are not instantly dismissed.
            if (Time.unscaledTime < battleInfoInputBlockedUntil)
                return;

            if (submitDown || cancelDown)
            {
                ClearBattleInfoWindow();
                return;
            }

            return;
        }

        if (battleDialogMode == 4)
        {
            // Battle type selection is intentionally click-only for Normal/Ranked.
            // Esc/B remains the only controller/keyboard shortcut for this window.
            if (cancelDown)
            {
                ClearOutgoingBattleConfirm();
                return;
            }

            return;
        }

        // From here down is only for starting an overworld player interaction.
        // During PvP or battle menus, Submit belongs to the battle UI and must not
        // interact with remote overworld player objects behind/near the battle scene.
        if (IsBattleRequestInputBlockedByBattleContext())
            return;

        try
        {
            if (GTSRuntimeHost.IsOpenForAio())
                return;
        }
        catch { }

        if (!submitDown)
            return;

        if (Time.time < battleRequestCooldownUntil)
            return;

        try
        {
            if (MenuScript.gameState != MenuScript.GameState.Open)
                return;
            if (PlayerController.isMoving || GameScript.inCutscene || GameScript.interacting)
                return;
        }
        catch { }

        RemotePlayer target = FindBattleRequestTarget();
        if (target == null)
            return;

        ShowOutgoingBattleConfirm(target);
    }


    private bool IsBattleRequestInputBlockedByBattleContext()
    {
        try
        {
            if (MMOnsterpatchPvPState.Active)
                return true;
        }
        catch { }

        try
        {
            if (MenuScript.gameState == MenuScript.GameState.Battle)
                return true;
        }
        catch { }

        try
        {
            if (GameScript.battlingEnemyTrainer || GameScript.aboutToBattleEnemyTrainer)
                return true;
        }
        catch { }

        try
        {
            BattleSystem bs = FindObjectOfType<BattleSystem>();
            if (bs != null)
            {
                if (IsBattleSystemGameObjectActive(bs, "menuBattleOrRun"))
                    return true;
                if (IsBattleSystemGameObjectActive(bs, "menuMonMoves"))
                    return true;
                if (IsBattleSystemGameObjectActive(bs, "panelEnemyMonButtons"))
                    return true;
                if (IsBattleSystemGameObjectActive(bs, "panelAllyMonButtons"))
                    return true;
            }
        }
        catch { }

        return false;
    }

    private bool IsBattleSystemGameObjectActive(BattleSystem bs, string fieldName)
    {
        try
        {
            if (bs == null || string.IsNullOrEmpty(fieldName))
                return false;

            FieldInfo f = typeof(BattleSystem).GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f == null)
                return false;

            GameObject go = f.GetValue(bs) as GameObject;
            return go != null && go.activeInHierarchy;
        }
        catch { }

        return false;
    }

    private RemotePlayer FindBattleRequestTarget()
    {
        if (player == null)
            return null;

        Vector2 origin = new Vector2(player.transform.position.x, player.transform.position.y);
        Vector3 forward3 = FacingToVector(PlayerController.facingDir);
        Vector2 forward = new Vector2(forward3.x, forward3.y);
        if (forward == Vector2.zero)
            forward = Vector2.down;

        float maxDist = Mathf.Max(0.05f, settings.BattleRequestDistance);
        float minDot = Mathf.Clamp(settings.BattleRequestForwardDot, -1f, 1f);

        RemotePlayer best = null;
        float bestScore = 9999f;

        foreach (RemotePlayer g in remotePlayers.Values)
        {
            if (g == null || g.obj == null)
                continue;

            Vector2 pos = new Vector2(g.obj.transform.position.x, g.obj.transform.position.y);
            Vector2 delta = pos - origin;
            float dist = delta.magnitude;
            if (dist > maxDist || dist <= 0.0001f)
                continue;

            float dot = Vector2.Dot(delta.normalized, forward.normalized);
            if (dot < minDot)
                continue;

            if (dist < bestScore)
            {
                bestScore = dist;
                best = g;
            }
        }

        return best;
    }

    private void SendBattleRequest(RemotePlayer target)
    {
        if (target == null || string.IsNullOrEmpty(target.id))
            return;

        string teamPayload = BuildLocalTeamPayload();
        if (string.IsNullOrEmpty(teamPayload))
        {
            ShowBattleInfoWindow("Battle Request", "No team data is available for battle.");
            return;
        }

        RefreshLocalAppearanceFromRuntime();
        string scene = GetLocalSceneName();

        SendLine("BATTLE_REQ|" +
            Escape(runtimePlayerId) + "|" +
            Escape(settings.PlayerName) + "|" +
            Escape(settings.ClusterId) + "|" +
            Escape(scene) + "|" +
            Escape(target.id) + "|" +
            teamPayload);

        outgoingBattleTargetId = target.id;
        battleRequestCooldownUntil = Time.time + 1.5f;
        Dbg("Sent battle request to " + target.name + " / " + target.id);
    }

    private void HandleBattleRequestPacket(string body)
    {
        string[] p = body.Split('|');
        if (p.Length < 3)
            return;

        string incomingFromId = Unescape(p[0]);
        string incomingFromName = Unescape(p[1]);
        string incomingTeamPayload = p[2];

        if (IsLocalBusyForBattleRequest())
        {
            SendLine("BATTLE_BUSY_REPLY|" +
                Escape(incomingFromId) + "|" +
                Escape(runtimePlayerId) + "|" +
                Escape(settings.PlayerName) + "|" +
                Escape(GetLocalAvailabilityStatus()));

            Dbg("Auto-busy replied to battle request from " + incomingFromName + " / " + incomingFromId + " because local status is " + GetLocalAvailabilityStatus());
            return;
        }

        pendingBattleFromId = incomingFromId;
        pendingBattleFromName = incomingFromName;
        pendingBattleTeamPayload = incomingTeamPayload;
        pendingBattleExpiresAt = Time.time + Mathf.Max(5f, settings.BattleRequestTimeoutSeconds);
        battleDialogMode = 1;
        battleDialogTitle = "Battle Request";
        battleDialogMessage = (string.IsNullOrWhiteSpace(pendingBattleFromName) ? "A player" : pendingBattleFromName) + " wants to battle.";
        battleDialogHint = "Accept the request?";

        ShowBattlePrompt(pendingBattleFromName + " wants to battle!");
        Dbg("Received battle request from " + pendingBattleFromName + " / " + pendingBattleFromId);
    }

    private void HandleBattleAcceptPacket(string body)
    {
        string[] p = body.Split('|');
        if (p.Length < 3)
            return;

        string fromId = Unescape(p[0]);
        string fromName = Unescape(p[1]);
        string teamPayload = p[2];

        outgoingBattleTargetId = "";
        Dbg("Battle request accepted by " + fromName + " / " + fromId);

        StartCoroutine(StartRemotePlayerPvPBattle(fromId, fromName, teamPayload));
    }

    private void HandleBattleDeclinePacket(string body)
    {
        string[] p = body.Split('|');
        string fromName = p.Length > 1 ? Unescape(p[1]) : "Player";
        outgoingBattleTargetId = "";
        pendingOutgoingBattleTarget = null;
        ShowBattleInfoWindow("Battle Declined", (string.IsNullOrWhiteSpace(fromName) ? "The other player" : fromName) + " declined your battle request.");
        Dbg("Battle request declined by " + fromName);
    }

    private void HandleBattleBusyPacket(string body)
    {
        string[] p = body.Split('|');
        string busyName = p.Length > 1 ? Unescape(p[1]) : "Player";
        string busyStatus = p.Length > 2 ? Unescape(p[2]) : "busy";

        outgoingBattleTargetId = "";
        pendingOutgoingBattleTarget = null;

        ShowBattleInfoWindow("Player Busy", (string.IsNullOrWhiteSpace(busyName) ? "That player" : busyName) + " is busy.");
        Dbg("Battle request target busy: " + busyName + " status=" + busyStatus);
    }

    private void HandleBattleCommandPacket(string body)
    {
        string[] p = body.Split('|');
        if (p.Length < 6)
            return;

        string fromId = Unescape(p[0]);
        string battleId = Unescape(p[1]);
        int actorSlot = ParseInt(p[2]);
        int moveSlot = ParseInt(p[3]);
        int targetSlot = ParseInt(p[4]);
        bool targetAlly = p[5] == "1" || p[5].Equals("true", StringComparison.OrdinalIgnoreCase);

        MMOnsterpatchRealPvP.QueueRemoteCommand(fromId, battleId, actorSlot, moveSlot, targetSlot, targetAlly);
        Dbg("Queued PvP command from " + fromId + " battle=" + battleId + " actor=" + actorSlot + " move=" + moveSlot + " target=" + targetSlot + " ally=" + targetAlly);
    }

    private void HandleBattleHitPacket(string body)
    {
        string[] p = body.Split('|');
        if (p.Length < 10)
            return;

        string fromId = Unescape(p[0]);
        string battleId = Unescape(p[1]);
        int actorSlot = ParseInt(p[2]);
        int moveSlot = ParseInt(p[3]);
        string targetSide = Unescape(p[4]);
        int targetSlot = ParseInt(p[5]);
        int amount = ParseInt(p[6]);
        int hpAfter = ParseInt(p[7]);
        int shieldAfter = ParseInt(p[8]);
        bool crit = p[9] == "1" || p[9].Equals("true", StringComparison.OrdinalIgnoreCase);

        MMOnsterpatchRealPvP.QueueRemoteHit(fromId, battleId, actorSlot, moveSlot, targetSide, targetSlot, amount, hpAfter, shieldAfter, crit);
        Dbg("Queued PvP hit from " + fromId + " battle=" + battleId + " actor=" + actorSlot + " move=" + moveSlot + " target=" + targetSide + targetSlot + " amount=" + amount + " hpAfter=" + hpAfter + " crit=" + crit);
    }

    private void HandleBattleDonePacket(string body)
    {
        string[] p = body.Split('|');
        if (p.Length < 4)
            return;

        string fromId = Unescape(p[0]);
        string battleId = Unescape(p[1]);
        int actorSlot = ParseInt(p[2]);
        int moveSlot = ParseInt(p[3]);

        MMOnsterpatchRealPvP.QueueRemoteDone(fromId, battleId, actorSlot, moveSlot);
        Dbg("Queued PvP done from " + fromId + " battle=" + battleId + " actor=" + actorSlot + " move=" + moveSlot);
    }

    private void HandleBattleStatePacket(string body)
    {
        string[] p = body.Split('|');
        if (p.Length < 3)
            return;

        string fromId = Unescape(p[0]);
        string battleId = Unescape(p[1]);
        string payload = p[2];

        if (!MMOnsterpatchPvPState.Active || !MMOnsterpatchPvPState.RealBattlesEnabled)
            return;

        if (!string.IsNullOrEmpty(MMOnsterpatchPvPState.BattleId) && battleId != MMOnsterpatchPvPState.BattleId)
            return;

        ApplyRemoteBattleState(payload);
        Dbg("Applied PvP battle state from " + fromId + " battle=" + battleId);
    }


    private void AcceptPendingBattleRequest()
    {
        string fromId = pendingBattleFromId;
        string fromName = pendingBattleFromName;
        string remoteTeamPayload = pendingBattleTeamPayload;

        string localPayload = BuildLocalTeamPayload();
        if (string.IsNullOrEmpty(localPayload))
        {
            ShowBattleToast("No team data available for battle.", 2.5f);
            ClearPendingBattleRequest();
            return;
        }

        SendLine("BATTLE_ACCEPT|" +
            Escape(fromId) + "|" +
            Escape(runtimePlayerId) + "|" +
            Escape(settings.PlayerName) + "|" +
            localPayload);

        ClearPendingBattleRequest();
        Dbg("Accepted battle request from " + fromName + " / " + fromId);

        StartCoroutine(StartRemotePlayerPvPBattle(fromId, fromName, remoteTeamPayload));
    }

    private void DeclinePendingBattleRequest()
    {
        string fromId = pendingBattleFromId;
        string fromName = pendingBattleFromName;

        SendLine("BATTLE_DECLINE|" +
            Escape(fromId) + "|" +
            Escape(runtimePlayerId) + "|" +
            Escape(settings.PlayerName));

        ClearPendingBattleRequest();
        Dbg("Declined battle request from " + fromName + " / " + fromId);
    }

    private void ClearPendingBattleRequest()
    {
        pendingBattleFromId = "";
        pendingBattleFromName = "";
        pendingBattleTeamPayload = "";
        pendingBattleExpiresAt = 0f;

        if (battleDialogMode == 1)
        {
            battleDialogMode = 0;
            battleDialogTitle = "";
            battleDialogMessage = "";
            battleDialogHint = "";
        }
    }

    private void ShowOutgoingBattleConfirm(RemotePlayer target)
    {
        if (target == null)
            return;

        pendingOutgoingBattleTarget = target;
        battleDialogMode = 2;
        battleDialogTitle = "Battle Request";
        battleDialogMessage = "Send a battle request to " + (string.IsNullOrWhiteSpace(target.name) ? "this player" : target.name) + "?";
        battleDialogHint = "";
        battleOverlayMessage = "";
        battleOverlayHideAt = 0f;
        if (battleOverlayRoot != null)
            battleOverlayRoot.SetActive(false);
    }

    private void ConfirmOutgoingBattleRequest()
    {
        RemotePlayer target = pendingOutgoingBattleTarget;
        if (target == null)
        {
            ClearOutgoingBattleConfirm();
            return;
        }

        battleDialogMode = 4;
        battleDialogTitle = "Battle Request";
        battleDialogMessage = "What type of battle?";
        battleDialogHint = "";
    }

    private void ConfirmOutgoingBattleTypeNormal()
    {
        RemotePlayer target = pendingOutgoingBattleTarget;
        ClearOutgoingBattleConfirm();

        if (target != null)
            SendBattleRequest(target);
    }

    private void ConfirmOutgoingBattleTypeRanked()
    {
        ClearOutgoingBattleConfirm();
        ShowBattleInfoWindow("System Message", "Ranked battles are currently unavailable.");
    }

    private void ClearOutgoingBattleConfirm()
    {
        pendingOutgoingBattleTarget = null;

        if (battleDialogMode == 2 || battleDialogMode == 4)
        {
            battleDialogMode = 0;
            battleDialogTitle = "";
            battleDialogMessage = "";
            battleDialogHint = "";
        }
    }

    private void ShowServerStatusWindow(string message)
    {
        string normalized = message ?? string.Empty;
        string title = "System Message";
        string body = normalized;

        if (normalized.IndexOf("disconnect", StringComparison.OrdinalIgnoreCase) >= 0)
            body = "Disconnected from MMOnsterpatch.";
        else if (normalized.IndexOf("connect", StringComparison.OrdinalIgnoreCase) >= 0)
            body = "Welcome to MMOnsterpatch";
        else if (string.IsNullOrWhiteSpace(body))
            body = "System message received.";

        ShowBattleInfoWindow(title, body);
        serverStatusUseNativePopup = true;
        if (EnsureNativeServerStatusPopup())
            RefreshNativeServerStatusPopup();
    }

    private void ShowBattleInfoWindow(string title, string message)
    {
        battleDialogMode = 3;
        battleDialogTitle = string.IsNullOrWhiteSpace(title) ? "Battle" : title;
        battleDialogMessage = string.IsNullOrWhiteSpace(message) ? "Battle request was declined." : message;
        battleDialogHint = "";
        serverStatusUseNativePopup = false;
        HideNativeServerStatusPopup();
        battleInfoInputBlockedUntil = Time.unscaledTime + 0.35f;
        battleOverlayMessage = "";
        battleOverlayHideAt = 0f;
        if (battleOverlayRoot != null)
            battleOverlayRoot.SetActive(false);
    }

    private void ClearBattleInfoWindow()
    {
        if (battleDialogMode == 3)
        {
            battleDialogMode = 0;
            battleDialogTitle = "";
            battleDialogMessage = "";
            battleDialogHint = "";
            serverStatusUseNativePopup = false;
            HideNativeServerStatusPopup();
        }
    }

    private bool IsBattleDialogOpen()
    {
        if (battleDialogMode == 1)
            return !string.IsNullOrEmpty(pendingBattleFromId);
        if (battleDialogMode == 2 || battleDialogMode == 4)
            return pendingOutgoingBattleTarget != null;
        return battleDialogMode == 3;
    }


    private bool EnsureNativeServerStatusPopup()
    {
        try
        {
            if (serverStatusNativeRoot != null)
                return true;

            Transform parent = FindNativeServerStatusParent();
            if (parent == null)
                return false;

            CacheNativeServerStatusAssets();

            GameObject root = new GameObject("MMOnsterpatch_NativeServerStatusWindow", typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(parent, false);
            serverStatusNativeRoot = root;

            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.5f, 0.5f);
            rootRt.anchorMax = new Vector2(0.5f, 0.5f);
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.anchoredPosition = Vector2.zero;

            // Keep the confirmed popup scale, but use a wider/lower mockup-style panel
            // so the layout matches the supplied System Message reference art.
            rootRt.sizeDelta = new Vector2(Mathf.Max(320f, settings != null ? settings.SystemMessagePopupWidth : 560f), Mathf.Max(120f, settings != null ? settings.SystemMessagePopupHeight : 180f));
            float popupScale = settings != null ? settings.SystemMessagePopupScale : 0.32f;
            rootRt.localScale = new Vector3(Mathf.Max(0.10f, popupScale), Mathf.Max(0.10f, popupScale), 1f);

            CanvasGroup cg = root.GetComponent<CanvasGroup>();
            cg.alpha = 1f;
            cg.blocksRaycasts = true;
            cg.interactable = true;

            Color32 dark = new Color32(44, 30, 25, 255);
            Color32 paper = new Color32(239, 229, 194, 255);
            bool hasNativePanelSprite = serverStatusNativePanelSprite != null;
            bool hasNativeButtonSprite = serverStatusNativeButtonSprite != null;

            // Prefer the actual game dialogue panel sprite when available.
            // Fallback keeps the hand-made Alpha Alert double border if the native asset cannot be found.
            if (!hasNativePanelSprite)
            {
                CreateNativeServerStatusFill(root.transform, "OuterBorder", 0f, dark);
                CreateNativeServerStatusFill(root.transform, "OuterGap", 3f, paper);
                CreateNativeServerStatusFill(root.transform, "InnerBorder", 6f, dark);
            }

            GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root.transform, false);
            RectTransform panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = hasNativePanelSprite ? Vector2.zero : new Vector2(9f, 9f);
            panelRt.offsetMax = hasNativePanelSprite ? Vector2.zero : new Vector2(-9f, -9f);
            serverStatusNativePanelImage = panel.GetComponent<Image>();
            serverStatusNativePanelImage.sprite = serverStatusNativePanelSprite;
            serverStatusNativePanelImage.type = hasNativePanelSprite ? Image.Type.Sliced : Image.Type.Simple;
            serverStatusNativePanelImage.color = hasNativePanelSprite ? Color.white : (Color)paper;
            serverStatusNativePanelImage.raycastTarget = true;

            // Supplied mockup layout: centered title, centered one-line welcome text,
            // Birb safely inside the lower-right content area, and a double-framed OK button
            // overlapping the lower window border.
            serverStatusNativeTitleText = CreateNativeServerStatusText(root.transform, "Title",
                settings != null ? settings.SystemMessageTitleOffsetX : 0f,
                settings != null ? settings.SystemMessageTitleOffsetY : 55f,
                settings != null ? settings.SystemMessageTitleWidth : 500f,
                settings != null ? settings.SystemMessageTitleHeight : 44f,
                settings != null ? settings.SystemMessageTitleFontSize : 30,
                TextAlignmentOptions.Center, dark);
            serverStatusNativeTitleText.fontStyle = FontStyles.Normal;
            serverStatusNativeTitleText.enableWordWrapping = false;

            serverStatusNativeMessageText = CreateNativeServerStatusText(root.transform, "Message",
                settings != null ? settings.SystemMessageBodyOffsetX : 0f,
                settings != null ? settings.SystemMessageBodyOffsetY : -10f,
                settings != null ? settings.SystemMessageBodyWidth : 430f,
                settings != null ? settings.SystemMessageBodyHeight : 32f,
                settings != null ? settings.SystemMessageBodyFontSize : 20,
                TextAlignmentOptions.Center, dark);
            serverStatusNativeMessageText.enableWordWrapping = false;
            serverStatusNativeMessageText.fontStyle = FontStyles.Normal;

            GameObject birbGo = new GameObject("BirbIcon", typeof(RectTransform), typeof(Image));
            birbGo.transform.SetParent(root.transform, false);
            RectTransform birbRt = birbGo.GetComponent<RectTransform>();
            birbRt.anchorMin = new Vector2(1f, 0f);
            birbRt.anchorMax = new Vector2(1f, 0f);
            birbRt.pivot = new Vector2(0.5f, 0.5f);
            // 2.5x the original 48x48 target, kept inside the frame.
            float birbSize = settings != null ? settings.SystemMessageBirbSize : 120f;
            birbRt.sizeDelta = new Vector2(Mathf.Max(1f, birbSize), Mathf.Max(1f, birbSize));
            birbRt.anchoredPosition = new Vector2(settings != null ? settings.SystemMessageBirbOffsetX : -50f, settings != null ? settings.SystemMessageBirbOffsetY : 76f);
            serverStatusNativeBirbImage = birbGo.GetComponent<Image>();
            serverStatusNativeBirbImage.sprite = serverStatusNativeBirbSprite;
            serverStatusNativeBirbImage.color = serverStatusNativeBirbSprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            serverStatusNativeBirbImage.preserveAspect = true;
            serverStatusNativeBirbImage.raycastTarget = false;

            // Double-framed native-style OK button, overlapping the lower border like the mockup.
            GameObject buttonOuter = new GameObject("OkayButtonOuterBorder", typeof(RectTransform), typeof(Image));
            buttonOuter.transform.SetParent(root.transform, false);
            RectTransform buttonOuterRt = buttonOuter.GetComponent<RectTransform>();
            buttonOuterRt.anchorMin = new Vector2(0.5f, 0f);
            buttonOuterRt.anchorMax = new Vector2(0.5f, 0f);
            buttonOuterRt.pivot = new Vector2(0.5f, 0.5f);
            float okBtnX = settings != null ? settings.SystemMessageButtonOffsetX : 0f;
            float okBtnY = settings != null ? settings.SystemMessageButtonOffsetY : -4f;
            float okBtnW = Mathf.Max(32f, settings != null ? settings.SystemMessageButtonWidth : 134f);
            float okBtnH = Mathf.Max(12f, settings != null ? settings.SystemMessageButtonHeight : 24f);
            buttonOuterRt.anchoredPosition = new Vector2(okBtnX, okBtnY);
            buttonOuterRt.sizeDelta = new Vector2(okBtnW + 24f, okBtnH + 24f);
            Image buttonOuterImage = buttonOuter.GetComponent<Image>();
            buttonOuterImage.sprite = null;
            buttonOuterImage.color = dark;
            buttonOuterImage.raycastTarget = false;

            GameObject buttonGap = new GameObject("OkayButtonOuterGap", typeof(RectTransform), typeof(Image));
            buttonGap.transform.SetParent(root.transform, false);
            RectTransform buttonGapRt = buttonGap.GetComponent<RectTransform>();
            buttonGapRt.anchorMin = new Vector2(0.5f, 0f);
            buttonGapRt.anchorMax = new Vector2(0.5f, 0f);
            buttonGapRt.pivot = new Vector2(0.5f, 0.5f);
            buttonGapRt.anchoredPosition = new Vector2(okBtnX, okBtnY);
            buttonGapRt.sizeDelta = new Vector2(okBtnW + 16f, okBtnH + 16f);
            Image buttonGapImage = buttonGap.GetComponent<Image>();
            buttonGapImage.sprite = null;
            buttonGapImage.color = paper;
            buttonGapImage.raycastTarget = false;

            GameObject buttonInner = new GameObject("OkayButtonInnerBorder", typeof(RectTransform), typeof(Image));
            buttonInner.transform.SetParent(root.transform, false);
            RectTransform buttonInnerRt = buttonInner.GetComponent<RectTransform>();
            buttonInnerRt.anchorMin = new Vector2(0.5f, 0f);
            buttonInnerRt.anchorMax = new Vector2(0.5f, 0f);
            buttonInnerRt.pivot = new Vector2(0.5f, 0.5f);
            buttonInnerRt.anchoredPosition = new Vector2(okBtnX, okBtnY);
            buttonInnerRt.sizeDelta = new Vector2(okBtnW + 8f, okBtnH + 8f);
            Image buttonInnerImage = buttonInner.GetComponent<Image>();
            buttonInnerImage.sprite = null;
            buttonInnerImage.color = dark;
            buttonInnerImage.raycastTarget = false;

            GameObject buttonGo = new GameObject("OkayButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(root.transform, false);
            RectTransform buttonRt = buttonGo.GetComponent<RectTransform>();
            buttonRt.anchorMin = new Vector2(0.5f, 0f);
            buttonRt.anchorMax = new Vector2(0.5f, 0f);
            buttonRt.pivot = new Vector2(0.5f, 0.5f);
            buttonRt.anchoredPosition = new Vector2(okBtnX, okBtnY);
            buttonRt.sizeDelta = new Vector2(okBtnW, okBtnH);
            serverStatusNativeButtonImage = buttonGo.GetComponent<Image>();
            serverStatusNativeButtonImage.sprite = serverStatusNativeButtonSprite;
            serverStatusNativeButtonImage.type = hasNativeButtonSprite ? Image.Type.Sliced : Image.Type.Simple;
            serverStatusNativeButtonImage.color = hasNativeButtonSprite ? Color.white : (Color)paper;
            serverStatusNativeButtonImage.raycastTarget = true;
            serverStatusNativeButton = buttonGo.GetComponent<Button>();
            serverStatusNativeButton.targetGraphic = serverStatusNativeButtonImage;
            serverStatusNativeButton.onClick.AddListener(delegate { ClearBattleInfoWindow(); });

            serverStatusNativeButtonText = CreateNativeServerStatusText(buttonGo.transform, "Text", 0f, 0f, okBtnW - 9f, okBtnH, settings != null ? settings.SystemMessageBodyFontSize : 20, TextAlignmentOptions.Center, dark);
            serverStatusNativeButtonText.fontStyle = FontStyles.Normal;
            serverStatusNativeButtonText.text = "OK";

            root.SetActive(false);
            return true;
        }
        catch (Exception ex)
        {
            Dbg("Native server status popup setup failed: " + ex.Message);
            return false;
        }
    }

    private void RefreshNativeServerStatusPopup()
    {
        if (serverStatusNativeRoot == null)
            return;

        try
        {
            serverStatusNativeRoot.SetActive(battleDialogMode == 3 && serverStatusUseNativePopup);
            if (!serverStatusNativeRoot.activeSelf)
                return;

            if (serverStatusNativeTitleText != null)
                serverStatusNativeTitleText.text = string.IsNullOrWhiteSpace(battleDialogTitle) ? "System Message" : battleDialogTitle;
            if (serverStatusNativeMessageText != null)
                serverStatusNativeMessageText.text = string.IsNullOrWhiteSpace(battleDialogMessage) ? "System message received." : battleDialogMessage;
            if (serverStatusNativeButtonText != null)
                serverStatusNativeButtonText.text = "OK";
            if (serverStatusNativeBirbImage != null && serverStatusNativeBirbSprite != null && serverStatusNativeBirbImage.sprite == null)
            {
                serverStatusNativeBirbImage.sprite = serverStatusNativeBirbSprite;
                serverStatusNativeBirbImage.color = Color.white;
            }

            // Keep the popup last in the UI hierarchy so it appears above the chat/trading windows.
            serverStatusNativeRoot.transform.SetAsLastSibling();
        }
        catch { }
    }

    private void HideNativeServerStatusPopup()
    {
        try
        {
            if (serverStatusNativeRoot != null)
                serverStatusNativeRoot.SetActive(false);
        }
        catch { }
    }

    private Transform FindNativeServerStatusParent()
    {
        try
        {
            GameScript gs = GetGameScript();
            if (gs != null)
            {
                DialogueManager dm = gs.dialogueManager;
                if (dm != null && dm.menuDialogue != null && dm.menuDialogue.transform.parent != null)
                    return dm.menuDialogue.transform.parent;

                if (gs.BattleCanvasObj != null)
                    return gs.BattleCanvasObj.transform;
            }

            Canvas c = FindObjectOfType<Canvas>();
            if (c != null)
                return c.transform;
        }
        catch { }

        return null;
    }

    private void CacheNativeServerStatusAssets()
    {
        try
        {
            GameScript gs = GetGameScript();
            DialogueManager dm = (gs != null) ? gs.dialogueManager : null;

            if (dm != null)
            {
                if (dm.txtDialogue != null && dm.txtDialogue.font != null)
                    serverStatusNativeFont = dm.txtDialogue.font;

                Image dialogueImage = null;
                if (dm.menuDialogue != null)
                {
                    dialogueImage = dm.menuDialogue.GetComponent<Image>();
                    if (dialogueImage == null)
                        dialogueImage = dm.menuDialogue.GetComponentInChildren<Image>(true);
                }
                if (dialogueImage != null && dialogueImage.sprite != null)
                    serverStatusNativePanelSprite = dialogueImage.sprite;

                if (dm.bChoice != null && dm.bChoice.Length > 0 && dm.bChoice[0] != null)
                {
                    Image choiceImage = dm.bChoice[0].GetComponent<Image>();
                    if (choiceImage == null)
                        choiceImage = dm.bChoice[0].GetComponentInChildren<Image>(true);
                    if (choiceImage != null && choiceImage.sprite != null)
                        serverStatusNativeButtonSprite = choiceImage.sprite;
                }
            }

            if (serverStatusNativeFont == null && gs != null && gs.boxManager != null && gs.boxManager.txtBox != null)
                serverStatusNativeFont = gs.boxManager.txtBox.font;

            if (serverStatusNativeBirbSprite == null)
                serverStatusNativeBirbSprite = FindNativeServerStatusBirbSprite();
        }
        catch { }
    }

    private Sprite FindNativeServerStatusBirbSprite()
    {
        try
        {
            string[] preferredNames = new string[]
            {
                "0_Birb",
                "0_BIRB",
                "0_Birb(Original)",
                "Birb",
                "birb",
                "birb_icon",
                "BirbIcon",
                "_monsters_0",
                "monsters_0"
            };

            Sprite[] sprites = Resources.FindObjectsOfTypeAll<Sprite>();
            if (sprites == null || sprites.Length == 0)
                return null;

            for (int p = 0; p < preferredNames.Length; p++)
            {
                string wanted = preferredNames[p];
                for (int i = 0; i < sprites.Length; i++)
                {
                    Sprite sp = sprites[i];
                    if (sp != null && string.Equals(sp.name, wanted, StringComparison.OrdinalIgnoreCase))
                        return sp;
                }
            }

            for (int i = 0; i < sprites.Length; i++)
            {
                Sprite sp = sprites[i];
                if (sp != null && sp.name != null && sp.name.IndexOf("Birb", StringComparison.OrdinalIgnoreCase) >= 0)
                    return sp;
            }
        }
        catch { }

        return null;
    }

    private TextMeshProUGUI CreateNativeServerStatusText(Transform parent, string name, float x, float y, float w, float h, int fontSize, TextAlignmentOptions align, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (serverStatusNativeFont != null)
            tmp.font = serverStatusNativeFont;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.color = color;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Image CreateNativeServerStatusFill(Transform parent, string name, float inset, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);

        Image img = go.GetComponent<Image>();
        img.sprite = null;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static void ApplyNativePanelImage(Image img, Sprite sprite, Color fallbackColor)
    {
        if (img == null)
            return;

        img.sprite = sprite;
        img.color = Color.white;
        img.raycastTarget = true;
        if (sprite != null)
        {
            Vector4 border = sprite.border;
            img.type = (border.x > 0f || border.y > 0f || border.z > 0f || border.w > 0f) ? Image.Type.Sliced : Image.Type.Simple;
            img.preserveAspect = false;
        }
        else
        {
            img.color = fallbackColor;
        }
    }

    private string BuildLocalTeamPayload()
    {
        try
        {
            GameScript gs = GetGameScript();
            if (gs == null || gs.teamMon == null)
                return "";

            List<string> parts = new List<string>();
            for (int i = 0; i < 4; i++)
            {
                Mon m = (i < gs.teamMon.Length) ? gs.teamMon[i] : null;
                if (m == null)
                {
                    parts.Add("_");
                }
                else
                {
                    string saveString = gs.ConstructMonSaveStringFromMon(m, false);
                    parts.Add(Convert.ToBase64String(Encoding.UTF8.GetBytes(saveString ?? "")));
                }
            }

            return string.Join("~", parts.ToArray());
        }
        catch (Exception ex)
        {
            Dbg("BuildLocalTeamPayload failed: " + ex.Message);
            return "";
        }
    }

    private Mon[] DecodeTeamPayload(GameScript gs, string payload)
    {
        Mon[] mons = new Mon[4];
        if (gs == null || string.IsNullOrWhiteSpace(payload))
            return mons;

        string[] parts = payload.Split('~');
        for (int i = 0; i < 4 && i < parts.Length; i++)
        {
            if (parts[i] == "_" || string.IsNullOrWhiteSpace(parts[i]))
            {
                mons[i] = null;
                continue;
            }

            try
            {
                string saveString = Encoding.UTF8.GetString(Convert.FromBase64String(parts[i]));
                mons[i] = gs.GetMonFromSaveString(saveString);
                if (mons[i] != null)
                {
                    mons[i].ClearBattleStatuses();
                    mons[i].ResetAllCooldowns();
                }
            }
            catch (Exception ex)
            {
                Dbg("DecodeTeamPayload slot " + i + " failed: " + ex.Message);
                mons[i] = null;
            }
        }

        return mons;
    }

    private IEnumerator StartRemotePlayerPvPBattle(string opponentId, string opponentName, string remoteTeamPayload)
    {
        if (MMOnsterpatchPvPState.Active)
        {
            ShowBattleToast("Already in a PvP battle.", 2f);
            yield break;
        }

        GameScript gs = GetGameScript();
        if (gs == null)
        {
            ShowBattleToast("Could not find GameScript for battle.", 3f);
            yield break;
        }

        BattleSystem bs = GetFieldValue<BattleSystem>(gs, "battleSystem");
        if (bs == null)
            bs = FindObjectOfType<BattleSystem>();

        if (bs == null)
        {
            ShowBattleToast("Could not find BattleSystem.", 3f);
            yield break;
        }

        Mon[] remoteTeam = DecodeTeamPayload(gs, remoteTeamPayload);
        bool any = false;
        for (int i = 0; i < remoteTeam.Length; i++)
        {
            if (remoteTeam[i] != null)
            {
                any = true;
                break;
            }
        }

        if (!any)
        {
            ShowBattleToast("Opponent has no valid team data.", 3f);
            yield break;
        }

        Mon[] enemyMon = GetFieldValue<Mon[]>(bs, "enemyMon");
        if (enemyMon == null || enemyMon.Length < 4)
        {
            enemyMon = new Mon[4];
            SetFieldValue(bs, "enemyMon", enemyMon);
        }

        for (int i = 0; i < 4; i++)
            enemyMon[i] = remoteTeam[i];

        MMOnsterpatchPvPState.Begin(gs, opponentId, opponentName, MakeBattleId(runtimePlayerId, opponentId), settings.DisableRunInPvP, settings.RealBattlesEnabled, settings.BattleCommandTimeoutSeconds, settings.BattleHitTimeoutSeconds);
        MMOnsterpatchPvPState.OpponentBattleSprite = settings.UseOpponentSpriteInBattleSplash ? GetRemotePlayerBattleSprite(opponentId) : null;

        try
        {
            GameScript.canMove = false;
            GameScript.interacting = false;
            GameScript.battlingEnemyTrainer = false;
            GameScript.aboutToBattleEnemyTrainer = false;
            SetFieldValue(gs, "lastEncounterWasTrainer", true);
            gs.ClearLastEncounterMons();
            gs.spawnedEncounterCrystal = false;
            if (gs.encounterCrystalObj != null)
                gs.encounterCrystalObj.SetActive(false);
        }
        catch { }

        Dbg("Starting MMOnsterpatch PvP battle vs " + opponentName);

        StartCoroutine(BattleSplashFlow(gs, opponentName));
        yield break;
    }


    private IEnumerator BattleSplashFlow(GameScript gs, string opponentName)
    {
        TryPlayMusic(gs, "20 Battle! (Wizard)");
        TryPlayCameraAnimation("zoomInAndOutFast");

        GameObject encounterObj = GetFieldValue<GameObject>(gs, "encounterObj");
        if (encounterObj != null)
        {
            encounterObj.SetActive(false);
            encounterObj.SetActive(true);
        }

        yield return new WaitForSeconds(0.6f);

        if (Camera.main != null)
            Camera.main.transform.position = new Vector3(0f, -50f, -10f);

        GameObject menuTop = GetFieldValue<GameObject>(gs, "menuTop");
        if (menuTop != null)
            menuTop.SetActive(false);

        SetupFakeTrainerForBattleSplash(gs, opponentName);
        object dialogueManager = GetFieldObject(gs, "dialogueManager");
        bool splashCalled = TryInvoke(dialogueManager, "BattleSplash", new object[0]);

        TryApplyOpponentBattleSpriteToBattleSplash(gs);

        yield return new WaitForSeconds(splashCalled ? 0.8f : 0.2f);

        TryApplyOpponentBattleSpriteToBattleSplash(gs);

        bool started = TryInvoke(gs, "StartEnemyTrainerEncounter2", new object[0]);
        if (!started)
        {
            try { MenuScript.gameState = MenuScript.GameState.Battle; } catch { }
            SetFieldValue(gs, "disableMenu", false);

            if (encounterObj != null)
            {
                Animation encAnim = encounterObj.GetComponent<Animation>();
                if (encAnim != null)
                    encAnim.Play("encounter2");
            }

            TryInvoke(gs, "SetTimescaleBasedOnBattleSpeed", new object[0]);

            BattleSystem bs = GetFieldValue<BattleSystem>(gs, "battleSystem");
            if (bs == null)
                bs = FindObjectOfType<BattleSystem>();

            try
            {
                if (bs != null)
                    bs.InitBattle();
            }
            catch (Exception ex)
            {
                MMOnsterpatchPvPState.EndAndRestoreExp();
                Dbg("BattleSystem.InitBattle failed: " + ex);
                ShowBattleToast("PvP battle failed to start. See MMOnsterpatch log.", 4f);
            }
        }
    }

    private void SetupFakeTrainerForBattleSplash(GameScript gs, string opponentName)
    {
        try
        {
            EnemyTrainerScriptableObject fake = ScriptableObject.CreateInstance<EnemyTrainerScriptableObject>();
            fake.trainerId = 9999;
            fake.trainerName = string.IsNullOrWhiteSpace(opponentName) ? "Player" : opponentName;
            fake.battleSprite = MMOnsterpatchPvPState.OpponentBattleSprite;
            fake.monTeam = new TeamMon[0];
            SetFieldValue(gs, "curEnemyTrainer", fake);
        }
        catch (Exception ex)
        {
            Dbg("SetupFakeTrainerForBattleSplash failed: " + ex.Message);
        }
    }

    private Sprite GetRemotePlayerBattleSprite(string opponentId)
    {
        try
        {
            RemotePlayer rp;
            if (string.IsNullOrEmpty(opponentId) || !remotePlayers.TryGetValue(opponentId, out rp) || rp == null || rp.obj == null)
                return null;

            Transform root = rp.visualRoot != null ? rp.visualRoot.transform : rp.obj.transform;
            SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);

            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer sr = renderers[i];
                if (sr == null || sr.sprite == null)
                    continue;

                string path = GetTransformPath(sr.transform).ToLowerInvariant();
                if (path.Contains("nameplate") || path.Contains("background"))
                    continue;

                return sr.sprite;
            }

            if (rp.fallbackRenderer != null)
                return rp.fallbackRenderer.sprite;
        }
        catch (Exception ex)
        {
            Dbg("GetRemotePlayerBattleSprite failed: " + ex.Message);
        }

        return null;
    }

    private static string GetTransformPath(Transform t)
    {
        if (t == null)
            return "";

        List<string> parts = new List<string>();
        while (t != null)
        {
            parts.Add(t.name ?? "");
            t = t.parent;
        }

        parts.Reverse();
        return string.Join("/", parts.ToArray());
    }

    private void TryApplyOpponentBattleSpriteToBattleSplash(GameScript gs)
    {
        Sprite sprite = MMOnsterpatchPvPState.OpponentBattleSprite;
        if (gs == null || sprite == null)
            return;

        try
        {
            object trainer = GetFieldObject(gs, "curEnemyTrainer");
            if (trainer != null)
            {
                FieldInfo f = trainer.GetType().GetField("battleSprite", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (f != null)
                    f.SetValue(trainer, sprite);
            }
        }
        catch { }

        GameObject splashObj = GetFieldValue<GameObject>(gs, "battleSplashObj");
        GameObject encounterObj = GetFieldValue<GameObject>(gs, "encounterObj");

        int changed = 0;
        changed += TryApplySpriteToCandidateImages(splashObj, sprite);
        changed += TryApplySpriteToCandidateImages(encounterObj, sprite);

        if (changed > 0)
            Dbg("Applied opponent player sprite to battle splash targets: " + changed);
    }

    private int TryApplySpriteToCandidateImages(GameObject root, Sprite sprite)
    {
        if (root == null || sprite == null)
            return 0;

        int changed = 0;

        try
        {
            SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer sr = renderers[i];
                if (sr == null || sr.sprite == null)
                    continue;

                string n = GetTransformPath(sr.transform).ToLowerInvariant();
                if (LooksLikeBattleTrainerImage(n))
                {
                    sr.sprite = sprite;
                    changed++;
                }
            }
        }
        catch { }

        try
        {
            Component[] components = root.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < components.Length; i++)
            {
                Component c = components[i];
                if (c == null)
                    continue;

                Type t = c.GetType();
                if (t.FullName != "UnityEngine.UI.Image")
                    continue;

                string n = GetTransformPath(c.transform).ToLowerInvariant();
                if (!LooksLikeBattleTrainerImage(n))
                    continue;

                PropertyInfo p = t.GetProperty("sprite", BindingFlags.Instance | BindingFlags.Public);
                if (p != null && p.CanWrite)
                {
                    p.SetValue(c, sprite, null);
                    changed++;
                }
            }
        }
        catch { }

        return changed;
    }

    private static bool LooksLikeBattleTrainerImage(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        if (name.Contains("trainer") || name.Contains("wizard") || name.Contains("rival") || name.Contains("bestfriend") || name.Contains("best_friend") || name.Contains("enemy") || name.Contains("portrait") || name.Contains("sprite"))
            return true;

        if (name.Contains("battle") && name.Contains("splash"))
            return true;

        return false;
    }

    private GameScript GetGameScript()
    {
        try
        {
            if (player != null && player.gameScript != null)
                return player.gameScript;
        }
        catch { }

        try { return FindObjectOfType<GameScript>(); } catch { }
        return null;
    }


    private bool IsLocalBusyForBattleRequest()
    {
        return GetLocalAvailabilityStatus() != "available";
    }

    private string GetLocalAvailabilityStatus()
    {
        try
        {
            if (MMOnsterpatchPvPState.Active)
                return "pvp_battle";

            if (battleDialogMode == 1 || battleDialogMode == 2)
                return "request_pending";

            if (battleDialogMode == 3)
                return "menu";

            try
            {
                if (GTSRuntimeHost.IsOpenForAio())
                    return "trading_post";
            }
            catch { }

            try
            {
                if (MenuScript.gameState == MenuScript.GameState.Battle)
                    return "battle";

                if (MenuScript.gameState != MenuScript.GameState.Open)
                    return "menu";
            }
            catch { }

            try
            {
                if (GameScript.inCutscene)
                    return "cutscene";
            }
            catch { }

            try
            {
                if (GameScript.interacting)
                    return "interacting";
            }
            catch { }
        }
        catch { }

        return "available";
    }

    private string GetLocalSceneName()
    {
        // For MMO/world-spawn routing, use the game's real map/location name first.
        // Unity scene name can stay the same while GameScript.curLocation changes, which
        // caused personal encounter reward spawns to stay owner-locked after a map change.
        try
        {
            GameScript gs = GetGameScript();
            if (gs != null && !string.IsNullOrWhiteSpace(gs.curLocation))
                return gs.curLocation.Trim();
        }
        catch { }

        if (player == null || player.gameObject == null)
            return "unknown";

        try { return player.gameObject.scene.name ?? "unknown"; }
        catch { return "unknown"; }
    }

    private void ShowBattlePrompt(string message)
    {
        // Incoming requests now use the centered Monsterpatch-style Accept / Decline window.
        battleOverlayMessage = "";
        battleOverlayHideAt = 0f;
        if (battleOverlayRoot != null)
            battleOverlayRoot.SetActive(false);
    }

    private void ShowBattleToast(string message, float seconds)
    {
        battleOverlayMessage = message;
        battleOverlayHideAt = Time.time + Mathf.Max(0.5f, seconds);
        EnsureBattleOverlay();
    }

    private void UpdateBattleOverlay()
    {
        if (string.IsNullOrEmpty(battleOverlayMessage) || Time.time > battleOverlayHideAt)
        {
            if (battleOverlayRoot != null)
                battleOverlayRoot.SetActive(false);
            return;
        }

        EnsureBattleOverlay();

        if (battleOverlayRoot == null)
            return;

        battleOverlayRoot.SetActive(true);

        Camera cam = Camera.main;
        if (cam != null)
        {
            battleOverlayRoot.transform.SetParent(cam.transform, false);
            float y = cam.orthographic ? -cam.orthographicSize * 0.72f : -2.25f;
            battleOverlayRoot.transform.localPosition = new Vector3(0f, y, 10f);
            battleOverlayRoot.transform.localRotation = Quaternion.identity;
            battleOverlayRoot.transform.localScale = Vector3.one;
        }
        else if (player != null)
        {
            battleOverlayRoot.transform.SetParent(null, true);
            Vector3 pos = player.transform.position + new Vector3(0f, 0.8f, 0f);
            pos.z = pos.y * 0.01f;
            battleOverlayRoot.transform.position = pos;
        }

        if (battleOverlayText != null)
            battleOverlayText.text = battleOverlayMessage;
        if (battleOverlayShadow != null)
            battleOverlayShadow.text = battleOverlayMessage;
    }

    private void EnsureBattleOverlay()
    {
        if (battleOverlayRoot != null)
            return;

        battleOverlayRoot = new GameObject("MMOnsterpatch_BattleOverlay");
        DontDestroyOnLoad(battleOverlayRoot);

        GameObject shadow = new GameObject("BattleOverlayShadow");
        shadow.transform.SetParent(battleOverlayRoot.transform, false);
        shadow.transform.localPosition = new Vector3(0.015f, -0.015f, 0f);
        battleOverlayShadow = shadow.AddComponent<TextMesh>();

        GameObject text = new GameObject("BattleOverlayText");
        text.transform.SetParent(battleOverlayRoot.transform, false);
        text.transform.localPosition = Vector3.zero;
        battleOverlayText = text.AddComponent<TextMesh>();

        ConfigureBattleOverlayText(battleOverlayShadow, true);
        ConfigureBattleOverlayText(battleOverlayText, false);
    }

    private void ConfigureBattleOverlayText(TextMesh tm, bool shadow)
    {
        if (tm == null) return;

        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontSize = 36;
        tm.characterSize = Mathf.Max(0.01f, settings.BattleOverlayCharacterSize);
        tm.richText = false;
        tm.color = shadow ? new Color(0f, 0f, 0f, 0.9f) : new Color(1f, 1f, 1f, 1f);

        MeshRenderer mr = tm.GetComponent<MeshRenderer>();
        if (mr != null)
            mr.sortingOrder = shadow ? 20000 : 20001;
    }

    private void TryPlayMusic(GameScript gs, string musicName)
    {
        try
        {
            object audio = GetFieldObject(gs, "audioSystemMusic");
            if (audio == null) return;
            TryInvoke(audio, "PlayMusic", new object[] { musicName });
        }
        catch { }
    }

    private void TryPlayCameraAnimation(string animName)
    {
        try
        {
            if (Camera.main == null) return;
            Animation anim = Camera.main.gameObject.GetComponent<Animation>();
            if (anim != null)
                anim.Play(animName);
        }
        catch { }
    }

    private static object GetFieldObject(object obj, string name)
    {
        if (obj == null) return null;
        FieldInfo f = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        if (f == null) return null;
        return f.GetValue(obj);
    }

    private static T GetFieldValue<T>(object obj, string name) where T : class
    {
        return GetFieldObject(obj, name) as T;
    }

    private static void SetFieldValue(object obj, string name, object value)
    {
        if (obj == null) return;
        FieldInfo f = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        if (f != null)
            f.SetValue(obj, value);
    }

    private static bool TryInvoke(object obj, string name, object[] args)
    {
        if (obj == null) return false;
        Type t = obj.GetType();
        MethodInfo[] methods = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        for (int i = 0; i < methods.Length; i++)
        {
            MethodInfo m = methods[i];
            if (m.Name != name)
                continue;

            ParameterInfo[] ps = m.GetParameters();
            int argCount = args == null ? 0 : args.Length;
            if (ps.Length != argCount)
                continue;

            try
            {
                m.Invoke(obj, args);
                return true;
            }
            catch { }
        }
        return false;
    }

    private void TryReadSaveAppearance()
    {
        if (!settings.ReadAppearanceFromSave) return;
        if (string.IsNullOrWhiteSpace(settings.SaveFilePath)) return;
        if (!File.Exists(settings.SaveFilePath))
        {
            Dbg("Configured save file not found: " + settings.SaveFilePath);
            return;
        }

        try
        {
            string json = File.ReadAllText(settings.SaveFilePath);

            string pn = MatchString(json, "playerName");
            if (!string.IsNullOrWhiteSpace(pn))
                settings.PlayerName = pn;

            playerDesign = MatchInt(json, "playerDesign", playerDesign);
            playerColor1 = MatchInt(json, "playerColor1", playerColor1);
            playerColor2 = MatchInt(json, "playerColor2", playerColor2);

            Dbg("Loaded save appearance: name=" + settings.PlayerName + ", design=" + playerDesign + ", colors=" + playerColor1 + "," + playerColor2);
        }
        catch (Exception ex)
        {
            Dbg("Could not read save appearance: " + ex.Message);
        }
    }

    private void Dbg(string msg)
    {
        try { if (log != null) log.LogInfo("[MMOnsterpatch] " + msg); } catch { }
        try { File.AppendAllText(debugLogPath, DateTime.Now.ToString("HH:mm:ss.fff") + " " + msg + "\n"); } catch { }
    }

    private static string MatchString(string json, string key)
    {
        var m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"([^\"]*)\"");
        return m.Success ? m.Groups[1].Value : "";
    }

    private static int MatchInt(string json, string key, int fallback)
    {
        var m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*(-?\\d+)");
        int v;
        return m.Success && int.TryParse(m.Groups[1].Value, out v) ? v : fallback;
    }

    private static string Escape(string s)
    {
        return Uri.EscapeDataString(s ?? "");
    }

    private static string Unescape(string s)
    {
        return Uri.UnescapeDataString(s ?? "");
    }

    private static string F(float v)
    {
        return v.ToString("0.####", CultureInfo.InvariantCulture);
    }

    private static float ParseFloat(string s)
    {
        float v;
        float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v);
        return v;
    }

    private static int ParseInt(string s)
    {
        int v;
        int.TryParse(s, out v);
        return v;
    }

    private static string SafeObjectName(string s)
    {
        if (string.IsNullOrEmpty(s)) return "unknown";
        foreach (char c in Path.GetInvalidFileNameChars())
            s = s.Replace(c, '_');
        return s.Replace(':', '_').Replace('|', '_').Replace('/', '_').Replace('\\', '_');
    }

    private static string GetPath(GameObject go)
    {
        if (go == null) return "<null>";
        string path = go.name;
        Transform t = go.transform;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}


