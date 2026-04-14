using ConsolePomodoro.Domain.Models;

namespace ConsolePomodoro.Core.Contracts
{
    /// <summary>
    /// An interface for configs from JSON.
    /// </summary>
    internal interface IJsonConfigRepository
    {
        /// <summary>
        /// Data validating for current config.
        /// </summary>
        /// <returns>
        /// A tuple containing:
        /// - <c>Success</c>: operation success flag 
        /// (true – success, false – error);
        /// - <c>Message</c>: error message 
        /// (null on success, otherwise the exception text).
        /// </returns>
        (bool Success, string? Message) Validate();
        /// <summary>
        /// Creating a default configuration using default path.
        /// </summary>
        /// <returns>
        /// A tuple containing:
        /// - <c>Success</c>: operation success flag 
        /// (true – success, false – error);
        /// - <c>Message</c>: error message 
        /// (null on success, otherwise the exception text).
        /// </returns>
        Task<(bool Success, string? Message)> CreateDefaultConfig();
        /// <summary>
        /// Method for getting current config.
        /// </summary>
        /// <returns>Current config.</returns>
        IConfig GetConfig();

        Task<(bool Success, string? Message)> SaveAsync();
    }
}
