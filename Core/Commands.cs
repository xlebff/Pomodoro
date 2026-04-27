namespace SimplePomodoro.Core;

internal static class Commands
{
    public const string StartCommandName = "start";
    public const string SetCommandName = "set";
    public const string GetCommandName = "get";

    public const string WorkSubcommandName = "work";
    public const string BreakSubcommandName = "break";
    public const string LongBreakSubcommandName = "long-break";
    public const string CyclesBeforeLongBreakSubcommandName = "cycles-to-long-break";
    public const string CyclesCountSubcommandName = "cycles";
    public const string MusicDirSubcommandName = "music-dir";
}
