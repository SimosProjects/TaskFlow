using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TaskFlow.Api.Infrastructure;
using TaskFlow.Api.Middleware;
using TaskFlow.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DbContext setup
var connString = builder.Configuration.GetConnectionString("TaskFlowDb");

if (string.IsNullOrWhiteSpace(connString) && builder.Environment.IsProduction())
{
    // Portfolio note: in a real production deployment this would throw.
    // Kept as warning here to avoid Azure Postgres costs during portfolio phase.
    Console.WriteLine("WARNING: Running InMemory in Production — data will not persist.");
    builder.Services.AddDbContext<TaskFlowDbContext>(o => o.UseInMemoryDatabase("TaskFlow"));
}
else if (string.IsNullOrWhiteSpace(connString))
{
    builder.Services.AddDbContext<TaskFlowDbContext>(o => o.UseInMemoryDatabase("TaskFlow"));
}
else
{
    builder.Services.AddDbContext<TaskFlowDbContext>(o => o.UseNpgsql(connString));
}

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<TaskFlowDbContext>(
        name: "database",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready"]);

// Application services
builder.Services.AddScoped<ITaskService, TaskService>();

var app = builder.Build();

// Enable Swagger
var enableSwagger = app.Configuration.GetValue<bool>("EnableSwagger");
if (enableSwagger || app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Correlation ID middleware must run first so every subsequent middleware
// and log statement in the request pipeline carries the correlation ID in scope.
app.UseMiddleware<CorrelationIdMiddleware>();

// Centralized exception handling
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Explicit routing keeps behavior consistent across hosting environments (including reverse proxies)
app.UseRouting();

app.UseAuthorization();

app.MapControllers();

// Liveness: process is running — no dependency checks.
// Azure Container Apps calls this to decide whether to restart the container.
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false  // Exclude all named checks — just proves the process responds.
});

// Readiness: process can serve traffic — includes database connectivity.
// ACA calls this to decide whether to route traffic to this revision.
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();
