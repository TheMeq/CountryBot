using System;

namespace DiscordBotTemplate.Logger;

public abstract class Logger
{
    protected readonly string SystemGuid;

    protected Logger()
    {
        SystemGuid = Guid.NewGuid().ToString()[^4..].ToUpper();
    }
}