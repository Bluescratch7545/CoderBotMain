using System.Net.Http;
using System.Text.Json;
using System.Text;

public class GroqService
{
    private readonly HttpClient _http = new HttpClient();

    public async Task<string> AskAsync(string prompt)
    {
        var apiKey = Environment.GetEnvironmentVariable("GROQ_KEY")?.Trim();

        var body = new
        {
            model = "llama-3.3-70b-versatile",
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            temperature = 0.7
        };



        var json = JsonSerializer.Serialize(body);

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.groq.com/openai/v1/chat/completions"
        );

        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var res = await _http.SendAsync(request);
        var text = await res.Content.ReadAsStringAsync();

        Console.WriteLine(text);
        using var doc = JsonDocument.Parse(text);
#pragma warning disable CS8603
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();
#pragma warning restore CS8603
    }
}