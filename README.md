# Inventory API

RESTful API for a product inventory: categories, products and stock movements.
Built for a Senior Developer technical test, following Spec-Driven Development —
`docs/SPEC.md` was written and committed before any code, and every deviation
from it is recorded in `docs/ADR.md`.


## Stack

| Concern | Choice |
|---|---|
| Runtime | .NET 10 |
| Architecture | Clean Architecture — Domain / Application / Infrastructure / Api |
| CQRS | MediatR, one handler per use case |
| Reads | EF Core with `AsNoTracking`, projected straight into DTOs |
| Writes | Dapper, inside an explicit transaction |
| Database | SQL Server 2022, schema from `db/init.sql` |
| Auth | OAuth2 / OIDC against Keycloak, JWT Bearer validation |
| Validation | FluentValidation through a MediatR pipeline behavior |
| Tests | xUnit, NSubstitute, FluentAssertions, coverlet |
| Docs | Swagger UI with an OAuth2 Authorize button |

## Run everything with Docker

```bash
cp .env.example .env      # adjust DB_PASSWORD if you like
docker compose up --build
```

That starts four things: SQL Server, a one-shot container that applies
`db/init.sql`, Keycloak with the `inventory` realm imported, and the API.

`DB_PASSWORD` becomes the SA password of the SQL Server container, so it has to
satisfy the SQL Server password policy — at least eight characters using three
of upper case, lower case, digits and symbols. A weaker value leaves
`inventory-sqlserver` restarting and nothing else starts behind it.

If a SQL Server instance already listens on the host's port 1433, set `DB_PORT`
to something free before starting. Only the published port moves; the API talks
to the container over the compose network on 1433 either way.

| Service | URL |
|---|---|
| Swagger UI | http://localhost:5080/swagger |
| Health | http://localhost:5080/health |
| Keycloak | http://localhost:8080 (admin / admin) |

The init container is expected to finish and stay at `Exited (0)`. If the schema
failed, its logs say why:

```bash
docker compose logs sqlserver-init
```

## Get a token

Two Keycloak users ship with the realm, so the two roles can be told apart:

| User | Password | Roles |
|---|---|---|
| `admin` | `admin` | `inventory.read`, `inventory.write` |
| `reader` | `reader` | `inventory.read` only |

`reader` can list and read but gets `403` on any `POST`, `PUT` or `DELETE`.

### From Swagger

Click **Authorize**, leave the client as `inventory-swagger`, and sign in with one
of the users above. Swagger then sends the bearer token on every call.

### From the command line

```bash
curl -s -X POST http://localhost:8080/realms/inventory/protocol/openid-connect/token \
  -d grant_type=password \
  -d client_id=inventory-swagger \
  -d username=admin \
  -d password=admin | jq -r .access_token
```

For machine-to-machine work, the confidential `inventory-api` client supports
client credentials:

```bash
curl -s -X POST http://localhost:8080/realms/inventory/protocol/openid-connect/token \
  -d grant_type=client_credentials \
  -d client_id=inventory-api \
  -d client_secret=inventory-api-secret | jq -r .access_token
```

Then call the API with it:

```bash
curl -H "Authorization: Bearer $TOKEN" http://localhost:5080/api/products
```

## Run locally without Docker

You need a reachable SQL Server and a reachable Keycloak. Apply the schema:

```bash
sqlcmd -S localhost,1433 -U sa -P "<password>" -C -v DbName="InventoryDb" -i db/init.sql
```

`db/init.sql` is idempotent: it creates what is missing, adds columns introduced
after the first release, and seeds only when the database is empty. Running it
again on a populated database is safe.

Then run the API. Configuration comes from environment variables only (Clean Code
rule 5) — see `.env.example` for the full list. For Visual Studio, the `DB_*` and
`KEYCLOAK_*` values are already in `Inventory/Inventory.Api/Properties/launchSettings.json`.

```bash
cd Inventory
dotnet run --project Inventory.Api
```

If a variable is missing, startup fails with a message naming it, rather than
with a connection error later.

## Tests

```bash
cd Inventory
dotnet test Inventory.UnitTests/Inventory.UnitTests.csproj
```

Unit tests need nothing external: handlers run against substituted repositories,
and the read handlers run against SQLite in-memory (see ADR-007).

```bash
dotnet test Inventory.IntegrationTests/Inventory.IntegrationTests.csproj
```

Integration tests **write to a real SQL Server**. They cover what the unit tests
cannot see, because those mock `IUnitOfWork`: that a failure mid-operation rolls
back every write, and that the repositories share the connection the unit of work
opened (ADR-006). Every one of them rolls back, so they leave nothing behind, but
point them at a throwaway database, never a real one. They read `DB_*` from the
environment and fall back to `localhost,1433 / InventoryDb / sa / admin`.

Coverage:

```bash
dotnet test Inventory.UnitTests/Inventory.UnitTests.csproj --collect:"XPlat Code Coverage"
```

## Layout

```
Inventory/
  Inventory.Domain/           Entities, enums, domain exceptions. Depends on nothing.
  Inventory.Application/      Commands, queries, handlers, validators, DTOs, persistence interfaces.
  Inventory.Infrastructure/   EF Core context and configurations, Dapper repositories, unit of work, DI.
  Inventory.Api/              Controllers, auth, Swagger, error envelope.
  Inventory.UnitTests/        Domain, handler, validator and read-model tests.
  Inventory.IntegrationTests/ Transaction and mapping tests against a real database.
db/init.sql                   Schema and seed, idempotent.
docker/keycloak/              Realm export imported on startup.
docs/                         SPEC.md, ADR.md, AI-LOG.md, decisions.md
```

Dependency direction: `Api → Application → Domain`, `Infrastructure → Application`.
Domain references nothing.

## API surface

Everything except `/health` and Swagger requires a bearer token. `GET` needs the
`inventory.read` role, and `POST` / `PUT` / `DELETE` need `inventory.write`.

| Method | Route |
|---|---|
| GET POST | `/api/categories` |
| GET PUT DELETE | `/api/categories/{id}` |
| GET POST | `/api/products` |
| GET PUT DELETE | `/api/products/{id}` |
| GET | `/api/products/{id}/movements` |
| GET POST | `/api/inventory/movements` |
| GET | `/api/inventory/movements/{id}` |
| GET | `/health` (anonymous) |

Errors follow RFC 7807: `400` validation with a field-to-messages dictionary,
`401` missing or invalid token, `403` wrong role, `404` not found, `409`
uniqueness or referential conflict, `422` business rule violation, `500`
otherwise with no stack trace in the response.

Deletes are logical. Deleting a product or a category sets `IsActive = false`, so
the movement history survives and list queries hide the row unless asked with
`?includeInactive=true`.

## Documentation

| File | What it holds |
|---|---|
| `docs/SPEC.md` | The contract: domain model, business rules, API, test plan |
| `docs/ADR.md` | Every decision, its alternatives and its cost |
| `docs/AI-LOG.md` | How generative AI was used, prompt by prompt |
| `docs/decisions.md` | Answers to the decision-making questions of the test |
