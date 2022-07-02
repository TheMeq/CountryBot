using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using CountryBot.Models;
using MySql.Data.MySqlClient;

namespace CountryBot.Utilities;
internal static class MySqlUtility
{
    private static string ConnectionStringBuilder()
    {
        var mySqlData = Program.StaticConfig.MysqlModel;
        var connectionString = $"server={mySqlData.Server};";
        connectionString += $"uid={mySqlData.Username};";
        connectionString += $"pwd={mySqlData.Password};";
        connectionString += $"database={mySqlData.Database};";
        connectionString += $"charset={mySqlData.Charset};";
        return connectionString;
    }

    private static List<T> ConvertToList<T>(this DataTable dataTable)
    {
        var columnNames = dataTable.Columns.Cast<DataColumn>()
            .Select(c => c.ColumnName)
            .ToList();
        var properties = typeof(T).GetProperties();
        return dataTable.AsEnumerable().Select(row =>
        {
            var objT = Activator.CreateInstance<T>();
            foreach (var propertyInfo in properties)
            {
                try
                {
                    if (!columnNames.Contains(propertyInfo.Name)) continue;
                    var newPropertyInfo = objT.GetType().GetProperty(propertyInfo.Name);
                    if (newPropertyInfo != null)
                    {
                        propertyInfo.SetValue(objT,
                            row[propertyInfo.Name] == DBNull.Value ? null : Convert.ChangeType(row[propertyInfo.Name], newPropertyInfo.PropertyType));
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(propertyInfo.Name);
                    Console.WriteLine(e);
                    throw;
                }
               
            }
            return objT;
        }).ToList();
    }

    private static DataTable DoQuery(string query, Dictionary<string, object> parameters = null)
    {
        using var connection = new MySqlConnection(ConnectionStringBuilder());
        connection.Open();

        using var command = new MySqlCommand();
        command.Connection = connection;
        command.CommandText = query;
        if (parameters != null)
            foreach (var (key, value) in parameters)
                command.Parameters.AddWithValue(key, value);

        using var sda = new MySqlDataAdapter(command);
        var dt = new DataTable();
        sda.Fill(dt);
        
        connection.CloseAsync();
        
        return dt;
    }

    private static void DoNonQuery(string query, Dictionary<string, object> parameters = null)
    {
        using var connection = new MySqlConnection(ConnectionStringBuilder());
        connection.Open();

        using var command = new MySqlCommand();
        command.Connection = connection;
        command.CommandText = query;
        if (parameters != null)
            foreach (var (key, value) in parameters)
                command.Parameters.AddWithValue(key, value);
        command.ExecuteNonQuery();
        connection.CloseAsync();
    }
    
    public static bool HasRows(this DataTable input)
    {
        return input.Rows.Count > 0;
    }

    internal static List<CountryModel> Search(string country)
    {
        var arguments = new Dictionary<string, object>()
        {
            {"Country", "%" + country + "%"},
        };
        var countryResults = DoQuery("SELECT * FROM valid_countries WHERE Country like @Country", arguments).ConvertToList<CountryModel>();
        var alternativeNamesResults = DoQuery("SELECT * FROM valid_countries WHERE AlternativeNames like @Country", arguments).ConvertToList<CountryModel>();
        var alpha2Results = DoQuery("SELECT * FROM valid_countries WHERE Alpha2 like @Country", arguments).ConvertToList<CountryModel>();
        var alpha3Results = DoQuery("SELECT * FROM valid_countries WHERE Alpha3 like @Country", arguments).ConvertToList<CountryModel>();

        var results = countryResults
            .Union(alternativeNamesResults)
            .Union(alpha2Results)
            .Union(alpha3Results)
            .DistinctBy(countryModel => countryModel.Country)
            .ToList();
        return results;

    }

    public static bool IsValidCountryCode(string countryCode)
    {
        var arguments = new Dictionary<string, object>()
        {
            {"Country", countryCode},
        };
        var alpha2Results = DoQuery("SELECT * FROM valid_countries WHERE Alpha2 = @Country", arguments).ConvertToList<CountryModel>();
        var alpha3Results = DoQuery("SELECT * FROM valid_countries WHERE Alpha3 = @Country", arguments).ConvertToList<CountryModel>();
        var results = alpha2Results.Union(alpha3Results).DistinctBy(countryModel => countryModel.Country).ToList();
        return results.Count == 1;
    }

    public static CountryModel GetCountry(string countryCode)
    {
        var arguments = new Dictionary<string, object>()
        {
            {"Country", countryCode},
        };
        var alpha2Results = DoQuery("SELECT * FROM valid_countries WHERE Alpha2 = @Country", arguments).ConvertToList<CountryModel>();
        var alpha3Results = DoQuery("SELECT * FROM valid_countries WHERE Alpha3 = @Country", arguments).ConvertToList<CountryModel>();
        var results = alpha2Results.Union(alpha3Results).DistinctBy(countryModel => countryModel.Country).ToList();
        return results[0];
    }

    public static bool IsUserInRoleAlready(ulong guildId, ulong userId)
    {
        var arguments = new Dictionary<string, object>()
        {
            {"UserId", userId},
            {"GuildId", guildId}
        };
        var results = DoQuery("SELECT * FROM users WHERE UserId = @UserId and GuildId = @GuildId", arguments).ConvertToList<UserModel>();
        return results.Count == 1;
    }

    public static UserModel GetUser(ulong guildId, ulong userId)
    {
        var arguments = new Dictionary<string, object>()
        {
            {"UserId", userId},
            {"GuildId", guildId}
        };
        var results = DoQuery("SELECT * FROM users WHERE UserId = @UserId and GuildId = @GuildId", arguments).ConvertToList<UserModel>();
        return results[0];
    }

    public static void RemoveUser(ulong guildId, ulong userId)
    {
        var arguments = new Dictionary<string, object>()
        {
            {"UserId", userId},
            {"GuildId", guildId}
        };
        DoNonQuery("DELETE FROM users WHERE UserId = @UserId and GuildId = @GuildId", arguments);
    }

    public static bool DoesRoleContainUsers(ulong guildId, int countryId)
    {
        var arguments = new Dictionary<string, object>()
        {
            {"countryId", countryId},
            {"GuildId", guildId}
        };
        var results = DoQuery("SELECT * FROM users WHERE CountryId = @countryId and GuildId = @GuildId", arguments
            ).ConvertToList<UserModel>();
        return results.Count > 0;
    }

    public static RoleModel GetRole(ulong guildId, int countryId)
    {
        var arguments = new Dictionary<string, object>()
        {
            {"countryId", countryId},
            {"GuildId", guildId}
        };
        var results = DoQuery("SELECT * FROM roles WHERE CountryId = @countryId and GuildId = @GuildId", arguments).ConvertToList<RoleModel>();
        return results[0];
    }

    public static void RemoveRole(ulong guildId, ulong roleInfoRoleId)
    {
        var arguments = new Dictionary<string, object>()
        {
            {"RoleId", roleInfoRoleId},
            {"GuildId", guildId}
        };
        DoNonQuery("DELETE FROM roles WHERE RoleId = @RoleId and GuildId = @GuildId", arguments);
    }

    public static bool DoesRoleExist(ulong guildId, int countryId)
    {
        var arguments = new Dictionary<string, object>()
        {
            {"CountryId", countryId},
            {"GuildId", guildId}
        };
        var results = DoQuery("SELECT * FROM roles WHERE CountryId = @CountryId and GuildId = @GuildId", arguments)
            .ConvertToList<RoleModel>();
        return results.Count > 0;
    }

    public static void AddUser(ulong guildId, ulong userId, int countryId)
    {
        var arguments = new Dictionary<string, object>()
        {
            {"UserId", userId},
            {"GuildId", guildId},
            {"CountryId", countryId}
        };
        DoNonQuery("INSERT INTO users (GuildId, UserId, CountryId) VALUES (@GuildId, @UserId, @CountryId)",arguments);
    }

    public static void AddRole(ulong guildId, ulong roleId, int countryId)
    {
        var arguments = new Dictionary<string, object>()
        {
            {"RoleId", roleId},
            {"GuildId", guildId},
            {"CountryId", countryId}
        };
        DoNonQuery("INSERT INTO roles (GuildId, RoleId, CountryId) VALUES (@GuildId, @RoleId, @CountryId)", arguments);
    }

    public static int UserCount()
    {
        var results = DoQuery("SELECT * FROM users").ConvertToList<UserModel>();
        return results.Count;
    }

    public static List<RoleModel> GetAllRolesForGuild(ulong guildId)
    {
        var arguments = new Dictionary<string, object>
        {
            {"GuildId", guildId}
        };
        return DoQuery("SELECT * FROM roles WHERE GuildId = @GuildId",arguments).ConvertToList<RoleModel>();
    }

    public static void RemoveAllRolesForGuild(ulong guildId)
    {
        var arguments = new Dictionary<string, object>
        {
            {"GuildId", guildId}
        };
        DoNonQuery("DELETE FROM roles WHERE GuildId = @GuildId",arguments);
    }

    public static void RemoveAllUsersForGuild(ulong guildId)
    {
        var arguments = new Dictionary<string, object>
        {
            {"GuildId", guildId}
        };
        DoNonQuery("DELETE FROM users WHERE GuildId = @GuildId", arguments);
    }
}