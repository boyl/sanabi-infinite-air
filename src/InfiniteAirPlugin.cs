using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace SanabiInfiniteAir;

[BepInPlugin("com.codex.sanabi.infiniteair", "SANABI Infinite Air", "0.2.4")]
public sealed class InfiniteAirPlugin : BasePlugin
{
    internal static ConfigEntry<KeyCode> ToggleKey = null!;
    internal static ConfigEntry<float> VerticalSpeed = null!;
    internal static ConfigEntry<bool> GamepadToggle = null!;
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

        new Harmony("com.codex.sanabi.infiniteair").PatchAll(typeof(InfiniteAirPlugin).Assembly);
        AddComponent<FlightDriver>();
        Log.LogInfo("Infinite Air loaded for Player and DlcPlayer. Toggle: " + ToggleKey.Value + " / LB+RB (Rewired)");
    }
}

public sealed class FlightDriver : MonoBehaviour
{
    private readonly GamepadToggleInput gamepadInput = new GamepadToggleInput();

    public FlightDriver(IntPtr pointer) : base(pointer) { }

    private void Update()
    {
        PlayerBase? player = PlayerBase.Instance;
        FlightState.Observe(player);
        bool gamepadPressed = gamepadInput.Poll();
        if (player == null || player.IsDead || player.IsIgnoreAllInput)
            return;

        if (Input.GetKeyDown(InfiniteAirPlugin.ToggleKey.Value) ||
            (InfiniteAirPlugin.GamepadToggle.Value && gamepadPressed))
        {
            FlightState.Toggle();
            InfiniteAirPlugin.PluginLog.LogInfo("Infinite Air " + (FlightState.Enabled ? "enabled" : "disabled"));
        }
    }

    private void OnGUI()
    {
        PlayerBase? player = PlayerBase.Instance;
        if (player == null || player.IsDead)
            return;

        string toggle = InfiniteAirPlugin.ToggleKey.Value +
            (InfiniteAirPlugin.GamepadToggle.Value ? " / LB+RB" : "");
        var style = new GUIStyle(GUI.skin.label);
        style.fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height / 72f), 14, 20);
        style.normal.textColor = Color.white;
        style.wordWrap = false;

        string firstLine = FlightState.Enabled
            ? "AIR ON  W/S, Arrows, Stick/D-pad: height"
            : "AIR OFF  " + toggle + ": on";
        string secondLine = FlightState.Enabled ? toggle + ": land" : "";
        float availableWidth = Screen.width - 32f;
        if (FlightState.Enabled && style.CalcSize(new GUIContent(firstLine)).x > availableWidth)
            firstLine = "AIR ON  Up/Down: height";

        Vector2 firstSize = style.CalcSize(new GUIContent(firstLine));
        Vector2 secondSize = FlightState.Enabled
            ? style.CalcSize(new GUIContent(secondLine)) : Vector2.zero;
        float width = Mathf.Min(Screen.width - 16f, Mathf.Max(firstSize.x, secondSize.x) + 16f);
        float height = firstSize.y + secondSize.y + 12f;
        var panel = new Rect(8f, 8f, width, height);

        GUI.Box(panel, new GUIContent(""), GUI.skin.box);
        GUI.Label(new Rect(16f, 12f, width - 16f, firstSize.y), firstLine, style);
        if (FlightState.Enabled)
            GUI.Label(new Rect(16f, 12f + firstSize.y, width - 16f, secondSize.y), secondLine, style);
    }
}

internal static class FlightState
{
    private static IntPtr currentPlayer;
    internal static bool Enabled { get; private set; }

    internal static void Observe(PlayerBase? player)
    {
        IntPtr pointer = player == null ? IntPtr.Zero : player.Pointer;
        if (currentPlayer == pointer)
            return;

        currentPlayer = pointer;
        Enabled = false;
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
