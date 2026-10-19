namespace XGJPlus;

/// <summary>
/// 简单的控制台进度条
/// </summary>
public class ProgressBar
{
    private readonly int _total;
    private readonly int _width;
    private int _current;

    public ProgressBar(int total, int width = 20)
    {
        _total = total;
        _width = width;
        _current = 0;
    }

    public void Tick(string? message = null)
    {
        _current++;
        var percent = (double)_current / _total;
        var filled = (int)(percent * _width);
        var bar = new string('=', filled) + new string(' ', _width - filled);
        var percentStr = $"{percent:P0}";
        var line = $"\r[{bar}] {percentStr}";
        if (!string.IsNullOrEmpty(message))
            line += $" {message}";
        Console.Write(line);
        if (_current >= _total)
            Console.WriteLine();
    }
}
