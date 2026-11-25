using System.Speech.Synthesis;

namespace SpeechApp.Services;

public sealed class TextToSpeechService : IDisposable
{
    private readonly SpeechSynthesizer _synth = new();
    private bool _paused;

    public event Action<string>? StateChanged;

    public IReadOnlyList<string> VoiceNames =>
        _synth.GetInstalledVoices()
            .Where(v => v.Enabled)
            .Select(v => v.VoiceInfo.Name)
            .ToList();

    public TextToSpeechService()
    {
        _synth.SpeakCompleted += (_, _) =>
        {
            _paused = false;
            StateChanged?.Invoke("TTS: durdu");
        };
    }

    public void SetVoice(string name)
    {
        try
        {
            _synth.SelectVoice(name);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Ses seçilemedi: " + ex.Message, ex);
        }
    }

    public void SetRate(int rate)
    {
        _synth.Rate = Math.Clamp(rate, -10, 10);
    }

    public void Speak(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Okunacak metin boş.");
        }

        _synth.SpeakAsyncCancelAll();
        _paused = false;
        StateChanged?.Invoke("TTS: okuyor");
        _synth.SpeakAsync(text);
    }

    public void PauseOrResume()
    {
        if (_synth.State == SynthesizerState.Speaking)
        {
            _synth.Pause();
            _paused = true;
            StateChanged?.Invoke("TTS: duraklatıldı");
        }
        else if (_paused)
        {
            _synth.Resume();
            _paused = false;
            StateChanged?.Invoke("TTS: okuyor");
        }
    }

    public void Stop()
    {
        _synth.SpeakAsyncCancelAll();
        if (_synth.State == SynthesizerState.Paused)
        {
            _synth.Resume();
        }

        _paused = false;
        StateChanged?.Invoke("TTS: durdu");
    }

    public void Dispose()
    {
        _synth.Dispose();
    }
}
