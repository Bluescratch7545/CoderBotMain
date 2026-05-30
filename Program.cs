using System.Net;
using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using DotNetEnv;
using System.Text;

class Program
{
    #nullable enable
    private DiscordSocketClient _client;
    private InteractionService _interactionService;
    private IServiceProvider _services;

    static Task Main(string[] args)
        => new Program().MainAsync();

    public async Task MainAsync()
    {
        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents =
                GatewayIntents.Guilds |
                GatewayIntents.GuildMessages |
                GatewayIntents.MessageContent,
            LogGatewayIntentWarnings = true

        });

        _interactionService = new InteractionService(_client.Rest);

        _services = new ServiceCollection()
            .AddSingleton(_client)
            .AddSingleton(_interactionService)
            .BuildServiceProvider();

        _client.Log += Log;
        
        _client.Ready += OnReady;

        _client.Ready += async () =>
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(2000);

                var channel = _client.GetChannel(1509914782680219688) as IMessageChannel;

                if (channel == null)
                {
                    return;
                }

                while (true)
                {
                    await channel.SendMessageAsync("Bot Active, time passed since last report: 5 minutes");
                    var messages = await channel.GetMessagesAsync(limit: 11).FlattenAsync();

                    var toDelete = messages
                        .Skip(1)
                        .Take(10);

                    foreach (var m in toDelete)
                    {
                        await channel.DeleteMessageAsync(m);
                    }

                    Console.WriteLine("Bot alive");

                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
            });
        };

        _client.MessageReceived += OnMessage;

        _client.SlashCommandExecuted += GetCustomCommandExecuteFuncs;

        _client.InteractionCreated += HandleInteraction;

        _ = Task.Run(async () =>
        {
            var _listener = new HttpListener();

            _listener.Prefixes.Add("http://*:10000/");
            _listener.Start();

            while (true)
            {
                var ctx = await _listener.GetContextAsync();

                var response = Encoding.UTF8.GetBytes("CoderBot Alive!");

                ctx.Response.OutputStream.Write(response);
                ctx.Response.Close();
            }
        });

        Env.Load(Path.Combine(Directory.GetCurrentDirectory(), "TOKEN.env"));
        string? token = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN");
        Console.WriteLine(token == null ? "TOKEN = NULL" : "TOKEN LOADED");
        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();
        await Task.Delay(-1);
    }
    private Task Log(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }

    private async Task<Task> OnReady()
    {
        Console.WriteLine($"Logged in as {_client.CurrentUser}");

        /*var testCommand = new SlashCommandBuilder()
            .WithName("test")
            .WithDescription("Test command...");

        await _client.CreateGlobalApplicationCommandAsync(testCommand.Build());*/
        await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        await _interactionService.RegisterCommandsGloballyAsync();

        return Task.CompletedTask;
    }

    private async Task OnMessage(SocketMessage message)
    {
        if (message.Author.IsBot)
            return;

        if (message.Content == "!hello")
        {
            await message.Channel.SendMessageAsync("Hello!");
        }
    }

    private async Task GetCustomCommandExecuteFuncs(SocketSlashCommand cmd)
    {
        if (cmd.CommandName == "test")
        {
            await cmd.RespondAsync("Test Works Command Hehe...");
        }
    }
    private async Task HandleInteraction(SocketInteraction interaction)
    {
        try
        {
            var context = new SocketInteractionContext(_client, interaction);
            await _interactionService.ExecuteCommandAsync(context, _services);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}