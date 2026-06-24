using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ProjectChecking.Api.Data;
using ProjectChecking.Api.Dtos;

namespace ProjectChecking.Api.Services;

public class ActivityService
{
    private readonly IDbConnectionFactory _factory;
    public ActivityService(IDbConnectionFactory factory) { _factory = factory; }

    public async Task<List<ActivityDto>> GetAllAsync()
    {
        using var conn = _factory.Create();
        var rows = await conn.QueryAsync<ActivityDto>(
            @"SELECT a.Id, a.Name,
                     (SELECT TOP 1 q.Id FROM ActivityQualifications aq
                      JOIN Qualifications q ON q.Id = aq.QualificationId
                      WHERE aq.ActivityId = a.Id ORDER BY q.Name) AS QualificationId,
                     (SELECT TOP 1 q.Name FROM ActivityQualifications aq
                      JOIN Qualifications q ON q.Id = aq.QualificationId
                      WHERE aq.ActivityId = a.Id ORDER BY q.Name) AS QualificationName
              FROM Activities a ORDER BY a.Id");
        return rows.ToList();
    }

    public async Task<int> CreateAsync(CreateActivityRequest req)
    {
        using var conn = (Microsoft.Data.SqlClient.SqlConnection)_factory.Create();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();
        var id = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "INSERT INTO Activities (Name) OUTPUT INSERTED.Id VALUES (@Name)",
            new { req.Name }, tx));
        if (req.QualificationId.HasValue && req.QualificationId.Value > 0)
        {
            await conn.ExecuteAsync(new CommandDefinition(
                "INSERT INTO ActivityQualifications (ActivityId, QualificationId) VALUES (@A, @Q)",
                new { A = id, Q = req.QualificationId.Value }, tx));
        }
        tx.Commit();
        return id;
    }

    public async Task UpdateAsync(int id, CreateActivityRequest req)
    {
        using var conn = (Microsoft.Data.SqlClient.SqlConnection)_factory.Create();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();
        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE Activities SET Name=@Name WHERE Id=@Id", new { Id = id, req.Name }, tx));
        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM ActivityQualifications WHERE ActivityId=@Id", new { Id = id }, tx));
        if (req.QualificationId.HasValue && req.QualificationId.Value > 0)
        {
            await conn.ExecuteAsync(new CommandDefinition(
                "INSERT INTO ActivityQualifications (ActivityId, QualificationId) VALUES (@A, @Q)",
                new { A = id, Q = req.QualificationId.Value }, tx));
        }
        tx.Commit();
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _factory.Create();
        await conn.ExecuteAsync("DELETE FROM ActivityQualifications WHERE ActivityId=@Id", new { Id = id });
        await conn.ExecuteAsync("DELETE FROM Activities WHERE Id=@Id", new { Id = id });
    }

    public async Task<List<ActivityQualificationDto>> GetActivityQualificationsAsync()
    {
        using var conn = _factory.Create();
        var rows = await conn.QueryAsync<ActivityQualificationDto>(
            @"SELECT aq.ActivityId, a.Name AS ActivityName, aq.QualificationId, q.Name AS QualificationName
              FROM ActivityQualifications aq
              JOIN Activities a ON a.Id = aq.ActivityId
              JOIN Qualifications q ON q.Id = aq.QualificationId
              ORDER BY a.Name, q.Name");
        return rows.ToList();
    }

    public async Task<List<QualificationDto>> GetQualificationsForActivityAsync(int activityId)
    {
        using var conn = _factory.Create();
        var rows = await conn.QueryAsync<QualificationDto>(
            @"SELECT q.Id, q.Name FROM ActivityQualifications aq
              JOIN Qualifications q ON q.Id = aq.QualificationId
              WHERE aq.ActivityId = @Id
              ORDER BY q.Name",
            new { Id = activityId });
        return rows.ToList();
    }

    public async Task<List<LeaderDto>> GetLeadersForActivityAsync(int activityId)
    {
        using var conn = _factory.Create();
        var leaders = (await conn.QueryAsync<(int Id, string FullName, string? Phone, string? Email)>(
            @"SELECT DISTINCT l.Id, l.FullName, l.Phone, l.Email
              FROM Leaders l
              JOIN LeaderQualifications lq ON lq.LeaderId = l.Id
              JOIN ActivityQualifications aq ON aq.QualificationId = lq.QualificationId
              WHERE aq.ActivityId = @Id
              ORDER BY l.FullName",
            new { Id = activityId })).ToList();

        var result = new List<LeaderDto>();
        foreach (var l in leaders)
        {
            var qs = (await conn.QueryAsync<(int Id, string Name)>(
                @"SELECT q.Id, q.Name FROM LeaderQualifications lq
                  JOIN Qualifications q ON q.Id = lq.QualificationId
                  WHERE lq.LeaderId = @Id ORDER BY q.Name",
                new { Id = l.Id })).ToList();
            result.Add(new LeaderDto(l.Id, l.FullName, l.Phone, l.Email,
                qs.Select(x => x.Id).ToList(), qs.Select(x => x.Name).ToList()));
        }
        return result;
    }

    public async Task LinkAsync(CreateActivityQualificationRequest req)
    {
        using var conn = _factory.Create();
        await conn.ExecuteAsync(
            @"IF NOT EXISTS (SELECT 1 FROM ActivityQualifications WHERE ActivityId=@ActivityId AND QualificationId=@QualificationId)
                INSERT INTO ActivityQualifications (ActivityId, QualificationId) VALUES (@ActivityId, @QualificationId)",
            new { req.ActivityId, req.QualificationId });
    }

    public async Task UnlinkAsync(int activityId, int qualificationId)
    {
        using var conn = _factory.Create();
        await conn.ExecuteAsync(
            "DELETE FROM ActivityQualifications WHERE ActivityId=@A AND QualificationId=@Q",
            new { A = activityId, Q = qualificationId });
    }
}

