using System.Threading.Tasks;
using Discord;
using CountryBot.Utilities;

namespace CountryBot.Logger;

public sealed class ConsoleLogger : Logger
{
    public async Task Log(LogMessage message)
    {
        await Task.Run(() => LogToConsoleAsync(message));
    }

    private async Task LogToConsoleAsync(LogMessage message)
    {
        await Task.Run(() => ColorConsole.WriteEmbeddedColorLine($"GUID:[yellow]{SystemGuid}[/yellow]: " + message));
    }
}