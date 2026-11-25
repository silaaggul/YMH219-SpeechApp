using SpeechApp.Services;

namespace SpeechApp.Forms;

public sealed class SpeechForm : Form
{
    private readonly TextToSpeechService _tts = new();
    private readonly SpeechToTextService _stt = new();
    private readonly RichTextBox _ttsBox;
    private readonly RichTextBox _sttBox;
    private readonly ListBox _historyList;
    private readonly Label _statusLabel;
    private readonly ComboBox _voiceBox;
    private readonly TrackBar _rateBar;

    public SpeechForm()
    {
        Text = "YMH219 Speech-to-Text / Text-to-Speech";
        ClientSize = new Size(860, 620);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(760, 540);

        _statusLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 34,
            Padding = new Padding(12, 8, 12, 8),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Text = "Aktif işlem: yok"
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 40));

        var ttsPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
        ttsPanel.Controls.Add(BuildTtsGroup(out _ttsBox, out _voiceBox, out _rateBar));

        var sttPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
        sttPanel.Controls.Add(BuildSttGroup(out _sttBox));

        _historyList = new ListBox { Dock = DockStyle.Fill };
        var historyGroup = new GroupBox { Text = "Zaman damgalı tanıma geçmişi", Dock = DockStyle.Fill, Padding = new Padding(8) };
        historyGroup.Controls.Add(_historyList);

        layout.Controls.Add(ttsPanel, 0, 0);
        layout.SetRowSpan(ttsPanel, 2);
        layout.Controls.Add(sttPanel, 1, 0);
        layout.Controls.Add(historyGroup, 1, 1);

        Controls.Add(layout);
        Controls.Add(_statusLabel);

        _tts.StateChanged += s => BeginInvoke(() => SetStatus(s));
        _stt.StateChanged += s => BeginInvoke(() => SetStatus(s));
        _stt.Failed += err => BeginInvoke(() =>
            MessageBox.Show(this, err, "Konuşma tanıma", MessageBoxButtons.OK, MessageBoxIcon.Warning));
        _stt.TextRecognized += text => BeginInvoke(() =>
        {
            _sttBox.AppendText(text + Environment.NewLine);
            _historyList.Items.Add($"{DateTime.Now:HH:mm:ss}  {text}");
        });

        LoadVoices();
        FormClosing += (_, _) =>
        {
            _tts.Dispose();
            _stt.Dispose();
        };
    }

    private Control BuildTtsGroup(out RichTextBox box, out ComboBox voices, out TrackBar rate)
    {
        var group = new GroupBox { Text = "Text-to-Speech", Dock = DockStyle.Fill };
        box = new RichTextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11), Text = "Merhaba, bu metin sesli okunacak." };

        var tools = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 84, Padding = new Padding(4) };
        voices = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260 };
        rate = new TrackBar { Minimum = -10, Maximum = 10, Value = 0, Width = 160, TickFrequency = 2 };
        var speak = new Button { Text = "Başlat", Width = 90, Height = 30 };
        var pause = new Button { Text = "Duraklat / Sürdür", Width = 140, Height = 30 };
        var stop = new Button { Text = "Durdur", Width = 90, Height = 30 };
        var clear = new Button { Text = "Temizle", Width = 90, Height = 30 };
        var save = new Button { Text = "Metni Kaydet", Width = 110, Height = 30 };

        speak.Click += (_, _) =>
        {
            try
            {
                if (voices.SelectedItem is string voice)
                {
                    _tts.SetVoice(voice);
                }

                _tts.SetRate(rate.Value);
                _tts.Speak(box.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "TTS", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };
        pause.Click += (_, _) => _tts.PauseOrResume();
        stop.Click += (_, _) => _tts.Stop();
        clear.Click += (_, _) => box.Clear();
        save.Click += (_, _) => SaveText(box.Text);

        tools.Controls.Add(new Label { Text = "Ses:", AutoSize = true, Margin = new Padding(4, 8, 4, 0) });
        tools.Controls.Add(voices);
        tools.Controls.Add(new Label { Text = "Hız:", AutoSize = true, Margin = new Padding(8, 8, 4, 0) });
        tools.Controls.Add(rate);
        tools.Controls.Add(speak);
        tools.Controls.Add(pause);
        tools.Controls.Add(stop);
        tools.Controls.Add(clear);
        tools.Controls.Add(save);

        group.Controls.Add(box);
        group.Controls.Add(tools);
        return group;
    }

    private Control BuildSttGroup(out RichTextBox box)
    {
        var group = new GroupBox { Text = "Speech-to-Text", Dock = DockStyle.Fill };
        box = new RichTextBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11) };
        var tools = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, Padding = new Padding(4) };
        var start = new Button { Text = "Mikrofonu Başlat", Width = 140, Height = 30 };
        var stop = new Button { Text = "Durdur", Width = 90, Height = 30 };
        var clear = new Button { Text = "Temizle", Width = 90, Height = 30 };
        var save = new Button { Text = "Metni Kaydet", Width = 110, Height = 30 };
        start.Click += (_, _) => _stt.Start();
        stop.Click += (_, _) => _stt.Stop();
        clear.Click += (_, _) => box.Clear();
        save.Click += (_, _) => SaveText(box.Text);
        tools.Controls.Add(start);
        tools.Controls.Add(stop);
        tools.Controls.Add(clear);
        tools.Controls.Add(save);
        group.Controls.Add(box);
        group.Controls.Add(tools);
        return group;
    }

    private void LoadVoices()
    {
        try
        {
            foreach (string name in _tts.VoiceNames)
            {
                _voiceBox.Items.Add(name);
            }

            if (_voiceBox.Items.Count > 0)
            {
                _voiceBox.SelectedIndex = 0;
            }
            else
            {
                SetStatus("TTS: yüklü ses bulunamadı");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Konuşma motoru açılamadı: " + ex.Message, "TTS", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SetStatus(string text)
    {
        _statusLabel.Text = "Aktif işlem: " + text;
        _statusLabel.BackColor = text.Contains("okuyor", StringComparison.OrdinalIgnoreCase) ||
                                 text.Contains("dinleniyor", StringComparison.OrdinalIgnoreCase)
            ? Color.FromArgb(198, 239, 206)
            : Color.FromArgb(255, 243, 176);
    }

    private void SaveText(string text)
    {
        try
        {
            using var dialog = new SaveFileDialog { Filter = "Metin (*.txt)|*.txt", FileName = "speech.txt" };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                File.WriteAllText(dialog.FileName, text);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Kaydedilemedi: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
