using MMEngine.Core;
using MMEngine.Graphics2D;
using MMEngine.Input;
using MMEngine.Platform.PS5;
using MMEngine.UI;
using SharpProspero.Application;

namespace MMEngine.Samples.Hello2D;

internal sealed class Game : MmApplication2D
{
    private Font2D? _font;
    private Texture2D? _logo;
    private readonly UiCanvas _ui = new();
    private ProgressBar? _trigger;
    private Button? _button;

    protected override bool RequiresTrueType => true;
    protected override bool RequiresPng => true;

    protected override void OnEngineLoad()
    {
        _font = Ps5Assets.LoadTrueType("/app0/assets/DejaVuSans.ttf", 30);
        _logo = Ps5Assets.LoadPng("/app0/assets/mm-mark.png");
        Panel panel = _ui.Add(new Panel { Bounds = new RectI(88, 88, 1744, 904) });
        panel.Add(new Card
        {
            Bounds = new RectI(112, 120, 320, 384),
            Artwork = _logo,
            Font = _font,
            Title = "HELLO 2D",
            Subtitle = "native asset card",
            Selected = true,
        });
        panel.Add(new Label { Bounds = new RectI(480, 138, 900, 48), Font = _font, Text = "MM ENGINE v0.1-alpha", ColorArgb = 0xFFFFFFFF });
        panel.Add(new Label { Bounds = new RectI(480, 194, 1100, 42), Font = _font, Text = "Native 2D - alpha, scaling, TrueType AA", ColorArgb = 0xFFAFC4DC });
        _button = panel.Add(new Button { Bounds = new RectI(480, 276, 330, 64), Font = _font, Text = "Hold Cross", Selected = true });
        _trigger = panel.Add(new ProgressBar { Bounds = new RectI(480, 378, 640, 20) });
    }

    protected override void OnEngineFrame(Canvas2D canvas, in FrameTime time, FrameContext context)
    {
        canvas.Clear(0xFF0B111C);
        canvas.FillRect(64, 64, canvas.Width - 128, canvas.Height - 128, 0xFF151F2F);
        canvas.FillRect(64, 64, 12, canvas.Height - 128, 0xFF42C7F5);
        if (_button is not null)
            _button.Pressed = Input.Held(DualSenseButtons.Cross);
        if (_trigger is not null)
            _trigger.Value = Input.Current.RightTrigger;
        _ui.Draw(canvas);
        if (_font is not null)
        {
            int percent = (int)(Input.Current.RightTrigger * 100);
            canvas.DrawText(_font, "R2 analog: " + percent.ToString() + "%", 480, 420, 0xFFFFFFFF);
            canvas.DrawText(_font, "OPTIONS + TOUCH PAD exits", 480, 478, 0xFF8EA3BC);
        }
        if (Input.Held(DualSenseButtons.Options | DualSenseButtons.TouchPad))
            context.RequestExit();
    }

    protected override void OnEngineUnload()
    {
        _logo?.Dispose();
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
