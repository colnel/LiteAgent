using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace FunctionTools;

/// <summary>
/// 方法信息（用于 List 返回）
/// </summary>
public class MethodInfo
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<ParameterInfo> Parameters { get; set; } = new();
}

/// <summary>
/// 参数信息
/// </summary>
public class ParameterInfo
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public bool IsOptional { get; set; }
    public object? DefaultValue { get; set; }
}

public class ToolInvoker
{// ---------- 实例方法：方法清单和动态调用 ----------

    /// <summary>
    /// 列出所有静态方法名、描述和参数格式
    /// </summary>
    /// <returns>方法信息列表</returns>
    public List<MethodInfo> List()
    {
        var methods = typeof(ToolFactory).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.DeclaringType == typeof(ToolFactory)) // 排除继承的方法
            .Select(m => new MethodInfo
            {
                Name = m.Name,
                Description = m.GetCustomAttribute<DescriptionAttribute>()?.Description ?? "",
                Parameters = m.GetParameters().Select(p => new ParameterInfo
                {
                    Name = p.Name ?? "",
                    Type = p.ParameterType.Name,
                    IsOptional = p.IsOptional,
                    DefaultValue = p.DefaultValue
                }).ToList()
            }).ToList();
        return methods;
    }

    /// <summary>
    /// 动态执行指定的静态方法
    /// </summary>
    /// <param name="methodName">方法名（如 "DataQuery" 或 "BashExecute"）</param>
    /// <param name="parameters">参数字典，键为参数名（不区分大小写），值为参数值</param>
    /// <returns>方法执行结果（字符串）</returns>
    public static object? Execute(string methodName, Dictionary<string, object> parameters)
    {
        // 查找静态方法（忽略大小写）
        var method = typeof(ToolFactory).GetMethod(methodName, BindingFlags.Public | BindingFlags.Static) ?? throw new ArgumentException($"Method '{methodName}' not found.");

        // 准备参数数组（按方法参数顺序）
        var methodParams = method.GetParameters();
        var args = new object?[methodParams.Length];
        for (int i = 0; i < methodParams.Length; i++)
        {
            var param = methodParams[i];
            
            if (param.Name != null&& parameters.TryGetValue(param.Name, out var value))
            {
                // 类型转换（基本类型和常见类型）
                args[i] = ConvertValue(value, param.ParameterType);
            }
            else if (param.IsOptional)
            {
                args[i] = param.DefaultValue;
            }
            else
            {
                throw new ArgumentException($"Missing required parameter: {param.Name}");
            }
        }

        // 调用静态方法
        return method.Invoke(null, args);
    }

    /// <summary>
    /// 动态执行指定的静态方法
    /// </summary>
    /// <param name="methodName">方法名（如 "DataQuery" 或 "BashExecute"）</param>
    /// <param name="parameters">JsonElement 对象，表示参数字典，键为参数名，值为参数值</param>
    /// <returns>方法执行结果（字符串）</returns>
    public static string Execute(string methodName, JsonElement parameters)
    {
        // 确保 parameters 是一个对象
        if (parameters.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Parameters must be a JSON object.");

        // 将 JsonElement 转换为 Dictionary<string, object>（不区分大小写）
        var paramDict = JsonElementToDictionary(parameters);

        // 查找静态方法（忽略大小写）
        var method = typeof(ToolFactory).GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        if (method == null)
            throw new ArgumentException($"Method '{methodName}' not found.");

        // 准备参数数组（按方法参数顺序）
        var methodParams = method.GetParameters();
        var args = new object[methodParams.Length];
        for (int i = 0; i < methodParams.Length; i++)
        {
            var param = methodParams[i];
            if (paramDict.TryGetValue(param.Name!, out var value))
            {
                // 特殊处理：如果形参类型是 Dictionary<string, object>，并且 value 本身是字典，则直接使用
                if (param.ParameterType == typeof(Dictionary<string, object>) && value is Dictionary<string, object> dict)
                {
                    args[i] = dict;
                }
                else
                {
                    args[i] = ConvertValue(value, param.ParameterType);
                }
            }
            else if (param.IsOptional)
            {
                args[i] = param.DefaultValue!;
            }
            else
            {
                throw new ArgumentException($"Missing required parameter: {param.Name}");
            }
        }

        // 调用静态方法
        var result = method.Invoke(null, args);
        return result?.ToString() ?? "";
    }

    // 辅助方法：将 JsonElement 转换为 Dictionary<string, object>
    private static Dictionary<string, object> JsonElementToDictionary(JsonElement element)
    {
        var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in element.EnumerateObject())
        {
            dict[property.Name] = ConvertJsonElementToObject(property.Value);
        }
        return dict;
    }

    // 辅助方法：递归转换 JsonElement 为 CLR 对象
    private static object ConvertJsonElementToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString()!,
            JsonValueKind.Number => element.TryGetInt64(out long l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null!,
            JsonValueKind.Object => JsonElementToDictionary(element),
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElementToObject).ToArray(),
            _ => element.GetRawText()
        };
    }

    // 类型转换方法
    private static object ConvertValue(object value, Type targetType)
    {
        if (value == null)
        {
            if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                throw new InvalidCastException($"Cannot convert null to {targetType}");
            return null!;
        }

        var sourceType = value.GetType();
        if (targetType.IsAssignableFrom(sourceType))
            return value;

        // 处理 Dictionary<string, object> 到 Dictionary<string, object> 的转换（直接返回）
        if (targetType == typeof(Dictionary<string, object>) && value is Dictionary<string, object>)
            return value;

        // 处理 JsonElement（理论上经过辅助方法后不会出现，但保留兼容）
        if (value is JsonElement jsonElement)
        {
            var converted = ConvertJsonElementToObject(jsonElement);
            return ConvertValue(converted, targetType);
        }

        // 基本类型转换
        try
        {
            if (targetType.IsEnum)
            {
                if (value is string enumName)
                    return Enum.Parse(targetType, enumName, true);
                if (value is int intVal)
                    return Enum.ToObject(targetType, intVal);
            }
            if (targetType == typeof(Guid) && value is string guidStr)
                return Guid.Parse(guidStr);
            return Convert.ChangeType(value, targetType);
        }
        catch (Exception ex)
        {
            throw new InvalidCastException($"Cannot convert {value} ({sourceType}) to {targetType}", ex);
        }
    }
}
