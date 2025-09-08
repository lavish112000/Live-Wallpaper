using Lively.Models.Message;
using System;

namespace Lively.Common.Extensions;

public static class IpcMessageExtensions
{
    public static void SendError(this Exception ex, Action<IpcMessage> send, string prefix = null)
    {
        send(new LivelyMessageConsole
        {
            Category = ConsoleMessageType.error,
            Message = prefix != null ? $"{prefix}: {ex}" : ex.ToString()
        });
    }

    public static void SendError(this string message, Action<IpcMessage> send, string prefix = null)
    {
        send(new LivelyMessageConsole
        {
            Category = ConsoleMessageType.error,
            Message = prefix != null ? $"{prefix}: {message}" : message
        });
    }

    public static void SendLog(this string message, Action<IpcMessage> send)
    {
        send(new LivelyMessageConsole
        {
            Category = ConsoleMessageType.log,
            Message = message
        });
    }

    public static void SendConsole(this string message, Action<IpcMessage> send)
    {
        send(new LivelyMessageConsole
        {
            Category = ConsoleMessageType.console,
            Message = message
        });
    }
}
