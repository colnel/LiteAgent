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
public class DataService(IOptions<DataSetting> _setting, ILogger<DataService> _logger) : IDisposable
{
    private readonly SqliteConnection _dbConnection = new(new SqliteConnectionStringBuilder
    {
        DataSource = _setting.Value.ConnectionString,
        Mode = SqliteOpenMode.ReadWriteCreate, // 不存在则创建
        Cache = SqliteCacheMode.Shared,
        DefaultTimeout = 5000, // 设置默认超时时间为5秒
        Password = "colnel*LiteAgent@2026" // 可选：设置数据库密码
    }.ToString());


    /// <summary>
    /// 初始化数据库连接和表结构
    /// </summary>
    internal  async Task Initialize()
    {
        try
        {
          await   ExecuteNonQueryAsync(@"
                CREATE TABLE IF NOT EXISTS agent_skills (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Description TEXT,
                    License TEXT,
                    Compatibility TEXT,
                    Metadata TEXT,
                    AllowedTools TEXT,
                    Body TEXT,
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    CreatedTime TEXT NOT NULL,
                    UpdatedTime TEXT
                )");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DataService 初始化失败:{ex}", ex);
        }
    }

    /// <summary>
    /// 添加一条新的 AgentSkill 记录，并返回新记录的条数，失败返回 -1
    /// </summary>
    /// <param name="skill"></param>
    /// <returns></returns>
    public async Task<int> AddSkill(AgentSkill skill)
    {
        const string insertSql = @"
                INSERT INTO agent_skills (Name, Description, License, Compatibility, Metadata, AllowedTools, Body, IsActive, CreatedTime, UpdatedTime)
                VALUES (@name, @description, @license, @compatibility, @metadata, @allowedTools, @body, @isActive, @createdTime, @updatedTime);
                SELECT last_insert_rowid();";
        using var command = new SqliteCommand(insertSql, _dbConnection);
        command.Parameters.AddWithValue("@name", skill.Name);
        command.Parameters.AddWithValue("@description", skill.Description);
        command.Parameters.AddWithValue("@license", skill.License ?? string.Empty);
        command.Parameters.AddWithValue("@compatibility", skill.Compatibility ?? string.Empty);
        command.Parameters.AddWithValue("@metadata", skill.Metadata ?? string.Empty);
        command.Parameters.AddWithValue("@allowedTools", skill.AllowedTools.ToString() ?? string.Empty);
        command.Parameters.AddWithValue("@body", skill.Body ?? string.Empty);
        command.Parameters.AddWithValue("@isActive", skill.IsActive ? 1 : 0);
        command.Parameters.AddWithValue("@createdTime", skill.CreatedTime.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("@updatedTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        var result = await command.ExecuteScalarAsync();
        return result != null ? Convert.ToInt32(result) : -1;
    }

    public async Task<int> UpdateSkill(AgentSkill skill)
    {
        const string updateSql = @"
                UPDATE agent_skills
                SET Name = @name, Description = @description, License = @license, Compatibility = @compatibility, Metadata = @metadata, AllowedTools = @allowedTools, Body = @body, IsActive = @isActive, UpdatedTime = @updatedTime
                WHERE Id = @id";
        using var command = new SqliteCommand(updateSql, _dbConnection);
        command.Parameters.AddWithValue("@id", skill.Id);
        command.Parameters.AddWithValue("@name", skill.Name);
        command.Parameters.AddWithValue("@description", skill.Description);
        command.Parameters.AddWithValue("@license", skill.License ?? string.Empty);
        command.Parameters.AddWithValue("@compatibility", skill.Compatibility ?? string.Empty);
        command.Parameters.AddWithValue("@metadata", skill.Metadata ?? string.Empty);
        command.Parameters.AddWithValue("@allowedTools", skill.AllowedTools.ToString() ?? string.Empty);
        command.Parameters.AddWithValue("@body", skill.Body ?? string.Empty);
        command.Parameters.AddWithValue("@isActive", skill.IsActive ? 1 : 0);
        command.Parameters.AddWithValue("@updatedTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        return await command.ExecuteNonQueryAsync();
    }

    public async Task<int> DeleteSkill(int id)
    {
        const string deleteSql = "DELETE FROM agent_skills WHERE Id = @id";
        using var command = new SqliteCommand(deleteSql, _dbConnection);
        command.Parameters.AddWithValue("@id", id);
        return await command.ExecuteNonQueryAsync();
    }
    public async Task<AgentSkill?> GetSkillById(int id)
    {
        const string selectSql = "SELECT * FROM agent_skills WHERE Id = @id";
        using var command = new SqliteCommand(selectSql, _dbConnection);
        command.Parameters.AddWithValue("@id", id);
        using var reader = await command.ExecuteReaderAsync();
        if (reader.Read())
        {
            return new AgentSkill
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Description = reader.GetString(reader.GetOrdinal("Description")),
                License = reader.GetString(reader.GetOrdinal("License")),
                Compatibility = reader.GetString(reader.GetOrdinal("Compatibility")),
                Metadata = reader.GetString(reader.GetOrdinal("Metadata")),
                AllowedTools = reader.GetString(reader.GetOrdinal("AllowedTools")).Split(','),
                Body = reader.GetString(reader.GetOrdinal("Body")),
                IsActive = reader.GetInt32(reader.GetOrdinal("IsActive")) == 1,
                CreatedTime = DateTime.Parse(reader.GetString(reader.GetOrdinal("CreatedTime"))),
                UpdatedTime = DateTime.Parse(reader.GetString(reader.GetOrdinal("UpdatedTime")))
            };
        }
        return null;
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