public class QualificationService
{
    private readonly IDbConnectionFactory _factory;
    public QualificationService(IDbConnectionFactory factory) { _factory = factory; }

    public async Task<List<QualificationDto>> GetAllAsync()
    {
        using var conn = _factory.Create();
        var rows = await conn.QueryAsync<QualificationDto>("SELECT Id, Name FROM Qualifications ORDER BY Id");
        return rows.ToList();
    }

    public async Task<int> CreateAsync(CreateQualificationRequest req)
    {
        using var conn = _factory.Create();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO Qualifications (Name) OUTPUT INSERTED.Id VALUES (@Name)", new { req.Name });
    }

    public async Task UpdateAsync(int id, CreateQualificationRequest req)
    {
        using var conn = _factory.Create();
        await conn.ExecuteAsync("UPDATE Qualifications SET Name=@Name WHERE Id=@Id", new { Id = id, req.Name });
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _factory.Create();
        await conn.ExecuteAsync("DELETE FROM LeaderQualifications WHERE QualificationId=@Id", new { Id = id });
        await conn.ExecuteAsync("DELETE FROM ActivityQualifications WHERE QualificationId=@Id", new { Id = id });
        await conn.ExecuteAsync("DELETE FROM Qualifications WHERE Id=@Id", new { Id = id });
    }
}

public class LeaderService
{
    private readonly IDbConnectionFactory _factory;
    public LeaderService(IDbConnectionFactory factory) { _factory = factory; }

    public async Task<List<LeaderDto>> GetAllAsync()
    {
        using var conn = _factory.Create();
        var leaders = (await conn.QueryAsync<(int Id, string FullName, string? Phone, string? Email)>(
            "SELECT Id, FullName, Phone, Email FROM Leaders ORDER BY Id")).ToList();
        var result = new List<LeaderDto>();
        foreach (var l in leaders)
        {
            var qs = (await conn.QueryAsync<(int Id, string Name)>(
                @"SELECT q.Id, q.Name FROM LeaderQualifications lq
                  JOIN Qualifications q ON q.Id = lq.QualificationId
                  WHERE lq.LeaderId=@Id ORDER BY q.Name",
                new { Id = l.Id })).ToList();
            result.Add(new LeaderDto(l.Id, l.FullName, l.Phone, l.Email,
                qs.Select(x => x.Id).ToList(), qs.Select(x => x.Name).ToList()));
        }
        return result;
    }

    public async Task<LeaderDto?> GetByIdAsync(int id)
    {
        using var conn = _factory.Create();
        var l = await conn.QuerySingleOrDefaultAsync<(int Id, string FullName, string? Phone, string? Email)>(
            "SELECT Id, FullName, Phone, Email FROM Leaders WHERE Id=@Id", new { Id = id });
        if (l.Equals(default((int, string, string?, string?))) && l.Id == 0) return null;
        var qs = (await conn.QueryAsync<(int Id, string Name)>(
            @"SELECT q.Id, q.Name FROM LeaderQualifications lq
              JOIN Qualifications q ON q.Id = lq.QualificationId
              WHERE lq.LeaderId=@Id ORDER BY q.Name",
            new { Id = id })).ToList();
        return new LeaderDto(l.Id, l.FullName, l.Phone, l.Email,
            qs.Select(x => x.Id).ToList(), qs.Select(x => x.Name).ToList());
    }

