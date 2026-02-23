# TaskFlow API

TaskFlow API is a production-style ASP.NET Core Web API built with .NET 8.

This project serves as:
- Modernization proof of current .NET 8 backend skills
- A portfolio artifact
- An interview discussion anchor
- A bridge narrative for recent independent work

---

## Commands

### Build
docker build -t taskflow-api:dev .

### Run (Dev for Swagger)
docker run --rm -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Development taskflow-api:dev

### Run locally (Docker Compose)
cp .env.example .env
docker compose up --build

Swagger: http://localhost:8080/swagger

### View status
docker compose ps

### Tail logs
docker compose logs -f api
docker compose logs -f postgres

### Reset everything (deletes DB volume)
docker compose down -v

---

## Tech Stack

- .NET 8 (LTS)
- ASP.NET Core Web API (Controller-based)
- C#
- xUnit (unit testing)
- Global exception middleware
- RFC 7807 ProblemDetails error responses
- EF Core 8
- PostgreSQL
- Npgsql
- Docker (multi-stage build)
- docker-compose

Planned next steps:
- Azure deployment
- CI/CD with GitHub Actions

---

## Architecture Overview

The application follows a layered design:
- Controllers → Services → Domain → Infrastructure (EF Core)

### Controllers
- Handle HTTP concerns only
- Thin endpoints
- Map DTOs to/from domain models
- Return appropriate HTTP status codes

### Services (Application Layer)
- Orchestrate use cases (Create, Complete, Retrieve)
- Encapsulate business logic
- Registered via dependency injection
- Database-backed implementation using EF Core and PostgreSQL Registered with Scoped lifetime (aligned with DbContext)

### Domain
- Framework-independent
- Enforces invariants (e.g., required title)
- Owns business behavior (`MarkComplete()`)

### Middleware
- Centralized exception handling
- Returns standardized RFC 7807 `ProblemDetails`
- Distinguishes between client errors (400) and server errors (500)

### Database & Containerization
- PostgreSQL runs in a dedicated container
- Internal Docker networking (Host=postgres)
- Named volume for persistent storage
- Environment-based configuration via .env
- Multi-stage Dockerfile for optimized runtime image

---

## Error Handling Strategy

The API uses centralized exception middleware to ensure:

- Consistent `application/problem+json` responses
- RFC 7807 compliance
- Clear separation between:
  - Validation errors (400)
  - Unexpected server failures (500)

DTO validation is handled automatically by `[ApiController]`.

---

## Request Flow Example

**POST /api/tasks**

1. Controller validates request DTO.
2. Service constructs a `TaskItem` domain object.
3. Domain enforces invariants.
4. Service stores task.
5. Controller returns `201 Created`.

---

## Testing Strategy

Unit tests focus on the service layer to validate:

- Task creation
- Retrieval
- Completion behavior
- Edge cases

This keeps business logic testable without HTTP dependencies.

---

## Why This Architecture?

The goal is clarity and maintainability:

- Controllers stay thin.
- Domain enforces invariants.
- Services orchestrate behavior.
- Errors are centralized and standardized.
- The design can evolve cleanly to EF Core and cloud deployment.

