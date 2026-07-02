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
        public const string Version = "0.11.0-compact-mail-attach";
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
        internal static string CachedOfficialSessionTokenBase64 = "";
        internal static string CachedOfficialSessionExpiresUtc = "";
        internal static string CachedOfficialSteamID64 = "";
        internal static string CachedOfficialAccountUUID = "";
        internal static bool TradingPostRememberWindowPosition = true;
        internal static bool TradingPostRememberWindowSize = true;
        internal static float TradingPostWindowX = 60f;
        internal static float TradingPostWindowY = 60f;
        internal static float TradingPostWindowWidth = 560f;
        internal static float TradingPostWindowHeight = 620f;
        internal static string TradingPostDefaultFilterSearchText = "";
        internal static string TradingPostDefaultFilterOffered = "All";
        internal static string TradingPostDefaultFilterRequested = "All";
        internal static string TradingPostDefaultFilterType = "All";
        internal static string TradingPostDefaultFilterTimeLeft = "All";
        internal static string TradingPostDefaultFilterSeller = "";
        internal static float TradingPostFilterButtonHeight = 30f;
        internal static float TradingPostFilterFieldHeight = 30f;
        internal static float TradingPostSearchFieldHeight = 30f;
        internal static float TradingPostRefreshButtonHeight = 30f;
        internal static float TradingPostSearchOffsetX = 0f;
        internal static float TradingPostSearchOffsetY = 0f;
        internal static float TradingPostSearchWidth = 120f;
        internal static float TradingPostSearchHeight = 30f;
        internal static float TradingPostOfferedFilterOffsetX = 0f;
        internal static float TradingPostOfferedFilterOffsetY = 0f;
        internal static float TradingPostOfferedFilterWidth = 92f;
        internal static float TradingPostOfferedFilterHeight = 30f;
        internal static float TradingPostRequestedFilterOffsetX = 0f;
        internal static float TradingPostRequestedFilterOffsetY = 0f;
        internal static float TradingPostRequestedFilterWidth = 105f;
        internal static float TradingPostRequestedFilterHeight = 30f;
        internal static float TradingPostTypeFilterOffsetX = 0f;
        internal static float TradingPostTypeFilterOffsetY = 0f;
        internal static float TradingPostTypeFilterWidth = 110f;
        internal static float TradingPostTypeFilterHeight = 30f;
        internal static float TradingPostTimeFilterOffsetX = 0f;
        internal static float TradingPostTimeFilterOffsetY = 0f;
        internal static float TradingPostTimeFilterWidth = 100f;
        internal static float TradingPostTimeFilterHeight = 30f;
        internal static float TradingPostSellerFilterOffsetX = 0f;
        internal static float TradingPostSellerFilterOffsetY = 0f;
        internal static float TradingPostSellerFilterWidth = 90f;
        internal static float TradingPostSellerFilterHeight = 30f;
        internal static float TradingPostRefreshFilterOffsetX = 0f;
        internal static float TradingPostRefreshFilterOffsetY = 0f;
        internal static float TradingPostRefreshFilterWidth = 90f;
        internal static float TradingPostRefreshFilterHeight = 30f;
        internal static float MailComposeOpenButtonWidth = 120f;
        internal static float MailComposeOpenButtonHeight = 30f;
        internal static float MailComposeDrawerWidth = 440f;
        internal static float MailComposeDrawerHeight = 430f;
        internal static float MailComposeDrawerGap = -2f;
        internal static float MailComposeDrawerOffsetY = 48f;
        internal static float MailComposeTitleOffsetX = 0f;
        internal static float MailComposeTitleOffsetY = 0f;
        internal static float MailComposeToLabelOffsetX = 0f;
        internal static float MailComposeToLabelOffsetY = 0f;
        internal static float MailComposeToLabelWidth = 54f;
        internal static float MailComposeToLabelHeight = 24f;
        internal static float MailComposeToFieldOffsetX = 0f;
        internal static float MailComposeToFieldOffsetY = 0f;
        internal static float MailComposeToFieldWidth = 260f;
        internal static float MailComposeToFieldHeight = 28f;
        internal static float MailComposeSubjectLabelOffsetX = 0f;
        internal static float MailComposeSubjectLabelOffsetY = 0f;
        internal static float MailComposeSubjectLabelWidth = 54f;
        internal static float MailComposeSubjectLabelHeight = 24f;
        internal static float MailComposeSubjectFieldOffsetX = 0f;
        internal static float MailComposeSubjectFieldOffsetY = 0f;
        internal static float MailComposeSubjectFieldWidth = 300f;
        internal static float MailComposeSubjectFieldHeight = 28f;
        internal static float MailComposeBodyLabelOffsetX = 0f;
        internal static float MailComposeBodyLabelOffsetY = 0f;
        internal static float MailComposeBodyLabelWidth = 54f;
        internal static float MailComposeBodyLabelHeight = 24f;
        internal static float MailComposeBodyFieldOffsetX = 0f;
        internal static float MailComposeBodyFieldOffsetY = 0f;
        internal static float MailComposeBodyFieldWidth = 360f;
        internal static float MailComposeBodyFieldHeight = 160f;
        internal static float MailComposeSendButtonOffsetX = 0f;
        internal static float MailComposeSendButtonOffsetY = 0f;
        internal static float MailComposeSendButtonWidth = 90f;
        internal static float MailComposeSendButtonHeight = 30f;
        internal static float MailComposeCancelButtonOffsetX = 0f;
        internal static float MailComposeCancelButtonOffsetY = 0f;
        internal static float MailComposeCancelButtonWidth = 90f;
        internal static float MailComposeCancelButtonHeight = 30f;
        internal static float MailComposeAttachmentLabelOffsetX = 0f;
        internal static float MailComposeAttachmentLabelOffsetY = 0f;
        internal static float MailComposeAttachmentLabelWidth = 82f;
        internal static float MailComposeAttachmentLabelHeight = 24f;
        internal static float MailComposeAttachmentTypeOffsetX = 0f;
        internal static float MailComposeAttachmentTypeOffsetY = 0f;
        internal static float MailComposeAttachmentTypeWidth = 132f;
        internal static float MailComposeAttachmentTypeHeight = 28f;
        internal static float MailComposeAttachmentValueOffsetX = 0f;
        internal static float MailComposeAttachmentValueOffsetY = 0f;
        internal static float MailComposeAttachmentValueWidth = 260f;
        internal static float MailComposeAttachmentValueHeight = 36f;
        internal static float MailComposeAttachmentArrowOffsetX = 0f;
        internal static float MailComposeAttachmentArrowOffsetY = 0f;
        internal static float MailComposeAttachmentArrowWidth = 28f;
        internal static float MailComposeAttachmentDropdownOffsetX = 0f;
        internal static float MailComposeAttachmentDropdownOffsetY = 0f;
        internal static float MailComposeAttachmentDropdownWidth = 360f;
        internal static float MailComposeAttachmentDropdownHeight = 170f;
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
        internal static float OfferedMONRowHeight = 52f;
        internal static float OfferedMONSelectedPreviewHeight = 44f;
        internal static bool OfferedMONShowIcon = true;
        internal static float OfferedMONIconSize = 36f;
        internal static float OfferedMONIconOffsetX = 0f;
        internal static float OfferedMONIconOffsetY = 0f;
        internal static float OfferedMONTextOffsetX = 44f;
        internal static float OfferedMONTextOffsetY = 0f;

        internal static float RequestedMONRowHeight = 52f;
        internal static float RequestedMONSelectedPreviewHeight = 44f;
        internal static bool RequestedMONShowIcon = true;
        internal static float RequestedMONIconSize = 36f;
        internal static float RequestedMONIconOffsetX = 0f;
        internal static float RequestedMONIconOffsetY = 0f;
        internal static float RequestedMONTextOffsetX = 44f;
        internal static float RequestedMONTextOffsetY = 0f;

        // Trading Post create-listing layout controls. These are intentionally exposed
        // so the layout can be tuned in goose.monsterpatch.gts.client.cfg without rebuilds.
        internal static float TradingPostCreateLabelWidth = 82f;
        internal static float TradingPostOfferedTypeButtonWidth = 125f;
        internal static float TradingPostOfferedFieldWidth = 220f;
        internal static float TradingPostOfferedSatsFieldWidth = 176f;
        internal static float TradingPostOfferedArrowWidth = 28f;
        internal static float TradingPostRequestTypeButtonWidth = 125f;
        internal static float TradingPostRequestFieldWidth = 220f;
        internal static float TradingPostRequestSatsFieldWidth = 176f;
        internal static float TradingPostRequestArrowWidth = 28f;
        internal static float TradingPostListPostButtonWidth = 118f;
        internal static float TradingPostCreateRowSpacing = 6f;
        internal static float TradingPostCreateColumnGap = 4f;
        internal static float TradingPostOfferedRowOffsetX = 51f;
        internal static float TradingPostRequestedRowOffsetX = 0f;
        internal static float TradingPostRequestFieldOffsetX = 0f;
        internal static float TradingPostListPostOffsetX = 0f;

        // Separate Create Listing window controls. These keep the main Trading Post
        // clean while allowing the listing form to be tuned from config.
        internal static float TradingPostCreateWindowX = 120f;
        internal static float TradingPostCreateWindowY = 120f;
        internal static float TradingPostCreateWindowWidth = 620f;
        internal static float TradingPostCreateWindowHeight = 410f;
        internal static bool TradingPostCreateUseSideDrawer = true;
        internal static float TradingPostCreateDrawerGap = -2f;
        internal static float TradingPostCreateDrawerOffsetY = 48f;
        internal static bool TradingPostCreateDimMainWindow = false;
        internal static float TradingPostCreateOpenButtonWidth = 170f;
        internal static float TradingPostCreateOpenButtonHeight = 30f;
        internal static float TradingPostCreateWindowPaddingTop = 6f;
        internal static float TradingPostCreateWindowPaddingLeft = 0f;
        internal static float TradingPostOfferedKindDropdownOffsetX = 0f;
        internal static float TradingPostOfferedKindDropdownWidth = 150f;
        internal static float TradingPostOfferedDropdownOffsetX = 115f;
        internal static float TradingPostOfferedDropdownWidth = 420f;
        internal static float TradingPostOfferedDropdownHeight = 170f;
        internal static float TradingPostRequestKindDropdownOffsetX = 0f;
        internal static float TradingPostRequestKindDropdownWidth = 150f;
        internal static float TradingPostRequestSpeciesDropdownOffsetX = 115f;
        internal static float TradingPostRequestSpeciesDropdownWidth = 250f;
        internal static float TradingPostRequestSpeciesDropdownHeight = 170f;
        internal static float TradingPostCreateTitleOffsetX = 0f;
        internal static float TradingPostCreateTitleOffsetY = 0f;

        // v0.10.5 grouped Create Listing positioning. These move a full logical control
        // together: the type button follows its MoN/SATS dropdown, and the value field
        // follows its arrow/list dropdown. The older per-element offsets below remain
        // as fine-tune controls and aliases for old configs.
        internal static float TradingPostOfferedTypeGroupOffsetX = 0f;
        internal static float TradingPostOfferedTypeGroupOffsetY = 0f;
        internal static float TradingPostOfferedValueGroupOffsetX = 0f;
        internal static float TradingPostOfferedValueGroupOffsetY = 0f;
        internal static float TradingPostRequestedTypeGroupOffsetX = 0f;
        internal static float TradingPostRequestedTypeGroupOffsetY = 0f;
        internal static float TradingPostRequestedValueGroupOffsetX = 0f;
        internal static float TradingPostRequestedValueGroupOffsetY = 0f;
        internal static float TradingPostListPostButtonOffsetX = 0f;
        internal static float TradingPostListPostButtonOffsetY = 0f;
        internal static float TradingPostOfferedKindDropdownOffsetY = 0f;
        internal static float TradingPostOfferedValueDropdownOffsetX = 0f;
        internal static float TradingPostOfferedValueDropdownOffsetY = 0f;
        internal static float TradingPostRequestKindDropdownOffsetY = 0f;
        internal static float TradingPostRequestedValueDropdownOffsetX = 0f;
        internal static float TradingPostRequestedValueDropdownOffsetY = 0f;

        internal static float TradingPostOfferedTypeButtonOffsetX = 0f;
        internal static float TradingPostOfferedTypeButtonOffsetY = 0f;
        internal static float TradingPostOfferedFieldOffsetX = 0f;
        internal static float TradingPostOfferedFieldOffsetY = 0f;
        internal static float TradingPostOfferedArrowOffsetX = 0f;
        internal static float TradingPostOfferedArrowOffsetY = 0f;
        internal static float TradingPostOfferedSatsFieldOffsetX = 0f;
        internal static float TradingPostOfferedSatsFieldOffsetY = 0f;
        internal static float TradingPostRequestTypeButtonOffsetX = 0f;
        internal static float TradingPostRequestTypeButtonOffsetY = 0f;
        internal static float TradingPostRequestFieldOffsetY = 0f;
        internal static float TradingPostRequestArrowOffsetX = 0f;
        internal static float TradingPostRequestArrowOffsetY = 0f;
        internal static float TradingPostRequestSatsFieldOffsetX = 0f;
        internal static float TradingPostRequestSatsFieldOffsetY = 0f;
        internal static float TradingPostListPostOffsetY = 0f;
        internal static float TradingPostCreateHelpTextOffsetX = 0f;
        internal static float TradingPostCreateHelpTextOffsetY = 0f;

        // Auction-house table icon controls.
        internal static float TradingPostTableIconSize = 28f;
        internal static float TradingPostTableIconTextGap = 6f;
        internal static bool TradingPostTableShowOfferedIcon = true;
        internal static bool TradingPostTableShowRequestedIcon = true;
        internal static float TradingPostOfferIconSize = 28f;
        internal static float TradingPostOfferIconOffsetX = 0f;
        internal static float TradingPostOfferIconOffsetY = 0f;
        internal static float TradingPostRequestedIconSize = 28f;
        internal static float TradingPostRequestedIconOffsetX = 0f;
        internal static float TradingPostRequestedIconOffsetY = 0f;
        internal static float TradingPostOfferSatsIconSize = 28f;
        internal static float TradingPostOfferSatsIconOffsetX = 0f;
        internal static float TradingPostOfferSatsIconOffsetY = 0f;
        internal static float TradingPostRequestedSatsIconSize = 28f;
        internal static float TradingPostRequestedSatsIconOffsetX = 0f;
        internal static float TradingPostRequestedSatsIconOffsetY = 0f;

        // Auction-house grid positioning controls. These are intentionally exposed so the
        // Browse/My Listings table can be tuned from config without rebuilding.
        internal static float TradingPostBrowseGridOffsetX = 0f;
        internal static float TradingPostBrowseGridOffsetY = 0f;
        internal static float TradingPostFilterRowOffsetX = 0f;
        internal static float TradingPostFilterRowOffsetY = 0f;
        internal static float TradingPostRefreshButtonOffsetX = 0f;
        internal static float TradingPostRefreshButtonOffsetY = 0f;
        internal static float TradingPostPagerOffsetX = 0f;
        internal static float TradingPostPagerOffsetY = 0f;
        internal static float TradingPostGridHeaderOffsetX = 0f;
        internal static float TradingPostGridHeaderOffsetY = 0f;
        internal static float TradingPostGridScrollHeightOffset = 0f;
        internal static float TradingPostTableRowHeight = 58f;
        internal static float TradingPostNameColumnWidth = 190f; // legacy alias; table header now says Offer
        internal static float TradingPostOfferColumnWidth = 190f;
        internal static float TradingPostRequestedColumnWidth = 135f;
        internal static float TradingPostTimeLeftColumnWidth = 80f;
        internal static float TradingPostSellerColumnWidth = 105f;
        // v0.10.4 removes the visible Price / Offer column. Keep width for old configs/backward compatibility.
        internal static float TradingPostPriceOfferColumnWidth = 110f;
        internal static bool TradingPostShowPriceOfferColumn = false;
        internal static float TradingPostActionColumnWidth = 150f;
        internal static float TradingPostActionButtonWidth = 145f;
        internal static float TradingPostActionButtonHeight = 34f;
        internal static float TradingPostHeaderOfferOffsetX = 0f;
        internal static float TradingPostHeaderOfferOffsetY = 0f;
        internal static float TradingPostHeaderRequestedOffsetX = 0f;
        internal static float TradingPostHeaderRequestedOffsetY = 0f;
        internal static float TradingPostHeaderTimeLeftOffsetX = 0f;
        internal static float TradingPostHeaderTimeLeftOffsetY = 0f;
        internal static float TradingPostHeaderSellerOffsetX = 0f;
        internal static float TradingPostHeaderSellerOffsetY = 0f;
        internal static float TradingPostHeaderPriceOfferOffsetX = 0f;
        internal static float TradingPostHeaderPriceOfferOffsetY = 0f;
        internal static float TradingPostHeaderActionOffsetX = 0f;
        internal static float TradingPostHeaderActionOffsetY = 0f;
        internal static float TradingPostOfferCellOffsetX = 0f;
        internal static float TradingPostOfferCellOffsetY = 0f;
        internal static float TradingPostRequestedCellOffsetX = 0f;
        internal static float TradingPostRequestedCellOffsetY = 0f;
        internal static float TradingPostTimeLeftCellOffsetX = 0f;
        internal static float TradingPostTimeLeftCellOffsetY = 0f;
        internal static float TradingPostSellerCellOffsetX = 0f;
        internal static float TradingPostSellerCellOffsetY = 0f;
        internal static float TradingPostPriceOfferCellOffsetX = 0f;
        internal static float TradingPostPriceOfferCellOffsetY = 0f;
        internal static float TradingPostActionCellOffsetX = 0f;
        internal static float TradingPostActionCellOffsetY = 0f;
        internal static float TradingPostRowOffsetX = 0f;
        internal static float TradingPostRowOffsetY = 0f;

        private const float NativeIconX = 0f;
        private const float NativeLabelX = 14f;
        private const float NativeButtonRowY = -1.5f;

        private Rect _windowRect = new Rect(60, 60, 560, 620);
        private Rect _createListingWindowRect = new Rect(120, 120, 620, 410);
        private bool _showCreateListingWindow;
        private bool _resizingWindow;
        private Vector2 _resizeStartScreenMouse;
        private Rect _resizeStartWindowRect;
        private bool _showWindow;
        private Vector2 _scroll;
        private string _status = "GTS not connected.";
        private string _requestSpecies = "";
        private string _offeredKind = "MON";
        private string _offeredSatsText = "";
        private bool _offeredKindDropdownOpen;
        private string _requestKind = "MON";
        private string _requestSatsText = "";
        private bool _requestKindDropdownOpen;
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
        private readonly List<MailItem> _mailItems = new List<MailItem>();
        private MailDetail _selectedMail;
        private int _mailPageIndex;
        private Vector2 _mailScroll;
        private Vector2 _mailDetailScroll;
        private string _mailRecipient = "";
        private string _mailSubject = "";
        private string _mailBody = "";
        private string _mailAttachmentKind = "NONE";
        private string _mailAttachmentSatsText = "";
        private bool _mailAttachmentMonDropdownOpen;
        private Vector2 _mailAttachmentMonDropdownScroll;
        private int _mailAttachmentBoxSlot = -1;
        private bool _showComposeMail;
        private Rect _mailComposeWindowRect = new Rect(120, 120, 440, 360);
        private string _mailSelfHandle = "";
        private int _mailUnreadCount;
        private int _mailTotalCount;
        private int _mailClaimableCount;
        private float _nextMailCountAt;
        private bool _busy;
        private bool _loggedIn;
        private string _username = "";
        private string _steamId64 = "";
        private string _sessionToken = "";
        private string _lastSentCharacterContextKey = "";
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
        private string _filterSearchText = "";
        private string _filterOffered = "All";
        private string _filterRequested = "All";
        private string _filterType = "All";
        private string _filterTimeLeft = "All";
        private string _filterSeller = "";
        private Rect _lastSavedTradingPostWindowRect;
        private float _lastTradingPostWindowConfigSaveAt;
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
        private Sprite _mpSatsCoinSprite;

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
                // Keeps the Steam/GTS AIO session token available for reconnect.
                // The server enforces 12-hour expiry and same-IP validation.
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

        public static void ResetStaleOfficialServerAuthForAio()
        {
            try
            {
                EnsureHost();
                if (Instance == null)
                    return;

                // Official Server save-select auth must never stay stuck behind an old
                // in-progress GTS/AIO state.  This is intentionally a hard reset for the
                // auth socket/session only; it does not touch local/offline saves.
                try { Instance._client?.Dispose(); } catch { }
                Instance._client = null;
                Instance._loggedIn = false;
                Instance._busy = false;
                Instance._aioAuthBusy = false;
                Instance._keepAliveBusy = false;
                Instance._username = "";
                Instance._steamId64 = "";
                Instance._sessionToken = "";
                Instance._status = "Official Server Steam auth reset.";

                if (DebugLogging)
                    GTSNativePatcher.RuntimeLog("Official Server reset stale Steam/AIO auth state before save-select login.");
            }
            catch (Exception ex)
            {
                GTSNativePatcher.RuntimeWarn("ResetStaleOfficialServerAuthForAio failed: " + ex.Message);
            }
        }

        private static bool TryParseMailboxComposeLayoutValue(string key, string value)
        {
            float f;
            if (!float.TryParse(value, out f)) return false;
            if (key.Equals("TitleOffsetX", StringComparison.OrdinalIgnoreCase)) { MailComposeTitleOffsetX = f; return true; }
            if (key.Equals("TitleOffsetY", StringComparison.OrdinalIgnoreCase)) { MailComposeTitleOffsetY = f; return true; }
            if (key.Equals("ToLabelOffsetX", StringComparison.OrdinalIgnoreCase)) { MailComposeToLabelOffsetX = f; return true; }
            if (key.Equals("ToLabelOffsetY", StringComparison.OrdinalIgnoreCase)) { MailComposeToLabelOffsetY = f; return true; }
            if (key.Equals("ToLabelWidth", StringComparison.OrdinalIgnoreCase)) { MailComposeToLabelWidth = f; return true; }
            if (key.Equals("ToLabelHeight", StringComparison.OrdinalIgnoreCase)) { MailComposeToLabelHeight = f; return true; }
            if (key.Equals("ToFieldOffsetX", StringComparison.OrdinalIgnoreCase)) { MailComposeToFieldOffsetX = f; return true; }
            if (key.Equals("ToFieldOffsetY", StringComparison.OrdinalIgnoreCase)) { MailComposeToFieldOffsetY = f; return true; }
            if (key.Equals("ToFieldWidth", StringComparison.OrdinalIgnoreCase)) { MailComposeToFieldWidth = f; return true; }
            if (key.Equals("ToFieldHeight", StringComparison.OrdinalIgnoreCase)) { MailComposeToFieldHeight = f; return true; }
            if (key.Equals("SubjectLabelOffsetX", StringComparison.OrdinalIgnoreCase)) { MailComposeSubjectLabelOffsetX = f; return true; }
            if (key.Equals("SubjectLabelOffsetY", StringComparison.OrdinalIgnoreCase)) { MailComposeSubjectLabelOffsetY = f; return true; }
            if (key.Equals("SubjectLabelWidth", StringComparison.OrdinalIgnoreCase)) { MailComposeSubjectLabelWidth = f; return true; }
            if (key.Equals("SubjectLabelHeight", StringComparison.OrdinalIgnoreCase)) { MailComposeSubjectLabelHeight = f; return true; }
            if (key.Equals("SubjectFieldOffsetX", StringComparison.OrdinalIgnoreCase)) { MailComposeSubjectFieldOffsetX = f; return true; }
            if (key.Equals("SubjectFieldOffsetY", StringComparison.OrdinalIgnoreCase)) { MailComposeSubjectFieldOffsetY = f; return true; }
            if (key.Equals("SubjectFieldWidth", StringComparison.OrdinalIgnoreCase)) { MailComposeSubjectFieldWidth = f; return true; }
            if (key.Equals("SubjectFieldHeight", StringComparison.OrdinalIgnoreCase)) { MailComposeSubjectFieldHeight = f; return true; }
            if (key.Equals("BodyLabelOffsetX", StringComparison.OrdinalIgnoreCase)) { MailComposeBodyLabelOffsetX = f; return true; }
            if (key.Equals("BodyLabelOffsetY", StringComparison.OrdinalIgnoreCase)) { MailComposeBodyLabelOffsetY = f; return true; }
            if (key.Equals("BodyLabelWidth", StringComparison.OrdinalIgnoreCase)) { MailComposeBodyLabelWidth = f; return true; }
            if (key.Equals("BodyLabelHeight", StringComparison.OrdinalIgnoreCase)) { MailComposeBodyLabelHeight = f; return true; }
            if (key.Equals("BodyFieldOffsetX", StringComparison.OrdinalIgnoreCase)) { MailComposeBodyFieldOffsetX = f; return true; }
            if (key.Equals("BodyFieldOffsetY", StringComparison.OrdinalIgnoreCase)) { MailComposeBodyFieldOffsetY = f; return true; }
            if (key.Equals("BodyFieldWidth", StringComparison.OrdinalIgnoreCase)) { MailComposeBodyFieldWidth = f; return true; }
            if (key.Equals("BodyFieldHeight", StringComparison.OrdinalIgnoreCase)) { MailComposeBodyFieldHeight = f; return true; }
            if (key.Equals("SendButtonOffsetX", StringComparison.OrdinalIgnoreCase)) { MailComposeSendButtonOffsetX = f; return true; }
            if (key.Equals("SendButtonOffsetY", StringComparison.OrdinalIgnoreCase)) { MailComposeSendButtonOffsetY = f; return true; }
            if (key.Equals("SendButtonWidth", StringComparison.OrdinalIgnoreCase)) { MailComposeSendButtonWidth = f; return true; }
            if (key.Equals("SendButtonHeight", StringComparison.OrdinalIgnoreCase)) { MailComposeSendButtonHeight = f; return true; }
            if (key.Equals("CancelButtonOffsetX", StringComparison.OrdinalIgnoreCase)) { MailComposeCancelButtonOffsetX = f; return true; }
            if (key.Equals("CancelButtonOffsetY", StringComparison.OrdinalIgnoreCase)) { MailComposeCancelButtonOffsetY = f; return true; }
            if (key.Equals("CancelButtonWidth", StringComparison.OrdinalIgnoreCase)) { MailComposeCancelButtonWidth = f; return true; }
            if (key.Equals("CancelButtonHeight", StringComparison.OrdinalIgnoreCase)) { MailComposeCancelButtonHeight = f; return true; }
            if (key.Equals("AttachmentLabelOffsetX", StringComparison.OrdinalIgnoreCase)) { MailComposeAttachmentLabelOffsetX = f; return true; }
            if (key.Equals("AttachmentLabelOffsetY", StringComparison.OrdinalIgnoreCase)) { MailComposeAttachmentLabelOffsetY = f; return true; }
            if (key.Equals("AttachmentLabelWidth", StringComparison.OrdinalIgnoreCase)) { MailComposeAttachmentLabelWidth = f; return true; }
            if (key.Equals("AttachmentLabelHeight", StringComparison.OrdinalIgnoreCase)) { MailComposeAttachmentLabelHeight = f; return true; }
            if (key.Equals("AttachmentTypeOffsetX", StringComparison.OrdinalIgnoreCase)) { MailComposeAttachmentTypeOffsetX = f; return true; }
            if (key.Equals("AttachmentTypeOffsetY", StringComparison.OrdinalIgnoreCase)) { MailComposeAttachmentTypeOffsetY = f; return true; }
            if (key.Equals("AttachmentTypeWidth", StringComparison.OrdinalIgnoreCase)) { MailComposeAttachmentTypeWidth = f; return true; }
            if (key.Equals("AttachmentTypeHeight", StringComparison.OrdinalIgnoreCase)) { MailComposeAttachmentTypeHeight = f; return true; }
            if (key.Equals("AttachmentValueOffsetX", StringComparison.OrdinalIgnoreCase)) { MailComposeAttachmentValueOffsetX = f; return true; }
            if (key.Equals("AttachmentValueOffsetY", StringComparison.OrdinalIgnoreCase)) { MailComposeAttachmentValueOffsetY = f; return true; }
            if (key.Equals("AttachmentValueWidth", StringComparison.OrdinalIgnoreCase)) { MailComposeAttachmentValueWidth = f; return true; }
            if (key.Equals("AttachmentValueHeight", StringComparison.OrdinalIgnoreCase)) { MailComposeAttachmentValueHeight = f; return true; }
            if (key.Equals("AttachmentArrowOffsetX", StringComparison.OrdinalIgnoreCase)) { MailComposeAttachmentArrowOffsetX = f; return true; }
            if (key.Equals("AttachmentArrowOffsetY", StringComparison.OrdinalIgnoreCase)) { MailComposeAttachmentArrowOffsetY = f; return true; }
            if (key.Equals("AttachmentArrowWidth", StringComparison.OrdinalIgnoreCase)) { MailComposeAttachmentArrowWidth = f; return true; }
            if (key.Equals("AttachmentDropdownOffsetX", StringComparison.OrdinalIgnoreCase)) { MailComposeAttachmentDropdownOffsetX = f; return true; }
            if (key.Equals("AttachmentDropdownOffsetY", StringComparison.OrdinalIgnoreCase)) { MailComposeAttachmentDropdownOffsetY = f; return true; }
            if (key.Equals("AttachmentDropdownWidth", StringComparison.OrdinalIgnoreCase)) { MailComposeAttachmentDropdownWidth = f; return true; }
            if (key.Equals("AttachmentDropdownHeight", StringComparison.OrdinalIgnoreCase)) { MailComposeAttachmentDropdownHeight = f; return true; }
            return false;
        }

        private void Awake()
        {
            Instance = this;
            LoadConfig();
            _windowRect = new Rect(TradingPostWindowX, TradingPostWindowY, TradingPostWindowWidth, TradingPostWindowHeight);
            ClampGtsWindowToScreen();
            _lastSavedTradingPostWindowRect = _windowRect;
            _filterSearchText = TradingPostDefaultFilterSearchText ?? "";
            _filterOffered = NormalizeFilterValue(TradingPostDefaultFilterOffered, "All");
            _filterRequested = NormalizeFilterValue(TradingPostDefaultFilterRequested, "All");
            _filterType = NormalizeFilterValue(TradingPostDefaultFilterType, "All");
            _filterTimeLeft = NormalizeFilterValue(TradingPostDefaultFilterTimeLeft, "All");
            _filterSeller = TradingPostDefaultFilterSeller ?? "";
            _createListingWindowRect = new Rect(TradingPostCreateWindowX, TradingPostCreateWindowY, TradingPostCreateWindowWidth, TradingPostCreateWindowHeight);
            _mailComposeWindowRect = new Rect(TradingPostWindowX + TradingPostWindowWidth + MailComposeDrawerGap, TradingPostWindowY + MailComposeDrawerOffsetY, MailComposeDrawerWidth, MailComposeDrawerHeight);
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

[Official Server Auth]
# Base64-obfuscated AIO session token cache. Server still validates token, expiry, ban state, and source IP.
CachedSessionToken = 
CachedSessionExpiresUtc = 
CachedSteamID64 = 
CachedAccountUUID = 

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

[Trading Post Create Layout]
# Create-listing layout controls. Change these if the Offered/Requested row needs tuning.
# Width keys affect spacing without needing a rebuild.
CreateLabelWidth = 82
OfferedTypeButtonWidth = 125
OfferedFieldWidth = 220
OfferedSatsFieldWidth = 176
OfferedArrowWidth = 28
RequestTypeButtonWidth = 125
RequestFieldWidth = 220
RequestSatsFieldWidth = 176
RequestArrowWidth = 28
ListPostButtonWidth = 118
CreateRowSpacing = 6
CreateColumnGap = 4
# Positive row/field offsets add extra spacing to the right.
OfferedRowOffsetX = 51
RequestedRowOffsetX = 0
RequestFieldOffsetX = 0
ListPostOffsetX = 0
# Separate Create Listing window. Positive X/Y move the popup right/down.
CreateWindowX = 120
CreateWindowY = 120
CreateWindowWidth = 620
CreateWindowHeight = 410
# Side drawer mode keeps Create Listing attached to the right of Trading Post instead of floating over the table.
CreateUseSideDrawer = true
# Positive gap separates it from the main window. Negative gap slightly overlaps borders so it feels attached.
CreateDrawerGap = -2
# Positive offset moves the drawer down relative to the main Trading Post top.
CreateDrawerOffsetY = 48
# true adds a subtle dark overlay over the main Trading Post while the drawer is open.
CreateDimMainWindow = false
CreateTitleOffsetX = 0
CreateTitleOffsetY = 0
OfferedTypeButtonOffsetX = 0
OfferedTypeButtonOffsetY = 0
OfferedFieldOffsetX = 0
OfferedFieldOffsetY = 0
OfferedArrowOffsetX = 0
OfferedArrowOffsetY = 0
OfferedSatsFieldOffsetX = 0
OfferedSatsFieldOffsetY = 0
RequestTypeButtonOffsetX = 0
RequestTypeButtonOffsetY = 0
RequestFieldOffsetY = 0
RequestArrowOffsetX = 0
RequestArrowOffsetY = 0
RequestSatsFieldOffsetX = 0
RequestSatsFieldOffsetY = 0
ListPostOffsetY = 0
CreateHelpTextOffsetX = 0
CreateHelpTextOffsetY = 0
CreateOpenButtonWidth = 170
CreateOpenButtonHeight = 30
CreateWindowPaddingTop = 6
CreateWindowPaddingLeft = 0
# Dropdown placement/sizing inside the Create Listing popup.
# OfferedKindDropdownOffsetX moves only the small MoN/SATS menu under [Offered: ...].
OfferedKindDropdownOffsetX = 0
OfferedKindDropdownWidth = 150
# OfferedDropdownOffsetX controls the full offered MoN list offset relative to the offered value box.
# 0 = line up with [MON Choose an Offered MoN].
OfferedDropdownOffsetX = 2
OfferedDropdownWidth = 420
OfferedDropdownHeight = 170
RequestKindDropdownOffsetX = 0
RequestKindDropdownWidth = 150
# RequestSpeciesDropdownOffsetX controls the full requested MoN list offset relative to the requested value box.
# 0 = line up with [MON Choose Requested MoN].
RequestSpeciesDropdownOffsetX = 40
RequestSpeciesDropdownWidth = 250
RequestSpeciesDropdownHeight = 170

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
# Table/listing icons shown in the Browse and My Listings auction-house table.
TradingPostTableIconSize = 28
TradingPostTableIconTextGap = 6
TradingPostTableShowOfferedIcon = true
TradingPostTableShowRequestedIcon = true

[Trading Post Browse Grid]
# Positional controls for the auction-house style Browse/My Listings table.
GridOffsetX = 0
GridOffsetY = 0
FilterRowOffsetX = 0
FilterRowOffsetY = 0
RefreshButtonOffsetX = 0
RefreshButtonOffsetY = 0
PagerOffsetX = 0
PagerOffsetY = 0
HeaderOffsetX = 0
HeaderOffsetY = 0
ScrollHeightOffset = 0
TableRowHeight = 58
# The first column now displays ""Offer"" instead of ""Name"". NameColumnWidth remains accepted as an old alias.
OfferColumnWidth = 115
RequestedColumnWidth = 100
TimeLeftColumnWidth = 80
SellerColumnWidth = 105
# v0.10.4 hides the Price / Offer column by default. Set true only for debugging/old layout testing.
ShowPriceOfferColumn = false
PriceOfferColumnWidth = 110
ActionColumnWidth = 150
ActionButtonWidth = 145
ActionButtonHeight = 34
RowOffsetX = 0
RowOffsetY = 0
HeaderOfferOffsetX = 0
HeaderOfferOffsetY = 0
HeaderRequestedOffsetX = 0
HeaderRequestedOffsetY = 0
HeaderTimeLeftOffsetX = 0
HeaderTimeLeftOffsetY = 0
HeaderSellerOffsetX = 0
HeaderSellerOffsetY = 0
HeaderPriceOfferOffsetX = 0
HeaderPriceOfferOffsetY = 0
HeaderActionOffsetX = 0
HeaderActionOffsetY = 0
OfferCellOffsetX = 0
OfferCellOffsetY = 0
RequestedCellOffsetX = 0
RequestedCellOffsetY = 0
TimeLeftCellOffsetX = 0
TimeLeftCellOffsetY = 0
SellerCellOffsetX = 0
SellerCellOffsetY = 0
PriceOfferCellOffsetX = 0
PriceOfferCellOffsetY = 0
ActionCellOffsetX = 0
ActionCellOffsetY = 0
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
RowHeight = 52
SelectedPreviewHeight = 44
ShowIcon = true
IconSize = 36
# Positive X moves right. Positive Y moves down.
IconOffsetX = 0
IconOffsetY = 0
# Text offsets inside the selector row/preview box.
TextOffsetX = 44
TextOffsetY = 0

[RequestedMON]
# Separate tuning for the Requested MoN selector.
RowHeight = 52
SelectedPreviewHeight = 44
ShowIcon = true
IconSize = 36
# Positive X moves right. Positive Y moves down.
IconOffsetX = 0
IconOffsetY = 0
# Text offsets inside the selector row/preview box.
TextOffsetX = 44
TextOffsetY = 0

[Debug]
DebugLogging = true

[CreateListingGroups]
# v0.10.5 grouped movement controls for the Create Listing drawer.
# These move a complete control group together so the button and its dropdown stay aligned.
# OfferedTypeGroupOffset moves [Offered: MoN/SATS] plus the MoN/SATS dropdown below it.
OfferedTypeGroupOffsetX = -50
OfferedTypeGroupOffsetY = 0
# OfferedValueGroupOffset moves the offered value box plus its arrow and full offered-MoN dropdown list.
OfferedValueGroupOffsetX = 37
OfferedValueGroupOffsetY = 0
# RequestedTypeGroupOffset moves [Requested: MoN/SATS] plus the MoN/SATS dropdown below it.
RequestedTypeGroupOffsetX = 0
RequestedTypeGroupOffsetY = 0
# RequestedValueGroupOffset moves the requested value box plus its arrow and full requested-MoN dropdown list.
RequestedValueGroupOffsetX = 0
RequestedValueGroupOffsetY = 0
# ListPostButtonOffset moves only the [List Post] button.
ListPostButtonOffsetX = 0
ListPostButtonOffsetY = 0

[CreateListingDropdowns]
# Fine tuning for dropdowns after the grouped offsets above are set.
# OfferedTypeDropdownOffset moves only the small MoN/SATS dropdown under [Offered: ...].
OfferedKindDropdownOffsetX = 0
OfferedKindDropdownOffsetY = 0
OfferedKindDropdownWidth = 150
# OfferedValueDropdownOffset moves only the full offered-MoN list after the offered value group position.
OfferedValueDropdownOffsetX = 0
OfferedValueDropdownOffsetY = 0
OfferedDropdownWidth = 420
OfferedDropdownHeight = 170
# RequestedTypeDropdownOffset moves only the small MoN/SATS dropdown under [Requested: ...].
RequestKindDropdownOffsetX = 0
RequestKindDropdownOffsetY = 0
RequestKindDropdownWidth = 150
# RequestedValueDropdownOffset moves only the full requested-MoN list after the requested value group position.
RequestedValueDropdownOffsetX = 0
RequestedValueDropdownOffsetY = 0
RequestSpeciesDropdownWidth = 250
RequestSpeciesDropdownHeight = 170

[ListingBoardColumns]
# Header/column controls for the listing board. Widths move following columns because this is a table layout.
OfferColumnWidth = 125
HeaderOfferOffsetX = 0
HeaderOfferOffsetY = 0
RequestedColumnWidth = 135
HeaderRequestedOffsetX = 0
HeaderRequestedOffsetY = 0
TimeLeftColumnWidth = 80
HeaderTimeLeftOffsetX = 0
HeaderTimeLeftOffsetY = 0
SellerColumnWidth = 105
HeaderSellerOffsetX = 0
HeaderSellerOffsetY = 0
ActionColumnWidth = 150
HeaderActionOffsetX = 0
HeaderActionOffsetY = 0
ShowPriceOfferColumn = false

[ListingView]
# Controls for the actual listing rows under the column headers.
TableRowHeight = 58
RowOffsetX = 0
RowOffsetY = 0
# OfferedMonOrSat controls the first row value, whether it is a MoN or SATS amount.
OfferCellOffsetX = 0
OfferCellOffsetY = 0
OfferIconSize = 45
OfferIconOffsetX = 0
OfferIconOffsetY = 0
# RequestedMonOrSat controls the requested row value, whether it is a MoN or SATS amount.
RequestedCellOffsetX = 0
RequestedCellOffsetY = 0
RequestedIconSize = 45
RequestedIconOffsetX = 0
RequestedIconOffsetY = 0
# SATS icons shown in the listing board when the offer/request is currency instead of a MoN.
OfferSatsIconSize = 45
OfferSatsIconOffsetX = 0
OfferSatsIconOffsetY = 0
RequestedSatsIconSize = 45
RequestedSatsIconOffsetX = 0
RequestedSatsIconOffsetY = 0
TimeLeftCellOffsetX = 0
TimeLeftCellOffsetY = 0
SellerCellOffsetX = 0
SellerCellOffsetY = 0
ActionCellOffsetX = 0
ActionCellOffsetY = 0
ActionButtonWidth = 145
ActionButtonHeight = 34

[Trading Post Window]
RememberWindowPosition = true
RememberWindowSize = true
WindowX = 60
WindowY = 60
WindowWidth = 560
WindowHeight = 620

[Trading Post Filters]
SearchText =
OfferedFilter = All
RequestedFilter = All
TypeFilter = All
TimeLeftFilter = All
SellerFilter =

[Trading Post Filter Bar]
# Legacy shared heights remain accepted; per-control values below are the new defaults.
FilterButtonHeight = 30
FilterFieldHeight = 30
SearchFieldHeight = 30
RefreshButtonHeight = 32
# Search name field.
SearchOffsetX = 0
SearchOffsetY = 0
SearchWidth = 120
SearchHeight = 32
# Offered filter button.
OfferedButtonOffsetX = 0
OfferedButtonOffsetY = 0
OfferedButtonWidth = 92
OfferedButtonHeight = 32
# Requested filter button.
RequestedButtonOffsetX = 0
RequestedButtonOffsetY = 0
RequestedButtonWidth = 105
RequestedButtonHeight = 32
# Type filter button.
TypeButtonOffsetX = 0
TypeButtonOffsetY = 0
TypeButtonWidth = 110
TypeButtonHeight = 32
# Time Left filter button.
TimeLeftButtonOffsetX = 0
TimeLeftButtonOffsetY = 0
TimeLeftButtonWidth = 100
TimeLeftButtonHeight = 32
# Seller text field.
SellerOffsetX = 0
SellerOffsetY = 0
SellerWidth = 90
SellerHeight = 32
# Refresh button.
RefreshButtonOffsetX = 0
RefreshButtonOffsetY = 0
RefreshButtonWidth = 90

[Mailbox Compose Drawer]
# Compose Mail opens as an attached side drawer like Create Listing.
ComposeButtonWidth = 120
ComposeButtonHeight = 30
DrawerWidth = 440
DrawerHeight = 430
DrawerGap = -2
DrawerOffsetY = 48
TitleOffsetX = 0
TitleOffsetY = 0
ToLabelOffsetX = 0
ToLabelOffsetY = 0
ToLabelWidth = 54
ToLabelHeight = 24
ToFieldOffsetX = 0
ToFieldOffsetY = 0
ToFieldWidth = 260
ToFieldHeight = 28
SubjectLabelOffsetX = 0
SubjectLabelOffsetY = 0
SubjectLabelWidth = 54
SubjectLabelHeight = 24
SubjectFieldOffsetX = 0
SubjectFieldOffsetY = 0
SubjectFieldWidth = 300
SubjectFieldHeight = 28
BodyLabelOffsetX = 0
BodyLabelOffsetY = 0
BodyLabelWidth = 54
BodyLabelHeight = 24
BodyFieldOffsetX = 0
BodyFieldOffsetY = 0
BodyFieldWidth = 360
BodyFieldHeight = 160
SendButtonOffsetX = 0
SendButtonOffsetY = 0
SendButtonWidth = 90
SendButtonHeight = 30
CancelButtonOffsetX = 0
CancelButtonOffsetY = 0
CancelButtonWidth = 90
CancelButtonHeight = 30
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
                    if ((currentSection.Equals("Trading Post Create Layout", StringComparison.OrdinalIgnoreCase)
                            || currentSection.Equals("TradingPostCreateLayout", StringComparison.OrdinalIgnoreCase))
                        && !key.StartsWith("TradingPost", StringComparison.OrdinalIgnoreCase))
                    {
                        key = "TradingPost" + key;
                    }
                    else if (currentSection.Equals("OfferedMON", StringComparison.OrdinalIgnoreCase)
                        && !key.StartsWith("OfferedMON", StringComparison.OrdinalIgnoreCase))
                    {
                        key = "OfferedMON" + key;
                    }
                    else if (currentSection.Equals("RequestedMON", StringComparison.OrdinalIgnoreCase)
                        && !key.StartsWith("RequestedMON", StringComparison.OrdinalIgnoreCase))
                    {
                        key = "RequestedMON" + key;
                    }
                    else if ((currentSection.Equals("Trading Post Listing Table", StringComparison.OrdinalIgnoreCase)
                            || currentSection.Equals("TradingPostListingTable", StringComparison.OrdinalIgnoreCase))
                        && !key.StartsWith("TradingPostTable", StringComparison.OrdinalIgnoreCase))
                    {
                        key = "TradingPost" + key;
                    }
                    if (key.Equals("Host", StringComparison.OrdinalIgnoreCase)) ServerHost = value;
                    else if (key.Equals("Port", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out int port)) ServerPort = port;
                    else if (key.Equals("AutoOpenBrowser", StringComparison.OrdinalIgnoreCase) && bool.TryParse(value, out bool aob)) AutoOpenSteamLogin = aob;
                    else if (key.Equals("CachedSessionToken", StringComparison.OrdinalIgnoreCase)) CachedOfficialSessionTokenBase64 = value;
                    else if (key.Equals("CachedSessionExpiresUtc", StringComparison.OrdinalIgnoreCase)) CachedOfficialSessionExpiresUtc = value;
                    else if (key.Equals("CachedSteamID64", StringComparison.OrdinalIgnoreCase)) CachedOfficialSteamID64 = value;
                    else if (key.Equals("CachedAccountUUID", StringComparison.OrdinalIgnoreCase)) CachedOfficialAccountUUID = value;
                    else if ((key.Equals("TradingPostRememberWindowPosition", StringComparison.OrdinalIgnoreCase) || (currentSection.Equals("Trading Post Window", StringComparison.OrdinalIgnoreCase) && key.Equals("RememberWindowPosition", StringComparison.OrdinalIgnoreCase))) && bool.TryParse(value, out bool tprwp)) TradingPostRememberWindowPosition = tprwp;
                    else if ((key.Equals("TradingPostRememberWindowSize", StringComparison.OrdinalIgnoreCase) || (currentSection.Equals("Trading Post Window", StringComparison.OrdinalIgnoreCase) && key.Equals("RememberWindowSize", StringComparison.OrdinalIgnoreCase))) && bool.TryParse(value, out bool tprws)) TradingPostRememberWindowSize = tprws;
                    else if ((key.Equals("TradingPostWindowX", StringComparison.OrdinalIgnoreCase) || (currentSection.Equals("Trading Post Window", StringComparison.OrdinalIgnoreCase) && key.Equals("WindowX", StringComparison.OrdinalIgnoreCase))) && float.TryParse(value, out float tpwx)) TradingPostWindowX = tpwx;
                    else if ((key.Equals("TradingPostWindowY", StringComparison.OrdinalIgnoreCase) || (currentSection.Equals("Trading Post Window", StringComparison.OrdinalIgnoreCase) && key.Equals("WindowY", StringComparison.OrdinalIgnoreCase))) && float.TryParse(value, out float tpwy)) TradingPostWindowY = tpwy;
                    else if ((key.Equals("TradingPostWindowWidth", StringComparison.OrdinalIgnoreCase) || (currentSection.Equals("Trading Post Window", StringComparison.OrdinalIgnoreCase) && key.Equals("WindowWidth", StringComparison.OrdinalIgnoreCase))) && float.TryParse(value, out float tpww)) TradingPostWindowWidth = tpww;
                    else if ((key.Equals("TradingPostWindowHeight", StringComparison.OrdinalIgnoreCase) || (currentSection.Equals("Trading Post Window", StringComparison.OrdinalIgnoreCase) && key.Equals("WindowHeight", StringComparison.OrdinalIgnoreCase))) && float.TryParse(value, out float tpwh)) TradingPostWindowHeight = tpwh;
                    else if ((key.Equals("TradingPostFilterSearchText", StringComparison.OrdinalIgnoreCase) || (currentSection.Equals("Trading Post Filters", StringComparison.OrdinalIgnoreCase) && key.Equals("SearchText", StringComparison.OrdinalIgnoreCase))) ) TradingPostDefaultFilterSearchText = value;
                    else if ((key.Equals("TradingPostFilterOffered", StringComparison.OrdinalIgnoreCase) || (currentSection.Equals("Trading Post Filters", StringComparison.OrdinalIgnoreCase) && key.Equals("OfferedFilter", StringComparison.OrdinalIgnoreCase))) ) TradingPostDefaultFilterOffered = value;
                    else if ((key.Equals("TradingPostFilterRequested", StringComparison.OrdinalIgnoreCase) || (currentSection.Equals("Trading Post Filters", StringComparison.OrdinalIgnoreCase) && key.Equals("RequestedFilter", StringComparison.OrdinalIgnoreCase))) ) TradingPostDefaultFilterRequested = value;
                    else if ((key.Equals("TradingPostFilterType", StringComparison.OrdinalIgnoreCase) || (currentSection.Equals("Trading Post Filters", StringComparison.OrdinalIgnoreCase) && key.Equals("TypeFilter", StringComparison.OrdinalIgnoreCase))) ) TradingPostDefaultFilterType = value;
                    else if ((key.Equals("TradingPostFilterTimeLeft", StringComparison.OrdinalIgnoreCase) || (currentSection.Equals("Trading Post Filters", StringComparison.OrdinalIgnoreCase) && key.Equals("TimeLeftFilter", StringComparison.OrdinalIgnoreCase))) ) TradingPostDefaultFilterTimeLeft = value;
                    else if ((key.Equals("TradingPostFilterSeller", StringComparison.OrdinalIgnoreCase) || (currentSection.Equals("Trading Post Filters", StringComparison.OrdinalIgnoreCase) && key.Equals("SellerFilter", StringComparison.OrdinalIgnoreCase))) ) TradingPostDefaultFilterSeller = value;
                    else if ((key.Equals("TradingPostFilterButtonHeight", StringComparison.OrdinalIgnoreCase) || (currentSection.Equals("Trading Post Filter Bar", StringComparison.OrdinalIgnoreCase) && key.Equals("FilterButtonHeight", StringComparison.OrdinalIgnoreCase))) && float.TryParse(value, out float tpfbh)) TradingPostFilterButtonHeight = tpfbh;
                    else if ((key.Equals("TradingPostFilterFieldHeight", StringComparison.OrdinalIgnoreCase) || (currentSection.Equals("Trading Post Filter Bar", StringComparison.OrdinalIgnoreCase) && key.Equals("FilterFieldHeight", StringComparison.OrdinalIgnoreCase))) && float.TryParse(value, out float tpffh)) TradingPostFilterFieldHeight = tpffh;
                    else if ((key.Equals("TradingPostSearchFieldHeight", StringComparison.OrdinalIgnoreCase) || (currentSection.Equals("Trading Post Filter Bar", StringComparison.OrdinalIgnoreCase) && key.Equals("SearchFieldHeight", StringComparison.OrdinalIgnoreCase))) && float.TryParse(value, out float tpsfh)) TradingPostSearchFieldHeight = tpsfh;
                    else if ((key.Equals("TradingPostRefreshButtonHeight", StringComparison.OrdinalIgnoreCase) || (currentSection.Equals("Trading Post Filter Bar", StringComparison.OrdinalIgnoreCase) && key.Equals("RefreshButtonHeight", StringComparison.OrdinalIgnoreCase))) && float.TryParse(value, out float tprfh)) { TradingPostRefreshButtonHeight = tprfh; TradingPostRefreshFilterHeight = tprfh; }
                    else if (currentSection.Equals("Trading Post Filter Bar", StringComparison.OrdinalIgnoreCase) && key.Equals("SearchOffsetX", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float tpsx)) TradingPostSearchOffsetX = tpsx;
                    else if (currentSection.Equals("Trading Post Filter Bar", StringComparison.OrdinalIgnoreCase) && key.Equals("SearchOffsetY", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float tpsy)) TradingPostSearchOffsetY = tpsy;
                    else if (currentSection.Equals("Trading Post Filter Bar", StringComparison.OrdinalIgnoreCase) && key.Equals("SearchWidth", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float tpsw)) TradingPostSearchWidth = tpsw;
                    else if (currentSection.Equals("Trading Post Filter Bar", StringComparison.OrdinalIgnoreCase) && key.Equals("SearchHeight", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float tpsh)) TradingPostSearchHeight = tpsh;
                    else if (currentSection.Equals("Trading Post Filter Bar", StringComparison.OrdinalIgnoreCase) && key.Equals("OfferedButtonOffsetX", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float tpobx)) TradingPostOfferedFilterOffsetX = tpobx;
                    else if (currentSection.Equals("Trading Post Filter Bar", StringComparison.OrdinalIgnoreCase) && key.Equals("OfferedButtonOffsetY", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float tpoby)) TradingPostOfferedFilterOffsetY = tpoby;
                    else if (currentSection.Equals("Trading Post Filter Bar", StringComparison.OrdinalIgnoreCase) && key.Equals("OfferedButtonWidth", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float tpobw)) TradingPostOfferedFilterWidth = tpobw;
                    else if (currentSection.Equals("Trading Post Filter Bar", StringComparison.OrdinalIgnoreCase) && key.Equals("OfferedButtonHeight", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float tpobh)) TradingPostOfferedFilterHeight = tpobh;
                    else if (currentSection.Equals("Trading Post Filter Bar", StringComparison.OrdinalIgnoreCase) && key.Equals("RequestedButtonOffsetX", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float tprbx)) TradingPostRequestedFilterOffsetX = tprbx;
                    else if (currentSection.Equals("Trading Post Filter Bar", StringComparison.OrdinalIgnoreCase) && key.Equals("RequestedButtonOffsetY", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float tprby)) TradingPostRequestedFilterOffsetY = tprby;
                    else if (currentSection.Equals("Trading Post Filter Bar", StringComparison.OrdinalIgnoreCase) && key.Equals("RequestedButtonWidth", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float tprbw)) TradingPostRequestedFilterWidth = tprbw;
                    else if (currentSection.Equals("Trading Post Filter Bar", StringComparison.OrdinalIgnoreCase) && key.Equals("RequestedButtonHeight", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float tprbh)) TradingPostRequestedFilterHeight = tprbh;
                    else if (currentSection.Equals("Trading Post Filter Bar", StringComparison.OrdinalIgnoreCase) && key.Equals("TypeButtonOffsetX", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float tptbx)) TradingPostTypeFilterOffsetX = tptbx;
                    else if (currentSection.Equals("Trading Post Filter Bar", StringComparison.OrdinalIgnoreCase) && key.Equals("TypeButtonOffsetY", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float tptby)) TradingPostTypeFilterOffsetY = tptby;
                    else if (currentSection.Equals("Trading Post Filter Bar", StringComparison.OrdinalIgnoreCase) && key.Equals("TypeButtonWidth", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float tptbw)) TradingPostTypeFilterWidth = tptbw;
                    else if (currentSection.Equals("Trading Post Filter Bar", StringComparison.OrdinalIgnoreCase) && key.Equals("TypeButtonHeight", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float tptbh)) TradingPostTypeFilterHeight = tptbh;
                    else if (currentSection.Equals("Trading Post Filter Bar", StringComparison.OrdinalIgnoreCase) && key.Equals("TimeLeftButtonOffsetX", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float tptix)) TradingPostTimeFilterOffsetX = tptix;
                    else if (currentSection.Equals("Trading Post Filter Bar", StringComparison.OrdinalIgnoreCase) && key.Equals("TimeLeftButtonOffsetY", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float tptiy)) TradingPostTimeFilterOffsetY = tptiy;
                    else if (currentSection.Equals("Trading Post Filter Bar", StringComparison.OrdinalIgnoreCase) && key.Equals("TimeLeftButtonWidth", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float tptiw)) TradingPostTimeFilterWidth = tptiw;
                    else if (currentSection.Equals("Trading Post Filter Bar", StringComparison.OrdinalIgnoreCase) && key.Equals("TimeLeftButtonHeight", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float tptih)) TradingPostTimeFilterHeight = tptih;
                    else if (currentSection.Equals("Trading Post Filter Bar", StringComparison.OrdinalIgnoreCase) && key.Equals("SellerOffsetX", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float tpslx)) TradingPostSellerFilterOffsetX = tpslx;
                    else if (currentSection.Equals("Trading Post Filter Bar", StringComparison.OrdinalIgnoreCase) && key.Equals("SellerOffsetY", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float tpsly)) TradingPostSellerFilterOffsetY = tpsly;
                    else if (currentSection.Equals("Trading Post Filter Bar", StringComparison.OrdinalIgnoreCase) && key.Equals("SellerWidth", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float tpslw)) TradingPostSellerFilterWidth = tpslw;
                    else if (currentSection.Equals("Trading Post Filter Bar", StringComparison.OrdinalIgnoreCase) && key.Equals("SellerHeight", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float tpslh)) TradingPostSellerFilterHeight = tpslh;
                    else if (currentSection.Equals("Trading Post Filter Bar", StringComparison.OrdinalIgnoreCase) && key.Equals("RefreshButtonOffsetX", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float tpflx)) TradingPostRefreshFilterOffsetX = tpflx;
                    else if (currentSection.Equals("Trading Post Filter Bar", StringComparison.OrdinalIgnoreCase) && key.Equals("RefreshButtonOffsetY", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float tpfly)) TradingPostRefreshFilterOffsetY = tpfly;
                    else if (currentSection.Equals("Trading Post Filter Bar", StringComparison.OrdinalIgnoreCase) && key.Equals("RefreshButtonWidth", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float tpflw)) TradingPostRefreshFilterWidth = tpflw;
                    else if (currentSection.Equals("Mailbox Compose Drawer", StringComparison.OrdinalIgnoreCase) && key.Equals("ComposeButtonWidth", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float mcbw)) MailComposeOpenButtonWidth = mcbw;
                    else if (currentSection.Equals("Mailbox Compose Drawer", StringComparison.OrdinalIgnoreCase) && key.Equals("ComposeButtonHeight", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float mcbh)) MailComposeOpenButtonHeight = mcbh;
                    else if (currentSection.Equals("Mailbox Compose Drawer", StringComparison.OrdinalIgnoreCase) && key.Equals("DrawerWidth", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float mcdw)) MailComposeDrawerWidth = mcdw;
                    else if (currentSection.Equals("Mailbox Compose Drawer", StringComparison.OrdinalIgnoreCase) && key.Equals("DrawerHeight", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float mcdh)) MailComposeDrawerHeight = mcdh;
                    else if (currentSection.Equals("Mailbox Compose Drawer", StringComparison.OrdinalIgnoreCase) && key.Equals("DrawerGap", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float mcdg)) MailComposeDrawerGap = mcdg;
                    else if (currentSection.Equals("Mailbox Compose Drawer", StringComparison.OrdinalIgnoreCase) && key.Equals("DrawerOffsetY", StringComparison.OrdinalIgnoreCase) && float.TryParse(value, out float mcdy)) MailComposeDrawerOffsetY = mcdy;
                    else if (currentSection.Equals("Mailbox Compose Drawer", StringComparison.OrdinalIgnoreCase)) TryParseMailboxComposeLayoutValue(key, value);
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
                    else if ((key.Equals("TradingPostCreateLabelWidth", StringComparison.OrdinalIgnoreCase) || key.Equals("CreateLabelWidth", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpclw)) TradingPostCreateLabelWidth = tpclw;
                    else if ((key.Equals("TradingPostOfferedTypeButtonWidth", StringComparison.OrdinalIgnoreCase) || key.Equals("OfferedTypeButtonWidth", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpotbw)) TradingPostOfferedTypeButtonWidth = tpotbw;
                    else if ((key.Equals("TradingPostOfferedFieldWidth", StringComparison.OrdinalIgnoreCase) || key.Equals("OfferedFieldWidth", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpofw)) TradingPostOfferedFieldWidth = tpofw;
                    else if ((key.Equals("TradingPostOfferedSatsFieldWidth", StringComparison.OrdinalIgnoreCase) || key.Equals("OfferedSatsFieldWidth", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tposfw)) TradingPostOfferedSatsFieldWidth = tposfw;
                    else if ((key.Equals("TradingPostOfferedArrowWidth", StringComparison.OrdinalIgnoreCase) || key.Equals("OfferedArrowWidth", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpoaw)) TradingPostOfferedArrowWidth = tpoaw;
                    else if ((key.Equals("TradingPostRequestTypeButtonWidth", StringComparison.OrdinalIgnoreCase) || key.Equals("RequestTypeButtonWidth", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tprtbw)) TradingPostRequestTypeButtonWidth = tprtbw;
                    else if ((key.Equals("TradingPostRequestFieldWidth", StringComparison.OrdinalIgnoreCase) || key.Equals("RequestFieldWidth", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tprfw)) TradingPostRequestFieldWidth = tprfw;
                    else if ((key.Equals("TradingPostRequestSatsFieldWidth", StringComparison.OrdinalIgnoreCase) || key.Equals("RequestSatsFieldWidth", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tprsfw)) TradingPostRequestSatsFieldWidth = tprsfw;
                    else if ((key.Equals("TradingPostRequestArrowWidth", StringComparison.OrdinalIgnoreCase) || key.Equals("RequestArrowWidth", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpraw)) TradingPostRequestArrowWidth = tpraw;
                    else if ((key.Equals("TradingPostListPostButtonWidth", StringComparison.OrdinalIgnoreCase) || key.Equals("ListPostButtonWidth", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tplpbw)) TradingPostListPostButtonWidth = tplpbw;
                    else if ((key.Equals("TradingPostCreateRowSpacing", StringComparison.OrdinalIgnoreCase) || key.Equals("CreateRowSpacing", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpcrs)) TradingPostCreateRowSpacing = tpcrs;
                    else if ((key.Equals("TradingPostCreateColumnGap", StringComparison.OrdinalIgnoreCase) || key.Equals("CreateColumnGap", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpccg)) TradingPostCreateColumnGap = tpccg;
                    else if ((key.Equals("TradingPostOfferedRowOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("OfferedRowOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tporox)) TradingPostOfferedRowOffsetX = tporox;
                    else if ((key.Equals("TradingPostRequestedRowOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("RequestedRowOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tprrox)) TradingPostRequestedRowOffsetX = tprrox;
                    else if ((key.Equals("TradingPostRequestFieldOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("RequestFieldOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tprfox)) TradingPostRequestFieldOffsetX = tprfox;
                    else if ((key.Equals("TradingPostListPostOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("ListPostOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tplpox)) TradingPostListPostOffsetX = tplpox;
                    else if ((key.Equals("TradingPostCreateWindowX", StringComparison.OrdinalIgnoreCase) || key.Equals("CreateWindowX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpcwx)) TradingPostCreateWindowX = tpcwx;
                    else if ((key.Equals("TradingPostCreateWindowY", StringComparison.OrdinalIgnoreCase) || key.Equals("CreateWindowY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpcwy)) TradingPostCreateWindowY = tpcwy;
                    else if ((key.Equals("TradingPostCreateWindowWidth", StringComparison.OrdinalIgnoreCase) || key.Equals("CreateWindowWidth", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpcww)) TradingPostCreateWindowWidth = tpcww;
                    else if ((key.Equals("TradingPostCreateWindowHeight", StringComparison.OrdinalIgnoreCase) || key.Equals("CreateWindowHeight", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpcwh)) TradingPostCreateWindowHeight = tpcwh;
                    else if ((key.Equals("TradingPostCreateUseSideDrawer", StringComparison.OrdinalIgnoreCase) || key.Equals("CreateUseSideDrawer", StringComparison.OrdinalIgnoreCase)) && bool.TryParse(value, out bool tpcusd)) TradingPostCreateUseSideDrawer = tpcusd;
                    else if ((key.Equals("TradingPostCreateDrawerGap", StringComparison.OrdinalIgnoreCase) || key.Equals("CreateDrawerGap", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpcdg)) TradingPostCreateDrawerGap = tpcdg;
                    else if ((key.Equals("TradingPostCreateDrawerOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("CreateDrawerOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpcdoy)) TradingPostCreateDrawerOffsetY = tpcdoy;
                    else if ((key.Equals("TradingPostCreateDimMainWindow", StringComparison.OrdinalIgnoreCase) || key.Equals("CreateDimMainWindow", StringComparison.OrdinalIgnoreCase)) && bool.TryParse(value, out bool tpcdmw)) TradingPostCreateDimMainWindow = tpcdmw;
                    else if ((key.Equals("TradingPostCreateOpenButtonWidth", StringComparison.OrdinalIgnoreCase) || key.Equals("CreateOpenButtonWidth", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpcobw)) TradingPostCreateOpenButtonWidth = tpcobw;
                    else if ((key.Equals("TradingPostCreateOpenButtonHeight", StringComparison.OrdinalIgnoreCase) || key.Equals("CreateOpenButtonHeight", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpcobh)) TradingPostCreateOpenButtonHeight = tpcobh;
                    else if ((key.Equals("TradingPostCreateWindowPaddingTop", StringComparison.OrdinalIgnoreCase) || key.Equals("CreateWindowPaddingTop", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpcwpt)) TradingPostCreateWindowPaddingTop = tpcwpt;
                    else if ((key.Equals("TradingPostCreateWindowPaddingLeft", StringComparison.OrdinalIgnoreCase) || key.Equals("CreateWindowPaddingLeft", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpcwpl)) TradingPostCreateWindowPaddingLeft = tpcwpl;
                    else if ((key.Equals("TradingPostOfferedDropdownOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("OfferedDropdownOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpodox)) TradingPostOfferedDropdownOffsetX = tpodox;
                    else if ((key.Equals("TradingPostOfferedDropdownWidth", StringComparison.OrdinalIgnoreCase) || key.Equals("OfferedDropdownWidth", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpodw)) TradingPostOfferedDropdownWidth = tpodw;
                    else if ((key.Equals("TradingPostOfferedDropdownHeight", StringComparison.OrdinalIgnoreCase) || key.Equals("OfferedDropdownHeight", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpodh)) TradingPostOfferedDropdownHeight = tpodh;
                    else if ((key.Equals("TradingPostRequestKindDropdownOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("RequestKindDropdownOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tprkdox)) TradingPostRequestKindDropdownOffsetX = tprkdox;
                    else if ((key.Equals("TradingPostRequestKindDropdownWidth", StringComparison.OrdinalIgnoreCase) || key.Equals("RequestKindDropdownWidth", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tprkdw)) TradingPostRequestKindDropdownWidth = tprkdw;
                    else if ((key.Equals("TradingPostRequestSpeciesDropdownOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("RequestSpeciesDropdownOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tprsodox)) TradingPostRequestSpeciesDropdownOffsetX = tprsodox;
                    else if ((key.Equals("TradingPostRequestSpeciesDropdownWidth", StringComparison.OrdinalIgnoreCase) || key.Equals("RequestSpeciesDropdownWidth", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tprsdw)) TradingPostRequestSpeciesDropdownWidth = tprsdw;
                    else if ((key.Equals("TradingPostRequestSpeciesDropdownHeight", StringComparison.OrdinalIgnoreCase) || key.Equals("RequestSpeciesDropdownHeight", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tprsdh)) TradingPostRequestSpeciesDropdownHeight = tprsdh;
                    else if ((key.Equals("TradingPostTableIconSize", StringComparison.OrdinalIgnoreCase) || key.Equals("TableIconSize", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tptis)) TradingPostTableIconSize = tptis;
                    else if ((key.Equals("TradingPostTableIconTextGap", StringComparison.OrdinalIgnoreCase) || key.Equals("TableIconTextGap", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tptitg)) TradingPostTableIconTextGap = tptitg;
                    else if ((key.Equals("TradingPostTableShowOfferedIcon", StringComparison.OrdinalIgnoreCase) || key.Equals("TableShowOfferedIcon", StringComparison.OrdinalIgnoreCase)) && bool.TryParse(value, out bool tptsoi)) TradingPostTableShowOfferedIcon = tptsoi;
                    else if ((key.Equals("TradingPostTableShowRequestedIcon", StringComparison.OrdinalIgnoreCase) || key.Equals("TableShowRequestedIcon", StringComparison.OrdinalIgnoreCase)) && bool.TryParse(value, out bool tptsri)) TradingPostTableShowRequestedIcon = tptsri;
                    else if ((key.Equals("TradingPostOfferIconSize", StringComparison.OrdinalIgnoreCase) || key.Equals("OfferIconSize", StringComparison.OrdinalIgnoreCase) || key.Equals("OfferedIconSize", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpois)) TradingPostOfferIconSize = tpois;
                    else if ((key.Equals("TradingPostOfferIconOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("OfferIconOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("OfferedIconOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpoiox)) TradingPostOfferIconOffsetX = tpoiox;
                    else if ((key.Equals("TradingPostOfferIconOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("OfferIconOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("OfferedIconOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpoioy)) TradingPostOfferIconOffsetY = tpoioy;
                    else if ((key.Equals("TradingPostRequestedIconSize", StringComparison.OrdinalIgnoreCase) || key.Equals("RequestedIconSize", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpris)) TradingPostRequestedIconSize = tpris;
                    else if ((key.Equals("TradingPostRequestedIconOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("RequestedIconOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpriox)) TradingPostRequestedIconOffsetX = tpriox;
                    else if ((key.Equals("TradingPostRequestedIconOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("RequestedIconOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tprioy)) TradingPostRequestedIconOffsetY = tprioy;
                    else if ((key.Equals("TradingPostOfferSatsIconSize", StringComparison.OrdinalIgnoreCase) || key.Equals("OfferSatsIconSize", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tposis)) TradingPostOfferSatsIconSize = tposis;
                    else if ((key.Equals("TradingPostOfferSatsIconOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("OfferSatsIconOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tposiox)) TradingPostOfferSatsIconOffsetX = tposiox;
                    else if ((key.Equals("TradingPostOfferSatsIconOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("OfferSatsIconOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tposioy)) TradingPostOfferSatsIconOffsetY = tposioy;
                    else if ((key.Equals("TradingPostRequestedSatsIconSize", StringComparison.OrdinalIgnoreCase) || key.Equals("RequestedSatsIconSize", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tprsis)) TradingPostRequestedSatsIconSize = tprsis;
                    else if ((key.Equals("TradingPostRequestedSatsIconOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("RequestedSatsIconOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tprsiox)) TradingPostRequestedSatsIconOffsetX = tprsiox;
                    else if ((key.Equals("TradingPostRequestedSatsIconOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("RequestedSatsIconOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tprsioy)) TradingPostRequestedSatsIconOffsetY = tprsioy;
                    else if ((key.Equals("TradingPostOfferedKindDropdownOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("OfferedKindDropdownOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpokdox)) TradingPostOfferedKindDropdownOffsetX = tpokdox;
                    else if ((key.Equals("TradingPostOfferedKindDropdownWidth", StringComparison.OrdinalIgnoreCase) || key.Equals("OfferedKindDropdownWidth", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpokdw)) TradingPostOfferedKindDropdownWidth = tpokdw;
                    else if ((key.Equals("TradingPostBrowseGridOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("GridOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpgox)) TradingPostBrowseGridOffsetX = tpgox;
                    else if ((key.Equals("TradingPostBrowseGridOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("GridOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpgoy)) TradingPostBrowseGridOffsetY = tpgoy;
                    else if ((key.Equals("TradingPostFilterRowOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("FilterRowOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpfox)) TradingPostFilterRowOffsetX = tpfox;
                    else if ((key.Equals("TradingPostFilterRowOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("FilterRowOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpfoy)) TradingPostFilterRowOffsetY = tpfoy;
                    else if ((key.Equals("TradingPostRefreshButtonOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("RefreshButtonOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tprbox)) TradingPostRefreshButtonOffsetX = tprbox;
                    else if ((key.Equals("TradingPostRefreshButtonOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("RefreshButtonOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tprboy)) TradingPostRefreshButtonOffsetY = tprboy;
                    else if ((key.Equals("TradingPostPagerOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("PagerOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tppox)) TradingPostPagerOffsetX = tppox;
                    else if ((key.Equals("TradingPostPagerOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("PagerOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tppoy)) TradingPostPagerOffsetY = tppoy;
                    else if ((key.Equals("TradingPostGridHeaderOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("HeaderOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpghox)) TradingPostGridHeaderOffsetX = tpghox;
                    else if ((key.Equals("TradingPostGridHeaderOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("HeaderOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpghoy)) TradingPostGridHeaderOffsetY = tpghoy;
                    else if ((key.Equals("TradingPostGridScrollHeightOffset", StringComparison.OrdinalIgnoreCase) || key.Equals("ScrollHeightOffset", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpgsho)) TradingPostGridScrollHeightOffset = tpgsho;
                    else if ((key.Equals("TradingPostTableRowHeight", StringComparison.OrdinalIgnoreCase) || key.Equals("TableRowHeight", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tptrh)) TradingPostTableRowHeight = tptrh;
                    else if ((key.Equals("TradingPostNameColumnWidth", StringComparison.OrdinalIgnoreCase) || key.Equals("NameColumnWidth", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpncw)) { TradingPostNameColumnWidth = tpncw; TradingPostOfferColumnWidth = tpncw; }
                    else if ((key.Equals("TradingPostOfferColumnWidth", StringComparison.OrdinalIgnoreCase) || key.Equals("OfferColumnWidth", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpocw)) TradingPostOfferColumnWidth = tpocw;
                    else if ((key.Equals("TradingPostRequestedColumnWidth", StringComparison.OrdinalIgnoreCase) || key.Equals("RequestedColumnWidth", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tprcw)) TradingPostRequestedColumnWidth = tprcw;
                    else if ((key.Equals("TradingPostTimeLeftColumnWidth", StringComparison.OrdinalIgnoreCase) || key.Equals("TimeLeftColumnWidth", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tptlcw)) TradingPostTimeLeftColumnWidth = tptlcw;
                    else if ((key.Equals("TradingPostSellerColumnWidth", StringComparison.OrdinalIgnoreCase) || key.Equals("SellerColumnWidth", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpscw)) TradingPostSellerColumnWidth = tpscw;
                    else if ((key.Equals("TradingPostShowPriceOfferColumn", StringComparison.OrdinalIgnoreCase) || key.Equals("ShowPriceOfferColumn", StringComparison.OrdinalIgnoreCase)) && bool.TryParse(value, out bool tpspoc)) TradingPostShowPriceOfferColumn = tpspoc;
                    else if ((key.Equals("TradingPostPriceOfferColumnWidth", StringComparison.OrdinalIgnoreCase) || key.Equals("PriceOfferColumnWidth", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tppocw)) TradingPostPriceOfferColumnWidth = tppocw;
                    else if ((key.Equals("TradingPostActionColumnWidth", StringComparison.OrdinalIgnoreCase) || key.Equals("ActionColumnWidth", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpacw)) TradingPostActionColumnWidth = tpacw;
                    else if ((key.Equals("TradingPostActionButtonWidth", StringComparison.OrdinalIgnoreCase) || key.Equals("ActionButtonWidth", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpabw)) TradingPostActionButtonWidth = tpabw;
                    else if ((key.Equals("TradingPostActionButtonHeight", StringComparison.OrdinalIgnoreCase) || key.Equals("ActionButtonHeight", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpabh)) TradingPostActionButtonHeight = tpabh;
                    else if ((key.Equals("TradingPostRowOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("RowOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tprox)) TradingPostRowOffsetX = tprox;
                    else if ((key.Equals("TradingPostRowOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("RowOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tproy)) TradingPostRowOffsetY = tproy;
                    else if ((key.Equals("TradingPostHeaderOfferOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("HeaderOfferOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tphoox)) TradingPostHeaderOfferOffsetX = tphoox;
                    else if ((key.Equals("TradingPostHeaderOfferOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("HeaderOfferOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tphooy)) TradingPostHeaderOfferOffsetY = tphooy;
                    else if ((key.Equals("TradingPostHeaderRequestedOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("HeaderRequestedOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tphrox)) TradingPostHeaderRequestedOffsetX = tphrox;
                    else if ((key.Equals("TradingPostHeaderRequestedOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("HeaderRequestedOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tphroy)) TradingPostHeaderRequestedOffsetY = tphroy;
                    else if ((key.Equals("TradingPostHeaderTimeLeftOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("HeaderTimeLeftOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tphtox)) TradingPostHeaderTimeLeftOffsetX = tphtox;
                    else if ((key.Equals("TradingPostHeaderTimeLeftOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("HeaderTimeLeftOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tphtoy)) TradingPostHeaderTimeLeftOffsetY = tphtoy;
                    else if ((key.Equals("TradingPostHeaderSellerOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("HeaderSellerOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tphsox)) TradingPostHeaderSellerOffsetX = tphsox;
                    else if ((key.Equals("TradingPostHeaderSellerOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("HeaderSellerOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tphsoy)) TradingPostHeaderSellerOffsetY = tphsoy;
                    else if ((key.Equals("TradingPostHeaderPriceOfferOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("HeaderPriceOfferOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tphpox)) TradingPostHeaderPriceOfferOffsetX = tphpox;
                    else if ((key.Equals("TradingPostHeaderPriceOfferOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("HeaderPriceOfferOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tphpoy)) TradingPostHeaderPriceOfferOffsetY = tphpoy;
                    else if ((key.Equals("TradingPostHeaderActionOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("HeaderActionOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tphaox)) TradingPostHeaderActionOffsetX = tphaox;
                    else if ((key.Equals("TradingPostHeaderActionOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("HeaderActionOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tphaoy)) TradingPostHeaderActionOffsetY = tphaoy;
                    else if ((key.Equals("TradingPostOfferCellOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("OfferCellOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("OfferedMonOrSatOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpocox)) TradingPostOfferCellOffsetX = tpocox;
                    else if ((key.Equals("TradingPostOfferCellOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("OfferCellOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("OfferedMonOrSatOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpocoy)) TradingPostOfferCellOffsetY = tpocoy;
                    else if ((key.Equals("TradingPostRequestedCellOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("RequestedCellOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("RequestedMonOrSatOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tprcox)) TradingPostRequestedCellOffsetX = tprcox;
                    else if ((key.Equals("TradingPostRequestedCellOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("RequestedCellOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("RequestedMonOrSatOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tprcoy)) TradingPostRequestedCellOffsetY = tprcoy;
                    else if ((key.Equals("TradingPostTimeLeftCellOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("TimeLeftCellOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tptlcox)) TradingPostTimeLeftCellOffsetX = tptlcox;
                    else if ((key.Equals("TradingPostTimeLeftCellOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("TimeLeftCellOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tptlcoy)) TradingPostTimeLeftCellOffsetY = tptlcoy;
                    else if ((key.Equals("TradingPostSellerCellOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("SellerCellOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpscox)) TradingPostSellerCellOffsetX = tpscox;
                    else if ((key.Equals("TradingPostSellerCellOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("SellerCellOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpscoy)) TradingPostSellerCellOffsetY = tpscoy;
                    else if ((key.Equals("TradingPostPriceOfferCellOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("PriceOfferCellOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tppocox)) TradingPostPriceOfferCellOffsetX = tppocox;
                    else if ((key.Equals("TradingPostPriceOfferCellOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("PriceOfferCellOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tppocoy)) TradingPostPriceOfferCellOffsetY = tppocoy;
                    else if ((key.Equals("TradingPostActionCellOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("ActionCellOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("ActionButtonOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpacox)) TradingPostActionCellOffsetX = tpacox;
                    else if ((key.Equals("TradingPostActionCellOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("ActionCellOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("ActionButtonOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpacoy)) TradingPostActionCellOffsetY = tpacoy;
                    else if ((key.Equals("TradingPostCreateTitleOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("CreateTitleOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpctox)) TradingPostCreateTitleOffsetX = tpctox;
                    else if ((key.Equals("TradingPostCreateTitleOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("CreateTitleOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpctoy)) TradingPostCreateTitleOffsetY = tpctoy;
                    else if ((key.Equals("TradingPostOfferedTypeGroupOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("OfferedTypeGroupOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpotgox)) TradingPostOfferedTypeGroupOffsetX = tpotgox;
                    else if ((key.Equals("TradingPostOfferedTypeGroupOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("OfferedTypeGroupOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpotgoy)) TradingPostOfferedTypeGroupOffsetY = tpotgoy;
                    else if ((key.Equals("TradingPostOfferedValueGroupOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("OfferedValueGroupOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpovgox)) TradingPostOfferedValueGroupOffsetX = tpovgox;
                    else if ((key.Equals("TradingPostOfferedValueGroupOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("OfferedValueGroupOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpovgoy)) TradingPostOfferedValueGroupOffsetY = tpovgoy;
                    else if ((key.Equals("TradingPostRequestedTypeGroupOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("RequestedTypeGroupOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tprtgox)) TradingPostRequestedTypeGroupOffsetX = tprtgox;
                    else if ((key.Equals("TradingPostRequestedTypeGroupOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("RequestedTypeGroupOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tprtgoy)) TradingPostRequestedTypeGroupOffsetY = tprtgoy;
                    else if ((key.Equals("TradingPostRequestedValueGroupOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("RequestedValueGroupOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tprvgox)) TradingPostRequestedValueGroupOffsetX = tprvgox;
                    else if ((key.Equals("TradingPostRequestedValueGroupOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("RequestedValueGroupOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tprvgoy)) TradingPostRequestedValueGroupOffsetY = tprvgoy;
                    else if ((key.Equals("TradingPostListPostButtonOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("ListPostButtonOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tplpbx)) TradingPostListPostButtonOffsetX = tplpbx;
                    else if ((key.Equals("TradingPostListPostButtonOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("ListPostButtonOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tplpby)) TradingPostListPostButtonOffsetY = tplpby;
                    else if ((key.Equals("TradingPostOfferedTypeDropdownOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("OfferedTypeDropdownOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("OfferedKindDropdownOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpotdy)) TradingPostOfferedKindDropdownOffsetY = tpotdy;
                    else if ((key.Equals("TradingPostOfferedValueDropdownOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("OfferedValueDropdownOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpovdx)) TradingPostOfferedValueDropdownOffsetX = tpovdx;
                    else if ((key.Equals("TradingPostOfferedValueDropdownOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("OfferedValueDropdownOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpovdy)) TradingPostOfferedValueDropdownOffsetY = tpovdy;
                    else if ((key.Equals("TradingPostRequestedTypeDropdownOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("RequestedTypeDropdownOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("RequestKindDropdownOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tprtdy)) TradingPostRequestKindDropdownOffsetY = tprtdy;
                    else if ((key.Equals("TradingPostRequestedValueDropdownOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("RequestedValueDropdownOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tprvdx)) TradingPostRequestedValueDropdownOffsetX = tprvdx;
                    else if ((key.Equals("TradingPostRequestedValueDropdownOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("RequestedValueDropdownOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tprvdy)) TradingPostRequestedValueDropdownOffsetY = tprvdy;
                    else if ((key.Equals("TradingPostOfferedTypeButtonOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("OfferedTypeButtonOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpotbox)) TradingPostOfferedTypeButtonOffsetX = tpotbox;
                    else if ((key.Equals("TradingPostOfferedTypeButtonOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("OfferedTypeButtonOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpotboy)) TradingPostOfferedTypeButtonOffsetY = tpotboy;
                    else if ((key.Equals("TradingPostOfferedFieldOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("OfferedFieldOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpoffox)) TradingPostOfferedFieldOffsetX = tpoffox;
                    else if ((key.Equals("TradingPostOfferedFieldOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("OfferedFieldOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpoffoy)) TradingPostOfferedFieldOffsetY = tpoffoy;
                    else if ((key.Equals("TradingPostOfferedArrowOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("OfferedArrowOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpoaox)) TradingPostOfferedArrowOffsetX = tpoaox;
                    else if ((key.Equals("TradingPostOfferedArrowOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("OfferedArrowOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpoaoy)) TradingPostOfferedArrowOffsetY = tpoaoy;
                    else if ((key.Equals("TradingPostOfferedSatsFieldOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("OfferedSatsFieldOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tposfox)) TradingPostOfferedSatsFieldOffsetX = tposfox;
                    else if ((key.Equals("TradingPostOfferedSatsFieldOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("OfferedSatsFieldOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tposfoy)) TradingPostOfferedSatsFieldOffsetY = tposfoy;
                    else if ((key.Equals("TradingPostRequestTypeButtonOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("RequestTypeButtonOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tprtbox)) TradingPostRequestTypeButtonOffsetX = tprtbox;
                    else if ((key.Equals("TradingPostRequestTypeButtonOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("RequestTypeButtonOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tprtboy)) TradingPostRequestTypeButtonOffsetY = tprtboy;
                    else if ((key.Equals("TradingPostRequestFieldOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("RequestFieldOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tprfoy)) TradingPostRequestFieldOffsetY = tprfoy;
                    else if ((key.Equals("TradingPostRequestArrowOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("RequestArrowOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpraox)) TradingPostRequestArrowOffsetX = tpraox;
                    else if ((key.Equals("TradingPostRequestArrowOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("RequestArrowOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpraoy)) TradingPostRequestArrowOffsetY = tpraoy;
                    else if ((key.Equals("TradingPostRequestSatsFieldOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("RequestSatsFieldOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tprsfox)) TradingPostRequestSatsFieldOffsetX = tprsfox;
                    else if ((key.Equals("TradingPostRequestSatsFieldOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("RequestSatsFieldOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tprsfoy)) TradingPostRequestSatsFieldOffsetY = tprsfoy;
                    else if ((key.Equals("TradingPostListPostOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("ListPostOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tplpoy)) TradingPostListPostOffsetY = tplpoy;
                    else if ((key.Equals("TradingPostCreateHelpTextOffsetX", StringComparison.OrdinalIgnoreCase) || key.Equals("CreateHelpTextOffsetX", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpchtox)) TradingPostCreateHelpTextOffsetX = tpchtox;
                    else if ((key.Equals("TradingPostCreateHelpTextOffsetY", StringComparison.OrdinalIgnoreCase) || key.Equals("CreateHelpTextOffsetY", StringComparison.OrdinalIgnoreCase)) && float.TryParse(value, out float tpchtoy)) TradingPostCreateHelpTextOffsetY = tpchtoy;
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
                EnsureTradingPostLayoutConfigDefaults(path);

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
                TradingPostCreateLabelWidth = Mathf.Clamp(TradingPostCreateLabelWidth, 0f, 220f);
                TradingPostOfferedTypeButtonWidth = Mathf.Clamp(TradingPostOfferedTypeButtonWidth, 90f, 280f);
                TradingPostOfferedFieldWidth = Mathf.Clamp(TradingPostOfferedFieldWidth, 120f, 480f);
                TradingPostOfferedSatsFieldWidth = Mathf.Clamp(TradingPostOfferedSatsFieldWidth, 80f, 320f);
                TradingPostOfferedArrowWidth = Mathf.Clamp(TradingPostOfferedArrowWidth, 18f, 60f);
                TradingPostRequestTypeButtonWidth = Mathf.Clamp(TradingPostRequestTypeButtonWidth, 90f, 280f);
                TradingPostRequestFieldWidth = Mathf.Clamp(TradingPostRequestFieldWidth, 120f, 480f);
                TradingPostRequestSatsFieldWidth = Mathf.Clamp(TradingPostRequestSatsFieldWidth, 80f, 320f);
                TradingPostRequestArrowWidth = Mathf.Clamp(TradingPostRequestArrowWidth, 18f, 60f);
                TradingPostListPostButtonWidth = Mathf.Clamp(TradingPostListPostButtonWidth, 70f, 240f);
                TradingPostCreateRowSpacing = Mathf.Clamp(TradingPostCreateRowSpacing, 0f, 30f);
                TradingPostCreateColumnGap = Mathf.Clamp(TradingPostCreateColumnGap, 0f, 40f);
                TradingPostOfferedRowOffsetX = Mathf.Clamp(TradingPostOfferedRowOffsetX, 0f, 300f);
                TradingPostRequestedRowOffsetX = Mathf.Clamp(TradingPostRequestedRowOffsetX, 0f, 300f);
                TradingPostRequestFieldOffsetX = Mathf.Clamp(TradingPostRequestFieldOffsetX, 0f, 300f);
                TradingPostListPostOffsetX = Mathf.Clamp(TradingPostListPostOffsetX, 0f, 300f);
                TradingPostCreateWindowX = Mathf.Clamp(TradingPostCreateWindowX, 0f, 2000f);
                TradingPostCreateWindowY = Mathf.Clamp(TradingPostCreateWindowY, 0f, 2000f);
                TradingPostCreateWindowWidth = Mathf.Clamp(TradingPostCreateWindowWidth, 420f, 1200f);
                TradingPostCreateWindowHeight = Mathf.Clamp(TradingPostCreateWindowHeight, 300f, 900f);
                TradingPostCreateDrawerGap = Mathf.Clamp(TradingPostCreateDrawerGap, -60f, 80f);
                TradingPostCreateDrawerOffsetY = Mathf.Clamp(TradingPostCreateDrawerOffsetY, -200f, 400f);
                TradingPostCreateOpenButtonWidth = Mathf.Clamp(TradingPostCreateOpenButtonWidth, 90f, 320f);
                TradingPostCreateOpenButtonHeight = Mathf.Clamp(TradingPostCreateOpenButtonHeight, 22f, 60f);
                TradingPostCreateWindowPaddingTop = Mathf.Clamp(TradingPostCreateWindowPaddingTop, 0f, 80f);
                TradingPostCreateWindowPaddingLeft = Mathf.Clamp(TradingPostCreateWindowPaddingLeft, 0f, 200f);
                TradingPostOfferedKindDropdownOffsetX = Mathf.Clamp(TradingPostOfferedKindDropdownOffsetX, 0f, 400f);
                TradingPostOfferedKindDropdownWidth = Mathf.Clamp(TradingPostOfferedKindDropdownWidth, 90f, 320f);
                TradingPostOfferedDropdownOffsetX = Mathf.Clamp(TradingPostOfferedDropdownOffsetX, 0f, 400f);
                TradingPostOfferedDropdownWidth = Mathf.Clamp(TradingPostOfferedDropdownWidth, 180f, 900f);
                TradingPostOfferedDropdownHeight = Mathf.Clamp(TradingPostOfferedDropdownHeight, 80f, 500f);
                TradingPostRequestKindDropdownOffsetX = Mathf.Clamp(TradingPostRequestKindDropdownOffsetX, 0f, 400f);
                TradingPostRequestKindDropdownWidth = Mathf.Clamp(TradingPostRequestKindDropdownWidth, 90f, 320f);
                TradingPostRequestSpeciesDropdownOffsetX = Mathf.Clamp(TradingPostRequestSpeciesDropdownOffsetX, 0f, 400f);
                TradingPostRequestSpeciesDropdownWidth = Mathf.Clamp(TradingPostRequestSpeciesDropdownWidth, 160f, 600f);
                TradingPostRequestSpeciesDropdownHeight = Mathf.Clamp(TradingPostRequestSpeciesDropdownHeight, 80f, 500f);
                TradingPostTableIconSize = Mathf.Clamp(TradingPostTableIconSize, 16f, 60f);
                TradingPostTableIconTextGap = Mathf.Clamp(TradingPostTableIconTextGap, 0f, 30f);
                TradingPostOfferIconSize = Mathf.Clamp(TradingPostOfferIconSize <= 0.01f ? TradingPostTableIconSize : TradingPostOfferIconSize, 12f, 80f);
                TradingPostOfferIconOffsetX = Mathf.Clamp(TradingPostOfferIconOffsetX, -80f, 120f);
                TradingPostOfferIconOffsetY = Mathf.Clamp(TradingPostOfferIconOffsetY, -60f, 120f);
                TradingPostRequestedIconSize = Mathf.Clamp(TradingPostRequestedIconSize <= 0.01f ? TradingPostTableIconSize : TradingPostRequestedIconSize, 12f, 80f);
                TradingPostRequestedIconOffsetX = Mathf.Clamp(TradingPostRequestedIconOffsetX, -80f, 120f);
                TradingPostRequestedIconOffsetY = Mathf.Clamp(TradingPostRequestedIconOffsetY, -60f, 120f);
                TradingPostOfferSatsIconSize = Mathf.Clamp(TradingPostOfferSatsIconSize <= 0.01f ? TradingPostOfferIconSize : TradingPostOfferSatsIconSize, 12f, 80f);
                TradingPostOfferSatsIconOffsetX = Mathf.Clamp(TradingPostOfferSatsIconOffsetX, -80f, 120f);
                TradingPostOfferSatsIconOffsetY = Mathf.Clamp(TradingPostOfferSatsIconOffsetY, -60f, 120f);
                TradingPostRequestedSatsIconSize = Mathf.Clamp(TradingPostRequestedSatsIconSize <= 0.01f ? TradingPostRequestedIconSize : TradingPostRequestedSatsIconSize, 12f, 80f);
                TradingPostRequestedSatsIconOffsetX = Mathf.Clamp(TradingPostRequestedSatsIconOffsetX, -80f, 120f);
                TradingPostRequestedSatsIconOffsetY = Mathf.Clamp(TradingPostRequestedSatsIconOffsetY, -60f, 120f);
                TradingPostBrowseGridOffsetX = Mathf.Clamp(TradingPostBrowseGridOffsetX, 0f, 300f);
                TradingPostBrowseGridOffsetY = Mathf.Clamp(TradingPostBrowseGridOffsetY, 0f, 300f);
                TradingPostFilterButtonHeight = Mathf.Clamp(TradingPostFilterButtonHeight, 22f, 48f);
                TradingPostFilterFieldHeight = Mathf.Clamp(TradingPostFilterFieldHeight, 22f, 48f);
                TradingPostSearchFieldHeight = Mathf.Clamp(TradingPostSearchFieldHeight, 22f, 48f);
                TradingPostRefreshButtonHeight = Mathf.Clamp(TradingPostRefreshButtonHeight, 22f, 48f);
                TradingPostSearchWidth = Mathf.Clamp(TradingPostSearchWidth, 60f, 260f);
                TradingPostSearchHeight = Mathf.Clamp(TradingPostSearchHeight <= 0.01f ? TradingPostSearchFieldHeight : TradingPostSearchHeight, 22f, 60f);
                TradingPostOfferedFilterWidth = Mathf.Clamp(TradingPostOfferedFilterWidth, 60f, 180f);
                TradingPostOfferedFilterHeight = Mathf.Clamp(TradingPostOfferedFilterHeight <= 0.01f ? TradingPostFilterButtonHeight : TradingPostOfferedFilterHeight, 22f, 60f);
                TradingPostRequestedFilterWidth = Mathf.Clamp(TradingPostRequestedFilterWidth, 70f, 200f);
                TradingPostRequestedFilterHeight = Mathf.Clamp(TradingPostRequestedFilterHeight <= 0.01f ? TradingPostFilterButtonHeight : TradingPostRequestedFilterHeight, 22f, 60f);
                TradingPostTypeFilterWidth = Mathf.Clamp(TradingPostTypeFilterWidth, 70f, 220f);
                TradingPostTypeFilterHeight = Mathf.Clamp(TradingPostTypeFilterHeight <= 0.01f ? TradingPostFilterButtonHeight : TradingPostTypeFilterHeight, 22f, 60f);
                TradingPostTimeFilterWidth = Mathf.Clamp(TradingPostTimeFilterWidth, 70f, 180f);
                TradingPostTimeFilterHeight = Mathf.Clamp(TradingPostTimeFilterHeight <= 0.01f ? TradingPostFilterButtonHeight : TradingPostTimeFilterHeight, 22f, 60f);
                TradingPostSellerFilterWidth = Mathf.Clamp(TradingPostSellerFilterWidth, 60f, 220f);
                TradingPostSellerFilterHeight = Mathf.Clamp(TradingPostSellerFilterHeight <= 0.01f ? TradingPostFilterFieldHeight : TradingPostSellerFilterHeight, 22f, 60f);
                TradingPostRefreshFilterWidth = Mathf.Clamp(TradingPostRefreshFilterWidth, 60f, 180f);
                TradingPostRefreshFilterHeight = Mathf.Clamp(TradingPostRefreshFilterHeight <= 0.01f ? TradingPostRefreshButtonHeight : TradingPostRefreshFilterHeight, 22f, 60f);
                MailComposeOpenButtonWidth = Mathf.Clamp(MailComposeOpenButtonWidth, 80f, 220f);
                MailComposeOpenButtonHeight = Mathf.Clamp(MailComposeOpenButtonHeight, 24f, 60f);
                MailComposeDrawerWidth = Mathf.Clamp(MailComposeDrawerWidth, 320f, 900f);
                MailComposeDrawerHeight = Mathf.Clamp(MailComposeDrawerHeight, 260f, 800f);
                MailComposeToFieldWidth = Mathf.Clamp(MailComposeToFieldWidth, 120f, 760f);
                MailComposeToFieldHeight = Mathf.Clamp(MailComposeToFieldHeight, 22f, 60f);
                MailComposeSubjectFieldWidth = Mathf.Clamp(MailComposeSubjectFieldWidth, 120f, 760f);
                MailComposeSubjectFieldHeight = Mathf.Clamp(MailComposeSubjectFieldHeight, 22f, 60f);
                MailComposeBodyFieldWidth = Mathf.Clamp(MailComposeBodyFieldWidth, 160f, 820f);
                MailComposeBodyFieldHeight = Mathf.Clamp(MailComposeBodyFieldHeight, 60f, 600f);
                MailComposeAttachmentTypeWidth = Mathf.Clamp(MailComposeAttachmentTypeWidth, 80f, 260f);
                MailComposeAttachmentTypeHeight = Mathf.Clamp(MailComposeAttachmentTypeHeight, 22f, 60f);
                MailComposeAttachmentValueWidth = Mathf.Clamp(MailComposeAttachmentValueWidth, 120f, 760f);
                MailComposeAttachmentValueHeight = Mathf.Clamp(MailComposeAttachmentValueHeight, 22f, 80f);
                MailComposeAttachmentArrowWidth = Mathf.Clamp(MailComposeAttachmentArrowWidth, 20f, 60f);
                MailComposeAttachmentDropdownWidth = Mathf.Clamp(MailComposeAttachmentDropdownWidth, 120f, 760f);
                MailComposeAttachmentDropdownHeight = Mathf.Clamp(MailComposeAttachmentDropdownHeight, 80f, 500f);
                MailComposeSendButtonWidth = Mathf.Clamp(MailComposeSendButtonWidth, 60f, 180f);
                MailComposeSendButtonHeight = Mathf.Clamp(MailComposeSendButtonHeight, 24f, 60f);
                MailComposeCancelButtonWidth = Mathf.Clamp(MailComposeCancelButtonWidth, 60f, 180f);
                MailComposeCancelButtonHeight = Mathf.Clamp(MailComposeCancelButtonHeight, 24f, 60f);
                TradingPostFilterRowOffsetX = Mathf.Clamp(TradingPostFilterRowOffsetX, 0f, 300f);
                TradingPostFilterRowOffsetY = Mathf.Clamp(TradingPostFilterRowOffsetY, 0f, 300f);
                TradingPostRefreshButtonOffsetX = Mathf.Clamp(TradingPostRefreshButtonOffsetX, 0f, 300f);
                TradingPostRefreshButtonOffsetY = Mathf.Clamp(TradingPostRefreshButtonOffsetY, 0f, 300f);
                TradingPostPagerOffsetX = Mathf.Clamp(TradingPostPagerOffsetX, 0f, 300f);
                TradingPostPagerOffsetY = Mathf.Clamp(TradingPostPagerOffsetY, 0f, 300f);
                TradingPostGridHeaderOffsetX = Mathf.Clamp(TradingPostGridHeaderOffsetX, 0f, 300f);
                TradingPostGridHeaderOffsetY = Mathf.Clamp(TradingPostGridHeaderOffsetY, 0f, 300f);
                TradingPostGridScrollHeightOffset = Mathf.Clamp(TradingPostGridScrollHeightOffset, -240f, 500f);
                TradingPostTableRowHeight = Mathf.Clamp(TradingPostTableRowHeight, 40f, 140f);
                TradingPostNameColumnWidth = Mathf.Clamp(TradingPostNameColumnWidth, 80f, 360f);
                TradingPostOfferColumnWidth = Mathf.Clamp(TradingPostOfferColumnWidth <= 0.01f ? TradingPostNameColumnWidth : TradingPostOfferColumnWidth, 80f, 420f);
                TradingPostRequestedColumnWidth = Mathf.Clamp(TradingPostRequestedColumnWidth, 70f, 300f);
                TradingPostTimeLeftColumnWidth = Mathf.Clamp(TradingPostTimeLeftColumnWidth, 50f, 180f);
                TradingPostSellerColumnWidth = Mathf.Clamp(TradingPostSellerColumnWidth, 60f, 260f);
                TradingPostPriceOfferColumnWidth = Mathf.Clamp(TradingPostPriceOfferColumnWidth, 70f, 260f);
                TradingPostActionColumnWidth = Mathf.Clamp(TradingPostActionColumnWidth, 80f, 260f);
                TradingPostActionButtonWidth = Mathf.Clamp(TradingPostActionButtonWidth, 70f, 240f);
                TradingPostActionButtonHeight = Mathf.Clamp(TradingPostActionButtonHeight, 22f, 80f);
                TradingPostRowOffsetX = Mathf.Clamp(TradingPostRowOffsetX, 0f, 300f);
                TradingPostRowOffsetY = Mathf.Clamp(TradingPostRowOffsetY, 0f, 120f);
                TradingPostHeaderOfferOffsetX = Mathf.Clamp(TradingPostHeaderOfferOffsetX, 0f, 300f);
                TradingPostHeaderOfferOffsetY = Mathf.Clamp(TradingPostHeaderOfferOffsetY, 0f, 120f);
                TradingPostHeaderRequestedOffsetX = Mathf.Clamp(TradingPostHeaderRequestedOffsetX, 0f, 300f);
                TradingPostHeaderRequestedOffsetY = Mathf.Clamp(TradingPostHeaderRequestedOffsetY, 0f, 120f);
                TradingPostHeaderTimeLeftOffsetX = Mathf.Clamp(TradingPostHeaderTimeLeftOffsetX, 0f, 300f);
                TradingPostHeaderTimeLeftOffsetY = Mathf.Clamp(TradingPostHeaderTimeLeftOffsetY, 0f, 120f);
                TradingPostHeaderSellerOffsetX = Mathf.Clamp(TradingPostHeaderSellerOffsetX, 0f, 300f);
                TradingPostHeaderSellerOffsetY = Mathf.Clamp(TradingPostHeaderSellerOffsetY, 0f, 120f);
                TradingPostHeaderPriceOfferOffsetX = Mathf.Clamp(TradingPostHeaderPriceOfferOffsetX, 0f, 300f);
                TradingPostHeaderPriceOfferOffsetY = Mathf.Clamp(TradingPostHeaderPriceOfferOffsetY, 0f, 120f);
                TradingPostHeaderActionOffsetX = Mathf.Clamp(TradingPostHeaderActionOffsetX, 0f, 300f);
                TradingPostHeaderActionOffsetY = Mathf.Clamp(TradingPostHeaderActionOffsetY, 0f, 120f);
                TradingPostOfferCellOffsetX = Mathf.Clamp(TradingPostOfferCellOffsetX, 0f, 300f);
                TradingPostOfferCellOffsetY = Mathf.Clamp(TradingPostOfferCellOffsetY, 0f, 120f);
                TradingPostRequestedCellOffsetX = Mathf.Clamp(TradingPostRequestedCellOffsetX, 0f, 300f);
                TradingPostRequestedCellOffsetY = Mathf.Clamp(TradingPostRequestedCellOffsetY, 0f, 120f);
                TradingPostTimeLeftCellOffsetX = Mathf.Clamp(TradingPostTimeLeftCellOffsetX, 0f, 300f);
                TradingPostTimeLeftCellOffsetY = Mathf.Clamp(TradingPostTimeLeftCellOffsetY, 0f, 120f);
                TradingPostSellerCellOffsetX = Mathf.Clamp(TradingPostSellerCellOffsetX, 0f, 300f);
                TradingPostSellerCellOffsetY = Mathf.Clamp(TradingPostSellerCellOffsetY, 0f, 120f);
                TradingPostPriceOfferCellOffsetX = Mathf.Clamp(TradingPostPriceOfferCellOffsetX, 0f, 300f);
                TradingPostPriceOfferCellOffsetY = Mathf.Clamp(TradingPostPriceOfferCellOffsetY, 0f, 120f);
                TradingPostActionCellOffsetX = Mathf.Clamp(TradingPostActionCellOffsetX, 0f, 300f);
                TradingPostActionCellOffsetY = Mathf.Clamp(TradingPostActionCellOffsetY, 0f, 120f);
                TradingPostCreateTitleOffsetX = Mathf.Clamp(TradingPostCreateTitleOffsetX, -300f, 300f);
                TradingPostCreateTitleOffsetY = Mathf.Clamp(TradingPostCreateTitleOffsetY, -120f, 120f);
                TradingPostOfferedTypeGroupOffsetX = Mathf.Clamp(TradingPostOfferedTypeGroupOffsetX, -300f, 300f);
                TradingPostOfferedTypeGroupOffsetY = Mathf.Clamp(TradingPostOfferedTypeGroupOffsetY, -120f, 120f);
                TradingPostOfferedValueGroupOffsetX = Mathf.Clamp(TradingPostOfferedValueGroupOffsetX, -300f, 300f);
                TradingPostOfferedValueGroupOffsetY = Mathf.Clamp(TradingPostOfferedValueGroupOffsetY, -120f, 120f);
                TradingPostRequestedTypeGroupOffsetX = Mathf.Clamp(TradingPostRequestedTypeGroupOffsetX, -300f, 300f);
                TradingPostRequestedTypeGroupOffsetY = Mathf.Clamp(TradingPostRequestedTypeGroupOffsetY, -120f, 120f);
                TradingPostRequestedValueGroupOffsetX = Mathf.Clamp(TradingPostRequestedValueGroupOffsetX, -300f, 300f);
                TradingPostRequestedValueGroupOffsetY = Mathf.Clamp(TradingPostRequestedValueGroupOffsetY, -120f, 120f);
                TradingPostListPostButtonOffsetX = Mathf.Clamp(TradingPostListPostButtonOffsetX, -300f, 300f);
                TradingPostListPostButtonOffsetY = Mathf.Clamp(TradingPostListPostButtonOffsetY, -120f, 120f);
                TradingPostOfferedKindDropdownOffsetY = Mathf.Clamp(TradingPostOfferedKindDropdownOffsetY, -120f, 120f);
                TradingPostOfferedValueDropdownOffsetX = Mathf.Clamp(TradingPostOfferedValueDropdownOffsetX, -300f, 300f);
                TradingPostOfferedValueDropdownOffsetY = Mathf.Clamp(TradingPostOfferedValueDropdownOffsetY, -120f, 120f);
                TradingPostRequestKindDropdownOffsetY = Mathf.Clamp(TradingPostRequestKindDropdownOffsetY, -120f, 120f);
                TradingPostRequestedValueDropdownOffsetX = Mathf.Clamp(TradingPostRequestedValueDropdownOffsetX, -300f, 300f);
                TradingPostRequestedValueDropdownOffsetY = Mathf.Clamp(TradingPostRequestedValueDropdownOffsetY, -120f, 120f);
                TradingPostOfferedTypeButtonOffsetX = Mathf.Clamp(TradingPostOfferedTypeButtonOffsetX, 0f, 300f);
                TradingPostOfferedTypeButtonOffsetY = Mathf.Clamp(TradingPostOfferedTypeButtonOffsetY, 0f, 120f);
                TradingPostOfferedFieldOffsetX = Mathf.Clamp(TradingPostOfferedFieldOffsetX, 0f, 300f);
                TradingPostOfferedFieldOffsetY = Mathf.Clamp(TradingPostOfferedFieldOffsetY, 0f, 120f);
                TradingPostOfferedArrowOffsetX = Mathf.Clamp(TradingPostOfferedArrowOffsetX, 0f, 300f);
                TradingPostOfferedArrowOffsetY = Mathf.Clamp(TradingPostOfferedArrowOffsetY, 0f, 120f);
                TradingPostOfferedSatsFieldOffsetX = Mathf.Clamp(TradingPostOfferedSatsFieldOffsetX, 0f, 300f);
                TradingPostOfferedSatsFieldOffsetY = Mathf.Clamp(TradingPostOfferedSatsFieldOffsetY, 0f, 120f);
                TradingPostRequestTypeButtonOffsetX = Mathf.Clamp(TradingPostRequestTypeButtonOffsetX, 0f, 300f);
                TradingPostRequestTypeButtonOffsetY = Mathf.Clamp(TradingPostRequestTypeButtonOffsetY, 0f, 120f);
                TradingPostRequestFieldOffsetY = Mathf.Clamp(TradingPostRequestFieldOffsetY, 0f, 120f);
                TradingPostRequestArrowOffsetX = Mathf.Clamp(TradingPostRequestArrowOffsetX, 0f, 300f);
                TradingPostRequestArrowOffsetY = Mathf.Clamp(TradingPostRequestArrowOffsetY, 0f, 120f);
                TradingPostRequestSatsFieldOffsetX = Mathf.Clamp(TradingPostRequestSatsFieldOffsetX, 0f, 300f);
                TradingPostRequestSatsFieldOffsetY = Mathf.Clamp(TradingPostRequestSatsFieldOffsetY, 0f, 120f);
                TradingPostListPostOffsetY = Mathf.Clamp(TradingPostListPostOffsetY, 0f, 120f);
                TradingPostCreateHelpTextOffsetX = Mathf.Clamp(TradingPostCreateHelpTextOffsetX, 0f, 300f);
                TradingPostCreateHelpTextOffsetY = Mathf.Clamp(TradingPostCreateHelpTextOffsetY, 0f, 120f);

                // Auto-migrate the oversized selector defaults from the first SATS test build.
                // Those values made the Create Listing row look stacked/overlapped in smaller windows.
                if (OfferedMONSelectedPreviewHeight >= 95f && OfferedMONIconSize >= 95f && OfferedMONTextOffsetX >= 49f)
                {
                    OfferedMONRowHeight = 52f;
                    OfferedMONSelectedPreviewHeight = 44f;
                    OfferedMONIconSize = 36f;
                    OfferedMONTextOffsetX = 44f;
                }
                if (RequestedMONSelectedPreviewHeight >= 95f && RequestedMONIconSize >= 95f && RequestedMONTextOffsetX >= 49f)
                {
                    RequestedMONRowHeight = 52f;
                    RequestedMONSelectedPreviewHeight = 44f;
                    RequestedMONIconSize = 36f;
                    RequestedMONTextOffsetX = 44f;
                }

                OfferedMONRowHeight = Mathf.Clamp(OfferedMONRowHeight, 20f, 100f);
                OfferedMONSelectedPreviewHeight = Mathf.Clamp(OfferedMONSelectedPreviewHeight, 20f, 100f);
                OfferedMONIconSize = Mathf.Clamp(OfferedMONIconSize, 12f, 96f);
                RequestedMONRowHeight = Mathf.Clamp(RequestedMONRowHeight, 20f, 100f);
                RequestedMONSelectedPreviewHeight = Mathf.Clamp(RequestedMONSelectedPreviewHeight, 20f, 100f);
                RequestedMONIconSize = Mathf.Clamp(RequestedMONIconSize, 12f, 96f);

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
            // Do not prune the Trading Post config anymore. The layout/UI is actively tuned
            // through goose.monsterpatch.gts.client.cfg, and destructive pruning was removing
            // useful positioning keys between test builds.
            return;
        }

        private static void EnsureTradingPostLayoutConfigDefaults(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                    return;
                string text = File.ReadAllText(path);
                StringBuilder add = new StringBuilder();
                if (text.IndexOf("[Trading Post Create Layout]", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    add.AppendLine();
                    add.AppendLine("[Trading Post Create Layout]");
                    add.AppendLine("# Create-listing layout controls. Change these if the Offered/Requested row needs tuning.");
                    add.AppendLine("# Width keys affect spacing without needing a rebuild.");
                    add.AppendLine("CreateLabelWidth = 82");
                    add.AppendLine("OfferedTypeButtonWidth = 125");
                    add.AppendLine("OfferedFieldWidth = 220");
                    add.AppendLine("OfferedSatsFieldWidth = 176");
                    add.AppendLine("OfferedArrowWidth = 28");
                    add.AppendLine("RequestTypeButtonWidth = 125");
                    add.AppendLine("RequestFieldWidth = 220");
                    add.AppendLine("RequestSatsFieldWidth = 176");
                    add.AppendLine("RequestArrowWidth = 28");
                    add.AppendLine("ListPostButtonWidth = 118");
                    add.AppendLine("CreateRowSpacing = 6");
                    add.AppendLine("CreateColumnGap = 4");
                    add.AppendLine("# Positive row/field offsets add extra spacing to the right.");
                    add.AppendLine("OfferedRowOffsetX = 51");
                    add.AppendLine("RequestedRowOffsetX = 0");
                    add.AppendLine("RequestFieldOffsetX = 0");
                    add.AppendLine("ListPostOffsetX = 0");
                    add.AppendLine("# Separate Create Listing window. Positive X/Y move the popup right/down.");
                    add.AppendLine("CreateWindowX = 120");
                    add.AppendLine("CreateWindowY = 120");
                    add.AppendLine("CreateWindowWidth = 620");
                    add.AppendLine("CreateWindowHeight = 410");
                    add.AppendLine("# Side drawer mode keeps Create Listing attached to the right of Trading Post instead of floating over the table.");
                    add.AppendLine("CreateUseSideDrawer = true");
                    add.AppendLine("CreateDrawerGap = -2");
                    add.AppendLine("CreateDrawerOffsetY = 48");
                    add.AppendLine("CreateDimMainWindow = false");
                    add.AppendLine("CreateTitleOffsetX = 0");
                    add.AppendLine("CreateTitleOffsetY = 0");
                    add.AppendLine("OfferedTypeButtonOffsetX = 0");
                    add.AppendLine("OfferedTypeButtonOffsetY = 0");
                    add.AppendLine("OfferedFieldOffsetX = 0");
                    add.AppendLine("OfferedFieldOffsetY = 0");
                    add.AppendLine("OfferedArrowOffsetX = 0");
                    add.AppendLine("OfferedArrowOffsetY = 0");
                    add.AppendLine("OfferedSatsFieldOffsetX = 0");
                    add.AppendLine("OfferedSatsFieldOffsetY = 0");
                    add.AppendLine("RequestTypeButtonOffsetX = 0");
                    add.AppendLine("RequestTypeButtonOffsetY = 0");
                    add.AppendLine("RequestFieldOffsetY = 0");
                    add.AppendLine("RequestArrowOffsetX = 0");
                    add.AppendLine("RequestArrowOffsetY = 0");
                    add.AppendLine("RequestSatsFieldOffsetX = 0");
                    add.AppendLine("RequestSatsFieldOffsetY = 0");
                    add.AppendLine("ListPostOffsetY = 0");
                    add.AppendLine("CreateHelpTextOffsetX = 0");
                    add.AppendLine("CreateHelpTextOffsetY = 0");
                    add.AppendLine("CreateOpenButtonWidth = 170");
                    add.AppendLine("CreateOpenButtonHeight = 30");
                    add.AppendLine("CreateWindowPaddingTop = 6");
                    add.AppendLine("CreateWindowPaddingLeft = 0");
                    add.AppendLine("# Dropdown placement/sizing inside the Create Listing popup.");
                    add.AppendLine("OfferedKindDropdownOffsetX = 0");
                    add.AppendLine("OfferedKindDropdownWidth = 150");
                    add.AppendLine("OfferedDropdownOffsetX = 115");
                    add.AppendLine("OfferedDropdownWidth = 420");
                    add.AppendLine("OfferedDropdownHeight = 170");
                    add.AppendLine("RequestKindDropdownOffsetX = 0");
                    add.AppendLine("RequestKindDropdownWidth = 150");
                    add.AppendLine("RequestSpeciesDropdownOffsetX = 115");
                    add.AppendLine("RequestSpeciesDropdownWidth = 250");
                    add.AppendLine("RequestSpeciesDropdownHeight = 170");
                }
                else if (text.IndexOf("CreateWindowX", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    add.AppendLine();
                    add.AppendLine("[Trading Post Create Layout]");
                    add.AppendLine("# Added in v0.10.1: separate Create Listing popup controls.");
                    add.AppendLine("CreateWindowX = 120");
                    add.AppendLine("CreateWindowY = 120");
                    add.AppendLine("CreateWindowWidth = 620");
                    add.AppendLine("CreateWindowHeight = 410");
                    add.AppendLine("# Side drawer mode keeps Create Listing attached to the right of Trading Post instead of floating over the table.");
                    add.AppendLine("CreateUseSideDrawer = true");
                    add.AppendLine("CreateDrawerGap = -2");
                    add.AppendLine("CreateDrawerOffsetY = 48");
                    add.AppendLine("CreateDimMainWindow = false");
                    add.AppendLine("CreateTitleOffsetX = 0");
                    add.AppendLine("CreateTitleOffsetY = 0");
                    add.AppendLine("OfferedTypeButtonOffsetX = 0");
                    add.AppendLine("OfferedTypeButtonOffsetY = 0");
                    add.AppendLine("OfferedFieldOffsetX = 0");
                    add.AppendLine("OfferedFieldOffsetY = 0");
                    add.AppendLine("OfferedArrowOffsetX = 0");
                    add.AppendLine("OfferedArrowOffsetY = 0");
                    add.AppendLine("OfferedSatsFieldOffsetX = 0");
                    add.AppendLine("OfferedSatsFieldOffsetY = 0");
                    add.AppendLine("RequestTypeButtonOffsetX = 0");
                    add.AppendLine("RequestTypeButtonOffsetY = 0");
                    add.AppendLine("RequestFieldOffsetY = 0");
                    add.AppendLine("RequestArrowOffsetX = 0");
                    add.AppendLine("RequestArrowOffsetY = 0");
                    add.AppendLine("RequestSatsFieldOffsetX = 0");
                    add.AppendLine("RequestSatsFieldOffsetY = 0");
                    add.AppendLine("ListPostOffsetY = 0");
                    add.AppendLine("CreateHelpTextOffsetX = 0");
                    add.AppendLine("CreateHelpTextOffsetY = 0");
                    add.AppendLine("CreateOpenButtonWidth = 170");
                    add.AppendLine("CreateOpenButtonHeight = 30");
                    add.AppendLine("CreateWindowPaddingTop = 6");
                    add.AppendLine("CreateWindowPaddingLeft = 0");
                    add.AppendLine("OfferedDropdownOffsetX = 115");
                    add.AppendLine("OfferedDropdownWidth = 420");
                    add.AppendLine("OfferedDropdownHeight = 170");
                    add.AppendLine("RequestKindDropdownOffsetX = 0");
                    add.AppendLine("RequestKindDropdownWidth = 150");
                    add.AppendLine("RequestSpeciesDropdownOffsetX = 115");
                    add.AppendLine("RequestSpeciesDropdownWidth = 250");
                    add.AppendLine("RequestSpeciesDropdownHeight = 170");
                }
                if (text.IndexOf("OfferedTypeButtonWidth", StringComparison.OrdinalIgnoreCase) < 0 ||
                    text.IndexOf("OfferedSatsFieldWidth", StringComparison.OrdinalIgnoreCase) < 0 ||
                    text.IndexOf("OfferedKindDropdownOffsetX", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    add.AppendLine();
                    add.AppendLine("[Trading Post Create Layout]");
                    add.AppendLine("# Added in v0.10.2: offered type/SATS controls.");
                    if (text.IndexOf("OfferedTypeButtonWidth", StringComparison.OrdinalIgnoreCase) < 0) add.AppendLine("OfferedTypeButtonWidth = 125");
                    if (text.IndexOf("OfferedSatsFieldWidth", StringComparison.OrdinalIgnoreCase) < 0) add.AppendLine("OfferedSatsFieldWidth = 176");
                    if (text.IndexOf("OfferedKindDropdownOffsetX", StringComparison.OrdinalIgnoreCase) < 0) add.AppendLine("OfferedKindDropdownOffsetX = 0");
                    if (text.IndexOf("OfferedKindDropdownWidth", StringComparison.OrdinalIgnoreCase) < 0) add.AppendLine("OfferedKindDropdownWidth = 150");
                }
                if (text.IndexOf("[OfferedMON]", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    add.AppendLine();
                    add.AppendLine("[OfferedMON]");
                    add.AppendLine("RowHeight = 52");
                    add.AppendLine("SelectedPreviewHeight = 44");
                    add.AppendLine("ShowIcon = true");
                    add.AppendLine("IconSize = 36");
                    add.AppendLine("IconOffsetX = 0");
                    add.AppendLine("IconOffsetY = 0");
                    add.AppendLine("TextOffsetX = 44");
                    add.AppendLine("TextOffsetY = 0");
                }
                if (text.IndexOf("[RequestedMON]", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    add.AppendLine();
                    add.AppendLine("[RequestedMON]");
                    add.AppendLine("RowHeight = 52");
                    add.AppendLine("SelectedPreviewHeight = 44");
                    add.AppendLine("ShowIcon = true");
                    add.AppendLine("IconSize = 36");
                    add.AppendLine("IconOffsetX = 0");
                    add.AppendLine("IconOffsetY = 0");
                    add.AppendLine("TextOffsetX = 44");
                    add.AppendLine("TextOffsetY = 0");
                }
                if (text.IndexOf("TradingPostTableIconSize", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    add.AppendLine();
                    add.AppendLine("[Trading Post Listing Table]");
                    add.AppendLine("# Icons shown inside the Browse/My Listings auction-house table.");
                    add.AppendLine("TableIconSize = 28");
                    add.AppendLine("TableIconTextGap = 6");
                    add.AppendLine("TableShowOfferedIcon = true");
                    add.AppendLine("TableShowRequestedIcon = true");
                }
                if (text.IndexOf("[Trading Post Browse Grid]", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    add.AppendLine();
                    add.AppendLine("[Trading Post Browse Grid]");
                    add.AppendLine("# Positional controls for the auction-house style Browse/My Listings table.");
                    add.AppendLine("GridOffsetX = 0");
                    add.AppendLine("GridOffsetY = 0");
                    add.AppendLine("FilterRowOffsetX = 0");
                    add.AppendLine("FilterRowOffsetY = 0");
                    add.AppendLine("RefreshButtonOffsetX = 0");
                    add.AppendLine("RefreshButtonOffsetY = 0");
                    add.AppendLine("PagerOffsetX = 0");
                    add.AppendLine("PagerOffsetY = 0");
                    add.AppendLine("HeaderOffsetX = 0");
                    add.AppendLine("HeaderOffsetY = 0");
                    add.AppendLine("ScrollHeightOffset = 0");
                    add.AppendLine("TableRowHeight = 58");
                    add.AppendLine("# The first column now displays 'Offer' instead of 'Name'. NameColumnWidth remains accepted as an old alias.");
                    add.AppendLine("OfferColumnWidth = 190");
                    add.AppendLine("RequestedColumnWidth = 135");
                    add.AppendLine("TimeLeftColumnWidth = 80");
                    add.AppendLine("SellerColumnWidth = 105");
                    add.AppendLine("# v0.10.4 hides the Price / Offer column by default. Set true only for debugging/old layout testing.");
                    add.AppendLine("ShowPriceOfferColumn = false");
                    add.AppendLine("PriceOfferColumnWidth = 110");
                    add.AppendLine("ActionColumnWidth = 150");
                    add.AppendLine("ActionButtonWidth = 145");
                    add.AppendLine("ActionButtonHeight = 34");
                    add.AppendLine("RowOffsetX = 0");
                    add.AppendLine("RowOffsetY = 0");
                    add.AppendLine("HeaderOfferOffsetX = 0");
                    add.AppendLine("HeaderOfferOffsetY = 0");
                    add.AppendLine("HeaderRequestedOffsetX = 0");
                    add.AppendLine("HeaderRequestedOffsetY = 0");
                    add.AppendLine("HeaderTimeLeftOffsetX = 0");
                    add.AppendLine("HeaderTimeLeftOffsetY = 0");
                    add.AppendLine("HeaderSellerOffsetX = 0");
                    add.AppendLine("HeaderSellerOffsetY = 0");
                    add.AppendLine("HeaderPriceOfferOffsetX = 0");
                    add.AppendLine("HeaderPriceOfferOffsetY = 0");
                    add.AppendLine("HeaderActionOffsetX = 0");
                    add.AppendLine("HeaderActionOffsetY = 0");
                    add.AppendLine("OfferCellOffsetX = 0");
                    add.AppendLine("OfferCellOffsetY = 0");
                    add.AppendLine("RequestedCellOffsetX = 0");
                    add.AppendLine("RequestedCellOffsetY = 0");
                    add.AppendLine("TimeLeftCellOffsetX = 0");
                    add.AppendLine("TimeLeftCellOffsetY = 0");
                    add.AppendLine("SellerCellOffsetX = 0");
                    add.AppendLine("SellerCellOffsetY = 0");
                    add.AppendLine("PriceOfferCellOffsetX = 0");
                    add.AppendLine("PriceOfferCellOffsetY = 0");
                    add.AppendLine("ActionCellOffsetX = 0");
                    add.AppendLine("ActionCellOffsetY = 0");
                }
                if (text.IndexOf("CreateUseSideDrawer", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    add.AppendLine();
                    add.AppendLine("[Trading Post Create Layout]");
                    add.AppendLine("# Added in v0.10.3: side-drawer Create Listing controls.");
                    add.AppendLine("CreateUseSideDrawer = true");
                    add.AppendLine("CreateDrawerGap = -2");
                    add.AppendLine("CreateDrawerOffsetY = 48");
                    add.AppendLine("CreateDimMainWindow = false");
                    add.AppendLine("CreateTitleOffsetX = 0");
                    add.AppendLine("CreateTitleOffsetY = 0");
                    add.AppendLine("OfferedTypeButtonOffsetX = 0");
                    add.AppendLine("OfferedTypeButtonOffsetY = 0");
                    add.AppendLine("OfferedFieldOffsetX = 0");
                    add.AppendLine("OfferedFieldOffsetY = 0");
                    add.AppendLine("OfferedArrowOffsetX = 0");
                    add.AppendLine("OfferedArrowOffsetY = 0");
                    add.AppendLine("OfferedSatsFieldOffsetX = 0");
                    add.AppendLine("OfferedSatsFieldOffsetY = 0");
                    add.AppendLine("RequestTypeButtonOffsetX = 0");
                    add.AppendLine("RequestTypeButtonOffsetY = 0");
                    add.AppendLine("RequestFieldOffsetY = 0");
                    add.AppendLine("RequestArrowOffsetX = 0");
                    add.AppendLine("RequestArrowOffsetY = 0");
                    add.AppendLine("RequestSatsFieldOffsetX = 0");
                    add.AppendLine("RequestSatsFieldOffsetY = 0");
                    add.AppendLine("ListPostOffsetY = 0");
                    add.AppendLine("CreateHelpTextOffsetX = 0");
                    add.AppendLine("CreateHelpTextOffsetY = 0");
                }
                if (text.IndexOf("[Trading Post Browse Grid]", StringComparison.OrdinalIgnoreCase) >= 0 && text.IndexOf("ShowPriceOfferColumn", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    add.AppendLine();
                    add.AppendLine("[Trading Post Browse Grid]");
                    add.AppendLine("# Added in v0.10.4: first column is Offer; Price / Offer column hidden by default; per-column offsets exposed.");
                    add.AppendLine("OfferColumnWidth = 190");
                    add.AppendLine("ShowPriceOfferColumn = false");
                    add.AppendLine("RowOffsetX = 0");
                    add.AppendLine("RowOffsetY = 0");
                    add.AppendLine("HeaderOfferOffsetX = 0");
                    add.AppendLine("HeaderOfferOffsetY = 0");
                    add.AppendLine("HeaderRequestedOffsetX = 0");
                    add.AppendLine("HeaderRequestedOffsetY = 0");
                    add.AppendLine("HeaderTimeLeftOffsetX = 0");
                    add.AppendLine("HeaderTimeLeftOffsetY = 0");
                    add.AppendLine("HeaderSellerOffsetX = 0");
                    add.AppendLine("HeaderSellerOffsetY = 0");
                    add.AppendLine("HeaderPriceOfferOffsetX = 0");
                    add.AppendLine("HeaderPriceOfferOffsetY = 0");
                    add.AppendLine("HeaderActionOffsetX = 0");
                    add.AppendLine("HeaderActionOffsetY = 0");
                    add.AppendLine("OfferCellOffsetX = 0");
                    add.AppendLine("OfferCellOffsetY = 0");
                    add.AppendLine("RequestedCellOffsetX = 0");
                    add.AppendLine("RequestedCellOffsetY = 0");
                    add.AppendLine("TimeLeftCellOffsetX = 0");
                    add.AppendLine("TimeLeftCellOffsetY = 0");
                    add.AppendLine("SellerCellOffsetX = 0");
                    add.AppendLine("SellerCellOffsetY = 0");
                    add.AppendLine("PriceOfferCellOffsetX = 0");
                    add.AppendLine("PriceOfferCellOffsetY = 0");
                    add.AppendLine("ActionCellOffsetX = 0");
                    add.AppendLine("ActionCellOffsetY = 0");
                }
                if (text.IndexOf("[Trading Post Create Layout]", StringComparison.OrdinalIgnoreCase) >= 0 && text.IndexOf("CreateTitleOffsetX", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    add.AppendLine();
                    add.AppendLine("[Trading Post Create Layout]");
                    add.AppendLine("# Added in v0.10.4: individual Create Listing drawer element offsets.");
                    add.AppendLine("CreateTitleOffsetX = 0");
                    add.AppendLine("CreateTitleOffsetY = 0");
                    add.AppendLine("OfferedTypeButtonOffsetX = 0");
                    add.AppendLine("OfferedTypeButtonOffsetY = 0");
                    add.AppendLine("OfferedFieldOffsetX = 0");
                    add.AppendLine("OfferedFieldOffsetY = 0");
                    add.AppendLine("OfferedArrowOffsetX = 0");
                    add.AppendLine("OfferedArrowOffsetY = 0");
                    add.AppendLine("OfferedSatsFieldOffsetX = 0");
                    add.AppendLine("OfferedSatsFieldOffsetY = 0");
                    add.AppendLine("RequestTypeButtonOffsetX = 0");
                    add.AppendLine("RequestTypeButtonOffsetY = 0");
                    add.AppendLine("RequestFieldOffsetY = 0");
                    add.AppendLine("RequestArrowOffsetX = 0");
                    add.AppendLine("RequestArrowOffsetY = 0");
                    add.AppendLine("RequestSatsFieldOffsetX = 0");
                    add.AppendLine("RequestSatsFieldOffsetY = 0");
                    add.AppendLine("ListPostOffsetY = 0");
                    add.AppendLine("CreateHelpTextOffsetX = 0");
                    add.AppendLine("CreateHelpTextOffsetY = 0");
                }
                if (text.IndexOf("[CreateListingGroups]", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    add.AppendLine();
                    add.AppendLine("[CreateListingGroups]");
                    add.AppendLine("# v0.10.5 grouped movement controls for the Create Listing drawer.");
                    add.AppendLine("# These move a complete control group together so the button and its dropdown stay aligned.");
                    add.AppendLine("# OfferedTypeGroupOffset moves [Offered: MoN/SATS] plus the MoN/SATS dropdown below it.");
                    add.AppendLine("OfferedTypeGroupOffsetX = 0");
                    add.AppendLine("OfferedTypeGroupOffsetY = 0");
                    add.AppendLine("# OfferedValueGroupOffset moves the offered value box plus its arrow and full offered-MoN dropdown list.");
                    add.AppendLine("OfferedValueGroupOffsetX = 0");
                    add.AppendLine("OfferedValueGroupOffsetY = 0");
                    add.AppendLine("# RequestedTypeGroupOffset moves [Requested: MoN/SATS] plus the MoN/SATS dropdown below it.");
                    add.AppendLine("RequestedTypeGroupOffsetX = 0");
                    add.AppendLine("RequestedTypeGroupOffsetY = 0");
                    add.AppendLine("# RequestedValueGroupOffset moves the requested value box plus its arrow and full requested-MoN dropdown list.");
                    add.AppendLine("RequestedValueGroupOffsetX = 0");
                    add.AppendLine("RequestedValueGroupOffsetY = 0");
                    add.AppendLine("# ListPostButtonOffset moves only the [List Post] button.");
                    add.AppendLine("ListPostButtonOffsetX = 0");
                    add.AppendLine("ListPostButtonOffsetY = 0");
                }
                if (text.IndexOf("[CreateListingDropdowns]", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    add.AppendLine();
                    add.AppendLine("[CreateListingDropdowns]");
                    add.AppendLine("# Fine tuning for dropdowns after the grouped offsets above are set.");
                    add.AppendLine("# OfferedTypeDropdownOffset moves only the small MoN/SATS dropdown under [Offered: ...].");
                    add.AppendLine("OfferedKindDropdownOffsetX = 0");
                    add.AppendLine("OfferedKindDropdownOffsetY = 0");
                    add.AppendLine("OfferedKindDropdownWidth = 150");
                    add.AppendLine("# OfferedValueDropdownOffset moves only the full offered-MoN list after the offered value group position.");
                    add.AppendLine("# OfferedDropdownOffsetX = 0 lines the full offered MoN list up with the offered value box.");
                    add.AppendLine("OfferedDropdownOffsetX = 0");
                    add.AppendLine("OfferedValueDropdownOffsetX = 0");
                    add.AppendLine("OfferedValueDropdownOffsetY = 0");
                    add.AppendLine("OfferedDropdownWidth = 420");
                    add.AppendLine("OfferedDropdownHeight = 170");
                    add.AppendLine("# RequestedTypeDropdownOffset moves only the small MoN/SATS dropdown under [Requested: ...].");
                    add.AppendLine("RequestKindDropdownOffsetX = 0");
                    add.AppendLine("RequestKindDropdownOffsetY = 0");
                    add.AppendLine("RequestKindDropdownWidth = 150");
                    add.AppendLine("# RequestedValueDropdownOffset moves only the full requested-MoN list after the requested value group position.");
                    add.AppendLine("# RequestSpeciesDropdownOffsetX = 0 lines the full requested MoN list up with the requested value box.");
                    add.AppendLine("RequestSpeciesDropdownOffsetX = 0");
                    add.AppendLine("RequestedValueDropdownOffsetX = 0");
                    add.AppendLine("RequestedValueDropdownOffsetY = 0");
                    add.AppendLine("RequestSpeciesDropdownWidth = 250");
                    add.AppendLine("RequestSpeciesDropdownHeight = 170");
                }
                if (text.IndexOf("[ListingBoardColumns]", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    add.AppendLine();
                    add.AppendLine("[ListingBoardColumns]");
                    add.AppendLine("# Header/column controls for the listing board. Widths move following columns because this is a table layout.");
                    add.AppendLine("OfferColumnWidth = 190");
                    add.AppendLine("HeaderOfferOffsetX = 0");
                    add.AppendLine("HeaderOfferOffsetY = 0");
                    add.AppendLine("RequestedColumnWidth = 135");
                    add.AppendLine("HeaderRequestedOffsetX = 0");
                    add.AppendLine("HeaderRequestedOffsetY = 0");
                    add.AppendLine("TimeLeftColumnWidth = 80");
                    add.AppendLine("HeaderTimeLeftOffsetX = 0");
                    add.AppendLine("HeaderTimeLeftOffsetY = 0");
                    add.AppendLine("SellerColumnWidth = 105");
                    add.AppendLine("HeaderSellerOffsetX = 0");
                    add.AppendLine("HeaderSellerOffsetY = 0");
                    add.AppendLine("ActionColumnWidth = 150");
                    add.AppendLine("HeaderActionOffsetX = 0");
                    add.AppendLine("HeaderActionOffsetY = 0");
                    add.AppendLine("ShowPriceOfferColumn = false");
                }
                if (text.IndexOf("[ListingView]", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    add.AppendLine();
                    add.AppendLine("[ListingView]");
                    add.AppendLine("# Controls for the actual listing rows under the column headers.");
                    add.AppendLine("TableRowHeight = 58");
                    add.AppendLine("RowOffsetX = 0");
                    add.AppendLine("RowOffsetY = 0");
                    add.AppendLine("# OfferedMonOrSat controls the first row value, whether it is a MoN or SATS amount.");
                    add.AppendLine("OfferCellOffsetX = 0");
                    add.AppendLine("OfferCellOffsetY = 0");
                    add.AppendLine("OfferIconSize = 28");
                    add.AppendLine("OfferIconOffsetX = 0");
                    add.AppendLine("OfferIconOffsetY = 0");
                    add.AppendLine("# RequestedMonOrSat controls the requested row value, whether it is a MoN or SATS amount.");
                    add.AppendLine("RequestedCellOffsetX = 0");
                    add.AppendLine("RequestedCellOffsetY = 0");
                    add.AppendLine("RequestedIconSize = 28");
                    add.AppendLine("RequestedIconOffsetX = 0");
                    add.AppendLine("RequestedIconOffsetY = 0");
                    add.AppendLine("# SATS icons shown in the listing board when the offer/request is currency instead of a MoN.");
                    add.AppendLine("OfferSatsIconSize = 28");
                    add.AppendLine("OfferSatsIconOffsetX = 0");
                    add.AppendLine("OfferSatsIconOffsetY = 0");
                    add.AppendLine("RequestedSatsIconSize = 28");
                    add.AppendLine("RequestedSatsIconOffsetX = 0");
                    add.AppendLine("RequestedSatsIconOffsetY = 0");
                    add.AppendLine("TimeLeftCellOffsetX = 0");
                    add.AppendLine("TimeLeftCellOffsetY = 0");
                    add.AppendLine("SellerCellOffsetX = 0");
                    add.AppendLine("SellerCellOffsetY = 0");
                    add.AppendLine("ActionCellOffsetX = 0");
                    add.AppendLine("ActionCellOffsetY = 0");
                    add.AppendLine("ActionButtonWidth = 145");
                    add.AppendLine("ActionButtonHeight = 34");
                }
                if (text.IndexOf("[Trading Post Filter Bar]", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    add.AppendLine();
                    add.AppendLine("[Trading Post Filter Bar]");
                    add.AppendLine("# Legacy shared heights remain accepted; per-control values below are the new defaults.");
                    add.AppendLine("FilterButtonHeight = 30");
                    add.AppendLine("FilterFieldHeight = 30");
                    add.AppendLine("SearchFieldHeight = 30");
                    add.AppendLine("RefreshButtonHeight = 32");
                    add.AppendLine("SearchOffsetX = 0");
                    add.AppendLine("SearchOffsetY = 0");
                    add.AppendLine("SearchWidth = 120");
                    add.AppendLine("SearchHeight = 32");
                    add.AppendLine("OfferedButtonOffsetX = 0");
                    add.AppendLine("OfferedButtonOffsetY = 0");
                    add.AppendLine("OfferedButtonWidth = 92");
                    add.AppendLine("OfferedButtonHeight = 32");
                    add.AppendLine("RequestedButtonOffsetX = 0");
                    add.AppendLine("RequestedButtonOffsetY = 0");
                    add.AppendLine("RequestedButtonWidth = 105");
                    add.AppendLine("RequestedButtonHeight = 32");
                    add.AppendLine("TypeButtonOffsetX = 0");
                    add.AppendLine("TypeButtonOffsetY = 0");
                    add.AppendLine("TypeButtonWidth = 110");
                    add.AppendLine("TypeButtonHeight = 32");
                    add.AppendLine("TimeLeftButtonOffsetX = 0");
                    add.AppendLine("TimeLeftButtonOffsetY = 0");
                    add.AppendLine("TimeLeftButtonWidth = 100");
                    add.AppendLine("TimeLeftButtonHeight = 32");
                    add.AppendLine("SellerOffsetX = 0");
                    add.AppendLine("SellerOffsetY = 0");
                    add.AppendLine("SellerWidth = 90");
                    add.AppendLine("SellerHeight = 32");
                    add.AppendLine("RefreshButtonOffsetX = 0");
                    add.AppendLine("RefreshButtonOffsetY = 0");
                    add.AppendLine("RefreshButtonWidth = 90");
                }
                if (text.IndexOf("[Mailbox Compose Drawer]", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    add.AppendLine();
                    add.AppendLine("[Mailbox Compose Drawer]");
                    add.AppendLine("# Compose Mail opens as an attached side drawer like Create Listing.");
                    add.AppendLine("ComposeButtonWidth = 120");
                    add.AppendLine("ComposeButtonHeight = 30");
                    add.AppendLine("DrawerWidth = 440");
                    add.AppendLine("DrawerHeight = 360");
                    add.AppendLine("DrawerGap = -2");
                    add.AppendLine("DrawerOffsetY = 48");
                    add.AppendLine("TitleOffsetX = 0");
                    add.AppendLine("TitleOffsetY = 0");
                    add.AppendLine("ToLabelOffsetX = 0");
                    add.AppendLine("ToLabelOffsetY = 0");
                    add.AppendLine("ToLabelWidth = 54");
                    add.AppendLine("ToLabelHeight = 24");
                    add.AppendLine("ToFieldOffsetX = 0");
                    add.AppendLine("ToFieldOffsetY = 0");
                    add.AppendLine("ToFieldWidth = 260");
                    add.AppendLine("ToFieldHeight = 28");
                    add.AppendLine("SubjectLabelOffsetX = 0");
                    add.AppendLine("SubjectLabelOffsetY = 0");
                    add.AppendLine("SubjectLabelWidth = 54");
                    add.AppendLine("SubjectLabelHeight = 24");
                    add.AppendLine("SubjectFieldOffsetX = 0");
                    add.AppendLine("SubjectFieldOffsetY = 0");
                    add.AppendLine("SubjectFieldWidth = 300");
                    add.AppendLine("SubjectFieldHeight = 28");
                    add.AppendLine("BodyLabelOffsetX = 0");
                    add.AppendLine("BodyLabelOffsetY = 0");
                    add.AppendLine("BodyLabelWidth = 54");
                    add.AppendLine("BodyLabelHeight = 24");
                    add.AppendLine("BodyFieldOffsetX = 0");
                    add.AppendLine("BodyFieldOffsetY = 0");
                    add.AppendLine("BodyFieldWidth = 360");
                    add.AppendLine("BodyFieldHeight = 160");
                    add.AppendLine("SendButtonOffsetX = 0");
                    add.AppendLine("SendButtonOffsetY = 0");
                    add.AppendLine("SendButtonWidth = 90");
                    add.AppendLine("SendButtonHeight = 30");
                    add.AppendLine("CancelButtonOffsetX = 0");
                    add.AppendLine("CancelButtonOffsetY = 0");
                    add.AppendLine("CancelButtonWidth = 90");
                    add.AppendLine("CancelButtonHeight = 30");
                }
                if (add.Length > 0)
                    File.AppendAllText(path, add.ToString());
            }
            catch { }
        }

        private void Update()
        {
            try
            {
                MaintainAioSessionKeepAlive();
                MaintainTradingPostMailCount();
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

        private void MaintainTradingPostMailCount()
        {
            try
            {
                if (!_loggedIn || _client == null || !_showWindow || _busy)
                    return;
                if (Time.realtimeSinceStartup < _nextMailCountAt)
                    return;
                _nextMailCountAt = Time.realtimeSinceStartup + 20f;
                StartCoroutine(RefreshMailCountCoroutine());
            }
            catch { }
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

            Rect beforeGtsWindowRect = _windowRect;
            bool createModalOpen = _showCreateListingWindow || _showComposeMail;
            bool previousGuiEnabled = GUI.enabled;
            DrawWindowBacking(_windowRect);
            if (createModalOpen) GUI.enabled = false;
            _windowRect = GUI.Window(77319, _windowRect, DrawWindow, GUIContent.none, _mpWindowStyle);
            GUI.enabled = previousGuiEnabled;
            if (!_resizingWindow)
            {
                // Only the bottom-right resize target is allowed to change size.
                _windowRect.width = beforeGtsWindowRect.width;
                _windowRect.height = beforeGtsWindowRect.height;
            }
            ClampGtsWindowToScreen();
            MaybeSaveTradingPostWindowRect();

            if (_showCreateListingWindow)
            {
                if (TradingPostCreateUseSideDrawer)
                    UpdateCreateListingDrawerRect();
                if (TradingPostCreateDimMainWindow)
                    DrawModalBlocker(_windowRect);
                DrawWindowBacking(_createListingWindowRect);
                _createListingWindowRect = GUI.Window(77320, _createListingWindowRect, DrawCreateListingWindow, GUIContent.none, _mpWindowStyle);
                GUI.BringWindowToFront(77320);
                if (TradingPostCreateUseSideDrawer)
                    UpdateCreateListingDrawerRect();
                else
                    ClampCreateListingWindowToScreen();
            }


            if (_showComposeMail)
            {
                UpdateMailComposeDrawerRect();
                DrawWindowBacking(_mailComposeWindowRect);
                _mailComposeWindowRect = GUI.Window(77321, _mailComposeWindowRect, DrawMailComposeWindow, GUIContent.none, _mpWindowStyle);
                GUI.BringWindowToFront(77321);
                UpdateMailComposeDrawerRect();
            }

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

        private void DrawModalBlocker(Rect r)
        {
            try
            {
                Color old = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, 0.16f);
                GUI.DrawTexture(r, Texture2D.whiteTexture);
                GUI.color = old;
            }
            catch { }
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

        private void MaybeSaveTradingPostWindowRect()
        {
            try
            {
                if (!TradingPostRememberWindowPosition && !TradingPostRememberWindowSize)
                    return;
                if (RectClose(_windowRect, _lastSavedTradingPostWindowRect))
                    return;
                Event e = Event.current;
                bool mouseUp = e != null && (e.rawType == EventType.MouseUp || e.type == EventType.MouseUp);
                if (!mouseUp && Time.unscaledTime - _lastTradingPostWindowConfigSaveAt < 0.75f)
                    return;
                SaveTradingPostWindowRectToConfig();
            }
            catch { }
        }

        private void SaveTradingPostWindowRectToConfig()
        {
            try
            {
                string path = Path.Combine(Paths.ConfigPath, ConfigFileName);
                if (!File.Exists(path))
                    return;
                if (TradingPostRememberWindowPosition)
                {
                    UpsertIniValue(path, "Trading Post Window", "WindowX", FloatCfg(_windowRect.x));
                    UpsertIniValue(path, "Trading Post Window", "WindowY", FloatCfg(_windowRect.y));
                }
                if (TradingPostRememberWindowSize)
                {
                    UpsertIniValue(path, "Trading Post Window", "WindowWidth", FloatCfg(_windowRect.width));
                    UpsertIniValue(path, "Trading Post Window", "WindowHeight", FloatCfg(_windowRect.height));
                }
                _lastSavedTradingPostWindowRect = _windowRect;
                _lastTradingPostWindowConfigSaveAt = Time.unscaledTime;
            }
            catch (Exception ex)
            {
                if (DebugLogging) GTSNativePatcher.RuntimeWarn("Could not save Trading Post window config: " + ex.Message);
            }
        }

        private static bool RectClose(Rect a, Rect b)
        {
            return Mathf.Abs(a.x - b.x) < 1f && Mathf.Abs(a.y - b.y) < 1f && Mathf.Abs(a.width - b.width) < 1f && Mathf.Abs(a.height - b.height) < 1f;
        }

        private static string FloatCfg(float value)
        {
            return value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static void UpsertIniValue(string path, string section, string key, string value)
        {
            List<string> lines = new List<string>();
            if (File.Exists(path))
                lines.AddRange(File.ReadAllLines(path));

            string sectionHeader = "[" + section + "]";
            int sectionIndex = -1;
            int nextSectionIndex = lines.Count;
            for (int i = 0; i < lines.Count; i++)
            {
                string trimmed = (lines[i] ?? "").Trim();
                if (string.Equals(trimmed, sectionHeader, StringComparison.OrdinalIgnoreCase))
                {
                    sectionIndex = i;
                    nextSectionIndex = lines.Count;
                    for (int j = i + 1; j < lines.Count; j++)
                    {
                        string t = (lines[j] ?? "").Trim();
                        if (t.StartsWith("[", StringComparison.Ordinal) && t.EndsWith("]", StringComparison.Ordinal))
                        {
                            nextSectionIndex = j;
                            break;
                        }
                    }
                    break;
                }
            }

            if (sectionIndex < 0)
            {
                if (lines.Count > 0 && !string.IsNullOrWhiteSpace(lines[lines.Count - 1]))
                    lines.Add("");
                lines.Add(sectionHeader);
                lines.Add(key + " = " + value);
                File.WriteAllLines(path, lines.ToArray());
                return;
            }

            for (int i = sectionIndex + 1; i < nextSectionIndex; i++)
            {
                string line = lines[i] ?? "";
                string trimmed = line.Trim();
                if (trimmed.StartsWith("#") || trimmed.StartsWith(";"))
                    continue;
                int eq = line.IndexOf('=');
                if (eq > 0 && string.Equals(line.Substring(0, eq).Trim(), key, StringComparison.OrdinalIgnoreCase))
                {
                    lines[i] = key + " = " + value;
                    File.WriteAllLines(path, lines.ToArray());
                    return;
                }
            }

            lines.Insert(nextSectionIndex, key + " = " + value);
            File.WriteAllLines(path, lines.ToArray());
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

        private void UpdateCreateListingDrawerRect()
        {
            try
            {
                float s = Mathf.Max(0.5f, WindowScale);
                float screenW = Screen.width / s;
                float screenH = Screen.height / s;
                _createListingWindowRect.width = Mathf.Clamp(TradingPostCreateWindowWidth, 420f, 1200f);
                _createListingWindowRect.height = Mathf.Clamp(TradingPostCreateWindowHeight, 300f, 900f);

                float x = _windowRect.x + _windowRect.width + TradingPostCreateDrawerGap;
                float y = _windowRect.y + TradingPostCreateDrawerOffsetY;

                // Prefer an attached right-side drawer. If there is not enough room on
                // the right side of the screen, fall back to the left side instead of
                // overlapping the listing grid.
                if (x + _createListingWindowRect.width > screenW)
                    x = _windowRect.x - _createListingWindowRect.width - TradingPostCreateDrawerGap;

                _createListingWindowRect.x = Mathf.Clamp(x, 0f, Mathf.Max(0f, screenW - _createListingWindowRect.width));
                _createListingWindowRect.y = Mathf.Clamp(y, 0f, Mathf.Max(0f, screenH - _createListingWindowRect.height));
            }
            catch { }
        }

        private void UpdateMailComposeDrawerRect()
        {
            try
            {
                float s = Mathf.Max(0.5f, WindowScale);
                float screenW = Screen.width / s;
                float screenH = Screen.height / s;
                _mailComposeWindowRect.width = Mathf.Clamp(MailComposeDrawerWidth, 320f, 900f);
                _mailComposeWindowRect.height = Mathf.Clamp(MailComposeDrawerHeight, 260f, 800f);

                float x = _windowRect.x + _windowRect.width + MailComposeDrawerGap;
                float y = _windowRect.y + MailComposeDrawerOffsetY;
                if (x + _mailComposeWindowRect.width > screenW)
                    x = _windowRect.x - _mailComposeWindowRect.width - MailComposeDrawerGap;

                _mailComposeWindowRect.x = Mathf.Clamp(x, 0f, Mathf.Max(0f, screenW - _mailComposeWindowRect.width));
                _mailComposeWindowRect.y = Mathf.Clamp(y, 0f, Mathf.Max(0f, screenH - _mailComposeWindowRect.height));
            }
            catch { }
        }

        private void ClampCreateListingWindowToScreen()
        {
            try
            {
                _createListingWindowRect.width = Mathf.Clamp(_createListingWindowRect.width, 420f, 1200f);
                _createListingWindowRect.height = Mathf.Clamp(_createListingWindowRect.height, 300f, 900f);
                float s = Mathf.Max(0.5f, WindowScale);
                float maxX = Mathf.Max(0f, (Screen.width / s) - _createListingWindowRect.width);
                float maxY = Mathf.Max(0f, (Screen.height / s) - _createListingWindowRect.height);
                _createListingWindowRect.x = Mathf.Clamp(_createListingWindowRect.x, 0f, maxX);
                _createListingWindowRect.y = Mathf.Clamp(_createListingWindowRect.y, 0f, maxY);
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

                if (!_windowRect.Contains(p) && !(_showCreateListingWindow && _createListingWindowRect.Contains(p)) && !(_showComposeMail && _mailComposeWindowRect.Contains(p))) return;

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

            Rect titleRect = GUILayoutUtility.GetRect(1f, 34f, GUILayout.ExpandWidth(true), GUILayout.Height(34f));
            GUI.Label(titleRect, "Trading Post", _mpTitleStyle);
            Rect closeRect = new Rect(titleRect.xMax - 34f, titleRect.y + 4f, 30f, 24f);
            if (GUI.Button(closeRect, "X", _mpCloseButtonStyle))
                _showWindow = false;

            DrawHorizontalRule();
            GUILayout.Space(4);

            if (!_loggedIn)
            {
                GUILayout.Label("Steam/OpenID Trading Station", _mpHeaderStyle);
                GUILayout.Label("Server: " + ServerHost + ":" + ServerPort, _mpTinyLabelStyle);
                GUILayout.Label("Status: " + _status, _mpTinyLabelStyle);
                GUILayout.Space(8);
                GUI.enabled = !_busy;
                if (AioIntegratedMode)
                {
                    GUI.enabled = false;
                    GUILayout.Button("Use Chat Connect", _mpButtonStyle, GUILayout.Height(34));
                }
                else if (GUILayout.Button("Login with Steam", _mpButtonStyle, GUILayout.Height(34)))
                    StartCoroutine(LoginWithSteamCoroutine());
                GUI.enabled = true;
            }
            else
            {
                DrawTradingPostTopBar();
                GUILayout.Space(5);
                GUILayout.Label("Status: " + _status, _mpTinyLabelStyle);

                GUILayout.Space(7);
                if (_mode == "Mail")
                {
                    DrawMailPanel();
                }
                else
                {
                    DrawAuctionFilters();
                    GUILayout.Space(4);
                    if (_mode == "Mine") DrawMyListings(); else DrawBrowseListings();
                    GUILayout.Space(7);
                    DrawCreateListingLaunchArea();
                }
            }

            GUILayout.EndVertical();

            DrawGtsResizeHandle();
            GUI.DragWindow(new Rect(0, 0, Mathf.Max(0f, _windowRect.width - 40f), 44));
        }

        private void DrawTradingPostTopBar()
        {
            GUILayout.BeginHorizontal();
            GUI.enabled = !_busy;
            if (GUILayout.Button("Browse", _mpButtonStyle, GUILayout.Height(30))) { _mode = "Browse"; StartCoroutine(SearchListingsCoroutine(0)); StartCoroutine(RefreshMailCountCoroutine()); }
            if (GUILayout.Button("My Listings", _mpButtonStyle, GUILayout.Height(30))) { _mode = "Mine"; StartCoroutine(MyListingsCoroutine(0)); StartCoroutine(RefreshMailCountCoroutine()); }
            if (GUILayout.Button("Claim Trades", _mpButtonStyle, GUILayout.Height(30), GUILayout.Width(130))) StartCoroutine(ClaimCoroutine());
            GUILayout.Label(":" + Mathf.Max(0, _mailClaimableCount) + " available", _mpTinyLabelStyle, GUILayout.Width(85));
            GUILayout.FlexibleSpace();
            string mailLabel = "✉ Mail" + (_mailUnreadCount > 0 ? " (" + _mailUnreadCount + ")" : "");
            if (GUILayout.Button(mailLabel, _mpButtonStyle, GUILayout.Height(30), GUILayout.Width(100))) { _mode = "Mail"; StartCoroutine(MailListCoroutine(0)); }
            if (!AioIntegratedMode && GUILayout.Button("Disconnect", _mpButtonStyle, GUILayout.Height(30), GUILayout.Width(100))) Disconnect(true, true);
            if (GUILayout.Button("Close", _mpButtonStyle, GUILayout.Height(30), GUILayout.Width(80))) _showWindow = false;
            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        private void DrawAuctionFilters()
        {
            AddPositiveSpace(TradingPostFilterRowOffsetY);
            GUILayout.BeginHorizontal();
            AddPositiveSpace(TradingPostFilterRowOffsetX);
            GUILayout.BeginVertical(_mpCardStyle);
            GUILayout.BeginHorizontal();

            _filterSearchText = DrawOffsetPlaceholderTextField(_filterSearchText, "Search name...", TradingPostSearchWidth, TradingPostSearchHeight, TradingPostSearchOffsetX, TradingPostSearchOffsetY);
            if (DrawOffsetButton("Offered: " + ShortFilterLabel(_filterOffered), TradingPostOfferedFilterWidth, TradingPostOfferedFilterHeight, TradingPostOfferedFilterOffsetX, TradingPostOfferedFilterOffsetY))
                _filterOffered = CycleFilter(_filterOffered, new string[] { "All", "MoN", "SATS" });
            if (DrawOffsetButton("Requested: " + ShortFilterLabel(_filterRequested), TradingPostRequestedFilterWidth, TradingPostRequestedFilterHeight, TradingPostRequestedFilterOffsetX, TradingPostRequestedFilterOffsetY))
                _filterRequested = CycleFilter(_filterRequested, new string[] { "All", "MoN", "SATS" });
            if (DrawOffsetButton("Type: " + ShortFilterLabel(_filterType), TradingPostTypeFilterWidth, TradingPostTypeFilterHeight, TradingPostTypeFilterOffsetX, TradingPostTypeFilterOffsetY))
                _filterType = CycleFilter(_filterType, new string[] { "All", "MoN-for-MoN", "MoN-for-SATS", "SATS-for-MoN" });
            if (DrawOffsetButton("Time: " + ShortFilterLabel(_filterTimeLeft), TradingPostTimeFilterWidth, TradingPostTimeFilterHeight, TradingPostTimeFilterOffsetX, TradingPostTimeFilterOffsetY))
                _filterTimeLeft = CycleFilter(_filterTimeLeft, new string[] { "All", "Short", "Medium", "Long" });
            _filterSeller = DrawOffsetPlaceholderTextField(_filterSeller, "Seller", TradingPostSellerFilterWidth, TradingPostSellerFilterHeight, TradingPostSellerFilterOffsetX, TradingPostSellerFilterOffsetY);
            GUILayout.FlexibleSpace();
            GUI.enabled = !_busy;
            if (DrawOffsetButton("Refresh", TradingPostRefreshFilterWidth, TradingPostRefreshFilterHeight, TradingPostRefreshFilterOffsetX + TradingPostRefreshButtonOffsetX, TradingPostRefreshFilterOffsetY + TradingPostRefreshButtonOffsetY))
                RefreshCurrentTradingPostPage(0);
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private bool DrawOffsetButton(string label, float width, float height, float offsetX, float offsetY)
        {
            bool clicked = false;
            width = Mathf.Max(24f, width);
            height = Mathf.Max(22f, height);
            GUILayout.BeginVertical(GUILayout.Width(Mathf.Max(1f, width + Mathf.Max(0f, offsetX))), GUILayout.Height(Mathf.Max(height, height + Math.Abs(offsetY))));
            AddSignedSpace(offsetY);
            GUILayout.BeginHorizontal();
            AddSignedSpace(offsetX);
            clicked = GUILayout.Button(label, _mpButtonStyle, GUILayout.Width(width), GUILayout.Height(height));
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            return clicked;
        }

        private string DrawOffsetPlaceholderTextField(string value, string placeholder, float width, float height, float offsetX, float offsetY)
        {
            string next;
            width = Mathf.Max(24f, width);
            height = Mathf.Max(22f, height);
            GUILayout.BeginVertical(GUILayout.Width(Mathf.Max(1f, width + Mathf.Max(0f, offsetX))), GUILayout.Height(Mathf.Max(height, height + Math.Abs(offsetY))));
            AddSignedSpace(offsetY);
            GUILayout.BeginHorizontal();
            AddSignedSpace(offsetX);
            next = DrawPlaceholderTextField(value, placeholder, width, height);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            return next;
        }

        private string DrawPlaceholderTextField(string value, string placeholder, float width, float height)
        {
            height = Mathf.Max(22f, height);
            Rect r = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));
            string controlName = "GTS_FILTER_" + placeholder.Replace(" ", "_");
            GUI.SetNextControlName(controlName);
            string next = GUI.TextField(r, value ?? "", _mpTextFieldStyle);
            if (string.IsNullOrEmpty(next) && GUI.GetNameOfFocusedControl() != controlName)
            {
                Rect pr = new Rect(r.x + 6f, r.y + Mathf.Max(3f, (r.height - 18f) * 0.5f), r.width - 10f, r.height - 3f);
                GUI.Label(pr, placeholder, _mpTinyLabelStyle);
            }
            return next ?? "";
        }

        private string CycleFilter(string current, string[] values)
        {
            if (values == null || values.Length == 0)
                return "All";
            string cur = NormalizeFilterValue(current, "All");
            for (int i = 0; i < values.Length; i++)
            {
                if (string.Equals(values[i], cur, StringComparison.OrdinalIgnoreCase))
                    return values[(i + 1) % values.Length];
            }
            return values[0];
        }

        private static string NormalizeFilterValue(string value, string fallback)
        {
            value = (value ?? "").Trim();
            return string.IsNullOrEmpty(value) ? (fallback ?? "All") : value;
        }

        private static string ShortFilterLabel(string value)
        {
            value = NormalizeFilterValue(value, "All");
            if (string.Equals(value, "MoN-for-MoN", StringComparison.OrdinalIgnoreCase)) return "MoN/MoN";
            if (string.Equals(value, "MoN-for-SATS", StringComparison.OrdinalIgnoreCase)) return "MoN/SATS";
            if (string.Equals(value, "SATS-for-MoN", StringComparison.OrdinalIgnoreCase)) return "SATS/MoN";
            return value;
        }

        private void RefreshCurrentTradingPostPage(int page)
        {
            if (_mode == "Mine") StartCoroutine(MyListingsCoroutine(page));
            else StartCoroutine(SearchListingsCoroutine(page));
        }

        private void DrawGtsResizeHandle()
        {
            try
            {
                const float handleSize = 28f;
                Rect handle = new Rect(_windowRect.width - handleSize - 5f, _windowRect.height - handleSize - 5f, handleSize, handleSize);
                GUI.Label(handle, "◢", _mpTinyLabelStyle);

                Event e = Event.current;
                if (e == null)
                    return;

                if (e.type == EventType.MouseDown && handle.Contains(e.mousePosition))
                {
                    _resizingWindow = true;
                    _resizeStartScreenMouse = GUIUtility.GUIToScreenPoint(e.mousePosition);
                    _resizeStartWindowRect = _windowRect;
                    e.Use();
                }
                else if (e.type == EventType.MouseDrag && _resizingWindow)
                {
                    Vector2 cur = GUIUtility.GUIToScreenPoint(e.mousePosition);
                    Vector2 delta = cur - _resizeStartScreenMouse;
                    _windowRect.width = Mathf.Clamp(_resizeStartWindowRect.width + delta.x, 420f, 1200f);
                    _windowRect.height = Mathf.Clamp(_resizeStartWindowRect.height + delta.y, 380f, 900f);
                    e.Use();
                }
                else if (e.type == EventType.MouseUp || e.rawType == EventType.MouseUp)
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

        private void DrawCreateListingLaunchArea()
        {
            GUILayout.BeginHorizontal(_mpCardStyle);
            GUILayout.Label("Create a new Trading Post listing in its own window.", _mpTinyLabelStyle);
            GUILayout.FlexibleSpace();
            GUI.enabled = !_busy && _loggedIn;
            if (GUILayout.Button("Create Listing", _mpButtonStyle, GUILayout.Width(TradingPostCreateOpenButtonWidth), GUILayout.Height(TradingPostCreateOpenButtonHeight)))
            {
                _showCreateListingWindow = true;
                if (TradingPostCreateUseSideDrawer)
                    UpdateCreateListingDrawerRect();
                _offeredMonDropdownOpen = false;
                _requestSpeciesDropdownOpen = false;
                _requestKindDropdownOpen = false;
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        private void DrawCreateListingWindow(int id)
        {
            EnsureGtsGuiStyles();
            GUILayout.BeginVertical();
            Rect titleRect = GUILayoutUtility.GetRect(1f, 32f, GUILayout.ExpandWidth(true), GUILayout.Height(32f));
            GUI.Label(titleRect, "Create Listing", _mpTitleStyle);
            Rect closeRect = new Rect(titleRect.xMax - 34f, titleRect.y + 4f, 30f, 24f);
            if (GUI.Button(closeRect, "X", _mpCloseButtonStyle))
                _showCreateListingWindow = false;
            DrawHorizontalRule();
            GUILayout.Space(TradingPostCreateWindowPaddingTop);
            GUILayout.BeginHorizontal();
            AddPositiveSpace(TradingPostCreateWindowPaddingLeft);
            GUILayout.BeginVertical();
            DrawCreateListingArea();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            if (!TradingPostCreateUseSideDrawer)
                GUI.DragWindow(new Rect(0, 0, Mathf.Max(0f, _createListingWindowRect.width - 40f), 42f));
        }

        private void DrawCreateListingArea()
        {
            AddPositiveSpace(TradingPostCreateTitleOffsetY);
            GUILayout.BeginHorizontal();
            AddPositiveSpace(TradingPostCreateTitleOffsetX);
            GUILayout.Label("Listing Details", _mpSectionTitleStyle);
            GUILayout.EndHorizontal();

            GameScript gs = FindObjectOfType<GameScript>();
            BoxManager bm = gs != null ? gs.boxManager : null;
            if (!OfferedIsSats())
                EnsureOfferedMonSelection(gs, bm);

            GUILayout.BeginHorizontal();
            AddPositiveSpace(TradingPostOfferedRowOffsetX);

            AddSignedSpace(TradingPostOfferedTypeGroupOffsetX + TradingPostOfferedTypeButtonOffsetX);
            GUILayout.BeginVertical();
            AddSignedSpace(TradingPostOfferedTypeGroupOffsetY + TradingPostOfferedTypeButtonOffsetY);
            GUI.enabled = !_busy;
            if (GUILayout.Button("Offered: " + SafeOfferedKindLabel() + (_offeredKindDropdownOpen ? " ▲" : " ▼"), _mpButtonStyle, GUILayout.Width(TradingPostOfferedTypeButtonWidth), GUILayout.Height(28)))
            {
                _offeredKindDropdownOpen = !_offeredKindDropdownOpen;
                _offeredMonDropdownOpen = false;
                _requestKindDropdownOpen = false;
                _requestSpeciesDropdownOpen = false;
            }
            GUI.enabled = true;
            GUILayout.EndVertical();

            AddSignedSpace(TradingPostCreateColumnGap + TradingPostOfferedValueGroupOffsetX);
            if (OfferedIsSats())
            {
                AddSignedSpace(TradingPostOfferedSatsFieldOffsetX);
                GUILayout.BeginVertical();
                AddSignedSpace(TradingPostOfferedValueGroupOffsetY + TradingPostOfferedSatsFieldOffsetY);
                GUI.SetNextControlName("GTS_OFFERED_SATS_AMOUNT");
                string nextOffered = GUILayout.TextField(_offeredSatsText ?? string.Empty, _mpTextFieldStyle, GUILayout.Width(TradingPostOfferedSatsFieldWidth), GUILayout.Height(28));
                _offeredSatsText = DigitsOnly(nextOffered, 9);
                GUILayout.EndVertical();
            }
            else
            {
                GUILayout.BeginHorizontal(GUILayout.Width(TradingPostOfferedFieldWidth + TradingPostOfferedArrowWidth + TradingPostCreateColumnGap + Math.Max(0f, TradingPostOfferedFieldOffsetX) + Math.Max(0f, TradingPostOfferedArrowOffsetX)));
                AddSignedSpace(TradingPostOfferedFieldOffsetX);
                GUILayout.BeginVertical();
                AddSignedSpace(TradingPostOfferedValueGroupOffsetY + TradingPostOfferedFieldOffsetY);
                DrawOfferedMonSelectionField(gs, bm, TradingPostOfferedFieldWidth);
                GUILayout.EndVertical();
                AddPositiveSpace(TradingPostCreateColumnGap + TradingPostOfferedArrowOffsetX);
                GUILayout.BeginVertical();
                AddPositiveSpace(TradingPostOfferedArrowOffsetY);
                GUI.enabled = !_busy;
                if (GUILayout.Button(_offeredMonDropdownOpen ? "▲" : "▼", _mpButtonStyle, GUILayout.Width(TradingPostOfferedArrowWidth), GUILayout.Height(28)))
                {
                    RefreshOfferedMonOptions(gs, bm);
                    _offeredMonDropdownOpen = !_offeredMonDropdownOpen;
                    _offeredKindDropdownOpen = false;
                    _requestSpeciesDropdownOpen = false;
                    _requestKindDropdownOpen = false;
                    if (_offeredMonDropdownOpen)
                        _offeredMonDropdownScroll = Vector2.zero;
                }
                GUI.enabled = true;
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (_offeredKindDropdownOpen)
                DrawOfferedKindDropdown();
            if (_offeredMonDropdownOpen && !OfferedIsSats())
                DrawOfferedMonDropdown(gs, bm);

            GUILayout.Space(TradingPostCreateRowSpacing);
            GUILayout.BeginHorizontal();
            AddPositiveSpace(TradingPostRequestedRowOffsetX);

            AddSignedSpace(TradingPostRequestedTypeGroupOffsetX + TradingPostRequestTypeButtonOffsetX);
            GUILayout.BeginVertical();
            AddSignedSpace(TradingPostRequestedTypeGroupOffsetY + TradingPostRequestTypeButtonOffsetY);
            GUI.enabled = !_busy;
            if (GUILayout.Button("Requested: " + SafeRequestKindLabel() + (_requestKindDropdownOpen ? " ▲" : " ▼"), _mpButtonStyle, GUILayout.Width(TradingPostRequestTypeButtonWidth), GUILayout.Height(28)))
            {
                _requestKindDropdownOpen = !_requestKindDropdownOpen;
                _requestSpeciesDropdownOpen = false;
                _offeredMonDropdownOpen = false;
                _offeredKindDropdownOpen = false;
            }
            GUI.enabled = true;
            GUILayout.EndVertical();

            AddSignedSpace(TradingPostRequestFieldOffsetX + TradingPostCreateColumnGap + TradingPostRequestedValueGroupOffsetX);
            if (RequestIsSats())
            {
                AddSignedSpace(TradingPostRequestSatsFieldOffsetX);
                GUILayout.BeginVertical();
                AddSignedSpace(TradingPostRequestedValueGroupOffsetY + TradingPostRequestSatsFieldOffsetY);
                GUI.SetNextControlName("GTS_REQUEST_SATS_AMOUNT");
                string nextText = GUILayout.TextField(_requestSatsText ?? string.Empty, _mpTextFieldStyle, GUILayout.Width(TradingPostRequestSatsFieldWidth), GUILayout.Height(28));
                _requestSatsText = DigitsOnly(nextText, 9);
                GUILayout.EndVertical();
            }
            else
            {
                GUILayout.BeginHorizontal(GUILayout.Width(TradingPostRequestFieldWidth + TradingPostRequestArrowWidth + TradingPostCreateColumnGap + Math.Max(0f, TradingPostRequestArrowOffsetX)));
                GUILayout.BeginVertical();
                AddSignedSpace(TradingPostRequestedValueGroupOffsetY + TradingPostRequestFieldOffsetY);
                DrawRequestedMonSelectionField(TradingPostRequestFieldWidth);
                GUILayout.EndVertical();
                AddPositiveSpace(TradingPostCreateColumnGap + TradingPostRequestArrowOffsetX);
                GUILayout.BeginVertical();
                AddPositiveSpace(TradingPostRequestArrowOffsetY);
                GUI.enabled = !_busy;
                if (GUILayout.Button(_requestSpeciesDropdownOpen ? "▲" : "▼", _mpButtonStyle, GUILayout.Width(TradingPostRequestArrowWidth), GUILayout.Height(28)))
                {
                    EnsureSpeciesOptionsLoaded(false);
                    _requestSpeciesDropdownOpen = !_requestSpeciesDropdownOpen;
                    _offeredMonDropdownOpen = false;
                    _offeredKindDropdownOpen = false;
                    _requestKindDropdownOpen = false;
                    if (_requestSpeciesDropdownOpen)
                        _requestSpeciesDropdownScroll = Vector2.zero;
                }
                GUI.enabled = true;
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }

            AddSignedSpace(TradingPostListPostButtonOffsetX + TradingPostListPostOffsetX + TradingPostCreateColumnGap);
            bool invalidSatsForSats = OfferedIsSats() && RequestIsSats();
            GUILayout.BeginVertical();
            AddSignedSpace(TradingPostListPostButtonOffsetY + TradingPostListPostOffsetY);
            GUI.enabled = !_busy && !invalidSatsForSats;
            if (GUILayout.Button("List Post", _mpButtonStyle, GUILayout.Height(28), GUILayout.Width(TradingPostListPostButtonWidth)))
                StartCoroutine(CreateListingCoroutine());
            GUI.enabled = true;
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            if (_requestKindDropdownOpen)
                DrawRequestKindDropdown();
            if (_requestSpeciesDropdownOpen && !RequestIsSats())
                DrawRequestSpeciesDropdown();

            AddPositiveSpace(TradingPostCreateHelpTextOffsetY);
            GUILayout.BeginHorizontal();
            AddPositiveSpace(TradingPostCreateHelpTextOffsetX);
            if (invalidSatsForSats)
                GUILayout.Label("SATS-for-SATS posts are not allowed.", _mpTinyLabelStyle);
            else if (OfferedIsSats() && !RequestIsSats())
                GUILayout.Label("Offered SATS are deposited into the post and returned by mail if it expires or is cancelled.", _mpTinyLabelStyle);
            else if (RequestIsSats())
                GUILayout.Label("Requested SATS must be a whole number.", _mpTinyLabelStyle);
            else
                GUILayout.Label("Requested MoN must exactly match the name, e.g. POPLIT.", _mpTinyLabelStyle);
            GUILayout.EndHorizontal();
        }

        private static void AddPositiveSpace(float px)
        {
            if (px > 0.01f)
                GUILayout.Space(px);
        }

        private static void AddSignedSpace(float px)
        {
            if (Math.Abs(px) > 0.01f)
                GUILayout.Space(px);
        }

        private bool OfferedIsSats()
        {
            return string.Equals(_offeredKind, "SATS", StringComparison.OrdinalIgnoreCase);
        }

        private string SafeOfferedKindLabel()
        {
            return OfferedIsSats() ? "SATS" : "MoN";
        }

        private bool RequestIsSats()
        {
            return string.Equals(_requestKind, "SATS", StringComparison.OrdinalIgnoreCase);
        }

        private string SafeRequestKindLabel()
        {
            return RequestIsSats() ? "SATS" : "MoN";
        }

        private void DrawOfferedKindDropdown()
        {
            AddSignedSpace(TradingPostOfferedTypeGroupOffsetY + TradingPostOfferedKindDropdownOffsetY);
            GUILayout.BeginHorizontal();
            GUILayout.Space(TradingPostOfferedRowOffsetX + TradingPostOfferedTypeGroupOffsetX + TradingPostOfferedKindDropdownOffsetX);
            GUILayout.BeginVertical(_mpCardStyle, GUILayout.Width(TradingPostOfferedKindDropdownWidth));
            if (GUILayout.Button("MoN", _mpButtonStyle, GUILayout.Height(26)))
            {
                _offeredKind = "MON";
                _offeredKindDropdownOpen = false;
                GUI.FocusControl(null);
            }
            if (GUILayout.Button("SATS", _mpButtonStyle, GUILayout.Height(26)))
            {
                _offeredKind = "SATS";
                _offeredKindDropdownOpen = false;
                _offeredMonDropdownOpen = false;
                if (RequestIsSats())
                {
                    _requestKind = "MON";
                    _status = "SATS-for-SATS posts are not allowed. Requested type was switched to MoN.";
                }
                GUI.FocusControl(null);
            }
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawRequestKindDropdown()
        {
            AddSignedSpace(TradingPostRequestedTypeGroupOffsetY + TradingPostRequestKindDropdownOffsetY);
            GUILayout.BeginHorizontal();
            GUILayout.Space(TradingPostRequestedRowOffsetX + TradingPostRequestedTypeGroupOffsetX + TradingPostRequestKindDropdownOffsetX);
            GUILayout.BeginVertical(_mpCardStyle, GUILayout.Width(TradingPostRequestKindDropdownWidth));
            if (GUILayout.Button("MoN", _mpButtonStyle, GUILayout.Height(26)))
            {
                _requestKind = "MON";
                _requestKindDropdownOpen = false;
                GUI.FocusControl(null);
            }
            if (GUILayout.Button("SATS", _mpButtonStyle, GUILayout.Height(26)))
            {
                if (OfferedIsSats())
                {
                    _status = "SATS-for-SATS posts are not allowed.";
                    _requestKindDropdownOpen = false;
                    GUI.FocusControl(null);
                }
                else
                {
                    _requestKind = "SATS";
                    _requestKindDropdownOpen = false;
                    _requestSpeciesDropdownOpen = false;
                    GUI.FocusControl(null);
                }
            }
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private static string DigitsOnly(string text, int maxLen)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < text.Length && sb.Length < maxLen; i++)
            {
                char c = text[i];
                if (c >= '0' && c <= '9')
                    sb.Append(c);
            }
            return sb.ToString();
        }

        private void DrawOfferedMonDropdown(GameScript gs, BoxManager bm)
        {
            RefreshOfferedMonOptions(gs, bm);

            AddSignedSpace(TradingPostOfferedValueGroupOffsetY + TradingPostOfferedValueDropdownOffsetY);
            GUILayout.BeginHorizontal();
            GUILayout.Space(TradingPostOfferedRowOffsetX + TradingPostOfferedTypeGroupOffsetX + TradingPostOfferedTypeButtonWidth + TradingPostCreateColumnGap + TradingPostOfferedValueGroupOffsetX + TradingPostOfferedDropdownOffsetX + TradingPostOfferedValueDropdownOffsetX);
            GUILayout.BeginVertical(_mpCardStyle, GUILayout.Width(TradingPostOfferedDropdownWidth));

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
                _offeredMonDropdownScroll = GUILayout.BeginScrollView(_offeredMonDropdownScroll, GUILayout.Height(TradingPostOfferedDropdownHeight));
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

            AddSignedSpace(TradingPostRequestedValueGroupOffsetY + TradingPostRequestedValueDropdownOffsetY);
            GUILayout.BeginHorizontal();
            GUILayout.Space(TradingPostRequestedRowOffsetX + TradingPostRequestedTypeGroupOffsetX + TradingPostRequestTypeButtonWidth + TradingPostCreateColumnGap + TradingPostRequestedValueGroupOffsetX + TradingPostRequestSpeciesDropdownOffsetX + TradingPostRequestedValueDropdownOffsetX);
            GUILayout.BeginVertical(_mpCardStyle, GUILayout.Width(TradingPostRequestSpeciesDropdownWidth));

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
                _requestSpeciesDropdownScroll = GUILayout.BeginScrollView(_requestSpeciesDropdownScroll, GUILayout.Height(TradingPostRequestSpeciesDropdownHeight));
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
            DrawAuctionListingTable("Open Listings", _listings, false);
        }

        private void DrawMyListings()
        {
            DrawAuctionListingTable("My Open Listings", _myListings, true);
        }

        private void DrawAuctionListingTable(string title, List<GtsListing> rows, bool mine)
        {
            AddPositiveSpace(TradingPostBrowseGridOffsetY);
            GUILayout.BeginHorizontal();
            AddPositiveSpace(TradingPostBrowseGridOffsetX);
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label(title, _mpSectionTitleStyle, GUILayout.Width(150));
            GUILayout.FlexibleSpace();
            AddPositiveSpace(TradingPostPagerOffsetX);
            GUI.enabled = !_busy && _loggedIn;
            if (GUILayout.Button("Prev", _mpButtonStyle, GUILayout.Width(70), GUILayout.Height(24 + TradingPostPagerOffsetY)))
            {
                if (mine) StartCoroutine(MyListingsCoroutine(Mathf.Max(0, _pageIndex - 1))); else StartCoroutine(SearchListingsCoroutine(Mathf.Max(0, _pageIndex - 1)));
            }
            if (GUILayout.Button("Next", _mpButtonStyle, GUILayout.Width(70), GUILayout.Height(24 + TradingPostPagerOffsetY)))
            {
                if (mine) StartCoroutine(MyListingsCoroutine(_pageIndex + 1)); else StartCoroutine(SearchListingsCoroutine(_pageIndex + 1));
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical(_mpCardStyle);
            AddPositiveSpace(TradingPostGridHeaderOffsetY);
            GUILayout.BeginHorizontal();
            AddPositiveSpace(TradingPostGridHeaderOffsetX);
            DrawListingCellWithOffset("Offer", TradingPostOfferColumnWidth, true, TradingPostHeaderOfferOffsetX, TradingPostHeaderOfferOffsetY);
            DrawListingCellWithOffset("Requested", TradingPostRequestedColumnWidth, true, TradingPostHeaderRequestedOffsetX, TradingPostHeaderRequestedOffsetY);
            DrawListingCellWithOffset("Time Left", TradingPostTimeLeftColumnWidth, true, TradingPostHeaderTimeLeftOffsetX, TradingPostHeaderTimeLeftOffsetY);
            DrawListingCellWithOffset("Seller", TradingPostSellerColumnWidth, true, TradingPostHeaderSellerOffsetX, TradingPostHeaderSellerOffsetY);
            if (TradingPostShowPriceOfferColumn)
                DrawListingCellWithOffset("Price / Offer", TradingPostPriceOfferColumnWidth, true, TradingPostHeaderPriceOfferOffsetX, TradingPostHeaderPriceOfferOffsetY);
            DrawListingCellWithOffset("Action", TradingPostActionColumnWidth, true, TradingPostHeaderActionOffsetX, TradingPostHeaderActionOffsetY);
            GUILayout.EndHorizontal();
            DrawHorizontalRule();

            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(Mathf.Max(120f, RichListingScrollHeight + TradingPostGridScrollHeightOffset)));
            if (rows == null || rows.Count == 0)
            {
                GUILayout.Label(mine ? "No open listings loaded yet." : "No listings loaded yet.", _mpLabelStyle);
            }
            else
            {
                foreach (GtsListing l in rows)
                    DrawListingTableRow(l, mine);
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void DrawListingTableRow(GtsListing l, bool mine)
        {
            if (l == null) return;
            AddPositiveSpace(TradingPostRowOffsetY);
            GUILayout.BeginHorizontal(_mpCardStyle, GUILayout.Height(TradingPostTableRowHeight));
            AddPositiveSpace(TradingPostRowOffsetX);
            RichMonMetadata meta = GetRichListingMetadata(l);
            Sprite offeredRowIcon = GetListingOfferedIcon(l, meta);
            bool offeredIsSats = IsOfferedSats(l);
            float offeredIconSize = offeredIsSats ? TradingPostOfferSatsIconSize : TradingPostOfferIconSize;
            float offeredIconOffsetX = offeredIsSats ? TradingPostOfferSatsIconOffsetX : TradingPostOfferIconOffsetX;
            float offeredIconOffsetY = offeredIsSats ? TradingPostOfferSatsIconOffsetY : TradingPostOfferIconOffsetY;
            DrawListingIconCellWithOffset(FormatListingOffered(l), offeredIsSats ? "SATS" : l.OfferedSpecies, offeredRowIcon, TradingPostOfferColumnWidth, TradingPostTableShowOfferedIcon && offeredRowIcon != null, TradingPostOfferCellOffsetX, TradingPostOfferCellOffsetY, offeredIconSize, offeredIconOffsetX, offeredIconOffsetY);
            Sprite requestedRowIcon = GetListingRequestedIcon(l);
            bool requestedIsSats = IsSatsListing(l);
            float requestedIconSize = requestedIsSats ? TradingPostRequestedSatsIconSize : TradingPostRequestedIconSize;
            float requestedIconOffsetX = requestedIsSats ? TradingPostRequestedSatsIconOffsetX : TradingPostRequestedIconOffsetX;
            float requestedIconOffsetY = requestedIsSats ? TradingPostRequestedSatsIconOffsetY : TradingPostRequestedIconOffsetY;
            DrawListingIconCellWithOffset(FormatListingRequest(l), requestedIsSats ? "SATS" : l.RequestSpecies, requestedRowIcon, TradingPostRequestedColumnWidth, TradingPostTableShowRequestedIcon && requestedRowIcon != null, TradingPostRequestedCellOffsetX, TradingPostRequestedCellOffsetY, requestedIconSize, requestedIconOffsetX, requestedIconOffsetY);
            DrawListingCellWithOffset(FormatTimeLeft(l.TimeLeftSeconds), TradingPostTimeLeftColumnWidth, false, TradingPostTimeLeftCellOffsetX, TradingPostTimeLeftCellOffsetY);
            DrawListingCellWithOffset(DisplayOwner(l), TradingPostSellerColumnWidth, false, TradingPostSellerCellOffsetX, TradingPostSellerCellOffsetY);
            if (TradingPostShowPriceOfferColumn)
                DrawListingCellWithOffset(FormatPriceOffer(l), TradingPostPriceOfferColumnWidth, false, TradingPostPriceOfferCellOffsetX, TradingPostPriceOfferCellOffsetY);

            GUI.enabled = !_busy;
            AddPositiveSpace(TradingPostActionCellOffsetX);
            GUILayout.BeginVertical();
            AddPositiveSpace(TradingPostActionCellOffsetY);
            if (mine)
            {
                if (GUILayout.Button("Cancel", _mpButtonStyle, GUILayout.Width(TradingPostActionButtonWidth), GUILayout.Height(TradingPostActionButtonHeight)))
                    StartCoroutine(CancelListingCoroutine(l));
            }
            else if (IsOwnListing(l))
            {
                GUI.enabled = false;
                GUILayout.Button("Your Listing", _mpButtonStyle, GUILayout.Width(TradingPostActionButtonWidth), GUILayout.Height(TradingPostActionButtonHeight));
                GUI.enabled = !_busy;
            }
            else if (IsSatsListing(l))
            {
                if (GUILayout.Button("Buy", _mpButtonStyle, GUILayout.Width(TradingPostActionButtonWidth), GUILayout.Height(TradingPostActionButtonHeight)))
                    StartCoroutine(BuySatsListingCoroutine(l));
            }
            else
            {
                if (GUILayout.Button("Offer MoN", _mpButtonStyle, GUILayout.Width(TradingPostActionButtonWidth), GUILayout.Height(TradingPostActionButtonHeight)))
                    StartCoroutine(OfferSelectedMonCoroutine(l));
            }
            GUI.enabled = true;
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void DrawListingCell(string text, float width, bool header)
        {
            GUIStyle style = header ? _mpSectionTitleStyle : _mpTinyLabelStyle;
            GUILayout.Label(text ?? "", style, GUILayout.Width(width));
        }

        private void DrawListingCellWithOffset(string text, float width, bool header, float offsetX, float offsetY)
        {
            GUILayout.BeginVertical(GUILayout.Width(Mathf.Max(1f, width + Mathf.Max(0f, offsetX))));
            AddPositiveSpace(offsetY);
            GUILayout.BeginHorizontal();
            AddPositiveSpace(offsetX);
            DrawListingCell(text, width, header);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void DrawListingIconCellWithOffset(string text, string fallbackLabel, Sprite icon, float width, bool showIcon, float offsetX, float offsetY, float iconSize, float iconOffsetX, float iconOffsetY)
        {
            GUILayout.BeginVertical(GUILayout.Width(Mathf.Max(1f, width + Mathf.Max(0f, offsetX))));
            AddPositiveSpace(offsetY);
            GUILayout.BeginHorizontal();
            AddPositiveSpace(offsetX);
            DrawListingIconCell(text, fallbackLabel, icon, width, showIcon, iconSize, iconOffsetX, iconOffsetY);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void DrawListingIconCell(string text, string fallbackLabel, Sprite icon, float width, bool showIcon, float configuredIconSize, float iconOffsetX, float iconOffsetY)
        {
            try
            {
                GUILayout.BeginHorizontal(GUILayout.Width(width), GUILayout.Height(50f));
                float iconSize = Mathf.Clamp(configuredIconSize <= 0.01f ? TradingPostTableIconSize : configuredIconSize, 12f, 80f);
                if (showIcon)
                {
                    GUILayout.BeginVertical(GUILayout.Width(Mathf.Max(1f, iconSize + Mathf.Max(0f, iconOffsetX))));
                    AddSignedSpace(iconOffsetY);
                    GUILayout.BeginHorizontal();
                    AddSignedSpace(iconOffsetX);
                    Rect r = GUILayoutUtility.GetRect(iconSize, iconSize, GUILayout.Width(iconSize), GUILayout.Height(iconSize));
                    DrawSpriteOrFallback(r, icon, fallbackLabel);
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                    GUILayout.Space(TradingPostTableIconTextGap);
                }
                float textW = Mathf.Max(24f, width - (showIcon ? iconSize + TradingPostTableIconTextGap + Mathf.Max(0f, iconOffsetX) : 0f));
                GUILayout.Label(text ?? "", _mpTinyLabelStyle, GUILayout.Width(textW));
                GUILayout.EndHorizontal();
            }
            catch
            {
                DrawListingCell(text, width, false);
            }
        }

        private Sprite GetListingOfferedIcon(GtsListing listing, RichMonMetadata meta)
        {
            try
            {
                if (listing == null) return null;
                if (IsOfferedSats(listing)) return GetSatsCoinSprite();
                bool shiny = meta != null ? meta.Shiny : listing.Shiny;
                string species = meta != null ? SafeNonEmpty(meta.Species, listing.OfferedSpecies) : listing.OfferedSpecies;
                return FindRichMonIconSprite(species, listing, shiny, true);
            }
            catch { return null; }
        }

        private Sprite GetListingRequestedIcon(GtsListing listing)
        {
            try
            {
                if (listing == null) return null;
                if (IsSatsListing(listing)) return GetSatsCoinSprite();
                return FindRichMonIconSprite(listing.RequestSpecies, listing, false, false);
            }
            catch { return null; }
        }

        private static bool LoadPngIntoTexture(Texture2D tex, byte[] data)
        {
            try
            {
                // Avoid a direct ImageConversion compile dependency because some Monsterpatch
                // reference sets do not include UnityEngine.ImageConversionModule.
                Type imageConversionType = Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule");
                if (imageConversionType == null)
                    imageConversionType = Type.GetType("UnityEngine.ImageConversion, UnityEngine.CoreModule");
                if (imageConversionType != null)
                {
                    MethodInfo loadImage = imageConversionType.GetMethod("LoadImage", new Type[] { typeof(Texture2D), typeof(byte[]), typeof(bool) });
                    if (loadImage != null)
                    {
                        object result = loadImage.Invoke(null, new object[] { tex, data, false });
                        return result is bool b && b;
                    }
                }

                // Older Unity reference sets may expose LoadImage as an instance method.
                MethodInfo instanceLoadImage = typeof(Texture2D).GetMethod("LoadImage", new Type[] { typeof(byte[]) });
                if (instanceLoadImage != null)
                {
                    object result = instanceLoadImage.Invoke(tex, new object[] { data });
                    return result is bool b && b;
                }

                instanceLoadImage = typeof(Texture2D).GetMethod("LoadImage", new Type[] { typeof(byte[]), typeof(bool) });
                if (instanceLoadImage != null)
                {
                    object result = instanceLoadImage.Invoke(tex, new object[] { data, false });
                    return result is bool b && b;
                }
            }
            catch { }
            return false;
        }

        private Sprite GetSatsCoinSprite()
        {
            try
            {
                if (_mpSatsCoinSprite != null)
                    return _mpSatsCoinSprite;

                Assembly asm = typeof(GTSRuntimeHost).Assembly;
                string resourceName = null;
                foreach (string n in asm.GetManifestResourceNames())
                {
                    if (n.EndsWith("SatsCoinIcon.png", StringComparison.OrdinalIgnoreCase))
                    {
                        resourceName = n;
                        break;
                    }
                }
                if (string.IsNullOrEmpty(resourceName))
                    return null;
                using (Stream stream = asm.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                        return null;
                    byte[] data = new byte[stream.Length];
                    stream.Read(data, 0, data.Length);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    tex.filterMode = FilterMode.Point;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    if (!LoadPngIntoTexture(tex, data))
                    {
                        UnityEngine.Object.Destroy(tex);
                        return null;
                    }
                    _mpSatsCoinSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    _mpSatsCoinSprite.name = "SatsCoinIcon";
                    return _mpSatsCoinSprite;
                }
            }
            catch { return null; }
        }

        private static string FormatTimeLeft(int seconds)
        {
            if (seconds <= 0) return "Expired";
            if (seconds >= 36 * 60 * 60) return "Long\n" + (seconds / 3600) + "h";
            if (seconds >= 12 * 60 * 60) return "Medium\n" + (seconds / 3600) + "h";
            int h = seconds / 3600;
            int m = (seconds % 3600) / 60;
            if (h > 0) return h + "h " + m + "m";
            return m + "m";
        }

        private void DrawListingCard(GtsListing l, bool mine)
        {
            DrawListingTableRow(l, mine);
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
                GUILayout.Label("Your listing: " + l.OfferedSpecies + " Lv." + l.Level + (l.Shiny ? " ★" : "") + "  wants " + FormatListingRequest(l), _mpLabelStyle);
                GUILayout.Label("MoN name: " + l.DisplayName + "   " + FormatListedOn(l), _mpTinyLabelStyle);
            }
            else
            {
                GUILayout.Label(DisplayOwner(l) + " offers " + l.OfferedSpecies + " Lv." + l.Level + (l.Shiny ? " ★" : "") + "  wants " + FormatListingRequest(l), _mpLabelStyle);
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
                string wanted = listing != null ? FormatListingRequest(listing) : "MoN";

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
            if (IsSatsListing(listing))
            {
                GUILayout.Space(Mathf.Max(0f, RichListingRequestedIconColumnWidth));
                return;
            }
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

            // A previous scene/title transition can leave the auth flags set even though
            // there is no usable socket anymore.  Clear that stale state so the next
            // Online Mode click can open Steam auth instead of sitting on Connecting.
            if ((_busy || _aioAuthBusy || _keepAliveBusy) && _client == null)
            {
                _busy = false;
                _aioAuthBusy = false;
                _keepAliveBusy = false;
            }

            _aioAuthBusy = true;
            try
            {
                LoadCachedOfficialSessionTokenIfUsable();

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
                    SaveCachedOfficialSessionToken(_sessionToken, _steamId64, _username, pp.Length >= 7 ? pp[6] : "");
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
                ClearCachedOfficialSessionToken();
                _sessionToken = "";
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
                        SaveCachedOfficialSessionToken(_sessionToken, _steamId64, _username, pp.Length >= 7 ? pp[6] : "");
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
            StartCoroutine(RefreshMailCountCoroutine());
        }

        private bool SendCharacterContextIfAvailable(bool force = false)
        {
            try
            {
                if (_client == null || !_loggedIn)
                    return false;

                string characterId = string.Empty;
                string publicHandle = string.Empty;
                try
                {
                    characterId = Goose.Monsterpatch.SocialPatcher.SocialNativePatcher.SocialRuntimeHost.GetCurrentCharacterIdForAio();
                    publicHandle = Goose.Monsterpatch.SocialPatcher.SocialNativePatcher.SocialRuntimeHost.GetCurrentPublicHandleForAio();
                }
                catch { }

                characterId = (characterId ?? string.Empty).Trim();
                publicHandle = (publicHandle ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(characterId) || string.IsNullOrEmpty(publicHandle))
                    return false;

                string key = characterId + "|" + publicHandle;
                if (!force && string.Equals(key, _lastSentCharacterContextKey, StringComparison.Ordinal))
                    return true;

                string line = _client.SendReadLine("CHARACTER_CONTEXT\t" + B64Encode(characterId) + "\t" + B64Encode(publicHandle));
                string[] p = Split(line);
                if (p.Length >= 2 && p[0] == "OK" && p[1] == "CHARACTER_CONTEXT")
                {
                    _lastSentCharacterContextKey = key;
                    _mailSelfHandle = publicHandle;
                    return true;
                }

                if (DebugLogging)
                    GTSNativePatcher.RuntimeWarn(ParseErr(line, "Character context not accepted yet."));
            }
            catch (Exception ex)
            {
                if (DebugLogging)
                    GTSNativePatcher.RuntimeWarn("CHARACTER_CONTEXT failed: " + ex.Message);
            }
            return false;
        }

        private IEnumerator SearchListingsCoroutine(int page)
        {
            if (!EnsureLoggedIn()) yield break;
            _busy = true;
            _status = "Loading GTS listings...";
            yield return null;
            try
            {
                SendCharacterContextIfAvailable();
                _listings.Clear();
                string line = _client.SendReadLine(BuildListingFilterCommand("GTS_SEARCH_FILTER_PAGE", Mathf.Max(0, page)));
                ParseListingPage(line, _listings, out _pageIndex, "GTS_SEARCH_PAGE");
                _status = "Loaded " + _listings.Count + " open listings.";
                _mode = "Browse";
                StartCoroutine(RefreshMailCountCoroutine());
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
                SendCharacterContextIfAvailable();
                _myListings.Clear();
                string line = _client.SendReadLine(BuildListingFilterCommand("GTS_MY_LISTINGS_FILTER_PAGE", Mathf.Max(0, page)));
                ParseListingPage(line, _myListings, out _pageIndex, "GTS_MY_LISTINGS_PAGE");
                _status = "Loaded " + _myListings.Count + " of your open listings.";
                _mode = "Mine";
                StartCoroutine(RefreshMailCountCoroutine());
            }
            catch (Exception ex) { _status = ex.Message; GTSNativePatcher.RuntimeWarn(ex.Message); }
            _busy = false;
        }

        private string BuildListingFilterCommand(string command, int page)
        {
            string offered = NormalizeFilterValue(_filterOffered, "All");
            string requested = NormalizeFilterValue(_filterRequested, "All");
            string type = NormalizeFilterValue(_filterType, "All");
            string timeLeft = NormalizeFilterValue(_filterTimeLeft, "All");
            return command + "\t" + Mathf.Max(0, page)
                + "\t" + B64Encode(_filterSearchText ?? "")
                + "\t" + offered
                + "\t" + requested
                + "\t" + type
                + "\t" + timeLeft
                + "\t" + B64Encode(_filterSeller ?? "");
        }

        private IEnumerator CreateListingCoroutine()
        {
            if (!EnsureLoggedIn()) yield break;
            GameScript gs = FindObjectOfType<GameScript>();
            BoxManager bm = gs != null ? gs.boxManager : null;

            string offerType = OfferedIsSats() ? "SATS" : "MON";
            string requestType = RequestIsSats() ? "SATS" : "MON";
            if (offerType == "SATS" && requestType == "SATS")
            {
                _status = "SATS-for-SATS posts are not allowed.";
                yield break;
            }

            Mon mon = null;
            int slot = -1;
            int offeredSats = 0;
            string offeredSpecies = "SATS";
            int offeredLevel = 0;
            string offeredNameB64 = B64Encode("SATS");
            int offeredGender = 0;
            int offeredShiny = 0;
            string offeredBlob = "";

            if (offerType == "SATS")
            {
                if (gs == null) { _status = "Game state is not available."; yield break; }
                offeredSats = ParseInt(_offeredSatsText, 0);
                if (offeredSats <= 0) { _status = "Enter offered SATS greater than 0."; yield break; }
                if (GameScript.sats < offeredSats) { _status = "Not enough SATS. Need " + offeredSats + "."; yield break; }
            }
            else
            {
                mon = GetOfferedBoxMon(gs, bm, out slot, out string whyNot);
                if (mon == null) { _status = "Cannot list: " + whyNot; yield break; }
                offeredSpecies = GetSpecies(mon);
                offeredLevel = GetLevel(mon);
                offeredNameB64 = B64Encode(GetMonDisplayName(mon));
                offeredGender = mon.gender;
                offeredShiny = mon.isShiny ? 1 : 0;
                offeredBlob = BuildMonBlob(gs, mon);
            }

            string requestValue = "";
            if (requestType == "SATS")
            {
                int satsPrice = ParseInt(_requestSatsText, 0);
                if (satsPrice <= 0) { _status = "Enter a SATS amount greater than 0."; yield break; }
                requestValue = satsPrice.ToString();
            }
            else
            {
                if (string.IsNullOrWhiteSpace(_requestSpecies)) { _status = "Choose a Requested MoN first."; yield break; }
                requestValue = _requestSpecies.Trim();
            }

            _busy = true;
            _status = "Creating Trading Post listing...";
            yield return null;
            try
            {
                SendCharacterContextIfAvailable(true);
                string cmd = "GTS_CREATE3\t" + offerType + "\t" + offeredSats + "\t" + requestType + "\t" + requestValue + "\t" + offeredSpecies + "\t" + offeredLevel + "\t" + offeredNameB64 + "\t" + offeredGender + "\t" + offeredShiny + "\t" + offeredBlob;
                string line = _client.SendReadLine(cmd);
                string[] p = Split(line);
                if (p.Length < 2 || p[0] != "OK" || p[1] != "GTS_CREATE") throw new Exception(ParseErr(line, "Could not create listing."));

                if (offerType == "SATS")
                {
                    gs.AddSATS(-offeredSats);
                    _status = "Post listed. " + offeredSats + " SATS deposited.";
                }
                else
                {
                    bm.boxMons[slot] = null;
                    if (bm.subMenu != null) bm.subMenu.SetActive(false);
                    try { bm.RefreshBox(); } catch { }
                    _offeredBoxSlot = -1;
                    _status = "Post listed. Offered MoN removed from storage.";
                }

                ForceSaveAfterTradingPostMutation(gs, "create listing");
                _showCreateListingWindow = false;
                _offeredMonDropdownOpen = false;
                _offeredKindDropdownOpen = false;
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
            if (IsSatsListing(listing))
            {
                _status = "This post wants SATS, not a MoN offer.";
                yield break;
            }
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
                SendCharacterContextIfAvailable(true);
                string blob = BuildMonBlob(gs, mon);
                string cmd = "GTS_OFFER	" + listing.Id + "	" + GetSpecies(mon) + "	" + GetLevel(mon) + "	" + B64Encode(GetMonDisplayName(mon)) + "	" + mon.gender + "	" + (mon.isShiny ? 1 : 0) + "	" + blob;
                string line = _client.SendReadLine(cmd);
                string[] p = Split(line);
                if (p.Length < 3 || p[0] != "OK" || p[1] != "GTS_OFFER") throw new Exception(ParseErr(line, "Could not complete offer."));

                bool receivedSats = p.Length >= 4 && string.Equals(p[2], "SATS", StringComparison.OrdinalIgnoreCase);
                Mon received = null;
                int satsReceived = 0;
                if (receivedSats)
                {
                    satsReceived = ParseInt(p[3], 0);
                    if (satsReceived <= 0) throw new Exception("Invalid SATS payout from server.");
                }
                else
                {
                    string receivedBlob = (p.Length >= 4 && string.Equals(p[2], "MON", StringComparison.OrdinalIgnoreCase)) ? p[3] : p[2];
                    // Transaction safety: build/validate the received mon first.
                    // Do not remove the offered mon unless the server accepted the trade AND the received blob imports correctly.
                    received = DecodeMonBlob(gs, receivedBlob);
                    try
                    {
                        received.uniqueID = gs.curUniqueIDCounter;
                        gs.curUniqueIDCounter++;
                    }
                    catch { }
                }

                bm.boxMons[slot] = null; // only now remove the offered MoN
                if (receivedSats)
                    gs.AddSATS(satsReceived);
                else
                    bm.AddMonToBox(received);
                if (bm.subMenu != null) bm.subMenu.SetActive(false);
                try { bm.RefreshBox(); } catch { }
                ForceSaveAfterTradingPostMutation(gs, "complete MoN trade");
                _capturedBoxMon = null;
                _capturedBoxSlot = -1;
                _offeredBoxSlot = -1;
                _offeredMonDropdownOpen = false;
                _status = receivedSats ? ("Trade complete. Received " + satsReceived + " SATS.") : "Trade complete. Received listed MoN.";
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

        private IEnumerator BuySatsListingCoroutine(GtsListing listing)
        {
            if (!EnsureLoggedIn()) yield break;
            if (listing == null) { _status = "No listing selected."; yield break; }
            if (!IsSatsListing(listing)) { _status = "This post does not request SATS."; yield break; }
            if (IsOwnListing(listing)) { _status = "You cannot buy your own listing."; yield break; }

            GameScript gs = FindObjectOfType<GameScript>();
            BoxManager bm = gs != null ? gs.boxManager : null;
            if (gs == null || bm == null) { _status = "Storage is not available."; yield break; }
            if (!bm.HasSpaceInBox()) { _status = "Storage is full. Make space before buying."; yield break; }
            if (GameScript.sats < listing.RequestSats) { _status = "Not enough SATS. Need " + listing.RequestSats + "."; yield break; }

            _busy = true;
            _status = "Buying listed MoN...";
            yield return null;
            try
            {
                SendCharacterContextIfAvailable(true);
                string line = _client.SendReadLine("GTS_BUY_SATS\t" + listing.Id + "\t" + listing.RequestSats);
                string[] p = Split(line);
                if (p.Length < 3 || p[0] != "OK" || p[1] != "GTS_BUY_SATS") throw new Exception(ParseErr(line, "Could not buy listing."));

                Mon received = DecodeMonBlob(gs, p[2]);
                try
                {
                    received.uniqueID = gs.curUniqueIDCounter;
                    gs.curUniqueIDCounter++;
                }
                catch { }

                gs.AddSATS(-listing.RequestSats);
                bm.AddMonToBox(received);
                if (bm.subMenu != null) bm.subMenu.SetActive(false);
                try { bm.RefreshBox(); } catch { }
                ForceSaveAfterTradingPostMutation(gs, "buy SATS listing");
                _status = "Purchase complete. Spent " + listing.RequestSats + " SATS.";
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
            if (!IsOfferedSats(listing) && !bm.HasSpaceInBox()) { _status = "Storage is full. Make space before cancelling."; yield break; }
            _busy = true;
            _status = "Cancelling listing...";
            yield return null;
            try
            {
                SendCharacterContextIfAvailable(true);
                string line = _client.SendReadLine("GTS_CANCEL\t" + listing.Id);
                string[] p = Split(line);
                if (p.Length < 3 || p[0] != "OK" || p[1] != "GTS_CANCEL") throw new Exception(ParseErr(line, "Could not cancel listing."));
                bool returnedSats = p.Length >= 4 && string.Equals(p[2], "SATS", StringComparison.OrdinalIgnoreCase);
                if (returnedSats)
                {
                    int amount = ParseInt(p[3], 0);
                    if (amount > 0) gs.AddSATS(amount);
                    _status = "Listing cancelled and " + amount + " SATS returned.";
                }
                else
                {
                    string blob = (p.Length >= 4 && string.Equals(p[2], "MON", StringComparison.OrdinalIgnoreCase)) ? p[3] : p[2];
                    ImportBlobToBox(gs, bm, blob);
                    try { bm.RefreshBox(); } catch { }
                    _status = "Listing cancelled and MoN returned.";
                }
                ForceSaveAfterTradingPostMutation(gs, "cancel listing");
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
                int monCount = ParseInt(p[2], 0);
                if (CountEmpty(bm.boxMons) < monCount) throw new Exception("Not enough storage space for " + monCount + " MoN claim(s).");
                int imported = 0;
                int satsClaimed = 0;
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
                    else if (np.Length >= 3 && np[0] == "SATS")
                    {
                        int amount = ParseInt(np[2], 0);
                        if (amount > 0)
                        {
                            gs.AddSATS(amount);
                            satsClaimed += amount;
                        }
                    }
                    else throw new Exception("Malformed claim response.");
                }
                try { bm.RefreshBox(); } catch { }
                ForceSaveAfterTradingPostMutation(gs, "claim trades");
                if (imported == 0 && satsClaimed == 0) _status = "No completed trades to claim.";
                else if (imported > 0 && satsClaimed > 0) _status = "Claimed " + imported + " MoN(s) and " + satsClaimed + " SATS.";
                else if (imported > 0) _status = "Claimed " + imported + " MoN(s).";
                else _status = "Claimed " + satsClaimed + " SATS.";
            }
            catch (Exception ex) { _status = ex.Message; GTSNativePatcher.RuntimeWarn(ex.Message); }
            _busy = false;
        }

        private void DrawMailPanel()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Mailbox", _mpSectionTitleStyle, GUILayout.Width(100));
            GUILayout.Label("Unread: " + _mailUnreadCount + "   Claimable: " + _mailClaimableCount, _mpTinyLabelStyle, GUILayout.Width(210));
            if (!string.IsNullOrEmpty(_mailSelfHandle)) GUILayout.Label("Address: " + _mailSelfHandle, _mpTinyLabelStyle, GUILayout.Width(180));
            GUILayout.FlexibleSpace();
            GUI.enabled = !_busy;
            if (GUILayout.Button("Compose Mail", _mpButtonStyle, GUILayout.Width(MailComposeOpenButtonWidth), GUILayout.Height(MailComposeOpenButtonHeight)))
            {
                _showComposeMail = true;
                UpdateMailComposeDrawerRect();
            }
            GUI.enabled = true;
            AddPositiveSpace(TradingPostRefreshButtonOffsetX);
            GUI.enabled = !_busy;
            if (GUILayout.Button("Refresh", _mpButtonStyle, GUILayout.Width(90), GUILayout.Height(24 + TradingPostRefreshButtonOffsetY))) StartCoroutine(MailListCoroutine(_mailPageIndex));
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(_mpCardStyle, GUILayout.Width(Mathf.Max(260f, _windowRect.width * 0.45f)));
            GUILayout.Label("Inbox", _mpSectionTitleStyle);
            _mailScroll = GUILayout.BeginScrollView(_mailScroll, GUILayout.Height(Mathf.Max(240f, _windowRect.height - 220f)));
            if (_mailItems.Count == 0)
            {
                GUILayout.Label("No mail loaded.", _mpTinyLabelStyle);
            }
            foreach (MailItem mail in _mailItems)
                DrawMailBanner(mail);
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(_mpCardStyle);
            DrawMailDetail();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

        }

        private void DrawMailBanner(MailItem mail)
        {
            if (mail == null) return;
            GUILayout.BeginVertical(_mpCardStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label((mail.IsRead ? "" : "● ") + SafeNonEmpty(mail.Subject, "Mail"), _mpSectionTitleStyle, GUILayout.Width(190));
            GUILayout.FlexibleSpace();
            GUI.enabled = !_busy;
            if (GUILayout.Button("View", _mpButtonStyle, GUILayout.Width(55), GUILayout.Height(24))) StartCoroutine(ViewMailCoroutine(mail.Id));
            if (GUILayout.Button("Delete", _mpButtonStyle, GUILayout.Width(65), GUILayout.Height(24))) StartCoroutine(DeleteMailCoroutine(mail.Id));
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            string attach = FormatMailAttachmentLabel(mail);
            GUILayout.Label("From: " + SafeNonEmpty(mail.Sender, "System") + "   " + attach, _mpTinyLabelStyle);
            GUILayout.Label("Received: " + SafeNonEmpty(mail.CreatedAtRaw, "?") + "   Expires: " + SafeNonEmpty(mail.ExpiresAtRaw, "?"), _mpTinyLabelStyle);
            GUILayout.EndVertical();
        }

        private void DrawMailDetail()
        {
            if (_selectedMail == null)
            {
                GUILayout.Label("Select a mail to view it.", _mpTinyLabelStyle);
                return;
            }

            GUILayout.Label(SafeNonEmpty(_selectedMail.Subject, "Mail"), _mpSectionTitleStyle);
            GUILayout.Label("From: " + SafeNonEmpty(_selectedMail.Sender, "System"), _mpTinyLabelStyle);
            GUILayout.Label("Received: " + SafeNonEmpty(_selectedMail.CreatedAtRaw, "?"), _mpTinyLabelStyle);
            GUILayout.Space(4);
            _mailDetailScroll = GUILayout.BeginScrollView(_mailDetailScroll, GUILayout.Height(150));
            DrawMailBodyWithClickableLinks(_selectedMail.Body ?? "");
            GUILayout.EndScrollView();

            if (_selectedMail.HasClaimableAttachment)
            {
                GUILayout.Space(5);
                string label = GetMailClaimButtonLabel(_selectedMail);
                GUI.enabled = !_busy;
                if (GUILayout.Button(label, _mpButtonStyle, GUILayout.Height(30))) StartCoroutine(ClaimMailAttachmentCoroutine(_selectedMail.Id));
                GUI.enabled = true;
            }
            else if (_selectedMail.AttachmentClaimed)
            {
                GUILayout.Label("Attachment already claimed.", _mpTinyLabelStyle);
            }

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUI.enabled = !_busy;
            if (GUILayout.Button("Reply", _mpButtonStyle, GUILayout.Width(80), GUILayout.Height(26)))
                BeginReplyToSelectedMail();
            if (GUILayout.Button(_selectedMail.IsSaved ? "Unsave" : "Save", _mpButtonStyle, GUILayout.Width(80), GUILayout.Height(26)))
                StartCoroutine(SaveMailCoroutine(_selectedMail.Id, !_selectedMail.IsSaved));
            if (GUILayout.Button("Delete", _mpButtonStyle, GUILayout.Width(80), GUILayout.Height(26)))
                StartCoroutine(DeleteMailCoroutine(_selectedMail.Id));
            if (GUILayout.Button("Close", _mpButtonStyle, GUILayout.Width(80), GUILayout.Height(26)))
                _selectedMail = null;
            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        private bool MailAttachmentIsMon()
        {
            return string.Equals(_mailAttachmentKind, "MON", StringComparison.OrdinalIgnoreCase);
        }

        private bool MailAttachmentIsSats()
        {
            return string.Equals(_mailAttachmentKind, "SATS", StringComparison.OrdinalIgnoreCase);
        }

        private string SafeMailAttachmentKindLabel()
        {
            if (MailAttachmentIsMon()) return "MoN";
            if (MailAttachmentIsSats()) return "SATS";
            return "None";
        }

        private void CycleMailAttachmentKind()
        {
            if (MailAttachmentIsMon()) _mailAttachmentKind = "SATS";
            else if (MailAttachmentIsSats()) _mailAttachmentKind = "NONE";
            else _mailAttachmentKind = "MON";
            _mailAttachmentMonDropdownOpen = false;
        }

        private Mon GetMailAttachmentBoxMon(GameScript gs, BoxManager bm, out int slot, out string whyNot)
        {
            slot = -1;
            whyNot = "Choose a MoN attachment.";
            if (gs == null || bm == null || bm.boxMons == null)
            {
                whyNot = "Storage is not available yet.";
                return null;
            }
            if (_mailAttachmentBoxSlot >= 0 && _mailAttachmentBoxSlot < bm.boxMons.Length && bm.boxMons[_mailAttachmentBoxSlot] != null)
            {
                slot = _mailAttachmentBoxSlot;
                whyNot = "";
                return bm.boxMons[slot];
            }
            for (int i = 0; i < bm.boxMons.Length; i++)
            {
                if (bm.boxMons[i] != null)
                {
                    _mailAttachmentBoxSlot = i;
                    slot = i;
                    whyNot = "";
                    return bm.boxMons[i];
                }
            }
            return null;
        }

        private void RefreshMailAttachmentMonOptions(GameScript gs, BoxManager bm)
        {
            _offeredMonOptions.Clear();
            try
            {
                if (gs == null || bm == null || bm.boxMons == null) return;
                for (int i = 0; i < bm.boxMons.Length; i++)
                {
                    Mon mon = bm.boxMons[i];
                    if (mon == null) continue;
                    int box = i / 27 + 1;
                    string label = "Box " + box + ": " + GetMonDisplayName(mon) + " Lv." + GetLevel(mon) + (mon.isShiny ? " ★" : "");
                    _offeredMonOptions.Add(new BoxMonOption { Slot = i, Label = label });
                }
            }
            catch (Exception ex)
            {
                if (DebugLogging) GTSNativePatcher.RuntimeWarn("Failed to refresh mail attachment MoN options: " + ex.Message);
            }
        }

        private void DrawMailAttachmentControls()
        {
            GameScript gs = FindObjectOfType<GameScript>();
            BoxManager bm = gs != null ? gs.boxManager : null;
            GUILayout.BeginHorizontal();
            DrawOffsetLabel("Attach:", MailComposeAttachmentLabelWidth, MailComposeAttachmentLabelHeight, MailComposeAttachmentLabelOffsetX, MailComposeAttachmentLabelOffsetY);
            AddSignedSpace(MailComposeAttachmentTypeOffsetX);
            AddSignedSpace(MailComposeAttachmentTypeOffsetY);
            if (GUILayout.Button(SafeMailAttachmentKindLabel() + " ▼", _mpButtonStyle, GUILayout.Width(MailComposeAttachmentTypeWidth), GUILayout.Height(MailComposeAttachmentTypeHeight)))
                CycleMailAttachmentKind();
            AddSignedSpace(MailComposeAttachmentValueOffsetX);
            AddSignedSpace(MailComposeAttachmentValueOffsetY);
            if (MailAttachmentIsSats())
            {
                GUI.SetNextControlName("GTS_MAIL_ATTACHMENT_SATS");
                string next = GUILayout.TextField(_mailAttachmentSatsText ?? string.Empty, _mpTextFieldStyle, GUILayout.Width(MailComposeAttachmentValueWidth), GUILayout.Height(MailComposeAttachmentValueHeight));
                _mailAttachmentSatsText = DigitsOnly(next, 9);
            }
            else if (MailAttachmentIsMon())
            {
                Mon mon = GetMailAttachmentBoxMon(gs, bm, out int slot, out string whyNot);
                string label = mon == null ? whyNot : ("Box " + (slot / 27 + 1) + ": " + GetMonDisplayName(mon) + " Lv." + GetLevel(mon) + (mon.isShiny ? " ★" : ""));
                Sprite icon = mon != null ? (GetBattleSpriteFromMon(mon, mon.isShiny) ?? FindSpriteOnObject(mon.monScriptableObject, true) ?? FindSpriteOnObject(mon, true)) : null;
                Rect rect = GUILayoutUtility.GetRect(MailComposeAttachmentValueWidth, MailComposeAttachmentValueHeight, GUILayout.Width(MailComposeAttachmentValueWidth), GUILayout.Height(MailComposeAttachmentValueHeight));
                GUI.Box(rect, GUIContent.none, _mpTextFieldStyle);
                DrawSelectorContent(rect, label, icon, mon != null ? GetSpecies(mon) : "MON", true, RequestedMONIconSize, RequestedMONIconOffsetX, RequestedMONIconOffsetY, RequestedMONTextOffsetX, RequestedMONTextOffsetY, false);
                AddSignedSpace(MailComposeAttachmentArrowOffsetX);
                AddSignedSpace(MailComposeAttachmentArrowOffsetY);
                if (GUILayout.Button(_mailAttachmentMonDropdownOpen ? "▲" : "▼", _mpButtonStyle, GUILayout.Width(MailComposeAttachmentArrowWidth), GUILayout.Height(MailComposeAttachmentValueHeight)))
                {
                    RefreshMailAttachmentMonOptions(gs, bm);
                    _mailAttachmentMonDropdownOpen = !_mailAttachmentMonDropdownOpen;
                    _mailAttachmentMonDropdownScroll = Vector2.zero;
                }
            }
            else
            {
                GUILayout.Label("No attachment", _mpTinyLabelStyle, GUILayout.Width(MailComposeAttachmentValueWidth), GUILayout.Height(MailComposeAttachmentValueHeight));
            }
            GUILayout.EndHorizontal();
            if (_mailAttachmentMonDropdownOpen && MailAttachmentIsMon())
                DrawMailAttachmentMonDropdown(gs, bm);
        }

        private void DrawMailAttachmentMonDropdown(GameScript gs, BoxManager bm)
        {
            GUILayout.BeginHorizontal();
            AddSignedSpace(MailComposeAttachmentDropdownOffsetX + MailComposeAttachmentLabelWidth + MailComposeAttachmentTypeWidth + 8f);
            AddSignedSpace(MailComposeAttachmentDropdownOffsetY);
            GUILayout.BeginVertical(_mpCardStyle, GUILayout.Width(MailComposeAttachmentDropdownWidth));
            GUILayout.Label("Choose MoN Attachment", _mpSectionTitleStyle);
            if (_offeredMonOptions.Count == 0)
            {
                GUILayout.Label("No local storage MoN found.", _mpTinyLabelStyle);
            }
            else
            {
                _mailAttachmentMonDropdownScroll = GUILayout.BeginScrollView(_mailAttachmentMonDropdownScroll, GUILayout.Height(MailComposeAttachmentDropdownHeight));
                foreach (BoxMonOption option in _offeredMonOptions)
                {
                    Sprite rowIcon = null;
                    Mon optionMon = null;
                    try
                    {
                        if (bm != null && bm.boxMons != null && option.Slot >= 0 && option.Slot < bm.boxMons.Length)
                            optionMon = bm.boxMons[option.Slot];
                        if (optionMon != null)
                            rowIcon = GetBattleSpriteFromMon(optionMon, optionMon.isShiny) ?? FindSpriteOnObject(optionMon.monScriptableObject, true) ?? FindSpriteOnObject(optionMon, true);
                    }
                    catch { }
                    string species = optionMon != null ? GetSpecies(optionMon) : "MON";
                    if (DrawSelectorRow(option.Label, rowIcon, species, OfferedMONRowHeight, OfferedMONShowIcon, OfferedMONIconSize, OfferedMONIconOffsetX, OfferedMONIconOffsetY, OfferedMONTextOffsetX, OfferedMONTextOffsetY))
                    {
                        _mailAttachmentBoxSlot = option.Slot;
                        _mailAttachmentMonDropdownOpen = false;
                    }
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void DrawMailComposeWindow(int id)
        {
            EnsureGtsGuiStyles();
            GUILayout.BeginVertical();
            Rect titleRect = GUILayoutUtility.GetRect(1f, 32f, GUILayout.ExpandWidth(true), GUILayout.Height(32f));
            Rect titled = new Rect(titleRect.x + MailComposeTitleOffsetX, titleRect.y + MailComposeTitleOffsetY, titleRect.width, titleRect.height);
            GUI.Label(titled, "Compose Mail", _mpTitleStyle);
            Rect closeRect = new Rect(titleRect.xMax - 34f, titleRect.y + 4f, 30f, 24f);
            if (GUI.Button(closeRect, "X", _mpCloseButtonStyle))
                _showComposeMail = false;
            DrawHorizontalRule();
            GUILayout.Space(6);

            DrawComposeTextFieldRow("To:", ref _mailRecipient,
                MailComposeToLabelOffsetX, MailComposeToLabelOffsetY, MailComposeToLabelWidth, MailComposeToLabelHeight,
                MailComposeToFieldOffsetX, MailComposeToFieldOffsetY, MailComposeToFieldWidth, MailComposeToFieldHeight,
                "Player#1234 or MMOMailmon");
            GUILayout.Space(5);
            DrawComposeTextFieldRow("Subject:", ref _mailSubject,
                MailComposeSubjectLabelOffsetX, MailComposeSubjectLabelOffsetY, MailComposeSubjectLabelWidth, MailComposeSubjectLabelHeight,
                MailComposeSubjectFieldOffsetX, MailComposeSubjectFieldOffsetY, MailComposeSubjectFieldWidth, MailComposeSubjectFieldHeight,
                "Subject");
            GUILayout.Space(5);

            DrawMailAttachmentControls();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            DrawOffsetLabel("Body:", MailComposeBodyLabelWidth, MailComposeBodyLabelHeight, MailComposeBodyLabelOffsetX, MailComposeBodyLabelOffsetY);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            AddSignedSpace(MailComposeBodyFieldOffsetX);
            AddSignedSpace(MailComposeBodyFieldOffsetY);
            GUI.SetNextControlName("GTS_MAIL_BODY");
            _mailBody = GUILayout.TextArea(_mailBody ?? "", _mpTextFieldStyle, GUILayout.Width(MailComposeBodyFieldWidth), GUILayout.Height(MailComposeBodyFieldHeight));
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.enabled = !_busy;
            if (DrawOffsetButton("Send", MailComposeSendButtonWidth, MailComposeSendButtonHeight, MailComposeSendButtonOffsetX, MailComposeSendButtonOffsetY)) StartCoroutine(SendMailCoroutine());
            if (DrawOffsetButton("Cancel", MailComposeCancelButtonWidth, MailComposeCancelButtonHeight, MailComposeCancelButtonOffsetX, MailComposeCancelButtonOffsetY)) _showComposeMail = false;
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0, 0, Mathf.Max(0f, _mailComposeWindowRect.width - 40f), 44));
        }

        private void DrawComposeTextFieldRow(string label, ref string value, float labelX, float labelY, float labelW, float labelH, float fieldX, float fieldY, float fieldW, float fieldH, string placeholder)
        {
            GUILayout.BeginHorizontal();
            DrawOffsetLabel(label, labelW, labelH, labelX, labelY);
            value = DrawOffsetPlaceholderTextField(value, placeholder, fieldW, fieldH, fieldX, fieldY);
            GUILayout.EndHorizontal();
        }

        private void DrawOffsetLabel(string text, float width, float height, float offsetX, float offsetY)
        {
            width = Mathf.Max(20f, width);
            height = Mathf.Max(18f, height);
            GUILayout.BeginVertical(GUILayout.Width(Mathf.Max(1f, width + Mathf.Max(0f, offsetX))), GUILayout.Height(Mathf.Max(height, height + Math.Abs(offsetY))));
            AddSignedSpace(offsetY);
            GUILayout.BeginHorizontal();
            AddSignedSpace(offsetX);
            GUILayout.Label(text, _mpTinyLabelStyle, GUILayout.Width(width), GUILayout.Height(height));
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void DrawMailBodyWithClickableLinks(string body)
        {
            string[] lines = (body ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            foreach (string raw in lines)
            {
                string line = raw ?? "";
                string trimmed = line.Trim();
                if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    if (GUILayout.Button(trimmed, _mpButtonStyle, GUILayout.Height(26)))
                        Application.OpenURL(trimmed);
                }
                else
                {
                    GUILayout.Label(line, _mpLabelStyle);
                }
            }
        }

        private void BeginReplyToSelectedMail()
        {
            try
            {
                if (_selectedMail == null)
                    return;
                string sender = SafeNonEmpty(_selectedMail.Sender, "");
                if (string.IsNullOrWhiteSpace(sender) || sender.Equals("System", StringComparison.OrdinalIgnoreCase) || sender.Equals("Trading Post", StringComparison.OrdinalIgnoreCase))
                {
                    _status = "This mail cannot be replied to automatically.";
                    return;
                }
                _mailRecipient = sender;
                string subject = SafeNonEmpty(_selectedMail.Subject, "Mail");
                _mailSubject = subject.StartsWith("Re:", StringComparison.OrdinalIgnoreCase) ? subject : "Re: " + subject;
                _mailBody = "";
                _showComposeMail = true;
                UpdateMailComposeDrawerRect();
                _status = "Reply started. To field filled with " + sender + ".";
            }
            catch (Exception ex)
            {
                _status = "Could not start reply: " + ex.Message;
            }
        }

        private IEnumerator RefreshMailCountCoroutine()
        {
            if (!EnsureLoggedIn()) yield break;
            if (_client == null) yield break;
            yield return null;
            try
            {
                SendCharacterContextIfAvailable();
                string line = _client.SendReadLine("MAIL_COUNT");
                string[] p = Split(line);
                if (p.Length >= 5 && p[0] == "OK" && p[1] == "MAIL_COUNT")
                {
                    _mailUnreadCount = ParseInt(p[2], 0);
                    _mailTotalCount = ParseInt(p[3], 0);
                    _mailClaimableCount = ParseInt(p[4], 0);
                    if (p.Length >= 6) _mailSelfHandle = B64DecodeSafe(p[5]);
                }
            }
            catch (Exception ex)
            {
                if (DebugLogging) GTSNativePatcher.RuntimeWarn("MAIL_COUNT failed: " + ex.Message);
            }
        }

        private IEnumerator MailListCoroutine(int page)
        {
            if (!EnsureLoggedIn()) yield break;
            _busy = true;
            _status = "Loading mail...";
            yield return null;
            try
            {
                SendCharacterContextIfAvailable(true);
                _mailItems.Clear();
                string line = _client.SendReadLine("MAIL_LIST\t" + Mathf.Max(0, page));
                string[] p = Split(line);
                if (p.Length < 6 || p[0] != "OK" || p[1] != "MAIL_LIST") throw new Exception(ParseErr(line, "Could not load mail."));
                _mailPageIndex = ParseInt(p[2], 0);
                _mailTotalCount = ParseInt(p[4], 0);
                _mailUnreadCount = ParseInt(p[5], 0);
                _mailClaimableCount = p.Length >= 7 ? ParseInt(p[6], 0) : _mailClaimableCount;
                if (p.Length >= 8) _mailSelfHandle = B64DecodeSafe(p[7]);
                while (true)
                {
                    string next = _client.ReadLine();
                    string[] mp = Split(next);
                    if (mp.Length >= 1 && mp[0] == "END") break;
                    if (mp.Length < 11 || mp[0] != "MAIL") throw new Exception("Malformed mail response.");
                    _mailItems.Add(new MailItem
                    {
                        Id = ParseInt(mp[1], 0),
                        Sender = B64DecodeSafe(mp[2]),
                        Subject = B64DecodeSafe(mp[3]),
                        CreatedAtRaw = mp[4],
                        IsRead = ParseInt(mp[5], 0) != 0,
                        IsSaved = ParseInt(mp[6], 0) != 0,
                        AttachmentType = SafeNonEmpty(mp[7], "NONE").ToUpperInvariant(),
                        AttachmentSats = ParseInt(mp[8], 0),
                        AttachmentClaimed = ParseInt(mp[9], 0) != 0,
                        MailType = mp[10],
                        ExpiresAtRaw = mp.Length >= 12 ? mp[11] : ""
                    });
                }
                _mode = "Mail";
                _status = "Loaded " + _mailItems.Count + " mail(s).";
            }
            catch (Exception ex) { _status = ex.Message; GTSNativePatcher.RuntimeWarn(ex.Message); }
            _busy = false;
        }

        private IEnumerator ViewMailCoroutine(int mailId)
        {
            if (!EnsureLoggedIn()) yield break;
            _busy = true;
            _status = "Opening mail...";
            yield return null;
            try
            {
                SendCharacterContextIfAvailable(true);
                string line = _client.SendReadLine("MAIL_VIEW\t" + mailId);
                string[] p = Split(line);
                if (p.Length < 14 || p[0] != "OK" || p[1] != "MAIL_VIEW") throw new Exception(ParseErr(line, "Could not open mail."));
                _selectedMail = new MailDetail
                {
                    Id = ParseInt(p[2], 0),
                    Sender = B64DecodeSafe(p[3]),
                    Subject = B64DecodeSafe(p[4]),
                    Body = B64DecodeSafe(p[5]),
                    CreatedAtRaw = p[6],
                    IsRead = ParseInt(p[7], 0) != 0,
                    IsSaved = ParseInt(p[8], 0) != 0,
                    AttachmentType = SafeNonEmpty(p[9], "NONE").ToUpperInvariant(),
                    AttachmentSats = ParseInt(p[10], 0),
                    SourceListingId = ParseInt(p[11], 0),
                    AttachmentClaimed = ParseInt(p[12], 0) != 0,
                    MailType = p[13],
                    ExpiresAtRaw = p.Length >= 15 ? p[14] : ""
                };
                _mailDetailScroll = Vector2.zero;
                _status = "Mail opened.";
                StartCoroutine(RefreshMailCountCoroutine());
            }
            catch (Exception ex) { _status = ex.Message; GTSNativePatcher.RuntimeWarn(ex.Message); }
            _busy = false;
        }

        private IEnumerator DeleteMailCoroutine(int mailId)
        {
            if (!EnsureLoggedIn()) yield break;
            _busy = true;
            _status = "Deleting mail...";
            yield return null;
            try
            {
                SendCharacterContextIfAvailable(true);
                string line = _client.SendReadLine("MAIL_DELETE\t" + mailId);
                string[] p = Split(line);
                if (p.Length < 3 || p[0] != "OK" || p[1] != "MAIL_DELETE") throw new Exception(ParseErr(line, "Could not delete mail."));
                if (_selectedMail != null && _selectedMail.Id == mailId) _selectedMail = null;
                _status = "Mail deleted.";
                StartCoroutine(MailListCoroutine(_mailPageIndex));
            }
            catch (Exception ex) { _status = ex.Message; GTSNativePatcher.RuntimeWarn(ex.Message); }
            _busy = false;
        }

        private IEnumerator SaveMailCoroutine(int mailId, bool save)
        {
            if (!EnsureLoggedIn()) yield break;
            _busy = true;
            yield return null;
            try
            {
                SendCharacterContextIfAvailable(true);
                string line = _client.SendReadLine("MAIL_SAVE\t" + mailId + "\t" + (save ? 1 : 0));
                string[] p = Split(line);
                if (p.Length < 4 || p[0] != "OK" || p[1] != "MAIL_SAVE") throw new Exception(ParseErr(line, "Could not update mail save state."));
                if (_selectedMail != null && _selectedMail.Id == mailId) _selectedMail.IsSaved = save;
                _status = save ? "Mail saved." : "Mail unsaved.";
            }
            catch (Exception ex) { _status = ex.Message; GTSNativePatcher.RuntimeWarn(ex.Message); }
            _busy = false;
        }

        private IEnumerator ClaimMailAttachmentCoroutine(int mailId)
        {
            if (!EnsureLoggedIn()) yield break;
            GameScript gs = FindObjectOfType<GameScript>();
            BoxManager bm = gs != null ? gs.boxManager : null;
            if (gs == null || bm == null) { _status = "Storage not available."; yield break; }
            _busy = true;
            _status = "Claiming mail attachment...";
            yield return null;
            try
            {
                SendCharacterContextIfAvailable(true);
                string line = _client.SendReadLine("MAIL_CLAIM\t" + mailId);
                string[] p = Split(line);
                if (p.Length < 5 || p[0] != "OK" || p[1] != "MAIL_CLAIM") throw new Exception(ParseErr(line, "Could not claim mail attachment."));
                string type = SafeNonEmpty(p[2], "NONE").ToUpperInvariant();
                if (type == "MON")
                {
                    if (!bm.HasSpaceInBox()) throw new Exception("Storage is full. Make space before claiming.");
                    ImportBlobToBox(gs, bm, p[4]);
                    try { bm.RefreshBox(); } catch { }
                    ForceSaveAfterTradingPostMutation(gs, "claim mail MoN");
                    _status = "Claimed mail MoN attachment.";
                }
                else if (type == "SATS")
                {
                    int amount = ParseInt(p[4], 0);
                    if (amount > 0) gs.AddSATS(amount);
                    ForceSaveAfterTradingPostMutation(gs, "claim mail SATS");
                    _status = "Claimed " + amount + " SATS from mail.";
                }
                else if (type == "ITEM")
                {
                    string itemSpec = p.Length >= 5 ? B64DecodeSafe(p[4]) : "";
                    int amount = p.Length >= 6 ? ParseInt(p[5], 1) : 1;
                    int itemId = ResolveAdminRewardItemId(gs, itemSpec);
                    if (itemId < 0) throw new Exception("Unknown item: " + itemSpec);
                    gs.AddItem(itemId, Mathf.Max(1, amount));
                    ForceSaveAfterTradingPostMutation(gs, "claim mail item");
                    _status = "Claimed " + Mathf.Max(1, amount) + "x " + GetItemDisplayName(gs, itemId, itemSpec) + " from mail.";
                }
                else if (type == "MON_REWARD")
                {
                    if (!bm.HasSpaceInBox()) throw new Exception("Storage is full. Make space before claiming.");
                    string monSpec = p.Length >= 5 ? B64DecodeSafe(p[4]) : "";
                    int level = p.Length >= 6 ? ParseInt(p[5], 1) : 1;
                    bool shiny = p.Length >= 7 && ParseInt(p[6], 0) != 0;
                    MonScriptableObject mso = ResolveAdminRewardMon(gs, monSpec);
                    if (mso == null) throw new Exception("Unknown MoN: " + monSpec);
                    Mon mon = BuildAdminRewardMon(gs, mso, level, shiny);
                    bm.AddMonToBox(mon);
                    try { bm.RefreshBox(); } catch { }
                    ForceSaveAfterTradingPostMutation(gs, "claim mail MoN reward");
                    _status = "Claimed reward MoN " + SafeNonEmpty(mso.monName, monSpec) + " Lv." + Mathf.Clamp(level <= 0 ? 1 : level, 1, 100) + (shiny ? " ★" : "") + ".";
                }
                if (_selectedMail != null && _selectedMail.Id == mailId) _selectedMail.AttachmentClaimed = true;
                StartCoroutine(MailListCoroutine(_mailPageIndex));
            }
            catch (Exception ex) { _status = ex.Message; GTSNativePatcher.RuntimeWarn(ex.Message); }
            _busy = false;
        }

        private IEnumerator SendMailCoroutine()
        {
            if (!EnsureLoggedIn()) yield break;
            if (string.IsNullOrWhiteSpace(_mailRecipient)) { _status = "Enter a mail recipient."; yield break; }
            if (string.IsNullOrWhiteSpace(_mailSubject)) { _status = "Enter a mail subject."; yield break; }
            GameScript gs = FindObjectOfType<GameScript>();
            BoxManager bm = gs != null ? gs.boxManager : null;
            string attachmentType = "NONE";
            int attachmentSats = 0;
            string attachmentBlob = "";
            int attachmentSlot = -1;
            if (MailAttachmentIsSats())
            {
                if (gs == null) { _status = "Game state is not available."; yield break; }
                attachmentType = "SATS";
                attachmentSats = ParseInt(_mailAttachmentSatsText, 0);
                if (attachmentSats <= 0) { _status = "Enter attached SATS greater than 0."; yield break; }
                if (GameScript.sats < attachmentSats) { _status = "Not enough SATS. Need " + attachmentSats + "."; yield break; }
            }
            else if (MailAttachmentIsMon())
            {
                Mon mon = GetMailAttachmentBoxMon(gs, bm, out attachmentSlot, out string whyNot);
                if (mon == null) { _status = "Cannot attach MoN: " + whyNot; yield break; }
                attachmentType = "MON";
                attachmentBlob = BuildMonBlob(gs, mon);
            }
            _busy = true;
            _status = "Sending mail...";
            yield return null;
            try
            {
                SendCharacterContextIfAvailable(true);
                string cmd = "MAIL_SEND\t" + B64Encode(_mailRecipient.Trim()) + "\t" + B64Encode(_mailSubject.Trim()) + "\t" + B64Encode(_mailBody ?? "") + "\t" + attachmentType + "\t" + attachmentSats + "\t" + attachmentBlob;
                string line = _client.SendReadLine(cmd);
                string[] p = Split(line);
                if (p.Length < 3 || p[0] != "OK" || p[1] != "MAIL_SEND") throw new Exception(ParseErr(line, "Could not send mail."));
                if (attachmentType == "SATS")
                    gs.AddSATS(-attachmentSats);
                else if (attachmentType == "MON" && bm != null && attachmentSlot >= 0 && attachmentSlot < bm.boxMons.Length)
                {
                    bm.boxMons[attachmentSlot] = null;
                    try { bm.RefreshBox(); } catch { }
                }
                if (attachmentType != "NONE" && gs != null)
                    ForceSaveAfterTradingPostMutation(gs, "send mail attachment");
                _mailRecipient = "";
                _mailSubject = "";
                _mailBody = "";
                _mailAttachmentKind = "NONE";
                _mailAttachmentSatsText = "";
                _mailAttachmentBoxSlot = -1;
                _mailAttachmentMonDropdownOpen = false;
                _showComposeMail = false;
                _status = "Mail sent.";
                StartCoroutine(MailListCoroutine(_mailPageIndex));
            }
            catch (Exception ex) { _status = ex.Message; GTSNativePatcher.RuntimeWarn(ex.Message); }
            _busy = false;
        }

        private void ForceSaveAfterTradingPostMutation(GameScript gs, string reason)
        {
            try
            {
                if (gs == null)
                    return;

                MethodInfo save = gs.GetType().GetMethod("ActuallySaveGame", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                if (save != null && save.GetParameters().Length == 0)
                {
                    save.Invoke(save.IsStatic ? null : (object)gs, null);
                    if (DebugLogging) GTSNativePatcher.RuntimeLog("Trading Post force-saved through GameScript.ActuallySaveGame after " + reason + ".");
                    return;
                }

                // Last-resort fallback. This should normally never run on Monsterpatch 0.181,
                // but it is safer than skipping persistence if a future game build renames ActuallySaveGame.
                gs.SaveGame();
                if (DebugLogging) GTSNativePatcher.RuntimeWarn("Trading Post fell back to GameScript.SaveGame after " + reason + "; this may show the native save confirmation.");
            }
            catch (Exception ex)
            {
                GTSNativePatcher.RuntimeWarn("Trading Post force-save failed after " + reason + ": " + ex.Message);
            }
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
                GtsListing listing = new GtsListing
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
                    CreatedAtRaw = parts.Length >= 11 ? parts[10] : "",
                    RequestType = parts.Length >= 12 ? SafeNonEmpty(parts[11], "MON") : "MON",
                    RequestSats = parts.Length >= 13 ? ParseInt(parts[12], 0) : 0,
                    ExpiresAtRaw = parts.Length >= 14 ? parts[13] : "",
                    TimeLeftSeconds = parts.Length >= 15 ? ParseInt(parts[14], 0) : 0,
                    OfferedType = parts.Length >= 16 ? SafeNonEmpty(parts[15], "MON") : "MON",
                    OfferedSats = parts.Length >= 17 ? ParseInt(parts[16], 0) : 0
                };
                if (listing.RequestType.Equals("SATS", StringComparison.OrdinalIgnoreCase))
                    listing.RequestSpecies = "SATS";
                target.Add(listing);
            }
        }

        private static bool IsSatsListing(GtsListing listing)
        {
            return listing != null && string.Equals(listing.RequestType, "SATS", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsOfferedSats(GtsListing listing)
        {
            return listing != null && string.Equals(listing.OfferedType, "SATS", StringComparison.OrdinalIgnoreCase);
        }

        private static string FormatListingOffered(GtsListing listing)
        {
            if (IsOfferedSats(listing))
                return Mathf.Max(0, listing.OfferedSats).ToString() + " SATS";
            return listing != null ? ((listing.Shiny ? "★ " : "") + SafeNonEmpty(listing.DisplayName, listing.OfferedSpecies) + "\nLv. " + listing.Level) : "MoN";
        }

        private static string FormatPriceOffer(GtsListing listing)
        {
            if (listing == null) return "";
            if (IsOfferedSats(listing)) return "Offers " + Mathf.Max(0, listing.OfferedSats) + " SATS";
            if (IsSatsListing(listing)) return Mathf.Max(0, listing.RequestSats) + " SATS";
            return "MoN trade";
        }

        private static string FormatListingRequest(GtsListing listing)
        {
            if (IsSatsListing(listing))
                return Mathf.Max(0, listing.RequestSats).ToString() + " SATS";
            return listing != null ? SafeNonEmpty(listing.RequestSpecies, "MoN") : "MoN";
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

        private static string FormatMailAttachmentLabel(MailItem mail)
        {
            try
            {
                if (mail == null) return "No attachment";
                string t = SafeNonEmpty(mail.AttachmentType, "NONE").ToUpperInvariant();
                if (t == "MON") return "MoN attached";
                if (t == "MON_REWARD") return "Reward MoN attached";
                if (t == "ITEM") return "Item attached";
                if (t == "SATS") return mail.AttachmentSats + " SATS attached";
            }
            catch { }
            return "No attachment";
        }

        private static string GetMailClaimButtonLabel(MailItem mail)
        {
            string t = mail != null ? SafeNonEmpty(mail.AttachmentType, "NONE").ToUpperInvariant() : "NONE";
            if (t == "SATS") return "Claim " + mail.AttachmentSats + " SATS";
            if (t == "ITEM") return "Claim attached Item";
            if (t == "MON_REWARD") return "Claim reward MoN";
            return "Claim attached MoN";
        }

        private static MonScriptableObject ResolveAdminRewardMon(GameScript gs, string monSpec)
        {
            try
            {
                if (gs == null || gs.monScriptableObject == null) return null;
                string raw = (monSpec ?? string.Empty).Trim();
                int id;
                if (int.TryParse(raw, out id))
                {
                    for (int i = 0; i < gs.monScriptableObject.Length; i++)
                    {
                        MonScriptableObject m = gs.monScriptableObject[i];
                        if (m == null) continue;
                        if (m.id == id || i == id) return m;
                    }
                }
                string norm = NormalizeKey(raw);
                for (int i = 0; i < gs.monScriptableObject.Length; i++)
                {
                    MonScriptableObject m = gs.monScriptableObject[i];
                    if (m == null) continue;
                    if (NormalizeKey(m.monName) == norm) return m;
                }
                for (int i = 0; i < gs.monScriptableObject.Length; i++)
                {
                    MonScriptableObject m = gs.monScriptableObject[i];
                    if (m == null) continue;
                    string n = NormalizeKey(m.monName);
                    if (!string.IsNullOrEmpty(norm) && (n.Contains(norm) || norm.Contains(n))) return m;
                }
            }
            catch { }
            return null;
        }

        private static Mon BuildAdminRewardMon(GameScript gs, MonScriptableObject mso, int level, bool shiny)
        {
            level = Mathf.Clamp(level <= 0 ? 1 : level, 1, 100);
            Mon mon = new Mon();
            mon.uniqueID = gs.curUniqueIDCounter;
            try { gs.curUniqueIDCounter++; } catch { }
            mon.monID = mso.id;
            mon.monScriptableObject = mso;
            mon.nickName = string.Empty;
            mon.gender = UnityEngine.Random.Range(0, 2);
            mon.isShiny = shiny;
            mon.curExp = gs.GetTotalExpForLevel(level);
            mon.curLevel = level;
            mon.statGrades = new int[] { 10, 10, 10, 10, 10, 10 };
            mon.moveIDs = new int[] { 0, 0, 0, 0 };
            mon.passiveAbilityIDs = new int[] { 0, 0, 0, 0 };
            mon.vibe = 0;
            mon.worldPosX = 0f;
            mon.worldPosY = 0f;
            mon.heldItem = null;
            mon.objName = string.Empty;
            mon.statBoostLevels = new int[] { 0, 0, 0, 0, 0, 0 };
            mon.runAwayValue = 0;
            mon.bottleId = 108;
            mon.upgradeChoice = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            mon.metLocation = "MMOMailmon Reward";
            try { mon.RefreshStatsWithLevelAndStuff(false); } catch { }
            try { mon.hp = mon.maxhp; } catch { }
            return mon;
        }

        private static int ResolveAdminRewardItemId(GameScript gs, string itemSpec)
        {
            try
            {
                if (gs == null || gs.itemScriptableObject == null) return -1;
                string raw = (itemSpec ?? string.Empty).Trim();
                int id;
                if (int.TryParse(raw, out id))
                {
                    if (gs.GetItemByID(id) != null) return id;
                    for (int i = 0; i < gs.itemScriptableObject.Length; i++)
                    {
                        ItemScriptableObject item = gs.itemScriptableObject[i];
                        if (item == null) continue;
                        if (item.itemId == id || i == id) return item.itemId;
                    }
                }
                string norm = NormalizeKey(raw);
                for (int i = 0; i < gs.itemScriptableObject.Length; i++)
                {
                    ItemScriptableObject item = gs.itemScriptableObject[i];
                    if (item == null) continue;
                    string itemName = "";
                    try { itemName = item.itemName; } catch { }
                    if (NormalizeKey(itemName) == norm || NormalizeKey(item.name) == norm) return item.itemId;
                }
                for (int i = 0; i < gs.itemScriptableObject.Length; i++)
                {
                    ItemScriptableObject item = gs.itemScriptableObject[i];
                    if (item == null) continue;
                    string n = NormalizeKey(item.itemName);
                    if (!string.IsNullOrEmpty(norm) && (n.Contains(norm) || norm.Contains(n))) return item.itemId;
                }
            }
            catch { }
            return -1;
        }

        private static string GetItemDisplayName(GameScript gs, int itemId, string fallback)
        {
            try
            {
                ItemScriptableObject item = gs != null ? gs.GetItemByID(itemId) : null;
                if (item != null && !string.IsNullOrWhiteSpace(item.itemName)) return item.itemName;
            }
            catch { }
            return SafeNonEmpty(fallback, "Item");
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
            if (clearSessionToken)
            {
                _sessionToken = "";
                ClearCachedOfficialSessionToken();
            }
            if (updateStatus) _status = "Disconnected.";
        }

        private void OnApplicationQuit()
        {
            // Official Server auth can persist for up to the server-approved lifetime.
            // Do not revoke the token here; the server enforces 12h expiry and same-IP validation.
            _applicationQuitting = true;
            Disconnect(false, false);
        }

        private void OnDestroy()
        {
            // Scene/unity teardown should not erase the cached token. Manual logout/clear paths still revoke and clear it.
            Disconnect(false, false);
        }

        private static string GetGtsConfigPath()
        {
            try { return Path.Combine(Paths.ConfigPath, ConfigFileName); } catch { return ConfigFileName; }
        }

        private static bool IsCachedOfficialSessionLocallyFresh()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(CachedOfficialSessionExpiresUtc))
                    return false;
                DateTime expires;
                if (!DateTime.TryParse(CachedOfficialSessionExpiresUtc.Trim(), null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out expires))
                    return false;
                return expires.ToUniversalTime() > DateTime.UtcNow.AddMinutes(1);
            }
            catch { return false; }
        }

        private static string DecodeCachedOfficialToken(string rawBase64)
        {
            try
            {
                rawBase64 = (rawBase64 ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(rawBase64)) return "";
                return Encoding.UTF8.GetString(Convert.FromBase64String(rawBase64));
            }
            catch { return ""; }
        }

        private static string EncodeCachedOfficialToken(string token)
        {
            try { return Convert.ToBase64String(Encoding.UTF8.GetBytes(token ?? "")); }
            catch { return ""; }
        }

        private static void LoadCachedOfficialSessionTokenIfUsable()
        {
            try
            {
                if (!string.IsNullOrEmpty(Instance != null ? Instance._sessionToken : ""))
                    return;
                if (!IsCachedOfficialSessionLocallyFresh())
                    return;
                string token = DecodeCachedOfficialToken(CachedOfficialSessionTokenBase64);
                if (string.IsNullOrEmpty(token))
                    return;
                if (Instance != null)
                {
                    Instance._sessionToken = token;
                    if (DebugLogging)
                        GTSNativePatcher.RuntimeLog("Loaded cached Official Server session token from config; server will validate expiry and source IP.");
                }
            }
            catch { }
        }

        private static void SaveCachedOfficialSessionToken(string token, string steamId64, string usernameOrDisplayName, string expiresUtc)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                    return;
                DateTime expires;
                if (string.IsNullOrWhiteSpace(expiresUtc) || !DateTime.TryParse(expiresUtc.Trim(), null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out expires))
                    expires = DateTime.UtcNow.AddHours(12);
                CachedOfficialSessionTokenBase64 = EncodeCachedOfficialToken(token);
                CachedOfficialSessionExpiresUtc = expires.ToUniversalTime().ToString("o");
                CachedOfficialSteamID64 = steamId64 ?? "";
                UpsertSimpleConfigValues(GetGtsConfigPath(), "Official Server Auth", new Dictionary<string, string>
                {
                    { "CachedSessionToken", CachedOfficialSessionTokenBase64 },
                    { "CachedSessionExpiresUtc", CachedOfficialSessionExpiresUtc },
                    { "CachedSteamID64", CachedOfficialSteamID64 },
                    { "CachedAccountUUID", CachedOfficialAccountUUID ?? "" }
                });
                if (DebugLogging)
                    GTSNativePatcher.RuntimeLog("Cached Official Server session token in config until " + CachedOfficialSessionExpiresUtc + ".");
            }
            catch (Exception ex)
            {
                if (DebugLogging) GTSNativePatcher.RuntimeWarn("Could not cache Official Server session token: " + ex.Message);
            }
        }

        private static void ClearCachedOfficialSessionToken()
        {
            try
            {
                CachedOfficialSessionTokenBase64 = "";
                CachedOfficialSessionExpiresUtc = "";
                CachedOfficialSteamID64 = "";
                CachedOfficialAccountUUID = "";
                UpsertSimpleConfigValues(GetGtsConfigPath(), "Official Server Auth", new Dictionary<string, string>
                {
                    { "CachedSessionToken", "" },
                    { "CachedSessionExpiresUtc", "" },
                    { "CachedSteamID64", "" },
                    { "CachedAccountUUID", "" }
                });
            }
            catch { }
        }

        private static void UpsertSimpleConfigValues(string path, string section, Dictionary<string, string> values)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                List<string> lines = File.Exists(path) ? new List<string>(File.ReadAllLines(path)) : new List<string>();
                int sectionStart = -1;
                int sectionEnd = lines.Count;
                for (int i = 0; i < lines.Count; i++)
                {
                    string t = lines[i].Trim();
                    if (t.StartsWith("[") && t.EndsWith("]"))
                    {
                        string name = t.Substring(1, t.Length - 2).Trim();
                        if (sectionStart >= 0)
                        {
                            sectionEnd = i;
                            break;
                        }
                        if (name.Equals(section, StringComparison.OrdinalIgnoreCase))
                            sectionStart = i;
                    }
                }
                if (sectionStart < 0)
                {
                    if (lines.Count > 0 && lines[lines.Count - 1].Trim().Length != 0)
                        lines.Add("");
                    lines.Add("[" + section + "]");
                    sectionStart = lines.Count - 1;
                    sectionEnd = lines.Count;
                }
                foreach (KeyValuePair<string, string> kv in values)
                {
                    bool replaced = false;
                    for (int i = sectionStart + 1; i < sectionEnd && i < lines.Count; i++)
                    {
                        string t = lines[i].Trim();
                        if (t.StartsWith("#") || t.StartsWith(";") || !t.Contains("="))
                            continue;
                        string key = t.Substring(0, t.IndexOf('=')).Trim();
                        if (key.Equals(kv.Key, StringComparison.OrdinalIgnoreCase))
                        {
                            lines[i] = kv.Key + " = " + (kv.Value ?? "");
                            replaced = true;
                            break;
                        }
                    }
                    if (!replaced)
                    {
                        lines.Insert(sectionEnd, kv.Key + " = " + (kv.Value ?? ""));
                        sectionEnd++;
                    }
                }
                File.WriteAllLines(path, lines.ToArray());
            }
            catch { }
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
            public string RequestType = "MON";
            public int RequestSats;
            public string OfferedSpecies;
            public string OfferedType = "MON";
            public int OfferedSats;
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
            public string ExpiresAtRaw = "";
            public int TimeLeftSeconds;
        }

        private class MailItem
        {
            public int Id;
            public string Sender;
            public string Subject;
            public string CreatedAtRaw;
            public bool IsRead;
            public bool IsSaved;
            public string AttachmentType = "NONE";
            public int AttachmentSats;
            public bool AttachmentClaimed;
            public string MailType;
            public string ExpiresAtRaw;
        }

        private class MailDetail : MailItem
        {
            public string Body;
            public int SourceListingId;
            public bool HasClaimableAttachment
            {
                get
                {
                    return !AttachmentClaimed && (string.Equals(AttachmentType, "MON", StringComparison.OrdinalIgnoreCase) || string.Equals(AttachmentType, "SATS", StringComparison.OrdinalIgnoreCase) || string.Equals(AttachmentType, "ITEM", StringComparison.OrdinalIgnoreCase) || string.Equals(AttachmentType, "MON_REWARD", StringComparison.OrdinalIgnoreCase));
                }
            }
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

                IAsyncResult ar = _tcp.BeginConnect(_host, _port, null, null);
                if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(8)))
                {
                    try { _tcp.Close(); } catch { }
                    _tcp = null;
                    throw new TimeoutException("Timed out connecting to GTS/Steam auth server " + _host + ":" + _port);
                }

                _tcp.EndConnect(ar);
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
