using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace LiteAgent.AgentHost.Services;

/// <summary>
/// SQLite 数据服务类，提供 User 表的增删改查操作
/// </summary>
public class DataService
{
    private readonly string _connectionString;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="databaseFile">数据库文件路径（如 "data.db"）</param>
    public DataService(string databaseFile)
    {
        // 构建连接字符串，启用自动关闭和共享缓存
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databaseFile,
            Mode = SqliteOpenMode.ReadWriteCreate, // 不存在则创建
            Cache = SqliteCacheMode.Shared
        }.ToString();

        // 初始化数据库：创建表（如果不存在）
        InitializeDatabase();
    }

    /// <summary>
    /// 初始化数据库，创建 User 表
    /// </summary>
    private void InitializeDatabase()
    {
        const string createTableSql = @"
                CREATE TABLE IF NOT EXISTS User (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Age INTEGER NOT NULL
                )";

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = new SqliteCommand(createTableSql, connection);
        command.ExecuteNonQuery();
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
                INSERT INTO User (Name, Age)
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
                UPDATE User
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
        const string deleteSql = "DELETE FROM User WHERE Id = @id";

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = new SqliteCommand(deleteSql, connection);
        command.Parameters.AddWithValue("@id", id);

        return command.ExecuteNonQuery();
    }

    /// <summary>
    /// 根据 Id 查询单个用户
    /// </summary>
    /// <returns>User 对象，未找到返回 null</returns>
    public User GetUserById(int id)
    {
        const string querySql = "SELECT Id, Name, Age FROM User WHERE Id = @id";

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = new SqliteCommand(querySql, connection);
        command.Parameters.AddWithValue("@id", id);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new User
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
    public List<User> GetAllUsers()
    {
        const string querySql = "SELECT Id, Name, Age FROM User ORDER BY Id";

        var users = new List<User>();
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = new SqliteCommand(querySql, connection);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            users.Add(new User
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
    public List<User> SearchUsersByName(string keyword)
    {
        const string searchSql = "SELECT Id, Name, Age FROM User WHERE Name LIKE @keyword ORDER BY Id";
        var users = new List<User>();

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = new SqliteCommand(searchSql, connection);
        command.Parameters.AddWithValue("@keyword", $"%{keyword}%");

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            users.Add(new User
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Age = reader.GetInt32(2)
            });
        }
        return users;
    }
}

/// <summary>
/// 用户实体类
/// </summary>
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }

    public override string ToString()
    {
        return $"Id: {Id}, Name: {Name}, Age: {Age}";
    }
}