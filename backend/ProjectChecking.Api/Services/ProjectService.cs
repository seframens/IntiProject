using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ProjectChecking.Api.Data;
using ProjectChecking.Api.Dtos;

namespace ProjectChecking.Api.Services;

public class ProjectFilter
{
    public string? Search { get; set; }
    public int? LeaderId { get; set; }
    public string? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string SortOrder { get; set; } = "desc"; // asc | desc
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int? WorkerId { get; set; }
    public int? OnlyLeaderId { get; set; } 
    public bool ExcludeCompleted { get; set; }
    public bool IncludeOnlyCompleted { get; set; }
    public bool DisablePaging { get; set; }
}

public class ProjectService
{
    private readonly IDbConnectionFactory _factory;

    public ProjectService(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<PagedResult<ProjectListItemDto>> ListAsync(ProjectFilter filter)
    {
        using var conn = _factory.Create();
        var where = new List<string>();
        var p = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            where.Add(@"(a.Name LIKE @Search OR p.Customer LIKE @Search OR ISNULL(l.FullName, N'') LIKE @Search OR ISNULL(p.Description, N'') LIKE @Search)");
            p.Add("Search", "%" + filter.Search.Trim() + "%");
        }

        if (filter.LeaderId.HasValue)
        {
            where.Add("p.LeaderId = @LeaderId");
            p.Add("LeaderId", filter.LeaderId.Value);
        }

        if (filter.OnlyLeaderId.HasValue)
        {
            where.Add("p.LeaderId = @OnlyLeaderId");
            p.Add("OnlyLeaderId", filter.OnlyLeaderId.Value);
        }

        if (!string.IsNullOrEmpty(filter.Status))
        {
            where.Add("p.Status = @Status");
            p.Add("Status", filter.Status);
        }

        if (filter.FromDate.HasValue)
        {
            where.Add("p.CreationDate >= @FromDate");
            p.Add("FromDate", filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            where.Add("p.CreationDate < @ToDate");
            p.Add("ToDate", filter.ToDate.Value.Date.AddDays(1));
        }

        if (filter.WorkerId.HasValue)
        {
            where.Add(@"EXISTS (
                SELECT 1 FROM ProjectWorkers pw WHERE pw.ProjectId = p.Id AND pw.WorkerId = @WorkerId
                UNION
                SELECT 1 FROM ProjectHistory ph WHERE ph.ProjectId = p.Id AND ph.WorkerId = @WorkerId
            )");
            p.Add("WorkerId", filter.WorkerId.Value);
        }

        if (filter.ExcludeCompleted)
        {
            where.Add("p.Status <> N'выполнен'");
        }

        if (filter.IncludeOnlyCompleted)
        {
            where.Add("p.Status = N'выполнен'");
        }

        var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : string.Empty;
        var sortDirection = filter.SortOrder?.ToLowerInvariant() == "asc" ? "ASC" : "DESC";

        var totalSql = $@"
            SELECT COUNT(*)
            FROM Projects p
            LEFT JOIN Leaders l ON l.Id = p.LeaderId
            JOIN Activities a ON a.Id = p.ActivityId
            {whereClause};";

        var total = await conn.ExecuteScalarAsync<int>(totalSql, p);

        string itemsSql;
        if (filter.DisablePaging)
        {
            itemsSql = $@"
                SELECT p.Id, p.ActivityId, a.Name AS ActivityName, p.Customer,
                       p.LeaderId, l.FullName AS LeaderFullName,
                       p.Status, p.CreationDate, p.CompletionDate
                FROM Projects p
                LEFT JOIN Leaders l ON l.Id = p.LeaderId
                JOIN Activities a ON a.Id = p.ActivityId
                {whereClause}
                ORDER BY p.CreationDate {sortDirection}, p.Id {sortDirection};";
        }
        else
        {
            p.Add("Offset", (Math.Max(1, filter.Page) - 1) * filter.PageSize);
            p.Add("Take", filter.PageSize);
            itemsSql = $@"
                SELECT p.Id, p.ActivityId, a.Name AS ActivityName, p.Customer,
                       p.LeaderId, l.FullName AS LeaderFullName,
                       p.Status, p.CreationDate, p.CompletionDate
                FROM Projects p
                LEFT JOIN Leaders l ON l.Id = p.LeaderId
                JOIN Activities a ON a.Id = p.ActivityId
                {whereClause}
                ORDER BY p.CreationDate {sortDirection}, p.Id {sortDirection}
                OFFSET @Offset ROWS FETCH NEXT @Take ROWS ONLY;";
        }

        var rows = await conn.QueryAsync<ProjectListItemDto>(itemsSql, p);
        return new PagedResult<ProjectListItemDto>(rows.ToList(), total, filter.Page, filter.PageSize);
    }

    public async Task<ProjectDetailDto?> GetByIdAsync(int id)
    {
        using var conn = _factory.Create();
        var header = await conn.QuerySingleOrDefaultAsync(
            @"SELECT p.Id, p.ActivityId, a.Name AS ActivityName, p.Customer, p.Description,
                     p.LeaderId, l.FullName AS LeaderFullName,
                     p.Status, p.CreationDate, p.CompletionDate
              FROM Projects p
              LEFT JOIN Leaders l ON l.Id = p.LeaderId
              JOIN Activities a ON a.Id = p.ActivityId
              WHERE p.Id = @Id",
            new { Id = id });
        if (header is null)
        {
            return null;
        }

        List<string> qualifications = new();
        if (header.LeaderId is int leaderId)
        {
            var qs = await conn.QueryAsync<string>(
                @"SELECT q.Name FROM LeaderQualifications lq
                  JOIN Qualifications q ON q.Id = lq.QualificationId
                  WHERE lq.LeaderId = @LeaderId
                  ORDER BY q.Name",
                new { LeaderId = leaderId });
            qualifications = qs.ToList();
        }

        var workers = (await conn.QueryAsync<WorkerDto>(
            @"SELECT w.Id, w.FullName, w.Phone, w.Email, w.Status
              FROM ProjectWorkers pw JOIN Workers w ON w.Id = pw.WorkerId
              WHERE pw.ProjectId = @Id
              ORDER BY w.FullName",
            new { Id = id })).ToList();

        var history = (await conn.QueryAsync(
            @"SELECT ph.WorkerId, w.FullName AS WorkerFullName, ph.JoinedAt, ph.LeftAt
              FROM ProjectHistory ph JOIN Workers w ON w.Id = ph.WorkerId
              WHERE ph.ProjectId = @Id AND ph.WorkerId IS NOT NULL
              ORDER BY ph.JoinedAt",
            new { Id = id })).Select(r => new ProjectHistoryItemDto(
                (int)r.WorkerId,
                (string)r.WorkerFullName,
                (DateTime)r.JoinedAt,
                (DateTime?)r.LeftAt)).ToList();

        return new ProjectDetailDto(
            (int)header.Id,
            (int)header.ActivityId,
            (string)header.ActivityName,
            (string)header.Customer,
            (string?)header.Description,
            (int?)header.LeaderId,
            (string?)header.LeaderFullName,
            qualifications,
            workers,
            (string)header.Status,
            (DateTime)header.CreationDate,
            (DateTime?)header.CompletionDate,
            history);
    }

    public async Task<int> CreateAsync(CreateProjectRequest request)
    {
        using var conn = (Microsoft.Data.SqlClient.SqlConnection)_factory.Create();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        await ValidateLeaderForActivityAsync(conn, tx, request.LeaderId, request.ActivityId);
        await ValidateWorkersAvailableAsync(conn, tx, request.WorkerIds);

        var id = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(
                @"INSERT INTO Projects (ActivityId, Customer, Description, LeaderId, CreationDate, Status, CompletionDate)
                  OUTPUT INSERTED.Id
                  VALUES (@ActivityId, @Customer, @Description, @LeaderId, @CreationDate, N'в очереди', NULL)",
                new
                {
                    request.ActivityId,
                    request.Customer,
                    request.Description,
                    request.LeaderId,
                    CreationDate = DateTime.UtcNow,
                },
                tx));

        await AssignWorkersAsync(conn, tx, id, request.WorkerIds, joining: true);
        tx.Commit();
        return id;
    }

