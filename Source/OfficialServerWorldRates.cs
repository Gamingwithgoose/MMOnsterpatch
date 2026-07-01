using HarmonyLib;
using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Goose.Monsterpatch.OfficialServer;

public static class OfficialServerWorldRatesRuntime
{
    private static readonly object Sync = new object();
    private static bool _loadedFromServer;
    private static float _expRate = 1f;
    private static float _satsRate = 1f;
    // v0.9.8: server shiny setting is denominator-style. 1000 = 1/1000, 1 = guaranteed shiny.
    private static float _shinyRate = 1000f;
    private static float _catchRate = 1f;
    private static float _itemDropRate = 1f;
    private static float _randomEncounterRate = 1f;
    private static float _visibleSpawnRate = 2f;
    private static float _rewardSpawnRate = 1f;
    private static float _rpGainRate = 1f;
    private static float _rpLossRate = 1f;
    private static float _seasonRewardRate = 1f;
    private static float _lastLogTime;
    private static int _starterFlowSuppressDepth;

    public static bool IsOnlineRatesActive
    {
        get
        {
            try
            {
                if (_starterFlowSuppressDepth > 0)
                    return false;
                return _loadedFromServer && OfficialServerSaveSelectNativeRuntime.IsOfficialOnlineModeActive();
            }
            catch { return false; }
        }
    }

    public static void BeginStarterSequenceClientOwned(string reason)
    {
        try
        {
            _starterFlowSuppressDepth++;
            ThrottledLog("Starter sequence is client-owned; server world rates disabled for " + (reason ?? "starter flow") + ".");
        }
        catch { _starterFlowSuppressDepth = 1; }
    }

    public static void EndStarterSequenceClientOwned(string reason)
    {
        try
        {
            _starterFlowSuppressDepth = Math.Max(0, _starterFlowSuppressDepth - 1);
            if (_starterFlowSuppressDepth == 0)
                ThrottledLog("Starter sequence client-owned guard released from " + (reason ?? "starter flow") + ".");
        }
        catch { _starterFlowSuppressDepth = 0; }
    }

    public static float ExpRate { get { lock (Sync) return _expRate; } }
    public static float SatsRate { get { lock (Sync) return _satsRate; } }
    public static float ShinyRate { get { lock (Sync) return _shinyRate; } }
    public static int ShinyOddsDenominator { get { lock (Sync) return Mathf.Max(1, Mathf.RoundToInt(_shinyRate)); } }
    public static float CatchRate { get { lock (Sync) return _catchRate; } }
    public static float ItemDropRate { get { lock (Sync) return _itemDropRate; } }
    public static float RandomEncounterRate { get { lock (Sync) return _randomEncounterRate; } }
    public static float RewardSpawnRate { get { lock (Sync) return _rewardSpawnRate; } }

    public static void ApplyServerLine(string line)
    {
        try
        {
            if (string.IsNullOrEmpty(line) || !line.StartsWith("OFFICIAL_WORLD_RATES|", StringComparison.OrdinalIgnoreCase))
                return;

            string[] p = line.Split('|');
            if (p.Length < 12)
                throw new Exception("world-rate response had too few fields");

            lock (Sync)
            {
                _expRate = ClampRate(ParseRate(p[1], 1f), 0f, 100f);
                _satsRate = ClampRate(ParseRate(p[2], 1f), 0f, 100f);
                _shinyRate = ClampRate(ParseRate(p[3], 1000f), 0f, 1000000f);
                _catchRate = ClampRate(ParseRate(p[4], 1f), 0f, 100f);
                _itemDropRate = ClampRate(ParseRate(p[5], 1f), 0f, 100f);
                _randomEncounterRate = ClampRate(ParseRate(p[6], 1f), 0f, 100f);
                _visibleSpawnRate = ClampRate(ParseRate(p[7], 2f), 0f, 100f);
                _rewardSpawnRate = ClampRate(ParseRate(p[8], 1f), 0f, 100f);
                _rpGainRate = ClampRate(ParseRate(p[9], 1f), 0f, 100f);
                _rpLossRate = ClampRate(ParseRate(p[10], 1f), 0f, 100f);
                _seasonRewardRate = ClampRate(ParseRate(p[11], 1f), 0f, 100f);
                _loadedFromServer = true;
            }

            Log("Server world rates applied: EXP=" + ExpRate.ToString("0.###", CultureInfo.InvariantCulture) +
                ", SATS=" + SatsRate.ToString("0.###", CultureInfo.InvariantCulture) +
                ", ShinyOddsDenom=" + ShinyRate.ToString("0.###", CultureInfo.InvariantCulture) +
                ", Catch=" + CatchRate.ToString("0.###", CultureInfo.InvariantCulture) +
                ", ItemDrop=" + ItemDropRate.ToString("0.###", CultureInfo.InvariantCulture) +
                ", RandomEncounter=" + RandomEncounterRate.ToString("0.###", CultureInfo.InvariantCulture) + ".");
        }
        catch (Exception ex)
        {
            Log("ApplyServerLine failed: " + ex.Message);
            ResetToOfflineDefaults("bad server world-rates line");
        }
    }

