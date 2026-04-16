using LiteAgent.AgentHost.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;

namespace LiteAgent.AgentHost.Services;

/// <summary>
/// SQLite 数据服务类，提供 AgentSkill 表的增删改查操作
/// </summary>
public class DataService(IOptions<string> _connString, ILogger<Worker> _logger) : IDisposable
{
    private readonly SqliteConnection _dbConnection = new(new SqliteConnectionStringBuilder
    {
        DataSource = _connString.Value,
        Mode = SqliteOpenMode.ReadWriteCreate, // 不存在则创建
        Cache = SqliteCacheMode.Shared,
        DefaultTimeout = 5000, // 设置默认超时时间为5秒
        Password = "colnel*LiteAgent@2026" // 可选：设置数据库密码
    }.ToString());

    /// <summary>
    /// 初始化数据库连接和表结构
    /// </summary>
    internal async Task Initialize()
    {
        try
        {
            await OpenAsync();
            await ExecuteNonQueryAsync(@"
                CREATE TABLE IF NOT EXISTS Skill (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Age INTEGER NOT NULL
                )");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DataService 初始化失败:{ex}", ex);
        }

    }

    /// <summary>
    /// 插入一条新用户记录
    /// </summary>
    /// <param name="name">姓名</param>
    /// <param name="age">年龄</param>
    /// <returns>新记录的自增 Id，失败返回 -1</returns>
    public int InsertUser(string name, int age)
    {
        const string insertSql = @"
                INSERT INTO AgentSkill (Name, Age)
                VALUES (@name, @age);
                SELECT last_insert_rowid();";

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = new SqliteCommand(insertSql, connection);
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@age", age);

        var result = command.ExecuteScalar();
        return result != null ? Convert.ToInt32(result) : -1;
    }

    /// <summary>
    /// 根据 Id 更新用户信息
    /// </summary>
    /// <returns>受影响的行数</returns>
    public int UpdateUser(int id, string newName, int newAge)
    {
        const string updateSql = @"
                UPDATE AgentSkill
                SET Name = @name, Age = @age
                WHERE Id = @id";

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = new SqliteCommand(updateSql, connection);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@name", newName);
        command.Parameters.AddWithValue("@age", newAge);

        return command.ExecuteNonQuery();
    }

    /// <summary>
    /// 根据 Id 删除用户
    /// </summary>
    /// <returns>受影响的行数</returns>
    public int DeleteUser(int id)
    {
        const string deleteSql = "DELETE FROM AgentSkill WHERE Id = @id";

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = new SqliteCommand(deleteSql, connection);
        command.Parameters.AddWithValue("@id", id);

        return command.ExecuteNonQuery();
    }

    /// <summary>
    /// 根据 Id 查询单个用户
    /// </summary>
    /// <returns>AgentSkill 对象，未找到返回 null</returns>
    public AgentSkill GetUserById(int id)
    {
        const string querySql = "SELECT Id, Name, Age FROM AgentSkill WHERE Id = @id";

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = new SqliteCommand(querySql, connection);
        command.Parameters.AddWithValue("@id", id);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new AgentSkill
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Age = reader.GetInt32(2)
            };
        }
        return null;
    }

    /// <summary>
    /// 获取所有用户列表
    /// </summary>
    public List<AgentSkill> GetAllUsers()
    {
        const string querySql = "SELECT Id, Name, Age FROM AgentSkill ORDER BY Id";

        var users = new List<AgentSkill>();
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = new SqliteCommand(querySql, connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            users.Add(new AgentSkill
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Age = reader.GetInt32(2)
            });
        }
        return users;
    }

    /// <summary>
    /// 按姓名模糊查询用户
    /// </summary>
    public List<AgentSkill> SearchUsersByName(string keyword)
    {
        const string searchSql = "SELECT Id, Name, Age FROM AgentSkill WHERE Name LIKE @keyword ORDER BY Id";
        var users = new List<AgentSkill>();

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = new SqliteCommand(searchSql, connection);
        command.Parameters.AddWithValue("@keyword", $"%{keyword}%");

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            users.Add(new AgentSkill
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Age = reader.GetInt32(2)
            });
        }
        return users;
    }
    public async Task<List<object>> ExecuteReaderAsync(string sql)
    {
        List<object> result = [];
        await OpenAsync();
        using var command = new SqliteCommand(sql, _dbConnection);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var obj = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                string strKey = reader.GetName(i);
                if (reader.GetFieldType(strKey) == typeof(DateTime))
                {
                    obj[strKey] = reader.IsDBNull(strKey) ? "" : reader.GetDateTime(i).ToString("yyyy-MM-dd HH:mm:ss");
                }
                else
                {
                    obj[strKey] = reader.IsDBNull(strKey) ? "" : reader[i];
                }
            }
            result.Add(obj);
        }
        return result;
    }

    public async Task<int> ExecuteNonQueryAsync(string sql)
    {
        int result;
        await OpenAsync();
        using var command = new SqliteCommand(sql, _dbConnection);
        result = await command.ExecuteNonQueryAsync();
        await CloseAsync();
        return result;
    }

    public async Task<object?> ExecuteScalarAsync(string sql)
    {
        object? result;
        await OpenAsync();
        using var command = new SqliteCommand(sql, _dbConnection);
        result = await command.ExecuteScalarAsync();
        await CloseAsync();
        return result;
    }

    public async Task OpenAsync()
    {
        if (_dbConnection != null && _dbConnection.State != ConnectionState.Open)
        {
            await _dbConnection.OpenAsync();
        }
    }

    public async Task CloseAsync()
    {
        if (_dbConnection != null && _dbConnection.State != ConnectionState.Closed)
        {
            await _dbConnection.CloseAsync();
        }
    }
    public void Dispose()
    {
        _dbConnection?.Dispose();
        GC.SuppressFinalize(this);
    }

}
