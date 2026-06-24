using System;

namespace ProjectChecking.Api.Models;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class User
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime? BirthDate { get; set; }
    public DateTime RegistrationDate { get; set; }
}

public class Activity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class Qualification
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class Leader
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
}

public class Worker
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string Status { get; set; } = "свободен";
}

public class Project
{
    public int Id { get; set; }
    public int ActivityId { get; set; }
    public string Customer { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? LeaderId { get; set; }
    public DateTime CreationDate { get; set; }
    public string Status { get; set; } = "в очереди";
    public DateTime? CompletionDate { get; set; }
}

public class ProjectHistory
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int? WorkerId { get; set; }
    public int? LeaderId { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? LeftAt { get; set; }
}

public class Session
{
    public string Token { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
