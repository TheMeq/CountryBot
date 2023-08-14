using CountryBot.Embeds;
using CountryBot.Logger;
using CountryBot.Utilities;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;

namespace CountryBot.Modules;

[RequireUserPermission(ChannelPermission.ManageRoles)]
[Group("admin","Admin commands to configure the CountryBot")]
public class AdminModule : InteractionModuleBase<SocketInteractionContext>
{
    private static ConsoleLogger _logger;
    private readonly DiscordSocketClient _client;

    private async Task Log(string ranCommand, string message , LogSeverity logSeverity = LogSeverity.Info, string source = "GeneralModule")
    {
        await _logger.Log(new LogMessage(logSeverity, source, $"[Guild: [red]{Context.Guild.Name} ({Context.Guild.Id})[/red]][User: [green]{Context.User.Username}#{Context.User.Discriminator} ({Context.User.Id})[/green]][Command: [cyan]{ranCommand}[/cyan]]"));
        if (message != "") await _logger.Log(new LogMessage(logSeverity, source, $"    {message}"));
    }

    public AdminModule(ConsoleLogger logger, DiscordSocketClient client)
    {
        _logger = logger;
        _client = client;
    }

    [SlashCommand("purge", "Removes all the roles and data created by the bot from your server.")]
    public async Task Purge()
    {
        await DeferAsync(ephemeral: true);
        ulong guildId;
        try
        {
            guildId = Context.Guild.Id;
        }
        catch
        {
            await Log("admin purge",$"Tried to DM the Bot.");
            var invalidGuildEmbed = BotEmbeds.NotInDms();
            await FollowupAsync(embed: invalidGuildEmbed.Build());
            return;
        }

        var allRolesForGuild = MySqlUtility.GetAllRolesForGuild(guildId);
        foreach (var roleInGuild in allRolesForGuild)
        {
            await Log("admin purge",$"Deleting [cyan]{roleInGuild.Id}[/cyan] in [red]{Context.Guild.Name}[/red]...");
            await Context.Guild.GetRole(roleInGuild.RoleId).DeleteAsync();
        }

        MySqlUtility.RemoveAllRolesForGuild(guildId);
        MySqlUtility.RemoveAllUsersForGuild(guildId);

        var purgeCompleteEmbed = BotEmbeds.PurgeComplete();
        await FollowupAsync(embed: purgeCompleteEmbed.Build(), ephemeral: true);
    }

    [SlashCommand("createdirectlyabove", "Specifies what position the bot should create the new roles above.")]
    public async Task CreateDirectlyAbove(IRole role)
    {
        await DeferAsync(ephemeral: true);
        ulong guildId;
        try
        {
            guildId = Context.Guild.Id;
        }
        catch
        {
            await Log("admin createdirectlyabove", "Tried to DM the Bot.");
            var invalidGuildEmbed = BotEmbeds.NotInDms();
            await FollowupAsync(embed: invalidGuildEmbed.Build(), ephemeral: true);
            return;
        }
        await Log("admin createdirectlyabove", $"All new roles will now appear above the role with ID {role.Id}");
        MySqlUtility.AddDirectlyUnderRole(guildId, role.Id);
        await FollowupAsync(embed: BotEmbeds.CreateDirectlyBelowComplete().Build(), ephemeral: true);
    }

    [SlashCommand("removedirectlyabove", "Resets the setting that specifies what position the bot should create the new roles above.")]
    public async Task RemoveDirectlyBelow()
    {
        await DeferAsync(ephemeral: true);
        ulong guildId;
        try
        {
            guildId = Context.Guild.Id;
        }
        catch
        {
            await Log("admin removedirectlyabove", "Tried to DM the Bot.");
            var invalidGuildEmbed = BotEmbeds.NotInDms();
            await FollowupAsync(embed: invalidGuildEmbed.Build(), ephemeral: true);
            return;
        }
        await Log("admin removedirectlyabove", $"All new roles will now appear at the bottom of the role list");
        MySqlUtility.AddDirectlyUnderRole(guildId, 0);
        await FollowupAsync(embed: BotEmbeds.RemoveDirectlyBelowComplete().Build(), ephemeral: true);
    }

