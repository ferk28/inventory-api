# Decision-Making Questions

Answers to the three questions in the "Toma de decisiones" section of the test. These are written by me from my own experience; the AI was only used to review structure and English wording.

Each answer uses a decision taken while building this API, so it can be checked against the code and against the matching record in [ADR.md](ADR.md).

## 1. A recent technical decision made with incomplete information

**Context:** The API validates Keycloak tokens, and under Docker the browser reaches Keycloak at `localhost:8080` while the API container reaches it at `keycloak:8080`. When I configured authentication, Docker Desktop was not working on my machine, so I could not run the full stack and see a real token go through.

**Decision:** I split the setting in two, `KEYCLOAK_AUTHORITY` for the issuer stamped on the token and `KEYCLOAK_METADATA_ADDRESS` for the discovery document, and pinned the issuer with `KC_HOSTNAME`. I accepted the gap and wrote it down in ADR-010 instead of waiting, because everything else could move forward on unit tests.

**Outcome:** The first real run returned `401 — The signature key was not found` for every valid token. `KC_HOSTNAME` also rewrote the key-set URL to `localhost`, which inside the container is the API itself. One extra variable, `KC_HOSTNAME_BACKCHANNEL_DYNAMIC=true`, fixed it, and I recorded the correction in the same ADR. Next time, I will prepare all the settings needed to run the complete system first. It is important because every error can then be fixed as soon as it appears.

## 2. Criteria for choosing between two equally valid technical approaches

**Criterion:** When two designs both work, I pick the one that lets me test the whole process, even if it has more steps and is harder to build. I also look at how the environments and systems work, and whether there is a tool that makes testing faster.

**Example, the Unit of Work (ADR-006):** The test requires EF Core for reads and Dapper for writes, so I split the application into queries and commands (ADR-000), and the write side needs an explicit transaction. Business rule BR-08 requires the movement insert and the stock update to commit together. Two designs were valid: pass an `IDbTransaction` into every repository method, or wrap the whole command in `IUnitOfWork.ExecuteInTransactionAsync(...)` over one scoped `SqlConnectionContext` that every repository shares. I chose the second because "runs inside a transaction" can then be tested with a mock and no database, and `System.Data` stays out of the Application layer.

**Outcome:** Every write handler has that test. Mocks cannot see a repository that opens its own connection, so two integration tests against the real database cover that case.

The same criterion decided error handling (ADR-013): I used exceptions translated in one place instead of `Result<T>`, because it kept handlers linear and gave every error the same response shape.

## 3. A technical decision I had to revert or change

**Original decision:** Products were deleted logically (ADR-001), but categories kept a physical `DELETE`, protected by a rule that refuses a category that still has active products.

**Why it had to change:** When I reviewed the business rules, I saw that `Categories.IsActive` existed but no endpoint could ever set it to false. Half of rule BR-03 ("an inactive category cannot receive products") could only be reached by editing the database by hand, and the API had two different meanings for "delete".

**What changed and the outcome:** In ADR-008 I made `DELETE /categories/{id}` logical, the same as for products, and kept the check for active products before anything is written. `GET /categories` now filters by `IsActive`, and the trade-off is written down: a deleted category's name stays taken, so the way back is to reactivate it.

**What I learned:** When products are deleted logically, their data stays available and can be reused instead of being written again. Queries can filter by active or inactive, and that is inexpensive because an index covers the column. And if a product comes back to the store, there is no need to register it again in the system.
