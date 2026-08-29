namespace MBRPN.Maui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var page = new MainPage();
        var window = new Window(page) { Title = "MB-RPN" };

        // Continuous memory must survive backgrounding and close; the 500ms dirty
        // timer alone can lose the last keystrokes on Android process death.
        window.Deactivated += (_, _) => page.FlushSave();
        window.Stopped += (_, _) => page.FlushSave();
        window.Destroying += (_, _) => page.FlushSave();
        return window;
    }
}
