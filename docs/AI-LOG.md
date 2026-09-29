# AI Usage Log

This file records how generative AI (Claude, Anthropic) was used during the development of this technical test, as required by the test brief. It lists the main prompts per phase, what the AI produced, and what I did with that output. Architectural decisions derived from these sessions are recorded in `ADR.md`.

Tool: Claude (claude.ai, Project workspace with the test PDF attached).
Working language with the AI: Spanish. All generated artifacts (spec, code, docs) are in English per Clean Code rule 1.

---

## Phase 0 — Planning and specification (2026-09-12)

### Prompt 1 — Roadmap
> "Configuré un proyecto aquí para realizar una prueba. Te di un PDF y las instrucciones, necesito empezar a realizar este proyecto, ¿puedes decirme paso a paso para hacerlo, siguiendo los puntos que dice el PDF?"

**AI output:** An 8-step roadmap (repo + AI docs → solution structure → database → TDD → endpoints → OAuth2 → Docker → Clean Code checklist). The AI flagged that, because the test evaluates Spec-Driven Development, the specification had to be written and committed before any code.

**My action:** Accepted the roadmap. Reordered nothing. Decided to keep SPEC.md, AI-LOG.md, ADR.md and decisions.md in a `docs/` folder.

### Prompt 2 — Specification
> "Sí, por favor" (in response to the offer to draft `SPEC.md` before generating any code)

**AI output:** `docs/SPEC.md` v1: domain model, business rules, full API contract, CQRS handler map, auth setup, environment variables, Docker layout, test plan, Clean Code constraints. The AI made three design assumptions explicitly and asked me to confirm them (see ADR-001..003).

**My action:** Reviewed the spec and answered its three assumptions: kept soft delete for products (ADR-001), chose that products always start at 0 instead of accepting an optional `initialStock` (ADR-002), and accepted Keycloak in docker-compose (ADR-003).

### Prompt 3 — Step 0 in detail
> "En la conversación anterior de este proyecto me diste pasos a realizar, ¿podemos empezar con el paso 0 detalladamente?"

**AI output:** Repository setup instructions, templates for this file, `ADR.md` and `decisions.md`, and the first commit message.

**My action:** Created the repository (`371804d`) and scaffolded the four-layer solution (`0de8862`), then placed the templates in `docs/`.

---

## Phase 1 — Solution structure (2026-09-12)

From here on the tool is **Claude Code in the terminal**, working directly on the repository, not the claude.ai chat workspace. That matters for the evidence: every prompt below produced commits, and the commit hashes are quoted so each claim can be checked against `git log`.

### Prompt 4 — Apply the generated files and order the project
> "tengo el archivo files (1) dentro de inventory-api, puedes ordenar el proyecto con base a ese archivo zip. Pasos a ejecutar. Copia los archivos. Copia .env.example a .env y agrega .env al .gitignore. Verifica los paquetes en Inventory.Infrastructure [...] En Program.cs de Inventory.Api, despues de var builder = ... builder.Services.AddInfrastructure(builder.Configuration); [...] Levanta la BD: docker compose up -d [...] Commit: feat(db): add SQL Server schema, domain entities and persistence setup. Registra en AI-LOG.md este prompt y en ADR.md la decision «script SQL vs migraciones EF»."

**AI output:** Copied the 17 source files over the Visual Studio stubs (the stubs used block namespaces and were missing usings, so they did not compile), created `.env`, registered `AddInfrastructure`, and stripped the template comments from `Program.cs` to satisfy Clean Code rules 2 and 7.

It flagged two things the instructions had assumed wrongly:
- The instructions said "8.x para net8.0", but the five projects target **`net10.0`** with the package graph already aligned on 10.0.12. It verified the packages against the real target instead of downgrading.
- Docker was not running, so step 4 could not be executed. It said so rather than reporting the step as done.

**My action:** Accepted. I later settled the target framework question myself by choosing .NET 10 (ADR-005).

