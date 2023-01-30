using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using CountryBot.Logger;
using CountryBot.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using static Discord.GatewayIntents;
using CountryBot.Models;
using Discord.Rest;

namespace CountryBot;
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
    public static ConfigModel StaticConfig { get; private set; }
    public static Task Main() => new Program().MainAsync();

    private async Task MainAsync()
    {
        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
                services
                    .AddSingleton(_ => new DiscordSocketClient(new DiscordSocketConfig
                    {
                        GatewayIntents = Guilds | GuildBans | GuildEmojis | GuildMessages | GuildMessageReactions | DirectMessages, AlwaysDownloadUsers = true
                    }))
                    .AddTransient<ConsoleLogger>()
                    .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                    .AddSingleton<InteractionHandler>()
                    .AddSingleton(_ => new CommandService(new CommandServiceConfig
                    {
                        LogLevel = LogSeverity.Debug,
                        DefaultRunMode = Discord.Commands.RunMode.Async
                    }))
            ).Build();

        await RunAsync(host);
    }

    private async Task RunAsync(IHost host)
    {
        using var serviceScope = host.Services.CreateScope();
        var provider = serviceScope.ServiceProvider;

        var commands = provider.GetRequiredService<InteractionService>();
        _client = provider.GetRequiredService<DiscordSocketClient>();
        StaticConfig = GeneralUtility.BuildConfig("appsettings.json");

        await provider.GetRequiredService<InteractionHandler>().InitializeAsync();

        _client.Log += _ => provider.GetRequiredService<ConsoleLogger>().Log(_);
        commands.Log += _ => provider.GetRequiredService<ConsoleLogger>().Log(_);

        _client.UserLeft += async (guild, user) =>
        {
            MySqlUtility.RemoveUser(guild.Id, user.Id);
            var log = new ConsoleLogger();
            var getUser = MySqlUtility.GetUser(guild.Id, user.Id);
            var getRole = MySqlUtility.GetRole(guild.Id, getUser.CountryId);
            var getCountryToRemove = MySqlUtility.GetCountryById(getRole.CountryId);
            var doesRoleContainUsers = MySqlUtility.DoesRoleContainUsers(guild.Id, getUser.CountryId);
            if (!doesRoleContainUsers)
            {
                await guild.GetRole(getRole.RoleId).DeleteAsync();
                await log.Log(new LogMessage(LogSeverity.Info, "Bot",
                    $"Removing role {getCountryToRemove.Country} from {guild.Name} since it's no longer needed."));
                MySqlUtility.RemoveRole(guild.Id, getRole.RoleId);
            }

            var userCount = MySqlUtility.UserCount();
            await _client.SetGameAsync($"{userCount:##,###} users across the world!", null, ActivityType.Watching);
        };

        _client.Ready += async () =>
        {
            if (IsDebug())
                await commands.RegisterCommandsToGuildAsync(StaticConfig.DiscordModel.TestGuildId);
            else
                await commands.RegisterCommandsGloballyAsync();

            var numberOfUsers = MySqlUtility.UserCount();
            await _client.SetGameAsync($"{numberOfUsers:##,###} users across the world!", null, ActivityType.Watching);

            var dateTime = new DateTime(2000, 1, 1, 0, 0, 0)
                .AddDays(Assembly.GetEntryAssembly()!.GetName().Version!.Build)
                .AddSeconds(Assembly.GetEntryAssembly()!.GetName().Version!.Revision * 2).ToUniversalTime();
            var dateSince = DateTime.UtcNow - dateTime;
            var log = new ConsoleLogger();
            await log.Log(new LogMessage(LogSeverity.Info, "Bot",
                $"Version: {Assembly.GetEntryAssembly()!.GetName().Version!}"));
            await log.Log(new LogMessage(LogSeverity.Info, "Bot", $"Build Date: {dateTime}"));
            await log.Log(new LogMessage(LogSeverity.Info, "Bot",
                $"            ({GeneralUtility.ToReadableString(dateSince)} ago)"));
            await log.Log(new LogMessage(LogSeverity.Info, "Bot", $"Bot is in {_client.Guilds.Count} guilds."));
        };

        _client.LeftGuild += async guild =>
        {
            var log = new ConsoleLogger();
            MySqlUtility.RemoveAllRolesForGuild(guild.Id);
            MySqlUtility.RemoveAllUsersForGuild(guild.Id);
            var userCount = MySqlUtility.UserCount();
            await log.Log(new LogMessage(LogSeverity.Info, "Bot", $"Bot was removed from guild '{guild.Name}'."));
            await log.Log(new LogMessage(LogSeverity.Info, "Bot", $"Bot is now in {_client.Guilds.Count} guilds."));
            await _client.SetGameAsync($"{userCount:##,###} users across the world!", null, ActivityType.Watching);
        };

        _client.JoinedGuild += async guild =>
        {
            var log = new ConsoleLogger();
            await log.Log(new LogMessage(LogSeverity.Info, "Bot", $"Bot was added to guild '{guild.Name}'."));
            await log.Log(new LogMessage(LogSeverity.Info, "Bot", $"Bot is now in {_client.Guilds.Count} guilds."));
        };

        await _client.LoginAsync(TokenType.Bot, StaticConfig.DiscordModel.Token);
        await _client.StartAsync();
        await Task.Delay(-1);
    }
}