    public static void ResetToOfflineDefaults(string reason)
    {
        lock (Sync)
        {
            _loadedFromServer = false;
            _expRate = 1f;
            _satsRate = 1f;
            _shinyRate = 1000f;
            _catchRate = 1f;
            _itemDropRate = 1f;
            _randomEncounterRate = 1f;
            _visibleSpawnRate = 2f;
            _rewardSpawnRate = 1f;
            _rpGainRate = 1f;
            _rpLossRate = 1f;
            _seasonRewardRate = 1f;
        }
        Log("Server world rates reset to offline/default behavior from " + (reason ?? "unknown") + ".");
    }

    public static int ApplyExpRate(int baseExp)
    {
        if (!IsOnlineRatesActive || baseExp <= 0)
            return baseExp;
        float rate = ExpRate;
        if (rate <= 0f)
            return 0;
        return Mathf.Max(1, Mathf.RoundToInt(baseExp * rate));
    }

    public static int ApplySatsRate(int amount)
    {
        if (!IsOnlineRatesActive || amount <= 0)
            return amount;
        float rate = SatsRate;
        if (rate <= 0f)
            return 0;
        return Mathf.Max(0, Mathf.RoundToInt(amount * rate));
    }

    public static int ApplyChanceRate(int basePercent, float rate)
    {
        if (rate <= 0f)
            return 0;
        return Mathf.Clamp(Mathf.RoundToInt(basePercent * rate), 0, 100);
    }

    public static bool ShouldAllowRandomEncounterStep()
    {
        if (!IsOnlineRatesActive)
            return true;
        float rate = RandomEncounterRate;
        if (rate <= 0f)
        {
            ThrottledLog("Random encounter blocked by server Random Encounter Rate = 0.");
            return false;
        }
        if (rate < 1f)
        {
            float roll = UnityEngine.Random.value;
            if (roll > rate)
                return false;
        }
        return true;
    }

    public static void ApplyRandomEncounterTriggerRate()
    {
        if (!IsOnlineRatesActive)
            return;
        float rate = RandomEncounterRate;
        if (rate <= 0f)
        {
            GameScript.encounterCounter = 0;
            GameScript.encounterTrigger = 999999;
            return;
        }
        if (Math.Abs(rate - 1f) < 0.001f)
            return;
        try
        {
            int trigger = Mathf.Max(1, GameScript.encounterTrigger);
            int adjusted = Mathf.Clamp(Mathf.RoundToInt(trigger / rate), 1, 999999);
            GameScript.encounterTrigger = adjusted;
        }
        catch { }
    }

    public static void ApplyShinyRate(Mon mon)
    {
        if (!IsOnlineRatesActive || mon == null)
            return;
        float rate = ShinyRate;
        if (rate <= 0f)
        {
            mon.isShiny = false;
            return;
        }

        float desiredChance = rate <= 1f ? 1f : Mathf.Clamp(1f / rate, 0f, 1f);
        mon.isShiny = UnityEngine.Random.value < desiredChance;
    }

