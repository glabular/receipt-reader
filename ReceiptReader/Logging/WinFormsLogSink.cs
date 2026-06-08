using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;

namespace ReceiptReader.Logging;

internal sealed class WinFormsLogSink : ILogEventSink
{
    private readonly LogRingBuffer _buffer;
    private readonly ITextFormatter _formatter;

    public WinFormsLogSink(LogRingBuffer buffer, string outputTemplate)
    {
        _buffer = buffer;
        _formatter = new MessageTemplateTextFormatter(outputTemplate);
    }

    public void Emit(LogEvent logEvent)
    {
        using var writer = new StringWriter();
        _formatter.Format(logEvent, writer);
        var line = writer.ToString().TrimEnd('\r', '\n');

        if (line.Length > 0)
        {
            _buffer.Append(line);
        }
    }
}
