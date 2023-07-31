using System;
using System.Collections.Generic;
using System.Linq;
using CountryBot.Models;
using CountryBot.Utilities;
using Discord;
using Discord.WebSocket;

namespace CountryBot.Embeds;

internal static class BotEmbeds
{
    private static readonly Color DiscordGreen = new(59, 165, 93);
    private static readonly Color DiscordYellow = new(250, 168, 26);
    private static readonly Color DiscordRed = new(237, 66, 59);

    public static EmbedBuilder SearchResults(List<CountryModel> listOfCountries, string searchQuery)
    {
        var searchResults = listOfCountries.Take(10).ToList();
        var searchResultCount = listOfCountries.Count();

        var embedBuilder = new EmbedBuilder()
            .WithTitle($"Search Results for '{searchQuery}'.")
            .WithDescription(searchResults
                .Aggregate(string.Empty,
                    (current, country) =>
                        current + $"{country.Country} - ``{country.Alpha2}`` or ``{country.Alpha3}``\r\n"))
            .WithColor(DiscordGreen);

        if (searchResultCount > 10)
        {
            embedBuilder.Description += $"**Note:** There are more than 10 results. Please refine your search criteria.";
        }

        embedBuilder.Description =
            "Use these with the ``/set`` command.\r\nAlternatively, use the ``/choose`` command to create an easy-to-use selector.\r\n\r\n" +
            embedBuilder.Description;

        return embedBuilder;
    }

    public static EmbedBuilder InvalidCountryCode(List<CountryModel> tryThis = null)
    {
        var embed = new EmbedBuilder()
            .WithTitle("CountryBot")
            .WithDescription("Sorry, that isn't a valid country/region code. Use the `/search` feature to find your country/region.")
            .WithColor(DiscordRed);
        if (tryThis == null) return embed;
        embed.Description = "Did you mean:\r\n";
        foreach (var country in tryThis)
        {
            embed.Description += $"``/set {country.Alpha2}`` for {country.Country}\r\n";
        }
        return embed;
    }

    public static EmbedBuilder CountrySet(CountryModel country)
    {
        return new EmbedBuilder()
            .WithTitle($"Your country/region has been set to {country.Country} on this guild!")
            .WithColor(DiscordGreen);
    }

    public static EmbedBuilder CountryRemoved()
    {
        return new EmbedBuilder()
            .WithTitle("Your country/region role has been removed on this guild.")
            .WithColor(DiscordGreen);
    }

    public static EmbedBuilder NotInDms()
    {
        return new EmbedBuilder()
            .WithTitle("Don't do the commands here!")
            .WithDescription("These commands are guild specific, so the command has to be done in the guild you want to use it on.")
            .WithColor(DiscordRed);
    }

    public static EmbedBuilder NotInCountry()
    {
        return new EmbedBuilder()
            .WithTitle("You are not in a country/region role on this guild.")
            .WithColor(DiscordYellow);
    }

    public static EmbedBuilder NoSearchResults(string searchQuery)
    {
        return new EmbedBuilder()
            .WithTitle($"Search Results for '{searchQuery}'.")
            .WithDescription("No Results Found.")
            .WithColor(DiscordRed);
    }

    public static EmbedBuilder Help(string supportUrl, bool isAdmin)
    {
        var embed = new EmbedBuilder()
            .WithTitle("CountryBot Help")
            .WithDescription($"For additional bot support, please join the support Discord: [CountryBot Support]({supportUrl}).\r\n\r\nThese are the commands you can use:")
            .AddField("/choose", "Provides a permanent drop down menu for users to select countries/regions.")
            .AddField("/search <country>", "Search for your country/region code.")
            .AddField("/set <country code>", "Sets your country/region role to the country/region with the given country/region code.")
            .AddField("/remove", "Removes your country/region role from this guild.")
            .AddField("/stats <optional:false>","Shows how many people have used the bot, use /stats true to see worldwide statistics.")
            .WithColor(DiscordYellow);
        if (!isAdmin) return embed;
        embed.AddField("/admin purge", "Removes all roles and user data from the database associated with this guild.");
        embed.AddField("/admin flags", "Choose whether your roles should have flag emoji's assigned to them (Requires a boosted server.");
        embed.AddField("/admin removeonempty", "Choose whether a role should be deleted if it's empty or not.");
        return embed;
    }


    public static EmbedBuilder PurgeComplete()
    {
        return new EmbedBuilder()
            .WithTitle("Purge Completed!")
            .WithDescription("All of the roles created by the CountryBot have been removed.")
            .WithColor(DiscordGreen);

    }

    public static EmbedBuilder RoleCapReached()
    {
        return new EmbedBuilder()
            .WithTitle("Role Limit Reached!")
            .WithDescription("We can't create any more roles in this server as it has reached it's role limit.")
            .WithColor(DiscordRed);
    }

    public static EmbedBuilder FlagChangeComplete(bool enableFlags)
    {
        return new EmbedBuilder()
            .WithTitle("Set Flag Role Icons")
            .WithDescription(enableFlags ? "Role Icons have been enabled for this guild." : "Role Icons have been disabled for this guild.")
            .WithColor(DiscordGreen);
    }

    public static EmbedBuilder FlagChangeFailed()
    {
        return new EmbedBuilder()
            .WithTitle("Set Flag Role Icons")
            .WithDescription("Role Icons can't be set for your server as it has not yet reached Tier 2.")
            .WithColor(DiscordYellow);
    }