### Prompt 5 — Structure review
> "puedes checar si estan bien ordenados los archivos dentro del proyecto?"

**AI output:** A layer-by-layer audit: dependency direction with no cycles, namespaces matching folders in all 17 files, one type per file, Clean Code rules 1, 2, 6 and 7 verified file by file, and `bin/obj/.vs/.env` confirmed ignored. It found one real inconsistency — `SPEC.md` said .NET 8 while the code targeted .NET 10 — plus the missing `.editorconfig` that SPEC section 13 itself requires.

**My action:** Asked for the `.editorconfig` and the ADR.

### Prompt 6 — Tooling and the first ADRs
> "si, deja listo el .editorconfig, el ADR"

**AI output:** `.editorconfig` (100 lines) enforcing what the toolchain can enforce: naming conventions for rule 3, `CA1502` complexity for rule 4, file-scoped namespaces as an error. It was explicit about what **cannot** be automated and wrote that into the file as comments: Roslyn has no "zero blank lines" rule (rule 2), one-type-per-file needs StyleCop (rule 6), and no analyzer detects commented-out code (rule 7).

Also ADR-004 (SQL script vs EF migrations), verified against the real script before claiming it was idempotent, and ADR-005 for the framework mismatch, left as `Proposed` for me to decide.

**My action:** Accepted both. Decided ADR-005 in favour of .NET 10 to avoid a coordinated downgrade of every package.

Commits: `d637e27`.

---

## Phase 2 — Database (2026-09-12 / 2026-09-13)

The schema was not written in one pass. It grew twice, both times because the contract in `SPEC.md` demanded a column that did not exist.

### Prompt 7 — Complete BR-03
> "BR-03 puedes actualizar el script para poner el IsActive del nuevo campo, puedes hacer los cambios para que generes una nueva columna, ya tienes los datos en launchSettings para la conexion a BD local."

**Context:** the AI had reported that BR-03 ("a product cannot be created with a non-existent **or inactive** category") was only half implemented, because `Category` had no `IsActive` column — so the inactive half was unenforceable.

**AI output:** Added `IsActive` to the entity, the EF configuration and `db/init.sql`, with a `COL_LENGTH` guard so databases created before the change get the column too. Changed `ICategoryWriteRepository.ExistsAsync` to `FindByIdAsync` so the handlers can tell the two failures apart: unknown category gives `404`, inactive category gives `409`.

It also found that my local database was **not** on `localhost,1433` as `launchSettings.json` claimed, but on `localhost\SQLEXPRESS`, and that a stray database literally named `InventoryDb` existed from a previous run of the script without its variable.

**My action:** Fixed the local SQL Server so it listens on `localhost,1433`.

### Prompt 8 — Logical delete and the `initialStock` contradiction
> "Puedes aomodar igual esta parte initialStock para que quede bien con el ADR-002 porque si quiero que sea borrado logico"

**AI output:** Removed `initialStock` from `SPEC.md` section 7.3, which still contradicted ADR-002 after I had only fixed section 12. Made category delete logical (ADR-008), which also made `Categories.IsActive` reachable from the API — until then no endpoint could set it.

It flagged a consequence I had not considered: category names stay unique after a logical delete, so re-creating a deleted category's name conflicts, and reactivating is the way back.

**My action:** Accepted.

Commits: `fcd17fa`, `b2d2ecc`, `24e56cf`, `4704033`.

---

## Phase 3 — TDD: commands and queries (2026-09-13)

### Prompt 9 — Start the TDD cycle
> "Seguiriamos con el paso 3? [...] Por cada command/query: escribe el test, falla, implementa, refactoriza [...] Haz commits pequenos: test: ... seguido de feat: ... — eso deja evidencia de TDD en el historial. o que hace falta. Iguamente ya hice commit como ves? alguna recomendacion o detalle que se me este olvidando"

**AI output:** Before writing code, it reviewed the state and reported a **blocker**: ADR-002 (accepted, products always start at 0) contradicted `SPEC.md` section 12, which still asked for an `initialStock` test. It refused to guess and asked which one won.

