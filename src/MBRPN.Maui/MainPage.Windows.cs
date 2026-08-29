#if WINDOWS
using Windows.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using MBRPN.Maui.Services;

namespace MBRPN.Maui;

/// <summary>
/// Physical keyboard support on Windows. Printable keys arrive via CharacterReceived
/// on the WinUI content root. Enter and Space never reach a plain KeyDown handler —
/// the XAML focus/interaction system consumes them first — so KeyDown is attached
/// with handledEventsToo: true and owns Enter, Space, arrows, and Delete. Backspace
/// and Escape arrive as control characters (\b, 0x1B) and stay on the character path
/// to avoid double dispatch.
/// </summary>
public partial class MainPage
{
    private UIElement? _winContent;
    private KeyEventHandler? _keyDownHandler;

    partial void HookKeyboard()
    {
        if (_winContent is not null) return;
        if (Window?.Handler?.PlatformView is not Microsoft.UI.Xaml.Window winWindow) return;

        _winContent = winWindow.Content;
        if (_winContent is null) return;
        _keyDownHandler = new KeyEventHandler(OnWinKeyDown);
        _winContent.CharacterReceived += OnCharacterReceived;
        _winContent.AddHandler(UIElement.KeyDownEvent, _keyDownHandler, handledEventsToo: true);
    }

    partial void UnhookKeyboard()
    {
        if (_winContent is null) return;
        _winContent.CharacterReceived -= OnCharacterReceived;
        _winContent.RemoveHandler(UIElement.KeyDownEvent, _keyDownHandler);
        _winContent = null;
    }

    private void OnCharacterReceived(UIElement sender, CharacterReceivedRoutedEventArgs e)
    {
        string? name = e.Character switch
        {
            '\r' or ' ' => null, // KeyDown owns these
            '\b' => "Backspace",
            '\x1b' => "Escape",
            var c when char.IsControl(c) => null,
            var c => c.ToString(),
        };

        if (name is not null && KeyboardMapper.Map(name) is { } key)
        {
            e.Handled = true;
            _ = PressAsync(key);
        }
    }

    private void OnWinKeyDown(object sender, KeyRoutedEventArgs e)
    {
        string? name = e.Key switch
        {
            VirtualKey.Enter => "Enter",
            VirtualKey.Space => " ",
            VirtualKey.Delete => "Delete",
            VirtualKey.Down => "ArrowDown",
            VirtualKey.Up => "ArrowUp",
            _ => null,
        };

        if (name is not null && KeyboardMapper.Map(name) is { } key)
        {
            e.Handled = true;
            _ = PressAsync(key);
        }
    }
}
#endif
