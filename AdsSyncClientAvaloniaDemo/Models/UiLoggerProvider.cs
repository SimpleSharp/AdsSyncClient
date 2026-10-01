using Avalonia.Logging;
using Microsoft.Extensions.Logging;

namespace AdsSyncClientAvaloniaDemo.Models
{
    internal sealed class UiLoggerProvider(ILogSink sink) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new UiLogger(categoryName, sink);
        public void Dispose() { }
    }
}