    public async Task UpdateAsync(int id, UpdateProjectRequest request)
    {
        using var conn = (Microsoft.Data.SqlClient.SqlConnection)_factory.Create();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        var current = await conn.QuerySingleOrDefaultAsync<(string Status, int? LeaderId, int ActivityId)>(
            new CommandDefinition("SELECT Status, LeaderId, ActivityId FROM Projects WHERE Id = @Id", new { Id = id }, tx));
        if (current.Equals(default((string, int?, int))))
        {
            throw new InvalidOperationException("Проект не найден.");
        }

        await ValidateLeaderForActivityAsync(conn, tx, request.LeaderId, request.ActivityId);

        var existingWorkerIds = (await conn.QueryAsync<int>(
            new CommandDefinition("SELECT WorkerId FROM ProjectWorkers WHERE ProjectId = @Id", new { Id = id }, tx))).ToHashSet();
        var newWorkerIds = request.WorkerIds.ToHashSet();
        var toAdd = newWorkerIds.Except(existingWorkerIds).ToList();
        var toRemove = existingWorkerIds.Except(newWorkerIds).ToList();
        await ValidateWorkersAvailableAsync(conn, tx, toAdd);

        await conn.ExecuteAsync(new CommandDefinition(
            @"UPDATE Projects
              SET ActivityId=@ActivityId, Customer=@Customer, Description=@Description, LeaderId=@LeaderId
              WHERE Id=@Id",
            new { Id = id, request.ActivityId, request.Customer, request.Description, request.LeaderId },
            tx));

        if (toRemove.Count > 0)
        {
            await RemoveWorkersAsync(conn, tx, id, toRemove);
        }
        if (toAdd.Count > 0)
        {
            await AssignWorkersAsync(conn, tx, id, toAdd, joining: true);
        }

        tx.Commit();
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = (Microsoft.Data.SqlClient.SqlConnection)_factory.Create();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();
        var workerIds = (await conn.QueryAsync<int>(
            new CommandDefinition("SELECT WorkerId FROM ProjectWorkers WHERE ProjectId = @Id", new { Id = id }, tx))).ToList();
        if (workerIds.Count > 0)
        {
            await RemoveWorkersAsync(conn, tx, id, workerIds);
        }
        await conn.ExecuteAsync(new CommandDefinition("DELETE FROM ProjectHistory WHERE ProjectId = @Id", new { Id = id }, tx));
        await conn.ExecuteAsync(new CommandDefinition("DELETE FROM Projects WHERE Id = @Id", new { Id = id }, tx));
        tx.Commit();
    }

