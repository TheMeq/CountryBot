﻿namespace CountryBot.Models;

internal class RoleModel
{
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public ulong RoleId { get; set; }
    public int CountryId { get; set; }
}

