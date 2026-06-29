using System.Collections.Generic;

public static class AIOVisibleConfigKeys
{
    public static string[] MmoSocialUserConfigKeys()
    {
        List<string> keys = new List<string>
        {
            // Connection basics players/admins may reasonably edit.
            "Server.Host",
            "Server.Port",
            "Server.ClusterId",
            "Server.ServerHost",
            "Server.ServerPort",

            // Chat controls/window basics.
            "Input.OpenChatKey",
            "Input.ToggleChatKey",
            "Window.InactiveOpacity",
            "Window.ActiveOpacity",
            "Window.WindowX",
            "Window.WindowY",
            "Window.WindowWidth",
            "Window.WindowHeight",
            "Window.LockWindow",

            // Nameplate display/colors are intentionally user-customizable.
            "Nameplates.ShowNameplates",
            "Nameplates.NameplateFontColorHex",
            "Nameplates.NameplateShadowColorHex",
            "Nameplates.ShowNameplateBackground",
            "Nameplates.NameplateBackgroundColorHex",

            // Popup layout tuning for the native System Message window.
            "System Message Popup.PopupWidth",
            "System Message Popup.PopupHeight",
            "System Message Popup.PopupScale",
            "System Message Popup.TitleOffsetX",
            "System Message Popup.TitleOffsetY",
            "System Message Popup.TitleWidth",
            "System Message Popup.TitleHeight",
            "System Message Popup.TitleFontSize",
            "System Message Popup.BodyOffsetX",
            "System Message Popup.BodyOffsetY",
            "System Message Popup.BodyWidth",
            "System Message Popup.BodyHeight",
            "System Message Popup.BodyFontSize",
            "System Message Popup.BirbOffsetX",
            "System Message Popup.BirbOffsetY",
            "System Message Popup.BirbSize",
            "System Message Popup.ButtonOffsetX",
            "System Message Popup.ButtonOffsetY",
            "System Message Popup.ButtonWidth",
            "System Message Popup.ButtonHeight",

            // Server-issued identity fields must persist even though users normally should not edit them.
            "Identity.LastResolvedSaveSlot",
            "Identity.SocialCharacterId",
            "Identity.SocialSecretToken",
            "Identity.PublicHandle",
            "Identity.PublicSerial",
            "Identity.LastDisplayName",
            "Identity.AccountUUID"
        };

        for (int i = 0; i < 6; i++)
        {
            string section = "Identity.slot" + i;
            keys.Add(section + ".SocialCharacterId");
            keys.Add(section + ".SocialSecretToken");
            keys.Add(section + ".PublicHandle");
            keys.Add(section + ".PublicSerial");
            keys.Add(section + ".SaveFingerprint");
            keys.Add(section + ".LastDisplayName");
        }

        return keys.ToArray();
    }

    public static string[] GlobalBoxUserConfigKeys()
    {
        return new string[]
        {
            "Controls.SelectButtonWindowsNumber",
            "Saving.AutoSaveOnGlobalBoxChanges"
        };
    }
}