    [SlashCommand("override", "Overrides the bot's role system. Converts existing server roles to work with the bot.")]
    public async Task Override(IRole role, string alpha2)
    {
        var countryCode = MySqlUtility.GetCountry(alpha2);
        await DeferAsync(ephemeral: true);
        ulong guildId;
        try
        {
            guildId = Context.Guild.Id;
        }
        catch
        {
            await Log("admin override","Tried to DM the Bot.");
            var invalidGuildEmbed = BotEmbeds.NotInDms();
            await FollowupAsync(embed: invalidGuildEmbed.Build(), ephemeral: true);
            return;
        }

        if (Context.User.Id != 207949234106793984)
        {
            await Log("admin override","Tried to use a developer command.");
            var notDeveloperEmbed = BotEmbeds.NotDeveloper();
            await FollowupAsync(embed: notDeveloperEmbed.Build(), ephemeral: true);
            return;
        }

        await Log("admin override",$"Manually adding [cyan]{countryCode.Country}[/cyan] to [red]{Context.Guild.Name}[/red]");
        MySqlUtility.AddRole(guildId, role.Id, countryCode.Id);
        await FollowupAsync(embed: BotEmbeds.OverrideComplete().Build(), ephemeral: true);
    }

    [SlashCommand("adduser", "Overrides the bot's role system. Converts existing server roles to work with the bot.")]
    public async Task AddUser(IUser user, string alpha2)
    {

        var country = MySqlUtility.GetCountry(alpha2);
        await DeferAsync(ephemeral: true);
        ulong guildId;
        try
        {
            guildId = Context.Guild.Id;
        }
        catch
        {
            await Log("admin adduser","Tried to DM the Bot.");
            var invalidGuildEmbed = BotEmbeds.NotInDms();
            await FollowupAsync(embed: invalidGuildEmbed.Build(), ephemeral: true);
            return;
        }

        if (Context.User.Id != 207949234106793984)
        {
            await Log("admin adduser","Tried to use a developer command.");
            var notDeveloperEmbed = BotEmbeds.NotDeveloper();
            await FollowupAsync(embed: notDeveloperEmbed.Build(), ephemeral: true);
            return;
        }

        await Log("admin adduser",$"Manually adding [green]{user.Username}[/green] to [cyan]{country.Country}[/cyan] in [red]{Context.Guild.Name}[/red]");
        MySqlUtility.AddUser(guildId, user.Id, country.Id);
        await FollowupAsync(embed: BotEmbeds.AddUserComplete().Build(), ephemeral: true);
        var userCount = MySqlUtility.UserCount();
        await _client.SetGameAsync($"{userCount:##,###} users across the world!", null, ActivityType.Watching);
    }