    public async Task SetStatusAsync(int id, string newStatus)
    {
        using var conn = _factory.Create();
        if (newStatus == "выполнен")
        {
            await conn.ExecuteAsync(
                @"UPDATE Projects SET Status = @Status, CompletionDate = @Date WHERE Id = @Id",
                new { Id = id, Status = newStatus, Date = DateTime.UtcNow });
        }
        else
        {
            await conn.ExecuteAsync(
                @"UPDATE Projects SET Status = @Status, CompletionDate = NULL WHERE Id = @Id",
                new { Id = id, Status = newStatus });
        }
    }

    public async Task<string?> GetStatusAsync(int id)
    {
        using var conn = _factory.Create();
        return await conn.ExecuteScalarAsync<string?>("SELECT Status FROM Projects WHERE Id = @Id", new { Id = id });
    }

    public async Task StartAsync(int id)
    {
        using var conn = _factory.Create();
        var status = await conn.ExecuteScalarAsync<string?>("SELECT Status FROM Projects WHERE Id = @Id", new { Id = id });
        if (status is null)
        {
            throw new InvalidOperationException("Проект не найден.");
        }
        if (status != "в очереди")
        {
            throw new InvalidOperationException("Можно запустить только проект со статусом «в очереди».");
        }
        await conn.ExecuteAsync(
            @"UPDATE Projects SET Status = N'активен' WHERE Id = @Id",
            new { Id = id });
    }

    public async Task CompleteAsync(int id)
    {
        using var conn = (Microsoft.Data.SqlClient.SqlConnection)_factory.Create();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();
        var current = await conn.QuerySingleOrDefaultAsync<string?>(
            new CommandDefinition("SELECT Status FROM Projects WHERE Id = @Id", new { Id = id }, tx));
        if (current is null)
        {
            throw new InvalidOperationException("Проект не найден.");
        }
        if (current == "выполнен")
        {
            tx.Commit();
            return;
        }

        var workerIds = (await conn.QueryAsync<int>(
            new CommandDefinition("SELECT WorkerId FROM ProjectWorkers WHERE ProjectId = @Id", new { Id = id }, tx))).ToList();
        if (workerIds.Count > 0)
        {
            await RemoveWorkersAsync(conn, tx, id, workerIds);
        }

        await conn.ExecuteAsync(new CommandDefinition(
            @"UPDATE Projects SET Status = N'выполнен', CompletionDate = @Date WHERE Id = @Id",
            new { Id = id, Date = DateTime.UtcNow },
            tx));
        tx.Commit();
    }

