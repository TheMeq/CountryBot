using System;
using System.Linq;
using CountryBot.Logger;
using CountryBot.Utilities;
using Discord;
using Discord.Interactions;
using System.Threading.Tasks;
using CountryBot.Embeds;
using Discord.WebSocket;
using RequireUserPermissionAttribute = Discord.Interactions.RequireUserPermissionAttribute;

namespace CountryBot.Modules;

public class GeneralModule : InteractionModuleBase<SocketInteractionContext>
{
    private static ConsoleLogger _logger;
    private readonly DiscordSocketClient _client;

    private async Task Log(string ranCommand, string message = "", LogSeverity logSeverity = LogSeverity.Info, string source = "GeneralModule")
    {
        await _logger.Log(new LogMessage(logSeverity, source, $"[Guild: [red]{Context.Guild.Name}[/red]][User: [green]{Context.User.Username}[/green]][Command: [cyan]{ranCommand}[/cyan]]"));
        if(message != "") await _logger.Log(new LogMessage(logSeverity, source, $"    {message}"));
    }

    public GeneralModule(ConsoleLogger logger, DiscordSocketClient client)
    {
        _logger = logger;
        _client = client;
    }

    [SlashCommand("search", "Search for your country/region code.")]
    public async Task Search(string searchQuery)
    {
        await DeferAsync(ephemeral: true);
        await Log("search",$"Parameter: {searchQuery}");
        var searchResult = MySqlUtility.Search(searchQuery);
        var searchEmbed = searchResult.Count == 0 ? BotEmbeds.NoSearchResults(searchQuery) : BotEmbeds.SearchResults(searchResult, searchQuery);
        await FollowupAsync(embed: searchEmbed.Build(), ephemeral: true);
    }

