using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectChecking.Api.Auth;
using ProjectChecking.Api.Dtos;
using ProjectChecking.Api.Services;

namespace ProjectChecking.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserService _users;
    private readonly ISessionStore _sessions;

    public AuthController(UserService users, ISessionStore sessions)
    {
        _users = users;
        _sessions = sessions;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _users.FindByLoginAsync(request.Login);
        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Неверный логин или пароль." });
        }
        var role = await _users.GetRoleNameAsync(user.RoleId);
        var session = _sessions.Create(user.Id, role);
        return new LoginResponse(session.Token, user.Id, user.Login, role, user.FullName);
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        var session = HttpContext.GetSession();
        if (session != null)
        {
            _sessions.Remove(session.Token);
        }
        return NoContent();
    }
}

[ApiController]
[Route("api/account")]
public class AccountController : ControllerBase
{
    private readonly UserService _users;

    public AccountController(UserService users)
    {
        _users = users;
    }

    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserDto>> Me()
    {
        var session = HttpContext.GetSession();
        if (session is null) return Unauthorized();
        var user = await _users.GetByIdAsync(session.UserId);
        if (user is null) return Unauthorized();
        var role = await _users.GetRoleNameAsync(user.RoleId);
        return new CurrentUserDto(user.Id, user.Login, role, user.FullName, user.Phone, user.BirthDate, user.RegistrationDate);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateMeRequest request)
    {
        var session = HttpContext.GetSession();
        if (session is null) return Unauthorized();
        try
        {
            await _users.UpdateMeAsync(session.UserId, request);
            return NoContent();
        }
        catch (System.InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