    public async Task<int> CreateAsync(CreateLeaderRequest req)
    {
        using var conn = (Microsoft.Data.SqlClient.SqlConnection)_factory.Create();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();
        var id = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "INSERT INTO Leaders (FullName, Phone, Email) OUTPUT INSERTED.Id VALUES (@FullName, @Phone, @Email)",
            new { req.FullName, req.Phone, req.Email }, tx));
        foreach (var q in req.QualificationIds.Distinct())
        {
            await conn.ExecuteAsync(new CommandDefinition(
                "INSERT INTO LeaderQualifications (LeaderId, QualificationId) VALUES (@LeaderId, @QualificationId)",
                new { LeaderId = id, QualificationId = q }, tx));
        }
        tx.Commit();
        return id;
    }

    public async Task UpdateAsync(int id, CreateLeaderRequest req)
    {
        using var conn = (Microsoft.Data.SqlClient.SqlConnection)_factory.Create();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();
        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE Leaders SET FullName=@FullName, Phone=@Phone, Email=@Email WHERE Id=@Id",
            new { Id = id, req.FullName, req.Phone, req.Email }, tx));
        await conn.ExecuteAsync(new CommandDefinition("DELETE FROM LeaderQualifications WHERE LeaderId=@Id", new { Id = id }, tx));
        foreach (var q in req.QualificationIds.Distinct())
        {
            await conn.ExecuteAsync(new CommandDefinition(
                "INSERT INTO LeaderQualifications (LeaderId, QualificationId) VALUES (@LeaderId, @QualificationId)",
                new { LeaderId = id, QualificationId = q }, tx));
        }
        tx.Commit();
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _factory.Create();
        await conn.ExecuteAsync("UPDATE Projects SET LeaderId=NULL WHERE LeaderId=@Id", new { Id = id });
        await conn.ExecuteAsync("DELETE FROM LeaderQualifications WHERE LeaderId=@Id", new { Id = id });
        await conn.ExecuteAsync("DELETE FROM Leaders WHERE Id=@Id", new { Id = id });
    }
}

public class WorkerService
{
    private readonly IDbConnectionFactory _factory;
    public WorkerService(IDbConnectionFactory factory) { _factory = factory; }

    public async Task<List<WorkerDto>> GetAllAsync(bool freeOnly = false)
    {
        using var conn = _factory.Create();
        var sql = "SELECT Id, FullName, Phone, Email, Status FROM Workers";
        if (freeOnly)
        {
            sql += " WHERE Status = N'свободен'";
        }
        sql += " ORDER BY Id";
        var rows = await conn.QueryAsync<WorkerDto>(sql);
        return rows.ToList();
    }

    public async Task<int> CreateAsync(CreateWorkerRequest req)
    {
        using var conn = _factory.Create();
        return await conn.ExecuteScalarAsync<int>(
            @"INSERT INTO Workers (FullName, Phone, Email, Status) OUTPUT INSERTED.Id
              VALUES (@FullName, @Phone, @Email, @Status)",
            new { req.FullName, req.Phone, req.Email, Status = string.IsNullOrEmpty(req.Status) ? "свободен" : req.Status });
    }

    public async Task UpdateAsync(int id, CreateWorkerRequest req)
    {
        using var conn = _factory.Create();
        await conn.ExecuteAsync(
            @"UPDATE Workers SET FullName=@FullName, Phone=@Phone, Email=@Email, Status=@Status WHERE Id=@Id",
            new { Id = id, req.FullName, req.Phone, req.Email, Status = string.IsNullOrEmpty(req.Status) ? "свободен" : req.Status });
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = (Microsoft.Data.SqlClient.SqlConnection)_factory.Create();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();
        await conn.ExecuteAsync(new CommandDefinition("DELETE FROM ProjectWorkers WHERE WorkerId=@Id", new { Id = id }, tx));
        await conn.ExecuteAsync(new CommandDefinition("DELETE FROM ProjectHistory WHERE WorkerId=@Id", new { Id = id }, tx));
        await conn.ExecuteAsync(new CommandDefinition("DELETE FROM Workers WHERE Id=@Id", new { Id = id }, tx));
        tx.Commit();
    }
}

public class RoleService
{
    private readonly IDbConnectionFactory _factory;
    public RoleService(IDbConnectionFactory factory) { _factory = factory; }

    public async Task<List<RoleDto>> GetAllAsync()
    {
        using var conn = _factory.Create();
        return (await conn.QueryAsync<RoleDto>("SELECT Id, Name FROM Roles ORDER BY Id")).ToList();
    }

    public async Task<int> CreateAsync(string name)
    {
        using var conn = _factory.Create();
        return await conn.ExecuteScalarAsync<int>("INSERT INTO Roles (Name) OUTPUT INSERTED.Id VALUES (@Name)", new { Name = name });
    }

    public async Task UpdateAsync(int id, string name)
    {
        using var conn = _factory.Create();
        await conn.ExecuteAsync("UPDATE Roles SET Name=@Name WHERE Id=@Id", new { Id = id, Name = name });
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _factory.Create();
        await conn.ExecuteAsync("DELETE FROM Roles WHERE Id=@Id", new { Id = id });
    }
}