It also listed what was missing before TDD could start (exceptions the test plan referenced but that did not exist, write-repository interfaces, the MediatR pipeline), flagged that my commit message `"Building entities and sql enviroment with classes in repo"` broke the Conventional Commits convention of the rest of the history and had a typo, and found a genuine gap: **BR-09 was not implemented** — `Product.ApplyMovement` validated stock but never checked whether the product was active.

**My action:** Resolved the contradiction by removing the `initialStock` case from section 12, and marked ADR-002 accepted. I chose not to amend the commit message.

### Prompt 10 — Run the cycle
> "ya hice las modificaciones con los docs. puedes arrancar con los pasos" / "vete con los puntos que siguen" / "si, sigue con los handlers y queries que faltan"

**AI output:** Eleven `test:` then `feat:` pairs. Every `test:` commit was pushed **actually red** — the suite did not compile, because the types the test referenced did not exist yet — and the following `feat:` made it green. That is the TDD evidence in the history.

The tests drove the design rather than the reverse: `IUnitOfWork`, the three write repositories and `NotFoundException` all exist in the shape the handler tests demanded.

Two judgement calls it made and documented:
- **ADR-006**: `IUnitOfWork` takes a delegate instead of threading `IDbTransaction` through every repository method. This makes "runs inside a transaction" testable without a database — the test configures the mock *not* to invoke the delegate and asserts no repository was touched.
- **ADR-007**: SQLite in-memory instead of the EF Core InMemory provider named in section 12, because InMemory is not relational and silently ignores the unique SKU index, which would make the filter tests pass for the wrong reason.

**My action:** Accepted both ADRs.

### Prompt 11 — Ask for the recommended implementation
> "Cual seria lo recomendado para ADR-006?" / "Puedes editar ADR-006 a lo mas recomendado y facl, para que no quede inconsistente con el proyecto"

**AI output:** Rewrote ADR-006 with a "How Infrastructure implements it" section: one `Scoped` `SqlConnectionContext` owning the connection and the ambient transaction, shared by `SqlUnitOfWork` and the three Dapper repositories, plus the registration rules that keep it correct (`Scoped`, never `Singleton`, because `SqlConnection` is not thread-safe).

It insisted the two integration tests were part of the decision, not an optional extra, because the failure mode is silent: if a repository opens its own connection, BR-08 breaks, the build succeeds and the unit tests still pass since they mock `IUnitOfWork`.

**My action:** Accepted. The two tests exist and pass (`Inventory.IntegrationTests`).

Commits: `3329a91` through `3d84eb2` (eleven test/feat pairs), plus `4704033`.

---

## Phase 4 — API endpoints and Swagger (2026-09-13)

### Prompt 12
> "si, sigue con el paso 5"

**AI output:** Four controllers over MediatR, the request contracts, Swashbuckle, and `ProblemDetailsExceptionHandler` producing the RFC 7807 envelope of section 7.1 — until then nothing produced it and every exception would have surfaced as a 500.

It then **started the API and exercised it with curl against the real database**, which is how it found two defects the unit tests could not:

1. **The `400` response arrived without the `errors` dictionary.** The mapper built a correct `ValidationProblemDetails` and its unit test passed, but the handler serialised it through the `ProblemDetails` static type and `System.Text.Json` dropped the derived property. It wrote a test that reads the written body, committed it red (`4526f87`), then fixed it.
2. **Validation messages came out in Spanish.** FluentValidation follows the machine culture, so the same `400` would answer in English on the reviewer's machine — non-deterministic, and mixed with our own English messages. Pinned with `ValidatorOptions.Global.LanguageManager.Culture`.

It also cleaned up the rows its smoke tests had written to my database.

**My action:** The AI generated the controllers and the exception handling from my spec; I defined the error format (RFC 7807, SPEC section 7.1).

Commits: `e39fad4`, `4526f87`, `ef9db77`.

