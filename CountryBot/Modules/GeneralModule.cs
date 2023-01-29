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
        await Log($"{Context.User.Username} used the Search command in {Context.Guild.Name}");
        await Log($"{Context.User.Username} used the Parameter {searchQuery}");
        var searchResult = MySqlUtility.Search(searchQuery);
        var searchEmbed = searchResult.Count == 0 ? BotEmbeds.NoSearchResults(searchQuery) : BotEmbeds.SearchResults(searchResult, searchQuery);
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

        await Log($"{Context.User.Username} used the Set command in {Context.Guild.Name}");
        await Log($"{Context.User.Username} used the Parameter {countryCode}");

        var isValidCountryCode = MySqlUtility.IsValidCountryCode(countryCode);
        if (!isValidCountryCode)
        {
            var searchResult = MySqlUtility.Search(countryCode);
            if (searchResult.Count > 1)
            {
                await Log($"Unable to set role for {Context.User.Username} to {countryCode} as it is invalid or there is more then 1 match.");
                var errorEmbed = searchResult.Count >= 2
                    ? BotEmbeds.InvalidCountryCode(searchResult)
                    : BotEmbeds.InvalidCountryCode();
                await RespondAsync(embed: errorEmbed.Build(), ephemeral: true);
                return;
            }
            await Log($"Unable to set role for {Context.User.Username} to {countryCode} but have found 1 closest match, using that instead.");
            countryCode = searchResult.First().Alpha2;
            await Log($"{Context.User.Username} used the Parameter {countryCode}");
        }

        var getCountry = MySqlUtility.GetCountry(countryCode);

        try
        {
            var isUserInRoleAlready = MySqlUtility.IsUserInRoleAlready(guildId, Context.User.Id);
            if (isUserInRoleAlready)
            {
                await Log($"{Context.User.Username} is already in a role. Removing current role from user...");
                var getUser = MySqlUtility.GetUser(guildId, Context.User.Id);
                var socketGuildUser = (SocketGuildUser) Context.User;
                var getRole = MySqlUtility.GetRole(guildId, getUser.CountryId);
                await socketGuildUser.RemoveRoleAsync(getRole.RoleId);
                MySqlUtility.RemoveUser(guildId, Context.User.Id);

                var doesRoleContainUsers = MySqlUtility.DoesRoleContainUsers(guildId, getUser.CountryId);
                if (!doesRoleContainUsers)
                {
                    await Log($"Removing unused role from {Context.Guild.Name}...");
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
                await Log($"Adding new role to {Context.Guild.Name}...");
                var roleCount = Context.Guild.Roles.Count;
                if (roleCount >= 249)
                {
                    await Log(
                        $"Tried to create a role in {Context.Guild.Name} but it has reached the Role count limit.");
                    var roleCapReachedEmbed = BotEmbeds.RoleCapReached();
                    await RespondAsync(embed: roleCapReachedEmbed.Build());
                    return;
                }

                var createdRole = await Context.Guild.CreateRoleAsync($"{getCountry.Country}", null, null, false, false,
                    RequestOptions.Default);
                await Log($"Created role {createdRole.Name} in {Context.Guild.Name}");
                if (Context.Guild.PremiumTier >= PremiumTier.Tier2)
                {
                    try
                    {
                        if (!MySqlUtility.GetFlagsDisabledForThisGuild(guildId))
                        {
                            await Log($"Attempting to set Emoji to ':flag_{getCountry.Alpha2.ToLower()}:'");
                            await createdRole.ModifyAsync(x =>
                                x.Emoji = Emoji.Parse($":flag_{getCountry.Alpha2.ToLower()}:"), RequestOptions.Default);
                            await Log($"Added Emoji for {getCountry.Country}");
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        await Log(
                            $"Cannot add Emoji for {getCountry.Country} on this guild as it is not boosted.");
                    }
                    catch (Exception exception)
                    {
                        await Log($"Cannot add Emoji for {getCountry.Country}");
                        Console.WriteLine(exception);
                    }
                }

                var socketGuildUser = (SocketGuildUser) Context.User;
                await socketGuildUser.AddRoleAsync(createdRole.Id);
                MySqlUtility.AddRole(guildId, createdRole.Id, getCountry.Id);
            }

            MySqlUtility.AddUser(guildId, Context.User.Id, getCountry.Id);
            var countrySetEmbed = BotEmbeds.CountrySet(getCountry);
            await RespondAsync(embed: countrySetEmbed.Build(), ephemeral: true);
        }
        catch (Discord.Net.HttpException)
        {
            await Log($"Unable to set role for {Context.User.Username} to {countryCode} in {Context.Guild.Name} - Missing Permissions");
            var errorEmbed = BotEmbeds.MissingPermissions();
            await RespondAsync(embed: errorEmbed.Build(), ephemeral: true);
        }
        
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
        await Log($"{Context.User.Username} used the Remove command in {Context.Guild.Name}");
        var isUserInRoleAlready = MySqlUtility.IsUserInRoleAlready(guildId, Context.User.Id);
        if (isUserInRoleAlready)
        {
            try
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

                var countryRemovedEmbed = BotEmbeds.CountryRemoved();
                await RespondAsync(embed: countryRemovedEmbed.Build(), ephemeral: true);
            }
            catch (Discord.Net.HttpException)
            {
                await Log($"Unable to remove role for {Context.User.Username} in {Context.Guild.Name} - Missing Permissions");
                var errorEmbed = BotEmbeds.MissingPermissions();
                await RespondAsync(embed: errorEmbed.Build(), ephemeral: true);
            }
        }
        else
        {
            var notInCountryEmbed = BotEmbeds.NotInCountry();
            await RespondAsync(embed: notInCountryEmbed.Build(), ephemeral: true);
        }
        
        
        var userCount = MySqlUtility.UserCount();
        await _client.SetGameAsync($"{userCount:##,###} users across the world!", null, ActivityType.Watching);
    }

    [SlashCommand("help", "View help information about this bot.")]
    public async Task Help()
    {
        await Log($"{Context.User.Username} used the Help command in {Context.Guild.Name}");
        var helpEmbed = BotEmbeds.Help();
        await RespondAsync(embed: helpEmbed.Build(), ephemeral: true);
    }

    [SlashCommand("stats", "Show how many users from around the world use the bot!")]
    public async Task Stats(bool worldWide = false)
    {
        await Log($"{Context.User.Username} used the Stats command in {Context.Guild.Name}");
        await Log($"{Context.User.Username} used the Parameter {worldWide}");
        var stats = MySqlUtility.GetStats(Context.Guild.Id, worldWide);
        var statsEmbed = BotEmbeds.Stats(Context.Guild.Name, stats, worldWide);
        await RespondAsync(embed: statsEmbed.Build(), ephemeral: true);
    }

    [RequireUserPermission(ChannelPermission.ManageRoles)]
    [SlashCommand("choose", "Allows you to select your country/region role from a list.")]
    public async Task Choose()
    {
        await Log($"{Context.User.Username} used the Choose command in {Context.Guild.Name}");
        await DeferAsync();
        var embed = BotEmbeds.CountryLetterSelector();
        var component = new ComponentBuilder();
        var countryList = new SelectMenuBuilder
        {
            CustomId = "countryLetterSelector",
            IsDisabled = false,
            MinValues = 1,
            MaxValues = 1,
            Placeholder = "Select what letter your country/region starts with."
        };
        const string countryLetters = "ABCDEFGHIJKLMNOPQRSTUVWYZ";
        foreach (var letter in countryLetters)
        {
            countryList.AddOption(letter.ToString(), letter.ToString(), $"List countries/regions beginning with '{letter}'");
        }

        component.WithSelectMenu(countryList);
        await FollowupAsync(embed: embed.Build(), components: component.Build(), ephemeral: true);
    }

    [ComponentInteraction("countryLetterSelector")]
    public async Task CountryLetterSelector(string selectedValue)
    {
        await Log($"{Context.User.Username} selected {selectedValue} from the Choose command in {Context.Guild.Name}");
        await DeferAsync();
        var embed = BotEmbeds.CountrySelector();
        var component = new ComponentBuilder();
        var countries = MySqlUtility.GetCountries(selectedValue);
        var countryList = new SelectMenuBuilder
        {
            CustomId = "countrySelector",
            IsDisabled = false,
            MinValues = 1,
            MaxValues = 1,
            Placeholder = "Select your country/region."
        };
        foreach (var country in countries)
        {
            countryList.AddOption(country.Country, country.Alpha2, $"Select {country.Country}", Emoji.Parse(":flag_" + country.Alpha2.ToLower() + ":"));
        }

        component.WithSelectMenu(countryList);
        await FollowupAsync(embed: embed.Build(), components: component.Build(), ephemeral: true);
    }

    [ComponentInteraction("countrySelector")]
    public async Task CountrySelector(string selectedValue)
    {
        await Log($"{Context.User.Username} selected {selectedValue} from the Choose command in {Context.Guild.Name}");
        await Set(selectedValue);
        
    }
}

