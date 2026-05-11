
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using FunctionTools.DataAccess;
namespace FunctionTools;

/// <summary>
/// 工具工厂：提供静态工具方法及动态调用能力
/// </summary>
public class ToolFactory
{
    // ---------- 静态工具方法 ----------

    /// <summary>
    /// 数据查询
    /// </summary>
    /// <param name="databaseType">数据库类型，目前仅支持 "SqlServer"</param>
    /// <param name="connectionString">连接字符串</param>
    /// <param name="sql">SQL 查询语句</param>
    /// <returns>查询结果（JSON 字符串）</returns>
    [Description("数据查询，返回JSON字符串")]
    public static string DataQuery(string databaseType, string connectionString, string sql)
    {
        using var accesser = new DataBaseClient(databaseType, connectionString);
        var result = accesser.ExecuteReader(sql);       
         var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(result, jsonOptions);
    }

    /// <summary>
    /// 执行 Bash 命令（跨平台）
    /// </summary>
    /// <param name="command">要执行的命令字符串</param>
    /// <returns>命令的标准输出和错误输出</returns>
    public static string BashExecute(string command)
    {
        string fileName;
        string arguments;
        if (OperatingSystem.IsWindows())
        {
            fileName = "cmd.exe";
            arguments = $"/c {command}";
        }
        else
        {
            fileName = "/bin/bash";
            arguments = $"-c \"{command}\"";
        }

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return string.IsNullOrEmpty(error) ? output.Trim() : $"ERROR: {error.Trim()}\n{output.Trim()}";
    }

    
}
