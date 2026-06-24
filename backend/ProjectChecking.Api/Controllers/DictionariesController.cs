using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ProjectChecking.Api.Dtos;
using ProjectChecking.Api.Services;

namespace ProjectChecking.Api.Controllers;

[ApiController]
[Route("api/activities")]
public class ActivitiesController : ControllerBase
{
    private readonly ActivityService _service;
    public ActivitiesController(ActivityService service) { _service = service; }

    [HttpGet]
    public async Task<ActionResult<System.Collections.Generic.List<ActivityDto>>> List() => await _service.GetAllAsync();

    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] CreateActivityRequest request) => await _service.CreateAsync(request);

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateActivityRequest request)
    {
        await _service.UpdateAsync(id, request);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("links")]
    public async Task<ActionResult<System.Collections.Generic.List<ActivityQualificationDto>>> Links()
        => await _service.GetActivityQualificationsAsync();

    [HttpPost("links")]
    public async Task<IActionResult> CreateLink([FromBody] CreateActivityQualificationRequest request)
    {
        await _service.LinkAsync(request);
        return NoContent();
    }

    [HttpDelete("links/{activityId:int}/{qualificationId:int}")]
    public async Task<IActionResult> DeleteLink(int activityId, int qualificationId)
    {
        await _service.UnlinkAsync(activityId, qualificationId);
        return NoContent();
    }

    [HttpGet("{id:int}/qualifications")]
    public async Task<ActionResult<System.Collections.Generic.List<QualificationDto>>> QualificationsForActivity(int id)
        => await _service.GetQualificationsForActivityAsync(id);

    [HttpGet("{id:int}/leaders")]
    public async Task<ActionResult<System.Collections.Generic.List<LeaderDto>>> LeadersForActivity(int id)
        => await _service.GetLeadersForActivityAsync(id);
}

[ApiController]
[Route("api/qualifications")]
public class QualificationsController : ControllerBase
{
    private readonly QualificationService _service;
    public QualificationsController(QualificationService service) { _service = service; }

    [HttpGet]
    public async Task<ActionResult<System.Collections.Generic.List<QualificationDto>>> List() => await _service.GetAllAsync();

    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] CreateQualificationRequest request) => await _service.CreateAsync(request);

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateQualificationRequest request)
    {
        await _service.UpdateAsync(id, request);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}

[ApiController]
[Route("api/leaders")]
public class LeadersController : ControllerBase
{
    private readonly LeaderService _service;
    public LeadersController(LeaderService service) { _service = service; }

    [HttpGet]
    public async Task<ActionResult<System.Collections.Generic.List<LeaderDto>>> List() => await _service.GetAllAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LeaderDto>> GetById(int id)
    {
        var leader = await _service.GetByIdAsync(id);
        return leader is null ? NotFound() : leader;
    }

    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] CreateLeaderRequest request) => await _service.CreateAsync(request);

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateLeaderRequest request)
    {
        await _service.UpdateAsync(id, request);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}

[ApiController]
[Route("api/workers")]
public class WorkersController : ControllerBase
{
    private readonly WorkerService _service;
    public WorkersController(WorkerService service) { _service = service; }

    [HttpGet]
    public async Task<ActionResult<System.Collections.Generic.List<WorkerDto>>> List([FromQuery] bool freeOnly = false)
        => await _service.GetAllAsync(freeOnly);

    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] CreateWorkerRequest request) => await _service.CreateAsync(request);

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateWorkerRequest request)
    {
        await _service.UpdateAsync(id, request);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
