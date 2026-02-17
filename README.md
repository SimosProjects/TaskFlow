# TaskFlow API

TaskFlow API is a production-style ASP.NET Core Web API built with .NET 8.

This project serves as:
- Modernization proof of current .NET 8 backend skills
- A portfolio artifact
- An interview discussion anchor
- A bridge narrative for recent independent work

---

## Tech Stack

- .NET 8 (LTS)
- ASP.NET Core Web API (Controller-based)
- C#
- xUnit (unit testing)
- Global exception middleware
- RFC 7807 ProblemDetails error responses

Planned next steps:
- EF Core + PostgreSQL
- Docker + docker-compose
- Azure deployment
- CI/CD with GitHub Actions

---

## Architecture Overview

The application follows a layered design:
- Controllers → Services (Application Layer) → Domain


### Controllers
- Handle HTTP concerns only
- Thin endpoints
- Map DTOs to/from domain models
- Return appropriate HTTP status codes

### Services (Application Layer)
- Orchestrate use cases (Create, Complete, Retrieve)
- Encapsulate business logic
- Registered via dependency injection
- In-memory implementation for Phase 1

### Domain
- Framework-independent
- Enforces invariants (e.g., required title)
- Owns business behavior (`MarkComplete()`)

### Middleware
- Centralized exception handling
- Returns standardized RFC 7807 `ProblemDetails`
- Distinguishes between client errors (400) and server errors (500)

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

## Concurrency Considerations

The current implementation uses an in-memory task store.

Because the service is registered as a Singleton, access to the shared list is synchronized to ensure thread safety under concurrent requests.

In a production environment, this would be replaced by a database-backed implementation with Scoped lifetime.

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

