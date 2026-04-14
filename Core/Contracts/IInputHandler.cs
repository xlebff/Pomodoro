namespace ConsolePomodoro.Core.Contracts
{
    /// <summary>
    /// Provides a mechanism to handle console key presses asynchronously.
    /// </summary>
    internal interface IInputHandler
    {
        /// <summary>
        /// Occurs when a console key is pressed while the handler is listening.
        /// </summary>
        /// <remarks>
        /// The event handler receives the pressed <see cref="ConsoleKey"/> 
        /// and should return a <see cref="Task"/>,
        /// allowing asynchronous operations in response to the key press. 
        /// Multiple subscribers are supported.
        /// </remarks>
        event Func<ConsoleKey, Task> KeyPressed;

        /// <summary>
        /// Starts an asynchronous loop that listens for 
        /// console key presses and raises the <see cref="KeyPressed"/> event.
        /// </summary>
        /// <param name="cancellationToken">
        /// A cancellation token that signals the request to stop listening. 
        /// The method should exit gracefully when cancellation is requested.
        /// </param>
        /// <remarks>
        /// This method is typically called once during application startup 
        /// and runs until the token is cancelled.
        /// It does not block the calling thread; instead, it starts the 
        /// listening loop in the background.
        /// </remarks>
        void StartListening(CancellationToken cancellationToken);
    }
}