    public static IEnumerator RatedCatchCoroutine(GameScript gs)
    {
        if (gs == null)
            yield break;

        GameObject curInteractingMonObj = GetPrivateField<GameObject>(gs, "curInteractingMonObj");
        if (curInteractingMonObj == null || gs.curInteractingMon == null)
            yield break;

        try { gs.subMenuWildMon.SetActive(false); } catch { }
        try { gs.panelMonNamePreview.SetActive(false); } catch { }
        try { gs.audioSystem.PlaySFX("cast"); } catch { }
        try { gs.spriteAnimator.PlayCastLong(); } catch { }
        try
        {
            if (gs.inventoryQ != null && gs.curBottleId >= 0 && gs.curBottleId < gs.inventoryQ.Length)
                gs.inventoryQ[gs.curBottleId]--;
        }
        catch { }
        try { Camera.main.gameObject.GetComponent<CameraFollow>().SetTarget(curInteractingMonObj); } catch { }
        yield return new WaitForSeconds(0.1f);

        GameObject g = null;
        try
        {
            g = UnityEngine.Object.Instantiate<GameObject>(gs.catchDiamondPrefab, curInteractingMonObj.transform.position, Quaternion.identity);
            Animator a = g != null ? g.GetComponent<Animator>() : null;
            if (a != null) a.Play("catchDiamondLock");
        }
        catch { }

        try { curInteractingMonObj.SetActive(false); } catch { }

        int numOfShakes = UnityEngine.Random.Range(1, 4);
        int baseChance = BaseCatchChance(gs.curInteractingMon);
        int chance = ApplyChanceRate(baseChance, CatchRate);
        bool success = UnityEngine.Random.Range(0, 100) < chance;
        if (success)
            numOfShakes = 3;

        yield return new WaitForSeconds(0.3f);
        for (int i = 0; i < numOfShakes; i++)
        {
            yield return new WaitForSeconds(0.6f);
            try { gs.audioSystem.PlaySFX("uiClick"); } catch { }
            try { if (g != null) g.GetComponent<Animator>().Play("catchDiamondShake"); } catch { }
        }
        yield return new WaitForSeconds(0.6f);

        if (success)
        {
            try { gs.audioSystem.PlaySFX("turn"); } catch { }
            try { UnityEngine.Object.Destroy(curInteractingMonObj); } catch { }
            try
            {
                if (g != null)
                {
                    CatchDiamondScript cds = g.GetComponent<CatchDiamondScript>();
                    if (cds != null) cds.Init(gs.playerObj.transform);
                    Animator a = g.GetComponent<Animator>();
                    if (a != null) a.Play("catchDiamondEnd");
                }
            }
            catch { }
            yield return new WaitForSeconds(1f);
            try { Camera.main.gameObject.GetComponent<CameraFollow>().SetTarget(gs.playerObj); } catch { }
            try { gs.spriteAnimator.PlayIdle(); } catch { }
            try { gs.curInteractingMon.RefreshStatsWithLevelAndStuff(); } catch { }
            try { gs.curInteractingMon.bottleId = gs.curBottleId; } catch { }
            try { gs.ShowDialogue("caughtMonInBottle"); } catch { }
        }
        else
        {
            try { gs.audioSystem.PlaySFX("uiCancel"); } catch { }
            try { gs.WhitePopExplode(curInteractingMonObj.transform.position); } catch { }
            try { if (g != null) UnityEngine.Object.Destroy(g); } catch { }
            try { curInteractingMonObj.SetActive(true); } catch { }
            try
            {
                if (curInteractingMonObj.transform.childCount > 0)
                {
                    Animation anim = curInteractingMonObj.transform.GetChild(0).GetComponent<Animation>();
                    if (anim != null) anim.Play("monFailToCatch");
                }
            }
            catch { }
            try { gs.ShowDialogue("failToCatchMon"); } catch { }
        }

        try { gs.AdvanceTime(9); } catch { }
    }

    private static int BaseCatchChance(Mon mon)
    {
        try
        {
            switch (mon.monScriptableObject.catchDifficulty)
            {
                case 0: return 90;
                case 1: return 60;
                case 2: return 30;
                case 3: return 10;
            }
        }
        catch { }
        return 0;
    }

    private static T GetPrivateField<T>(object obj, string fieldName) where T : class
    {
        try
        {
            FieldInfo f = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return f != null ? f.GetValue(obj) as T : null;
        }
        catch { return null; }
    }

    private static float ParseRate(string s, float fallback)
    {
        if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
            return v;
        if (float.TryParse(s, out v))
            return v;
        return fallback;
    }

    private static float ClampRate(float v, float min, float max)
    {
        if (float.IsNaN(v) || float.IsInfinity(v))
            return min;
        return Mathf.Clamp(v, min, max);
    }

    private static void Log(string message)
    {
        try { Debug.Log("[MMOnsterpatch Official Server Rates] " + message); } catch { }
    }

    private static void ThrottledLog(string message)
    {
        try
        {
            if (Time.realtimeSinceStartup - _lastLogTime < 1.5f)
                return;
            _lastLogTime = Time.realtimeSinceStartup;
            Log(message);
        }
        catch { }
    }
}


