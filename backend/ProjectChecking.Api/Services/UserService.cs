using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ProjectChecking.Api.Auth;
using ProjectChecking.Api.Data;
using ProjectChecking.Api.Dtos;
using ProjectChecking.Api.Models;

namespace ProjectChecking.Api.Services;

public class UserService
{
    private readonly IDbConnectionFactory _factory;

    public UserService(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<User?> FindByLoginAsync(string login)
    {
        using var conn = _factory.Create();
        return await conn.QuerySingleOrDefaultAsync<User>(
            @"SELECT Id, Login, PasswordHash, RoleId, FullName, Phone, BirthDate, RegistrationDate
              FROM Users WHERE Login = @Login",
            new { Login = login });
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        using var conn = _factory.Create();
        return await conn.QuerySingleOrDefaultAsync<User>(
            @"SELECT Id, Login, PasswordHash, RoleId, FullName, Phone, BirthDate, RegistrationDate
              FROM Users WHERE Id = @Id",
            new { Id = id });
    }

    public async Task<string> GetRoleNameAsync(int roleId)
    {
        using var conn = _factory.Create();
        return await conn.ExecuteScalarAsync<string>(
            "SELECT Name FROM Roles WHERE Id = @Id",
            new { Id = roleId }) ?? string.Empty;
    }

    public async Task<List<RoleDto>> GetRolesAsync()
    {
        using var conn = _factory.Create();
        var rows = await conn.QueryAsync<Role>("SELECT Id, Name FROM Roles ORDER BY Id");
        return rows.Select(r => new RoleDto(r.Id, r.Name)).ToList();
    }

    public async Task<List<UserListItemDto>> GetAllAsync()
    {
        using var conn = _factory.Create();
        var rows = await conn.QueryAsync<(int Id, string Login, string Role, string FullName, string? Phone, DateTime? BirthDate, DateTime RegistrationDate)>(
            @"SELECT u.Id, u.Login, r.Name AS Role, u.FullName, u.Phone, u.BirthDate, u.RegistrationDate
              FROM Users u JOIN Roles r ON r.Id = u.RoleId
              ORDER BY u.Id");
        return rows.Select(r => new UserListItemDto(r.Id, r.Login, r.Role, r.FullName, r.Phone, r.BirthDate, r.RegistrationDate)).ToList();
    }

    public async Task<int> CreateAsync(CreateUserRequest request)
    {
        using var conn = _factory.Create();
        var passwordHash = PasswordHasher.Hash(request.Password);
        return await conn.ExecuteScalarAsync<int>(
            @"INSERT INTO Users (Login, PasswordHash, RoleId, FullName, Phone, BirthDate, RegistrationDate)
              OUTPUT INSERTED.Id
              VALUES (@Login, @PasswordHash, @RoleId, @FullName, @Phone, @BirthDate, @Now)",
            new
            {
                request.Login,
                PasswordHash = passwordHash,
                request.RoleId,
                request.FullName,
                request.Phone,
                request.BirthDate,
                Now = DateTime.UtcNow,
            });
    }

    public async Task UpdateAsync(int id, UpdateUserRequest request)
    {
        using var conn = _factory.Create();
        if (!string.IsNullOrEmpty(request.Password))
        {
            var hash = PasswordHasher.Hash(request.Password);
            await conn.ExecuteAsync(
                @"UPDATE Users SET Login=@Login, RoleId=@RoleId, FullName=@FullName,
                    Phone=@Phone, BirthDate=@BirthDate, PasswordHash=@PasswordHash WHERE Id=@Id",
                new { Id = id, request.Login, request.RoleId, request.FullName, request.Phone, request.BirthDate, PasswordHash = hash });
        }
        else
        {
            await conn.ExecuteAsync(
                @"UPDATE Users SET Login=@Login, RoleId=@RoleId, FullName=@FullName,
                    Phone=@Phone, BirthDate=@BirthDate WHERE Id=@Id",
                new { Id = id, request.Login, request.RoleId, request.FullName, request.Phone, request.BirthDate });
        }
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _factory.Create();
        await conn.ExecuteAsync("DELETE FROM Users WHERE Id=@Id", new { Id = id });
    }

    public async Task UpdateMeAsync(int userId, UpdateMeRequest request)
    {
        using var conn = _factory.Create();
        if (!string.IsNullOrEmpty(request.NewPassword))
        {
            if (request.NewPassword != request.ConfirmPassword)
            {
                throw new InvalidOperationException("Пароли не совпадают.");
            }
            var hash = PasswordHasher.Hash(request.NewPassword);
            await conn.ExecuteAsync(
                @"UPDATE Users SET FullName=@FullName, Phone=@Phone, BirthDate=@BirthDate, PasswordHash=@PasswordHash WHERE Id=@Id",
                new { Id = userId, request.FullName, request.Phone, request.BirthDate, PasswordHash = hash });
        }
        else
        {
            await conn.ExecuteAsync(
                @"UPDATE Users SET FullName=@FullName, Phone=@Phone, BirthDate=@BirthDate WHERE Id=@Id",
                new { Id = userId, request.FullName, request.Phone, request.BirthDate });
        }
    }
}
