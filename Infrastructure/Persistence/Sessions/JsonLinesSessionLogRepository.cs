using System.Text;
using System.Text.Json;
using ConsolePomodoro.Core.Contracts;
using ConsolePomodoro.Domain.Models;

namespace ConsolePomodoro.Infrastructure.Persistence.Sessions
{
    /// <summary>
    /// Static class for simple loggin sessions to track working time.
    /// </summary>
    internal static class JsonLinesSessionLogRepository
    {
        private static readonly string _logPath =
            "./Data/Sessions/sessions.jsonl";
        private static readonly JsonSerializerOptions _options =
            new() { WriteIndented = false };

        /// <summary>
        /// Writes a record to ./Data/Sessions/sessions.jsonl.
        /// </summary>
        /// <param name="logEntry">
        /// Current record.
        /// </param>
        /// <returns>
        /// In case of any 
        /// exception (missing file, invalid JSON, etc.)
        /// a tuple is returned with Success = false and the error 
        /// text in the Message.
        /// </returns>
        public static async Task<(bool Success, string? Message)> LogAsync(SessionRecord logEntry)
        {
            try
            {
                string jsonLine = JsonSerializer.Serialize(logEntry, _options);
                await File.AppendAllLinesAsync(_logPath,
                    [jsonLine],
                    Encoding.UTF8);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }

            return (true, null);
        }
    }
}
