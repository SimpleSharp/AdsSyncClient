using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdsSyncClientAvaloniaDemo.Models
{
    public interface ILogSink
    {
        void Append(string message);
    }

    public class LogSink(Action<string> action) : ILogSink
    {
        public void Append(string message) => action(message);
    }
}
