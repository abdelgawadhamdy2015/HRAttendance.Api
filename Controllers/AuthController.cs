using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using HRAttendance.Api.Data;
using HRAttendance.Api.DTOs;
using HRAttendance.Api.Models;
using HRAttendance.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRAttendance.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtTokenService _jwtTokenService;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public AuthController(AppDbContext db, JwtTokenService jwtTokenService)
    {
        _db = db;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Username and password are required.");

        var exists = await _db.Users.AnyAsync(u =>
            u.Username == request.Username || u.Email == request.Email);
        if (exists)
            return Conflict("A user with this username or email already exists.");

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            FullName = request.FullName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // New users start with no permissions - assign them via POST /api/permissions/assign.
        var (token, expiresAt) = _jwtTokenService.GenerateToken(user, Array.Empty<string>());

        return Ok(new AuthResponse(user.Id, user.Username, user.Email, token, expiresAt, new()));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await _db.Users
            .Include(u => u.UserPermissions)
            .ThenInclude(up => up.Permission)
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user is null || !user.IsActive)
            return Unauthorized("Invalid username or password.");

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
            return Unauthorized("Invalid username or password.");

        var permissionNames = user.UserPermissions.Select(up => up.Permission.Name).ToList();
        var (token, expiresAt) = _jwtTokenService.GenerateToken(user, permissionNames);

        return Ok(new AuthResponse(user.Id, user.Username, user.Email, token, expiresAt, permissionNames));
    }

    // Returns the currently logged-in user, resolved from the JWT sent in the Authorization header.
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<AuthResponse>> Me()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = await _db.Users
            .Include(u => u.UserPermissions)
            .ThenInclude(up => up.Permission)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null) return NotFound();

        var permissionNames = user.UserPermissions.Select(up => up.Permission.Name).ToList();
        return Ok(new AuthResponse(user.Id, user.Username, user.Email, string.Empty, DateTime.MinValue, permissionNames));
    }
}