    public static EmbedBuilder Stats(string guildName, IEnumerable<StatsModel> stats, bool worldWide)
    {
        var embedTitle = worldWide ? "Stats across the world!" : $"Stats in {guildName}!";
        var embedDescription = stats.Aggregate("Here is where most users are from!\r\n\r\n", (current, stat) => current + $":flag_{stat.Alpha2.ToLower()}: {stat.Country} - {stat.Result} users!\r\n");
        return new EmbedBuilder()
            .WithTitle(embedTitle)
            .WithDescription(embedDescription)
            .WithColor(DiscordGreen);
    }

    public static EmbedBuilder MissingPermissions()
    {
        return new EmbedBuilder()
            .WithTitle("Missing Permissions")
            .WithDescription(
                "It appears this bot is missing the Manage Roles permission. Please contact this guild's Administrator to fix this.")
            .WithColor(DiscordRed);
    }

    public static EmbedBuilder CountrySelector()
    {
        return new EmbedBuilder()
            .WithTitle("CountryBot")
            .WithDescription(
                "Please use the drop down below to select which country/region role you would like.");
    }

    public static EmbedBuilder CountryLetterSelector()
    {
        return new EmbedBuilder()
            .WithTitle("CountryBot")
            .WithDescription(
                "Please use the drop down below to select which letter your country/region starts with.");
    }

    public static EmbedBuilder AlreadyInCountryCode(CountryModel country)
    {
        return new EmbedBuilder()
            .WithTitle($"Your country/region is already set to {country.Country} on this guild!")
            .WithColor(DiscordYellow);
    }

    public static EmbedBuilder NewStats(string guildName, IEnumerable<StatsModel> stats, int page, int userCount, int guildCount, int guildUserCount)
    {
        var statsToShow = stats.Skip((page * 9) - 9).Take(9);
        var maxPages = (int)Math.Ceiling((double)stats.Count() / 9);
        var embedTitle = $"Stats in {guildName}!";
        var embedDescription = $"Here is where users are from in this guild!\r\n(Page {page}/{maxPages})\r\n\r\n";
        if (guildName == "Worldwide")
        {
            embedTitle = "Stats across the world!";
            embedDescription = $"Here is where CountryBot users are from across the world!\r\n(Page {page}/{maxPages})\r\n\r\n";
        }

        var embed = new EmbedBuilder()
            .WithTitle(embedTitle)
            .WithDescription(embedDescription)
            .WithColor(DiscordGreen)
            .WithThumbnailUrl("https://cdn.discordapp.com/avatars/992112299894636614/98a2a0e1fce02c61f18acd7a79725e34.png?size=512")
            .WithFooter($"{userCount:##,###} CountryBot users across {guildCount:##,###} guilds, {guildUserCount:##,###} in this guild!");

        foreach (var stat in statsToShow)
        {
            embed.Description += $":flag_{stat.Alpha2.ToLower()}: **{stat.Country}** - {stat.Result:##,###} user{stat.Result.Plural()}!\r\n";
        }
        return embed;
    }

    public static EmbedBuilder NotDeveloper()
    {
        return new EmbedBuilder()
            .WithTitle("Sorry, this command is only available to the Bot Developer as it's used for debugging and troubleshooting.")
            .WithColor(DiscordRed);
    }

    public static EmbedBuilder RemoveOnEmptyChangeComplete(bool enableRemoveOnEmpty)
    {
        return new EmbedBuilder()
            .WithTitle("Set toggle to Remove Roles when empty")
            .WithDescription(enableRemoveOnEmpty ? "Roles will now be removed when they are empty." : "Roles will no longer be removed when they are empty.")
            .WithColor(DiscordGreen);
    }

    public static EmbedBuilder OverrideComplete()
    {
        return new EmbedBuilder()
            .WithTitle("Override Added")
            .WithDescription("An override was added successfully.")
            .WithColor(DiscordGreen);
    }

    public static EmbedBuilder AddUserComplete()
    {
        return new EmbedBuilder()
            .WithTitle("User Added")
            .WithDescription("A user was added to a role successfully.")
            .WithColor(DiscordGreen);
    }

    public static EmbedBuilder JoinedGuild(SocketGuild guild)
    {
        return new EmbedBuilder()
            .WithTitle("Joined Guild")
            .WithDescription($"**{guild.Name}** ({guild.Id})")
            .WithColor(DiscordGreen)
            .WithThumbnailUrl(guild.IconUrl);
    }

    public static EmbedBuilder LeftGuild(SocketGuild guild)
    {
        return new EmbedBuilder()
            .WithTitle("Left Guild")
            .WithDescription($"**{guild.Name}** ({guild.Id})")
            .WithColor(DiscordRed)
            .WithThumbnailUrl(guild.IconUrl);
    }

    public static EmbedBuilder RemovedCountryInGuild(SocketGuild guild, SocketGuildUser user)
    {
        return new EmbedBuilder()
            .WithTitle("User Removed Country")
            .WithDescription($"Guild: **{guild.Name}** ({guild.Id})\r\nUser: **{user.Username}** ({user.Id})")
            .WithColor(DiscordRed)
            .WithThumbnailUrl(user.GetAvatarUrl());
    }

    public static EmbedBuilder AddedCountryInGuild(SocketGuild guild, SocketGuildUser user, CountryModel country)
    {
        return new EmbedBuilder()
            .WithTitle("User Added Country")
            .WithDescription($"Guild: **{guild.Name}** ({guild.Id})\r\nUser: **{user.Username}** ({user.Id})\r\nCountry: **{country.Country}** :flag_{country.Alpha2.ToLower()}:")
            .WithColor(DiscordGreen)
            .WithThumbnailUrl(user.GetAvatarUrl());
    }

    public static EmbedBuilder PurgeEmpty()
    {
        return new EmbedBuilder()
            .WithTitle("Empty Roles Removed")
            .WithDescription("All roles created by CountryBot that are empty have been removed.")
            .WithColor(DiscordGreen);
    }
}
