// Modules/Authentication/API/Controllers/AuthController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ApiPdfCsv.Modules.Authentication.Application.DTOs.Auth;
using ApiPdfCsv.Modules.Authentication.Application.DTOs.User;
using ApiPdfCsv.Modules.Authentication.Application.Services;

namespace ApiPdfCsv.Modules.Authentication.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var result = await _authService.Register(request);

        if (result.Success)
        {
            return Ok(result);
        }

        if (result.Errors?.Any() == true)
        {
            return BadRequest(new
            {
                result.Success,
                result.Message,
                Errors = result.Errors.GroupBy(e => e.Code)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray())
            });
        }

        return StatusCode(StatusCodes.Status500InternalServerError, new
        {
            result.Success,
            result.Message,
            result.Exception
        });
    }
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await _authService.Login(request);

        if (result.Success)
        {
            return Ok(result);
        }

        if (result.Errors?.Any() == true)
        {
            return Unauthorized(new
            {
                result.Success,
                result.Message,
                Errors = result.Errors.GroupBy(e => e.Code)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray())
            });
        }

        return StatusCode(StatusCodes.Status500InternalServerError, new
        {
            result.Success,
            result.Message,
            result.Exception
        });
    }
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _authService.Logout();
        return Ok(new { Message = "Logged out successfully" });
    }

    [HttpPut("change-username")]
    [Authorize]
    public async Task<IActionResult> ChangeUserName([FromBody] ChangeUserNameRequest request)
    {
        var result = await _authService.ChangeUserName(request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var result = await _authService.ChangePassword(request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("deleteUser")]
    [Authorize]
    public async Task<IActionResult> DeleteUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { Success = false, Message = "User ID not found in token." });
        }

        var result = await _authService.DeleteUser(userId);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var user = await _authService.GetCurrentUser();
        return Ok(user);
    }
}
