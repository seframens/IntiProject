using System;
using System.Collections.Generic;

namespace ProjectChecking.Wpf.Models;

public class LoginRequest
{
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Login { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class CurrentUserDto
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime? BirthDate { get; set; }
    public DateTime RegistrationDate { get; set; }
}

public class UpdateMeRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? NewPassword { get; set; }
    public string? ConfirmPassword { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string? Phone { get; set; }
    public DateTime? BirthDate { get; set; }
    public DateTime RegistrationDate { get; set; }
}

public class UserCreateRequest
{
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string? Phone { get; set; }
    public DateTime? BirthDate { get; set; }
}

public class UserUpdateRequest
{
    public string Login { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string? Phone { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Password { get; set; }
}

public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class NamedDictionaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ActivityDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? QualificationId { get; set; }
    public string? QualificationName { get; set; }
}

public class ActivityQualificationDto
{
    public int ActivityId { get; set; }
    public string ActivityName { get; set; } = string.Empty;
    public int QualificationId { get; set; }
    public string QualificationName { get; set; } = string.Empty;
}

public class LeaderDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public List<int> QualificationIds { get; set; } = new();
    public List<string> Qualifications { get; set; } = new();
}

public class LeaderUpsertRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public List<int> QualificationIds { get; set; } = new();
}

public class WorkerDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string Status { get; set; } = "свободен";
}

public class WorkerUpsertRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string Status { get; set; } = "свободен";
}

public class ProjectDto
{
    public int Id { get; set; }
    public int ActivityId { get; set; }
    public string ActivityName { get; set; } = string.Empty;
    public string Customer { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? LeaderId { get; set; }
    public string? LeaderFullName { get; set; }
    public DateTime CreationDate { get; set; }
    public string Status { get; set; } = "в очереди";
    public DateTime? CompletionDate { get; set; }
}

public class ProjectDetailDto
{
    public int Id { get; set; }
    public int ActivityId { get; set; }
    public string ActivityName { get; set; } = string.Empty;
    public string Customer { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? LeaderId { get; set; }
    public string? LeaderFullName { get; set; }
    public List<string> LeaderQualifications { get; set; } = new();
    public List<WorkerDto> Workers { get; set; } = new();
    public string Status { get; set; } = "в очереди";
    public DateTime CreationDate { get; set; }
    public DateTime? CompletionDate { get; set; }
    public List<ProjectHistoryItemDto> History { get; set; } = new();
}

public class ProjectHistoryItemDto
{
    public int WorkerId { get; set; }
    public string WorkerFullName { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public DateTime? LeftAt { get; set; }
}

public class ProjectCreateRequest
{
    public int ActivityId { get; set; }
    public string Customer { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? LeaderId { get; set; }
    public List<int> WorkerIds { get; set; } = new();
}

public class ProjectUpdateRequest
{
    public int ActivityId { get; set; }
    public string Customer { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? LeaderId { get; set; }
    public List<int> WorkerIds { get; set; } = new();
}

public class ProjectListResponse
{
    public List<ProjectDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class CsvImportResponse
{
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class ProblemResponse
{
    public string? Title { get; set; }
    public string? Detail { get; set; }
    public string? Message { get; set; }
    public int? Status { get; set; }
}
