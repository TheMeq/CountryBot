using System.Collections.Generic;
using CountryBot.Models;
using Discord;

namespace CountryBot.Embeds
{
    internal static class BotEmbeds
    {
        public static EmbedBuilder SearchResults(List<CountryModel> result, string query)
        {
            var embed = new EmbedBuilder
            {
                Title = $"Search Results for {query}"
            };
            foreach (var country in result)
            {
                embed.Description += $"{country.Country} - {country.Alpha2} or {country.Alpha3}\r\n";
            }

            return embed;
        }

        public static EmbedBuilder InvalidCountryCode(CountryModel tryThis = null)
        {
            var embed = new EmbedBuilder
            {
                Title = $"Sorry, that isn't a valid country code."
            };
            if (tryThis != null)
            {
                embed.Description += $"Did you mean: ``/set {tryThis.Alpha2}`` for {tryThis.Country}?";
            }
            return embed;
        }

        public static EmbedBuilder CountrySet(CountryModel country)
        {
            var embed = new EmbedBuilder
            {
                Title = $"Your country has been set to {country.Country}!"
            };
            return embed;
        }

        public static EmbedBuilder CountryRemoved()
        {
            var embed = new EmbedBuilder
            {
                Title = "Your country role has been reset."
            };
            return embed;
        }

        public static EmbedBuilder NotInDms()
        {
            var embed = new EmbedBuilder
            {
                Title = "Don't do the commands here!",
                Description = "These commands are guild specific, so the command has to be done in the guild you want to set or remove the role on."
            };
            return embed;
        }
    }
}
