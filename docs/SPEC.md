# Inventory API — Specification

| Field | Value |
|---|---|
| Project | Product Inventory RESTful API (technical test — Senior Developer) |
| Author | Fernando Perez |
| Status | Draft v1 — written **before** any code was generated (Spec-Driven Development) |
| Date | 2026-09-12 |

This document is the contract the implementation must satisfy. Every command, query, endpoint and business rule listed here must exist in the code, and nothing in the code should exist without being described here (or in a later revision of this spec, recorded in `ADR.md`).

---

## 1. Goal

Build a RESTful API that manages a product inventory: products, categories and inventory movements (stock in / stock out). The solution must be dockerized, use SQL Server, apply CQRS (EF Core for reads, Dapper for writes), be protected with OAuth2 and ship with unit tests, Swagger and a README.

## 2. Non-goals (explicitly out of scope)

- Multi-warehouse / multi-location stock.
- Purchase orders, suppliers, pricing history.
- User management UI (authentication is delegated to an OAuth2 provider).
- Pagination beyond simple `page`/`pageSize` query parameters.

## 3. Technology decisions

| Concern | Decision | Rationale |
|---|---|---|
| Runtime | .NET 8 (LTS) | Required by the test; LTS |
| Architecture | Clean Architecture (Domain / Application / Infrastructure / Api) | Dependency rule, testable core |
| CQRS | MediatR — `IRequest<T>` for commands and queries | Explicit separation, one handler per use case |
| Reads | EF Core 8 + `AsNoTracking()` | Required by the test |
| Writes | Dapper over `IDbConnection` (SqlConnection) | Required by the test |
| Validation | FluentValidation via MediatR pipeline behavior | Keeps handlers single-responsibility |
| Auth | OAuth2 / OIDC — Keycloak in docker-compose, JWT Bearer validation in the API | Real OAuth2 provider, still one-command startup |
| Tests | xUnit + NSubstitute + FluentAssertions | Standard .NET stack |
| Docs | Swashbuckle (Swagger UI with OAuth2 "Authorize") | Required by the test |
| Config | Environment variables only (`.env` + `docker-compose.yml`) | Clean Code rule 5 |

## 4. Solution layout

```
src/
  Inventory.Domain/          Entities, enums, domain exceptions. No dependencies.
  Inventory.Application/     Commands, queries, handlers, validators, DTOs, interfaces (IProductWriteRepository, IInventoryReadContext...).
  Inventory.Infrastructure/  EF Core DbContext + configurations (reads), Dapper repositories (writes), migrations, DI registration.
  Inventory.Api/             Controllers, auth setup, Swagger, Program.cs, middleware (error handling).
tests/
  Inventory.UnitTests/       Handler, validator and domain tests.
db/
  init.sql                   Schema + seed (used by the SQL Server container).
docker/
  keycloak/realm-export.json Pre-configured realm, client and roles.
docs/
  SPEC.md  ADR.md  AI-LOG.md  decisions.md
```

Dependency direction: `Api → Application → Domain`, `Infrastructure → Application`. Domain references nothing.

## 5. Domain model

### 5.1 Category
| Field | Type | Rules |
|---|---|---|
| Id | int (identity) | PK |
| Name | string(100) | Required, unique (case-insensitive) |
| Description | string(500) | Optional |
| CreatedAt | datetime2 | Set on insert (UTC) |

### 5.2 Product
| Field | Type | Rules |
|---|---|---|
| Id | int (identity) | PK |
| Sku | string(50) | Required, unique |
| Name | string(150) | Required |
| Description | string(500) | Optional |
| Price | decimal(18,2) | ≥ 0 |
| Stock | int | ≥ 0, **never edited directly** — only changed through inventory movements |
| CategoryId | int | FK → Categories, required |
| IsActive | bit | Default true; delete is logical (soft delete) |
| CreatedAt / UpdatedAt | datetime2 | UTC |

### 5.3 InventoryMovement
| Field | Type | Rules |
|---|---|---|
| Id | int (identity) | PK |
| ProductId | int | FK → Products, required |
| Type | tinyint / enum `MovementType { In = 1, Out = 2 }` | Required |
| Quantity | int | > 0 |
| Reason | string(250) | Optional (e.g. "purchase", "sale", "adjustment") |
| CreatedAt | datetime2 | UTC |

Movements are **immutable**: no update or delete endpoints.

## 6. Business rules

