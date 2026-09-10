using System.Text;
using FluentValidation;
using HRAttendance.Api.Data;
using HRAttendance.Api.Dtos;
using HRAttendance.Api.helpers;
using HRAttendance.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme { In = Microsoft.OpenApi.Models.ParameterLocation.Header, Description = "Enter: Bearer {your JWT token}", Name = "Authorization", Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey, Scheme = "Bearer" });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        { new Microsoft.OpenApi.Models.OpenApiSecurityScheme { Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

var useSqlServer = builder.Configuration.GetValue("UseSqlServer", true);
var connectionString = builder.Configuration.GetConnectionString("Default");
if (useSqlServer && string.IsNullOrWhiteSpace(connectionString)) throw new InvalidOperationException("ConnectionStrings__Default must be configured for the SQL Server database.");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (useSqlServer) options.UseSqlServer(connectionString);
    else throw new InvalidOperationException("The HR API requires SQL Server. In-memory storage is disabled.");
});

builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddScoped<IStatisticsCalculationService, StatisticsCalculationService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IValidator<PreviousYearStatisticsInput>, PreviousYearStatisticsInputValidator>();
builder.Services.AddScoped<IValidator<CurrentYearStatisticsInput>, CurrentYearStatisticsInputValidator>();
builder.Services.AddScoped<IValidator<UpdateEmployeeStatisticsRequest>, UpdateEmployeeStatisticsRequestValidator>();
builder.Services.AddScoped<IAttendanceReportService, AttendanceReportService>();
builder.Services.AddSingleton<IAttendanceReportPdfService, AttendanceReportPdfService>();
QuestPDF.Settings.License = LicenseType.Community;

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32) throw new InvalidOperationException("Jwt__Key must be configured and contain at least 32 characters.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();
builder.Services.AddCors(options => options.AddPolicy("AllowFlutterApp", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    SeedData.Seed(db);
    CaseStatisticsSeedData.Seed(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("AllowFlutterApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