[HarmonyPatch(typeof(GameScript), "HatchStarterMon")]
public static class OfficialServerWorldRates_GameScript_HatchStarterMon_ClientOwned_Patch
{
    public static void Prefix()
    {
        OfficialServerWorldRatesRuntime.BeginStarterSequenceClientOwned("HatchStarterMon");
    }

    public static Exception Finalizer(Exception __exception)
    {
        OfficialServerWorldRatesRuntime.EndStarterSequenceClientOwned("HatchStarterMon");
        return __exception;
    }
}

[HarmonyPatch(typeof(GameScript), "GetExpGain")]
public static class OfficialServerWorldRates_GameScript_GetExpGain_Patch
{
    public static void Postfix(ref int __result)
    {
        __result = OfficialServerWorldRatesRuntime.ApplyExpRate(__result);
    }
}

[HarmonyPatch(typeof(GameScript), "AddSATS")]
public static class OfficialServerWorldRates_GameScript_AddSATS_Patch
{
    public static void Prefix(ref int n)
    {
        n = OfficialServerWorldRatesRuntime.ApplySatsRate(n);
    }
}

[HarmonyPatch(typeof(GameScript), "SetUniqueIDAndInitializeMon")]
public static class OfficialServerWorldRates_GameScript_SetUniqueIDAndInitializeMon_Patch
{
    public static void Postfix(Mon m)
    {
        OfficialServerWorldRatesRuntime.ApplyShinyRate(m);
    }
}

[HarmonyPatch(typeof(GameScript), "WildEncounterCheck")]
public static class OfficialServerWorldRates_GameScript_WildEncounterCheck_Patch
{
    public static bool Prefix()
    {
        return OfficialServerWorldRatesRuntime.ShouldAllowRandomEncounterStep();
    }
}

[HarmonyPatch(typeof(GameScript), "ResetEncounterTrigger")]
public static class OfficialServerWorldRates_GameScript_ResetEncounterTrigger_Patch
{
    public static void Postfix()
    {
        OfficialServerWorldRatesRuntime.ApplyRandomEncounterTriggerRate();
    }
}

[HarmonyPatch]
public static class OfficialServerWorldRates_BattleSystem_TryRollItemDrop_Patch
{
    public static MethodBase TargetMethod()
    {
        try
        {
            return typeof(BattleSystem).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(m => m.Name == "TryRollItemDrop" && m.ReturnType == typeof(bool));
        }
        catch { return null; }
    }

    public static void Prefix([HarmonyArgument("chancePercent")] ref int chancePercent)
    {
        if (!OfficialServerWorldRatesRuntime.IsOnlineRatesActive)
            return;
        chancePercent = OfficialServerWorldRatesRuntime.ApplyChanceRate(chancePercent, OfficialServerWorldRatesRuntime.ItemDropRate);
    }
}

[HarmonyPatch]
public static class OfficialServerWorldRates_GameScript_TryRollItemDrop_Patch
{
    public static MethodBase TargetMethod()
    {
        try
        {
            return typeof(GameScript).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(m => m.Name == "TryRollItemDrop" && m.GetParameters().Length == 4);
        }
        catch { return null; }
    }

    public static void Prefix([HarmonyArgument("chancePercent")] ref int chancePercent)
    {
        if (!OfficialServerWorldRatesRuntime.IsOnlineRatesActive)
            return;
        chancePercent = OfficialServerWorldRatesRuntime.ApplyChanceRate(chancePercent, OfficialServerWorldRatesRuntime.ItemDropRate);
    }
}

[HarmonyPatch]
public static class OfficialServerWorldRates_GameScript_TryToCatchWildMon2_Patch
{
    public static MethodBase TargetMethod()
    {
        try
        {
            return typeof(GameScript).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(m => m.Name == "TryToCatchWildMon2" && typeof(IEnumerator).IsAssignableFrom(m.ReturnType));
        }
        catch { return null; }
    }

    public static bool Prefix(GameScript __instance, ref IEnumerator __result)
    {
        if (!OfficialServerWorldRatesRuntime.IsOnlineRatesActive)
            return true;

        float rate = OfficialServerWorldRatesRuntime.CatchRate;
        if (Math.Abs(rate - 1f) < 0.001f)
            return true;

        __result = OfficialServerWorldRatesRuntime.RatedCatchCoroutine(__instance);
        return false;
    }
}