| Id | Rule |
|---|---|
| BR-01 | Category name must be unique (case-insensitive). |
| BR-02 | Product SKU must be unique. |
| BR-03 | A product cannot be created with a non-existent or inactive category. |
| BR-04 | A category with active products cannot be deleted → `409 Conflict`. |
| BR-05 | Product delete is logical (`IsActive = false`); inactive products are excluded from list queries by default. |
| BR-06 | `Stock` is read-only from the API's point of view; the only way to change it is `POST /api/inventory/movements`. |
| BR-07 | An `Out` movement whose quantity exceeds current stock is rejected → `422 Unprocessable Entity` (`InsufficientStockException`). |
| BR-08 | Registering a movement and updating the product stock happen in the **same database transaction** (Dapper). |
| BR-09 | Movements cannot be registered against an inactive product → `422`. |
| BR-10 | All timestamps are stored and returned in UTC (ISO-8601). |

## 7. API contract

Base path: `/api`. Content type: `application/json`. All endpoints require `Authorization: Bearer <token>` except `/health` and Swagger.

### 7.1 Error envelope (RFC 7807 Problem Details)
```json
{ "type": "https://httpstatuses.com/422", "title": "Insufficient stock", "status": 422,
  "detail": "Product 12 has 3 units in stock, cannot remove 5.", "errors": { } }
```
| Situation | Status |
|---|---|
| Validation failure (FluentValidation) | 400 — `errors` contains field → messages |
| Missing / invalid token | 401 |
| Entity not found | 404 |
| Uniqueness / referential conflict (BR-01, BR-02, BR-04) | 409 |
| Business rule violation (BR-07, BR-09) | 422 |
| Unhandled | 500 (no stack trace in response) |

### 7.2 Categories
| Method | Route | Body / Params | Response |
|---|---|---|---|
| GET | `/categories` | — | `200` `CategoryDto[]` |
| GET | `/categories/{id}` | — | `200` `CategoryDto` / `404` |
| POST | `/categories` | `CreateCategoryRequest { name, description }` | `201` + `Location` header, `CategoryDto` |
| PUT | `/categories/{id}` | `UpdateCategoryRequest { name, description }` | `204` / `404` / `409` |
| DELETE | `/categories/{id}` | — | `204` / `404` / `409` (BR-04) |

`CategoryDto { id, name, description, createdAt }`

### 7.3 Products
| Method | Route | Body / Params | Response |
|---|---|---|---|
| GET | `/products` | `?categoryId=&search=&includeInactive=false&page=1&pageSize=20` | `200` `PagedResult<ProductDto>` |
| GET | `/products/{id}` | — | `200` `ProductDto` / `404` |
| POST | `/products` | `CreateProductRequest { sku, name, description, price, categoryId, initialStock? }` | `201`, `ProductDto` |
| PUT | `/products/{id}` | `UpdateProductRequest { name, description, price, categoryId }` | `204` / `404` / `409` |
| DELETE | `/products/{id}` | — | `204` (soft delete) / `404` |

`ProductDto { id, sku, name, description, price, stock, categoryId, categoryName, isActive, createdAt, updatedAt }`
`PagedResult<T> { items: T[], page, pageSize, totalCount }`

`initialStock` (optional, ≥ 0): when provided and > 0 the API creates the product **and** an `In` movement with reason `"initial stock"` in one transaction.

### 7.4 Inventory movements
| Method | Route | Body / Params | Response |
|---|---|---|---|
| POST | `/inventory/movements` | `RegisterMovementRequest { productId, type: "In" \| "Out", quantity, reason? }` | `201` `MovementDto` / `404` / `422` |
| GET | `/inventory/movements` | `?productId=&type=&from=&to=&page=&pageSize=` | `200` `PagedResult<MovementDto>` |
| GET | `/products/{id}/movements` | `?page=&pageSize=` | `200` `PagedResult<MovementDto>` |

`MovementDto { id, productId, productSku, type, quantity, reason, stockAfter, createdAt }`

### 7.5 Health
`GET /health` → `200` when the API and the database are reachable. Anonymous.

## 8. CQRS map (one handler per use case)

| Kind | Name | Repository / Context |
|---|---|---|
| Command | `CreateCategoryCommand` | `ICategoryWriteRepository` (Dapper) |
| Command | `UpdateCategoryCommand` | Dapper |
| Command | `DeleteCategoryCommand` | Dapper |
| Command | `CreateProductCommand` | `IProductWriteRepository` + `IInventoryMovementWriteRepository` (Dapper, transaction) |
| Command | `UpdateProductCommand` | Dapper |
| Command | `DeleteProductCommand` | Dapper (soft delete) |
| Command | `RegisterInventoryMovementCommand` | Dapper, transaction (BR-07, BR-08, BR-09) |
| Query | `GetCategoriesQuery`, `GetCategoryByIdQuery` | `IInventoryReadContext` (EF Core, `AsNoTracking`) |
| Query | `GetProductsQuery`, `GetProductByIdQuery` | EF Core |
| Query | `GetInventoryMovementsQuery`, `GetProductMovementsQuery` | EF Core |

