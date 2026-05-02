# Signing Session Schema + 24-Hour Expiry Fix Implementation Plan

> For Hermes: Use subagent-driven-development skill to implement this plan task-by-task.

Goal: Make the live SQL Server schema match the current signing-session code, change link lifetime from 7 days to 24 hours, and add verification that catches schema drift against real SQL Server.

Architecture: Keep the current secure-link design: owner creates a session, only the token hash is stored, recipient verifies email, and the session can expire or be revoked. Fix the immediate production blocker by bringing EF migrations and snapshot back in sync with the C# model before any signed-PDF artifact work.

Tech Stack: ASP.NET Core Razor Pages, EF Core 8, SQL Server, xUnit, .NET 8.

---

## Problem Summary

Current code expects `SigningSessions` to contain:
- `ExpiresAtUtc`
- `SignedByName`

But the checked-in migrations and current model snapshot do not include those columns, even though:
- `src/PdfSigning.Web/Models/Documents/SigningSession.cs` defines them
- `src/PdfSigning.Web/Data/ApplicationDbContext.cs` configures them
- `src/PdfSigning.Web/Services/Documents/DocumentSigningService.cs` reads/writes them

That mismatch explains the live SQL error:
- `Invalid column name 'ExpiresAtUtc'`
- `Invalid column name 'SignedByName'`

A second correctness issue is that `DocumentSigningService` still uses a 7-day default, while the agreed demo requirement is 24 hours.

---

## Files to Modify

Primary code files:
- Modify: `src/PdfSigning.Web/Services/Documents/DocumentSigningService.cs`
- Modify: `tests/PdfSigning.Web.Tests/Services/Documents/DocumentSigningServiceTests.cs`
- Create: `src/PdfSigning.Web/Data/Migrations/<timestamp>_AddSigningSessionExpiryAndSignerName.cs`
- Update via EF: `src/PdfSigning.Web/Data/Migrations/<timestamp>_AddSigningSessionExpiryAndSignerName.Designer.cs`
- Update via EF: `src/PdfSigning.Web/Data/Migrations/ApplicationDbContextModelSnapshot.cs`

Optional verification helper if needed during implementation only:
- Temporary only: `/tmp/...` live SQL validation harness

---

## Task 1: Lock the lifetime requirement to 24 hours in tests first

Objective: Make the requirement executable before changing service code.

Files:
- Modify: `tests/PdfSigning.Web.Tests/Services/Documents/DocumentSigningServiceTests.cs`

Step 1: Change the lifetime assertion in `CreateSigningSessionAsync_creates_secure_link_and_assigns_fields`.

Replace the expectation:
- from `clock.UtcNow.AddDays(7)`
- to `clock.UtcNow.AddHours(24)`

Step 2: Keep the existing completion test asserting `SignedByName` is persisted.

This is already valuable because it proves the service behavior that the SQL schema must support.

Step 3: Run the focused test and verify it fails.

Run:
`/home/evanm/.dotnet/dotnet test tests/PdfSigning.Web.Tests/PdfSigning.Web.Tests.csproj --filter CreateSigningSessionAsync_creates_secure_link_and_assigns_fields -v minimal`

Expected:
- FAIL because code still adds 7 days.

Step 4: Commit after the service change in Task 2, not yet.

---

## Task 2: Change signing-link lifetime from 7 days to 24 hours

Objective: Align service behavior with the agreed demo requirement.

Files:
- Modify: `src/PdfSigning.Web/Services/Documents/DocumentSigningService.cs`

Step 1: Replace the constant at the top of the service.

Current:
- `private const int DefaultLinkLifetimeDays = 7;`

Target:
- either `private static readonly TimeSpan DefaultLinkLifetime = TimeSpan.FromHours(24);`
- or a similar 24-hour expression

Step 2: Update session creation to use the new duration.

Current:
- `ExpiresAtUtc = now.AddDays(DefaultLinkLifetimeDays)`

Target:
- `ExpiresAtUtc = now.Add(DefaultLinkLifetime)`

Step 3: Run the focused lifetime test again.

Run:
`/home/evanm/.dotnet/dotnet test tests/PdfSigning.Web.Tests/PdfSigning.Web.Tests.csproj --filter CreateSigningSessionAsync_creates_secure_link_and_assigns_fields -v minimal`

Expected:
- PASS

Step 4: Run the full signing-service tests.

Run:
`/home/evanm/.dotnet/dotnet test tests/PdfSigning.Web.Tests/PdfSigning.Web.Tests.csproj --filter DocumentSigningServiceTests -v minimal`

Expected:
- PASS

Step 5: Commit.

Suggested commit:
`git commit -m "fix: set signing links to 24 hour lifetime"`

---

## Task 3: Generate the missing EF migration for SigningSessions

Objective: Create the schema change that the live SQL database needs.

Files:
- Create: `src/PdfSigning.Web/Data/Migrations/<timestamp>_AddSigningSessionExpiryAndSignerName.cs`
- Update: matching `.Designer.cs`
- Update: `src/PdfSigning.Web/Data/Migrations/ApplicationDbContextModelSnapshot.cs`

Step 1: Generate the migration from the current model.

