using ReceiptReader.Logging;
using Serilog;

namespace ReceiptReader.UI;

internal sealed class LogViewerForm : Form
{
    private const int MaxTextLength = 2_000_000;

    private readonly LogRingBuffer _buffer;
    private readonly string _appName;
    private readonly TextBox _logTextBox;
    private Action<string>? _lineAppendedHandler;

    private bool _applicationExiting;

    public LogViewerForm(LogRingBuffer buffer, string appName)
    {
        _buffer = buffer;
        _appName = appName;

        Text = $"{appName} — Logs";
        Size = new Size(1200, 600);
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = false;

        _logTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            WordWrap = false,
            Font = new Font(FontFamily.GenericMonospace, 9f),
            HideSelection = false
        };

        Controls.Add(_logTextBox);

        Resize += OnResizeHideToTray;
        FormClosing += OnFormClosing;
        VisibleChanged += OnVisibleChanged;
    }

    private void OnVisibleChanged(object? sender, EventArgs e)
    {
        if (Visible)
        {
            LoadSnapshot();
            SubscribeToLiveUpdates();
        }
        else
        {
            UnsubscribeFromLiveUpdates();
        }
    }

    private void OnResizeHideToTray(object? sender, EventArgs e)
    {
        if (WindowState == FormWindowState.Minimized)
        {
            HideToTray();
        }
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_applicationExiting)
        {
            return;
        }

        if (e.CloseReason != CloseReason.UserClosing)
        {
            return;
        }

        var confirmExit = MessageBox.Show(
            $"Exit {_appName}?",
            "Confirm Exit",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirmExit != DialogResult.Yes)
        {
            e.Cancel = true;
            return;
        }

        Log.Information("Application exit requested from log viewer.");
        PrepareForApplicationExit();
    }

    private void HideToTray()
    {
        Log.Information("Log viewer minimized to system tray.");
        ShowInTaskbar = false;
        Hide();
    }

    internal void PrepareForApplicationExit()
    {
        _applicationExiting = true;
    }

    private void LoadSnapshot()
    {
        var snapshot = _buffer.GetSnapshot();
        _logTextBox.Text = string.Join(Environment.NewLine, snapshot);
        ScrollToEnd();
    }

    private void SubscribeToLiveUpdates()
    {
        UnsubscribeFromLiveUpdates();

        _lineAppendedHandler = line =>
        {
            if (IsDisposed)
            {
                return;
            }

            if (InvokeRequired)
            {
                BeginInvoke(AppendLineAndScroll);
                return;
            }

            AppendLineAndScroll();

            void AppendLineAndScroll()
            {
                if (_logTextBox.TextLength > 0)
                {
                    _logTextBox.AppendText(Environment.NewLine);
                }

                _logTextBox.AppendText(line);
                TrimExcessText();
                ScrollToEnd();
            }
        };

        _buffer.LineAppended += _lineAppendedHandler;
    }

    private void UnsubscribeFromLiveUpdates()
    {
        if (_lineAppendedHandler is not null)
        {
            _buffer.LineAppended -= _lineAppendedHandler;
            _lineAppendedHandler = null;
        }
    }

    private void TrimExcessText()
    {
        if (_logTextBox.TextLength <= MaxTextLength)
        {
            return;
        }

        var overflow = _logTextBox.TextLength - MaxTextLength;
        var nextNewLine = _logTextBox.Text.IndexOf('\n', overflow);

        if (nextNewLine >= 0)
        {
            _logTextBox.Select(0, nextNewLine + 1);
            _logTextBox.SelectedText = string.Empty;
        }
    }

    private void ScrollToEnd()
    {
        _logTextBox.SelectionStart = _logTextBox.TextLength;
        _logTextBox.SelectionLength = 0;
        _logTextBox.ScrollToCaret();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            UnsubscribeFromLiveUpdates();
            _logTextBox.Dispose();
        }

        base.Dispose(disposing);
    }
}