---

## Phase 5 — OAuth2 (2026-09-13)

### Prompt 13
> "si, sigue con el paso 6"

**AI output:** JWT Bearer validation, `inventory.read` on GET and `inventory.write` on writes, the Keycloak realm export, and the Swagger Authorize button. Verified by HTTP that every endpoint answers `401` without a token and that `/health` stays anonymous — before this, `UseAuthorization()` was wired but **no endpoint carried `[Authorize]`, so the API was completely open**.

It identified three failure modes that all compile and start cleanly and only fail when a real token arrives (ADR-010):
- Keycloak puts realm roles in a `realm_access` claim as raw JSON; `JwtBearer` leaves it alone, so `RequireRole` never matches and a valid token with the right role still gets `403`.
- Keycloak issues `aud=account` by default; without an audience mapper in the realm, every token is rejected.
- The token issuer and the discovery address are different hosts under Docker.

**My action:** I reviewed each failure mode against the difference between `401` (the API cannot identify the caller: missing or invalid token) and `403` (the caller is identified but lacks the required role). That distinction pointed to the cause of each case: the `aud=account` issue produced `401` because the token itself was rejected, while the `realm_access` issue produced `403` because the token was valid but the roles were not being read. I accepted the three fixes the AI proposed: a claims transformation that maps `realm_access.roles` into standard role claims, an audience mapper in the realm export, and separate settings for the issuer and the discovery address. Docker was not running yet, so I could only verify them with real tokens once the stack ran in Phase 8, which is where the issuer fix turned out to be incomplete.

Commits: `09dce8a`, `06aaf69`, `4a2c7d8`.

---

## Phase 6 — Docker (2026-09-13)

### Prompt 14
> "si, sigue con el paso 7"

**AI output:** Multi-stage `Dockerfile` restoring from the project files before copying sources, the `api` service in compose waiting on the schema container and Keycloak's health check, `.dockerignore` excluding `.env`, and `.gitattributes` forcing LF on the files that break with CRLF on a Linux reviewer's machine (ADR-011).

**Reported honestly as unverified at the time:** Docker Desktop never started during any of those sessions ("Docker Desktop is unable to start"). `docker compose config` validated the YAML, but the stack had never been built or run, and the AI refused to report the Docker requirement as satisfied. Phase 8 closes this.

**My action:** Left the Docker requirement open until the daemon worked, rather than claiming it.

Commits: `d56ee3c`, `11f9749`.

---

## Phase 7 — README and final review (2026-09-13)

### Prompt 15
> "si, redacta las entradas de AI-LOG de las fases 1 a 7"

**AI output:** The `README.md` (setup, Docker, local run, both test suites, how to get a token for either user) and this log.

**My action:** I added to the README how to build the Docker image and run the API locally.

---

## Phase 8 — Running the stack for real (2026-09-13)

### Prompt 16
> "puedes revisar el proyecto, ya solo faltaria un paso, pero me quede en la parte donde debia configurar docker. Puedes revisar porfavor"

**AI output:** Docker Desktop was working this time, so the AI built the image and ran `docker compose up --build` end to end. The YAML that `docker compose config` had validated in Phase 6 turned out to hide three defects, none of which any test or static check could see:

1. **`DB_PASSWORD=admin` in my local `.env`.** It becomes `MSSQL_SA_PASSWORD`, and SQL Server refuses it, so the database container never becomes healthy and nothing downstream starts. `.env.example` already carried a strong value; my copy did not.
2. **Every valid token was rejected with `401 — The signature key was not found`.** `KC_HOSTNAME` rewrites all advertised URLs, so the discovery document served at `keycloak:8080` still pointed `jwks_uri` at `localhost:8080` — which, inside the API container, is the API itself. Fixed with `KC_HOSTNAME_BACKCHANNEL_DYNAMIC=true` (ADR-010 correction).
3. **No token could be obtained at all**, because the imported realm users had no `email` and Keycloak 24+ stamps them with a `VERIFY_PROFILE` required action: `400 invalid_grant — Account is not fully set up`.

