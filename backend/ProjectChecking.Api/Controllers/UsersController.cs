using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ProjectChecking.Api.Auth;
using ProjectChecking.Api.Dtos;
using ProjectChecking.Api.Services;

namespace ProjectChecking.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly UserService _users;
    public UsersController(UserService users) { _users = users; }

    [HttpGet]
    public async Task<ActionResult<System.Collections.Generic.List<UserListItemDto>>> List() => await _users.GetAllAsync();

    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] CreateUserRequest request)
    {
        var id = await _users.CreateAsync(request);
        return Ok(id);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request)
    {
        var session = HttpContext.GetSession();
        if (session != null && session.UserId == id)
        {
            var me = await _users.GetByIdAsync(id);
            if (me != null && me.RoleId != request.RoleId)
            {
                return BadRequest(new { message = "Нельзя изменить собственную роль." });
            }
        }
        await _users.UpdateAsync(id, request);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var session = HttpContext.GetSession();
        if (session != null && session.UserId == id)
        {
            return BadRequest(new { message = "Нельзя удалить самого себя." });
        }
        await _users.DeleteAsync(id);
        return NoContent();
    }
}

[ApiController]
[Route("api/roles")]
public class RolesController : ControllerBase
{
    private readonly RoleService _roles;
    public RolesController(RoleService roles) { _roles = roles; }

    [HttpGet]
    public async Task<ActionResult<System.Collections.Generic.List<RoleDto>>> List() => await _roles.GetAllAsync();

    [HttpPost]
    public async Task<ActionResult<int>> Create([FromBody] CreateActivityRequest request) => await _roles.CreateAsync(request.Name);

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateActivityRequest request)
    {
        await _roles.UpdateAsync(id, request.Name);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _roles.DeleteAsync(id);
        return NoContent();
    }
}
