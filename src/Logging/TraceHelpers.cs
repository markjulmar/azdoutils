using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace Julmar.AzDOUtilities;

/// <summary>
/// Trace helper class
/// </summary>
internal class TraceHelpers
{
    internal LogLevel TraceLevel = LogLevel.None;
    internal Action<string>? LogHandler;

    /// <summary>
    /// Return whether this event will be logged based on the log level.
    /// </summary>
    /// <param name="level">Log level to check</param>
    /// <returns>True/False</returns>
    public bool WillLog(LogLevel level) => TraceLevel.HasFlag(level);

    /// <summary>
    /// Object used to trace entry/exit methods
    /// </summary>
    private class EnterExitAction : IDisposable
    {
        private readonly TraceHelpers helper;
        private readonly LogLevel logLevel;
        private string? methodName;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="helper">Trace helper</param>
        /// <param name="logLevel">Logging level to use</param>
        /// <param name="methodName">Name of method entered</param>
        /// <param name="args">Method arguments</param>
        public EnterExitAction(TraceHelpers helper, LogLevel logLevel, string methodName, object? args)
        {
            this.helper = helper ?? throw new ArgumentNullException(nameof(helper));
            this.logLevel = logLevel;
            this.methodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
            
            var parameters = string.Empty;
            if (args is object[] pms)
                parameters = string.Join(',', pms.Select(DumpObject));
            else if (args != null)
                parameters = args.ToString()??"";

            helper.WriteLine(logLevel, $">> {methodName}({parameters})");
        }

        /// <summary>
        /// Dump an object to the trace stream.
        /// </summary>
        /// <param name="obj">Object to dump</param>
        /// <returns>String representing object</returns>
        private static string DumpObject(object? obj)
        {
            switch (obj)
            {
                case null: 
                    return "null";
                case string s:
                    return $"\"{s}\"";
                case WorkItem wit:
                    return wit.Id == null ? $"{wit.WorkItemType} NewItem" : $"{wit.WorkItemType} {wit.Id}";
                case IEnumerable enumerable:
                {
                    var sb = new StringBuilder("[");
                    int pos = 0;
                    foreach (var item in enumerable)
                    {
                        if (pos++ > 0) sb.Append(',');
                        if (enumerable is string str)
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
                case CancellationToken token:
                    return token == CancellationToken.None
                        ? "CancellationToken.None"
                        : token.ToString()??"CancellationToken";
                default:
                    return obj.ToString()??obj.GetType().Name;
            }
        }

        /// <summary>
        /// Dispose the enter/exit method
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public void Dispose()
        {
            if (methodName == null)
                throw new ObjectDisposedException("EnterExitAction disposed more than once.");

            helper.WriteLine(logLevel, $"<< {methodName}()");
            methodName = null;
        }
    }

    /// <summary>
    /// Method to display a Patch object being sent to Azure DevOps.
    /// </summary>
    /// <param name="document"></param>
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

    /// <summary>
    /// Method to dump a byte array as a hex block
    /// </summary>
    /// <param name="buffer">Buffer</param>
    /// <param name="bytesPerLine">Hex bytes per line</param>
    public void Dump(byte[] buffer, int bytesPerLine = 16)
    {
        if (LogHandler == null) return;

        var sb = new StringBuilder();
        for (int line = 0; line < buffer.Length; line += bytesPerLine)
        {
            byte[] lineBytes = buffer.Skip(line).Take(bytesPerLine).ToArray();
            sb.AppendFormat("{0:x8} ", line)
              .Append(string.Join(" ", lineBytes.Select(b => b.ToString("x2")).ToArray()).PadRight(bytesPerLine * 3))
              .Append(' ')
              .Append(new string(lineBytes.Select(b => b < 32 ? '.' : (char)b).ToArray()))
              .AppendLine();
        }
        LogHandler?.Invoke(sb.ToString());
    }

    /// <summary>
    /// Write a line to the log if the specified LogLevel is turned on.
    /// </summary>
    /// <param name="level">Log level</param>
    /// <param name="message">Text to write</param>
    public void WriteLine(LogLevel level, string message)
    {
        if (TraceLevel.HasFlag(level))
            LogHandler?.Invoke(message);
    }

    /// <summary>
    /// Method to generate an Enter/Exit event
    /// </summary>
    /// <param name="level">Level</param>
    /// <param name="args">Arguments</param>
    /// <param name="method">Method name</param>
    /// <returns>A disposable object</returns>
    public IDisposable? Enter(LogLevel level, object? args = null, [CallerMemberName] string method = "")
    {
        return LogHandler != null && TraceLevel.HasFlag(level)
            ? new EnterExitAction(this, level, method, args)
            : null;
    }

    /// <summary>
    /// Method to generate an Enter/Exit event
    /// </summary>
    /// <param name="args">Argument</param>
    /// <param name="method">Method name</param>
    /// <returns>A disposable object</returns>
    public IDisposable? Enter(object? args = null, [CallerMemberName] string method = "") 
        => Enter(LogLevel.EnterExit, args, method);
}