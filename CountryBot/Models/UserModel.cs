namespace CountryBot.Models;

public class UserModel
{
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public int CountryId { get; set; }
}