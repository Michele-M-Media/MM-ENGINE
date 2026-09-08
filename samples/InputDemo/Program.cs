using System;
using MMEngine.Core;
using MMEngine.Graphics2D;
using MMEngine.Input;
using MMEngine.Platform.PS5;
using SharpProspero.Application;

namespace MMEngine.Samples.InputDemo;

internal sealed class Game : MmApplication2D
{
    private Font2D? _font;
    protected override bool RequiresTrueType => true;

    protected override void OnEngineLoad()
        => _font = Ps5Assets.LoadTrueType("/app0/assets/DejaVuSans.ttf", 26);

    protected override void OnEngineFrame(Canvas2D canvas, in FrameTime time, FrameContext context)
    {
        canvas.Clear(0xFF0B1019);
        canvas.FillRect(70, 60, 1780, 960, 0xFF151D2A);
        Font2D font = _font!;
        canvas.DrawText(font, "MM ENGINE / DualSense input", 118, 96, 0xFFFFFFFF);
        canvas.DrawText(font, Input.Current.Connected ? "CONNECTED" : "NOT CONNECTED", 1450, 96,
            Input.Current.Connected ? 0xFF5FE39B : 0xFFFF7381);

        DrawStick(canvas, 360, 390, Input.Current.LeftStick.X, Input.Current.LeftStick.Y, "LEFT STICK");
        DrawStick(canvas, 770, 390, Input.Current.RightStick.X, Input.Current.RightStick.Y, "RIGHT STICK");
        DrawTrigger(canvas, 1160, 250, Input.Current.LeftTrigger, "L2");
        DrawTrigger(canvas, 1390, 250, Input.Current.RightTrigger, "R2");

        DrawButton(canvas, 1120, 620, "SQUARE", DualSenseButtons.Square);
        DrawButton(canvas, 1350, 540, "TRIANGLE", DualSenseButtons.Triangle);
        DrawButton(canvas, 1580, 620, "CIRCLE", DualSenseButtons.Circle);
        DrawButton(canvas, 1350, 700, "CROSS", DualSenseButtons.Cross);
        if (Input.Pressed(DualSenseButtons.Cross))
        {
            SetControllerVibration(96, 48);
            SetControllerLightBar(0x35, 0xC8, 0xF4);
        }
        else if (Input.Released(DualSenseButtons.Cross))
        {
            SetControllerVibration(0, 0);
            SetControllerLightBar(0x20, 0x45, 0x80);
        }
        canvas.DrawText(font, "Cross also drives vibration + light bar", 118, 875, 0xFF9EB2C9);
        canvas.DrawText(font, "OPTIONS + TOUCH PAD exits", 118, 928, 0xFF9EB2C9);
        if (Input.Held(DualSenseButtons.Options | DualSenseButtons.TouchPad))
            context.RequestExit();
    }

    private void DrawStick(Canvas2D canvas, int cx, int cy, float x, float y, string label)
    {
        canvas.FillRoundedRect(cx - 125, cy - 125, 250, 250, 125, 0xFF222D40);
        canvas.DrawLine(cx - 95, cy, cx + 95, cy, 0xFF4A5B74);
        canvas.DrawLine(cx, cy - 95, cx, cy + 95, 0xFF4A5B74);
        int knobX = cx + (int)(x * 90);
        int knobY = cy + (int)(y * 90);
        canvas.FillRoundedRect(knobX - 22, knobY - 22, 44, 44, 22, 0xFF4CCAF4);
        canvas.DrawText(_font!, label, cx - 74, cy + 154, 0xFFFFFFFF);
    }

    private void DrawTrigger(Canvas2D canvas, int x, int y, float value, string name)
    {
        canvas.FillRect(x, y, 100, 260, 0xFF222D40);
        int fill = (int)(value * 260);
        canvas.FillRect(x, y + 260 - fill, 100, fill, 0xFF6E8CF7);
        canvas.DrawText(_font!, name, x + 32, y + 286, 0xFFFFFFFF);
    }

    private void DrawButton(Canvas2D canvas, int x, int y, string name, DualSenseButtons button)
    {
        bool held = Input.Held(button);
        bool pressed = Input.Pressed(button);
        bool released = Input.Released(button);
        uint color = pressed ? 0xFFFFFFFF : held ? 0xFF51D795 : released ? 0xFFFFBB4D : 0xFF29364B;
        canvas.FillRoundedRect(x, y, 180, 64, 14, color);
        canvas.DrawText(_font!, name, x + 18, y + 17, held || pressed ? 0xFF0C1420 : 0xFFFFFFFF);
    }

    protected override void OnEngineUnload()
    {
        SetControllerVibration(0, 0);
        _font?.Dispose();
    }
}

internal static class Program
{
    private static void Main()
    {
        using (var game = new Game())
            game.Run();
        ProcessExit.Exit();
    }
}
