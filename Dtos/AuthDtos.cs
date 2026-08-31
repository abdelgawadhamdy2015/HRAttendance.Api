using System;
using System.Collections.Generic;

namespace HRAttendance.Api.DTOs;

public record RegisterRequest(string Username, string Email, string Password, string FullName);

public record LoginRequest(string Username, string Password);

public record AuthResponse(
    int UserId,
    string Username,
    string Email,
    string Token,
    DateTime ExpiresAt,
    List<string> Permissions);
