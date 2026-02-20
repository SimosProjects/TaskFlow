
using TaskFlow.Api.Services;
using TaskFlow.Api.Middleware;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Register TaskService with a Scoped lifetime.
// Each HTTP request receives its own instance.
//
// This aligns with typical application service patterns and prepares the
// service for future EF Core integration, where DbContext is also Scoped
// and not thread-safe.
//
// Previously this was Singleton when using shared in-memory state.
// Scoped is the correct lifetime for database-backed services.
builder.Services.AddScoped<ITaskService, TaskService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Registers the EF Core DbContext using PostgreSQL.
// Scoped lifetime ensures one DbContext instance per HTTP request.
builder.Services.AddDbContext<TaskFlowDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("TaskFlowDb"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
