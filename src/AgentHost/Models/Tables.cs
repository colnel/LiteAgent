using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Intrinsics.X86;
using System.Text.Json;
using System.Xml.Linq;
using static System.Net.WebRequestMethods;

namespace LiteAgent.AgentHost.Models;

/// <summary>
/// 表示一个 Agent 的技能实体，映射到数据库表 <c>AgentSkill</c>。
/// 包含技能的元数据（名称、版本、作者等）、状态信息以及可序列化的参数字典。
/// </summary>
/// <remarks>
/// - 使用实体属性上的数据注解（例如 <see cref="System.ComponentModel.DataAnnotations.Key"/>、
///   <see cref="System.ComponentModel.DataAnnotations.Required"/>、<see cref="System.ComponentModel.DataAnnotations.MaxLengthAttribute"/>）
///   来约束数据库列和模型验证。  
/// - <see cref="Metadata"/> 持久化为 JSON 字符串（在支持的数据库上使用 <c>jsonb</c> 类型）。
/// - <see cref="MetadataParameters"/> 为非映射属性，方便在代码中以字典形式读写参数；对其的写入会同步到 <see cref="Metadata"/>。
/// </remarks>
[Table("AgentSkill")]
public class AgentSkill
{
    /// <summary>
    /// 自增主键。
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 技能名称。必填，最大长度 64。
    /// 默认值为 "skill name"（仅用于新实例的便捷默认）。
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string Name { get; set; } = "skill name";

    /// <summary>
    /// 技能的简要描述。可选，最大长度 1024。
    /// </summary>
    [MaxLength(1024)]
    public string Description { get; set; } = "Describes what the skill does and when to use it.";

    /// <summary>
    /// 技能版本（例如 "1.0.0"）。可选，最大长度 50。
    /// </summary>
    [MaxLength(50)]
    public string? License { get; set; }

    /// <summary>
    /// 作者或提供者名称。可选，最大长度 500。
    /// </summary>
    [MaxLength(500)]
    public string? Compatibility { get; set; }

    // 持久化为 JSON 字符串
    [Column(TypeName = "jsonb")] // 如果使用的数据库支持 JSON 类型，可调整或移除该特性
    public string? Metadata { get; set; }

    /// <summary>
    /// 非映射属性：将 <see cref="Metadata"/> 反序列化为字典，写入时会同步序列化回 <see cref="Metadata"/>。
    /// 使用 <see cref="System.Text.Json.JsonSerializer"/> 进行序列化/反序列化。
    /// </summary>
    /// <remarks>
    /// 反序列化可能引发异常（例如格式错误），调用方应根据需要做好异常处理或校验。
    /// 若 <see cref="Metadata"/> 为空或仅包含空白，则返回 <c>null</c>。
    /// </remarks>
    [NotMapped]
    public Dictionary<string, object>? MetadataParameters
    {
        get => string.IsNullOrWhiteSpace(Metadata) ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(Metadata);
        set => Metadata = value is null ? null : JsonSerializer.Serialize(value);
    }
    /// <summary>
    /// 相关工具列表或说明（例如以逗号分隔）。可选，最大长度 500。
    /// </summary>
    [MaxLength(500)]
    public string[] AllowedTools { get; set; } = [];

    /// <summary>
    /// 技能的详细说明或使用指南。可选，最大长度 4000。
    /// 章节建议：
    /// 流程指导
    /// 输入输出示例
    /// 常见边界情况
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// 标记技能是否处于激活状态。默认值为 <c>true</c>。
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 创建时间，使用 UTC 时区。默认初始化为 <see cref="DateTime.UtcNow"/>。
    /// </summary>
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 最近一次更新时间，若未更新则为 <c>null</c>。
    /// </summary>
    public DateTime? UpdatedTime { get; set; }

    /// <summary>
    /// 转化为 Markdown 格式的字符串，适用于在 Markdown 支持的环境中展示技能信息。
    /// </summary>
    /// <returns>Markdown 格式的技能信息字符串。</returns>
    public string ToMd()
    {
        string md = $$"""
---
name: {{Name}}
description: {{Description}}
license: {{License}}
metadata:
""";
        var paras = MetadataParameters;
        if (paras != null)
        {
            foreach (var kv in paras)
            {
                md += $"  {kv.Key}: {kv.Value}\n";
            }
        }
        md += "---";
        if (!string.IsNullOrWhiteSpace(Body))
        {
            md += $"\n\n{Body}";
        }
        return md;
    }

    /// <summary>
    /// 返回实体的简要字符串表示，便于调试与日志记录。
    /// </summary>
    public override string ToString()
    {
        {
            return $"Id: {Id}, Name: {Name}, License: {License},Compatibility: {Compatibility},  Active: {IsActive},  UpdatedTime: {UpdatedTime}";
        }
    }
}