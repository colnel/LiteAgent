using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;

namespace FunctionTools.DataAccess;

/// <summary>
/// 多数据库访问工具类，支持 MySQL、PostgreSQL、Oracle、ClickHouse、达梦、人大金仓
/// </summary>
public class DataBaseClient : IDisposable
{
    private readonly IDbConnection _connection;
    private bool _disposed;

    /// <summary>
    /// 构造函数，根据数据库类型创建连接
    /// </summary>
    /// <param name="dbType">数据库类型</param>
    /// <param name="connectionString">连接字符串</param>
    /// <exception cref="NotSupportedException">不支持的数据库类型</exception>
    /// <exception cref="Exception">驱动加载失败或连接创建失败</exception>
    public DataBaseClient(string type, string connectionString)
    {
        if (!Enum.TryParse(type, true, out DataBaseType dbType))
        {
            throw new NotSupportedException($"Unsupported database type: {type}");
        }
        _connection = CreateConnection(dbType, connectionString);
        _connection.Open();
    }

    /// <summary>
    /// 根据数据库类型动态创建 IDbConnection 实例（反射加载驱动）
    /// </summary>
    private static IDbConnection CreateConnection(DataBaseType dbType, string connectionString)
    {
        return dbType switch
        {
            DataBaseType.MySql => new MySqlConnector.MySqlConnection(connectionString),
            DataBaseType.PostgreSQL => new Npgsql.NpgsqlConnection(connectionString),
            DataBaseType.Oracle => new Oracle.ManagedDataAccess.Client.OracleConnection(connectionString),
            DataBaseType.ClickHouse => new ClickHouse.Client.ADO.ClickHouseConnection(connectionString),
            DataBaseType.DaMeng => new Dm.DmConnection(connectionString),
            DataBaseType.KingbaseES => new Kdbndp.KdbndpConnection(connectionString),
            _ => throw new NotSupportedException($"不支持的数据库类型: {dbType}")
        };
    }
    /// <summary>
    /// 执行查询，返回结果集（行列表，每行为字段名->值的字典）
    /// </summary>
    /// <param name="sql">SQL 语句</param>
    /// <returns>查询结果的 List&lt;Dictionary&lt;string, object&gt;&gt;，若无数据则返回空列表</returns>
    public List<Dictionary<string, object?>> ExecuteReader(string sql)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        using var reader = command.ExecuteReader();
        var results = new List<Dictionary<string, object?>>();
        while (reader.Read())
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                string columnName = reader.GetName(i);
                object value = reader.GetValue(i);
                row[columnName] = value == DBNull.Value ? null : value;
            }
            results.Add(row);
        }
        _connection.Close();
        return results;
    }

    /// <summary>
    /// 执行非查询 SQL（INSERT/UPDATE/DELETE 等）
    /// </summary>
    /// <param name="sql">SQL 语句</param>
    /// <returns>受影响的行数</returns>
    public int ExecuteNonQuery(string sql)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        int affectedRows = command.ExecuteNonQuery();
        _connection.Close();
        return affectedRows;
    }

    /// <summary>
    /// 执行查询，返回结果集的第一行第一列
    /// </summary>
    /// <param name="sql">SQL 语句</param>
    /// <returns>标量值，若无数据则返回 null</returns>
    public object? ExecuteScalar(string sql)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        var result = command.ExecuteScalar();
        _connection.Close();
        return result ;
    }

    /// <summary>
    /// 关闭连接并释放资源
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _connection?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

}
