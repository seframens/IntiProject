using System;
using System.Collections.Generic;

namespace ProjectChecking.Api.Dtos;

public record LoginRequest(string Login, string Password);

public record LoginResponse(string Token, int UserId, string Login, string Role, string FullName);

public record CurrentUserDto(int Id, string Login, string Role, string FullName, string? Phone, DateTime? BirthDate, DateTime RegistrationDate);

public record UpdateMeRequest(string FullName, string? Phone, DateTime? BirthDate, string? NewPassword, string? ConfirmPassword);

public record RoleDto(int Id, string Name);

public record UserListItemDto(int Id, string Login, string Role, string FullName, string? Phone, DateTime? BirthDate, DateTime RegistrationDate);

public record CreateUserRequest(string Login, string Password, int RoleId, string FullName, string? Phone, DateTime? BirthDate);

public record UpdateUserRequest(string Login, int RoleId, string FullName, string? Phone, DateTime? BirthDate, string? Password);

public record ActivityDto(int Id, string Name, int? QualificationId, string? QualificationName);

public record CreateActivityRequest(string Name, int? QualificationId);

public record QualificationDto(int Id, string Name);

public record CreateQualificationRequest(string Name);

public record LeaderDto(int Id, string FullName, string? Phone, string? Email, List<int> QualificationIds, List<string> Qualifications);

public record CreateLeaderRequest(string FullName, string? Phone, string? Email, List<int> QualificationIds);

public record WorkerDto(int Id, string FullName, string? Phone, string? Email, string Status);

public record CreateWorkerRequest(string FullName, string? Phone, string? Email, string Status);

public record ProjectListItemDto(
    int Id,
    int ActivityId,
    string ActivityName,
    string Customer,
    int? LeaderId,
    string? LeaderFullName,
    string Status,
    DateTime CreationDate,
    DateTime? CompletionDate);

public record ProjectDetailDto(
    int Id,
    int ActivityId,
    string ActivityName,
    string Customer,
    string? Description,
    int? LeaderId,
    string? LeaderFullName,
    List<string> LeaderQualifications,
    List<WorkerDto> Workers,
    string Status,
    DateTime CreationDate,
    DateTime? CompletionDate,
    List<ProjectHistoryItemDto> History);

public record ProjectHistoryItemDto(int WorkerId, string WorkerFullName, DateTime JoinedAt, DateTime? LeftAt);

public record CreateProjectRequest(int ActivityId, string Customer, string? Description, int? LeaderId, List<int> WorkerIds);

public record UpdateProjectRequest(int ActivityId, string Customer, string? Description, int? LeaderId, List<int> WorkerIds);

public record PagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize);

public record ActivityQualificationDto(int ActivityId, string ActivityName, int QualificationId, string QualificationName);

public record CreateActivityQualificationRequest(int ActivityId, int QualificationId);
