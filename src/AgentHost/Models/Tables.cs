using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace LiteAgent.AgentHost.Models;

/// <summary>
/// 表示一个 Agent 的技能实体，映射到数据库表 <c>AgentSkills</c>。
/// 包含技能的元数据（名称、版本、作者等）、状态信息以及可序列化的参数字典。
/// </summary>
/// <remarks>
/// - 使用实体属性上的数据注解（例如 <see cref="System.ComponentModel.DataAnnotations.Key"/>、
///   <see cref="System.ComponentModel.DataAnnotations.Required"/>、<see cref="System.ComponentModel.DataAnnotations.MaxLengthAttribute"/>）
///   来约束数据库列和模型验证。  
/// - <see cref="Content"/> 持久化为 JSON 字符串（在支持的数据库上使用 <c>jsonb</c> 类型）。
/// - <see cref="Parameters"/> 为非映射属性，方便在代码中以字典形式读写参数；对其的写入会同步到 <see cref="Content"/>。
/// </remarks>
[Table("AgentSkills")]
public class AgentSkills
{
    /// <summary>
    /// 自增主键。
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 技能名称。必填，最大长度 50。
    /// 默认值为 "skill name"（仅用于新实例的便捷默认）。
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = "skill name";

    /// <summary>
    /// 技能的简要描述。可选，最大长度 200。
    /// </summary>
    [MaxLength(200)]
    public string? Description { get; set; }

    /// <summary>
    /// 技能版本（例如 "1.0.0"）。可选，最大长度 50。
    /// </summary>
    [MaxLength(50)]
    public string? Version { get; set; }

    /// <summary>
    /// 作者或提供者名称。可选，最大长度 50。
    /// </summary>
    [MaxLength(50)]
    public string? Author { get; set; }

    /// <summary>
    /// 标记技能是否处于激活状态。默认值为 <c>true</c>。
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 相关工具列表或说明（例如以逗号分隔）。可选，最大长度 500。
    /// </summary>
    [MaxLength(500)]
    public string? Tools { get; set; }

    /// <summary>
    /// 创建时间，使用 UTC 时区。默认初始化为 <see cref="DateTime.UtcNow"/>。
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 最近一次更新时间，若未更新则为 <c>null</c>。
    /// </summary>
    public DateTime? UpdatedTime { get; set; }

    // 持久化为 JSON 字符串
    [Column(TypeName = "jsonb")] // 如果使用的数据库支持 JSON 类型，可调整或移除该特性
    public string? Content { get; set; }

    /// <summary>
    /// 非映射属性：将 <see cref="Content"/> 反序列化为字典，写入时会同步序列化回 <see cref="Content"/>。
    /// 使用 <see cref="System.Text.Json.JsonSerializer"/> 进行序列化/反序列化。
    /// </summary>
    /// <remarks>
    /// 反序列化可能引发异常（例如格式错误），调用方应根据需要做好异常处理或校验。
    /// 若 <see cref="Content"/> 为空或仅包含空白，则返回 <c>null</c>。
    /// </remarks>
    [NotMapped]
    public Dictionary<string, object>? Parameters
    {
        get => string.IsNullOrWhiteSpace(Content)
            ? null
            : JsonSerializer.Deserialize<Dictionary<string, object>>(Content);
        set => Content = value is null ? null : JsonSerializer.Serialize(value);
    }

    /// <summary>
    /// 返回实体的简要字符串表示，便于调试与日志记录。
    /// </summary>
    public override string ToString()
    {
        return $"Id: {Id}, Name: {Name}, Version: {Version},Author: {Author},  Active: {IsActive},  UpdatedTime: {UpdatedTime}";
    }
}