Pipeline behaviors: `ValidationBehavior` (FluentValidation) → handler. Commands **may** read through Dapper when they need a row for a decision (e.g. current stock) to keep the write path in one connection/transaction; they never use the EF context.

## 9. Authentication (OAuth2)

- Provider: Keycloak (`quay.io/keycloak/keycloak:25`) started by docker-compose with an imported realm `inventory`.
- Client `inventory-api` (confidential) — flow: **Client Credentials** for machine-to-machine, plus a test user `admin / admin` with the **Password** flow for Swagger convenience.
- API validates JWTs with `AddAuthentication().AddJwtBearer()`: `Authority = KEYCLOAK_AUTHORITY`, `Audience = inventory-api`.
- Roles: `inventory.read` (GET endpoints) and `inventory.write` (POST/PUT/DELETE). Enforced with authorization policies.
- Swagger UI exposes the OAuth2 "Authorize" button using the Password flow against the Keycloak token endpoint.

## 10. Configuration (environment variables)

| Variable | Used by | Example |
|---|---|---|
| `ConnectionStrings__Inventory` | API | `Server=sqlserver,1433;Database=Inventory;User Id=sa;Password=...;TrustServerCertificate=True` |
| `MSSQL_SA_PASSWORD` | SQL Server container | `Your_password123` |
| `KEYCLOAK_AUTHORITY` | API | `http://keycloak:8080/realms/inventory` |
| `KEYCLOAK_AUDIENCE` | API | `inventory-api` |
| `KEYCLOAK_ADMIN` / `KEYCLOAK_ADMIN_PASSWORD` | Keycloak | `admin` / `admin` |
| `ASPNETCORE_ENVIRONMENT` | API | `Development` |

No value is hard-coded in `appsettings.json` except non-secret defaults; a committed `.env.example` documents every variable.

## 11. Docker

- `src/Inventory.Api/Dockerfile`: multi-stage (`sdk:8.0` build/test → `aspnet:8.0` runtime), non-root user.
- `docker-compose.yml` services: `sqlserver` (healthcheck via `sqlcmd`), `db-init` (runs `db/init.sql` once), `keycloak` (imports realm), `api` (`depends_on: service_healthy`).
- `docker compose up --build` must be the only command required.

## 12. Testing plan (TDD — tests are written before each handler)

| Area | Test cases |
|---|---|
| `RegisterInventoryMovementHandler` | In increases stock; Out decreases stock; Out > stock throws `InsufficientStockException`; unknown product throws `NotFoundException`; inactive product throws `ProductInactiveException`; repository called inside one transaction |
| `CreateProductHandler` | Creates product; with `initialStock` also registers an In movement; unknown category → `NotFoundException`; duplicate SKU → `ConflictException` |
| `DeleteCategoryHandler` | Category with active products → `ConflictException`; empty category deleted |
| Validators | Every required field, `quantity > 0`, `price >= 0`, string lengths |
| Queries | `GetProductsQuery` filters by category/search and excludes inactive by default (EF Core InMemory provider) |
| Domain | `Product.ApplyMovement()` guards (if stock logic lives in the entity) |

Target: all critical handlers and validators covered; coverage reported with `coverlet` (`dotnet test --collect:"XPlat Code Coverage"`).

## 13. Clean Code constraints (from the test — enforced during review)

1. Code, comments, commit messages and docs in English; only API response messages may be localized.
2. No blank lines inside methods except one before `return`.
3. Self-descriptive method names; no explanatory comments needed.
4. Single responsibility: ≤ 25 lines per method, no double nesting.
5. Configuration only from environment variables.
6. One class/enum per file.
7. No commented-out code.
8. Pass objects, not loose properties.

`.editorconfig` + built-in analyzers (`<AnalysisLevel>latest-recommended</AnalysisLevel>`, `TreatWarningsAsErrors`) help catch 3, 4 and 6 automatically.

## 14. Deliverables checklist

- [ ] Public repository with this `docs/` folder
- [ ] `docker compose up --build` starts SQL Server, Keycloak and the API
- [ ] Swagger at `/swagger` with working OAuth2 login
- [ ] All endpoints in §7 implemented and protected
- [ ] Unit tests in §12 green, coverage report
- [ ] `README.md` (setup, run, test, debug locally, get a token)
- [ ] `docs/AI-LOG.md`, `docs/ADR.md`, `docs/decisions.md`

## 15. Open points to confirm before coding

- Soft delete vs hard delete for products (spec assumes soft).
- Whether `initialStock` on product creation is wanted or products always start at 0.
- Keycloak vs an external provider (Auth0) — spec assumes Keycloak to keep everything local.