    [SlashCommand("flags", "Choose whether your roles should have flags or not. This only works if your guild is server boosted.")]
    [Throttle(ThrottleBy.Guild, 1, 21600)]
    public async Task Flags(bool enableFlags)
    {
        await Log("admin flags",$"Parameter: [yellow]{enableFlags}[/yellow]");
        await DeferAsync(ephemeral: true);
        if (Context.Guild.PremiumTier >= PremiumTier.Tier2)
        {
            ulong guildId;
            try
            {
                guildId = Context.Guild.Id;
            }
            catch
            {
                await Log("admin flags", "Tried to DM the Bot.");
                var invalidGuildEmbed = BotEmbeds.NotInDms();
                await FollowupAsync(embed: invalidGuildEmbed.Build(), ephemeral: true);
                return;
            }

            var allRolesForGuild = MySqlUtility.GetAllRolesForGuild(guildId);
            if (enableFlags)
            {
                await Log("admin flags", "Adding emoji's...");
            }
            else
            {
                await Log("admin flags", "Removing emoji's...");
            }

            foreach (var roleInGuild in allRolesForGuild)
            {
                if (enableFlags)
                {
                    var getCountry = MySqlUtility.GetCountryById(roleInGuild.CountryId);
                    var roleToModify = Context.Guild.GetRole(roleInGuild.RoleId);
                    await roleToModify.ModifyAsync(
                        x => x.Emoji = Emoji.Parse($":flag_{getCountry.Alpha2.ToLower()}:"),
                        RequestOptions.Default);
                }
                else
                {
                    var roleToModify = Context.Guild.GetRole(roleInGuild.RoleId);
                    await roleToModify.ModifyAsync(x => x.Emoji = null, RequestOptions.Default);
                }
            }

            MySqlUtility.SetGuildFlag(guildId, enableFlags ? 1 : 0);
            var flagChangeCompleteEmbed = BotEmbeds.FlagChangeComplete(enableFlags);
            await FollowupAsync(embed: flagChangeCompleteEmbed.Build(), ephemeral: true);
        }
        else
        {
            var flagChangeFailedEmbed = BotEmbeds.FlagChangeFailed();
            await FollowupAsync(embed: flagChangeFailedEmbed.Build(), ephemeral: true);
        }
    }

    [SlashCommand("removeonempty", "Choose whether your roles should be removed when the last user leaves.")]
    public async Task RemoveOnEmpty(bool enableRemoveOnEmpty)
    {

        await Log("admin removeonempty", $"Parameter: [yellow]{enableRemoveOnEmpty}[/yellow]");
        await DeferAsync(ephemeral: true);

        ulong guildId;
        try
        {
            guildId = Context.Guild.Id;
        }
        catch
        {
            await Log("admin removeonempty","Tried to DM the Bot.");
            var invalidGuildEmbed = BotEmbeds.NotInDms();
            await FollowupAsync(embed: invalidGuildEmbed.Build(), ephemeral: true);
            return;
        }

        MySqlUtility.SetGuildRemoveOnEmpty(guildId, enableRemoveOnEmpty ? 1 : 0);
        var removeOnEmptyChangeCompleteEmbed = BotEmbeds.RemoveOnEmptyChangeComplete(enableRemoveOnEmpty);
        await FollowupAsync(embed: removeOnEmptyChangeCompleteEmbed.Build(), ephemeral: true);


    }

    [SlashCommand("purgeempty", "Removes all empty CountryBot roles on the guild.")]
    public async Task PurgeEmpty()
    {

        await Log("admin purgeempty", $"Parameter: none");
        await DeferAsync(ephemeral: true);

        ulong guildId;
        try
        {
            guildId = Context.Guild.Id;
        }
        catch
        {
            await Log("admin purgeempty", "Tried to DM the Bot.");
            var invalidGuildEmbed = BotEmbeds.NotInDms();
            await FollowupAsync(embed: invalidGuildEmbed.Build(), ephemeral: true);
            return;
        }

        var roles = Context.Guild.Roles;
        var countries = MySqlUtility.GetCountries();

        foreach (var role in roles)
        {
            var roleUsers = Context.Guild.GetRole(role.Id).Members.ToList();

            if (roleUsers.Count != 0) continue;
            if (countries.All(x => x.Country != role.Name)) continue;

            var country = countries.First(x => x.Country == role.Name);
            var count = MySqlUtility.DoesRoleContainUsers(guildId, country.Id);

            if (count) continue;

            MySqlUtility.RemoveRole(guildId, role.Id);
            await Log("admin purgeempty",
                $"Removing [red]{role.Name}[/red] from [red]{Context.Guild.Name}[/red]");
            await role.DeleteAsync();

        }

        var purgeEmptyEmbed = BotEmbeds.PurgeEmpty();
        await FollowupAsync(embed: purgeEmptyEmbed.Build(), ephemeral: true);


    }
}