Run from repo root:
`/home/evanm/.dotnet/dotnet ef migrations add AddSigningSessionExpiryAndSignerName --project src/PdfSigning.Web/PdfSigning.Web.csproj --startup-project src/PdfSigning.Web/PdfSigning.Web.csproj`

If `dotnet ef` is unavailable on PATH, use the explicit SDK path or ensure the tool is installed first.

Step 2: Inspect the generated migration carefully.

It should add, at minimum:
- `ExpiresAtUtc` to `SigningSessions` as non-nullable `datetimeoffset`
- `SignedByName` to `SigningSessions` as nullable `nvarchar(200)`

Important: because existing rows may already exist, confirm the migration gives `ExpiresAtUtc` a safe default for old data during migration. For a demo database with zero live sessions this is low risk, but the migration still must be valid SQL.

Step 3: Inspect the regenerated snapshot.

Confirm `ApplicationDbContextModelSnapshot.cs` now includes:
- `ExpiresAtUtc`
- `SignedByName`
- existing `AccessTokenHash` unique index remains intact

Step 4: If EF generates unexpected extra changes, stop and trim them before proceeding.

Step 5: Commit.

Suggested commit:
`git commit -m "fix: add missing signing session columns to ef migrations"`

---

## Task 4: Verify migrations in code before touching the live DB

Objective: Prove the repo is internally consistent before applying anything to the running SQL Server.

Files:
- No new repo files required

Step 1: Run the full test suite.

Run:
`/home/evanm/.dotnet/dotnet test tests/PdfSigning.Web.Tests/PdfSigning.Web.Tests.csproj -v minimal`

Expected:
- PASS

Step 2: Optionally generate a SQL script for review.

Run:
`/home/evanm/.dotnet/dotnet ef migrations script --project src/PdfSigning.Web/PdfSigning.Web.csproj --startup-project src/PdfSigning.Web/PdfSigning.Web.csproj`

Expected:
- script includes `ALTER TABLE [SigningSessions] ADD [ExpiresAtUtc] ...`
- script includes `ALTER TABLE [SigningSessions] ADD [SignedByName] ...`

Step 3: If the script looks correct, proceed.

---

## Task 5: Apply the migration to the live development SQL Server

Objective: Bring the real database schema into alignment with the code.

Files:
- No repo file changes expected

Step 1: Use the same effective connection source the app already uses.

Do not print secrets.

Step 2: Apply migrations.

Run either the app startup path or explicit EF update against the configured development database, for example:
`/home/evanm/.dotnet/dotnet ef database update --project src/PdfSigning.Web/PdfSigning.Web.csproj --startup-project src/PdfSigning.Web/PdfSigning.Web.csproj`

Step 3: Verify applied migrations and columns using a safe query path.

Prefer a temporary .NET inspector if direct SQL tooling is unavailable.

Confirm `SigningSessions` now contains:
- `ExpiresAtUtc`
- `SignedByName`
- existing columns already present

Step 4: If migration application fails, stop before making code changes for signed-PDF artifacts.

---

## Task 6: Prove the live signing-link flow works after migration

Objective: Re-run the exact live scenario that previously failed.

Files:
- No permanent repo changes required

Step 1: Re-run the temporary live harness or equivalent app-service call against the real SQL database inside a transaction.

Step 2: Verify `CreateSigningSessionAsync` succeeds without SQL invalid-column errors.

Step 3: Verify the returned expiry is approximately 24 hours from the injected clock/current time.

Step 4: Optionally verify `CompleteSigningAsync` succeeds and can set `SignedByName` in SQL inside a rolled-back transaction.

Success criteria:
- no `Invalid column name` error
- session creation succeeds
- completion succeeds
- live SQL schema now matches the code path

---

## Task 7: Add a regression guard against future schema drift

Objective: Reduce the chance of repeating this exact failure mode.

Files:
- Modify: `docs/PDFSIGNING_PROJECT_MAP.md` or another developer doc only if useful
- Optional: add one integration-style validation test if the project is ready for SQL-backed test infrastructure later

Step 1: Record the rule:
- if entity model changes, generate and inspect the migration immediately
- do not trust InMemory tests alone for schema-sensitive behavior

Step 2: If you want stronger enforcement later, add a SQL-backed test lane or a CI check that fails when model changes exist without a migration.

This can be deferred, but the lesson should be captured.

---

## Verification Checklist

Code-level:
- [ ] `DocumentSigningService` now creates 24-hour links
- [ ] `DocumentSigningServiceTests` expect 24 hours, not 7 days
- [ ] Full test suite passes

Schema-level:
- [ ] New migration exists for `ExpiresAtUtc`
- [ ] New migration exists for `SignedByName`
- [ ] `ApplicationDbContextModelSnapshot.cs` includes both properties
- [ ] Live `SigningSessions` table includes both columns

Workflow-level:
- [ ] Live `CreateSigningSessionAsync` works against SQL Server
- [ ] Live `CompleteSigningAsync` can persist `SignedByName`
- [ ] No invalid-column SQL errors remain

Out of scope for this plan:
- final signed PDF artifact generation
- storing a separate signed artifact key on `Document`
- owner retrieval/download of a final signed artifact

Those should be the next implementation phase after this fix lands.
