# Decision-Making Questions

Answers to the three questions in the "Toma de decisiones" section of the test. The three answers are written by me from my own experience; the AI reviewed their structure and wording, and some sentences are its rephrasings that I accepted. The section *Other decisions behind this API* was drafted with the AI from ADR-000 and ADR-013 and reviewed by me.

Each answer uses a decision taken while building this API, so it can be checked against the code and against the matching record in [ADR.md](ADR.md).

## 1. A recent technical decision made with incomplete information

**Context:** The API validates Keycloak tokens, and under Docker the browser reaches Keycloak at `localhost:8080` while the API container reaches it at `keycloak:8080`. When I configured authentication, Docker Desktop was not working on my machine, so I could not run the full stack and see a real token go through.

**Decision:** I split the setting in two, `KEYCLOAK_AUTHORITY` for the issuer stamped on the token and `KEYCLOAK_METADATA_ADDRESS` for the discovery document, and pinned the issuer with `KC_HOSTNAME`. I accepted the gap and wrote it down in ADR-010 instead of waiting, because everything else could move forward on unit tests.

**Outcome:** The first real run returned `401 — The signature key was not found` for every valid token. `KC_HOSTNAME` also rewrote the key-set URL to `localhost`, which inside the container is the API itself. One extra variable, `KC_HOSTNAME_BACKCHANNEL_DYNAMIC=true`, fixed it, and I recorded the correction in the same ADR. Next time, I will prepare all the settings needed to run the complete system first, so every error can be fixed as soon as it appears.

## 2. Criteria for choosing between two equally valid technical approaches

**Criterion:** When two designs both work, I pick the one I can verify with a fast test, even if it takes more code to build. If both are equally easy to test, I pick the one that keeps infrastructure details out of the inner layers.

**Example, the Unit of Work (ADR-006):** Writes go through Dapper, so every transaction has to be explicit, and business rule BR-08 requires the movement insert and the stock update to commit together. Two designs were valid: pass an `IDbTransaction` into every repository method, or wrap the whole command in `IUnitOfWork.ExecuteInTransactionAsync(...)`, with every repository sharing one scoped `SqlConnectionContext`. I chose the second because it keeps `System.Data` out of the Application layer, and a unit test with a mock, without a database, can prove that a handler runs inside a transaction.

**Outcome:** Every write handler has that test. Mocks cannot see a repository that opens its own connection, so two integration tests against the real database cover that case.

## 3. A technical decision I had to revert or change

**Original decision:** Products were deleted logically (ADR-001), but categories kept a physical `DELETE`, protected by a rule that refuses a category that still has active products.

**Why it had to change:** When I reviewed the business rules, I saw that `Categories.IsActive` existed but no endpoint could ever set it to false. Half of rule BR-03 ("an inactive category cannot receive products") could only be reached by editing the database by hand, and the API had two different meanings for "delete".

**What changed and the outcome:** In ADR-008 I made `DELETE /categories/{id}` logical, the same as for products, and kept the check for active products before anything is written. `GET /categories` now filters by `IsActive`, and the trade-off is written down: a deleted category's name stays taken, so the way back is to reactivate it.

**What I learned:** The gap was in code generated with AI that compiled and passed its tests, so only a review of the business rules against the endpoints could find it. Building a system with AI works when I take deliberate decisions about the architecture and check the result against the specification. It is also important to document what I asked the AI and how it built each part, because that record is what let me trace this gap back to its origin.

## Other decisions behind this API

Two more decisions shape the whole codebase. Both are recorded in [ADR.md](ADR.md).

### EF Core for reads, Dapper for writes (ADR-000)

**Decision:** I split the application into queries and commands, so each ORM has one job and they never meet in the same handler. Query handlers use EF Core with `AsNoTracking` and project straight into a DTO, because filters, paging and joins compose easily in LINQ. Command handlers write through Dapper, because a write needs an explicit transaction and exact SQL, such as `OUTPUT INSERTED.Id` to return the new key.

**Outcome:** Both paths share one schema, so it lives in the idempotent `db/init.sql` (ADR-004) instead of EF Core migrations, and the read handlers are tested against SQLite in-memory (ADR-007). The cost is keeping the EF mapping and the SQL in sync by hand, so an integration test checks that Dapper still loads every field of `Product` from SQL Server.

### Exceptions instead of `Result<T>` (ADR-013)

**Decision:** Handlers can fail in four expected ways: invalid input, a missing resource, a conflict with existing data, or a broken domain rule such as insufficient stock. Each one is a typed exception, and `ProblemDetailsMapper` is the only place that turns them into `400`, `404`, `409` or `422`. I chose this over `Result<T>` because handlers stay linear, every endpoint returns the same error shape, and an exception inside the unit of work rolls the transaction back without extra code.

**Outcome:** The cost showed up in the project review: a unique-index violation from SQL Server was not a mapped type, so two identical requests arriving together returned `500` instead of `409`. The write repositories now translate SQL errors 2601 and 2627 into `ConflictException`, and an integration test forces a real duplicate against SQL Server to prove it.
