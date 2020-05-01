using System;
using System.Collections;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace Julmar.AzDOUtilities
{
    [Flags]
    public enum LogLevel
    {
        None = 0,
        Query = 1,
        LinqQuery = 2,
        EnterExit = 4,
        PatchDocument = 8,
        RelatedApis = 16,
        LinqExpression = 32,
        RawApis = 64
    }

    internal class TraceHelpers
    {
        internal LogLevel TraceLevel = LogLevel.None;
        internal Action<string> LogHandler;
        internal int indent = 0;

        public bool WillLog(LogLevel level)
        {
            return TraceLevel.HasFlag(level);
        }

        class EnterExitAction : IDisposable
        {
            private readonly TraceHelpers helper;
            private readonly LogLevel logLevel;
            private string methodName;

            public EnterExitAction(TraceHelpers helper, LogLevel logLevel, string methodName, object args)
            {
                this.helper = helper;
                this.logLevel = logLevel;
                this.methodName = methodName;
                string parameters = "";
                if (args != null && args is object[] pms)
                {
                    parameters = string.Join(',', pms.Select(DumpObject));
                }
                else if (args != null)
                    parameters = args.ToString();

                helper.WriteLine(logLevel, $">> {methodName}({parameters})");
                helper.indent++;
            }

            private string DumpObject(object obj)
            {
                if (obj == null) return "null";

                if (obj is string s)
                {
                    return $"\"{s}\"";
                }
                else if (obj is WorkItem wit)
                {
                    return (wit.Id == null)
                        ? $"{wit.WorkItemType} NewItem"
                        : $"{wit.WorkItemType} {wit.Id}";
                }
                else if (obj is IEnumerable)
                {
                    StringBuilder sb = new StringBuilder("[");
                    int pos = 0;
                    foreach (var item in (IEnumerable)obj)
                    {
                        if (pos++ > 0) sb.Append(',');
                        if (obj is string str)
                        {
                            sb.Append($"\"{str}\"");
                        }
                        else
                        {
                            sb.Append(item?.ToString() ?? "null");
                        }
                    }
                    sb.Append("]");
                    return sb.ToString();
                }
                else if (obj is CancellationToken)
                {
                    return (((CancellationToken)obj) == CancellationToken.None)
                        ? "CancellationToken.None"
                        : ((CancellationToken)obj).ToString();
                }
                return obj.ToString();
            }

            public void Dispose()
            {
                if (methodName == null)
                    throw new ObjectDisposedException("EnterExitAction disposed more than once.");

                helper.indent--;
                helper.WriteLine(logLevel, $"<< {methodName}()");
                methodName = null;
            }
        }

        public void Dump(JsonPatchDocument document)
        {
            if (LogHandler != null
                && TraceLevel.HasFlag(LogLevel.PatchDocument))
            {
                var sb = new StringBuilder($"JsonPatchDocument {document.GetHashCode()}");
                sb.AppendLine();
                foreach (var entry in document)
                {
                    if (entry.Operation == Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Remove)
                    {
                        sb.AppendLine($"  {entry.Operation}: {entry.Path}");
                    }
                    else
                    {
                        sb.AppendLine($"  {entry.Operation}: {entry.Path} = \"{entry.Value}\"");
                    }
                }

                LogHandler?.Invoke(sb.ToString());
            }
        }

        public void Dump(byte[] buffer, int bytesPerLine = 16)
        {
            if (LogHandler == null) return;

            var sb = new StringBuilder();
            for (int line = 0; line < buffer.Length; line += bytesPerLine)
            {
                byte[] lineBytes = buffer.Skip(line).Take(bytesPerLine).ToArray();
                sb.AppendFormat("{0:x8} ", line);
                sb.Append(string.Join(" ", lineBytes.Select(b => b.ToString("x2"))
                       .ToArray()).PadRight(bytesPerLine * 3));
                sb.Append(" ");
                sb.Append(new string(lineBytes.Select(b => b < 32 ? '.' : (char)b)
                       .ToArray()));
                sb.AppendLine();
            }
            LogHandler?.Invoke(sb.ToString());
        }

        public void WriteLine(LogLevel level, string message)
        {
            if (TraceLevel.HasFlag(level))
                LogHandler?.Invoke(message);
        }

        public IDisposable Enter(LogLevel level, object args = null, [CallerMemberName] string method = "")
        {
            return (LogHandler != null && TraceLevel.HasFlag(level))
                ? new EnterExitAction(this, level, method, args)
                : null;
        }

        public IDisposable Enter(object args = null, [CallerMemberName] string method = "")
        {
            return Enter(LogLevel.EnterExit, args, method);
        }
    }
}
