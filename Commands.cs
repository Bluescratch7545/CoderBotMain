using Discord;
using Discord.Interactions;
using System.Net.Http;
using System.Text;
using System.Text.Json;

public class Commands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly GroqService _service = new GroqService();

    [SlashCommand("ai", "ai test cmd")]
    public async Task AI(string question, IAttachment? file = null)
    {

        if (question == "rulesshow" && Context.User.Id == 1389274198052175976)
        {
            await DeferAsync(ephemeral: true);

            string rulesList = File.ReadAllText("prompts/base.txt");

            await FollowupAsync($"```txt\n{rulesList}\n```");

            return;
        }


        await DeferAsync();

        bool hasAttachement = file != null;

        string extraInstructions = "";

        var detector = new LooksLikeCode(question);
        var isCode = detector.IsCode;

        if (!hasAttachement && isCode)
        {
            extraInstructions =
                "\n\n- Mention briefly that attachments are easier for large code snippets.";
        }

        string input = question;
        string basePrompt = File.ReadAllText("prompts/base.txt");

        if (file != null)
        {
            using var http = new HttpClient();
            var bytes = await http.GetByteArrayAsync(file.Url);

            string lang = GetLanguage(file.Filename);

            input += $"\n\n--- FILE ---\n";
            input += $"Filename: {file.Filename}\n";
            input += $"Language: {lang}\n";
            input += $"Type: {file.ContentType}\n\n";

            if (file.ContentType.StartsWith("text/") || lang != "Unknown")
            {
                string text = Encoding.UTF8.GetString(bytes).Trim();
                text = text.Replace("\uFEFF", "");

                input += text;

                extraInstructions += $"- You are analyzing a file in the {lang} language. \n- Try your best to respond more accurate please.";
            }
            else
            {
                input += "[Binary file - not readable as text]";
            }
        }

        string finalPrompt =
            basePrompt +
            extraInstructions +
            "\n\nUSER QUESTION:\n" +
            input;

        Console.WriteLine(finalPrompt);

        string response = await _service.AskAsync(finalPrompt);

        if (string.IsNullOrWhiteSpace(response))
        {
            response = "No response from AI. Sorry!";
        }

        if (response.Length > 1900)
            response = response[..1900];

        await FollowupAsync(response);
    }
    string GetLanguage(string filename)
    {
        return Path.GetExtension(filename).ToLower() switch
        {
            ".cs" => "C#",
            ".js" => "JavaScript",
            ".ts" => "TypeScript",
            ".py" => "Python",
            ".cpp" => "C++",
            ".c" => "C",
            ".java" => "Java",
            ".json" => "JSON",
            ".html" => "HTML",
            ".css" => "CSS",
            _ => "Unknown"
        };
    }
}