    public async Task UncompleteAsync(int id)
    {
        using var conn = (Microsoft.Data.SqlClient.SqlConnection)_factory.Create();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();
        var current = await conn.QuerySingleOrDefaultAsync<string?>(
            new CommandDefinition("SELECT Status FROM Projects WHERE Id = @Id", new { Id = id }, tx));
        if (current is null)
        {
            throw new InvalidOperationException("Проект не найден.");
        }
        if (current != "выполнен")
        {
            throw new InvalidOperationException("Отменить можно только завершённый проект.");
        }

        var lastWorkerIds = (await conn.QueryAsync<int>(new CommandDefinition(
            @"SELECT DISTINCT WorkerId
              FROM ProjectHistory
              WHERE ProjectId = @Id
                AND WorkerId IS NOT NULL
                AND LeftAt IS NOT NULL
                AND LeftAt = (SELECT MAX(LeftAt) FROM ProjectHistory WHERE ProjectId = @Id AND WorkerId IS NOT NULL)",
            new { Id = id }, tx))).ToList();

        if (lastWorkerIds.Count > 0)
        {
            var blocked = (await conn.QueryAsync<(int Id, string Status, string FullName)>(new CommandDefinition(
                "SELECT Id, Status, FullName FROM Workers WHERE Id IN @Ids",
                new { Ids = lastWorkerIds }, tx))).ToList();
            var busy = blocked.Where(b => b.Status == "занят").ToList();
            if (busy.Count > 0)
            {
                var names = string.Join(", ", busy.Select(b => b.FullName));
                throw new InvalidOperationException($"Работники уже заняты другим проектом: {names}.");
            }
        }

        await conn.ExecuteAsync(new CommandDefinition(
            @"UPDATE Projects SET Status = N'активен', CompletionDate = NULL WHERE Id = @Id",
            new { Id = id }, tx));

        if (lastWorkerIds.Count > 0)
        {
            await AssignWorkersAsync(conn, tx, id, lastWorkerIds, joining: true);
        }

        tx.Commit();
    }

    private static async Task ValidateLeaderForActivityAsync(IDbConnection conn, IDbTransaction tx, int? leaderId, int activityId)
    {
        if (!leaderId.HasValue)
        {
            return;
        }
        var matches = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            @"SELECT COUNT(1)
              FROM ActivityQualifications aq
              JOIN LeaderQualifications lq ON lq.QualificationId = aq.QualificationId
              WHERE aq.ActivityId = @ActivityId AND lq.LeaderId = @LeaderId",
            new { ActivityId = activityId, LeaderId = leaderId.Value },
            tx));
        if (matches == 0)
        {
            throw new InvalidOperationException("Квалификация руководителя не соответствует выбранному виду деятельности.");
        }
    }

    private static async Task ValidateWorkersAvailableAsync(IDbConnection conn, IDbTransaction tx, IReadOnlyCollection<int> workerIds)
    {
        if (workerIds.Count == 0)
        {
            return;
        }
        var busy = (await conn.QueryAsync<(int Id, string FullName)>(new CommandDefinition(
            "SELECT Id, FullName FROM Workers WHERE Id IN @Ids AND Status = N'занят'",
            new { Ids = workerIds }, tx))).ToList();
        if (busy.Count > 0)
        {
            var names = string.Join(", ", busy.Select(b => b.FullName));
            throw new InvalidOperationException($"Работники уже заняты другим проектом: {names}.");
        }
    }

    private static async Task AssignWorkersAsync(IDbConnection conn, IDbTransaction tx, int projectId, IReadOnlyCollection<int> workerIds, bool joining)
    {
        if (workerIds.Count == 0)
        {
            return;
        }
        var now = DateTime.UtcNow;
        foreach (var wid in workerIds)
        {
            await conn.ExecuteAsync(new CommandDefinition(
                "INSERT INTO ProjectWorkers (ProjectId, WorkerId) VALUES (@ProjectId, @WorkerId)",
                new { ProjectId = projectId, WorkerId = wid }, tx));
            await conn.ExecuteAsync(new CommandDefinition(
                "UPDATE Workers SET Status = N'занят' WHERE Id = @Id",
                new { Id = wid }, tx));
            if (joining)
            {
                await conn.ExecuteAsync(new CommandDefinition(
                    "INSERT INTO ProjectHistory (ProjectId, WorkerId, JoinedAt) VALUES (@ProjectId, @WorkerId, @Now)",
                    new { ProjectId = projectId, WorkerId = wid, Now = now }, tx));
            }
        }
    }

    private static async Task RemoveWorkersAsync(IDbConnection conn, IDbTransaction tx, int projectId, IReadOnlyCollection<int> workerIds)
    {
        if (workerIds.Count == 0)
        {
            return;
        }
        var now = DateTime.UtcNow;
        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM ProjectWorkers WHERE ProjectId = @ProjectId AND WorkerId IN @Ids",
            new { ProjectId = projectId, Ids = workerIds }, tx));
        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE Workers SET Status = N'свободен' WHERE Id IN @Ids",
            new { Ids = workerIds }, tx));
        await conn.ExecuteAsync(new CommandDefinition(
            @"UPDATE ProjectHistory SET LeftAt = @Now
              WHERE ProjectId = @ProjectId AND WorkerId IN @Ids AND LeftAt IS NULL",
            new { ProjectId = projectId, Ids = workerIds, Now = now }, tx));
    }
}
