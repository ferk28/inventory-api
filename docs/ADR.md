# Architecture Decision Records

Each record documents a decision, who proposed it (AI or me), the alternatives considered, and the final outcome. Status values: `Proposed` (by AI, awaiting my decision), `Accepted`, `Modified`, `Rejected`.

Format: Context → Options → Decision → Consequences.

---

## ADR-000 — Clean Architecture with CQRS; EF Core for reads, Dapper for writes

- **Date:** 2026-09-12
- **Proposed by:** AI (roadmap, Prompt 1)
- **Status:** Accepted

**Context:** The test requires EF Core for reads and Dapper for writes, and recommends CQRS.

**Options:**
1. Single repository layer mixing both ORMs behind one interface.
2. CQRS: queries go through EF Core (`AsNoTracking`), commands go through Dapper. Four projects: Domain, Application, Infrastructure, Api.

**Decision:** Option 2. The read/write ORM split maps one-to-one onto the query/command split, so CQRS is the natural fit rather than an added layer.

**Consequences:** Two persistence paths to maintain; the EF model and the Dapper SQL must stay in sync with the same schema (`db/init.sql`). Unit tests target handlers with mocked interfaces, so neither ORM is needed in tests.

---

## ADR-001 — Soft delete for products

- **Date:** 2026-09-12
- **Proposed by:** AI (SPEC.md v1, section 15)
- **Status:** Accepted

**Context:** A product can have inventory movements. Hard-deleting it would orphan or cascade-delete its history.

**Options:**
1. Hard delete with `ON DELETE CASCADE` on movements.
2. Hard delete blocked when movements exist (409 Conflict).
3. Soft delete (`IsActive = 0`); product excluded from queries, history preserved.

**Decision:** Option 3. Soft delete. To keep old data for future, and reusable information to prevent duplicated items

**Consequences:** Major data concurrency in tables. But manage with past products can be organized when the enterprise wants to pay bills in goverment or adutory. Every queries grows and grows and must includes extra query to hide dleted items.

---

## ADR-002 — Optional `initialStock` on product creation

- **Date:** 2026-09-12
- **Proposed by:** AI (SPEC.md v1, section 15)
- **Status:** Accepted

**Context:** Should a product be creatable with stock, or must stock always enter through a movement?

**Options:**
1. Products always start at 0; stock only changes via `POST /inventory-movements`.
2. Optional `initialStock` that creates an `In` movement in the same transaction.

**Decision:** Option 1. Product always start at 0, stock only changes via POST

**Consequences:** Blocking items when product get to 0, error by persistence in databases. If the enterprise have 1 or more stores can be conflicts in the same item

---

## ADR-003 — Keycloak in docker-compose as the OAuth2 provider

- **Date:** 2026-09-12
- **Proposed by:** AI (SPEC.md v1, section 15)
- **Status:** Accepted

**Context:** Endpoints must be protected with OAuth2. The evaluator must be able to run everything with one `docker compose up`.

**Options:**
1. External provider (Auth0, Azure AD): less setup in the repo, but the evaluator needs an account/tenant.
2. Keycloak container with a pre-imported realm: fully local, one command, heavier compose file.
3. Self-issued JWTs from the API itself: simplest, but it is not OAuth2.

**Decision:** Keycloak container with a pre-imported realm: fully local, one command, heavier compose file

**Consequences:** While it makes the initial docker-compose.yml file heavier, it is the only option that completely satisfies both of your core requirements: it is fully local and runnable with a single command, and it uses a genuine OAuth2 flow
---

## ADR-004 — Idempotent SQL script instead of EF Core migrations

- **Date:** 2026-09-12
- **Proposed by:** AI (Prompt 2, persistence setup)
- **Status:** Accepted

**Context:** The schema has to exist before the API starts, and the evaluator must get a working database from `docker compose up` alone, without running any `dotnet` command. EF Core is already referenced in `Inventory.Infrastructure` for the read side, so migrations were available.