Two further defects surfaced from calling the running API:

4. **`POST /api/inventory/movements` rejected its own documented body.** `{"type":"In"}` did not deserialise and the response returned `"type": 1`, both against SPEC section 7.4. Fixed with a global `JsonStringEnumConverter` (ADR-012).
5. **The container ran as root**, although SPEC section 11 asked for a non-root user. Fixed with `USER $APP_UID`.

**Verified after the fixes**, from a torn-down volume and a fresh `docker compose up --build -d --wait`: all four containers reach their expected state (`sqlserver-init` at `Exited (0)`), `/health` and Swagger answer `200`, `/api/products` answers `401` without a token and `200` with one, `reader` gets `403` on a write while `admin` gets `201`, the password and client-credentials grants both work, and the business rules answer on the wire — `409` for a duplicate name and for deleting a category with active products, `400` with the field dictionary, `404`, and `422` for both insufficient stock (BR-07) and an inactive product (BR-09). A movement pair `In 7` / `Out 3` left the product at `stock 4` with `stockAfter` stamped `7` then `4`. The 138 unit tests stay green after the serialiser change.

**My action:** Defect 1 also exposed a wider problem: weak credentials were versioned in the repository. I moved every secret out of `docker-compose.yml` into a `.env` file excluded from Git, left only variable references in the compose file, and documented each variable in `.env.example` with a value that meets the SQL Server password policy. I also removed the `admin` password fallback from the integration tests' `DatabaseScopeFactory`. The API already fails at startup when a `DB_*` variable is missing, and `docker compose` now refuses to start without `DB_PASSWORD`, instead of silently using a weak default.

A later commit of mine (`86052e5`) reintroduced `admin` hard-coded in `docker-compose.yml`, which again kept SQL Server from starting. The project review caught it, and the compose file went back to reading every value from `.env` (Phase 9).

---

## Phase 9 — Acting on the project review (2026-09-29)

### Prompt 17
> "ayudame con este punto del feedback del proyecto, como lo harias?" / "la A, arma el borrador y el ADR-013" / "limpia los TODO del AI-LOG" / "puedes corregir el codigo porfavor"

**Context:** the review marked `docs/decisions.md` as critical: the template comment was still in the file, answer 1 ended in an empty `**Outcome:**`, and the answers had spelling mistakes. It asked for real decisions from this project, each closed with a concrete outcome.

**AI output:**
- It mapped the three questions to decisions already recorded in `ADR.md` (ADR-010, ADR-006 and ADR-008), checked each technical claim against the code, and drafted the answers.
- It pointed out that the file's header says the answers are mine, so it offered two options: I rewrite its draft in my own words, or the header says the AI wrote them. I chose the first.
- It wrote ADR-013 (exceptions translated in one place instead of `Result<T>`), a decision that was already in the code but had never been recorded.
- It filled the two `TODO` entries of this log that git history could confirm, and asked me about the rest.
- Reviewing this log against the repository, it found that my commit `86052e5` had hard-coded `admin` as the SQL Server password in `docker-compose.yml`, so `docker compose up` could not start the database and the log claimed the opposite. It restored the `${DB_*}` variables, made `DB_PASSWORD` mandatory, and removed the `admin` fallback from the integration tests. It also found three statements in this log that contradicted other entries, and corrected them.

**My action:** I rewrote the three answers in `decisions.md` in my own words, and the AI then corrected spelling and grammar. I answered the open `TODO` entries of this log myself.

**What the AI proposed that I rejected or changed.**
- It proposed amending my commit `d637e27` to follow Conventional Commits. I kept the history as it was, because rewriting commits already pushed to a public repository changes their hashes for anyone who has cloned it, and applied the convention strictly from that point on.
- ADR-005 was left open as `Proposed`, with the framework version undecided. I chose .NET 10, which meets the ".NET 8 or higher" requirement and avoided downgrading packages already referenced.

