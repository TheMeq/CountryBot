using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MySql.Data.MySqlClient;

namespace DiscordBotTemplate.Utilities;
internal static class MySqlUtility
{
    private static string ConnectionStringBuilder()
    {
        var connectionString = $"server={Program.StaticConfig["mysql:server"]};";
        connectionString += $"uid={Program.StaticConfig["mysql:username"]};";
        connectionString += $"pwd={Program.StaticConfig["mysql:password"]};";
        connectionString += $"database={Program.StaticConfig["mysql:database"]};";
        connectionString += $"charset={Program.StaticConfig["mysql:charset"]};";
        return connectionString;
    }

    public static List<T> ConvertToList<T>(this DataTable dataTable)
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

    public static DataTable DoQuery(string query, Dictionary<string, object> parameters = null)
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

    public static void DoNonQuery(string query, Dictionary<string, object> parameters = null)
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

}