**Options:**
1. EF Core migrations applied at startup with `Database.Migrate()`.
2. EF Core migrations applied manually with `dotnet ef database update`.
3. A single idempotent `db/init.sql` run by a one-shot `sqlserver-init` container that waits on the SQL Server healthcheck.

**Decision:** Option 3. `db/init.sql` is the single source of truth for the schema; the EF model in `Persistence/Configurations/` mirrors it but never creates it.

**Consequences:**
- The evaluator needs no .NET SDK to get a database, and the init container's exit code makes schema failures visible (`Exited (0)` = success).
- Writes go through Dapper with hand-written SQL, so a migration-generated schema would have been half-authoritative anyway; a plain script keeps one source of truth.
- The cost is drift risk: any change to an `IEntityTypeConfiguration` must be mirrored in `db/init.sql` by hand. There is no `dotnet ef migrations add` safety net, and no generated down-migration. Mitigation is review discipline plus the fact that EF is read-only here, so a mismatch surfaces immediately as a mapping error on the first query.
- The script must stay idempotent (`DB_ID` / `OBJECT_ID ... IS NULL` guards, plus `IF NOT EXISTS` on the seed data) because the init container re-runs on every `docker compose up`.

---

## ADR-005 — Target framework: net10.0 instead of the net8.0 stated in SPEC.md

- **Date:** 2026-09-12
- **Proposed by:** AI (structure review, Prompt 2)
- **Status:** Accepted

**Context:** `SPEC.md` section 3 records .NET 10 as the runtime decision, but the scaffolded solution targets `net10.0` across all five projects, with the whole package graph aligned on 10.0.12 (EF Core, ASP.NET Core, JwtBearer) on SDK 10.0.401. The test itself does not pin a .NET version.

**Options:**
1. Retarget all projects to `net8.0` and downgrade every package to 8.x, matching SPEC.md as written.
2. Keep `net10.0` and correct SPEC.md section 3, recording the change here.

**Decision:** Keep `net10.0` and correct SPEC.md section 3

**Consequences:** Option 1 restores the document as the literal contract and lands on an LTS release, at the cost of a coordinated downgrade of every `PackageReference`. Option 2 costs one edit to SPEC.md and keeps the dependency graph that already builds clean, but leaves the delivered solution on a newer release than the document originally promised.

---

## ADR-006 — `IUnitOfWork` takes a delegate instead of passing `IDbTransaction` around

- **Date:** 2026-09-13
- **Proposed by:** AI (Prompt 3, TDD on the write side)
- **Status:** Accepted

**Context:** BR-08 requires the movement insert and the stock update to share one transaction. The write side is Dapper, so the transaction has to be explicit. The shape of that abstraction was driven by the handler tests.

**Options:**
1. Each write repository method takes an `IDbTransaction` parameter, and handlers open and commit it.
2. `IUnitOfWork.ExecuteInTransactionAsync(operation, cancellationToken)` wraps the whole unit of work in a delegate.

**Decision:** Option 2, with a generic overload for commands that return a value and a void overload for those that do not.

**How Infrastructure implements it:** a single `SqlConnectionContext`, registered `Scoped`, owns the connection and the ambient transaction for the request. `SqlUnitOfWork` and all three Dapper repositories take that same instance:

- `SqlConnectionContext` exposes the open `SqlConnection` and the `CurrentTransaction`, which is `null` outside a transaction.
- `SqlUnitOfWork.ExecuteInTransactionAsync` begins the transaction on that connection, runs the delegate, commits on success and rolls back on any exception.
- Every repository method passes `transaction: _context.CurrentTransaction` to its Dapper call. Dapper ignores a `null` transaction, so reads outside a unit of work keep working unchanged.

Registration rules that make or break it:

- `SqlConnectionContext` must be `Scoped`, never `Singleton`: `SqlConnection` is not thread-safe, and a singleton would be shared across concurrent requests. `ISqlConnectionFactory` may stay `Singleton` because it only holds the connection string.
- The repositories must receive the context by constructor injection, not open their own connection through the factory.