**Deviations from the spec found during review.**
The AI's reviews against `SPEC.md` (Prompts 5 and 9) and its runs of the real API (Phases 4 and 8) found the following gaps in code it had generated:
- BR-09 was never implemented: `Product.ApplyMovement` checked stock but not whether the product was active.
- BR-03 was only half implemented, because `Categories.IsActive` did not exist.
- `MovementDto` returned `stockAfter` with no column behind it (ADR-009).
- `SPEC.md` section 7.3 still specified `initialStock` after ADR-002 had removed it.
- Every endpoint was unprotected: `UseAuthorization()` was present, but no `[Authorize]` attribute existed.
- `MovementType` crossed the wire as `1` and `2` and rejected the documented `"In"` / `"Out"` body, against SPEC section 7.4 (ADR-012).
- The container ran as root, although SPEC section 11 required a non-root user.

**Mistakes the AI made.**
- Removing `initialStock` from the spec also deleted the `Body/Params` cell of the `POST /products` row, leaving a broken three-column table. It was caught and repaired in `19cb956`.
- A `git add -A` swept an unrelated edit I was making to `ADR.md` into commit `c191398`. Since then, I stage files explicitly by path.

**Mistakes I made.**
- I used a deliberately weak password in my local `.env` to make testing easier. SQL Server rejected it, so the database never started and the whole stack failed. It also reflected a wider problem: weak credentials were versioned in the repository. I moved every secret into an untracked `.env`, documented each variable in `.env.example` with a value that meets the SQL Server password policy, and removed the `sa/admin` fallback from the code. The lesson: a convenient shortcut in development can hide exactly the failure a reviewer will hit first.

**Where I worked by hand.**
- I chose the architecture from the AI's recommendations (ADR-000): the layers, the models each one owns, and how the services that run the application fit together. I then created the solution structure myself in Visual Studio (`0de8862`): four Clean Architecture projects plus two test projects. The AI's 8-step roadmap (Prompt 1) was the plan I followed.

**Where verification came from running the code, not from tests.**
I ran the database scripts locally and checked that the tables and indexes were created as specified. The two most valuable defects of the whole exercise, the missing `errors` dictionary and the Spanish validation messages, were found by starting the API and calling it with curl. Both had passing unit tests. Phase 8 repeated the lesson at the infrastructure level: `docker compose config` passed, the image built, all 138 tests were green, and the stack still could not issue a token, validate one, or accept the movement body documented in its own spec. This is the main thing I take from the exercise: a green test suite proves the units, not the wire format, and a valid configuration file proves the syntax, not the system.

### Prompt 18 — Written feedback, in Claude Code (2026-09-29)
> "puedes darle una revisada a los cambios con base al documento [Fernando_Feedback.pdf] para ver si esta todo bien cumpliendo?" / "sí, sigue con esos puntos" / "sigue con decisions.md"

**Context:** the written feedback for phase 3 arrived as a PDF. It asked for four things: `decisions.md` rewritten with real decisions and concrete outcomes; unique-index violations answered with `409` instead of `500`; the second `_sender.Send` removed from the `POST` actions; and every secret moved out of the repository, with `[Authorize]` at class level on `ProductsController` and `InventoryMovementsController`. This session ran in Claude Code, inside the repository, instead of claude.ai.

**AI output:**
- It checked every point of the PDF against the repository and listed what was done, partly done and missing.
- `409` on a duplicate (`a50b9a1`): the write repositories translate SQL errors 2601 and 2627 into `ConflictException`, so the mapper does not depend on an infrastructure type. Three integration tests write the duplicate straight into SQL Server.
- One `Send` per `POST` (`7d81b83`): `CreateProduct`, `CreateCategory` and `RegisterInventoryMovement` return their DTO, built from the entity they just wrote. The same commit added `[Authorize]` at class level on the two controllers.
- Secrets (`987a723`): the `inventory-api` client secret and the demo users' passwords became `${...}` placeholders that Keycloak fills from `.env` on import, and compose refuses to start without them. `launchSettings.json` no longer carries `DB_PORT` or `DB_PASSWORD`, which moved to user secrets. `DatabaseScopeFactory` lost its `localhost/sa` fallback: the integration tests read the repository's `.env` and fail naming any missing variable.
- For `decisions.md`, it drafted the section *Other decisions behind this API* (ADR-000 and ADR-013) and proposed rephrasings for the three answers. Because the file says the answers are mine, it kept its drafts out of the repository until I chose what to use.

