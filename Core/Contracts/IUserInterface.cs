using System;
using System.Collections.Generic;
using System.Text;
using ConsolePomodoro.Engine;
using ConsolePomodoro.Infrastructure.UI;
using ConsolePomodoro.Resources;
using Spectre.Console;

namespace ConsolePomodoro.Core.Contracts
{
    internal interface IUserInterface
    {
        ConsolePrintingState PrintingState { get; }


        Task WriteMessageAsync(string message, float delay = 0);


        void Skip();


        Task DrawProgressBarAsync(
            int phase,
            float totalSeconds,
            Func<TimeSpan> GetElapsed);


        Task OnPomodoroStartAsync(object? sender, EventArgs e);

        Task OnPomodoroEndAsync(object? sender, EventArgs e);

        Task OnPomodoroIntAsync(object? sender, EventArgs e);

        void OnPhaseStart(object? sender, EventArgs e);

        void OnPhaseEnd(object? sender, EventArgs e);
    }
}