**Consequences:**
- `System.Data` stays out of the Application method signatures, and handlers read as one linear sequence.
- "Runs inside a transaction" becomes unit-testable without a database: the test configures the mock *not* to invoke the delegate and asserts no repository was touched. Every write handler has that test.
- The failure mode of a wrong registration is silent. If a repository opens its own connection, each statement commits on its own, BR-08 is broken, the build still succeeds and the unit tests still pass because they mock `IUnitOfWork`. Two integration tests against the real database close that gap and are part of this decision: one asserting that a failure midway through `RegisterInventoryMovement` leaves stock and movements unchanged, and one asserting that the repository and the unit of work observe the same connection.
- Nested calls are not supported; a handler must not call another handler that also opens a transaction.

---

## ADR-007 — SQLite in-memory for query tests instead of the EF Core InMemory provider

- **Date:** 2026-09-13
- **Proposed by:** AI (Prompt 3, query tests)
- **Status:** Accepted

**Context:** `SPEC.md` section 12 named the EF Core InMemory provider for `GetProductsQuery`. The read side is EF Core over SQL Server, and the query relies on relational behaviour: a join to resolve the category name, and the unique SKU index.

**Options:**
1. EF Core InMemory provider, as written in the spec.
2. SQLite in-memory (`Filename=:memory:`) with `EnsureCreated`.

**Decision:** Option 2. Section 12 is updated to match.

**Consequences:** SQLite is a real relational store, so LINQ goes through an actual query translator and constraints are enforced; InMemory silently ignores unique indexes and column limits, which would make the filter tests pass for the wrong reason. The trade-off is that SQLite is not SQL Server: collation and case sensitivity differ, so a `Contains` search that passes here can still behave differently in production. Those differences belong in integration tests against the real container, not in these unit tests.

---

## ADR-008 — Category delete is logical, like products

- **Date:** 2026-09-13
- **Proposed by:** Me
- **Status:** Accepted

**Context:** ADR-001 made product delete logical. Categories kept a hard `DELETE`, guarded by BR-04. That left `Categories.IsActive` — added for BR-03 — unreachable from the API: no endpoint could ever set it, so the inactive half of BR-03 was dead code in practice.

**Options:**
1. Keep the hard delete and add a separate `PATCH /categories/{id}/deactivate` endpoint.
2. Make `DELETE /categories/{id}` logical, the same way `DELETE /products/{id}` already is.

**Decision:** Option 2. `DELETE` sets `IsActive = false`; the row and its product history stay.

**Consequences:**
- One delete semantic across the API instead of two, and `IsActive` is reachable through the contract, so BR-03 is exercised by real use rather than by direct SQL.
- BR-04 still applies: a category holding active products is refused with `409` before anything is written. Deactivating is only allowed once the category is empty of active products, so no product is ever orphaned into an inactive category.
- Category names stay unique (BR-01) even after deletion, because the row is still there. Re-creating a category with a deleted category's name will conflict; reactivating the existing one is the way back.
- `GET /categories` must now filter by `IsActive` the way `GET /products` does, and `CategoryDto` gains `isActive`.

---

## ADR-009 — Store `StockAfter` on each movement instead of computing it

- **Date:** 2026-09-13
- **Proposed by:** AI (Prompt 4, queries)
- **Status:** Accepted

**Context:** `MovementDto` in SPEC section 7.4 returns `stockAfter`, the stock the product was left with after that movement. Nothing in the schema held it, so the read side had no source for the field.

**Options:**
1. Compute it at read time as a running total: `SUM(CASE WHEN Type = 1 THEN Quantity ELSE -Quantity END) OVER (PARTITION BY ProductId ORDER BY CreatedAt, Id)`.
2. Store it in an `InventoryMovements.StockAfter` column, written by the same transaction that applies the movement.
3. Drop `stockAfter` from the contract.

**Decision:** Option 2. `Product.ApplyMovement` stamps the resulting stock onto the movement, so the value is produced by the write that caused it.

