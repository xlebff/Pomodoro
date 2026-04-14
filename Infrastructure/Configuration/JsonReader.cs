using System.Runtime;
using System.Text;
using System.Text.Json;
using ConsolePomodoro.Resources;

namespace ConsolePomodoro.Infrastructure.Configuration
{
    internal static class JsonReader
    {
        /// <summary>
        /// Asynchronously reads JSON from a file and deserializes it 
        /// to the specified type.
        /// </summary>
        /// <typeparam name="T">The type of object that is 
        /// being deserialized to.</typeparam>
        /// <param name="path">The path to the file containing 
        /// the JSON data.</param>
        /// <returns>
        /// A tuple containing:
        /// - <c>res</c>: deserialized object of type 
        /// <typeparamref name="T"/> or default value in case of error;
        /// - <c>Success</c>: operation success flag 
        /// (true – success, false – error);
        /// - <c>Message</c>: error message 
        /// (null on success, otherwise the exception text).</returns>
        /// <remarks>
        /// The file is readable in UTF-8 encoding. In case of any 
        /// exception (missing file, invalid JSON, etc.)
        /// a tuple is returned with Success = false and the error 
        /// text in the Message.
        /// </remarks>
        public static async Task<(T? res, bool Success, string? Message)> ReadAsync<T>(
            string path)
        {
            try
            {
                string json = await File.ReadAllTextAsync(path, Encoding.UTF8);
                T? res = JsonSerializer.Deserialize<T>(json);
                return (res, true, null);
            }
            catch (Exception ex)
            {
                return (default(T), false, ex.Message);
            }
        }

        public static async Task<(bool Success, string? Message)> SaveAsync<T>(string path, T config)
        {
            try
            {
                await File.WriteAllTextAsync(path,
                    JsonSerializer.Serialize(config, jsonOptions));
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }

            return (true, null);
        }

        static JsonSerializerOptions jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
    }
}
