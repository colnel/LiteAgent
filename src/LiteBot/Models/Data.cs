using System;
using System.Collections.Generic;
using System.Text;

namespace LiteBot.Models;
// Data/Models/User.cs
public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserName { get; set; }
    public string NickName { get; set; }
    public string AvatarUrl { get; set; }
    public string StatusMessage { get; set; }
    public bool IsOnline { get; set; }
    // 好友关系通过 Conversation 间接建立
}

// Data/Models/Conversation.cs
public class Conversation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; }      // 当前用户
    public string ContactId { get; set; }   // 对方用户
    public DateTime CreatedAt { get; set; }
    public User Contact { get; set; }       // 导航属性
    public ICollection<Message> Messages { get; set; }
}

// Data/Models/Message.cs
public class Message
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ConversationId { get; set; }
    public string SenderId { get; set; }
    public string Content { get; set; }
    public DateTime SentAt { get; set; }
    public bool IsRead { get; set; }
}

// Data/Models/TaskItem.cs
public class TaskItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; }
    public string Description { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string AssignedUserId { get; set; }
}

// Data/Models/SystemConfig.cs
public class SystemConfig
{
    public string Key { get; set; }
    public string Value { get; set; }
}