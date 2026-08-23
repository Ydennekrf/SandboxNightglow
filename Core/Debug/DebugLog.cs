using Godot;
using System;
using System.IO;

namespace ethra.V1;

public static class DebugLog
{
    private static readonly object Sync = new();

    public static bool Enabled { get; set; } = true;
    public static bool MirrorToGodotOutput { get; set; }
    public static string LogFilePath { get; set; } = "user://logs/debug.log";
    public static string ResolvedLogFilePath => ResolveLogPath();

    public static void Info(string category, string message, Node context = null)
    {
        Write("INFO", category, message, context);
    }

    public static void Warning(string category, string message, Node context = null)
    {
        Write("WARN", category, message, context);
        if (MirrorToGodotOutput)
        {
            GD.PushWarning(FormatConsoleMessage(category, message, context));
        }
    }

    public static void Error(string category, string message, Node context = null)
    {
        Write("ERROR", category, message, context);
        if (MirrorToGodotOutput)
        {
            GD.PushError(FormatConsoleMessage(category, message, context));
        }
    }

    public static void Clear()
    {
        string path = ResolveLogPath();
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        lock (Sync)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            File.WriteAllText(path, string.Empty);
        }
    }

    private static void Write(string level, string category, string message, Node context)
    {
        if (!Enabled)
        {
            return;
        }

        string path = ResolvedLogFilePath;
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        string line = FormatLine(level, category, message, context);

        lock (Sync)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            File.AppendAllText(path, line + System.Environment.NewLine);
        }

        if (MirrorToGodotOutput && level == "INFO")
        {
            GD.Print(FormatConsoleMessage(category, message, context));
        }
    }

    private static string FormatLine(string level, string category, string message, Node context)
    {
        string safeCategory = string.IsNullOrWhiteSpace(category) ? "General" : category.Trim();
        string contextPath = context?.GetPath().ToString() ?? string.Empty;
        ulong frame = Engine.GetProcessFrames();
        ulong ticks = Time.GetTicksMsec();
        return $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] [{safeCategory}] frame={frame} ticks={ticks} path={contextPath} {message}";
    }

    private static string FormatConsoleMessage(string category, string message, Node context)
    {
        string safeCategory = string.IsNullOrWhiteSpace(category) ? "General" : category.Trim();
        string contextPath = context?.GetPath().ToString();
        return string.IsNullOrWhiteSpace(contextPath)
            ? $"[{safeCategory}] {message}"
            : $"[{safeCategory}] {contextPath}: {message}";
    }

    private static string ResolveLogPath()
    {
        string configuredPath = string.IsNullOrWhiteSpace(LogFilePath)
            ? "user://logs/debug.log"
            : LogFilePath.Trim();

        if (configuredPath.StartsWith("user://", StringComparison.OrdinalIgnoreCase)
            || configuredPath.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
        {
            return ProjectSettings.GlobalizePath(configuredPath);
        }

        return configuredPath;
    }
}