**Consequences:**
- Reads stay a plain projection. Option 1 needs a window function, which EF Core cannot express in LINQ, so the read side would have had to drop to raw SQL and break the EF-for-reads rule in ADR-000.
- The value is an audit fact: it records what the stock actually was at that moment, not what a later recomputation thinks it should have been. If stock is ever corrected outside the movement history, the recomputed version would silently disagree with reality; the stored one preserves it.
- The cost is a denormalised column that can drift if anything writes movements without going through the domain. Nothing does today: `RecordStockAfter` is `internal` and only `Product.ApplyMovement` calls it, so a movement cannot be stamped except by applying it.
- The column is nullable, because a movement rejected by BR-07 or BR-09 never gets stamped. Rows written before this change are backfilled by `db/init.sql` with the running total from option 1, which is exact for a history that only ever moved through the API.

---

## ADR-010 — Keycloak wiring: role flattening, a separate Swagger client, and a split issuer

- **Date:** 2026-09-13
- **Proposed by:** AI (Prompt 6, OAuth2)
- **Status:** Accepted

**Context:** ADR-003 chose Keycloak but not how the API consumes it. Three details decide whether authentication works at all, and each fails silently rather than loudly.

**Decisions:**

1. **Realm roles are flattened into role claims.** Keycloak puts them in a `realm_access` claim holding raw JSON; `JwtBearer` does not interpret it, so `RequireRole("inventory.read")` never matches and a valid token carrying the right role still gets `403`. `KeycloakRoleFlattener` runs on `OnTokenValidated`. The alternative, mapping client roles from `resource_access`, has the same problem one level deeper.

2. **Swagger uses its own public client.** `inventory-api` stays confidential for client credentials; a browser cannot hold its secret. `inventory-swagger` is public with PKCE and direct access grants, so the Authorize button works without shipping a secret to the browser.

3. **The issuer and the discovery address are separate settings.** Under Docker the browser reaches Keycloak at `localhost:8080` and the API container reaches it at `keycloak:8080`. The token is stamped with the first, discovery must be fetched from the second. `KEYCLOAK_AUTHORITY` carries the issuer and `KEYCLOAK_METADATA_ADDRESS` the discovery URL; `KC_HOSTNAME` pins Keycloak so it always issues the external one.

   **Correction, after the stack first ran (2026-09-13):** splitting the two settings is necessary but was not sufficient. `KC_HOSTNAME` rewrites *every* URL Keycloak advertises, so the discovery document fetched at `keycloak:8080` still returned `"jwks_uri": "http://localhost:8080/..."`. Inside the API container that host is the API itself, listening on the same port, so the key set was never found and every valid token came back `401 invalid_token — The signature key was not found`. `KC_HOSTNAME_BACKCHANNEL_DYNAMIC=true` resolves backchannel URLs from the request host while leaving the issuer on `localhost`, which is what both sides need.

**Consequences:**
- Both clients need an audience mapper. Keycloak issues `aud=account` by default, and `ValidateAudience` then rejects every token.
- `KEYCLOAK_REQUIRE_HTTPS_METADATA=false` is set for local compose only. It must be `true` anywhere else, and the variable exists so that is a deployment choice rather than a code change.
- The realm ships `admin` and `reader` so the two roles can be told apart; without a read-only user, "the policies work" would only mean "the token is accepted".
- Realm secrets are committed in `docker/keycloak/realm-export.json`. That is deliberate for a reviewable test — the whole point is one-command startup — and is exactly what must not be done for a real deployment.
- Imported users need an `email`. Keycloak 24+ enables the declarative user profile, where `email` is required, so a user imported without one is created with a `VERIFY_PROFILE` required action and the password grant answers `400 invalid_grant — Account is not fully set up`. Found only by asking the running realm for a token.

---

## ADR-011 — Publish the API as a container built from the repository root

- **Date:** 2026-09-13
- **Proposed by:** AI (Prompt 7, Docker)
- **Status:** Accepted

**Context:** The test requires everything needed to build and run the application and the database with Docker. SQL Server, its init container and Keycloak were already in compose; the API itself was not.

