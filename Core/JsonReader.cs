using System.Text;
using System.Text.Json;

namespace SimplePomodoro.Core;

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
    public static async Task<T?> ReadAsync<T>(
        string path)
    {
        try
        {
            string json = await File.ReadAllTextAsync(path, Encoding.UTF8);
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return (default(T));
        }
    }

    public static async Task<bool> SaveAsync<T>(string path, T config)
    {
        try
        {
            await File.WriteAllTextAsync(path,
                JsonSerializer.Serialize(config, jsonOptions));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return false;
        }

        return true;
    }

    static readonly JsonSerializerOptions jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };
}
