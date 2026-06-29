using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using BepInEx;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Goose.Monsterpatch.GTSAllInOnePatcher
{
    public class GTSRuntimeHost : MonoBehaviour
    {
        public const string Name = "Monsterpatch GTS All-In-One Patcher Runtime";
        public const string Version = "0.1.65-aio-stat-order-plus-minus";
        private const string ConfigFileName = "goose.monsterpatch.gts.client.cfg";

        internal static GTSRuntimeHost Instance;
        internal static bool AioIntegratedMode = true;
        internal static string ServerHost = "mon.gamingwithgoose.com";
        internal static int ServerPort = 61526;
        internal static bool AutoOpenSteamLogin = true;
        internal static bool DebugLogging = true;
        internal static float WindowScale = 1.0f;
        internal static bool ShowBoxOverlayButton = false;
        internal static float BoxOverlayX = 1180f;
        internal static float BoxOverlayY = 78f;
        internal static bool ShowNativeBoxButton = false;
        internal static float NativeButtonX = 12f;
        internal static float NativeButtonY = -40f;
        // Rich listing test UI. This is intentionally client-side for the first test:
        // it reads the existing listing blob already sent by the current server and
        // renders safe display info. The proper release can move this metadata to
        // server-side fields once the layout is tuned.
        internal static bool ShowRichListingInfo = true;
        internal static bool RichListingUseMonIcon = true;
        internal static float RichListingIconSize = 58f;
        internal static float RichListingIconOffsetX = 0f;
        internal static float RichListingIconOffsetY = 0f;
        internal static bool RichListingShowRequestedMonIcon = true;
        internal static float RichListingRequestedIconSize = 58f;
        internal static float RichListingRequestedIconOffsetX = -15f;
        internal static float RichListingRequestedIconOffsetY = 0f;
        internal static float RichListingRequestedIconColumnWidth = 66f;
        internal static bool RichListingStatsSingleLine = true;
        internal static string[] RichListingVibeIndexNames = new string[20];
        internal static string RichListingVibePlusColorHex = "A2BA9C";
        internal static string RichListingVibeMinusColorHex = "85ACBD";
        internal static Color RichListingVibePlusColor = new Color32(162, 186, 156, 255);
        internal static Color RichListingVibeMinusColor = new Color32(133, 172, 189, 255);
        internal static float RichListingCardSpacing = 6f;
        internal static float RichListingCardInnerPadding = 4f;
        // Listing card width controls. CardWidth=0 means auto mode.
        internal static float RichListingCardWidth = 0f;
        internal static float RichListingCardAutoBaseWidth = 500f;
        internal static bool RichListingCardAutoFollowRequestedIconOffsetX = true;
        internal static float RichListingCardAutoExtraWidth = 0f;
        internal static float RichListingCardOffsetX = -20f;
        internal static string RichListingCardAlign = "Left";
        internal static float RichListingHeaderOffsetX = 0f;
        internal static float RichListingHeaderOffsetY = 0f;
        internal static float RichListingNameOffsetX = 0f;
        internal static float RichListingNameOffsetY = 0f;
        internal static float RichListingMetaOffsetX = 0f;
        internal static float RichListingMetaOffsetY = 0f;
        internal static float RichListingStatsOffsetX = 0f;
        internal static float RichListingStatsOffsetY = 0f;
        internal static float RichListingStatsRowSpacing = 2f;
        internal static float RichListingStatCellWidth = 64f;
        internal static float RichListingStatLabelWidth = 30f;
        internal static float RichListingStatGradeWidth = 26f;
        internal static float RichListingButtonHeight = 28f;
        internal static float RichListingButtonWidth = 260f;
        internal static float RichListingButtonOffsetX = 0f;
        internal static string RichListingButtonAlign = "Center";
        internal static float RichListingScrollHeight = 260f;
        internal static int RichListingHeaderFontSize = 16;
        internal static int RichListingNameFontSize = 14;
        internal static int RichListingMetaFontSize = 14;
        internal static int RichListingStatsFontSize = 13;
        internal static string RichListingShinyMarkerText = "✦";
        internal static string RichListingShinyMarkerColorHex = "D6B24A";
        internal static int RichListingShinyMarkerFontSize = 14;
        internal static bool RichListingShinyMarkerUseChatIcon = true;
        internal static string RichListingShinyMarkerSpriteName = "sparklyIcon";
        internal static float RichListingShinyMarkerOffsetX = 0f;
        internal static float RichListingShinyMarkerOffsetY = 0f;
        internal static bool RichListingMetadataDebugLogging = false;

        // Selector row/preview layout tuning for Create Listing dropdowns.
        internal static float OfferedMONRowHeight = 100f;
        internal static float OfferedMONSelectedPreviewHeight = 100f;
        internal static bool OfferedMONShowIcon = true;
        internal static float OfferedMONIconSize = 100f;
        internal static float OfferedMONIconOffsetX = 0f;
        internal static float OfferedMONIconOffsetY = 0f;
        internal static float OfferedMONTextOffsetX = 50f;
        internal static float OfferedMONTextOffsetY = 0f;

        internal static float RequestedMONRowHeight = 100f;
        internal static float RequestedMONSelectedPreviewHeight = 100f;
        internal static bool RequestedMONShowIcon = true;
        internal static float RequestedMONIconSize = 100f;
        internal static float RequestedMONIconOffsetX = 0f;
        internal static float RequestedMONIconOffsetY = 0f;
        internal static float RequestedMONTextOffsetX = 50f;
        internal static float RequestedMONTextOffsetY = 0f;

        private const float NativeIconX = 0f;
        private const float NativeLabelX = 14f;
        private const float NativeButtonRowY = -1.5f;

        private Rect _windowRect = new Rect(60, 60, 560, 620);
        private bool _resizingWindow;
        private bool _showWindow;
        private Vector2 _scroll;
        private string _status = "GTS not connected.";
        private string _requestSpecies = "";
        private bool _requestSpeciesDropdownOpen;
        private Vector2 _requestSpeciesDropdownScroll;
        private bool _speciesOptionsLoaded;
        private readonly List<string> _speciesOptions = new List<string>();
        private int _offeredBoxSlot = -1;
        private bool _offeredMonDropdownOpen;
        private Vector2 _offeredMonDropdownScroll;
        private readonly List<BoxMonOption> _offeredMonOptions = new List<BoxMonOption>();
        private int _pageIndex;
        private readonly List<GtsListing> _listings = new List<GtsListing>();
        private readonly List<GtsListing> _myListings = new List<GtsListing>();
        private bool _busy;
        private bool _loggedIn;
        private string _username = "";
        private string _steamId64 = "";
        private string _sessionToken = "";
        private bool _applicationQuitting;
        private bool _aioAuthBusy;
        private bool _keepAliveBusy;
        private float _nextKeepAliveAt;
        private const float AioKeepAliveSeconds = 30f;
        private GtsSocketClient _client;
        private GameObject _gtsSubMenuButton;
        private readonly Dictionary<GameObject, Vector2> _subMenuOriginalPositions = new Dictionary<GameObject, Vector2>();
        private bool _cancelOriginalCaptured;
        private Vector2 _subMenuOriginalSize;
        private string _mode = "Browse";
        private Mon _capturedBoxMon;
        private int _capturedBoxSlot = -1;
        private bool _capturedFromGlobalView;

        private GameObject _nativeButtonRoot;
        private Button _nativeGtsButton;
        private TMP_Text _nativeButtonText;
        private TMP_Text _nativeButtonLabel;
        private GameObject _nativeIcon;
        private GameObject _nativeHighlight;
        private Sprite _cachedGtsBoxSprite;
        private Transform _nativeButtonParent;        private readonly Dictionary<string, Sprite> _richIconCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

        private bool _gtsGuiStylesReady;
        private GUIStyle _mpWindowStyle;
        private GUIStyle _mpTitleStyle;
        private GUIStyle _mpHeaderStyle;
        private GUIStyle _mpLabelStyle;
        private GUIStyle _mpTinyLabelStyle;
        private GUIStyle _mpButtonStyle;
        private GUIStyle _mpCloseButtonStyle;
        private GUIStyle _mpCardStyle;
        private GUIStyle _mpTextFieldStyle;
        private GUIStyle _mpSectionTitleStyle;
        private GUIStyle _mpRichHeaderStyle;
        private GUIStyle _mpRichNameStyle;
        private GUIStyle _mpRichMetaStyle;
        private GUIStyle _mpRichVibePlusStyle;
        private GUIStyle _mpRichVibeMinusStyle;
        private GUIStyle _mpRichStatStyle;
        private Texture2D _mpPaperTex;
        private Texture2D _mpCardTex;
        private Texture2D _mpButtonTex;
        private Texture2D _mpButtonHoverTex;
        private Texture2D _mpButtonActiveTex;
        private Texture2D _mpDarkTex;
        private Texture2D _mpTextFieldTex;
        private Texture2D _mpGenderMaleTex;
        private Texture2D _mpGenderFemaleTex;
        private Texture2D _mpGenderUnknownTex;

        public static void EnsureHost()
        {
            GTSNativePatcher.EnsureRuntimeHost();
        }

        public static bool IsOpenForAio()
        {
            try { return Instance != null && Instance._showWindow; } catch { return false; }
        }

        public static bool IsLoggedInForAio()
        {
            try { return Instance != null && Instance._loggedIn && Instance._client != null; } catch { return false; }
        }

        public static bool IsAuthBusyForAio()
        {
            try { return Instance != null && (Instance._busy || Instance._aioAuthBusy); } catch { return false; }
        }

        public static string GetStatusForAio()
        {
            try { return Instance != null ? Instance._status : "Trading Post runtime not loaded."; } catch { return "Trading Post status unavailable."; }
        }

        public static string GetSteamId64ForAio()
        {
            try { return Instance != null ? (Instance._steamId64 ?? string.Empty) : string.Empty; } catch { return string.Empty; }
        }

        public static string GetSteamDisplayNameForAio()
        {
            try { return Instance != null ? (Instance._username ?? string.Empty) : string.Empty; } catch { return string.Empty; }
        }

        public static string GetAioSessionTokenForSocial()
        {
            try { return Instance != null ? (Instance._sessionToken ?? string.Empty) : string.Empty; } catch { return string.Empty; }
        }

        public static bool IsMouseOverWindowForAio(Vector2 guiMousePosition)
        {
            try
            {
                if (Instance == null || !Instance._showWindow)
                    return false;
                float s = Mathf.Max(0.5f, WindowScale);
                Rect r = Instance._windowRect;
                if (Math.Abs(s - 1f) > 0.001f)
                    r = new Rect(r.x * s, r.y * s, r.width * s, r.height * s);
                return r.Contains(guiMousePosition);
            }
            catch { return false; }
        }

        public static void OpenFromAioChatWindow()
        {
            try
            {
                EnsureHost();
                if (Instance == null)
                    return;
                Instance._showWindow = true;
                Instance._status = Instance._loggedIn ? "Ready." : "Connect with Steam from the MMOnsterpatch chat window first.";
                if (Instance._loggedIn && !Instance._busy)
                    Instance.StartCoroutine(Instance.SearchListingsCoroutine(Instance._pageIndex));
                if (DebugLogging) GTSNativePatcher.RuntimeLog("Trading Post opened from AIO chat window.");
            }
            catch (Exception ex)
            {
                GTSNativePatcher.RuntimeWarn("OpenFromAioChatWindow failed: " + ex.Message);
            }
        }

        public static IEnumerator EnsureSteamAuthForAioCoroutine()
        {
            EnsureHost();
            if (Instance == null)
                yield break;
            yield return Instance.EnsureAioAuthenticatedCoroutine();
        }

        public static void DisconnectForAio(bool clearSessionToken)
        {
            try
            {
                if (Instance != null)
                    Instance.Disconnect(true, clearSessionToken);
            }
            catch { }
        }

        public static void DisconnectNetworkOnlyForAio()
        {
            try
            {
                // Keeps the Steam/GTS AIO session token in memory for this game process only.
                // Reconnect can use SESSION_LOGIN without opening Steam again.
                if (Instance != null)
                    Instance.Disconnect(true, false);
            }
            catch { }
        }

        public static void ClearPlaySessionTokenForAio()
        {
            try
            {
                if (Instance != null)
                    Instance.Disconnect(true, true);
            }
            catch { }
        }

        private void Awake()
        {
            Instance = this;
            LoadConfig();
            GTSNativePatcher.RuntimeLog($"{Name} {Version} runtime host loaded. Server={ServerHost}:{ServerPort}");
        }

        private static void LoadConfig()
        {
            try
            {
                string path = Path.Combine(Paths.ConfigPath, ConfigFileName);
                if (!File.Exists(path))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllText(path, @"[Server]
Host = mon.gamingwithgoose.com
Port = 61526

[Steam OpenID]
AutoOpenBrowser = true

[UI]
WindowScale = 1.0
ShowBoxOverlayButton = false
BoxOverlayX = 1180
BoxOverlayY = 78
ShowNativeBoxButton = false
# Positive move Right - Negative move Left
NativeButtonX = 12
# Positive move Up - Negative move Down
NativeButtonY = -40


[Rich Listing Display]
ShowRichListingInfo = true
RichListingUseMonIcon = true
# Offered MoN icon. Offered art uses shiny sprites when the listed MoN is shiny.
RichListingIconSize = 58
# Positive X moves offered icon right. Positive Y moves offered icon down.
RichListingIconOffsetX = 0
RichListingIconOffsetY = 0
# Requested MoN icon shown on the right side of browse/listing cards. Requested art always uses normal/non-shiny sprites.
RichListingShowRequestedMonIcon = true
RichListingRequestedIconSize = 58
RichListingRequestedIconOffsetX = 50
RichListingRequestedIconOffsetY = 0
RichListingRequestedIconColumnWidth = 66
# Draw HP/ATK/DEF/MAG/MDF/SPD in one row instead of two rows.
RichListingStatsSingleLine = true
# Hex RGB color for the vibe/stat name boosted by the MoN vibe. Default matches the green sample provided.
RichListingVibePlusColorHex = A2BA9C
# Hex RGB color for the vibe/stat name reduced by the MoN vibe. Default matches the blue sample provided.
RichListingVibeMinusColorHex = 85ACBD
# v0.5 compact stat widths. These are repeated here so existing v0.4 configs pick up the thinner one-line layout.
RichListingStatCellWidth = 64
RichListingStatLabelWidth = 34
RichListingStatGradeWidth = 18
# Optional manual vibe index display-name mapping. Leave blank to let the mod try to resolve the game's internal name first.
RichListingVibeIndex0Name =
RichListingVibeIndex1Name =
RichListingVibeIndex2Name =
RichListingVibeIndex3Name =
RichListingVibeIndex4Name =
RichListingVibeIndex5Name =
RichListingVibeIndex6Name =
RichListingVibeIndex7Name =
RichListingVibeIndex8Name =
RichListingVibeIndex9Name =
RichListingVibeIndex10Name =
RichListingVibeIndex11Name =
RichListingVibeIndex12Name =
RichListingVibeIndex13Name =
RichListingVibeIndex14Name =
RichListingVibeIndex15Name =
RichListingVibeIndex16Name =
RichListingVibeIndex17Name =
RichListingVibeIndex18Name =
RichListingVibeIndex19Name =
# Space between listing cards and small inner padding before content.
RichListingCardSpacing = 6
RichListingCardInnerPadding = 4
# Listing-card width. 0 = auto-fit. Auto base shrinks/grows with RequestedIconOffsetX if enabled.
RichListingCardWidth = 0
RichListingCardAutoBaseWidth = 500
RichListingCardAutoFollowRequestedIconOffsetX = false
RichListingCardAutoExtraWidth = 0
RichListingCardOffsetX = -30
RichListingCardAlign = Left
# Header/name/meta/stat element offsets. Positive X moves right. Positive Y moves down.
RichListingHeaderOffsetX = 0
RichListingHeaderOffsetY = 0
RichListingNameOffsetX = 0
RichListingNameOffsetY = 0
RichListingMetaOffsetX = 0
RichListingMetaOffsetY = 0
RichListingStatsOffsetX = 0
RichListingStatsOffsetY = 0
RichListingStatsRowSpacing = 2
# Widths used by each stat cell.
RichListingStatCellWidth = 64
RichListingStatLabelWidth = 34
RichListingStatGradeWidth = 18
RichListingButtonHeight = 28
# Width/position for listing action buttons like Offer Chosen MoN / Cancel Listing / Your Listing.
# Set RichListingButtonWidth = 0 to stretch full card width. Align supports Left, Center, or Right.
RichListingButtonWidth = 260
RichListingButtonOffsetX = 0
RichListingButtonAlign = Center
RichListingScrollHeight = 260
RichListingHeaderFontSize = 16
RichListingNameFontSize = 14
RichListingMetaFontSize = 14
RichListingStatsFontSize = 13
# Shiny marker next to offered MoN in the listing header.
# v1.1.8 disables the loose internal sprite search because it could grab wrong blue UI art.
RichListingShinyMarkerText = ✦
RichListingShinyMarkerColorHex = D6B24A
RichListingShinyMarkerFontSize = 28
RichListingShinyMarkerUseChatIcon = true
RichListingShinyMarkerSpriteName = sparklyIcon
# Positive X moves shiny marker right. Positive Y moves shiny marker down.
RichListingShinyMarkerOffsetX = 0
RichListingShinyMarkerOffsetY = -5
# Logs reflected Mon fields used for vibe/stat debugging. Leave false for normal play.
RichListingMetadataDebugLogging = false


[OfferedMON]
# Slightly taller selected-preview and dropdown rows for the Offered MoN selector.
RowHeight = 100
SelectedPreviewHeight = 100
ShowIcon = true
IconSize = 100
# Positive X moves right. Positive Y moves down.
IconOffsetX = 0
IconOffsetY = 0
# Text offsets inside the selector row/preview box.
TextOffsetX = 50
TextOffsetY = 0

[RequestedMON]
# Separate tuning for the Requested MoN selector.
RowHeight = 100
SelectedPreviewHeight = 100
ShowIcon = true
IconSize = 100
# Positive X moves right. Positive Y moves down.
IconOffsetX = 0
IconOffsetY = 0
# Text offsets inside the selector row/preview box.
TextOffsetX = 50
TextOffsetY = 0

[Debug]
DebugLogging = true
");
                }
                string currentSection = "";
                foreach (string raw in File.ReadAllLines(path))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";")) continue;
                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        currentSection = line.Substring(1, line.Length - 2).Trim();
                        continue;
                    }
                    int eq = line.IndexOf('=');
                    if (eq < 0) continue;
                    string key = line.Substring(0, eq).Trim();
                    string value = line.Substring(eq + 1).Trim();

                    // v0.9 generated clean [OfferedMON] / [RequestedMON] headers with short keys
                    // like TextOffsetX, IconSize, RowHeight, etc. The first parser only accepted
                    // fully-prefixed keys, so those values did not actually move the text/icon.
                    // Make the parser section-aware while still accepting the prefixed names.
                    if (currentSection.Equals("OfferedMON", StringComparison.OrdinalIgnoreCase)
                        && !key.StartsWith("OfferedMON", StringComparison.OrdinalIgnoreCase))
                    {
                        key = "OfferedMON" + key;
                    }
                    else if (currentSection.Equals("RequestedMON", StringComparison.OrdinalIgnoreCase)
                        && !key.StartsWith("RequestedMON", StringComparison.OrdinalIgnoreCase))
                    {
                        key = "RequestedMON" + key;
                    }
                    if (key.Equals("Host", StringComparison.OrdinalIgnoreCase)) ServerHost = value;
                    else if (key.Equals("Port", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out int port)) ServerPort = port;
                    else if (key.Equals("AutoOpenBrowser", StringComparison.OrdinalIgnoreCase) && bool.TryParse(value, out bool aob)) AutoOpenSteamLogin = aob;
                    else if (key.Equals("WindowScale", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float scale)) WindowScale = scale;
                    else if (key.Equals("ShowBoxOverlayButton", StringComparison.OrdinalIgnoreCase) && bool.TryParse(value, out bool overlay)) ShowBoxOverlayButton = overlay;
                    else if (key.Equals("BoxOverlayX", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float ox)) BoxOverlayX = ox;
                    else if (key.Equals("BoxOverlayY", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float oy)) BoxOverlayY = oy;
                    else if (key.Equals("ShowNativeBoxButton", StringComparison.OrdinalIgnoreCase) && bool.TryParse(value, out bool snb)) ShowNativeBoxButton = snb;
                    else if (key.Equals("NativeButtonX", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float nbx)) NativeButtonX = nbx;
                    else if (key.Equals("NativeButtonY", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float nby)) NativeButtonY = nby;                    else if (key.Equals("ShowRichListingInfo", StringComparison.OrdinalIgnoreCase) && bool.TryParse(value, out bool srli)) ShowRichListingInfo = srli;
                    else if (key.Equals("RichListingUseMonIcon", StringComparison.OrdinalIgnoreCase) && bool.TryParse(value, out bool rlmii)) RichListingUseMonIcon = rlmii;
                    else if (key.Equals("RichListingIconSize", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float rlis)) RichListingIconSize = rlis;
                    else if (key.Equals("RichListingIconOffsetX", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float rliox)) RichListingIconOffsetX = rliox;
                    else if (key.Equals("RichListingIconOffsetY", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float rlioy)) RichListingIconOffsetY = rlioy;
                    else if (key.Equals("RichListingShowRequestedMonIcon", StringComparison.OrdinalIgnoreCase) && bool.TryParse(value, out bool rlreqshow)) RichListingShowRequestedMonIcon = rlreqshow;
                    else if (key.Equals("RichListingRequestedIconSize", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float rlreqs)) RichListingRequestedIconSize = rlreqs;
                    else if (key.Equals("RichListingRequestedIconOffsetX", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float rlreqox)) RichListingRequestedIconOffsetX = rlreqox;
                    else if (key.Equals("RichListingRequestedIconOffsetY", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float rlreqoy)) RichListingRequestedIconOffsetY = rlreqoy;
                    else if (key.Equals("RichListingRequestedIconColumnWidth", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float rlreqcw)) RichListingRequestedIconColumnWidth = rlreqcw;
                    else if (key.Equals("RichListingStatsSingleLine", StringComparison.OrdinalIgnoreCase) && bool.TryParse(value, out bool rlsol)) RichListingStatsSingleLine = rlsol;
                    else if (key.Equals("RichListingVibePlusColorHex", StringComparison.OrdinalIgnoreCase)) { RichListingVibePlusColorHex = SafeNonEmpty(value, "A2BA9C"); RichListingVibePlusColor = ParseRichListingHexColor(RichListingVibePlusColorHex, RichListingVibePlusColor); }
                    else if (key.Equals("RichListingVibeMinusColorHex", StringComparison.OrdinalIgnoreCase)) { RichListingVibeMinusColorHex = SafeNonEmpty(value, "85ACBD"); RichListingVibeMinusColor = ParseRichListingHexColor(RichListingVibeMinusColorHex, RichListingVibeMinusColor); }
                    else if (key.StartsWith("RichListingVibeIndex", StringComparison.OrdinalIgnoreCase) && key.EndsWith("Name", StringComparison.OrdinalIgnoreCase))
                    {
                        string middle = key.Substring("RichListingVibeIndex".Length, key.Length - "RichListingVibeIndex".Length - "Name".Length);
                        if (int.TryParse(middle, out int vibeIndex) && vibeIndex >= 0 && vibeIndex < RichListingVibeIndexNames.Length)
                            RichListingVibeIndexNames[vibeIndex] = value != null ? value.Trim() : "";
                    }
                    else if (key.Equals("RichListingCardSpacing", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float rlcs)) RichListingCardSpacing = rlcs;
                    else if (key.Equals("RichListingCardInnerPadding", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float rlcip)) RichListingCardInnerPadding = rlcip;
                    else if (key.Equals("RichListingCardWidth", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float rlcw)) RichListingCardWidth = rlcw;
                    else if (key.Equals("RichListingCardAutoBaseWidth", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float rlcabw)) RichListingCardAutoBaseWidth = rlcabw;
                    else if (key.Equals("RichListingCardAutoFollowRequestedIconOffsetX", StringComparison.OrdinalIgnoreCase) && bool.TryParse(value, out bool rlcaf)) RichListingCardAutoFollowRequestedIconOffsetX = rlcaf;
                    else if (key.Equals("RichListingCardAutoExtraWidth", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float rlcaew)) RichListingCardAutoExtraWidth = rlcaew;
                    else if (key.Equals("RichListingCardOffsetX", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float rlcoffx)) RichListingCardOffsetX = rlcoffx;
                    else if (key.Equals("RichListingCardAlign", StringComparison.OrdinalIgnoreCase)) RichListingCardAlign = value;
                    else if (key.Equals("RichListingHeaderOffsetX", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float rlhox)) RichListingHeaderOffsetX = rlhox;
                    else if (key.Equals("RichListingHeaderOffsetY", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float rlhoy)) RichListingHeaderOffsetY = rlhoy;
                    else if (key.Equals("RichListingNameOffsetX", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float rlnox)) RichListingNameOffsetX = rlnox;
                    else if (key.Equals("RichListingNameOffsetY", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float rlnoy)) RichListingNameOffsetY = rlnoy;
                    else if (key.Equals("RichListingMetaOffsetX", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float rlmox)) RichListingMetaOffsetX = rlmox;
                    else if (key.Equals("RichListingMetaOffsetY", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float rlmoy)) RichListingMetaOffsetY = rlmoy;
                    else if (key.Equals("RichListingStatsOffsetX", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float rlsox)) RichListingStatsOffsetX = rlsox;
                    else if (key.Equals("RichListingStatsOffsetY", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float rlsoy)) RichListingStatsOffsetY = rlsoy;
                    else if (key.Equals("RichListingStatsRowSpacing", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float rlsrs)) RichListingStatsRowSpacing = rlsrs;
                    else if (key.Equals("RichListingStatCellWidth", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float rlscw)) RichListingStatCellWidth = rlscw;
                    else if (key.Equals("RichListingStatLabelWidth", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float rlslw)) RichListingStatLabelWidth = rlslw;
                    else if (key.Equals("RichListingStatGradeWidth", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float rlsgw)) RichListingStatGradeWidth = rlsgw;
                    else if (key.Equals("RichListingButtonHeight", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float rlbh)) RichListingButtonHeight = rlbh;
                    else if (key.Equals("RichListingButtonWidth", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float rlbw)) RichListingButtonWidth = rlbw;
                    else if (key.Equals("RichListingButtonOffsetX", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float rlboffx)) RichListingButtonOffsetX = rlboffx;
                    else if (key.Equals("RichListingButtonAlign", StringComparison.OrdinalIgnoreCase)) RichListingButtonAlign = value;
                    else if (key.Equals("RichListingScrollHeight", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float rlsh)) RichListingScrollHeight = rlsh;
                    else if (key.Equals("RichListingHeaderFontSize", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out int rlhfs)) RichListingHeaderFontSize = rlhfs;
                    else if (key.Equals("RichListingNameFontSize", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out int rlnfs)) RichListingNameFontSize = rlnfs;
                    else if (key.Equals("RichListingMetaFontSize", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out int rlmfs)) RichListingMetaFontSize = rlmfs;
                    else if (key.Equals("RichListingStatsFontSize", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out int rlsfs)) RichListingStatsFontSize = rlsfs;
                    else if (key.Equals("RichListingShinyMarkerText", StringComparison.OrdinalIgnoreCase)) RichListingShinyMarkerText = value;
                    else if (key.Equals("RichListingShinyMarkerColorHex", StringComparison.OrdinalIgnoreCase))
                    {
                        RichListingShinyMarkerColorHex = NormalizeHex6(value, "D6B24A");
                    }
                    else if (key.Equals("RichListingShinyMarkerFontSize", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out int rlsmfs)) RichListingShinyMarkerFontSize = rlsmfs;
                    else if (key.Equals("RichListingShinyMarkerUseChatIcon", StringComparison.OrdinalIgnoreCase) && bool.TryParse(value, out bool rlsmuci)) RichListingShinyMarkerUseChatIcon = rlsmuci;
                    else if (key.Equals("RichListingShinyMarkerSpriteName", StringComparison.OrdinalIgnoreCase)) RichListingShinyMarkerSpriteName = value;
                    else if (key.Equals("RichListingShinyMarkerOffsetX", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float rlsmox)) RichListingShinyMarkerOffsetX = rlsmox;
                    else if (key.Equals("RichListingShinyMarkerOffsetY", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float rlsmoy)) RichListingShinyMarkerOffsetY = rlsmoy;
                    else if (key.Equals("RichListingMetadataDebugLogging", StringComparison.OrdinalIgnoreCase) && bool.TryParse(value, out bool rlmdbg)) RichListingMetadataDebugLogging = rlmdbg;
                    else if (key.Equals("OfferedMONRowHeight", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float omrh)) OfferedMONRowHeight = omrh;
                    else if (key.Equals("OfferedMONSelectedPreviewHeight", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float omsph)) OfferedMONSelectedPreviewHeight = omsph;
                    else if (key.Equals("OfferedMONShowIcon", StringComparison.OrdinalIgnoreCase) && bool.TryParse(value, out bool omsi)) OfferedMONShowIcon = omsi;
                    else if (key.Equals("OfferedMONIconSize", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float omis)) OfferedMONIconSize = omis;
                    else if (key.Equals("OfferedMONIconOffsetX", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float omiox)) OfferedMONIconOffsetX = omiox;
                    else if (key.Equals("OfferedMONIconOffsetY", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float omioy)) OfferedMONIconOffsetY = omioy;
                    else if (key.Equals("OfferedMONTextOffsetX", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float omtox)) OfferedMONTextOffsetX = omtox;
                    else if (key.Equals("OfferedMONTextOffsetY", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float omtoy)) OfferedMONTextOffsetY = omtoy;
                    else if (key.Equals("RequestedMONRowHeight", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float rmrh)) RequestedMONRowHeight = rmrh;
                    else if (key.Equals("RequestedMONSelectedPreviewHeight", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float rmsph)) RequestedMONSelectedPreviewHeight = rmsph;
                    else if (key.Equals("RequestedMONShowIcon", StringComparison.OrdinalIgnoreCase) && bool.TryParse(value, out bool rmsi)) RequestedMONShowIcon = rmsi;
                    else if (key.Equals("RequestedMONIconSize", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float rmis)) RequestedMONIconSize = rmis;
                    else if (key.Equals("RequestedMONIconOffsetX", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float rmiox)) RequestedMONIconOffsetX = rmiox;
                    else if (key.Equals("RequestedMONIconOffsetY", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float rmioy)) RequestedMONIconOffsetY = rmioy;
                    else if (key.Equals("RequestedMONTextOffsetX", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float rmtox)) RequestedMONTextOffsetX = rmtox;
                    else if (key.Equals("RequestedMONTextOffsetY", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float rmtoy)) RequestedMONTextOffsetY = rmtoy;
                    else if (key.Equals("DebugLogging", StringComparison.OrdinalIgnoreCase) && bool.TryParse(value, out bool dbg)) DebugLogging = dbg;
                }

                PruneGtsUserConfig(path);

                RichListingIconSize = Mathf.Clamp(RichListingIconSize, 24f, 160f);
                RichListingRequestedIconSize = Mathf.Clamp(RichListingRequestedIconSize, 24f, 160f);
                RichListingRequestedIconColumnWidth = Mathf.Clamp(RichListingRequestedIconColumnWidth, 30f, 220f);
                RichListingStatCellWidth = Mathf.Clamp(RichListingStatCellWidth, 28f, 160f);
                RichListingStatLabelWidth = Mathf.Clamp(RichListingStatLabelWidth, 18f, 90f);
                RichListingStatGradeWidth = Mathf.Clamp(RichListingStatGradeWidth, 18f, 90f);
                RichListingScrollHeight = Mathf.Clamp(RichListingScrollHeight, 160f, 520f);
                RichListingButtonHeight = Mathf.Clamp(RichListingButtonHeight, 18f, 60f);
                RichListingButtonWidth = Mathf.Clamp(RichListingButtonWidth, 0f, 800f);
                RichListingCardWidth = Mathf.Clamp(RichListingCardWidth, 0f, 1200f);
                RichListingCardAutoBaseWidth = Mathf.Clamp(RichListingCardAutoBaseWidth, 320f, 1200f);
                RichListingHeaderFontSize = Mathf.Clamp(RichListingHeaderFontSize, 9, 26);
                RichListingNameFontSize = Mathf.Clamp(RichListingNameFontSize, 8, 22);
                RichListingMetaFontSize = Mathf.Clamp(RichListingMetaFontSize, 8, 22);
                RichListingStatsFontSize = Mathf.Clamp(RichListingStatsFontSize, 8, 22);
                RichListingShinyMarkerFontSize = Mathf.Clamp(RichListingShinyMarkerFontSize, 10, 30);
                RichListingShinyMarkerOffsetX = Mathf.Clamp(RichListingShinyMarkerOffsetX, -60f, 60f);
                RichListingShinyMarkerOffsetY = Mathf.Clamp(RichListingShinyMarkerOffsetY, -40f, 40f);
                OfferedMONRowHeight = Mathf.Clamp(OfferedMONRowHeight, 20f, 60f);
                OfferedMONSelectedPreviewHeight = Mathf.Clamp(OfferedMONSelectedPreviewHeight, 20f, 60f);
                OfferedMONIconSize = Mathf.Clamp(OfferedMONIconSize, 12f, 48f);
                RequestedMONRowHeight = Mathf.Clamp(RequestedMONRowHeight, 20f, 60f);
                RequestedMONSelectedPreviewHeight = Mathf.Clamp(RequestedMONSelectedPreviewHeight, 20f, 60f);
                RequestedMONIconSize = Mathf.Clamp(RequestedMONIconSize, 12f, 48f);

                // instead of the first. Users can still set an explicit index such as 6 manually.
                
                // Earlier test builds shipped with NativeButtonX=310 / NativeButtonY=-6, then
                // NativeButtonX=230 / NativeButtonY=-36. If an existing config still has one of
                // those exact old defaults, auto-migrate to the current cleaner default so users
                // do not need to delete their cfg. Custom positions are left alone.
                bool oldOffscreenDefault = NativeButtonX >= 300f && Math.Abs(NativeButtonY - (-6f)) < 0.01f;
                bool oldV019Default = Math.Abs(NativeButtonX - 230f) < 0.01f && Math.Abs(NativeButtonY - (-36f)) < 0.01f;
                if (oldOffscreenDefault || oldV019Default)
                {
                    NativeButtonX = 12f;
                    NativeButtonY = -40f;
                    if (DebugLogging)
                        GTSNativePatcher.RuntimeLog("Auto-corrected old GTS native button position to X=12, Y=-40.");
                }

                // Final build: Trading Post is opened from the pause menu only.
                // Force-disable older config values so old cfg files do not resurrect the Box UI GTS button.
                if (ShowBoxOverlayButton || ShowNativeBoxButton)
                {
                    ShowBoxOverlayButton = false;
                    ShowNativeBoxButton = false;
                    if (DebugLogging)
                        GTSNativePatcher.RuntimeLog("Disabled Box UI GTS buttons; pause-menu Trading Post is the only entry point.");
                }
            }
            catch (Exception ex)
            {
                GTSNativePatcher.RuntimeWarn("GTS config load failed: " + ex.Message);
            }
        }

        private static void PruneGtsUserConfig(string path)
        {
            AIOConfigPruner.Prune(path, new string[]
            {
                "Server.Host",
                "Server.Port",
                "Steam OpenID.AutoOpenBrowser",
                "UI.WindowScale"
            });
        }

        private void Update()
        {
            try
            {
                MaintainAioSessionKeepAlive();
                GameScript gs = FindObjectOfType<GameScript>();
                if (gs != null)
                {
                    DisableBoxUiGtsButtons(gs);
                    MaintainGtsSelectionArrow(gs);
                }
            }
            catch (Exception ex)
            {
                if (DebugLogging) GTSNativePatcher.RuntimeWarn("GTS Update failed: " + ex.Message);
            }
        }

        public static void OpenFromNativeMenu()
        {
            try
            {
                if (Instance == null)
                    return;

                Instance._showWindow = true;
                Instance._status = Instance._loggedIn ? "Ready." : "Not logged in. Click Login with Steam.";
                if (DebugLogging)
                    GTSNativePatcher.RuntimeLog("GTS opened from native Box UI button.");
            }
            catch (Exception ex)
            {
                GTSNativePatcher.RuntimeWarn("OpenFromNativeMenu failed: " + ex.Message);
            }
        }

        public static void OpenFromPauseMenu()
        {
            OpenFromAioChatWindow();
        }

        public static void CaptureSelectionFromBoxManager(object boxManagerObj)
        {
            try
            {
                EnsureInstanceForCapture();
                if (Instance == null)
                    return;

                BoxManager bm = boxManagerObj as BoxManager;
                if (bm == null || bm.boxMons == null)
                    return;

                GameScript gs = bm.gameScript;
                int slot = bm.curBoxEntry + bm.curBoxTab * 27;
                Mon mon = null;
                if (slot >= 0 && slot < bm.boxMons.Length)
                    mon = bm.boxMons[slot];

                // BoxManager.SelectBoxEntry also sets GameScript.curInteractingMon when it opens the submenu.
                // This is a better source of truth once the injected GTS row becomes the selected button.
                if (mon == null && gs != null)
                    mon = gs.curInteractingMon;

                int foundSlot = mon != null ? FindMonSlot(bm, mon) : slot;
                if (foundSlot >= 0)
                    slot = foundSlot;

                Instance._capturedBoxMon = mon;
                Instance._capturedBoxSlot = slot;
                Instance._capturedFromGlobalView = IsGlobalBoxView(bm);

                if (mon != null && slot >= 0)
                    Instance._offeredBoxSlot = slot;

                if (mon != null && string.IsNullOrWhiteSpace(Instance._requestSpecies))
                    Instance._requestSpecies = GetSpecies(mon);

                if (DebugLogging)
                    GTSNativePatcher.RuntimeLog("Captured GTS selected MoN: " + (mon == null ? "NULL" : GetSpecies(mon)) + " slot=" + slot + " global=" + Instance._capturedFromGlobalView);
            }
            catch (Exception ex)
            {
                GTSNativePatcher.RuntimeWarn("CaptureSelectionFromBoxManager failed: " + ex.Message);
            }
        }

        public void EnsureTopRightButtonFromGameScriptObject(object gameScriptObj)
        {
            try
            {
                EnsureTopRightButton(gameScriptObj as GameScript);
            }
            catch (Exception ex)
            {
                if (DebugLogging) GTSNativePatcher.RuntimeWarn("EnsureTopRightButtonFromGameScriptObject failed: " + ex.Message);
            }
        }

        public void EnsureTopRightButtonFromBoxManagerObject(object boxManagerObj)
        {
            try
            {
                BoxManager bm = boxManagerObj as BoxManager;
                EnsureTopRightButton(bm != null ? bm.gameScript : null);
            }
            catch (Exception ex)
            {
                if (DebugLogging) GTSNativePatcher.RuntimeWarn("EnsureTopRightButtonFromBoxManagerObject failed: " + ex.Message);
            }
        }

        

        
        
        

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        private void EnsureTopRightButton(GameScript gs)
        {
            try
            {
                if (!ShowNativeBoxButton)
                {
                    DisableBoxUiGtsButtons(gs);
                    return;
                }

                BoxManager bm = gs != null ? gs.boxManager : null;
                bool shouldShow = gs != null && bm != null && gs.menuBox != null && gs.menuBox.activeSelf;
                if (!shouldShow)
                {
                    if (_nativeButtonRoot != null) _nativeButtonRoot.SetActive(false);
                    return;
                }

                CreateOrUpdateNativeGtsButton(gs, bm);
                if (_nativeButtonRoot != null) _nativeButtonRoot.SetActive(true);
                if (_gtsSubMenuButton != null) _gtsSubMenuButton.SetActive(false);
            }
            catch (Exception ex)
            {
                if (DebugLogging) GTSNativePatcher.RuntimeWarn("EnsureTopRightButton failed: " + ex.Message);
            }
        }

        private void DisableBoxUiGtsButtons(GameScript gs)
        {
            try
            {
                if (_nativeButtonRoot != null)
                {
                    Destroy(_nativeButtonRoot);
                    _nativeButtonRoot = null;
                    _nativeGtsButton = null;
                    _nativeButtonText = null;
                    _nativeButtonLabel = null;
                    _nativeIcon = null;
                    _nativeHighlight = null;
                    _nativeButtonParent = null;
                }

                if (_gtsSubMenuButton != null)
                    _gtsSubMenuButton.SetActive(false);

                BoxManager bm = gs != null ? gs.boxManager : null;
                if (bm != null)
                    RestoreSubMenuIfNeeded(bm);
            }
            catch (Exception ex)
            {
                if (DebugLogging) GTSNativePatcher.RuntimeWarn("DisableBoxUiGtsButtons failed: " + ex.Message);
            }
        }

        private void CreateOrUpdateNativeGtsButton(GameScript gs, BoxManager bm)
        {
            if (gs == null || bm == null) return;
            Transform parent = bm.txtBox != null ? bm.txtBox.transform.parent : gs.menuBox.transform;
            if (parent == null) return;

            if (_nativeButtonRoot != null && _nativeButtonParent != parent)
            {
                Destroy(_nativeButtonRoot);
                _nativeButtonRoot = null;
                _nativeGtsButton = null;
                _nativeButtonText = null;
                _nativeButtonLabel = null;
                _nativeIcon = null;
                _nativeHighlight = null;
            }

            if (_nativeButtonRoot == null)
            {
                _nativeButtonParent = parent;
                _nativeButtonRoot = new GameObject("MonsterpatchGTSBoxButtonRoot", typeof(RectTransform));
                _nativeButtonRoot.transform.SetParent(parent, false);
                _nativeButtonRoot.transform.SetAsLastSibling();
                RectTransform rootRt = _nativeButtonRoot.GetComponent<RectTransform>();
                rootRt.anchorMin = new Vector2(0f, 1f);
                rootRt.anchorMax = new Vector2(0f, 1f);
                rootRt.pivot = new Vector2(0f, 1f);
                rootRt.sizeDelta = new Vector2(78f, 15f);

                TMP_FontAsset font = bm.txtBox != null ? bm.txtBox.font : null;
                _nativeGtsButton = CreateNativeIconButton(_nativeButtonRoot.transform, "MonsterpatchGTSBoxButton", new Vector2(NativeIconX, NativeButtonRowY), font, out _nativeButtonText);
                _nativeGtsButton.onClick.AddListener(() =>
                {
                    try
                    {
                        GameScript g = FindObjectOfType<GameScript>();
                        BoxManager currentBm = g != null ? g.boxManager : null;
                        if (currentBm != null)
                            CaptureSelectionFromBoxManager(currentBm);
                    }
                    catch { }

                    _showWindow = true;
                    _status = _loggedIn ? "Ready." : "Not logged in. Click Login with Steam.";
                });
            }

            RectTransform rr = _nativeButtonRoot.GetComponent<RectTransform>();
            if (rr != null)
                rr.anchoredPosition = new Vector2(NativeButtonX, NativeButtonY);
            if (_nativeButtonRoot != null)
                _nativeButtonRoot.transform.SetAsLastSibling();

            TMP_FontAsset currentFont = bm.txtBox != null ? bm.txtBox.font : null;
            Color textColor = Color.black;
            try
            {
                if (bm.txtBox != null)
                    textColor = bm.txtBox.color;
            }
            catch { }

            if (_nativeButtonLabel == null)
                _nativeButtonLabel = CreateNativeLabel(_nativeButtonRoot.transform, "MonsterpatchGTSBoxButtonLabel", "GTS", new Vector2(NativeLabelX, NativeButtonRowY), currentFont, textColor);
            else
                UpdateNativeLabel(_nativeButtonLabel, "GTS", currentFont, textColor);

            EnsureNativeButtonIcon(bm);
            WireNativeButtonNavigation(bm);
            UpdateNativeButtonVisuals();
        }

        private Button CreateNativeIconButton(Transform parent, string name, Vector2 pos, TMP_FontAsset font, out TMP_Text text)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
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
            text.color = Color.black;

            return btn;
        }

        private TMP_Text CreateNativeLabel(Transform parent, string name, string label, Vector2 pos, TMP_FontAsset font, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(58f, 10f);

            TMP_Text tmp = go.GetComponent<TextMeshProUGUI>();
            UpdateNativeLabel(tmp, label, font, color);
            return tmp;
        }

        private void UpdateNativeLabel(TMP_Text tmp, string label, TMP_FontAsset font, Color color)
        {
            if (tmp == null) return;
            tmp.text = label;
            if (font != null) tmp.font = font;
            tmp.fontSize = 7.5f;
            tmp.fontStyle = FontStyles.Normal;
            tmp.enableAutoSizing = false;
            tmp.enableWordWrapping = false;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
            tmp.color = color;
            tmp.gameObject.SetActive(true);
        }

        private void EnsureNativeButtonIcon(BoxManager bm)
        {
            try
            {
                if (bm == null || bm.inBoxIcon == null || _nativeGtsButton == null) return;

                _cachedGtsBoxSprite = FindSpriteByName("boxColors_2") ?? _cachedGtsBoxSprite;

                Image sourceImage = bm.inBoxIcon.GetComponent<Image>();
                if (sourceImage == null) sourceImage = bm.inBoxIcon.GetComponentInChildren<Image>(true);
                Sprite fallbackSprite = sourceImage != null ? sourceImage.sprite : null;

                if (_nativeIcon == null)
                {
                    _nativeIcon = Instantiate(bm.inBoxIcon, _nativeGtsButton.transform);
                    _nativeIcon.name = "MonsterpatchGTS_BoxIcon";
                }

                EnsureNativeHighlight(_cachedGtsBoxSprite ?? fallbackSprite);
                SetupNativeIcon(_nativeIcon, _cachedGtsBoxSprite ?? fallbackSprite, Color.white);
                ClearButtonText(_nativeGtsButton);
                MakeButtonBackgroundInvisible(_nativeGtsButton);
            }
            catch (Exception ex)
            {
                if (DebugLogging) GTSNativePatcher.RuntimeWarn("GTS box button icon setup failed: " + ex.Message);
            }
        }

        private Sprite FindSpriteByName(string spriteName)
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

        private void EnsureNativeHighlight(Sprite sprite)
        {
            if (_nativeGtsButton == null) return;

            if (_nativeHighlight == null)
            {
                _nativeHighlight = new GameObject("MonsterpatchGTS_SelectedGlow", typeof(RectTransform), typeof(Image));
                _nativeHighlight.transform.SetParent(_nativeGtsButton.transform, false);
                _nativeHighlight.transform.SetAsFirstSibling();
            }

            RectTransform rt = _nativeHighlight.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;
                rt.sizeDelta = new Vector2(14f, 14f);
            }

            Image img = _nativeHighlight.GetComponent<Image>();
            if (img != null)
            {
                if (sprite != null) img.sprite = sprite;
                img.color = new Color(1f, 1f, 1f, 0.55f);
                img.raycastTarget = false;
                img.enabled = true;
            }
        }

        private void SetupNativeIcon(GameObject iconObj, Sprite sprite, Color color)
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

        private void ClearButtonText(Button button)
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

        private void MakeButtonBackgroundInvisible(Button button)
        {
            if (button == null) return;

            Image img = button.GetComponent<Image>();
            if (img != null)
            {
                img.color = new Color(1f, 1f, 1f, 0f);
                img.raycastTarget = true;
            }
        }

        private void UpdateNativeButtonVisuals()
        {
            try
            {
                if (_nativeButtonText != null) _nativeButtonText.text = string.Empty;
                if (_nativeButtonLabel != null)
                {
                    _nativeButtonLabel.text = "GTS";
                    _nativeButtonLabel.gameObject.SetActive(true);
                    _nativeButtonLabel.raycastTarget = false;
                }
                MakeButtonBackgroundInvisible(_nativeGtsButton);
                if (_nativeHighlight != null)
                    _nativeHighlight.SetActive(_showWindow);
            }
            catch { }
        }

        private void WireNativeButtonNavigation(BoxManager bm)
        {
            try
            {
                if (_nativeGtsButton == null) return;
                Navigation nav = new Navigation { mode = Navigation.Mode.Explicit };
                if (bm != null && bm.buttonBoxEntry != null && bm.buttonBoxEntry.Length > 0 && bm.buttonBoxEntry[0] != null)
                    nav.selectOnDown = bm.buttonBoxEntry[0].GetComponent<Selectable>();
                _nativeGtsButton.navigation = nav;
            }
            catch (Exception ex)
            {
                if (DebugLogging) GTSNativePatcher.RuntimeWarn("GTS native button navigation failed: " + ex.Message);
            }
        }

        private static void EnsureInstanceForCapture()
        {
            if (Instance == null)
                GTSNativePatcher.EnsureRuntimeHost();
        }

        private void OnGUI()
        {
            // Overlay button intentionally disabled; GTS opens from the native Box UI icon button.

            if (!_showWindow) return;

            EnsureGtsGuiStyles();

            float s = Mathf.Max(0.5f, WindowScale);
            Matrix4x4 oldMatrix = GUI.matrix;
            if (Math.Abs(s - 1f) > 0.001f)
                GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(s, s, 1f));

            DrawWindowBacking(_windowRect);
            _windowRect = GUI.Window(77319, _windowRect, DrawWindow, GUIContent.none, _mpWindowStyle);
            ClampGtsWindowToScreen();
            ConsumeImguiEventInsideWindow();
            GUI.matrix = oldMatrix;
        }

        private void EnsureGtsGuiStyles()
        {
            if (_gtsGuiStylesReady) return;

            // Alpha Warning / System Message palette.
            // Cream paper body with dark pixel borders; no green title/buttons.
            Color32 paper = new Color32(239, 229, 194, 255);
            Color32 card = new Color32(247, 239, 210, 255);
            Color32 button = new Color32(239, 229, 194, 255);
            Color32 buttonHover = new Color32(247, 239, 210, 255);
            Color32 buttonActive = new Color32(224, 211, 176, 255);
            Color32 dark = new Color32(44, 30, 25, 255);
            Color32 field = new Color32(239, 229, 194, 255);

            // Window frame is drawn manually as an Alpha Warning-style double border.
            _mpPaperTex = MakeSolidTex(paper);
            _mpCardTex = MakeBorderedTex(card, dark, 2);
            _mpButtonTex = MakeBorderedTex(button, dark, 2);
            _mpButtonHoverTex = MakeBorderedTex(buttonHover, dark, 2);
            _mpButtonActiveTex = MakeBorderedTex(buttonActive, dark, 2);
            _mpDarkTex = MakeSolidTex(dark);
            _mpTextFieldTex = MakeBorderedTex(field, dark, 2);
            _mpGenderMaleTex = MakeRoundedBadgeTex(new Color32(133, 172, 189, 255), dark, 1, 5);
            _mpGenderFemaleTex = MakeRoundedBadgeTex(new Color32(214, 150, 180, 255), dark, 1, 5);
            _mpGenderUnknownTex = MakeRoundedBadgeTex(new Color32(180, 170, 150, 255), dark, 1, 5);

            Color darkText = new Color32(53, 34, 34, 255);
            Color mutedText = new Color32(53, 34, 34, 255);
            Color buttonText = new Color32(53, 34, 34, 255);
            Font nativeGuiFont = FindNativeImGuiFont();

            _mpWindowStyle = new GUIStyle(GUI.skin.window);
            SetAllStyleStates(_mpWindowStyle, null, darkText);
            _mpWindowStyle.padding = new RectOffset(17, 17, 14, 17);
            _mpWindowStyle.margin = new RectOffset(0, 0, 0, 0);
            _mpWindowStyle.border = new RectOffset(0, 0, 0, 0);
            _mpWindowStyle.overflow = new RectOffset(0, 0, 0, 0);

            _mpTitleStyle = new GUIStyle(GUI.skin.label);
            _mpTitleStyle.fontSize = 20;
            _mpTitleStyle.fontStyle = FontStyle.Bold;
            _mpTitleStyle.alignment = TextAnchor.MiddleCenter;
            _mpTitleStyle.normal.textColor = darkText;

            _mpHeaderStyle = new GUIStyle(GUI.skin.label);
            _mpHeaderStyle.fontSize = 14;
            _mpHeaderStyle.fontStyle = FontStyle.Bold;
            _mpHeaderStyle.normal.textColor = darkText;
            _mpHeaderStyle.margin = new RectOffset(0, 0, 2, 2);

            _mpLabelStyle = new GUIStyle(GUI.skin.label);
            _mpLabelStyle.fontSize = 14;
            _mpLabelStyle.normal.textColor = darkText;
            _mpLabelStyle.margin = new RectOffset(0, 0, 2, 2);

            _mpTinyLabelStyle = new GUIStyle(GUI.skin.label);
            _mpTinyLabelStyle.fontSize = 13;
            _mpTinyLabelStyle.normal.textColor = mutedText;
            _mpTinyLabelStyle.margin = new RectOffset(0, 0, 1, 1);
            _mpTinyLabelStyle.wordWrap = true;

            _mpSectionTitleStyle = new GUIStyle(_mpLabelStyle);
            _mpSectionTitleStyle.fontStyle = FontStyle.Bold;
            _mpSectionTitleStyle.normal.textColor = darkText;

            _mpRichHeaderStyle = new GUIStyle(_mpLabelStyle);
            _mpRichHeaderStyle.fontSize = RichListingHeaderFontSize;
            _mpRichHeaderStyle.fontStyle = FontStyle.Bold;
            _mpRichHeaderStyle.wordWrap = true;
            _mpRichHeaderStyle.margin = new RectOffset(0, 0, 1, 1);

            _mpRichNameStyle = new GUIStyle(_mpTinyLabelStyle);
            _mpRichNameStyle.fontSize = RichListingNameFontSize;
            _mpRichNameStyle.wordWrap = true;
            _mpRichNameStyle.margin = new RectOffset(0, 0, 1, 1);

            _mpRichMetaStyle = new GUIStyle(_mpTinyLabelStyle);
            _mpRichMetaStyle.fontSize = RichListingMetaFontSize;
            _mpRichMetaStyle.wordWrap = true;
            _mpRichMetaStyle.margin = new RectOffset(0, 0, 1, 1);

            _mpRichVibePlusStyle = new GUIStyle(_mpRichMetaStyle);
            _mpRichVibePlusStyle.fontStyle = FontStyle.Bold;
            _mpRichVibePlusStyle.normal.textColor = RichListingVibePlusColor;
            _mpRichVibePlusStyle.hover.textColor = RichListingVibePlusColor;
            _mpRichVibePlusStyle.active.textColor = RichListingVibePlusColor;
            _mpRichVibePlusStyle.focused.textColor = RichListingVibePlusColor;

            _mpRichVibeMinusStyle = new GUIStyle(_mpRichMetaStyle);
            _mpRichVibeMinusStyle.fontStyle = FontStyle.Bold;
            _mpRichVibeMinusStyle.normal.textColor = RichListingVibeMinusColor;
            _mpRichVibeMinusStyle.hover.textColor = RichListingVibeMinusColor;
            _mpRichVibeMinusStyle.active.textColor = RichListingVibeMinusColor;
            _mpRichVibeMinusStyle.focused.textColor = RichListingVibeMinusColor;

            _mpRichStatStyle = new GUIStyle(_mpTinyLabelStyle);
            _mpRichStatStyle.fontSize = RichListingStatsFontSize;
            _mpRichStatStyle.fontStyle = FontStyle.Bold;
            _mpRichStatStyle.alignment = TextAnchor.MiddleLeft;
            _mpRichStatStyle.margin = new RectOffset(0, 0, 0, 0);
            _mpRichStatStyle.wordWrap = false;
            _mpRichStatStyle.richText = true;
            _mpRichStatStyle.clipping = TextClipping.Clip;

            _mpButtonStyle = new GUIStyle(GUI.skin.button);
            SetAllStyleStates(_mpButtonStyle, _mpButtonTex, buttonText);
            _mpButtonStyle.hover.background = _mpButtonHoverTex;
            _mpButtonStyle.onHover.background = _mpButtonHoverTex;
            _mpButtonStyle.focused.background = _mpButtonHoverTex;
            _mpButtonStyle.onFocused.background = _mpButtonHoverTex;
            _mpButtonStyle.active.background = _mpButtonActiveTex;
            _mpButtonStyle.onActive.background = _mpButtonActiveTex;
            _mpButtonStyle.fontSize = 14;
            _mpButtonStyle.alignment = TextAnchor.MiddleCenter;
            _mpButtonStyle.padding = new RectOffset(8, 8, 5, 5);
            _mpButtonStyle.margin = new RectOffset(2, 2, 2, 2);

            _mpCloseButtonStyle = new GUIStyle(_mpButtonStyle);
            _mpCloseButtonStyle.fontStyle = FontStyle.Bold;
            _mpCloseButtonStyle.padding = new RectOffset(5, 5, 2, 2);

            _mpCardStyle = new GUIStyle(GUI.skin.box);
            SetAllStyleStates(_mpCardStyle, _mpCardTex, darkText);
            _mpCardStyle.border = new RectOffset(2, 2, 2, 2);
            int richPad = Mathf.RoundToInt(Mathf.Clamp(RichListingCardInnerPadding, 0f, 24f));
            int richSpacing = Mathf.RoundToInt(Mathf.Clamp(RichListingCardSpacing, 0f, 30f));
            _mpCardStyle.padding = new RectOffset(10 + richPad, 10 + richPad, 8 + richPad, 9 + richPad);
            _mpCardStyle.margin = new RectOffset(0, 0, richSpacing, richSpacing);

            _mpTextFieldStyle = new GUIStyle(GUI.skin.textField);
            SetAllStyleStates(_mpTextFieldStyle, _mpTextFieldTex, darkText);
            _mpTextFieldStyle.border = new RectOffset(2, 2, 2, 2);
            _mpTextFieldStyle.fontSize = 14;
            _mpTextFieldStyle.padding = new RectOffset(6, 6, 4, 4);

            ApplyNativeFont(nativeGuiFont, _mpWindowStyle, _mpTitleStyle, _mpHeaderStyle, _mpLabelStyle, _mpTinyLabelStyle, _mpButtonStyle, _mpCloseButtonStyle, _mpCardStyle, _mpTextFieldStyle, _mpSectionTitleStyle, _mpRichHeaderStyle, _mpRichNameStyle, _mpRichMetaStyle, _mpRichVibePlusStyle, _mpRichVibeMinusStyle, _mpRichStatStyle);

            _gtsGuiStylesReady = true;
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

        private static Texture2D MakeSolidTex(Color color)
        {
            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, color);
            tex.Apply(false, true);
            return tex;
        }

        private static Texture2D MakeBorderedTex(Color fill, Color border, int borderPx)
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

        private static Texture2D MakeRoundedBadgeTex(Color fill, Color border, int borderPx, int radius)
        {
            const int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(0f, 0f, 0f, 0f);
            int r = Mathf.Clamp(radius, 1, size / 2);
            int b = Mathf.Clamp(borderPx, 0, 6);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool outside = false;

                    int cx = x < r ? r : (x >= size - r ? size - r - 1 : x);
                    int cy = y < r ? r : (y >= size - r ? size - r - 1 : y);
                    int dx = x - cx;
                    int dy = y - cy;
                    if ((x < r || x >= size - r || y < r || y >= size - r) && (dx * dx + dy * dy > r * r))
                        outside = true;

                    if (outside)
                    {
                        tex.SetPixel(x, y, clear);
                        continue;
                    }

                    bool isBorder = x < b || y < b || x >= size - b || y >= size - b;
                    tex.SetPixel(x, y, isBorder ? border : fill);
                }
            }

            tex.Apply(false, true);
            return tex;
        }

        private static void SetAllStyleStates(GUIStyle style, Texture2D background, Color textColor)
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

        private static void SetState(GUIStyleState state, Texture2D background, Color textColor)
        {
            state.background = background;
            state.textColor = textColor;
        }

        private void DrawWindowBacking(Rect r)
        {
            if (_mpDarkTex == null || _mpPaperTex == null) return;

            // Alpha Warning-style double frame:
            // dark outer line, cream gap, dark inner line, cream paper center.
            GUI.DrawTexture(new Rect(r.x - 5f, r.y - 5f, r.width + 10f, r.height + 10f), _mpDarkTex);
            GUI.DrawTexture(new Rect(r.x - 2f, r.y - 2f, r.width + 4f, r.height + 4f), _mpPaperTex);
            GUI.DrawTexture(new Rect(r.x + 1f, r.y + 1f, r.width - 2f, r.height - 2f), _mpDarkTex);
            GUI.DrawTexture(new Rect(r.x + 5f, r.y + 5f, r.width - 10f, r.height - 10f), _mpPaperTex);
        }

        private void DrawHorizontalRule()
        {
            Rect r = GUILayoutUtility.GetRect(1f, 5f, GUILayout.ExpandWidth(true));
            GUI.DrawTexture(new Rect(r.x, r.y + 1f, r.width, 2f), _mpDarkTex);
            GUI.DrawTexture(new Rect(r.x, r.y + 4f, r.width, 1f), _mpDarkTex);
        }

        private void ClampGtsWindowToScreen()
        {
            try
            {
                _windowRect.width = Mathf.Clamp(_windowRect.width, 420f, 1200f);
                _windowRect.height = Mathf.Clamp(_windowRect.height, 380f, 900f);
                float s = Mathf.Max(0.5f, WindowScale);
                float maxX = Mathf.Max(0f, (Screen.width / s) - _windowRect.width);
                float maxY = Mathf.Max(0f, (Screen.height / s) - _windowRect.height);
                _windowRect.x = Mathf.Clamp(_windowRect.x, 0f, maxX);
                _windowRect.y = Mathf.Clamp(_windowRect.y, 0f, maxY);
            }
            catch { }
        }

        private void ConsumeImguiEventInsideWindow()
        {
            try
            {
                Event e = Event.current;
                if (e == null || !_showWindow) return;

                float s = Mathf.Max(0.5f, WindowScale);
                Vector2 p = e.mousePosition;
                if (Math.Abs(s - 1f) > 0.001f)
                    p = new Vector2(p.x / s, p.y / s);

                if (!_windowRect.Contains(p)) return;

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

        public static bool IsMouseOverGtsWindow()
        {
            try
            {
                return Instance != null && Instance.IsMouseOverWindowInstance();
            }
            catch { return false; }
        }

        private bool IsMouseOverWindowInstance()
        {
            if (!_showWindow) return false;
            try
            {
                float s = Mathf.Max(0.5f, WindowScale);
                Vector3 mp = Input.mousePosition;
                Vector2 guiMouse = new Vector2(mp.x, Screen.height - mp.y);
                if (Math.Abs(s - 1f) > 0.001f)
                    guiMouse = new Vector2(guiMouse.x / s, guiMouse.y / s);
                return _windowRect.Contains(guiMouse);
            }
            catch { return false; }
        }

        private void DrawBoxOverlayButton()
        {
            try
            {
                if (!ShowBoxOverlayButton || _showWindow) return;
                GameScript gs = FindObjectOfType<GameScript>();
                if (gs == null || gs.menuBox == null || !gs.menuBox.activeSelf) return;

                float x = BoxOverlayX;
                float y = BoxOverlayY;
                // Keep the temporary test button on-screen even if the user's resolution differs.
                x = Mathf.Clamp(x, 0f, Mathf.Max(0f, Screen.width - 100f));
                y = Mathf.Clamp(y, 0f, Mathf.Max(0f, Screen.height - 45f));

                if (GUI.Button(new Rect(x, y, 90f, 36f), "GTS"))
                {
                    _showWindow = true;
                    _status = _loggedIn ? "Ready." : "Not logged in. Click Login with Steam.";
                }
            }
            catch (Exception ex)
            {
                if (DebugLogging) GTSNativePatcher.RuntimeWarn("GTS overlay button failed: " + ex.Message);
            }
        }

        private void DrawWindow(int id)
        {
            EnsureGtsGuiStyles();

            GUILayout.BeginVertical();

            Rect titleRect = GUILayoutUtility.GetRect(1f, 30f, GUILayout.ExpandWidth(true), GUILayout.Height(30f));
            GUI.Label(titleRect, "Trading Post", _mpTitleStyle);
            Rect closeRect = new Rect(titleRect.xMax - 30f, titleRect.y + 2f, 28f, 24f);
            if (GUI.Button(closeRect, "X", _mpCloseButtonStyle))
                _showWindow = false;

            DrawHorizontalRule();
            GUILayout.Space(4);

            GUILayout.Label("Steam/OpenID Trading Station", _mpHeaderStyle);
            GUILayout.Label("Server: " + ServerHost + ":" + ServerPort, _mpTinyLabelStyle);
            GUILayout.Label("Status: " + _status, _mpTinyLabelStyle);
            if (_loggedIn)
            {
                GUILayout.Label("Logged in: " + _username + (string.IsNullOrEmpty(_steamId64) ? "" : "  SteamID64: " + _steamId64), _mpTinyLabelStyle);
            }

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUI.enabled = !_busy;
            if (!_loggedIn)
            {
                if (AioIntegratedMode)
                {
                    GUI.enabled = false;
                    GUILayout.Button("Use Chat Connect", _mpButtonStyle, GUILayout.Height(34));
                    GUI.enabled = !_busy;
                }
                else if (GUILayout.Button("Login with Steam", _mpButtonStyle, GUILayout.Height(34)))
                    StartCoroutine(LoginWithSteamCoroutine());
            }
            else
            {
                if (GUILayout.Button("Browse", _mpButtonStyle, GUILayout.Height(30))) { _mode = "Browse"; StartCoroutine(SearchListingsCoroutine(0)); }
                if (GUILayout.Button("My Listings", _mpButtonStyle, GUILayout.Height(30))) { _mode = "Mine"; StartCoroutine(MyListingsCoroutine(0)); }
                if (GUILayout.Button("Claim Trades", _mpButtonStyle, GUILayout.Height(30))) StartCoroutine(ClaimCoroutine());
                if (!AioIntegratedMode && GUILayout.Button("Disconnect", _mpButtonStyle, GUILayout.Height(30))) Disconnect(true, true);
            }
            GUI.enabled = true;
            if (GUILayout.Button("Close", _mpButtonStyle, GUILayout.Height(30), GUILayout.Width(80))) _showWindow = false;
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            DrawSelectedMonSummary();

            if (_loggedIn)
            {
                GUILayout.Space(8);
                DrawCreateListingArea();
                GUILayout.Space(8);
                if (_mode == "Mine") DrawMyListings(); else DrawBrowseListings();
            }

            GUILayout.EndVertical();

            DrawGtsResizeHandle();
            GUI.DragWindow(new Rect(0, 0, Mathf.Max(0f, _windowRect.width - 40f), 44));
        }

        private void DrawGtsResizeHandle()
        {
            try
            {
                const float handleSize = 22f;
                Rect handle = new Rect(_windowRect.width - handleSize - 5f, _windowRect.height - handleSize - 5f, handleSize, handleSize);
                GUI.Label(handle, "◢", _mpTinyLabelStyle);

                Event e = Event.current;
                if (e == null)
                    return;

                if (e.type == EventType.MouseDown && handle.Contains(e.mousePosition))
                {
                    _resizingWindow = true;
                    e.Use();
                }
                else if (e.type == EventType.MouseDrag && _resizingWindow)
                {
                    _windowRect.width = Mathf.Clamp(e.mousePosition.x + 8f, 420f, 1200f);
                    _windowRect.height = Mathf.Clamp(e.mousePosition.y + 8f, 380f, 900f);
                    e.Use();
                }
                else if (e.type == EventType.MouseUp)
                {
                    _resizingWindow = false;
                }
            }
            catch { }
        }

        private void DrawSelectedMonSummary()
        {
            GameScript gs = FindObjectOfType<GameScript>();
            BoxManager bm = gs != null ? gs.boxManager : null;
            EnsureOfferedMonSelection(gs, bm);
            Mon mon = GetOfferedBoxMon(gs, bm, out int slot, out string whyNot);
            if (mon == null)
            {
                GUILayout.Label("Chosen Offered MoN: " + whyNot, _mpLabelStyle);
                return;
            }
            int box = slot / 27 + 1;
            GUILayout.Label("Chosen Offered MoN: Box " + box + ": " + GetMonDisplayName(mon) + " Lv." + GetLevel(mon) + (mon.isShiny ? " ★" : ""), _mpLabelStyle);
        }

        private void DrawCreateListingArea()
        {
            GUILayout.Label("Create Listing", _mpSectionTitleStyle);

            GameScript gs = FindObjectOfType<GameScript>();
            BoxManager bm = gs != null ? gs.boxManager : null;
            EnsureOfferedMonSelection(gs, bm);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Offered MoN:", _mpLabelStyle, GUILayout.Width(115));
            GUILayout.BeginHorizontal(GUILayout.Width(250));
            DrawOfferedMonSelectionField(gs, bm, 220f);
            GUI.enabled = !_busy;
            if (GUILayout.Button(_offeredMonDropdownOpen ? "▲" : "▼", _mpButtonStyle, GUILayout.Width(28), GUILayout.Height(28)))
            {
                RefreshOfferedMonOptions(gs, bm);
                _offeredMonDropdownOpen = !_offeredMonDropdownOpen;
                _requestSpeciesDropdownOpen = false;
                if (_offeredMonDropdownOpen)
                    _offeredMonDropdownScroll = Vector2.zero;
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (_offeredMonDropdownOpen)
                DrawOfferedMonDropdown(gs, bm);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Requested MoN:", _mpLabelStyle, GUILayout.Width(115));

            GUILayout.BeginHorizontal(GUILayout.Width(250));
            DrawRequestedMonSelectionField(220f);
            GUI.enabled = !_busy;
            if (GUILayout.Button(_requestSpeciesDropdownOpen ? "▲" : "▼", _mpButtonStyle, GUILayout.Width(28), GUILayout.Height(28)))
            {
                EnsureSpeciesOptionsLoaded(false);
                _requestSpeciesDropdownOpen = !_requestSpeciesDropdownOpen;
                _offeredMonDropdownOpen = false;
                if (_requestSpeciesDropdownOpen)
                    _requestSpeciesDropdownScroll = Vector2.zero;
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUI.enabled = !_busy;
            if (GUILayout.Button("List Offered MoN", _mpButtonStyle, GUILayout.Height(28)))
                StartCoroutine(CreateListingCoroutine(_requestSpecies));
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            if (_requestSpeciesDropdownOpen)
                DrawRequestSpeciesDropdown();

            GUILayout.Label("Requested MoN must exactly match the name, e.g. POPLIT.", _mpTinyLabelStyle);
        }

        private void DrawOfferedMonDropdown(GameScript gs, BoxManager bm)
        {
            RefreshOfferedMonOptions(gs, bm);

            GUILayout.BeginHorizontal();
            GUILayout.Space(115);
            GUILayout.BeginVertical(_mpCardStyle, GUILayout.Width(420));

            GUILayout.BeginHorizontal();
            GUILayout.Label("Choose Offered MoN", _mpSectionTitleStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh", _mpButtonStyle, GUILayout.Width(70), GUILayout.Height(24)))
                RefreshOfferedMonOptions(gs, bm);
            GUILayout.EndHorizontal();

            if (_offeredMonOptions.Count == 0)
            {
                GUILayout.Label("No local storage MoN found.", _mpTinyLabelStyle);
            }
            else
            {
                _offeredMonDropdownScroll = GUILayout.BeginScrollView(_offeredMonDropdownScroll, GUILayout.Height(170));
                foreach (BoxMonOption option in _offeredMonOptions)
                {
                    Mon optionMon = null;
                    if (bm != null && bm.boxMons != null && option.Slot >= 0 && option.Slot < bm.boxMons.Length)
                        optionMon = bm.boxMons[option.Slot];
                    Sprite rowIcon = null;
                    string species = optionMon != null ? GetSpecies(optionMon) : "MON";
                    if (OfferedMONShowIcon && optionMon != null)
                        rowIcon = GetBattleSpriteFromMon(optionMon, optionMon.isShiny) ?? FindSpriteOnObject(optionMon.monScriptableObject, true) ?? FindSpriteOnObject(optionMon, true);
                    if (DrawSelectorRow(option.Label, rowIcon, species, OfferedMONRowHeight, OfferedMONShowIcon, OfferedMONIconSize, OfferedMONIconOffsetX, OfferedMONIconOffsetY, OfferedMONTextOffsetX, OfferedMONTextOffsetY))
                    {
                        _offeredBoxSlot = option.Slot;
                        _offeredMonDropdownOpen = false;
                        GUI.FocusControl(null);
                    }
                }
                GUILayout.EndScrollView();
            }

            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawOfferedMonSelectionField(GameScript gs, BoxManager bm, float width)
        {
            Mon mon = GetOfferedBoxMon(gs, bm, out int slot, out string whyNot);
            string label = mon != null ? GetOfferedMonFieldText(gs, bm) : whyNot;
            Sprite icon = null;
            string fallback = mon != null ? GetSpecies(mon) : "MON";
            if (OfferedMONShowIcon && mon != null)
                icon = GetBattleSpriteFromMon(mon, mon.isShiny) ?? FindSpriteOnObject(mon.monScriptableObject, true) ?? FindSpriteOnObject(mon, true);
            DrawSelectorField(label, icon, fallback, width, OfferedMONSelectedPreviewHeight, OfferedMONShowIcon, OfferedMONIconSize, OfferedMONIconOffsetX, OfferedMONIconOffsetY, OfferedMONTextOffsetX, OfferedMONTextOffsetY);
        }

        private void DrawRequestedMonSelectionField(float width)
        {
            string label = string.IsNullOrWhiteSpace(_requestSpecies) ? "Choose Requested MoN." : _requestSpecies;
            Sprite icon = RequestedMONShowIcon && !string.IsNullOrWhiteSpace(_requestSpecies) ? FindRichMonIconSprite(_requestSpecies, null, false, false) : null;
            DrawSelectorField(label, icon, SafeNonEmpty(_requestSpecies, "MON"), width, RequestedMONSelectedPreviewHeight, RequestedMONShowIcon, RequestedMONIconSize, RequestedMONIconOffsetX, RequestedMONIconOffsetY, RequestedMONTextOffsetX, RequestedMONTextOffsetY);
        }

        private void DrawSelectorField(string text, Sprite icon, string fallbackLabel, float width, float height, bool showIcon, float iconSize, float iconOffsetX, float iconOffsetY, float textOffsetX, float textOffsetY)
        {
            Rect rect = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));
            GUI.Box(rect, GUIContent.none, _mpTextFieldStyle);
            DrawSelectorContent(rect, text, icon, fallbackLabel, showIcon, iconSize, iconOffsetX, iconOffsetY, textOffsetX, textOffsetY, false);
        }

        private bool DrawSelectorRow(string text, Sprite icon, string fallbackLabel, float rowHeight, bool showIcon, float iconSize, float iconOffsetX, float iconOffsetY, float textOffsetX, float textOffsetY)
        {
            Rect rect = GUILayoutUtility.GetRect(10f, rowHeight, GUILayout.ExpandWidth(true), GUILayout.Height(rowHeight));
            bool clicked = GUI.Button(rect, GUIContent.none, _mpButtonStyle);
            DrawSelectorContent(rect, text, icon, fallbackLabel, showIcon, iconSize, iconOffsetX, iconOffsetY, textOffsetX, textOffsetY, true);
            return clicked;
        }

        private void DrawSelectorContent(Rect rect, string text, Sprite icon, string fallbackLabel, bool showIcon, float iconSize, float iconOffsetX, float iconOffsetY, float textOffsetX, float textOffsetY, bool button)
        {
            try
            {
                if (showIcon)
                {
                    float size = Mathf.Clamp(iconSize, 12f, Mathf.Max(12f, rect.height - 4f));
                    Rect iconRect = new Rect(rect.x + 6f + iconOffsetX, rect.y + ((rect.height - size) * 0.5f) + iconOffsetY, size, size);
                    DrawSpriteOrFallback(iconRect, icon, fallbackLabel);
                }

                GUIStyle style = new GUIStyle(button ? _mpButtonStyle : _mpLabelStyle);
                style.alignment = TextAnchor.MiddleLeft;
                style.wordWrap = false;
                style.clipping = TextClipping.Clip;
                style.richText = false;
                style.padding = new RectOffset(0, 0, 0, 0);
                style.margin = new RectOffset(0, 0, 0, 0);
                style.normal.background = null;
                style.hover.background = null;
                style.active.background = null;
                style.focused.background = null;
                style.normal.textColor = _mpLabelStyle.normal.textColor;
                style.hover.textColor = _mpLabelStyle.normal.textColor;
                style.focused.textColor = _mpLabelStyle.normal.textColor;
                style.active.textColor = _mpLabelStyle.normal.textColor;

                float textX = rect.x + Mathf.Max(6f, textOffsetX);
                float textY = rect.y + textOffsetY;
                float textW = Mathf.Max(20f, rect.width - (textX - rect.x) - 6f);
                Rect textRect = new Rect(textX, textY, textW, rect.height);
                GUI.Label(textRect, SafeNonEmpty(text, ""), style);
            }
            catch { }
        }

        private void DrawSpriteOrFallback(Rect rect, Sprite sprite, string fallbackLabel)
        {
            try
            {
                if (sprite != null && sprite.texture != null)
                {
                    Texture2D tex = sprite.texture;
                    Rect tr = sprite.textureRect;
                    Rect uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
                    GUI.DrawTextureWithTexCoords(rect, tex, uv, true);
                    return;
                }
            }
            catch { }

            DrawIconFallback(rect, SafeNonEmpty(fallbackLabel, "MON"));
        }

        private string GetOfferedMonFieldText(GameScript gs, BoxManager bm)
        {
            Mon mon = GetOfferedBoxMon(gs, bm, out int slot, out string whyNot);
            if (mon == null)
                return whyNot;

            int box = slot / 27 + 1;
            return "Box " + box + ": " + GetMonDisplayName(mon) + " Lv." + GetLevel(mon) + (mon.isShiny ? " ★" : "");
        }

        private void RefreshOfferedMonOptions(GameScript gs, BoxManager bm)
        {
            _offeredMonOptions.Clear();
            try
            {
                if (gs == null || bm == null || bm.boxMons == null)
                    return;

                for (int i = 0; i < bm.boxMons.Length; i++)
                {
                    Mon mon = bm.boxMons[i];
                    if (mon == null)
                        continue;

                    int box = i / 27 + 1;
                    string label = "Box " + box + ": " + GetMonDisplayName(mon) + " Lv." + GetLevel(mon) + (mon.isShiny ? " ★" : "");
                    _offeredMonOptions.Add(new BoxMonOption { Slot = i, Label = label });
                }
            }
            catch (Exception ex)
            {
                if (DebugLogging)
                    GTSNativePatcher.RuntimeWarn("Failed to refresh offered MoN dropdown options: " + ex.Message);
            }
        }

        private void EnsureOfferedMonSelection(GameScript gs, BoxManager bm)
        {
            try
            {
                if (bm == null || bm.boxMons == null)
                {
                    _offeredBoxSlot = -1;
                    return;
                }

                if (_offeredBoxSlot >= 0 && _offeredBoxSlot < bm.boxMons.Length && bm.boxMons[_offeredBoxSlot] != null)
                    return;

                Mon selected = GetSelectedBoxMon(gs, bm, out int selectedSlot, out string ignored);
                if (selected != null && selectedSlot >= 0 && selectedSlot < bm.boxMons.Length)
                {
                    _offeredBoxSlot = selectedSlot;
                    return;
                }

                for (int i = 0; i < bm.boxMons.Length; i++)
                {
                    if (bm.boxMons[i] != null)
                    {
                        _offeredBoxSlot = i;
                        return;
                    }
                }

                _offeredBoxSlot = -1;
            }
            catch
            {
                _offeredBoxSlot = -1;
            }
        }

        private Mon GetOfferedBoxMon(GameScript gs, BoxManager bm, out int slot, out string whyNot)
        {
            slot = -1;
            whyNot = "Choose an Offered MoN.";
            if (gs == null || bm == null || bm.boxMons == null)
            {
                whyNot = "Storage is not available yet.";
                return null;
            }

            EnsureOfferedMonSelection(gs, bm);
            slot = _offeredBoxSlot;
            if (slot < 0 || slot >= bm.boxMons.Length)
            {
                whyNot = "Choose an Offered MoN.";
                return null;
            }

            Mon mon = bm.boxMons[slot];
            if (mon == null)
            {
                whyNot = "Chosen Offered MoN is no longer in storage.";
                _offeredBoxSlot = -1;
                return null;
            }

            whyNot = "";
            return mon;
        }

        private void DrawRequestSpeciesDropdown()
        {
            EnsureSpeciesOptionsLoaded(false);

            GUILayout.BeginHorizontal();
            GUILayout.Space(115);
            GUILayout.BeginVertical(_mpCardStyle, GUILayout.Width(250));

            GUILayout.BeginHorizontal();
            GUILayout.Label("Choose Requested MoN", _mpSectionTitleStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh", _mpButtonStyle, GUILayout.Width(70), GUILayout.Height(24)))
                EnsureSpeciesOptionsLoaded(true);
            GUILayout.EndHorizontal();

            if (_speciesOptions.Count == 0)
            {
                GUILayout.Label("No MoN names found yet.", _mpTinyLabelStyle);
            }
            else
            {
                _requestSpeciesDropdownScroll = GUILayout.BeginScrollView(_requestSpeciesDropdownScroll, GUILayout.Height(170));
                foreach (string speciesName in _speciesOptions)
                {
                    Sprite rowIcon = RequestedMONShowIcon ? FindRichMonIconSprite(speciesName, null, false, false) : null;
                    if (DrawSelectorRow(speciesName, rowIcon, speciesName, RequestedMONRowHeight, RequestedMONShowIcon, RequestedMONIconSize, RequestedMONIconOffsetX, RequestedMONIconOffsetY, RequestedMONTextOffsetX, RequestedMONTextOffsetY))
                    {
                        _requestSpecies = speciesName;
                        _requestSpeciesDropdownOpen = false;
                        GUI.FocusControl(null);
                    }
                }
                GUILayout.EndScrollView();
            }

            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void EnsureSpeciesOptionsLoaded(bool forceRefresh)
        {
            if (_speciesOptionsLoaded && !forceRefresh)
                return;

            _speciesOptions.Clear();
            try
            {
                HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                MonScriptableObject[] mons = Resources.FindObjectsOfTypeAll<MonScriptableObject>();
                foreach (MonScriptableObject monData in mons)
                {
                    if (monData == null)
                        continue;

                    string speciesName = monData.monName;
                    if (string.IsNullOrWhiteSpace(speciesName))
                        speciesName = monData.name;
                    if (string.IsNullOrWhiteSpace(speciesName))
                        continue;

                    speciesName = speciesName.Trim();
                    if (seen.Add(speciesName))
                        _speciesOptions.Add(speciesName);
                }

                _speciesOptions.Sort(StringComparer.OrdinalIgnoreCase);
                _speciesOptionsLoaded = true;

                if (DebugLogging)
                    GTSNativePatcher.RuntimeLog("Loaded " + _speciesOptions.Count + " GTS requested MoN option(s).");
            }
            catch (Exception ex)
            {
                _speciesOptionsLoaded = true;
                if (DebugLogging)
                    GTSNativePatcher.RuntimeWarn("Failed to load GTS requested MoN dropdown options: " + ex.Message);
            }
        }

        private void DrawBrowseListings()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Open Listings", _mpSectionTitleStyle, GUILayout.Width(110));
            GUI.enabled = !_busy && _loggedIn;
            if (GUILayout.Button("Refresh", _mpButtonStyle, GUILayout.Width(90))) StartCoroutine(SearchListingsCoroutine(_pageIndex));
            if (GUILayout.Button("Prev", _mpButtonStyle, GUILayout.Width(70))) StartCoroutine(SearchListingsCoroutine(Mathf.Max(0, _pageIndex - 1)));
            if (GUILayout.Button("Next", _mpButtonStyle, GUILayout.Width(70))) StartCoroutine(SearchListingsCoroutine(_pageIndex + 1));
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(RichListingScrollHeight));
            if (_listings.Count == 0)
            {
                GUILayout.Label("No listings loaded yet.", _mpLabelStyle);
            }
            foreach (GtsListing l in _listings)
                DrawListingCard(l, false);
            GUILayout.EndScrollView();
        }

        private void DrawMyListings()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("My Open Listings", _mpSectionTitleStyle, GUILayout.Width(130));
            GUI.enabled = !_busy && _loggedIn;
            if (GUILayout.Button("Refresh", _mpButtonStyle, GUILayout.Width(90))) StartCoroutine(MyListingsCoroutine(_pageIndex));
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(RichListingScrollHeight));
            if (_myListings.Count == 0)
            {
                GUILayout.Label("No open listings loaded yet.", _mpLabelStyle);
            }
            foreach (GtsListing l in _myListings)
                DrawListingCard(l, true);
            GUILayout.EndScrollView();
        }

        private void DrawListingCard(GtsListing l, bool mine)
        {
            BeginListingCardLayout();

            if (ShowRichListingInfo)
                DrawRichListingInfo(l, mine);
            else
                DrawLegacyListingInfo(l, mine);

            if (mine)
            {
                GUI.enabled = !_busy;
                if (DrawListingActionButton("Cancel Listing and Return MoN"))
                    StartCoroutine(CancelListingCoroutine(l));
                GUI.enabled = true;
            }
            else
            {
                bool ownListing = IsOwnListing(l);
                if (ownListing)
                {
                    GUI.enabled = false;
                    DrawListingActionButton("Your Listing");
                    GUI.enabled = true;
                }
                else
                {
                    GUI.enabled = !_busy;
                    if (DrawListingActionButton("Offer Chosen MoN for this Listing"))
                        StartCoroutine(OfferSelectedMonCoroutine(l));
                    GUI.enabled = true;
                }
            }

            EndListingCardLayout();
        }

        private float GetEffectiveListingCardWidth()
        {
            try
            {
                if (RichListingCardWidth > 0.01f)
                    return RichListingCardWidth;

                float width = RichListingCardAutoBaseWidth + RichListingCardAutoExtraWidth;

                // Auto-fit mode: let the requested icon's X offset move the card edge too.
                // Example: RequestedIconOffsetX = -15 makes the card 15px narrower.
                if (RichListingCardAutoFollowRequestedIconOffsetX)
                    width += RichListingRequestedIconOffsetX;

                // GUILayout cannot truly move a row left with a negative Space(), so in
                // auto mode we treat a negative card offset as "also shrink the card".
                if (RichListingCardOffsetX < 0f)
                    width += RichListingCardOffsetX;

                return Mathf.Clamp(width, 320f, 1200f);
            }
            catch
            {
                return Mathf.Clamp(RichListingCardAutoBaseWidth, 320f, 1200f);
            }
        }

        private void BeginListingCardLayout()
        {
            float width = GetEffectiveListingCardWidth();
            float offsetX = RichListingCardOffsetX;
            string align = string.IsNullOrWhiteSpace(RichListingCardAlign) ? "Left" : RichListingCardAlign.Trim();

            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            if (offsetX > 0f)
                GUILayout.Space(offsetX);

            if (align.Equals("Center", StringComparison.OrdinalIgnoreCase))
                GUILayout.FlexibleSpace();
            else if (align.Equals("Right", StringComparison.OrdinalIgnoreCase))
                GUILayout.FlexibleSpace();

            // Hard cap the card width. v1.1.3 only passed Width(), which IMGUI could
            // still stretch if child controls asked to expand. Min/Max/Expand false
            // makes the card actually follow the configured width.
            GUILayout.BeginVertical(
                _mpCardStyle,
                GUILayout.Width(width),
                GUILayout.MinWidth(width),
                GUILayout.MaxWidth(width),
                GUILayout.ExpandWidth(false)
            );
        }

        private void EndListingCardLayout()
        {
            string align = string.IsNullOrWhiteSpace(RichListingCardAlign) ? "Left" : RichListingCardAlign.Trim();

            GUILayout.EndVertical();

            if (align.Equals("Left", StringComparison.OrdinalIgnoreCase))
                GUILayout.FlexibleSpace();
            else if (align.Equals("Center", StringComparison.OrdinalIgnoreCase))
                GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();
        }

        private bool DrawListingActionButton(string label)
        {
            float width = Mathf.Max(0f, RichListingButtonWidth);
            float offsetX = RichListingButtonOffsetX;
            string align = string.IsNullOrWhiteSpace(RichListingButtonAlign) ? "Center" : RichListingButtonAlign.Trim();

            GUILayout.BeginHorizontal();
            if (offsetX > 0f)
                GUILayout.Space(offsetX);

            bool stretch = width <= 0.01f;
            if (!stretch)
            {
                if (align.Equals("Center", StringComparison.OrdinalIgnoreCase))
                    GUILayout.FlexibleSpace();
                else if (align.Equals("Right", StringComparison.OrdinalIgnoreCase))
                    GUILayout.FlexibleSpace();

                bool clicked = GUILayout.Button(label, _mpButtonStyle, GUILayout.Width(width), GUILayout.Height(RichListingButtonHeight));

                if (align.Equals("Center", StringComparison.OrdinalIgnoreCase))
                    GUILayout.FlexibleSpace();
                else if (align.Equals("Left", StringComparison.OrdinalIgnoreCase))
                    GUILayout.FlexibleSpace();

                GUILayout.EndHorizontal();
                return clicked;
            }
            else
            {
                bool clicked = GUILayout.Button(label, _mpButtonStyle, GUILayout.Height(RichListingButtonHeight));
                GUILayout.EndHorizontal();
                return clicked;
            }
        }

        private void DrawLegacyListingInfo(GtsListing l, bool mine)
        {
            if (mine)
            {
                GUILayout.Label("Your listing: " + l.OfferedSpecies + " Lv." + l.Level + (l.Shiny ? " ★" : "") + "  wants " + l.RequestSpecies, _mpLabelStyle);
                GUILayout.Label("MoN name: " + l.DisplayName + "   " + FormatListedOn(l), _mpTinyLabelStyle);
            }
            else
            {
                GUILayout.Label(DisplayOwner(l) + " offers " + l.OfferedSpecies + " Lv." + l.Level + (l.Shiny ? " ★" : "") + "  wants " + l.RequestSpecies, _mpLabelStyle);
                GUILayout.Label("MoN name: " + l.DisplayName + "   " + FormatListedOn(l), _mpTinyLabelStyle);
            }
        }

        private void DrawRichListingInfo(GtsListing l, bool mine)
        {
            RichMonMetadata meta = GetRichListingMetadata(l);
            GUILayout.BeginHorizontal();

            if (RichListingUseMonIcon)
                DrawRichListingIcon(l, meta, true);

            GUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.MinWidth(200f));
            SpaceMaybe(RichListingHeaderOffsetY);
            string ownerLabel = mine ? "You" : DisplayOwner(l);
            DrawRichListingHeader(ownerLabel, meta, l);

            SpaceMaybe(RichListingNameOffsetY);
            GUILayout.BeginHorizontal();
            SpaceMaybe(RichListingNameOffsetX);
            string nameLine = "MoN name: " + meta.DisplayName + "   " + FormatListedOn(l);
            GUILayout.Label(nameLine, _mpRichNameStyle);
            GUILayout.EndHorizontal();

            SpaceMaybe(RichListingMetaOffsetY);
            GUILayout.BeginHorizontal();
            SpaceMaybe(RichListingMetaOffsetX);
            DrawVibeMetaLine(meta);
            GUILayout.EndHorizontal();

            SpaceMaybe(RichListingStatsOffsetY);
            if (RichListingStatsSingleLine)
            {
                GUILayout.BeginHorizontal();
                SpaceMaybe(RichListingStatsOffsetX);
                DrawStatsSingleLine(meta);
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.BeginHorizontal();
                SpaceMaybe(RichListingStatsOffsetX);
                DrawStatCell("HP", meta.HpGrade, meta);
                DrawStatCell("ATK", meta.AtkGrade, meta);
                DrawStatCell("MAG", meta.MagGrade, meta);
                GUILayout.EndHorizontal();
                SpaceMaybe(RichListingStatsRowSpacing);
                GUILayout.BeginHorizontal();
                SpaceMaybe(RichListingStatsOffsetX);
                DrawStatCell("DEF", meta.DefGrade, meta);
                DrawStatCell("RES", meta.MdfGrade, meta);
                DrawStatCell("SPD", meta.SpdGrade, meta);
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();

            if (RichListingShowRequestedMonIcon)
            {
                DrawRequestedListingIcon(l);
            }

            GUILayout.EndHorizontal();
        }

        private void DrawRichListingHeader(string ownerLabel, RichMonMetadata meta, GtsListing listing)
        {
            try
            {
                string owner = SafeNonEmpty(ownerLabel, "Player");
                string offered = meta != null ? SafeNonEmpty(meta.Species, listing != null ? listing.OfferedSpecies : "MoN") : "MoN";
                string wanted = listing != null ? SafeNonEmpty(listing.RequestSpecies, "MoN") : "MoN";

                GUILayout.BeginHorizontal();
                SpaceMaybe(RichListingHeaderOffsetX);
                GUILayout.Label(owner + " offers " + offered, _mpRichHeaderStyle, GUILayout.ExpandWidth(false));
                if (meta != null && meta.Shiny)
                    DrawShinyHeaderIcon();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                SpaceMaybe(RichListingHeaderOffsetX);
                GUILayout.Label(owner + " wants " + wanted, _mpRichHeaderStyle);
                GUILayout.EndHorizontal();
            }
            catch
            {
                GUILayout.Label("Trading Post listing", _mpRichHeaderStyle);
            }
        }

        private void DrawShinyHeaderIcon()
        {
            int size = Mathf.Clamp(RichListingShinyMarkerFontSize, 10, 30);
            float ox = RichListingShinyMarkerOffsetX;
            float oy = RichListingShinyMarkerOffsetY;

            if (RichListingShinyMarkerUseChatIcon)
            {
                try
                {
                    string spriteName = string.IsNullOrWhiteSpace(RichListingShinyMarkerSpriteName) ? "sparklyIcon" : RichListingShinyMarkerSpriteName.Trim();
                    Sprite sprite = FindSpriteByName(spriteName);
                    if (sprite != null && sprite.texture != null)
                    {
                        Rect layoutRect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
                        Rect drawRect = new Rect(layoutRect.x + ox, layoutRect.y + oy, layoutRect.width, layoutRect.height);
                        DrawSpriteOrFallback(drawRect, sprite, "★");
                        return;
                    }
                }
                catch { }
            }

            string marker = string.IsNullOrWhiteSpace(RichListingShinyMarkerText) ? "✦" : RichListingShinyMarkerText.Trim();
            GUIContent content = new GUIContent(marker);

            GUIStyle style = new GUIStyle(_mpRichHeaderStyle ?? GUI.skin.label);
            style.fontSize = size;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;
            style.wordWrap = false;
            style.clipping = TextClipping.Overflow;
            style.normal.textColor = ColorFromHex(RichListingShinyMarkerColorHex, new Color32(214, 178, 74, 255));
            style.hover.textColor = style.normal.textColor;
            style.active.textColor = style.normal.textColor;
            style.focused.textColor = style.normal.textColor;

            Vector2 labelSize = style.CalcSize(content);
            Rect layoutLabelRect = GUILayoutUtility.GetRect(labelSize.x + 4f, Mathf.Max(labelSize.y + 2f, size + 4f), GUILayout.Width(labelSize.x + 4f), GUILayout.Height(Mathf.Max(labelSize.y + 2f, size + 4f)));
            Rect drawLabelRect = new Rect(layoutLabelRect.x + ox, layoutLabelRect.y + oy, layoutLabelRect.width, layoutLabelRect.height);
            GUI.Label(drawLabelRect, content, style);
        }

        private static Color ColorFromHex(string value, Color fallback)
        {
            try
            {
                string hex = NormalizeHex6(value, "");
                if (string.IsNullOrEmpty(hex))
                    return fallback;

                byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                return new Color32(r, g, b, 255);
            }
            catch { return fallback; }
        }

        private static string NormalizeHex6(string value, string fallback)
        {
            try
            {
                string hex = (value ?? "").Trim();
                if (hex.StartsWith("#", StringComparison.Ordinal))
                    hex = hex.Substring(1);
                if (hex.Length == 6)
                {
                    for (int i = 0; i < hex.Length; i++)
                    {
                        char c = hex[i];
                        bool ok = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
                        if (!ok)
                            return fallback;
                    }
                    return hex.ToUpperInvariant();
                }
            }
            catch { }
            return fallback;
        }

        private static string FormatListedOn(GtsListing listing)
        {
            try
            {
                string raw = listing != null ? listing.CreatedAtRaw : null;
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    DateTime dt;
                    if (DateTime.TryParse(raw, out dt))
                        return "Listed On: " + dt.ToString("MMMM d, yyyy");
                }
            }
            catch { }
            return "Listed On: Unknown";
        }

        private static void SpaceMaybe(float px)
        {
            if (Math.Abs(px) > 0.01f)
                GUILayout.Space(px);
        }

        private void DrawVibeMetaLine(RichMonMetadata meta)
        {
            if (meta == null)
            {
                DrawInlineMetaText("Level: ?   Gender: ");
                DrawGenderBadge("?");
                DrawInlineMetaText("   Vibe: ?");
                return;
            }

            DrawInlineMetaText("Level: " + meta.Level + "   Gender: ");
            DrawGenderBadge(meta.GenderSymbol);
            DrawInlineMetaText("   Vibe: " + meta.Vibe);
        }

        private void DrawInlineMetaText(string text)
        {
            GUIContent content = new GUIContent(text ?? "");
            Vector2 size = (_mpRichMetaStyle ?? GUI.skin.label).CalcSize(content);
            Rect rect = GUILayoutUtility.GetRect(size.x + 2f, Mathf.Max(16f, size.y + 2f), GUILayout.Width(size.x + 2f), GUILayout.Height(Mathf.Max(16f, size.y + 2f)));
            GUI.Label(rect, content, _mpRichMetaStyle ?? GUI.skin.label);
        }

        private void DrawGenderBadge(string symbol)
        {
            string s = string.IsNullOrWhiteSpace(symbol) ? "?" : symbol.Trim();
            float w = 18f;
            float h = 16f;
            Rect rect = GUILayoutUtility.GetRect(w, h, GUILayout.Width(w), GUILayout.Height(h));

            Texture2D tex = _mpGenderUnknownTex;
            if (s.Contains("♂"))
                tex = _mpGenderMaleTex;
            else if (s.Contains("♀"))
                tex = _mpGenderFemaleTex;

            if (tex != null)
                GUI.DrawTexture(rect, tex, ScaleMode.StretchToFill, true);

            GUIStyle style = new GUIStyle(_mpRichMetaStyle ?? GUI.skin.label);
            style.alignment = TextAnchor.MiddleCenter;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = new Color32(53, 34, 34, 255);
            style.hover.textColor = style.normal.textColor;
            style.active.textColor = style.normal.textColor;
            style.focused.textColor = style.normal.textColor;
            GUI.Label(rect, s, style);
        }

        private void DrawStatsSingleLine(RichMonMetadata meta)
        {
            try
            {
                string text =
                    BuildStatCellText("HP", string.IsNullOrEmpty(meta != null ? meta.HpGrade : null) ? "?" : meta.HpGrade, meta) + "     " +
                    BuildStatCellText("ATK", string.IsNullOrEmpty(meta != null ? meta.AtkGrade : null) ? "?" : meta.AtkGrade, meta) + "     " +
                    BuildStatCellText("MAG", string.IsNullOrEmpty(meta != null ? meta.MagGrade : null) ? "?" : meta.MagGrade, meta) + "     " +
                    BuildStatCellText("DEF", string.IsNullOrEmpty(meta != null ? meta.DefGrade : null) ? "?" : meta.DefGrade, meta) + "     " +
                    BuildStatCellText("RES", string.IsNullOrEmpty(meta != null ? meta.MdfGrade : null) ? "?" : meta.MdfGrade, meta) + "     " +
                    BuildStatCellText("SPD", string.IsNullOrEmpty(meta != null ? meta.SpdGrade : null) ? "?" : meta.SpdGrade, meta);

                GUILayout.Label(text, _mpRichStatStyle, GUILayout.ExpandWidth(true));
            }
            catch
            {
                GUILayout.Label("HP: ?     ATK: ?     MAG: ?     DEF: ?     RES: ?     SPD: ?", _mpRichStatStyle, GUILayout.ExpandWidth(true));
            }
        }

        private void DrawStatCell(string label, string grade, RichMonMetadata meta)
        {
            string displayGrade = string.IsNullOrEmpty(grade) ? "?" : grade;
            string text = BuildStatCellText(label, displayGrade, meta);
            GUILayout.Label(text, _mpRichStatStyle, GUILayout.Width(RichListingStatCellWidth), GUILayout.Height(RichListingStatsFontSize + 4));
        }

        private static string BuildStatCellText(string label, string grade, RichMonMetadata meta)
        {
            string statLabel = label + ":";
            if (meta != null)
            {
                if (StatLabelMatches(label, meta.VibePlusStat))
                    statLabel = "<color=#" + RichListingVibePlusColorHex + ">+" + statLabel + "</color>";
                else if (StatLabelMatches(label, meta.VibeMinusStat))
                    statLabel = "<color=#" + RichListingVibeMinusColorHex + ">-" + statLabel + "</color>";
            }
            return statLabel + " " + grade;
        }

        private GUIStyle GetStatLabelStyleForVibe(string label, RichMonMetadata meta)
        {
            if (meta != null)
            {
                if (StatLabelMatches(label, meta.VibePlusStat))
                    return _mpRichVibePlusStyle ?? _mpRichStatStyle;
                if (StatLabelMatches(label, meta.VibeMinusStat))
                    return _mpRichVibeMinusStyle ?? _mpRichStatStyle;
            }
            return _mpRichStatStyle;
        }

        private static bool StatLabelMatches(string label, string vibeStat)
        {
            string a = NormalizeStatNameForCompare(label);
            string b = NormalizeStatNameForCompare(vibeStat);
            return !string.IsNullOrEmpty(a) && a == b;
        }

        private static string NormalizeStatNameForCompare(string value)
        {
            string s = (value ?? "").Trim().ToUpperInvariant();
            if (s == "MDF") return "RES";
            if (s == "SPDEF" || s == "SPDEFENSE" || s == "SPECIALDEFENSE") return "RES";
            if (s == "ATTACK") return "ATK";
            if (s == "MAGIC") return "MAG";
            if (s == "DEFENSE") return "DEF";
            if (s == "SPEED") return "SPD";
            return s;
        }

        private RichMonMetadata GetRichListingMetadata(GtsListing listing)
        {
            if (listing == null)
                return new RichMonMetadata();
            if (listing.RichMetadataParsed && string.Equals(listing.RichMetadataBlob, listing.BlobB64, StringComparison.Ordinal))
                return listing.RichMetadata ?? BuildFallbackMetadata(listing);

            RichMonMetadata meta = BuildFallbackMetadata(listing);
            listing.RichMetadataParsed = true;
            listing.RichMetadataBlob = listing.BlobB64;
            listing.RichMetadata = meta;

            try
            {
                if (string.IsNullOrEmpty(listing.BlobB64))
                    return meta;
                GameScript gs = FindObjectOfType<GameScript>();
                if (gs == null)
                    return meta;
                Mon mon = DecodeMonBlob(gs, listing.BlobB64);
                if (mon == null)
                    return meta;

                meta.Species = SafeNonEmpty(GetSpecies(mon), listing.OfferedSpecies);
                meta.DisplayName = SafeNonEmpty(GetMonDisplayName(mon), listing.DisplayName);
                meta.Level = GetLevel(mon);
                meta.Shiny = mon.isShiny;
                meta.GenderSymbol = GenderSymbolFromInt(SafeGetIntValue(mon, new[] { "gender", "Gender" }, listing.Gender));
                int vibeIndex = GetVibeIndex(mon);
                meta.VibeIndex = vibeIndex;
                meta.Vibe = ResolveVibeDisplayName(vibeIndex, GetVibeText(mon));
                ApplyVibePlusMinus(meta, vibeIndex);

                meta.HpGrade = GetStatGradeFromArrayOrFallback(mon, 0, "HP", new[] { "hpGrade", "HPGrade", "gradeHP", "hpIV", "hpIv", "ivHP", "HPIV", "hpPotential", "potentialHP", "hpGene", "geneHP" });
                meta.AtkGrade = GetStatGradeFromArrayOrFallback(mon, 1, "ATK", new[] { "attackGrade", "atkGrade", "ATKGrade", "gradeAttack", "gradeAtk", "attackIV", "atkIV", "ivAttack", "ivAtk", "ATKIV", "attackPotential", "atkPotential", "potentialAttack", "potentialAtk", "attackGene", "atkGene" });
                meta.MagGrade = GetStatGradeFromArrayOrFallback(mon, 2, "MAG", new[] { "magicGrade", "magGrade", "MAGGrade", "spAttackGrade", "spAtkGrade", "specialAttackGrade", "gradeMagic", "gradeMag", "magicIV", "magIV", "spAttackIV", "spAtkIV", "specialAttackIV", "ivMagic", "ivMag", "ivSpAttack", "ivSpAtk", "MAGIV", "magicPotential", "magPotential", "potentialMagic", "potentialMag" });
                meta.DefGrade = GetStatGradeFromArrayOrFallback(mon, 3, "DEF", new[] { "defenseGrade", "defGrade", "DEFGrade", "gradeDefense", "gradeDef", "defenseIV", "defIV", "ivDefense", "ivDef", "DEFIV", "defensePotential", "defPotential", "potentialDefense", "potentialDef", "defenseGene", "defGene" });
                meta.MdfGrade = GetStatGradeFromArrayOrFallback(mon, 4, "MDF", new[] { "resGrade", "RESGrade", "resistanceGrade", "magicDefenseGrade", "magDefenseGrade", "mdfGrade", "MDFGrade", "spDefenseGrade", "spDefGrade", "specialDefenseGrade", "gradeMagicDefense", "gradeMdf", "resIV", "RESIV", "magicDefenseIV", "magDefenseIV", "mdfIV", "spDefenseIV", "spDefIV", "specialDefenseIV", "ivMagicDefense", "ivMdf", "ivSpDefense", "ivSpDef", "MDFIV", "resPotential", "magicDefensePotential", "mdfPotential", "potentialMagicDefense", "potentialMdf" });
                meta.SpdGrade = GetStatGradeFromArrayOrFallback(mon, 5, "SPD", new[] { "speedGrade", "spdGrade", "SPDGrade", "gradeSpeed", "gradeSpd", "speedIV", "spdIV", "ivSpeed", "ivSpd", "SPDIV", "speedPotential", "spdPotential", "potentialSpeed", "potentialSpd", "speedGene", "spdGene" });

                if (RichListingMetadataDebugLogging && !listing.RichMetadataDebugLogged)
                {
                    listing.RichMetadataDebugLogged = true;
                    DebugLogInterestingMonFields(mon, meta.Species, listing.Id);
                }
            }
            catch (Exception ex)
            {
                if (RichListingMetadataDebugLogging || DebugLogging)
                    GTSNativePatcher.RuntimeWarn("Rich listing metadata parse failed for listing #" + listing.Id + ": " + ex.Message);
            }
            return meta;
        }

        private static RichMonMetadata BuildFallbackMetadata(GtsListing listing)
        {
            RichMonMetadata meta = new RichMonMetadata();
            if (listing == null)
                return meta;
            meta.Species = SafeNonEmpty(listing.OfferedSpecies, "UNKNOWN");
            meta.DisplayName = SafeNonEmpty(listing.DisplayName, meta.Species);
            meta.Level = listing.Level > 0 ? listing.Level : 1;
            meta.GenderSymbol = GenderSymbolFromInt(listing.Gender);
            meta.Shiny = listing.Shiny;
            meta.Vibe = "?";
            meta.VibeIndex = -1;
            meta.VibePlusStat = "?";
            meta.VibeMinusStat = "?";
            meta.HpGrade = meta.AtkGrade = meta.DefGrade = meta.MagGrade = meta.MdfGrade = meta.SpdGrade = "?";
            return meta;
        }

        private void DrawRichListingIcon(GtsListing listing, RichMonMetadata meta, bool offeredIcon)
        {
            float size = Mathf.Clamp(offeredIcon ? RichListingIconSize : RichListingRequestedIconSize, 24f, 160f);
            float offsetX = offeredIcon ? RichListingIconOffsetX : RichListingRequestedIconOffsetX;
            float offsetY = offeredIcon ? RichListingIconOffsetY : RichListingRequestedIconOffsetY;
            float columnWidth = offeredIcon
                ? size + Math.Abs(offsetX) + 8f
                : Mathf.Max(RichListingRequestedIconColumnWidth, size + Math.Abs(offsetX) + 8f);
            float height = size + Math.Abs(offsetY) + 4f;
            GUILayout.BeginVertical(GUILayout.Width(columnWidth), GUILayout.Height(height));
            Rect baseRect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
            Rect iconRect = new Rect(baseRect.x + offsetX, baseRect.y + offsetY, size, size);

            string species = offeredIcon
                ? (meta != null ? meta.Species : listing != null ? listing.OfferedSpecies : null)
                : (listing != null ? listing.RequestSpecies : null);
            bool shiny = offeredIcon && meta != null && meta.Shiny;
            Sprite sprite = FindRichMonIconSprite(species, listing, shiny, offeredIcon);
            if (sprite != null && sprite.texture != null)
            {
                try
                {
                    Texture2D tex = sprite.texture;
                    Rect tr = sprite.textureRect;
                    Rect uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
                    GUI.DrawTextureWithTexCoords(iconRect, tex, uv, true);
                }
                catch
                {
                    DrawIconFallback(iconRect, SafeNonEmpty(species, "MON"));
                }
            }
            else
            {
                DrawIconFallback(iconRect, SafeNonEmpty(species, "MON"));
            }
            GUILayout.EndVertical();
        }

        private void DrawRequestedListingIcon(GtsListing listing)
        {
            // Requested MoN art is intentionally normal/non-shiny and metadata-only.
            DrawRichListingIcon(listing, null, false);
        }

        private void DrawIconFallback(Rect rect, string species)
        {
            try
            {
                GUI.DrawTexture(rect, _mpTextFieldTex != null ? _mpTextFieldTex : Texture2D.whiteTexture);
                string label = SafeNonEmpty(species, "MON");
                if (label.Length > 3) label = label.Substring(0, 3).ToUpperInvariant();
                GUI.Label(rect, label, _mpRichHeaderStyle ?? _mpLabelStyle);
            }
            catch { }
        }

        private Sprite FindRichMonIconSprite(string species, GtsListing listing, bool shiny, bool allowBlobMon)
        {
            species = SafeNonEmpty(species, listing != null ? listing.OfferedSpecies : "");
            if (string.IsNullOrEmpty(species))
                return null;

            string cacheKey = NormalizeKey(species) + "|" + (shiny ? "shiny" : "normal") + "|" + (allowBlobMon ? "blob" : "species");
            if (_richIconCache.TryGetValue(cacheKey, out Sprite cached))
                return cached;

            Sprite result = null;
            try
            {
                if (allowBlobMon && listing != null && !string.IsNullOrEmpty(listing.BlobB64))
                {
                    GameScript gs = FindObjectOfType<GameScript>();
                    if (gs != null)
                    {
                        Mon mon = DecodeMonBlob(gs, listing.BlobB64);
                        if (mon != null)
                        {
                            result = GetBattleSpriteFromMon(mon, shiny);
                            if (result == null)
                                result = FindSpriteOnObject(mon.monScriptableObject, true) ?? FindSpriteOnObject(mon, true);
                        }
                    }
                }
            }
            catch { }

            if (result == null)
                result = FindSpriteForSpeciesNormalOrShiny(species, shiny);

            if (result == null)
                result = FindSpriteResourceForSpecies(species, shiny);

            _richIconCache[cacheKey] = result;
            return result;
        }

        private static Sprite GetBattleSpriteFromMon(Mon mon, bool shiny)
        {
            try
            {
                if (mon == null || mon.monScriptableObject == null)
                    return null;
                Sprite[] frames = shiny ? mon.monScriptableObject.spritesShiny : mon.monScriptableObject.sprites;
                if ((frames == null || frames.Length == 0) && shiny)
                    frames = mon.monScriptableObject.sprites;
                return FirstUsableSprite(frames);
            }
            catch { return null; }
        }

        private static Sprite FindSpriteForSpeciesNormalOrShiny(string species, bool shiny)
        {
            try
            {
                GameScript gs = FindObjectOfType<GameScript>();
                if (gs == null || gs.monScriptableObject == null)
                    return null;
                string key = NormalizeKey(species);
                if (string.IsNullOrEmpty(key))
                    return null;
                for (int i = 0; i < gs.monScriptableObject.Length; i++)
                {
                    MonScriptableObject mso = gs.monScriptableObject[i];
                    if (mso == null) continue;
                    string monName = "";
                    try { monName = mso.monName; } catch { monName = ""; }
                    if (NormalizeKey(monName) != key)
                        continue;
                    Sprite[] frames = shiny ? mso.spritesShiny : mso.sprites;
                    if ((frames == null || frames.Length == 0) && shiny)
                        frames = mso.sprites;
                    Sprite sprite = FirstUsableSprite(frames);
                    if (sprite != null)
                        return sprite;
                    return FindSpriteOnObject(mso, true);
                }
            }
            catch { }
            return null;
        }

        private static Sprite FirstUsableSprite(Sprite[] frames)
        {
            if (frames == null) return null;
            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i] != null)
                    return frames[i];
            }
            return null;
        }

        private static Sprite FindSpriteOnObject(object obj, bool preferIconNamed)
        {
            if (obj == null) return null;
            try
            {
                Type t = obj.GetType();
                Sprite fallback = null;
                foreach (FieldInfo f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (typeof(Sprite).IsAssignableFrom(f.FieldType))
                    {
                        Sprite sprite = f.GetValue(obj) as Sprite;
                        if (sprite == null) continue;
                        string n = f.Name.ToLowerInvariant();
                        if (!preferIconNamed || n.Contains("icon") || n.Contains("menu") || n.Contains("box") || n.Contains("small")) return sprite;
                        if (fallback == null) fallback = sprite;
                    }
                    else if (typeof(Sprite[]).IsAssignableFrom(f.FieldType))
                    {
                        Sprite[] arr = f.GetValue(obj) as Sprite[];
                        if (arr == null || arr.Length == 0) continue;
                        string n = f.Name.ToLowerInvariant();
                        Sprite sprite = null;
                        for (int i = 0; i < arr.Length; i++) { if (arr[i] != null) { sprite = arr[i]; break; } }
                        if (sprite == null) continue;
                        if (!preferIconNamed || n.Contains("icon") || n.Contains("menu") || n.Contains("box") || n.Contains("small") || n.Contains("sprite")) return sprite;
                        if (fallback == null) fallback = sprite;
                    }
                }
                foreach (PropertyInfo prop in t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (!prop.CanRead || prop.GetIndexParameters().Length != 0) continue;
                    if (typeof(Sprite).IsAssignableFrom(prop.PropertyType))
                    {
                        Sprite sprite = null;
                        try { sprite = prop.GetValue(obj, null) as Sprite; } catch { }
                        if (sprite == null) continue;
                        string n = prop.Name.ToLowerInvariant();
                        if (!preferIconNamed || n.Contains("icon") || n.Contains("menu") || n.Contains("box") || n.Contains("small")) return sprite;
                        if (fallback == null) fallback = sprite;
                    }
                    else if (typeof(Sprite[]).IsAssignableFrom(prop.PropertyType))
                    {
                        Sprite[] arr = null;
                        try { arr = prop.GetValue(obj, null) as Sprite[]; } catch { }
                        if (arr == null || arr.Length == 0) continue;
                        string n = prop.Name.ToLowerInvariant();
                        Sprite sprite = null;
                        for (int i = 0; i < arr.Length; i++) { if (arr[i] != null) { sprite = arr[i]; break; } }
                        if (sprite == null) continue;
                        if (!preferIconNamed || n.Contains("icon") || n.Contains("menu") || n.Contains("box") || n.Contains("small") || n.Contains("sprite")) return sprite;
                        if (fallback == null) fallback = sprite;
                    }
                }
                return fallback;
            }
            catch { return null; }
        }

        private static Sprite FindSpriteResourceForSpecies(string species, bool shiny)
        {
            try
            {
                string key = NormalizeKey(species);
                if (string.IsNullOrEmpty(key)) return null;
                Sprite loose = null;
                Sprite[] sprites = Resources.FindObjectsOfTypeAll<Sprite>();
                foreach (Sprite sprite in sprites)
                {
                    if (sprite == null || string.IsNullOrEmpty(sprite.name)) continue;
                    string n = NormalizeKey(sprite.name);
                    if (!n.Contains(key)) continue;
                    bool wantsShiny = shiny;
                    bool spriteLooksShiny = n.Contains("shiny") || n.Contains("shine") || n.Contains("alt");
                    if (wantsShiny && !spriteLooksShiny)
                    {
                        if (loose == null) loose = sprite;
                        continue;
                    }
                    if (!wantsShiny && spriteLooksShiny)
                        continue;
                    bool iconish = n.Contains("icon") || n.Contains("box") || n.Contains("menu") || n.Contains("small") || n.Contains("party");
                    if (iconish) return sprite;
                    if (loose == null) loose = sprite;
                }
                return loose;
            }
            catch { return null; }
        }

        private static Color ParseRichListingHexColor(string value, Color fallback)
        {
            try
            {
                string s = (value ?? "").Trim();
                if (s.StartsWith("#")) s = s.Substring(1);
                if (s.Length == 8) s = s.Substring(0, 6);
                if (s.Length != 6) return fallback;
                byte r = byte.Parse(s.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                byte g = byte.Parse(s.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                byte b = byte.Parse(s.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                return new Color32(r, g, b, 255);
            }
            catch { return fallback; }
        }

        private static int GetVibeIndex(Mon mon)
        {
            MemberValue mv = GetFirstMemberValueWithName(mon, new[] { "vibe", "Vibe", "vibeIndex", "VibeIndex", "nature", "Nature", "natureIndex", "NatureIndex", "personality", "Personality", "mood", "Mood", "temperament", "Temperament" });
            if (!mv.Found || mv.Value == null) return -1;
            int index;
            if (TryConvertToInt(mv.Value, out index)) return index;
            string raw = ValueToDisplayString(mv.Value);
            if (int.TryParse(raw, out index)) return index;
            return -1;
        }

        private static string ResolveVibeDisplayName(int vibeIndex, string fallbackText)
        {
            try
            {
                if (vibeIndex >= 0)
                {
                    // First allow manual config overrides if Goose wants to rename one later.
                    if (vibeIndex < RichListingVibeIndexNames.Length)
                    {
                        string manual = RichListingVibeIndexNames[vibeIndex];
                        if (!string.IsNullOrWhiteSpace(manual))
                            return manual.Trim();
                    }

                    string key = "VIBE" + vibeIndex.ToString();
                    string localized = GameScript.GetLocalizedTextFromKey(key, GameScript.LocTable.Game_Text_B);
                    if (!string.IsNullOrWhiteSpace(localized)
                        && !localized.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        return localized.Trim();
                    }
                }
            }
            catch { }

            string fb = SafeNonEmpty(fallbackText, "?");
            if (vibeIndex >= 0 && fb == vibeIndex.ToString())
                return "?";
            return fb;
        }

        private static void ApplyVibePlusMinus(RichMonMetadata meta, int vibeIndex)
        {
            if (meta == null) return;
            int plus;
            int minus;
            if (TryGetVibePlusMinus(vibeIndex, out plus, out minus))
            {
                meta.VibePlusStat = VibeStatName(plus);
                meta.VibeMinusStat = VibeStatName(minus);
            }
            else
            {
                meta.VibePlusStat = "?";
                meta.VibeMinusStat = "?";
            }
        }

        private static bool TryGetVibePlusMinus(int vibe, out int plus, out int minus)
        {
            plus = -1;
            minus = -1;
            switch (vibe)
            {
                case 0: plus = 0; minus = 1; return true;
                case 1: plus = 0; minus = 2; return true;
                case 2: plus = 0; minus = 3; return true;
                case 3: plus = 0; minus = 4; return true;
                case 4: plus = 1; minus = 0; return true;
                case 5: plus = 1; minus = 2; return true;
                case 6: plus = 1; minus = 3; return true;
                case 7: plus = 1; minus = 4; return true;
                case 8: plus = 2; minus = 0; return true;
                case 9: plus = 2; minus = 1; return true;
                case 10: plus = 2; minus = 3; return true;
                case 11: plus = 2; minus = 4; return true;
                case 12: plus = 3; minus = 0; return true;
                case 13: plus = 3; minus = 1; return true;
                case 14: plus = 3; minus = 2; return true;
                case 15: plus = 3; minus = 4; return true;
                case 16: plus = 4; minus = 0; return true;
                case 17: plus = 4; minus = 1; return true;
                case 18: plus = 4; minus = 2; return true;
                case 19: plus = 4; minus = 3; return true;
                default: return false;
            }
        }

        private static string VibeStatName(int statIndex)
        {
            switch (statIndex)
            {
                case 0: return "ATK";
                case 1: return "MAG";
                case 2: return "DEF";
                case 3: return "RES";
                case 4: return "SPD";
                default: return "?";
            }
        }

        private static string GetStatGradeFromArrayOrFallback(Mon mon, int index, string statLabel, string[] exactNames)
        {
            string fromArray = GetStatGradeFromArray(mon, index);
            if (!string.IsNullOrEmpty(fromArray) && fromArray != "?") return fromArray;
            return GetStatGrade(mon, statLabel, exactNames);
        }

        private static string GetStatGradeFromArray(Mon mon, int index)
        {
            try
            {
                if (mon != null && mon.statGrades != null && index >= 0 && index < mon.statGrades.Length)
                    return GradeFromMonsterpatchStatGrade(mon.statGrades[index]);
            }
            catch { }
            return "?";
        }

        private static string GradeFromMonsterpatchStatGrade(int grade)
        {
            switch (Mathf.Clamp(grade, 0, 4))
            {
                case 4: return "S";
                case 3: return "A";
                case 2: return "B";
                case 1: return "C";
                default: return "D";
            }
        }

        private static string GetVibeText(Mon mon)
        {
            MemberValue mv = GetFirstMemberValueWithName(mon, new[] { "vibe", "Vibe", "vibeIndex", "VibeIndex", "nature", "Nature", "natureIndex", "NatureIndex", "personality", "Personality", "mood", "Mood", "temperament", "Temperament" });
            if (!mv.Found || mv.Value == null) return "?";

            try
            {
                Type vt = mv.Value.GetType();
                if (vt.IsEnum)
                    return SafeNonEmpty(mv.Value.ToString(), "?");
            }
            catch { }

            int index;
            if (TryConvertToInt(mv.Value, out index))
            {
                string reflected = TryResolveIndexedVibeName(mon, index);
                if (!string.IsNullOrWhiteSpace(reflected))
                    return reflected.Trim();

                string configured = GetConfiguredVibeIndexName(index);
                if (!string.IsNullOrWhiteSpace(configured))
                    return configured.Trim();

                // Better than showing a bare "6" when the game exposes only the index.
                return "Vibe " + index.ToString();
            }

            string raw = ValueToDisplayString(mv.Value);
            if (int.TryParse(raw, out index))
            {
                string reflected = TryResolveIndexedVibeName(mon, index);
                if (!string.IsNullOrWhiteSpace(reflected))
                    return reflected.Trim();
                string configured = GetConfiguredVibeIndexName(index);
                if (!string.IsNullOrWhiteSpace(configured))
                    return configured.Trim();
                return "Vibe " + index.ToString();
            }

            return SafeNonEmpty(raw, "?");
        }

        private static string GetConfiguredVibeIndexName(int index)
        {
            try
            {
                if (index >= 0 && index < RichListingVibeIndexNames.Length)
                    return RichListingVibeIndexNames[index];
            }
            catch { }
            return null;
        }

        private static string TryResolveIndexedVibeName(Mon mon, int index)
        {
            if (index < 0) return null;
            try
            {
                string v;
                if (mon != null)
                {
                    v = TryResolveIndexedNameFromObject(mon, index);
                    if (!string.IsNullOrWhiteSpace(v)) return v;
                    if (mon.monScriptableObject != null)
                    {
                        v = TryResolveIndexedNameFromObject(mon.monScriptableObject, index);
                        if (!string.IsNullOrWhiteSpace(v)) return v;
                    }
                }

                GameScript gs = FindObjectOfType<GameScript>();
                if (gs != null)
                {
                    v = TryResolveIndexedNameFromObject(gs, index);
                    if (!string.IsNullOrWhiteSpace(v)) return v;
                }

                v = TryResolveIndexedNameFromType(typeof(Mon), null, index);
                if (!string.IsNullOrWhiteSpace(v)) return v;
                v = TryResolveIndexedNameFromType(typeof(MonScriptableObject), null, index);
                if (!string.IsNullOrWhiteSpace(v)) return v;
                v = TryResolveIndexedNameFromType(typeof(GameScript), null, index);
                if (!string.IsNullOrWhiteSpace(v)) return v;
            }
            catch { }
            return null;
        }

        private static string TryResolveIndexedNameFromObject(object obj, int index)
        {
            if (obj == null) return null;
            return TryResolveIndexedNameFromType(obj.GetType(), obj, index);
        }

        private static string TryResolveIndexedNameFromType(Type t, object instance, int index)
        {
            if (t == null || index < 0) return null;
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            if (instance != null)
                flags |= BindingFlags.Instance;

            try
            {
                foreach (FieldInfo f in t.GetFields(flags))
                {
                    if (!LooksLikeVibeNameContainer(f.Name)) continue;
                    object value = null;
                    try { value = f.GetValue(f.IsStatic ? null : instance); } catch { }
                    string found = TryGetIndexedDisplayName(value, index);
                    if (!string.IsNullOrWhiteSpace(found)) return found;
                }

                foreach (PropertyInfo prop in t.GetProperties(flags))
                {
                    if (!prop.CanRead || prop.GetIndexParameters().Length != 0) continue;
                    if (!LooksLikeVibeNameContainer(prop.Name)) continue;
                    object value = null;
                    try { value = prop.GetValue((prop.GetGetMethod(true) != null && prop.GetGetMethod(true).IsStatic) ? null : instance, null); } catch { }
                    string found = TryGetIndexedDisplayName(value, index);
                    if (!string.IsNullOrWhiteSpace(found)) return found;
                }
            }
            catch { }
            return null;
        }

        private static bool LooksLikeVibeNameContainer(string name)
        {
            string n = (name ?? "").ToLowerInvariant();
            if (!(n.Contains("vibe") || n.Contains("nature") || n.Contains("personality") || n.Contains("mood") || n.Contains("temperament")))
                return false;
            return n.Contains("name") || n.Contains("text") || n.Contains("label") || n.Contains("string") || n.EndsWith("s");
        }

        private static string TryGetIndexedDisplayName(object value, int index)
        {
            if (value == null || index < 0) return null;
            try
            {
                if (value is string single)
                    return null;

                if (value is Array arr)
                {
                    if (index < arr.Length)
                    {
                        object item = arr.GetValue(index);
                        return item == null ? null : SafeNonEmpty(item.ToString(), null);
                    }
                }

                if (value is IList list)
                {
                    if (index < list.Count)
                    {
                        object item = list[index];
                        return item == null ? null : SafeNonEmpty(item.ToString(), null);
                    }
                }
            }
            catch { }
            return null;
        }

        private static string GetStatGrade(Mon mon, string statLabel, string[] exactNames)
        {
            MemberValue mv = GetFirstMemberValueWithName(mon, exactNames);
            if (mv.Found)
                return ValueToGrade(mv.Value, mv.Name);

            mv = FindLooseStatMember(mon, statLabel);
            if (mv.Found)
                return ValueToGrade(mv.Value, mv.Name);

            return "?";
        }

        private static MemberValue FindLooseStatMember(object obj, string statLabel)
        {
            if (obj == null) return MemberValue.None;
            try
            {
                string[] statTokens = StatTokens(statLabel);
                Type t = obj.GetType();
                foreach (FieldInfo f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    string n = f.Name.ToLowerInvariant();
                    if (!LooksLikeGradeOrIvField(n) || !ContainsAny(n, statTokens)) continue;
                    return new MemberValue(true, f.Name, SafeGetFieldValue(f, obj));
                }
                foreach (PropertyInfo p in t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (!p.CanRead || p.GetIndexParameters().Length != 0) continue;
                    string n = p.Name.ToLowerInvariant();
                    if (!LooksLikeGradeOrIvField(n) || !ContainsAny(n, statTokens)) continue;
                    object v = null;
                    try { v = p.GetValue(obj, null); } catch { }
                    return new MemberValue(true, p.Name, v);
                }
            }
            catch { }
            return MemberValue.None;
        }

        private static string[] StatTokens(string statLabel)
        {
            switch ((statLabel ?? "").ToUpperInvariant())
            {
                case "HP": return new[] { "hp", "health" };
                case "ATK": return new[] { "atk", "attack" };
                case "DEF": return new[] { "def", "defense" };
                case "MAG": return new[] { "mag", "magic", "spatk", "spattack", "specialattack" };
                case "MDF": return new[] { "mdf", "magdef", "magicdefense", "spdef", "spdefense", "specialdefense" };
                case "SPD": return new[] { "spd", "speed" };
                default: return new[] { statLabel != null ? statLabel.ToLowerInvariant() : "" };
            }
        }

        private static bool LooksLikeGradeOrIvField(string lowerName)
        {
            if (string.IsNullOrEmpty(lowerName)) return false;
            return lowerName.Contains("grade") || lowerName.Contains("iv") || lowerName.Contains("potential") || lowerName.Contains("gene");
        }

        private static bool ContainsAny(string value, string[] tokens)
        {
            if (value == null || tokens == null) return false;
            for (int i = 0; i < tokens.Length; i++)
            {
                string t = tokens[i];
                if (!string.IsNullOrEmpty(t) && value.Contains(t.ToLowerInvariant())) return true;
            }
            return false;
        }

        private static string ValueToGrade(object val, string memberName)
        {
            if (val == null) return "?";
            try
            {
                if (val is string s) return NormalizeGradeString(s);
                if (val is char c) return NormalizeGradeString(c.ToString());
                Type vt = val.GetType();
                if (vt.IsEnum) return NormalizeGradeString(val.ToString());
                int i;
                if (TryConvertToInt(val, out i))
                {
                    string n = (memberName ?? "").ToLowerInvariant();
                    if (n.Contains("grade") && i >= 0 && i <= 5)
                        return GradeFromZeroToFive(i);
                    if (i >= 0 && i <= 5 && !n.Contains("level"))
                        return GradeFromZeroToFive(i);
                    if (i >= 0 && i <= 31)
                        return GradeFromIv(i);
                    return "?";
                }
                return NormalizeGradeString(val.ToString());
            }
            catch { return "?"; }
        }

        private static string GradeFromZeroToFive(int i)
        {
            switch (Mathf.Clamp(i, 0, 5))
            {
                case 5: return "S";
                case 4: return "A";
                case 3: return "B";
                case 2: return "C";
                case 1: return "D";
                default: return "F";
            }
        }

        private static string GradeFromIv(int iv)
        {
            if (iv >= 31) return "S";
            if (iv >= 26) return "A";
            if (iv >= 20) return "B";
            if (iv >= 14) return "C";
            if (iv >= 8) return "D";
            return "F";
        }

        private static string NormalizeGradeString(string s)
        {
            s = SafeNonEmpty(s, "?").Trim();
            if (s.Length == 0) return "?";
            string u = s.ToUpperInvariant();
            if (u.Contains("PERFECT") || u == "S" || u == "SS") return "S";
            if (u.StartsWith("A")) return "A";
            if (u.StartsWith("B")) return "B";
            if (u.StartsWith("C")) return "C";
            if (u.StartsWith("D")) return "D";
            if (u.StartsWith("F") || u.Contains("BAD")) return "F";
            return s.Length <= 3 ? s : s.Substring(0, Math.Min(6, s.Length));
        }

        private static object GetFirstMemberValue(object obj, string[] names)
        {
            MemberValue mv = GetFirstMemberValueWithName(obj, names);
            return mv.Found ? mv.Value : null;
        }

        private static MemberValue GetFirstMemberValueWithName(object obj, string[] names)
        {
            if (obj == null || names == null) return MemberValue.None;
            Type t = obj.GetType();
            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i];
                try
                {
                    FieldInfo f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (f != null) return new MemberValue(true, f.Name, SafeGetFieldValue(f, obj));
                    PropertyInfo p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (p != null && p.CanRead && p.GetIndexParameters().Length == 0)
                    {
                        object v = null;
                        try { v = p.GetValue(obj, null); } catch { }
                        return new MemberValue(true, p.Name, v);
                    }
                }
                catch { }
            }
            return MemberValue.None;
        }

        private static object SafeGetFieldValue(FieldInfo f, object obj)
        {
            try { return f.GetValue(obj); } catch { return null; }
        }

        private static int SafeGetIntValue(object obj, string[] names, int fallback)
        {
            object val = GetFirstMemberValue(obj, names);
            int i;
            return TryConvertToInt(val, out i) ? i : fallback;
        }

        private static bool TryConvertToInt(object val, out int i)
        {
            i = 0;
            if (val == null) return false;
            try
            {
                if (val is int iv) { i = iv; return true; }
                if (val is short sv) { i = sv; return true; }
                if (val is byte bv) { i = bv; return true; }
                if (val is long lv) { i = (int)lv; return true; }
                if (val is float fv) { i = Mathf.RoundToInt(fv); return true; }
                if (val is double dv) { i = (int)Math.Round(dv); return true; }
                if (val is bool bo) { i = bo ? 1 : 0; return true; }
                return int.TryParse(val.ToString(), out i);
            }
            catch { return false; }
        }

        private static string ValueToDisplayString(object val)
        {
            if (val == null) return "?";
            try
            {
                string s = val.ToString();
                return SafeNonEmpty(s, "?");
            }
            catch { return "?"; }
        }

        private static string GenderSymbolFromInt(int gender)
        {
            switch (gender)
            {
                case 0: return "♂";
                case 1: return "♀";
                case 2: return "—";
                default: return "?";
            }
        }

        private static string SafeNonEmpty(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private static string NormalizeKey(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            StringBuilder sb = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char c = char.ToLowerInvariant(value[i]);
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9')) sb.Append(c);
            }
            return sb.ToString();
        }

        private static void DebugLogInterestingMonFields(Mon mon, string species, int listingId)
        {
            try
            {
                if (mon == null) return;
                List<string> bits = new List<string>();
                Type t = mon.GetType();
                foreach (FieldInfo f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    string n = f.Name.ToLowerInvariant();
                    if (n.Contains("vibe") || n.Contains("nature") || n.Contains("personality") || n.Contains("mood") || LooksLikeGradeOrIvField(n))
                    {
                        object v = SafeGetFieldValue(f, mon);
                        bits.Add(f.Name + "=" + (v == null ? "null" : v.ToString()));
                    }
                }
                if (bits.Count > 0)
                    GTSNativePatcher.RuntimeLog("Rich listing metadata fields for #" + listingId + " " + species + ": " + string.Join(", ", bits.ToArray()));
                else
                    GTSNativePatcher.RuntimeLog("Rich listing metadata fields for #" + listingId + " " + species + ": no obvious vibe/stat grade fields found on Mon.");
            }
            catch (Exception ex)
            {
                GTSNativePatcher.RuntimeWarn("Rich listing metadata debug failed: " + ex.Message);
            }
        }

        private void EnsureGtsButton(GameScript gs)
        {
            if (gs == null || gs.boxManager == null || gs.menuBox == null) return;
            BoxManager bm = gs.boxManager;
            if (!gs.menuBox.activeSelf || bm.subMenu == null || !bm.subMenu.activeSelf)
            {
                if (_gtsSubMenuButton != null) _gtsSubMenuButton.SetActive(false);
                RestoreSubMenuIfNeeded(bm);
                return;
            }
            if (bm.buttonSubMenu == null || bm.buttonSubMenu.Length == 0) return;

            CreateOrUpdateGtsSubMenuButton(gs, bm);
            if (_gtsSubMenuButton != null) _gtsSubMenuButton.SetActive(true);
        }

        private void CreateOrUpdateGtsSubMenuButton(GameScript gs, BoxManager bm)
        {
            if (bm == null || bm.buttonSubMenu == null || bm.buttonSubMenu.Length == 0) return;

            // Box submenu order observed in-game:
            // 0 Summary, 1 Move, 2 Item, 3 Send to, 4 Release, 5 Cancel.
            // Insert GTS between Send to and Release.
            int sendIndex = bm.buttonSubMenu.Length > 3 ? 3 : Math.Max(0, bm.buttonSubMenu.Length - 2);
            int releaseIndex = bm.buttonSubMenu.Length > 4 ? 4 : Math.Max(0, bm.buttonSubMenu.Length - 1);

            GameObject sendButton = bm.buttonSubMenu[sendIndex];
            GameObject releaseButton = bm.buttonSubMenu[releaseIndex];
            GameObject template = releaseButton != null ? releaseButton : sendButton;
            if (template == null) return;

            if (_gtsSubMenuButton == null)
            {
                _gtsSubMenuButton = Instantiate(template, template.transform.parent);
                _gtsSubMenuButton.name = "ButtonGTSClient";

                Button btn = _gtsSubMenuButton.GetComponent<Button>();
                if (btn == null) btn = _gtsSubMenuButton.AddComponent<Button>();
                btn.onClick.RemoveAllListeners();
                Image target = _gtsSubMenuButton.GetComponent<Image>();
                if (target != null) btn.targetGraphic = target;
                btn.interactable = true;
                btn.onClick.AddListener(() =>
                {
                    _showWindow = true;
                    _status = _loggedIn ? "Ready." : "Not logged in. Click Login with Steam.";
                });

                SetButtonLabel(_gtsSubMenuButton, "GTS");
            }

            CaptureSubMenuOriginals(bm);

            RectTransform sendRt = sendButton != null ? sendButton.GetComponent<RectTransform>() : null;
            RectTransform releaseRt = releaseButton != null ? releaseButton.GetComponent<RectTransform>() : null;
            RectTransform gtsRt = _gtsSubMenuButton.GetComponent<RectTransform>();
            if (sendRt == null || releaseRt == null || gtsRt == null) return;

            Vector2 step = releaseRt.anchoredPosition - sendRt.anchoredPosition;
            if (Mathf.Abs(step.y) < 0.01f) step = new Vector2(0f, -24f);

            // GTS takes the original Release row position. Release and Cancel move down one row.
            Vector2 releaseOriginal = GetOriginalPosition(releaseButton, releaseRt.anchoredPosition);
            gtsRt.anchoredPosition = releaseOriginal;
            for (int i = releaseIndex; i < bm.buttonSubMenu.Length; i++)
            {
                GameObject b = bm.buttonSubMenu[i];
                if (b == null) continue;
                RectTransform rt = b.GetComponent<RectTransform>();
                if (rt == null) continue;
                rt.anchoredPosition = GetOriginalPosition(b, rt.anchoredPosition) + step;
            }

            int sibling = releaseButton != null ? releaseButton.transform.GetSiblingIndex() : template.transform.GetSiblingIndex();
            _gtsSubMenuButton.transform.SetSiblingIndex(sibling);
            _gtsSubMenuButton.SetActive(true);

            WireGtsSubMenuNavigation(sendButton, _gtsSubMenuButton, releaseButton);
        }

        private void CaptureSubMenuOriginals(BoxManager bm)
        {
            if (bm == null || bm.buttonSubMenu == null) return;
            if (_subMenuOriginalPositions.Count == 0)
            {
                foreach (GameObject b in bm.buttonSubMenu)
                {
                    if (b == null) continue;
                    RectTransform rt = b.GetComponent<RectTransform>();
                    if (rt != null && !_subMenuOriginalPositions.ContainsKey(b))
                        _subMenuOriginalPositions.Add(b, rt.anchoredPosition);
                }
            }

            if (!_cancelOriginalCaptured)
            {
                RectTransform menuRt = bm.subMenu != null ? bm.subMenu.GetComponent<RectTransform>() : null;
                _subMenuOriginalSize = menuRt != null ? menuRt.sizeDelta : Vector2.zero;
                if (menuRt != null)
                {
                    Vector2 sz = menuRt.sizeDelta;
                    sz.y += 24f;
                    menuRt.sizeDelta = sz;
                }
                _cancelOriginalCaptured = true;
            }
        }

        private Vector2 GetOriginalPosition(GameObject obj, Vector2 fallback)
        {
            if (obj != null && _subMenuOriginalPositions.TryGetValue(obj, out Vector2 v)) return v;
            return fallback;
        }

        private void WireGtsSubMenuNavigation(GameObject sendButton, GameObject gtsButton, GameObject releaseButton)
        {
            Selectable sendSel = sendButton != null ? sendButton.GetComponent<Selectable>() : null;
            Selectable gtsSel = gtsButton != null ? gtsButton.GetComponent<Selectable>() : null;
            Selectable releaseSel = releaseButton != null ? releaseButton.GetComponent<Selectable>() : null;
            if (gtsSel == null) return;

            Navigation gtsNav = gtsSel.navigation;
            gtsNav.mode = Navigation.Mode.Explicit;
            gtsNav.selectOnUp = sendSel;
            gtsNav.selectOnDown = releaseSel;
            gtsSel.navigation = gtsNav;

            if (sendSel != null)
            {
                Navigation nav = sendSel.navigation;
                nav.mode = Navigation.Mode.Explicit;
                nav.selectOnDown = gtsSel;
                sendSel.navigation = nav;
            }

            if (releaseSel != null)
            {
                Navigation nav = releaseSel.navigation;
                nav.mode = Navigation.Mode.Explicit;
                nav.selectOnUp = gtsSel;
                releaseSel.navigation = nav;
            }
        }

        private void MaintainGtsSelectionArrow(GameScript gs)
        {
            try
            {
                if (_gtsSubMenuButton == null || !_gtsSubMenuButton.activeInHierarchy) return;
                if (gs == null || gs.eventSystem == null) return;
                if (gs.eventSystem.currentSelectedGameObject != _gtsSubMenuButton) return;
                if (gs.arrowRightObj != null)
                {
                    gs.arrowRightObj.transform.SetParent(_gtsSubMenuButton.transform);
                    gs.arrowRightObj.transform.localPosition = new Vector3(-38f, 0f, 0f);
                }
                gs.lastButtonObj = _gtsSubMenuButton;
            }
            catch { }
        }

        private void RestoreSubMenuIfNeeded(BoxManager bm)
        {
            try
            {
                if (bm == null) return;
                foreach (KeyValuePair<GameObject, Vector2> kv in _subMenuOriginalPositions)
                {
                    if (kv.Key == null) continue;
                    RectTransform rt = kv.Key.GetComponent<RectTransform>();
                    if (rt != null) rt.anchoredPosition = kv.Value;
                }
                RectTransform menuRt = bm.subMenu != null ? bm.subMenu.GetComponent<RectTransform>() : null;
                if (menuRt != null && _subMenuOriginalSize != Vector2.zero) menuRt.sizeDelta = _subMenuOriginalSize;
                _cancelOriginalCaptured = false;
            }
            catch { }
        }

        private IEnumerator EnsureAioAuthenticatedCoroutine()
        {
            if (_loggedIn && _client != null)
                yield break;

            _aioAuthBusy = true;
            try
            {
                if (!string.IsNullOrEmpty(_sessionToken))
                {
                    yield return SessionLoginCoroutine();
                    if (_loggedIn && _client != null)
                    {
                        _aioAuthBusy = false;
                        yield break;
                    }
                }

                yield return LoginWithSteamCoroutine();
            }
            finally
            {
                _aioAuthBusy = false;
            }
        }

        private IEnumerator SessionLoginCoroutine()
        {
            if (string.IsNullOrEmpty(_sessionToken))
                yield break;

            _busy = true;
            _status = "Restoring Trading Post Steam session...";
            yield return null;

            try
            {
                Disconnect(false, false);
                _client = new GtsSocketClient(ServerHost, ServerPort);
                _client.Connect();
                string line = _client.SendReadLine("SESSION_LOGIN\t" + _sessionToken);
                string[] pp = Split(line);
                if (pp.Length >= 2 && pp[0] == "OK" && pp[1] == "LOGIN")
                {
                    _loggedIn = true;
                    _username = pp.Length >= 3 ? B64Decode(pp[2]) : "Steam user";
                    _steamId64 = pp.Length >= 4 ? pp[3] : "";
                    if (pp.Length >= 6 && !string.IsNullOrEmpty(pp[5]))
                        _sessionToken = pp[5];
                    _status = "Steam session restored.";
                    _nextKeepAliveAt = Time.realtimeSinceStartup + AioKeepAliveSeconds;
                    _busy = false;
                    yield break;
                }

                throw new Exception(ParseErr(line, "Stored Trading Post session expired."));
            }
            catch (Exception ex)
            {
                _status = ex.Message;
                GTSNativePatcher.RuntimeWarn("Session restore failed: " + ex.Message);
                Disconnect(false, false);
                _busy = false;
            }
        }

        private void MaintainAioSessionKeepAlive()
        {
            if (!AioIntegratedMode || !_loggedIn || _client == null || _busy || _keepAliveBusy)
                return;
            if (Time.realtimeSinceStartup < _nextKeepAliveAt)
                return;
            StartCoroutine(KeepAliveCoroutine());
        }

        private IEnumerator KeepAliveCoroutine()
        {
            _keepAliveBusy = true;
            _nextKeepAliveAt = Time.realtimeSinceStartup + AioKeepAliveSeconds;
            bool shouldRestoreSession = false;
            string tokenToRestore = null;

            yield return null;
            try
            {
                string line = _client.SendReadLine("PING");
                string[] p = Split(line);
                if (p.Length < 2 || p[0] != "OK" || p[1] != "PING")
                    throw new Exception(ParseErr(line, "Trading Post keepalive failed."));

                PollAcceptedOfferNotifications();
            }
            catch (Exception ex)
            {
                GTSNativePatcher.RuntimeWarn("Trading Post keepalive failed: " + ex.Message);
                tokenToRestore = _sessionToken;
                Disconnect(false, false);
                _sessionToken = tokenToRestore;
                shouldRestoreSession = !string.IsNullOrEmpty(_sessionToken);
            }

            if (shouldRestoreSession)
                yield return SessionLoginCoroutine();

            _keepAliveBusy = false;
        }

        private void PollAcceptedOfferNotifications()
        {
            try
            {
                if (_client == null || !_loggedIn)
                    return;

                string line = _client.SendReadLine("GTS_NOTIFY_ACCEPTED");
                string[] p = Split(line);
                if (p.Length >= 3 && p[0] == "OK" && p[1] == "GTS_NOTIFY_ACCEPTED")
                {
                    int count = ParseInt(p[2], 0);
                    for (int i = 0; i < count; i++)
                        Goose.Monsterpatch.SocialPatcher.SocialNativePatcher.SocialRuntimeHost.AddTradingPostAcceptedNotification();
                }
                else if (DebugLogging)
                {
                    GTSNativePatcher.RuntimeWarn("Trading Post accepted-notification poll returned unexpected response: " + line);
                }
            }
            catch (Exception ex)
            {
                if (DebugLogging)
                    GTSNativePatcher.RuntimeWarn("Trading Post accepted-notification poll failed: " + ex.Message);
            }
        }

        private IEnumerator LoginWithSteamCoroutine()
        {
            _busy = true;
            _status = "Connecting to GTS server...";
            yield return null;

            string state = null;
            int ttl = 300;

            try
            {
                Disconnect(false);
                _client = new GtsSocketClient(ServerHost, ServerPort);
                _client.Connect();

                _status = "Starting Steam OpenID login...";
                string line = _client.SendReadLine("STEAM_OPENID_BEGIN");
                string[] p = Split(line);
                if (p.Length < 4 || p[0] != "OK" || p[1] != "STEAM_OPENID_BEGIN")
                    throw new Exception(ParseErr(line, "Could not start Steam login."));

                state = p[2];
                string loginUrl = B64Decode(p[3]);
                ttl = p.Length >= 5 ? ParseInt(p[4], 300) : 300;

                _status = "Steam login opened. Complete login in your browser.";
                if (AutoOpenSteamLogin) Application.OpenURL(loginUrl);
                else GTSNativePatcher.RuntimeLog("Open Steam login URL: " + loginUrl);
            }
            catch (Exception ex)
            {
                _status = ex.Message;
                GTSNativePatcher.RuntimeWarn("Steam login failed: " + ex.Message);
                Disconnect(false);
                _busy = false;
                yield break;
            }

            bool success = false;
            float end = Time.realtimeSinceStartup + Mathf.Clamp(ttl, 60, 600);
            while (Time.realtimeSinceStartup < end && !success)
            {
                yield return new WaitForSecondsRealtime(2f);

                try
                {
                    string poll = _client.SendReadLine("STEAM_OPENID_POLL\t" + state);
                    string[] pp = Split(poll);
                    if (pp.Length >= 1 && pp[0] == "PENDING")
                    {
                        _status = "Waiting for Steam login...";
                        continue;
                    }

                    if (pp.Length >= 2 && pp[0] == "OK" && pp[1] == "LOGIN")
                    {
                        _loggedIn = true;
                        _username = pp.Length >= 3 ? B64Decode(pp[2]) : "Steam user";
                        _steamId64 = pp.Length >= 4 ? pp[3] : "";
                        if (pp.Length >= 6 && !string.IsNullOrEmpty(pp[5]))
                            _sessionToken = pp[5];
                        _nextKeepAliveAt = Time.realtimeSinceStartup + AioKeepAliveSeconds;
                        _status = "Steam login successful.";
                        success = true;
                        break;
                    }

                    throw new Exception(ParseErr(poll, "Steam authentication failed. Please make sure Steam is running and you are online."));
                }
                catch (Exception ex)
                {
                    _status = ex.Message;
                    GTSNativePatcher.RuntimeWarn("Steam login failed: " + ex.Message);
                    Disconnect(false);
                    _busy = false;
                    yield break;
                }
            }

            if (!success)
            {
                _status = "Steam authentication timed out. Please try again.";
                GTSNativePatcher.RuntimeWarn(_status);
                Disconnect(false);
                _busy = false;
                yield break;
            }

            _busy = false;
            StartCoroutine(SearchListingsCoroutine(0));
        }

        private IEnumerator SearchListingsCoroutine(int page)
        {
            if (!EnsureLoggedIn()) yield break;
            _busy = true;
            _status = "Loading GTS listings...";
            yield return null;
            try
            {
                _listings.Clear();
                string line = _client.SendReadLine("GTS_SEARCH_PAGE\t" + Mathf.Max(0, page) + "\t*");
                ParseListingPage(line, _listings, out _pageIndex, "GTS_SEARCH_PAGE");
                _status = "Loaded " + _listings.Count + " open listings.";
                _mode = "Browse";
            }
            catch (Exception ex) { _status = ex.Message; GTSNativePatcher.RuntimeWarn(ex.Message); }
            _busy = false;
        }

        private IEnumerator MyListingsCoroutine(int page)
        {
            if (!EnsureLoggedIn()) yield break;
            _busy = true;
            _status = "Loading my listings...";
            yield return null;
            try
            {
                _myListings.Clear();
                string line = _client.SendReadLine("GTS_MY_LISTINGS_PAGE\t" + Mathf.Max(0, page));
                ParseListingPage(line, _myListings, out _pageIndex, "GTS_MY_LISTINGS_PAGE");
                _status = "Loaded " + _myListings.Count + " of your open listings.";
                _mode = "Mine";
            }
            catch (Exception ex) { _status = ex.Message; GTSNativePatcher.RuntimeWarn(ex.Message); }
            _busy = false;
        }

        private IEnumerator CreateListingCoroutine(string requestSpecies)
        {
            if (!EnsureLoggedIn()) yield break;
            GameScript gs = FindObjectOfType<GameScript>();
            BoxManager bm = gs != null ? gs.boxManager : null;
            Mon mon = GetOfferedBoxMon(gs, bm, out int slot, out string whyNot);
            if (mon == null) { _status = "Cannot list: " + whyNot; yield break; }
            if (string.IsNullOrWhiteSpace(requestSpecies)) { _status = "Choose a Requested MoN first."; yield break; }

            _busy = true;
            _status = "Creating GTS listing...";
            yield return null;
            try
            {
                string blob = BuildMonBlob(gs, mon);
                string cmd = "GTS_CREATE\t" + requestSpecies.Trim() + "\t" + GetSpecies(mon) + "\t" + GetLevel(mon) + "\t" + B64Encode(GetMonDisplayName(mon)) + "\t" + mon.gender + "\t" + (mon.isShiny ? 1 : 0) + "\t" + blob;
                string line = _client.SendReadLine(cmd);
                string[] p = Split(line);
                if (p.Length < 2 || p[0] != "OK" || p[1] != "GTS_CREATE") throw new Exception(ParseErr(line, "Could not create listing."));

                bm.boxMons[slot] = null;
                if (bm.subMenu != null) bm.subMenu.SetActive(false);
                try { bm.RefreshBox(); } catch { }
                gs.SaveGame();
                _offeredBoxSlot = -1;
                _offeredMonDropdownOpen = false;
                _status = "Listing created. MoN removed from storage.";
                StartCoroutine(MyListingsCoroutine(0));
            }
            catch (Exception ex) { _status = ex.Message; GTSNativePatcher.RuntimeWarn(ex.Message); }
            _busy = false;
        }

        private IEnumerator OfferSelectedMonCoroutine(GtsListing listing)
        {
            if (!EnsureLoggedIn()) yield break;
            if (listing == null) { _status = "No listing selected."; yield break; }
            if (IsOwnListing(listing))
            {
                _status = "You cannot offer to your own listing. Use My Listings to cancel/claim instead.";
                yield break;
            }

            GameScript gs = FindObjectOfType<GameScript>();
            BoxManager bm = gs != null ? gs.boxManager : null;
            Mon mon = GetOfferedBoxMon(gs, bm, out int slot, out string whyNot);
            if (mon == null) { _status = "Cannot offer: " + whyNot; yield break; }
            if (!string.Equals(GetSpecies(mon), listing.RequestSpecies, StringComparison.OrdinalIgnoreCase))
            {
                _status = "Chosen MoN is " + GetSpecies(mon) + ", but listing wants " + listing.RequestSpecies + ".";
                yield break;
            }

            _busy = true;
            _status = "Offering chosen MoN...";
            yield return null;
            try
            {
                string blob = BuildMonBlob(gs, mon);
                string cmd = "GTS_OFFER	" + listing.Id + "	" + GetSpecies(mon) + "	" + GetLevel(mon) + "	" + B64Encode(GetMonDisplayName(mon)) + "	" + mon.gender + "	" + (mon.isShiny ? 1 : 0) + "	" + blob;
                string line = _client.SendReadLine(cmd);
                string[] p = Split(line);
                if (p.Length < 3 || p[0] != "OK" || p[1] != "GTS_OFFER") throw new Exception(ParseErr(line, "Could not complete offer."));

                // Transaction safety: build/validate the received mon first.
                // Do not remove the offered mon unless the server accepted the trade AND the received blob imports correctly.
                Mon received = DecodeMonBlob(gs, p[2]);
                try
                {
                    received.uniqueID = gs.curUniqueIDCounter;
                    gs.curUniqueIDCounter++;
                }
                catch { }

                bm.boxMons[slot] = null; // only now remove the offered MoN
                bm.AddMonToBox(received);
                if (bm.subMenu != null) bm.subMenu.SetActive(false);
                try { bm.RefreshBox(); } catch { }
                gs.SaveGame();
                _capturedBoxMon = null;
                _capturedBoxSlot = -1;
                _offeredBoxSlot = -1;
                _offeredMonDropdownOpen = false;
                _status = "Trade complete. Received listed MoN.";
                StartCoroutine(SearchListingsCoroutine(_pageIndex));
            }
            catch (Exception ex)
            {
                _status = ex.Message;
                GTSNativePatcher.RuntimeWarn(ex.Message);
                try { bm.RefreshBox(); } catch { }
            }
            _busy = false;
        }

        private IEnumerator CancelListingCoroutine(GtsListing listing)
        {
            if (!EnsureLoggedIn()) yield break;
            GameScript gs = FindObjectOfType<GameScript>();
            BoxManager bm = gs != null ? gs.boxManager : null;
            if (gs == null || bm == null) { _status = "Storage not available."; yield break; }
            if (!bm.HasSpaceInBox()) { _status = "Storage is full. Make space before cancelling."; yield break; }
            _busy = true;
            _status = "Cancelling listing...";
            yield return null;
            try
            {
                string line = _client.SendReadLine("GTS_CANCEL\t" + listing.Id);
                string[] p = Split(line);
                if (p.Length < 3 || p[0] != "OK" || p[1] != "GTS_CANCEL") throw new Exception(ParseErr(line, "Could not cancel listing."));
                ImportBlobToBox(gs, bm, p[2]);
                try { bm.RefreshBox(); } catch { }
                gs.SaveGame();
                _status = "Listing cancelled and MoN returned.";
                StartCoroutine(MyListingsCoroutine(0));
            }
            catch (Exception ex) { _status = ex.Message; GTSNativePatcher.RuntimeWarn(ex.Message); }
            _busy = false;
        }

        private IEnumerator ClaimCoroutine()
        {
            if (!EnsureLoggedIn()) yield break;
            GameScript gs = FindObjectOfType<GameScript>();
            BoxManager bm = gs != null ? gs.boxManager : null;
            if (gs == null || bm == null) { _status = "Storage not available."; yield break; }
            _busy = true;
            _status = "Claiming completed trades...";
            yield return null;
            try
            {
                string line = _client.SendReadLine("GTS_CLAIM");
                string[] p = Split(line);
                if (p.Length < 3 || p[0] != "OK" || p[1] != "GTS_CLAIM") throw new Exception(ParseErr(line, "Could not claim trades."));
                int count = ParseInt(p[2], 0);
                if (CountEmpty(bm.boxMons) < count) throw new Exception("Not enough storage space for " + count + " claim(s).");
                int imported = 0;
                while (true)
                {
                    string next = _client.ReadLine();
                    string[] np = Split(next);
                    if (np.Length >= 1 && np[0] == "END") break;
                    if (np.Length >= 3 && np[0] == "CLAIM")
                    {
                        ImportBlobToBox(gs, bm, np[2]);
                        imported++;
                    }
                    else throw new Exception("Malformed claim response.");
                }
                try { bm.RefreshBox(); } catch { }
                gs.SaveGame();
                _status = imported == 0 ? "No completed trades to claim." : "Claimed " + imported + " MoN(s).";
            }
            catch (Exception ex) { _status = ex.Message; GTSNativePatcher.RuntimeWarn(ex.Message); }
            _busy = false;
        }

        private void ParseListingPage(string firstLine, List<GtsListing> target, out int pageIndex, string expected)
        {
            target.Clear();
            string[] p = Split(firstLine);
            if (p.Length < 6 || p[0] != "OK" || p[1] != expected) throw new Exception(ParseErr(firstLine, "Could not load listings."));
            pageIndex = ParseInt(p[2], 0);
            while (true)
            {
                string line = _client.ReadLine();
                string[] parts = Split(line);
                if (parts.Length >= 1 && parts[0] == "END") break;
                if (parts.Length < 10 || parts[0] != "LISTING") throw new Exception("Malformed listing response.");
                target.Add(new GtsListing
                {
                    Id = ParseInt(parts[1], 0),
                    OwnerUsername = parts[2],
                    RequestSpecies = parts[3],
                    OfferedSpecies = parts[4],
                    Level = ParseInt(parts[5], 1),
                    DisplayName = B64DecodeSafe(parts[6]),
                    Gender = ParseInt(parts[7], 0),
                    Shiny = ParseInt(parts[8], 0) != 0,
                    BlobB64 = parts[9],
                    CreatedAtRaw = parts.Length >= 11 ? parts[10] : ""
                });
            }
        }

        private static string DisplayOwner(GtsListing listing)
        {
            try
            {
                string owner = listing != null ? listing.OwnerUsername : null;
                if (string.IsNullOrWhiteSpace(owner)) return "Unknown player";
                if (owner.StartsWith("steam_", StringComparison.OrdinalIgnoreCase) && owner.Length > 12)
                    return "Steam " + owner.Substring(owner.Length - 6);
                return owner;
            }
            catch { return "Unknown player"; }
        }

        private bool EnsureLoggedIn()
        {
            if (_client == null || !_loggedIn)
            {
                _status = "Not logged in. Click Login with Steam first.";
                return false;
            }
            return true;
        }

        private Mon GetSelectedBoxMon(GameScript gs, BoxManager bm, out int slot, out string whyNot)
        {
            slot = -1;
            whyNot = "Box not open.";
            if (gs == null || bm == null || bm.boxMons == null) return null;
            if (gs.menuBox == null || !gs.menuBox.activeSelf)
            {
                whyNot = "No active Box selection.";
                return null;
            }

            // First prefer the mon captured at the moment the native GTS row was injected/opened.
            // This avoids losing the selected box slot after focus moves to the GTS row/window.
            if (_capturedBoxMon != null)
            {
                int capturedSlot = _capturedBoxSlot;
                if (capturedSlot < 0 || capturedSlot >= bm.boxMons.Length || !object.ReferenceEquals(bm.boxMons[capturedSlot], _capturedBoxMon))
                    capturedSlot = FindMonSlot(bm, _capturedBoxMon);

                if (capturedSlot >= 0 && capturedSlot < bm.boxMons.Length)
                {
                    slot = capturedSlot;
                    return _capturedBoxMon;
                }
            }

            // Fallback: BoxManager.SelectBoxEntry sets this when the submenu opens.
            if (gs.curInteractingMon != null)
            {
                int interactingSlot = FindMonSlot(bm, gs.curInteractingMon);
                if (interactingSlot >= 0)
                {
                    slot = interactingSlot;
                    _capturedBoxMon = gs.curInteractingMon;
                    _capturedBoxSlot = interactingSlot;
                    return gs.curInteractingMon;
                }
            }

            // Last fallback: current box cursor slot.
            slot = bm.curBoxEntry + bm.curBoxTab * 27;
            if (slot < 0 || slot >= bm.boxMons.Length)
            {
                whyNot = "Invalid selected slot.";
                return null;
            }
            Mon mon = bm.boxMons[slot];
            if (mon == null) whyNot = "No MoN selected in the current box slot.";
            return mon;
        }

        private static int FindMonSlot(BoxManager bm, Mon mon)
        {
            try
            {
                if (bm == null || bm.boxMons == null || mon == null) return -1;
                for (int i = 0; i < bm.boxMons.Length; i++)
                {
                    if (object.ReferenceEquals(bm.boxMons[i], mon))
                        return i;
                }
            }
            catch { }
            return -1;
        }

        private static bool IsGlobalBoxView(BoxManager bm)
        {
            try
            {
                return bm != null && bm.txtBox != null && bm.txtBox.text != null && bm.txtBox.text.ToUpperInvariant().Contains("GLOBAL");
            }
            catch { return false; }
        }

        private static string BuildMonBlob(GameScript gs, Mon mon)
        {
            string save = gs.ConstructMonSaveStringFromMon(mon, false);
            return B64Encode(save);
        }

        private static void ImportBlobToBox(GameScript gs, BoxManager bm, string blobB64)
        {
            string save = B64Decode(blobB64);
            Mon mon = gs.GetMonFromSaveString(save);
            try
            {
                mon.uniqueID = gs.curUniqueIDCounter;
                gs.curUniqueIDCounter++;
            }
            catch { }
            bm.AddMonToBox(mon);
        }

        private bool IsOwnListing(GtsListing listing)
        {
            try
            {
                if (listing == null || string.IsNullOrEmpty(listing.OwnerUsername) || string.IsNullOrEmpty(_username)) return false;
                return string.Equals(listing.OwnerUsername.Trim(), _username.Trim(), StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        private static Mon DecodeMonBlob(GameScript gs, string blobB64)
        {
            string save = B64Decode(blobB64);
            return gs.GetMonFromSaveString(save);
        }

        private static int CountEmpty(Mon[] arr)
        {
            if (arr == null) return 0;
            int c = 0;
            for (int i = 0; i < arr.Length; i++) if (arr[i] == null) c++;
            return c;
        }

        private static string GetSpecies(Mon mon)
        {
            try { return mon != null && mon.monScriptableObject != null ? mon.monScriptableObject.monName : "UNKNOWN"; }
            catch { return "UNKNOWN"; }
        }

        private static string GetMonDisplayName(Mon mon)
        {
            try
            {
                if (mon == null) return "UNKNOWN";
                if (!string.IsNullOrWhiteSpace(mon.nickName)) return mon.nickName;
                return GetSpecies(mon);
            }
            catch { return GetSpecies(mon); }
        }

        private static int GetLevel(Mon mon)
        {
            try { return mon.curLevel; }
            catch { return 1; }
        }

        private static void SetButtonLabel(GameObject button, string label)
        {
            if (button == null) return;
            TMP_Text tmp = button.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null) { tmp.text = label; return; }
            Text txt = button.GetComponentInChildren<Text>(true);
            if (txt != null) txt.text = label;
        }

        private void Disconnect(bool updateStatus = true)
        {
            Disconnect(updateStatus, false);
        }

        private void Disconnect(bool updateStatus, bool clearSessionToken)
        {
            if (clearSessionToken && _client != null && !string.IsNullOrEmpty(_sessionToken))
            {
                try { _client.SendReadLine("SESSION_LOGOUT\t" + _sessionToken); } catch { }
            }

            try { _client?.Dispose(); } catch { }
            _client = null;
            _loggedIn = false;
            _username = "";
            _steamId64 = "";
            _keepAliveBusy = false;
            if (clearSessionToken) _sessionToken = "";
            if (updateStatus) _status = "Disconnected.";
        }

        private void OnApplicationQuit()
        {
            // Do not persist the auto-auth token across launches.
            // Best-effort revoke on quit, then next game launch requires Steam auth again.
            _applicationQuitting = true;
            Disconnect(false, true);
        }

        private void OnDestroy()
        {
            // Scene/unity teardown should not erase the token unless the app is actually closing.
            // The token is memory-only and is intentionally not written to config/disk.
            Disconnect(false, _applicationQuitting);
        }

        private static string[] Split(string line)
        {
            return (line ?? "").Split('\t');
        }

        private static int ParseInt(string raw, int fallback)
        {
            return int.TryParse(raw, out int v) ? v : fallback;
        }

        private static string B64Encode(string s)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(s ?? ""));
        }

        private static string B64Decode(string s)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(s ?? ""));
        }

        private static string B64DecodeSafe(string s)
        {
            try { return B64Decode(s); } catch { return "?"; }
        }

        private static string ParseErr(string line, string fallback)
        {
            string[] p = Split(line);
            if (p.Length >= 2 && p[0] == "ERR") return B64DecodeSafe(p[1]);
            return fallback + " Raw: " + line;
        }

        private class BoxMonOption
        {
            public int Slot;
            public string Label;
        }

        private class GtsListing
        {
            public int Id;
            public string OwnerUsername;
            public string RequestSpecies;
            public string OfferedSpecies;
            public int Level;
            public string DisplayName;
            public int Gender;
            public bool Shiny;
            public string BlobB64;
            public string CreatedAtRaw;
            public bool RichMetadataParsed;
            public bool RichMetadataDebugLogged;
            public string RichMetadataBlob;
            public RichMonMetadata RichMetadata;
        }

        private class RichMonMetadata
        {
            public string Species = "UNKNOWN";
            public string DisplayName = "UNKNOWN";
            public int Level = 1;
            public bool Shiny;
            public string GenderSymbol = "?";
            public string Vibe = "?";
            public int VibeIndex = -1;
            public string VibePlusStat = "?";
            public string VibeMinusStat = "?";
            public string HpGrade = "?";
            public string AtkGrade = "?";
            public string DefGrade = "?";
            public string MagGrade = "?";
            public string MdfGrade = "?";
            public string SpdGrade = "?";
        }

        private struct MemberValue
        {
            public readonly bool Found;
            public readonly string Name;
            public readonly object Value;
            public MemberValue(bool found, string name, object value)
            {
                Found = found;
                Name = name;
                Value = value;
            }
            public static readonly MemberValue None = new MemberValue(false, null, null);
        }

        private class GtsSocketClient : IDisposable
        {
            private readonly string _host;
            private readonly int _port;
            private TcpClient _tcp;
            private StreamReader _reader;
            private StreamWriter _writer;

            public GtsSocketClient(string host, int port)
            {
                _host = host;
                _port = port;
            }

            public void Connect()
            {
                _tcp = new TcpClient();
                _tcp.ReceiveTimeout = 15000;
                _tcp.SendTimeout = 15000;
                _tcp.Connect(_host, _port);
                NetworkStream ns = _tcp.GetStream();
                _reader = new StreamReader(ns, Encoding.UTF8);
                _writer = new StreamWriter(ns, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" };
            }

            public string SendReadLine(string command)
            {
                WriteLine(command);
                return ReadLine();
            }

            public void WriteLine(string command)
            {
                if (_writer == null) throw new IOException("Not connected to GTS server.");
                _writer.WriteLine(command);
            }

            public string ReadLine()
            {
                if (_reader == null) throw new IOException("Not connected to GTS server.");
                string line = _reader.ReadLine();
                if (line == null) throw new IOException("GTS server disconnected.");
                return line;
            }

            public void Dispose()
            {
                try { _reader?.Dispose(); } catch { }
                try { _writer?.Dispose(); } catch { }
                try { _tcp?.Close(); } catch { }
                _reader = null;
                _writer = null;
                _tcp = null;
            }
        }
    }

    
    
}
