using System.Collections.Generic;
using System.Linq;
using CountryBot.Models;
using Discord;

namespace CountryBot.Embeds;

internal static class BotEmbeds
{
    public static EmbedBuilder SearchResults(IEnumerable<CountryModel> listOfCountries, string searchQuery)
    {
        return new EmbedBuilder()
            .WithTitle($"Search Results for '{searchQuery}'.")
            .WithDescription(listOfCountries
                .Aggregate(string.Empty,
                    (current, country) => 
                        current + $"{country.Country} - {country.Alpha2} or {country.Alpha3}\r\n"))
            .WithColor(new Color(0x3BA55D));
    }

    public static EmbedBuilder InvalidCountryCode(CountryModel tryThis = null)
    {
        return new EmbedBuilder()
            .WithTitle($"Sorry, that isn't a valid country code.")
            .WithDescription(
                tryThis == null 
                    ? null 
                    : $"Did you mean: ``/set {tryThis.Alpha2}`` for {tryThis.Country}?")
            .WithColor(new Color(0xED4245));
    }

    public static EmbedBuilder CountrySet(CountryModel country)
    {
        return new EmbedBuilder()
            .WithTitle($"Your country has been set to {country.Country} on this guild!")
            .WithColor(new Color(0x3BA55D));
    }

    public static EmbedBuilder CountryRemoved()
    {
        return new EmbedBuilder()
            .WithTitle("Your country role has been removed on this guild.")
            .WithColor(new Color(0x3BA55D));
    }

    public static EmbedBuilder NotInDms()
    {
        return new EmbedBuilder()
            .WithTitle("Don't do the commands here!")
            .WithDescription("These commands are guild specific, so the command has to be done in the guild you want to set or remove the role on.")
            .WithColor(new Color(0xED4245));
    }

    public static EmbedBuilder NotInCountry()
    {
        return new EmbedBuilder()
            .WithTitle("You are not in a country role on this guild.")
            .WithColor(new Color(0xFAA81A));
    }

    public static EmbedBuilder NoSearchResults(string searchQuery)
    {
        return new EmbedBuilder()
            .WithTitle($"Search Results for '{searchQuery}'.")
            .WithDescription("No Results Found.")
            .WithColor(new Color(0xFAA81A));            
    }    
}