**Options:**
1. Publish the API on the host and copy the binaries into a runtime image.
2. Multi-stage Dockerfile: restore and publish with the SDK image, ship only the ASP.NET runtime.

**Decision:** Option 2, with the build context at the repository root so the four project files are copied and restored before the sources.

**Consequences:**
- The reviewer needs no .NET SDK, only Docker.
- Copying the `.csproj` files first means a source-only change reuses the cached restore layer instead of re-downloading every package.
- The runtime image carries no SDK and no sources.
- The test projects are deliberately outside the build: the image is the deliverable, not the test run. `.dockerignore` also excludes `bin/`, `obj/` and `.env`, so no local build output or secret is ever copied into a layer.
- The API waits on `service_completed_successfully` for the schema container and on Keycloak's health check, so a first `docker compose up` cannot start the API against a database without tables.
- The runtime stage switches to the image's non-root `app` user (`USER $APP_UID`, uid 1654), as SPEC section 11 requires. Nothing in the container writes to disk, so no volume or permission change is needed.
- `DB_PASSWORD` becomes the SA password of the SQL Server container, so it must satisfy the SQL Server password policy (at least eight characters from three of: upper, lower, digit, symbol). A weak value does not fail the API — it stops the database container from ever becoming healthy.

---

## ADR-012 — Enum values cross the wire as names, not numbers

- **Date:** 2026-09-13
- **Proposed by:** AI (Prompt 16, running the compose stack)
- **Status:** Accepted

**Context:** SPEC section 7.4 documents the movement type as `"In" | "Out"`, and `MovementDto` is specified to return the same. `System.Text.Json` serialises enums as their numeric value by default, so the API answered `"type": 1` and rejected the documented request body with `400 — The JSON value could not be converted to RegisterMovementRequest`. Query-string binding hid the problem: `GET /api/inventory/movements?type=Out` works, because model binding uses the type converter rather than the JSON serialiser.

**Options:**
1. Change the spec to numeric values and let the enum serialise as `1` and `2`.
2. Register `JsonStringEnumConverter` globally, so every enum on the contract travels as its name.
3. Take a `string` in the request record and parse it in the controller.

**Decision:** Option 2 — one converter registered on `AddControllers().AddJsonOptions(...)`.

**Consequences:**
- The wire format now matches the spec in both directions, and Swagger shows an `In`/`Out` dropdown instead of an integer field.
- Renaming an enum member becomes a breaking API change. That is the right trade: the names are the contract, and the numeric values stay an internal storage detail of the `TINYINT` column.
- Option 3 was rejected because it moves parsing into the controller and produces a different error shape than every other bad field.
- No unit test caught this. All 138 pass either side of the change, because they construct commands in memory and never cross the serialiser — the same blind spot recorded for the `errors` dictionary in Phase 4.

---

## ADR-013 — Exceptions translated in one place instead of `Result<T>`

- **Date:** 2026-09-29
- **Proposed by:** AI (recorded after the fact, following the project review; the approach was in place since Prompt 3)
- **Status:** Accepted

**Context:** Handlers have four expected ways to fail: invalid input, a missing resource, a conflict with existing data, and a broken domain rule (BR-07 insufficient stock, BR-09 inactive product). Each one must reach the client as a `ProblemDetails` body with the right status code. The domain rules are enforced inside `Product.ApplyMovement`, and validation runs in a MediatR pipeline behavior before the handler.

**Options:**
1. Return `Result<T>` from every handler and domain method, and map failures to HTTP in each controller action.
2. Throw typed exceptions (`ValidationException`, `NotFoundException`, `ConflictException`, `DomainException`) and translate them in a single `IExceptionHandler`.

**Decision:** Option 2. `ValidationBehavior` throws FluentValidation's `ValidationException`, handlers throw `NotFoundException` and `ConflictException`, and the domain throws subclasses of `DomainException`. `ProblemDetailsMapper` is the only place that maps them to `400`, `404`, `409` and `422`; anything else becomes a `500` with a generic detail and is logged.

