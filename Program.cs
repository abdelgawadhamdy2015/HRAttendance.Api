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

builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}",
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// --- Database ---
var useSqlServer = builder.Configuration.GetValue<bool>("UseSqlServer");
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (useSqlServer)
        options.UseSqlServer(builder.Configuration.GetConnectionString("Default"));
    else
        options.UseInMemoryDatabase("master");
});

// --- Auth ---
builder.Services.AddSingleton<JwtTokenService>();

// --- Case-statistics domain services ---
builder.Services.AddScoped<IStatisticsCalculationService, StatisticsCalculationService>();
builder.Services.AddScoped<IAuditService, AuditService>();

// FluentValidation - explicit registrations (predictable, no assembly scanning surprises)
builder.Services.AddScoped<IValidator<PreviousYearStatisticsInput>, PreviousYearStatisticsInputValidator>();
builder.Services.AddScoped<IValidator<CurrentYearStatisticsInput>, CurrentYearStatisticsInputValidator>();
builder.Services.AddScoped<IValidator<UpdateEmployeeStatisticsRequest>, UpdateEmployeeStatisticsRequestValidator>();

// --- Attendance reports (legacy HR app) ---
builder.Services.AddScoped<IAttendanceReportService, AttendanceReportService>();
builder.Services.AddSingleton<IAttendanceReportPdfService, AttendanceReportPdfService>();

// QuestPDF: Community is free for eligible individuals/organizations.
QuestPDF.Settings.License = LicenseType.Community;

var jwtSection = builder.Configuration.GetSection("Jwt");
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!))
    };
});

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFlutterApp", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

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