    [SlashCommand("set", "Set your country/region role by specifying the country/region code.")]
    // ReSharper disable once MemberCanBePrivate.Global
    public async Task Set(string countryCode)
    {
        await DeferAsync(ephemeral: true);
        countryCode = countryCode.ToUpper();
        await Log("set", $"Parameter: {countryCode}");

        ulong guildId;
        try
        {
            guildId = Context.Guild.Id;
        }
        catch
        {
            await Log("set","Tried to DM the Bot.");
            var invalidGuildEmbed = BotEmbeds.NotInDms();
            await FollowupAsync(embed: invalidGuildEmbed.Build());
            return;
        }
        
        var isValidCountryCode = MySqlUtility.IsValidCountryCode(countryCode);
        if (!isValidCountryCode)
        {
            var searchResult = MySqlUtility.Search(countryCode);
            if (searchResult.Count > 1)
            {
                await Log("set", "Unable to set role as parameter is invalid or there is more then 1 match.");
                var errorEmbed = searchResult.Count >= 2
                    ? BotEmbeds.InvalidCountryCode(searchResult)
                    : BotEmbeds.InvalidCountryCode();
                await FollowupAsync(embed: errorEmbed.Build(), ephemeral: true);
                return;
            }

            if (searchResult.Count == 0)
            {
                await Log("set", "Unable to set role as parameter is invalid.");
                var errorEmbed = BotEmbeds.InvalidCountryCode();
                await FollowupAsync(embed: errorEmbed.Build(), ephemeral: true);
                return;
            }
           
            await Log("set", "Have found 1 closest match, using that instead.");
            countryCode = searchResult.First().Alpha2;
            await Log("set", $"Best Parameter: {countryCode}");
            
            
        }

        var getCountry = MySqlUtility.GetCountry(countryCode);

        try
        {
            var isUserInRoleAlready = MySqlUtility.IsUserInRoleAlready(guildId, Context.User.Id);
            if (isUserInRoleAlready)
            {
                
                var getUser = MySqlUtility.GetUser(guildId, Context.User.Id);
                var socketGuildUser = (SocketGuildUser) Context.User;
                var getRole = MySqlUtility.GetRole(guildId, getUser!.CountryId);
                var getCountryToRemove = MySqlUtility.GetCountryById(getRole.CountryId);
                await Log("set",$"Already in role {getCountryToRemove.Country}.");
                if (getCountryToRemove.Alpha2 == countryCode)
                {
                    await Log("set", "No need to change role as they are already in it.");
                    var errorEmbed = BotEmbeds.AlreadyInCountryCode(getCountryToRemove);
                    await FollowupAsync(embed: errorEmbed.Build(), ephemeral: true);
                    return;
                }

                await Log("set", "Removing from Role.");
                await socketGuildUser.RemoveRoleAsync(getRole.RoleId);
                MySqlUtility.RemoveUser(guildId, Context.User.Id);

                var doesRoleContainUsers = MySqlUtility.DoesRoleContainUsers(guildId, getUser.CountryId);
                if (!doesRoleContainUsers)
                {
                    var guildInfo = MySqlUtility.GetGuild(guildId);
                    if (guildInfo == null || guildInfo.RemoveOnEmpty == 1)
                    {
                        await Context.Guild.GetRole(getRole.RoleId).DeleteAsync();
                        await Log("set",
                            $"Removing role {getCountryToRemove.Country} from {Context.Guild.Name} since it's no longer needed.");
                        MySqlUtility.RemoveRole(guildId, getRole.RoleId);
                    }
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
                await Log("set",$"Adding new role to {Context.Guild.Name}...");
                var roleCount = Context.Guild.Roles.Count;
                if (roleCount >= 249)
                {
                    await Log(
                        "set", "Tried to create a role but guild has reached the role count limit.");
                    var roleCapReachedEmbed = BotEmbeds.RoleCapReached();
                    await FollowupAsync(embed: roleCapReachedEmbed.Build());
                    return;
                }

                var createdRole = await Context.Guild.CreateRoleAsync($"{getCountry.Country}", null, null, false, false,
                    RequestOptions.Default);
                await Log("set",$"Created role {createdRole.Name}.");
                if (Context.Guild.PremiumTier >= PremiumTier.Tier2)
                {
                    try
                    {
                        if (!MySqlUtility.GetFlagsDisabledForThisGuild(guildId))
                        {
                            await Log("set", $"Attempting to set Emoji to ':flag_{getCountry.Alpha2.ToLower()}:'");
                            await createdRole.ModifyAsync(x =>
                                x.Emoji = Emoji.Parse($":flag_{getCountry.Alpha2.ToLower()}:"), RequestOptions.Default);
                            await Log("set", $"Added Emoji for {getCountry.Country}");
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        await Log(
                            "set", $"Cannot add Emoji for {getCountry.Country} on this guild as it is not boosted.");
                    }
                    catch (Exception exception)
                    {
                        await Log("set", $"Cannot add Emoji for {getCountry.Country}");
                        Console.WriteLine(exception);
                    }
                }

                var socketGuildUser = (SocketGuildUser) Context.User;
                await socketGuildUser.AddRoleAsync(createdRole.Id);
                MySqlUtility.AddRole(guildId, createdRole.Id, getCountry.Id);
            }

            MySqlUtility.AddUser(guildId, Context.User.Id, getCountry.Id);
            await Log("set", $"Added role {getCountry.Country} to {Context.User.Username}.");
            var countrySetEmbed = BotEmbeds.CountrySet(getCountry);
            await FollowupAsync(embed: countrySetEmbed.Build(), ephemeral: true);
        }
        catch (Discord.Net.HttpException)
        {
            await Log("set", $"Unable to set role for {Context.User.Username} to {countryCode} - Bot is missing permissions.");
            var errorEmbed = BotEmbeds.MissingPermissions();
            await FollowupAsync(embed: errorEmbed.Build(), ephemeral: true);
        }
        
        var userCount = MySqlUtility.UserCount();
        await _client.SetGameAsync($"{userCount:##,###} users across the world!", null, ActivityType.Watching);        
    }

    [SlashCommand("remove", "Remove your currently set country.")]
    public async Task Remove()
    {
        await DeferAsync(ephemeral: true);
        ulong guildId;
        try
        {
            guildId = Context.Guild.Id;
        }
        catch
        {
            await Log("remove", "tried to DM the Bot.");
            var invalidGuildEmbed = BotEmbeds.NotInDms();
            await FollowupAsync(embed: invalidGuildEmbed.Build());
            return;
        }
        var isUserInRoleAlready = MySqlUtility.IsUserInRoleAlready(guildId, Context.User.Id);
        if (isUserInRoleAlready)
        {
            try
            {
                var getUser = MySqlUtility.GetUser(guildId, Context.User.Id);
                var socketGuildUser = (SocketGuildUser) Context.User;
                var getRole = MySqlUtility.GetRole(guildId, getUser!.CountryId);
                var getCountry = MySqlUtility.GetCountryById(getRole.CountryId);
                await socketGuildUser.RemoveRoleAsync(getRole.RoleId);
                await Log("remove", $"Removed from {getCountry.Country}");
                MySqlUtility.RemoveUser(guildId, Context.User.Id);

                var doesRoleContainUsers = MySqlUtility.DoesRoleContainUsers(guildId, getUser.CountryId);
                if (!doesRoleContainUsers)
                {
                    var guildInfo = MySqlUtility.GetGuild(guildId);
                    if (guildInfo == null || guildInfo.RemoveOnEmpty == 1)
                    {
                        await Context.Guild.GetRole(getRole.RoleId).DeleteAsync();
                        await Log("remove", $"Removing role {getCountry.Country} since it's no longer needed.");
                        MySqlUtility.RemoveRole(guildId, getRole.RoleId);
                    }
                }

                var countryRemovedEmbed = BotEmbeds.CountryRemoved();
                await FollowupAsync(embed: countryRemovedEmbed.Build(), ephemeral: true);
            }
            catch (Discord.Net.HttpException)
            {
                await Log("remove", $"Unable to remove role for {Context.User.Username} - Bot missing permissions");
                var errorEmbed = BotEmbeds.MissingPermissions();
                await FollowupAsync(embed: errorEmbed.Build(), ephemeral: true);
            }
        }
        else
        {
            var notInCountryEmbed = BotEmbeds.NotInCountry();
            await Log("remove", "Can't remove as not in a role yet.");
            await FollowupAsync(embed: notInCountryEmbed.Build(), ephemeral: true);
        }

        var userCount = MySqlUtility.UserCount();
        await _client.SetGameAsync($"{userCount:##,###} users across the world!", null, ActivityType.Watching);
    }

    [SlashCommand("help", "View help information about this bot.")]
    public async Task Help()
    {
        var isAdmin = false;
        var permissions = ((SocketGuildUser) Context.User).GuildPermissions;
        if (permissions.Has(GuildPermission.ManageRoles))
        {
            isAdmin = true;
        }
        
        await DeferAsync(ephemeral: true);
        await Log("help");
        var helpEmbed = BotEmbeds.Help(Program.StaticConfig.SupportUrl, isAdmin);
        await FollowupAsync(embed: helpEmbed.Build(), ephemeral: true);
    }

    [SlashCommand("stats", "Show how many users from around the world use the bot!")]
    public async Task Stats(bool worldWide = false)
    {
        await DeferAsync();
        await Log("stats", $"Parameter: {worldWide}");
        var stats = MySqlUtility.GetStats(Context.Guild.Id, worldWide);
        var maxPages = (int)Math.Ceiling((double)stats.Count() / 9);
        var guildName = worldWide ? "Worldwide" : Context.Guild.Name;
        var userCount = MySqlUtility.UserCount();
        var guildCount = _client.Guilds.Count;
        var guildUserCount = MySqlUtility.UserCount(Context.Guild.Id);
        var embed = BotEmbeds.NewStats(guildName, stats, 1, userCount, guildCount,guildUserCount);
        var components = new ComponentBuilder();
        if (maxPages > 1)
        {
            components.WithButton("Next", $"StatsNavigator:next,1,{worldWide}", ButtonStyle.Secondary);
        }
        await FollowupAsync(embed: embed.Build(), components: components.Build(), ephemeral: true);
    }

    [ComponentInteraction("StatsNavigator:*,*,*")]
    public async Task StatsAdditional(string direction, int currentPage, string worldWide)
    {
        switch (direction)
        {
            case "next":
                currentPage++;
                break;
            case "previous":
                currentPage--;
                break;
        }
        await DeferAsync(ephemeral: true);
        await Log("stats", $"Parameter: {worldWide}, {direction}, {currentPage}");
        var stats = MySqlUtility.GetStats(Context.Guild.Id, bool.Parse(worldWide));
        var maxPages = (int)Math.Ceiling((double)stats.Count() / 9);
        var guildName = bool.Parse(worldWide) ? "Worldwide" : Context.Guild.Name;
        var userCount = MySqlUtility.UserCount();
        var guildCount = _client.Guilds.Count;
        var guildUserCount = MySqlUtility.UserCount(Context.Guild.Id);
        var embed = BotEmbeds.NewStats(guildName, stats, currentPage, userCount, guildCount, guildUserCount);
        var components = new ComponentBuilder();
        if (currentPage > 1)
        {
            components.WithButton("Previous", $"StatsNavigator:previous,{currentPage},{worldWide}", ButtonStyle.Secondary);
        }
        if (maxPages > currentPage)
        {
            components.WithButton("Next", $"StatsNavigator:next,{currentPage},{worldWide}", ButtonStyle.Secondary);
        }


        await ModifyOriginalResponseAsync(x =>
        {
            x.Embed = embed.Build();
            x.Components = components.Build();
        });
    }

    [RequireUserPermission(ChannelPermission.ManageRoles)]
    [SlashCommand("choose", "Allows you to select your country/region role from a list.")]
    public async Task Choose()
    {
        await Log("choose");
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
        await FollowupAsync(embed: embed.Build(), components: component.Build());
    }

    [ComponentInteraction("countryLetterSelector")]
    public async Task CountryLetterSelector(string selectedValue)
    {
        await Log("choose", $"Parameter: {selectedValue}");
        await DeferAsync(ephemeral: true);
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
        await Log("choose", $"Parameter: {selectedValue}");
        await Set(selectedValue);
        
    }
}

