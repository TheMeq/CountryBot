namespace CountryBot.Models;
public class ConfigModel
{
    public DiscordModel DiscordModel { get; set; }
    public MysqlModel MysqlModel { get; set; }
}

public class DiscordModel
{
    public string Token { get; set; }
    public ulong GuildId { get; set; }
    public ulong TestGuildId { get; set; }
}

public class MysqlModel
{
    public string Server { get; set; }
    public string Database { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string Charset { get; set; }
}