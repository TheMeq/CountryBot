using System;
using System.Threading.Tasks;
using CountryBot.Models;
using Discord.Interactions;
using Discord.WebSocket;

namespace CountryBot.Modules;

internal class CustomModule
{
    public static async Task SetNickname(SocketInteractionContext context, CountryModel getCountry, SocketGuildUser socketGuildUser)
    {
        try
        {
            await socketGuildUser.ModifyAsync(x => x.Nickname = getCountry.Emoji);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

    }

    public static async Task RemoveNickname(SocketInteractionContext context, CountryModel getCountry, SocketGuildUser socketGuildUser)
    {
        try
        {
            await socketGuildUser.ModifyAsync(x => x.Nickname = null);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

    }
}