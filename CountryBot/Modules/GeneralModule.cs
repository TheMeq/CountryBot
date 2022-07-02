using CountryBot.Logger;
using CountryBot.Utilities;
using Discord;
using Discord.Interactions;
using System.Threading.Tasks;
using CountryBot.Embeds;
using Discord.WebSocket;

namespace CountryBot.Modules;

public class GeneralModule : InteractionModuleBase<SocketInteractionContext>
{
    private static ConsoleLogger _logger;
    private readonly DiscordSocketClient _client;

    private async Task Log(string command, LogSeverity logSeverity = LogSeverity.Info)
    {
        await _logger.Log(
            new LogMessage(logSeverity, "GeneralModule", $"User: {Context.User.Username} - Command: {command}"));
    }

    public GeneralModule(ConsoleLogger logger, DiscordSocketClient client)
    {
        _logger = logger;
        _client = client;
    }

    [SlashCommand("search", "Search for your countries code.")]
    public async Task Search(string query)
    {
        await Log($"Search for {query}");
        var result = MySqlUtility.Search(query);

        var embed = BotEmbeds.SearchResults(result, query);
        await RespondAsync(embed: embed.Build(), ephemeral: true);
    }

    [SlashCommand("set", "Set's your country using the country code.")]
    public async Task Set(string countryCode)
    {
        var guildId = Context.Guild.Id;
        await Log($"Set __ to {countryCode}");

        // Check if countrycode is valid
        var result = MySqlUtility.IsValidCountryCode(countryCode);
        if (!result)
        {
            var errorEmbed = BotEmbeds.InvalidCountryCode();
            await RespondAsync(embed: errorEmbed.Build(), ephemeral: true);
            return;
        }

        var country = MySqlUtility.GetCountry(countryCode);

        var alreadyInRole = MySqlUtility.IsUserInRoleAlready(guildId, Context.User.Id);
        if (alreadyInRole)
        {
            var userInfo = MySqlUtility.GetUser(guildId, Context.User.Id);
            var user = (SocketGuildUser) Context.User;
            var roleInfo = MySqlUtility.GetRole(guildId, userInfo.CountryId);
            await user.RemoveRoleAsync(roleInfo.RoleId);
            MySqlUtility.RemoveUser(guildId, Context.User.Id);

            var roleRequired = MySqlUtility.DoesRoleContainUsers(guildId, userInfo.CountryId);
            if (!roleRequired)
            {
                await Context.Guild.GetRole(roleInfo.RoleId).DeleteAsync();
                MySqlUtility.RemoveRole(guildId, roleInfo.RoleId);
            }
        }

        var doesRoleExist = MySqlUtility.DoesRoleExist(guildId, country.Id);
        if (doesRoleExist)
        {
            var roleInfo = MySqlUtility.GetRole(guildId, country.Id);
            var user = (SocketGuildUser) Context.User;
            await user.AddRoleAsync(roleInfo.RoleId);

        }
        else
        {
            var role = await Context.Guild.CreateRoleAsync($"{country.Country}", null, null, false, false,
                RequestOptions.Default);
            var user = (SocketGuildUser) Context.User;
            await user.AddRoleAsync(role.Id);
            MySqlUtility.AddRole(guildId, role.Id, country.Id);
        }

        MySqlUtility.AddUser(guildId, Context.User.Id, country.Id);
        var doneEmbed = BotEmbeds.CountrySet(country);
        await RespondAsync(embed: doneEmbed.Build(), ephemeral: true);
        var numberOfUsers = MySqlUtility.UserCount();
        await _client.SetGameAsync($"{numberOfUsers:##,###} users across the world!", null, ActivityType.Watching);

    }

    [SlashCommand("remove", "Remove your currently set country.")]
    public async Task Remove()
    {
        var guildId = Context.Guild.Id;
        await Log($"Remove __");
        var alreadyInRole = MySqlUtility.IsUserInRoleAlready(guildId, Context.User.Id);
        if (alreadyInRole)
        {
            var userInfo = MySqlUtility.GetUser(guildId, Context.User.Id);
            var user = (SocketGuildUser) Context.User;
            var roleInfo = MySqlUtility.GetRole(guildId, userInfo.CountryId);
            await user.RemoveRoleAsync(roleInfo.RoleId);
            MySqlUtility.RemoveUser(guildId, Context.User.Id);

            var roleRequired = MySqlUtility.DoesRoleContainUsers(guildId, userInfo.CountryId);
            if (!roleRequired)
            {
                await Context.Guild.GetRole(roleInfo.RoleId).DeleteAsync();
                MySqlUtility.RemoveRole(guildId, roleInfo.RoleId);
            }
        }

        var removeEmbed = BotEmbeds.CountryRemoved();
        await RespondAsync(embed: removeEmbed.Build(), ephemeral: true);
        var numberOfUsers = MySqlUtility.UserCount();
        await _client.SetGameAsync($"{numberOfUsers:##,###} users across the world!", null, ActivityType.Watching);

    }
}