**Consequences:**
- Handlers read as one linear sequence, and controllers carry no error-mapping code. The response shape is the same for every endpoint, because one mapper produces all of them.
- An exception thrown inside `IUnitOfWork.ExecuteInTransactionAsync` rolls the transaction back with no extra code, which is exactly what BR-08 needs when a domain rule fails midway through a movement.
- The mapper is a pure function, so every status code is covered by `ProblemDetailsMapperTests` without starting the API.
- The cost is that failures are not visible in method signatures: nothing in `Task<ProductDto>` says it can throw `NotFoundException`, and the compiler cannot force a caller to handle it. Expected failures also pay the cost of building a stack trace, which is acceptable at this request volume.
- A new exception type that is not added to the mapper silently becomes a `500`. The mapper tests are the guard against that.

---

## ADR-014 — Web UI: Razor Pages over HTTP, Keycloak sign-in with PKCE, shipped as one `.exe`

- **Date:** 2026-09-29
- **Proposed by:** AI (Prompt 19, phase 3 requirement); the two main choices were mine
- **Status:** Accepted

**Context:** Phase 3 asks for a .NET web interface (Razor Pages or MVC) that consumes this API, covers its main operations, handles loading and error states, validates on the client, and is delivered as a self-contained `win-x64` executable that opens without the SDK. The API requires a Keycloak token on every call.

**Options:**
1. MVC with controllers and views, or Razor Pages with one `PageModel` per screen.
2. Reference the Application DTOs from the UI, or give the UI its own copy of the wire contract.
3. Sign in with a login form that sends the password to Keycloak (password grant), or redirect to Keycloak's login page (authorization code with PKCE).

**Decision:**
- Razor Pages. Every screen is a form or a list with its own handler, so controllers would only add routing ceremony.
- `Inventory.Web` references no other project. A typed `InventoryApiClient` over `IHttpClientFactory` has one method per endpoint and its own records, and any non-success answer becomes an `ApiException` carrying the ProblemDetails `detail` and field errors.
- Authorization code with PKCE against a new public client, `inventory-web`. The password grant is deprecated by OAuth 2.1 and would put credentials in the app. The client is public because a secret inside a distributed `.exe` is not a secret.

**How the pieces fit:**
- A cookie holds the session and the tokens. `TokenRefreshingCookieEvents` trades the refresh token for a new access token a minute before the five-minute Keycloak token expires, and signs the user out if Keycloak refuses.
- `AccessTokenHandler` adds the bearer token to every API call, so no page touches a token.
- Pages catch only the errors a user can fix in the form they sent (`400`, `409`, `422`); field errors land under their field, conflicts and business rules in the summary. `ApiExceptionPageFilter` turns the rest into pages: `401` back to login and then to the same page, `403` to *Access denied*, `404` to *Not found*, and an unreachable API to *API unavailable* with a retry link.
- Client validation uses DataAnnotations that mirror the API's validators, with jQuery unobtrusive validation. A small script disables a submitted form's button and shows a spinner, so a slow call cannot be sent twice.
- Roles come from a `roles` claim that the `inventory-web` client adds to the ID token. Write pages require `inventory.write`, and the write buttons are hidden for `reader`.
- The `.exe` is a single-file, self-contained publish. `ContentRootPath` is the executable's folder, so `wwwroot` and `appsettings.json` are found wherever it is started from, and it opens the browser on start.

**Consequences:**
- The UI tests the API as a real client does. A contract change breaks the UI's JSON reading, which is the point of keeping a separate copy.
- The product lists inside the movement filter and the register form load at most 100 products, the API's page limit. That is enough for this data set; a larger catalogue would need a search box instead of a dropdown.
- The UI must run on `http://localhost:5090`, the only redirect URI the realm accepts. Moving it means changing the realm too.
- Keycloak's login page, not this app, decides what a failed sign-in looks like.
- The UI is in English, like the API and its error messages, so both read as one product.
