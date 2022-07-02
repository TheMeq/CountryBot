using System.Runtime.InteropServices;
using CountryBot.Embeds;
using CountryBot.Logger;
using CountryBot.Models;
using CountryBot.Utilities;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace CountryBot.Modules;

[RequireUserPermission(ChannelPermission.ManageRoles)]
[Group("admin","Admin commands to configure the CountryBot")]
public class AdminModule : InteractionModuleBase<SocketInteractionContext>
{
    private static ConsoleLogger _logger;
    private readonly DiscordSocketClient _client;

    private async Task Log(string ranCommand, LogSeverity logSeverity = LogSeverity.Info)
    {
        await _logger.Log(new LogMessage(logSeverity, "GeneralModule", $"User: {Context.User.Username} - Command: {ranCommand}"));
    }

    public AdminModule(ConsoleLogger logger, DiscordSocketClient client)
    {
        _logger = logger;
        _client = client;
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

    [SlashCommand("flags", "Choose whether your roles should have flags or not.")]
    public async Task Flags(bool enableFlags)
    {

    }


}