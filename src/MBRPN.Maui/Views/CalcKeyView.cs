using MBRPN.Engine.Keys;
using MBRPN.Maui.Theme;

namespace MBRPN.Maui.Views;

/// <summary>
/// One key: teal f-legend on the faceplate above; flat key cap with the primary label
/// and a violet g-legend caption inside. Port of the web app's CalcKey.razor. Not a
/// Button on purpose — buttons can't host the three styled legends and would steal
/// Space/Enter keyboard focus on Windows.
/// </summary>
public sealed class CalcKeyView : ContentView
{
    public KeyDef Def { get; }
    public event Action<KeyId>? Pressed;

    private readonly Border _cap;
    private readonly Brush _normalBrush;
    private readonly Brush _pressedBrush;

    public CalcKeyView(KeyDef def)
    {
        Def = def;
        bool isEnter = def.Id == KeyId.Enter;

        (Color top, Color bottom, Color border, Color text) = def.Id switch
        {
            KeyId.F => (Palette.KeyFTop, Palette.KeyFBottom, Palette.KeyFBorder, Palette.KeyFText),
            KeyId.G => (Palette.KeyGTop, Palette.KeyGBottom, Palette.KeyGBorder, Palette.KeyGText),
            KeyId.Enter => (Palette.KeyHi, Palette.KeyLo, Palette.TealDim, Palette.Teal),
            _ => (Palette.KeyHi, Palette.KeyLo, Palette.KeyBorder, Palette.KeyText),
        };

        _normalBrush = VerticalGradient(top, bottom);
        _pressedBrush = VerticalGradient(Palette.KeyPressedHi, Palette.KeyPressedLo);

        var fLabel = new Label
        {
            Text = def.FLabel ?? " ",
            TextColor = Palette.Teal,
            FontSize = 10.5,
            FontAttributes = FontAttributes.Bold,
            CharacterSpacing = 0.6,
            LineBreakMode = LineBreakMode.NoWrap,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            HeightRequest = 16,
        };

        var primary = new Label
        {
            Text = isEnter ? "E\nN\nT\nE\nR" : def.PrimaryLabel,
            TextColor = text,
            FontSize = isEnter ? 12.5 : 15,
            FontAttributes = def.Id is KeyId.F or KeyId.G
                ? FontAttributes.Bold | FontAttributes.Italic
                : FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.NoWrap,
        };

        var gLabel = new Label
        {
            Text = def.GLabel ?? " ",
            TextColor = Palette.Violet,
            FontSize = 10,
            FontAttributes = FontAttributes.Bold,
            LineBreakMode = LineBreakMode.NoWrap,
            HorizontalTextAlignment = TextAlignment.Center,
        };

        _cap = new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 9 },
            Stroke = new SolidColorBrush(border),
            StrokeThickness = 1,
            Background = _normalBrush,
            Padding = new Thickness(2, 4),
            Content = new VerticalStackLayout
            {
                Spacing = 2,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Children = { primary, gLabel },
            },
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => OnTapped();
        _cap.GestureRecognizers.Add(tap);

        Content = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(16),
                new RowDefinition(GridLength.Star),
            },
            Children = { fLabel, _cap },
        };
        Grid.SetRow(_cap, 1);
    }

    private async void OnTapped()
    {
        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
        catch (FeatureNotSupportedException)
        {
            // desktop: no haptics hardware
        }

        Pressed?.Invoke(Def.Id);

        // Brief pressed pulse, mirroring the web's .key:active style.
        _cap.Background = _pressedBrush;
        _cap.TranslationY = 2;
        await Task.Delay(90);
        _cap.Background = _normalBrush;
        _cap.TranslationY = 0;
    }

    private static LinearGradientBrush VerticalGradient(Color top, Color bottom) => new(
        [new GradientStop(top, 0f), new GradientStop(bottom, 1f)],
        new Point(0, 0), new Point(0, 1));
}
