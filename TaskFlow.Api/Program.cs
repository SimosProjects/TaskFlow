using Microsoft.EntityFrameworkCore;
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
if (string.IsNullOrWhiteSpace(connString))
{
    builder.Services.AddDbContext<TaskFlowDbContext>(o => o.UseInMemoryDatabase("TaskFlow"));
}
else
{
    builder.Services.AddDbContext<TaskFlowDbContext>(o => o.UseNpgsql(connString));
}

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

// Centralized exception handling
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Explicit routing keeps behavior consistent across hosting environments (including reverse proxies)
app.UseRouting();

app.UseAuthorization();

app.MapControllers();

app.Run();