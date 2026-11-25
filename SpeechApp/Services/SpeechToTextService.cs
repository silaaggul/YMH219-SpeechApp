using System.Globalization;
using System.Speech.Recognition;

namespace SpeechApp.Services;

public sealed class SpeechToTextService : IDisposable
{
    private SpeechRecognitionEngine? _engine;

    public event Action<string>? TextRecognized;
    public event Action<string>? StateChanged;
    public event Action<string>? Failed;

    public bool IsListening => _engine is not null;

    public void Start()
    {
        if (_engine is not null)
        {
            return;
        }

        try
        {
            CultureInfo culture = CultureInfo.InstalledUICulture;
            try
            {
                _engine = new SpeechRecognitionEngine(culture);
            }
            catch
            {
                _engine = new SpeechRecognitionEngine(new CultureInfo("en-US"));
            }

            _engine.LoadGrammar(new DictationGrammar());
            _engine.SetInputToDefaultAudioDevice();
            _engine.SpeechRecognized += (_, e) =>
            {
                string text = e.Result.Text;
                TextRecognized?.Invoke(text);
            };
            _engine.RecognizeCompleted += (_, e) =>
            {
                if (e.Error is not null)
                {
                    Failed?.Invoke("Tanıma hatası: " + e.Error.Message);
                }
            };

            _engine.RecognizeAsync(RecognizeMode.Multiple);
            StateChanged?.Invoke("STT: dinleniyor");
        }
        catch (Exception ex)
        {
            Dispose();
            Failed?.Invoke("Mikrofona veya konuşma servisine erişilemedi: " + ex.Message);
            StateChanged?.Invoke("STT: kapalı");
        }
    }

    public void Stop()
    {
        Dispose();
        StateChanged?.Invoke("STT: kapalı");
    }

    public void Dispose()
    {
        if (_engine is null)
        {
            return;
        }

        try
        {
            _engine.RecognizeAsyncStop();
        }
        catch
        {
        }

        _engine.Dispose();
        _engine = null;
    }
}
