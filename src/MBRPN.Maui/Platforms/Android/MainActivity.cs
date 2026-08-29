using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.View;

namespace MBRPN.Maui;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
    ScreenOrientation = ScreenOrientation.SensorLandscape,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        EnterFullscreen();
    }

    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);
        if (hasFocus) EnterFullscreen();
    }

    /// <summary>Immersive landscape: hide status/nav bars, draw into the cutout area.</summary>
    private void EnterFullscreen()
    {
        if (Window is not { } window) return;

        WindowCompat.SetDecorFitsSystemWindows(window, false);
        if (WindowCompat.GetInsetsController(window, window.DecorView) is { } controller)
        {
            controller.Hide(WindowInsetsCompat.Type.SystemBars());
            controller.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
        }

        if (OperatingSystem.IsAndroidVersionAtLeast(28) && window.Attributes is { } attrs)
        {
            attrs.LayoutInDisplayCutoutMode = Android.Views.LayoutInDisplayCutoutMode.ShortEdges;
            window.Attributes = attrs;
        }
    }
}
