using System.Collections.Generic;
using System.Linq;
using CountryBot.Models;
using Discord;

namespace CountryBot.Embeds;

internal static class BotEmbeds
{
    private static readonly Color DiscordGreen = new(59, 165, 93);
    private static readonly Color DiscordYellow = new(250, 168, 26);
    private static readonly Color DiscordRed = new(237, 66, 59);
    
    public static EmbedBuilder SearchResults(IEnumerable<CountryModel> listOfCountries, string searchQuery)
    {
        return new EmbedBuilder()
            .WithTitle($"Search Results for '{searchQuery}'.")
            .WithDescription(listOfCountries
                .Aggregate(string.Empty,
                    (current, country) => 
                        current + $"{country.Country} - {country.Alpha2} or {country.Alpha3}\r\n"))
            .WithColor(DiscordGreen);
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
            .WithDescription("These commands are guild specific, so the command has to be done in the guild you want to set or remove the role on.")
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

    public static EmbedBuilder Help()
    {
        return new EmbedBuilder()
            .WithTitle("CountryBot Help")
            .WithDescription("These are the commands you can use:")
            .AddField("/choose", "Provides a permanent drop down menu for users to select countries/regions.")
            .AddField("/search <country>", "Search for your country/region code.")
            .AddField("/set <country code>", "Sets your country/region role to the country/region with the given country/region code.")
            .AddField("/remove", "Removes your country/region role from this guild.")
            .AddField("/stats <optional:false>","Shows how many people have used the bot, use /stats true to see worldwide statistics.")
            .WithColor(DiscordYellow);
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
            .WithDescription("Role Icons can't be set for your server as it has not yet reached Tier 2")
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
}