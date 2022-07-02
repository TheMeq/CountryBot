using System;
using System.Linq;
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

    private async Task Log(string ranCommand, LogSeverity logSeverity = LogSeverity.Info)
    {
        await _logger.Log(new LogMessage(logSeverity, "GeneralModule", $"User: {Context.User.Username} - Command: {ranCommand}"));
    }

    public GeneralModule(ConsoleLogger logger, DiscordSocketClient client)
    {
        _logger = logger;
        _client = client;
    }

    [SlashCommand("search", "Search for your country code.")]
    public async Task Search(string searchQuery)
    {
        await Log($"Search for {searchQuery}");
        var searchResult = MySqlUtility.Search(searchQuery);
        var searchEmbed = BotEmbeds.SearchResults(searchResult, searchQuery);
        await RespondAsync(embed: searchEmbed.Build(), ephemeral: true);
    }

    [SlashCommand("set", "Set your country role by specifying the country code.")]
    public async Task Set(string countryCode)
    {
        countryCode = countryCode.ToUpper();
        ulong guildId;
        try
        {
            guildId = Context.Guild.Id;
        }
        catch
        {
            await Log($"{Context.User.Username} tried to DM the Bot.");
            var invalidGuildEmbed = BotEmbeds.NotInDms();
            await RespondAsync(embed: invalidGuildEmbed.Build());
            return;
        }

        await Log($"Set {Context.User.Username} to {countryCode} in {Context.Guild.Name}");

        var isValidCountryCode = MySqlUtility.IsValidCountryCode(countryCode);
        if (!isValidCountryCode)
        {
            var searchResult = MySqlUtility.Search(countryCode).FirstOrDefault();
            var errorEmbed = searchResult != null ? BotEmbeds.InvalidCountryCode(searchResult) : BotEmbeds.InvalidCountryCode();
            await RespondAsync(embed: errorEmbed.Build(), ephemeral: true);
            return;
        }

        var getCountry = MySqlUtility.GetCountry(countryCode);

        var isUserInRoleAlready = MySqlUtility.IsUserInRoleAlready(guildId, Context.User.Id);
        if (isUserInRoleAlready)
        {
            var getUser = MySqlUtility.GetUser(guildId, Context.User.Id);
            var socketGuildUser = (SocketGuildUser) Context.User;
            var getRole = MySqlUtility.GetRole(guildId, getUser.CountryId);
            await socketGuildUser.RemoveRoleAsync(getRole.RoleId);
            MySqlUtility.RemoveUser(guildId, Context.User.Id);

            var doesRoleContainUsers = MySqlUtility.DoesRoleContainUsers(guildId, getUser.CountryId);
            if (!doesRoleContainUsers)
            {
                await Context.Guild.GetRole(getRole.RoleId).DeleteAsync();
                MySqlUtility.RemoveRole(guildId, getRole.RoleId);
            }
        }

        var doesRoleExist = MySqlUtility.DoesRoleExist(guildId, getCountry.Id);
        if (doesRoleExist)
        {
            var getRole = MySqlUtility.GetRole(guildId, getCountry.Id);
            var socketGuildUser = (SocketGuildUser) Context.User;
            await socketGuildUser.AddRoleAsync(getRole.RoleId);
        }
        else
        {
            var createdRole = await Context.Guild.CreateRoleAsync($"{getCountry.Country}", null, null, false, false, RequestOptions.Default);
            try
            {
                Console.WriteLine($"Attempting to set Emoji to ':flag_{getCountry.Alpha2.ToLower()}:'");
                await createdRole.ModifyAsync(x => x.Emoji = Emoji.Parse($":flag_{getCountry.Alpha2.ToLower()}:"));
                Console.WriteLine($"Added Emoji for {getCountry.Country}");
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine($"Cannot add Emoji for {getCountry.Country} on this guild as it is not boosted.");
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Cannot add Emoji for {getCountry.Country}");
                Console.WriteLine(exception);
            }
            
            var socketGuildUser = (SocketGuildUser) Context.User;
            await socketGuildUser.AddRoleAsync(createdRole.Id);
            MySqlUtility.AddRole(guildId, createdRole.Id, getCountry.Id);
        }

        MySqlUtility.AddUser(guildId, Context.User.Id, getCountry.Id);
        var countrySetEmbed = BotEmbeds.CountrySet(getCountry);
        await RespondAsync(embed: countrySetEmbed.Build(), ephemeral: true);
        var userCount = MySqlUtility.UserCount();
        await _client.SetGameAsync($"{userCount:##,###} users across the world!", null, ActivityType.Watching);
    }

    [SlashCommand("remove", "Remove your currently set country.")]
    public async Task Remove()
    {
        ulong guildId;
        try
        {
            guildId = Context.Guild.Id;
        }
        catch
        {
            await Log($"{Context.User.Username} tried to DM the Bot.");
            var invalidGuildEmbed = BotEmbeds.NotInDms();
            await RespondAsync(embed: invalidGuildEmbed.Build());
            return;
        }
        await Log($"Remove {Context.User.Username} in {Context.Guild.Name}");
        var isUserInRoleAlready = MySqlUtility.IsUserInRoleAlready(guildId, Context.User.Id);
        if (isUserInRoleAlready)
        {
            var getUser = MySqlUtility.GetUser(guildId, Context.User.Id);
            var socketGuildUser = (SocketGuildUser) Context.User;
            var getRole = MySqlUtility.GetRole(guildId, getUser.CountryId);
            await socketGuildUser.RemoveRoleAsync(getRole.RoleId);
            MySqlUtility.RemoveUser(guildId, Context.User.Id);

            var doesRoleContainUsers = MySqlUtility.DoesRoleContainUsers(guildId, getUser.CountryId);
            if (!doesRoleContainUsers)
            {
                await Context.Guild.GetRole(getRole.RoleId).DeleteAsync();
                MySqlUtility.RemoveRole(guildId, getRole.RoleId);
            }
        }

        var countryRemovedEmbed = BotEmbeds.CountryRemoved();
        await RespondAsync(embed: countryRemovedEmbed.Build(), ephemeral: true);
        var userCount = MySqlUtility.UserCount();
        await _client.SetGameAsync($"{userCount:##,###} users across the world!", null, ActivityType.Watching);
    }
}

