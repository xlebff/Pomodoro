namespace SimplePomodoro.Core;

/// <summary>
///     Contains constant string values used for command-line argument parsing.
///     Defines the names of top-level commands and their subcommands.
/// </summary>
internal static class Commands
{
    /// <summary>Command name to start the Pomodoro timer (default action).</summary>
    public const string StartCommandName = "start";

    /// <summary>Command name to change a configuration value (requires subcommand and new value).</summary>
    public const string SetCommandName = "set";

    /// <summary>Command name to retrieve the current value of a configuration setting.</summary>
    public const string GetCommandName = "get";

    /// <summary>Subcommand for setting or getting the work phase duration (in minutes).</summary>
    public const string WorkSubcommandName = "work";

    /// <summary>Subcommand for setting or getting the short break phase duration (in minutes).</summary>
    public const string BreakSubcommandName = "break";

    /// <summary>Subcommand for setting or getting the long break phase duration (in minutes).</summary>
    public const string LongBreakSubcommandName = "long-break";

    /// <summary>Subcommand for setting or getting the number of work/break cycles before a long break.</summary>
    public const string CyclesBeforeLongBreakSubcommandName =
        "cycles-to-long-break";

    /// <summary>Subcommand for setting or getting the total number of cycles in a session.</summary>
    public const string CyclesCountSubcommandName = "cycles";

    /// <summary>Subcommand for setting or getting the music directory path.</summary>
    public const string MusicDirSubcommandName = "music-dir";
}