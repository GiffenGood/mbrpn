using System.Diagnostics;

namespace MBRPN.Maui.Services;

internal sealed class KeySound : IDisposable
{
#if WINDOWS
    private Windows.Media.Playback.MediaPlayer? _player;
    private Windows.Media.Core.MediaSource? _source;
    private Windows.Storage.Streams.IRandomAccessStream? _stream;
#endif
    private bool _unavailable;

    public void Play()
    {
        if (_unavailable) return;
        try
        {
#if WINDOWS
            if (_player is null)
            {
                _stream = CreateClick().AsRandomAccessStream();
                _source = Windows.Media.Core.MediaSource.CreateFromStream(_stream, "audio/wav");
                _player = new Windows.Media.Playback.MediaPlayer
                {
                    Source = _source,
                    Volume = 0.18,
                    CommandManager = { IsEnabled = false },
                };
            }
            _player.PlaybackSession.Position = TimeSpan.Zero;
            _player.Play();
#elif ANDROID
            using var audio = Android.App.Application.Context.GetSystemService(
                Android.Content.Context.AudioService) as Android.Media.AudioManager;
            audio?.PlaySoundEffect(Android.Media.SoundEffect.KeyClick, 0.15f);
#endif
        }
        catch (Exception ex)
        {
            // Audio is optional; a missing output device must not interrupt input.
            Debug.WriteLine($"Key sound unavailable: {ex.Message}");
            Dispose();
            _unavailable = true;
        }
    }

#if WINDOWS
    private static MemoryStream CreateClick()
    {
        const int sampleRate = 24000;
        const int samples = 720;
        var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true))
        {
            writer.Write("RIFF"u8);
            writer.Write(36 + samples * sizeof(short));
            writer.Write("WAVEfmt "u8);
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(sampleRate);
            writer.Write(sampleRate * sizeof(short));
            writer.Write((short)sizeof(short));
            writer.Write((short)16);
            writer.Write("data"u8);
            writer.Write(samples * sizeof(short));
            for (int i = 0; i < samples; i++)
            {
                double time = (double)i / sampleRate;
                double envelope = Math.Sin(Math.PI * i / (samples - 1)) * Math.Exp(-time * 160);
                double tone = Math.Sin(2 * Math.PI * 1100 * time)
                    + 0.3 * Math.Sin(2 * Math.PI * 2200 * time);
                writer.Write((short)(short.MaxValue * 0.4 * envelope * tone));
            }
        }
        stream.Position = 0;
        return stream;
    }
#endif

    public void Dispose()
    {
#if WINDOWS
        _player?.Dispose();
        _source?.Dispose();
        _stream?.Dispose();
        _player = null;
        _source = null;
        _stream = null;
#endif
    }
}
