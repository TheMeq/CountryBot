using System;
using System.Threading.Tasks;
using Discord;

namespace CountryBot.Logger;

public sealed class ConsoleLogger : Logger
{
    public async Task Log(LogMessage message)
    {
        await Task.Run(() => LogToConsoleAsync(message));
    }

    private async Task LogToConsoleAsync(LogMessage message)
    {
        await Task.Run(() => Console.WriteLine($"GUID:{SystemGuid}: " + message));
    }
}