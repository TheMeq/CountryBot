﻿using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordBotTemplate.Logger;
using DiscordBotTemplate.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using static Discord.GatewayIntents;


namespace DiscordBotTemplate;
internal class Program
{
    private static bool IsDebug()
{
#if DEBUG
    return true;
#else
        return false;
#endif
}

private DiscordSocketClient _client;
public static IConfiguration StaticConfig { get; private set; }
public static Task Main() => new Program().MainAsync();

private async Task MainAsync()
{
    var config = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json")

        .Build();

    using var host = Host.CreateDefaultBuilder()
        .ConfigureServices((_, services) =>
            services
                .AddSingleton(config)
                .AddSingleton(_ => new DiscordSocketClient(new DiscordSocketConfig
                {
                    GatewayIntents = Guilds |
                                     GuildBans |
                                     GuildEmojis |
                                     GuildMessages |
                                     GuildMessageReactions |
                                     DirectMessages,
                    AlwaysDownloadUsers = true
                }))
                .AddTransient<ConsoleLogger>()
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                .AddSingleton<InteractionHandler>()
                .AddSingleton(_ => new CommandService(new CommandServiceConfig
                {
                    LogLevel = LogSeverity.Debug,
                    DefaultRunMode = Discord.Commands.RunMode.Async
                }))
        )
        .Build();

    await RunAsync(host);
}

private async Task RunAsync(IHost host)
{
    using var serviceScope = host.Services.CreateScope();
    var provider = serviceScope.ServiceProvider;

    var commands = provider.GetRequiredService<InteractionService>();
    _client = provider.GetRequiredService<DiscordSocketClient>();

    var config = provider.GetRequiredService<IConfigurationRoot>();
    StaticConfig = config;

    await provider.GetRequiredService<InteractionHandler>().InitializeAsync();

    _client.Log += _ => provider.GetRequiredService<ConsoleLogger>().Log(_);
    commands.Log += _ => provider.GetRequiredService<ConsoleLogger>().Log(_);

    _client.JoinedGuild += async _ =>
    {
        var guildCount = _client.Guilds.Count;
        await _client.SetGameAsync($"{guildCount} guilds play my games!", null, ActivityType.Watching);
    };

    _client.LeftGuild += async _ =>
    {
        var guildCount = _client.Guilds.Count;
        await _client.SetGameAsync($"{guildCount} guilds play my games!", null, ActivityType.Watching);
    };

    _client.Ready += async () =>
    {
        if (IsDebug())
            await commands.RegisterCommandsToGuildAsync(ulong.Parse(config["testGuild"]));
        else
            await commands.RegisterCommandsGloballyAsync();

        var guildCount = _client.Guilds.Count;
        await _client.SetGameAsync($"{guildCount} guilds play my games!", null, ActivityType.Watching);

        //var unused = new RecurringJobUtility(_client);

        var dateTime = new DateTime(2000, 1, 1, 0, 0, 0)
            .AddDays(Assembly.GetEntryAssembly()!.GetName().Version!.Build)
            .AddSeconds(Assembly.GetEntryAssembly()!.GetName().Version!.Revision * 2).ToUniversalTime();

        var dateSince = DateTime.UtcNow - dateTime;

        var log = new ConsoleLogger();
        await log.Log(new LogMessage(LogSeverity.Info, "Bot", $"Version: {Assembly.GetEntryAssembly()!.GetName().Version!}"));
        await log.Log(new LogMessage(LogSeverity.Info, "Bot", $"Build Date: {dateTime}"));
        await log.Log(new LogMessage(LogSeverity.Info, "Bot", $"            ({GeneralUtility.ToReadableString(dateSince)} ago)"));
    };

    await _client.LoginAsync(TokenType.Bot, config["tokens:discord"]);
    await _client.StartAsync();

    await Task.Delay(-1);

}
}