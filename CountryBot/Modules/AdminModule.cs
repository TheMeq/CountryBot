using CountryBot.Embeds;
using CountryBot.Logger;
using CountryBot.Utilities;
using Discord;
using Discord.Interactions;
using System.Threading.Tasks;

namespace CountryBot.Modules;

[RequireUserPermission(ChannelPermission.ManageRoles)]
[Group("admin","Admin commands to configure the CountryBot")]
public class AdminModule : InteractionModuleBase<SocketInteractionContext>
{
    private static ConsoleLogger _logger;

    private async Task Log(string ranCommand, LogSeverity logSeverity = LogSeverity.Info)
    {
        await _logger.Log(new LogMessage(logSeverity, "GeneralModule", $"User: {Context.User.Username} - Command: {ranCommand}"));
    }

    public AdminModule(ConsoleLogger logger)
    {
        _logger = logger;
    }

    [SlashCommand("purge", "Removes all the roles and data created by the bot from your server.")]
    public async Task Purge()
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

        var allRolesForGuild = MySqlUtility.GetAllRolesForGuild(guildId);
        foreach (var roleInGuild in allRolesForGuild)
        {
            await Context.Guild.GetRole(roleInGuild.RoleId).DeleteAsync();
        }

        MySqlUtility.RemoveAllRolesForGuild(guildId);
        MySqlUtility.RemoveAllUsersForGuild(guildId);

        var purgeCompleteEmbed = BotEmbeds.PurgeComplete();
        await RespondAsync(embed: purgeCompleteEmbed.Build(), ephemeral: true);
    }

    [SlashCommand("flags", "Choose whether your roles should have flags or not. This only works if your guild is server boosted.")]
    public async Task Flags(bool enableFlags)
    {
        if (Context.Guild.PremiumTier >= PremiumTier.Tier2)
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

            var allRolesForGuild = MySqlUtility.GetAllRolesForGuild(guildId);
            foreach (var roleInGuild in allRolesForGuild)
            {
                if (enableFlags)
                {
                    var getCountry = MySqlUtility.GetCountryById(roleInGuild.CountryId);
                    await Context.Guild.GetRole(roleInGuild.RoleId).ModifyAsync(x =>
                        x.Emoji = Emoji.Parse($":flag_{getCountry.Alpha2.ToLower()}:"));

                }
                else
                {
                    await Context.Guild.GetRole(roleInGuild.RoleId)
                        .ModifyAsync(x => x.Emoji = Optional<Emoji>.Unspecified);
                }
            }

            MySqlUtility.SetGuildFlag(guildId, enableFlags ? 1 : 0);
            var flagChangeCompleteEmbed = BotEmbeds.FlagChangeComplete(enableFlags);
            await RespondAsync(embed: flagChangeCompleteEmbed.Build(), ephemeral: true);
        }
        else
        {
            var flagChangeFailedEmbed = BotEmbeds.FlagChangeFailed();
            await RespondAsync(embed: flagChangeFailedEmbed.Build(), ephemeral: true);
        }
    }


}