**My action:**
- I brought up SQL Server and Keycloak with Docker and ran the integration tests myself. I confirmed that the client-credentials grant works with the secret from `.env`; the AI then checked that the old secret and `admin/admin` are rejected.
- In `decisions.md` I rewrote the Unit of Work example, merged the last sentences of answer 1, and pasted the new section. I accepted the AI's criterion for answer 2 and its "What I learned" for answer 3, and I changed the header so it says which parts are rephrasings or drafts by the AI.

**Problems found while verifying.**
- The first SQL Server container was created with the example password from `.env.example`, not mine. SQL Server only applies `MSSQL_SA_PASSWORD` when the volume is created, so I recreated it with `docker compose down -v`.
- The integration tests still failed with `Login failed for user 'sa'`, because a local SQL Server Express instance was also listening on port 1433 and answered first. I moved the container to port 14330 through `DB_PORT`.

**Mistakes the AI made.**
- It checked the new password with `sqlcmd` inside the container and reported that it worked. That check could not see the SQL Server Express instance on the host, so the tests failed when I ran them.
- It told me that answers 2 and 3 of `decisions.md` had six sentences each; they have seven and eight. It corrected the count when it reviewed the final file.

**Known trade-off.** A `POST` now returns `createdAt` with fractions of a second, taken from the entity, while a later `GET` returns it rounded to the second, because the column is `DATETIME2(0)`.


### Prompt 19 — Web UI for phase 3, in Claude Code (2026-09-29)
> "sí, empieza con la interfaz web, que seria lo recomendado?"

**Context:** phase 3 adds a requirement: a .NET web interface (Razor Pages or MVC) that consumes this API, delivered as a self-contained Windows `.exe`. The review looks at UI judgement (clarity, usability, loading and error states) and at how the API is consumed (`HttpClient`, `async/await`, data mapping, client validation).

**AI output:**
- A recommendation, with the alternatives, for the project type, how the UI reaches the API, how users sign in, the screens, and how the `.exe` is built. I chose Razor Pages and Keycloak sign-in with authorization code and PKCE, both the recommended options (ADR-014).
- The `Inventory.Web` project: a typed `InventoryApiClient`, cookie sign-in with token refresh, a bearer-token handler, a page filter for API errors, and the dashboard, product, category and movement pages. Plus a new `inventory-web` client in the realm, and the `Inventory.Web.Tests` project with 24 tests.
- ADR-014 and the *Web UI* section of the README, with the publish command.

**Verified by running it, not only by tests.** With the compose stack up, the AI drove the real sign-in through Keycloak with `curl` for both users and exercised every screen. `admin` created a category and a product, registered an entry and an exit, edited the price and deleted both records. The API's answers reached the forms: a duplicate SKU (`409`), a negative price (`400`, under its field), an exit larger than the stock (`422`), and deleting a category with active products (`409`). `reader` saw no write button and got *Access denied* on every write page. Stopping the API container led to *API unavailable*, whose retry link returned to the same search. Signing out also ended the Keycloak session. With the published `.exe`, a session left idle past the five-minute token lifetime kept working: the next request refreshed the token and renewed the cookie, with no new sign-in.

**Mistakes the AI made.**
- The rule "every page requires a signed-in user" also covered CSS and scripts, so the error pages shown to signed-out users would have had no styles. Running the published `.exe` caught it; static files are now public.
