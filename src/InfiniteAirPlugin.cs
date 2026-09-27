using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace SanabiInfiniteAir;

[BepInPlugin("com.codex.sanabi.infiniteair", "SANABI Infinite Air", "0.2.8")]
public sealed class InfiniteAirPlugin : BasePlugin
{
    internal static ConfigEntry<KeyCode> ToggleKey = null!;
    internal static ConfigEntry<float> VerticalSpeed = null!;
    internal static ConfigEntry<bool> GamepadToggle = null!;
    internal static ConfigEntry<bool> ShowHint = null!;
    internal static ManualLogSource PluginLog = null!;

    public override void Load()
    {
        PluginLog = Log;
        ToggleKey = Config.Bind("Controls", "ToggleKey", KeyCode.F8,
            "Enable or disable infinite air. Disabling in mid-air restores falling.");
        VerticalSpeed = Config.Bind("Movement", "VerticalSpeed", 9f,
            "Up/down speed while infinite air is enabled.");
        GamepadToggle = Config.Bind("Controls", "GamepadToggle", true,
            "Press LB + RB together to toggle infinite air (Rewired gamepad input).");
        ShowHint = Config.Bind("Display", "ShowHint", true,
            "Show a brief hint near the player when infinite air is toggled.");

        new Harmony("com.codex.sanabi.infiniteair").PatchAll(typeof(InfiniteAirPlugin).Assembly);
        AddComponent<FlightDriver>();
        Log.LogInfo("Infinite Air loaded for Player and DlcPlayer. Toggle: " + ToggleKey.Value + " / LB+RB (Rewired)");
    }
}

public sealed class FlightDriver : MonoBehaviour
{
    private readonly GamepadToggleInput gamepadInput = new GamepadToggleInput();
    private float hintUntil;

    public FlightDriver(IntPtr pointer) : base(pointer) { }

    private void Update()
    {
        PlayerBase? player = PlayerBase.Instance;
        if (FlightState.Observe(player))
            hintUntil = 0f;
        bool gamepadPressed = gamepadInput.Poll();
        if (player == null || player.IsDead || player.IsIgnoreAllInput)
            return;

        if (Input.GetKeyDown(InfiniteAirPlugin.ToggleKey.Value) ||
            (InfiniteAirPlugin.GamepadToggle.Value && gamepadPressed))
        {
            FlightState.Toggle();
            hintUntil = Time.unscaledTime + 0.9f;
            InfiniteAirPlugin.PluginLog.LogInfo("Infinite Air " + (FlightState.Enabled ? "enabled" : "disabled"));
        }
    }

    private void OnGUI()
    {
        if (!InfiniteAirPlugin.ShowHint.Value || Time.unscaledTime >= hintUntil)
            return;

        PlayerBase? player = PlayerBase.Instance;
        if (player == null || player.IsDead)
            return;

        Camera? camera = Camera.main;
        if (camera == null)
            return;
        Vector3 screenPoint = camera.WorldToScreenPoint(player.transform.position);
        if (screenPoint.z <= 0f)
            return;

        var style = new GUIStyle(GUI.skin.label);
        style.fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height / 72f), 14, 20);
        style.normal.textColor = Color.white;
        style.wordWrap = false;

        string hint = FlightState.Enabled ? "AIR: ON" : "AIR: OFF";
        Vector2 textSize = style.CalcSize(new GUIContent(hint));
        float width = Mathf.Min(Screen.width - 16f, textSize.x + 16f);
        float height = textSize.y + 8f;
        float sideGap = Mathf.Clamp(Screen.height / 24f, 36f, 72f);
        float rightX = screenPoint.x + sideGap;
        float leftX = screenPoint.x - sideGap - width;
        float x = rightX + width + 8f <= Screen.width ? rightX : leftX;
        x = Mathf.Clamp(Mathf.Round(x), 8f, Screen.width - width - 8f);
        float playerY = Screen.height - screenPoint.y;
        float y = Mathf.Clamp(Mathf.Round(playerY - height / 2f), 8f, Screen.height - height - 8f);
        var panel = new Rect(x, y, width, height);

        GUI.Box(panel, new GUIContent(""), GUI.skin.box);
        GUI.Label(new Rect(x + 8f, y + 4f, width - 16f, textSize.y), hint, style);
    }
}

internal static class FlightState
{
    private static IntPtr currentPlayer;
    internal static bool Enabled { get; private set; }

    internal static bool Observe(PlayerBase? player)
    {
        IntPtr pointer = player == null ? IntPtr.Zero : player.Pointer;
        if (currentPlayer == pointer)
            return false;

        currentPlayer = pointer;
        Enabled = false;
        return true;
    }

    internal static void Toggle() => Enabled = !Enabled;

    internal static bool ActiveFor(PlayerBase player) =>
        Enabled && currentPlayer == player.Pointer && !player.IsDead && !player.IsIgnoreAllInput && !player.OnGround;

    internal static float VerticalInput(PlayerBase player)
    {
        bool up = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        bool down = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        float keyboard = (up ? 1f : 0f) - (down ? 1f : 0f);
        if (keyboard != 0f)
            return keyboard;

        // The game's own movement direction already contains its Rewired gamepad mapping.
        return Mathf.Clamp(player._directionalInput.y, -1, 1);
    }

    internal static void BeforeMove(PlayerBase player)
    {
        if (!ActiveFor(player))
            return;

        Vector2 velocity = player._velocity;
        velocity.y = VerticalInput(player) * InfiniteAirPlugin.VerticalSpeed.Value;
        player._velocity = velocity;
    }

    internal static void AfterMove(PlayerBase player)
    {
        if (!ActiveFor(player))
            return;

        Vector2 velocity = player._velocity;
        velocity.y = VerticalInput(player) * InfiniteAirPlugin.VerticalSpeed.Value;
        player._velocity = velocity;
    }
}

[HarmonyPatch(typeof(Player), "Update")]
internal static class MainPlayerUpdatePatch
{
    private static void Prefix(Player __instance) => FlightState.BeforeMove(__instance);
    private static void Postfix(Player __instance) => FlightState.AfterMove(__instance);
}

[HarmonyPatch(typeof(DlcPlayer), "Update")]
internal static class DlcPlayerUpdatePatch
{
    private static void Prefix(DlcPlayer __instance) => FlightState.BeforeMove(__instance);
    private static void Postfix(DlcPlayer __instance) => FlightState.AfterMove(__instance);
}

[HarmonyPatch(typeof(PlayerBase), "get_Gravity")]
internal static class GravityPatch
{
    private static void Postfix(PlayerBase __instance, ref float __result)
    {
        if (FlightState.ActiveFor(__instance))
            __result = 0f;
    }
}
