namespace CountryBot.Models;

internal class GuildsModel
{
    public ulong GuildId { get; set; }
    public int FlagsEnabled { get; set; }
    public int RemoveOnEmpty { get; set; }
    public ulong CreateDirectlyBelow { get; set; }

}