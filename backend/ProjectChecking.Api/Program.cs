using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using ProjectChecking.Api.Auth;
using ProjectChecking.Api.Data;
using ProjectChecking.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddControllers(options =>
{
    options.Filters.Add<RequireAuthFilter>();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Project Checking API",
        Version = "v1",
        Description = "Backend API учёта проектов транспортной инфраструктуры",
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Введите токен из ответа /api/auth/login",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] { }
        }
    });
});

builder.Services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
builder.Services.AddSingleton<ISessionStore, InMemorySessionStore>();
builder.Services.AddSingleton<RequireAuthFilter>();
builder.Services.AddSingleton<SchemaInitializer>();

builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<RoleService>();
builder.Services.AddScoped<ActivityService>();
builder.Services.AddScoped<QualificationService>();
builder.Services.AddScoped<LeaderService>();
builder.Services.AddScoped<WorkerService>();
builder.Services.AddScoped<ProjectService>();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressMapClientErrors = false;
});

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:AutoMigrate", true))
{
    using var scope = app.Services.CreateScope();
    var initializer = scope.ServiceProvider.GetRequiredService<SchemaInitializer>();
    try
    {
        initializer.Initialize();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Database initialization failed. The API will start but database calls will fail.");
    }
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Project Checking API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors();
app.UseMiddleware<AuthMiddleware>();
app.MapControllers();

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow }));

app.Run();
