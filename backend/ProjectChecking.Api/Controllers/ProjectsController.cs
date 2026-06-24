using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectChecking.Api.Auth;
using ProjectChecking.Api.Dtos;
using ProjectChecking.Api.Services;
using Dapper;
using ProjectChecking.Api.Data;

namespace ProjectChecking.Api.Controllers;

[ApiController]
[Route("api/projects")]
public class ProjectsController : ControllerBase
{
    private readonly ProjectService _service;
    private readonly ActivityService _activityService;
    private readonly LeaderService _leaderService;
    private readonly WorkerService _workerService;
    private readonly UserService _userService;
    private readonly IDbConnectionFactory _dbFactory;

    public ProjectsController(
        ProjectService service,
        ActivityService activityService,
        LeaderService leaderService,
        WorkerService workerService,
        UserService userService,
        IDbConnectionFactory dbFactory)
    {
        _service = service;
        _activityService = activityService;
        _leaderService = leaderService;
        _workerService = workerService;
        _userService = userService;
        _dbFactory = dbFactory;
    }

    public class ListQuery
    {
        public string? Search { get; set; }
        public int? LeaderId { get; set; }
        public string? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string SortOrder { get; set; } = "desc";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int? WorkerId { get; set; }
        public bool ExcludeCompleted { get; set; }
        public bool IncludeOnlyCompleted { get; set; }
        public string? Scope { get; set; }
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ProjectListItemDto>>> List([FromQuery] ListQuery query)
    {
        var filter = new ProjectFilter
        {
            Search = query.Search,
            LeaderId = query.LeaderId,
            Status = query.Status,
            FromDate = query.FromDate,
            ToDate = query.ToDate,
            SortOrder = query.SortOrder,
            Page = query.Page < 1 ? 1 : query.Page,
            PageSize = query.PageSize <= 0 ? 20 : query.PageSize,
            WorkerId = query.WorkerId,
            ExcludeCompleted = query.ExcludeCompleted,
            IncludeOnlyCompleted = query.IncludeOnlyCompleted,
        };

        if (!string.IsNullOrEmpty(query.Scope))
        {
            var session = HttpContext.GetSession();
            if (session is null) return Unauthorized();
            var me = await _userService.GetByIdAsync(session.UserId);
            if (me is null) return Unauthorized();

            using var conn = _dbFactory.Create();
            if (string.Equals(query.Scope, "leader", StringComparison.OrdinalIgnoreCase))
            {
                var leaderId = await conn.ExecuteScalarAsync<int?>(
                    "SELECT TOP 1 Id FROM Leaders WHERE FullName = @FullName",
                    new { me.FullName });
                if (leaderId is null)
                {
                    return new PagedResult<ProjectListItemDto>(new List<ProjectListItemDto>(), 0, filter.Page, filter.PageSize);
                }
                filter.LeaderId = leaderId;
            }
            else if (string.Equals(query.Scope, "worker", StringComparison.OrdinalIgnoreCase))
            {
                var workerId = await conn.ExecuteScalarAsync<int?>(
                    "SELECT TOP 1 Id FROM Workers WHERE FullName = @FullName",
                    new { me.FullName });
                if (workerId is null)
                {
                    return new PagedResult<ProjectListItemDto>(new List<ProjectListItemDto>(), 0, filter.Page, filter.PageSize);
                }
                filter.WorkerId = workerId;
            }
        }

        return await _service.ListAsync(filter);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProjectDetailDto>> GetById(int id)
    {
        var project = await _service.GetByIdAsync(id);
        return project is null ? NotFound() : project;
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] CreateProjectRequest request)
    {
        try
        {
            return await _service.CreateAsync(request);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProjectRequest request)
    {
        try
        {
            await _service.UpdateAsync(id, request);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("{id:int}/start")]
    public async Task<IActionResult> Start(int id)
    {
        try
        {
            await _service.StartAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/complete")]
    public async Task<IActionResult> Complete(int id)
    {
        try
        {
            await _service.CompleteAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/uncomplete")]
    public async Task<IActionResult> Uncomplete(int id)
    {
        try
        {
            await _service.UncompleteAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    public class CsvImportResult
    {
        public int Imported { get; set; }
        public int Skipped { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    [HttpPost("import-csv")]
    public async Task<ActionResult<CsvImportResult>> ImportCsv(IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "Файл не передан." });
        }

        var result = new CsvImportResult();
        var activities = (await _activityService.GetAllAsync())
            .ToDictionary(a => a.Name.Trim().ToLowerInvariant(), a => a.Id);
        var activitiesById = activities.ToDictionary(kv => kv.Value, kv => kv.Key);
        var leaders = (await _leaderService.GetAllAsync())
            .ToDictionary(l => l.FullName.Trim().ToLowerInvariant(), l => l.Id);
        var workers = (await _workerService.GetAllAsync())
            .ToDictionary(w => w.FullName.Trim().ToLowerInvariant(), w => w.Id);

        using var reader = new StreamReader(file.OpenReadStream(), System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var configuration = new CsvConfiguration(CultureInfo.GetCultureInfo("ru-RU"))
        {
            Delimiter = ";",
            HasHeaderRecord = true,
            BadDataFound = null,
            MissingFieldFound = null,
            TrimOptions = TrimOptions.Trim,
        };
        using var csv = new CsvReader(reader, configuration);

        if (!await csv.ReadAsync())
        {
            return BadRequest(new { message = "Пустой CSV-файл." });
        }
        csv.ReadHeader();

        var requiredHeaders = new[] { "Тема", "Заказчик", "Описание", "Руководитель", "Работники" };
        var missing = requiredHeaders.Where(h => csv.HeaderRecord is null || !csv.HeaderRecord.Contains(h)).ToList();
        if (missing.Count > 0)
        {
            return BadRequest(new { message = "Не хватает колонок: " + string.Join(", ", missing) });
        }

        var row = 1;
        while (await csv.ReadAsync())
        {
            row++;
            try
            {
                var topic = csv.GetField("Тема")?.Trim() ?? string.Empty;
                var customer = csv.GetField("Заказчик")?.Trim() ?? string.Empty;
                var description = csv.GetField("Описание")?.Trim();
                var leaderName = csv.GetField("Руководитель")?.Trim();
                var workerNames = csv.GetField("Работники")?.Trim();

                if (string.IsNullOrEmpty(topic) || string.IsNullOrEmpty(customer))
                {
                    result.Skipped++;
                    result.Errors.Add($"Строка {row}: пропущена обязательная колонка.");
                    continue;
                }

                if (!activities.TryGetValue(topic.ToLowerInvariant(), out var activityId))
                {
                    result.Skipped++;
                    result.Errors.Add($"Строка {row}: неизвестный вид деятельности «{topic}».");
                    continue;
                }

                int? leaderId = null;
                if (!string.IsNullOrEmpty(leaderName))
                {
                    if (!leaders.TryGetValue(leaderName.ToLowerInvariant(), out var l))
                    {
                        result.Skipped++;
                        result.Errors.Add($"Строка {row}: неизвестный руководитель «{leaderName}».");
                        continue;
                    }
                    leaderId = l;
                }

                var workerIds = new List<int>();
                if (!string.IsNullOrEmpty(workerNames))
                {
                    foreach (var w in workerNames.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        if (!workers.TryGetValue(w.ToLowerInvariant(), out var wid))
                        {
                            result.Skipped++;
                            result.Errors.Add($"Строка {row}: неизвестный работник «{w}».");
                            workerIds = null!;
                            break;
                        }
                        workerIds.Add(wid);
                    }
                    if (workerIds is null)
                    {
                        continue;
                    }
                }

                try
                {
                    await _service.CreateAsync(new CreateProjectRequest(activityId, customer, description, leaderId, workerIds));
                    result.Imported++;
                }
                catch (InvalidOperationException ex)
                {
                    result.Skipped++;
                    result.Errors.Add($"Строка {row}: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                result.Skipped++;
                result.Errors.Add($"Строка {row}: {ex.Message}");
            }
        }

        return result;
    }
}
