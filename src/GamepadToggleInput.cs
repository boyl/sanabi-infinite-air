using Rewired;

namespace SanabiInfiniteAir;

// 将游戏实际使用的输入系统转换为一次性开关事件。
internal sealed class GamepadToggleInput
{
    private readonly ChordEdge edge = new ChordEdge();
    private int reportedControllerCount = -1;

    internal bool Poll()
    {
        if (!ReInput.isReady)
            return edge.Update(false);

        var joysticks = ReInput.controllers.Joysticks;
        int count = ReInput.controllers.joystickCount;
        bool report = reportedControllerCount != count;
        bool held = false;
        for (int i = 0; i < count; i++)
        {
            var joystick = joysticks[i];
            var gamepad = joystick.GetTemplate<IGamepadTemplate>();
            if (report)
                InfiniteAirPlugin.PluginLog.LogInfo("Gamepad toggle: " + joystick.name +
                    (gamepad == null ? " has no gamepad template; use keyboard toggle." : " LB+RB ready via Rewired."));
            if (gamepad != null)
            {
                // 两个肩键必须来自同一个手柄，避免多设备组合误触。
                held |= gamepad.leftBumper.value && gamepad.rightBumper.value;
            }
        }

        reportedControllerCount = count;
        return edge.Update(held);
    }
}
