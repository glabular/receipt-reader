namespace ReceiptReader.Logging;

internal sealed class LogRingBuffer
{
    private readonly int _capacity;
    private readonly Queue<string> _lines = new();
    private readonly object _lock = new();

    public LogRingBuffer(int capacity)
    {
        _capacity = capacity;
    }

    public event Action<string>? LineAppended;

    public void Append(string line)
    {
        Action<string>? handler;

        lock (_lock)
        {
            _lines.Enqueue(line);

            while (_lines.Count > _capacity)
            {
                _lines.Dequeue();
            }

            handler = LineAppended;
        }

        handler?.Invoke(line);
    }

    public IReadOnlyList<string> GetSnapshot()
    {
        lock (_lock)
        {
            return _lines.ToArray();
        }
    }
}
