# PdfSigningApp Demo Delivery Master Plan

> For Hermes / orchestrator agents: treat this as the source-of-truth execution brief for finishing the PDF e-sign project for a live client demo.
> This file is intentionally opinionated. Where the user has not finalized product choices, use the recommended defaults below unless the user explicitly overrides them.

## 1. Mission

Deliver a secure, believable, demo-ready PDF e-sign workflow that shows:
- an authenticated owner uploading a PDF
- the owner placing required signature fields
- the owner preparing a secure recipient signing session
- the recipient opening a private signing link
- recipient verification before document access
- recipient signing completion
- owner visibility into document status and completion
- a final signed PDF artifact that the owner can retrieve and show during the client demo

This plan prioritizes:
- security over convenience
- a clean happy-path demo over broad feature scope
- small verifiable phases over large risky rewrites
- private handling of secrets, links, documents, passwords, and keys at all times

---

## 2. Recommended Demo Defaults

These are the default assumptions unless the user changes them later.

### Product defaults
- Single internal owner account model for the demo
- Single external signer per document
- One document at a time per demo walkthrough
- Typed signature plus signer name and timestamp for the demo baseline
- One active signing session per document at a time
- 24-hour default session expiry
- Manual revoke support
- Single-use completion behavior once signing finishes
- Recipient must verify by entering the email address tied to the session before viewing/signing
- Final signed PDF artifact is a required demo outcome, not optional polish

### Technology defaults
- ASP.NET Core Razor Pages on .NET 8
- ASP.NET Identity for owner authentication
- Entity Framework Core with SQL Server
- Local private filesystem storage outside webroot for demo and short-term production direction
- PdfSharpCore for PDF operations already in use
- Shared CSS in `wwwroot/css/site.css`
- Progressive enhancement JavaScript only where it materially improves UX

### Demo operating defaults
- No real user data in demo environments
- No real passwords, API keys, connection strings, or recipient links shown in docs, commits, logs, screenshots, or chat transcripts
- Demo secrets remain in `.env.local`, .NET User Secrets, or environment variables only
- Demo must be internet-accessible for remote use, but local rehearsal on the host machine should still be supported first
- Owner is allowed to copy and view signing links in the UI for controlled manual sharing during the demo

### Email defaults
- Preferred for this demo: generate secure links in-app for controlled manual sharing
- Real email sending is optional later hardening, not a blocker for demo readiness

---

## 3. Non-Negotiable Security Invariants

These rules are mandatory for all implementation and demo preparation work.

1. Never commit secrets
   - no real passwords
   - no API keys
   - no SMTP credentials
   - no user-secrets payloads
   - no connection strings with real passwords
   - no raw signing tokens in source or docs

2. Never store raw signing tokens in the database
   - only store a secure hash of the token
   - raw token is returned once at creation time only

3. Never serve private PDFs from public static paths
   - documents and signed outputs must live outside `wwwroot`
   - files must be streamed through authorized application endpoints

4. Never trust the link alone
   - require server-side session validation
   - require recipient email verification before access to document content
   - reject expired, revoked, malformed, or mismatched sessions

5. Never trust client-side authorization
   - all document, field, PDF, and session actions must enforce server-side checks
   - owner-scoped actions and recipient-scoped actions must remain logically separate

6. Never leak secrets in logs or UI
   - do not print raw tokens
   - do not log connection strings
   - do not expose stack traces or sensitive storage paths to users
   - redact or avoid sensitive PII where practical

7. Always use defense-in-depth on mutations
   - anti-forgery on write actions
   - POST for state-changing operations
   - validation on all user inputs
   - conservative file upload validation

8. Always assume demo artifacts can leak
   - use fake recipient data only
   - use non-sensitive sample PDFs only
   - rotate or replace any accidental demo secrets immediately

---

## 4. Client Demo Scope

### In scope
- Owner login
- PDF upload
- Private PDF storage
- Document list and detail view
- Signature field placement and deletion
- Secure signing session creation
- Recipient email verification gate
- Recipient signing experience
- Document completion state
- Basic owner visibility into signing status
- Security-conscious handling of tokens, files, and secrets

### Strongly preferred for the demo
- Final signed PDF artifact generation and retrieval
- Revoke signing session action
- Friendly but non-leaky error states for expired/revoked/wrong-email flows
- Basic audit trail timestamps
- Demo checklist / rehearsal instructions

### Out of scope unless explicitly requested
- Multi-recipient routing / signing order
- Advanced PKI / certificate-backed digital signatures
- Full production-grade email infrastructure if not already stable
- Complex RBAC or organization/team support
- Public self-signup / tenant onboarding
- External OAuth / SSO integration
- Long-term compliance implementation beyond demo-safe best practices

---

## 5. Golden Demo Story

The target demo should be explainable in under 3 minutes.

1. Owner signs in.
2. Owner uploads a sample PDF.
3. Owner opens document details.
4. Owner places one or more required signature fields.
5. Owner creates a signing session for a sample recipient email.
6. Owner shares the secure link in the approved demo-safe manner.
7. Recipient opens link.
8. Recipient verifies email.
9. Recipient signs.
10. App records completion and updates document status.
11. Owner sees completion state and, ideally, opens the final signed artifact.

### Failure states worth demoing if time permits
- wrong recipient email
- expired link
- revoked link
- attempting reuse after completion

---

## 6. Trust Boundaries and Threat Model

### Actors
- Owner: authenticated internal user managing documents
- Recipient: external user with one scoped signing session
- App server: trusted enforcement point
- Database: stores metadata, sessions, and token hashes
- Private file storage: stores uploaded PDFs and signed artifacts
- Browser: untrusted client; all permissions must be re-checked server-side

### High-risk assets
- uploaded PDFs
- completed signed PDFs
- raw signing tokens
- recipient PII such as email address
- owner credentials
- connection strings
- SMTP or email provider credentials
- any cloud storage credentials

### Primary risks
- IDOR / owner-document access bypass
- leaked raw signing links
- public file exposure through predictable URLs
- replay of expired or completed sessions
- accidental secret leakage in repo or logs
- malformed or unsafe file upload
- stale sessions staying active too long
- over-broad exception details during demo

### Minimum mitigations
- owner-scoped queries on owner routes
- session + token hash + recipient email verification on recipient routes
- expiration and revocation enforced server-side
- private file storage only
- generic user-facing errors
- test coverage for failure paths

---

## 7. Current Repository Map

### Solution and projects
- `PdfSigningApp.sln`
- `src/PdfSigning.Web/PdfSigning.Web.csproj`
- `tests/PdfSigning.Web.Tests/PdfSigning.Web.Tests.csproj`

### App bootstrap
- `src/PdfSigning.Web/Program.cs`
  - configures SQL Server, Identity, Razor Pages, services, and migrations at startup

### Data layer
- `src/PdfSigning.Web/Data/ApplicationDbContext.cs`
- `src/PdfSigning.Web/Data/ApplicationDbContextFactory.cs`
- `src/PdfSigning.Web/Data/Migrations/*`

### Core models
- `src/PdfSigning.Web/Models/ApplicationUser.cs`
- `src/PdfSigning.Web/Models/Documents/Document.cs`
- `src/PdfSigning.Web/Models/Documents/SignatureField.cs`
- `src/PdfSigning.Web/Models/Documents/SigningSession.cs`
- `src/PdfSigning.Web/Models/Documents/DocumentStatus.cs`

### Document services
- `src/PdfSigning.Web/Services/Documents/DocumentWorkflowService.cs`
- `src/PdfSigning.Web/Services/Documents/DocumentReadService.cs`
- `src/PdfSigning.Web/Services/Documents/DocumentFieldService.cs`
- `src/PdfSigning.Web/Services/Documents/DocumentStatusService.cs`
- `src/PdfSigning.Web/Services/Documents/DocumentSigningService.cs`
- `src/PdfSigning.Web/Services/Documents/DocumentFileStore.cs`
- interfaces and DTO model files in the same folder

### Owner-facing Razor Pages
- `src/PdfSigning.Web/Pages/Documents/Index.cshtml`
- `src/PdfSigning.Web/Pages/Documents/Index.cshtml.cs`
- `src/PdfSigning.Web/Pages/Documents/Details.cshtml`
- `src/PdfSigning.Web/Pages/Documents/Details.cshtml.cs`
- `src/PdfSigning.Web/Pages/Documents/View.cshtml`
- `src/PdfSigning.Web/Pages/Documents/View.cshtml.cs`
- `src/PdfSigning.Web/Pages/Documents/Pdf.cshtml`
- `src/PdfSigning.Web/Pages/Documents/Pdf.cshtml.cs`

### Recipient-facing Razor Pages
- `src/PdfSigning.Web/Pages/Sign/Index.cshtml`
- `src/PdfSigning.Web/Pages/Sign/Index.cshtml.cs`
- `src/PdfSigning.Web/Pages/Sign/Pdf.cshtml`
- `src/PdfSigning.Web/Pages/Sign/Pdf.cshtml.cs`

### Shared UI assets
- `src/PdfSigning.Web/Pages/Shared/_Layout.cshtml`
- `src/PdfSigning.Web/Pages/Shared/_Layout.cshtml.css`
- `src/PdfSigning.Web/Pages/Shared/_LoginPartial.cshtml`
- `src/PdfSigning.Web/wwwroot/css/site.css`
- `src/PdfSigning.Web/wwwroot/js/document-preview.js`

### Local infrastructure and docs
- `docker-compose.yml`
- `README.md`
- `docs/PDFSIGNING_PROJECT_MAP.md`

### Tests
- `tests/PdfSigning.Web.Tests/Pages/Documents/DetailsModelTests.cs`
- `tests/PdfSigning.Web.Tests/Services/Documents/DocumentFileStoreTests.cs`
- `tests/PdfSigning.Web.Tests/Services/Documents/DocumentSigningServiceTests.cs`
- `tests/PdfSigning.Web.Tests/Services/Documents/DocumentFieldServiceTests.cs`
- `tests/PdfSigning.Web.Tests/Services/Documents/DocumentStatusServiceTests.cs`
- `tests/PdfSigning.Web.Tests/Services/Documents/DocumentReadServiceTests.cs`
- `tests/PdfSigning.Web.Tests/Services/Documents/DocumentWorkflowServiceTests.cs`
- `tests/PdfSigning.Web.Tests/UnitTest1.cs` likely legacy / placeholder and should be reviewed

---

## 8. Data Model and Storage Rules

### Documents table / model intent
Store:
- title
- original filename
- content type
- private storage key
- owner user id
- lifecycle status
- created/completed timestamps

### Signature fields
Store:
- label
- page number
- X/Y/Width/Height
- required flag
- creation timestamp
- optional association to a signing session if needed by workflow

### Signing sessions
Store:
- document id
- recipient email
- hashed access token only
- created timestamp
- expiry timestamp
- revoked timestamp if revoked
- completion timestamp if completed
- signer name if captured

### File storage rules
- source PDFs are stored privately outside webroot
- signed output PDFs are stored privately outside webroot
- storage keys are app-internal identifiers, not public URLs
- file access must happen through app endpoints with authorization checks
- temporary files must be cleaned up

---

## 9. Technology Guidance for Demo Readiness

These are the recommended choices to keep the demo realistic but achievable.

### Keep
- ASP.NET Core Razor Pages
- ASP.NET Identity
- EF Core + SQL Server
- Docker SQL Server for local verification
- PdfSharpCore for current PDF handling path
- shared stylesheet strategy

### Avoid for now
- changing to a SPA framework
- introducing OAuth/SSO
- adding background infrastructure that is not required for the demo
- production cloud migrations unless the user explicitly wants hosted demo deployment
- advanced cryptographic signature systems unless already mostly implemented

### Recommended additions if missing
- structured audit/event logging for sensitive actions
- stronger upload validation rules
- explicit signed artifact storage strategy
- explicit status/state machine rules
- end-to-end demo checklist document if not embedded here

---

## 10. Functional Requirements by Workflow Stage

### Stage A: Owner authentication
Must support:
- owner sign-in
- restricted access to `/Documents`
- owner-only access to owned documents

### Stage B: Upload and document creation
Must support:
- PDF upload
- title capture or reasonable default title
- content-type and extension validation
- private file persistence
- creation of a document record

### Stage C: Field placement
Must support:
- adding signature fields
- deleting signature fields
- required flag handling
- stable persistence of coordinates
- reloading the page without losing field state

### Stage D: Signing session creation
Must support:
- recipient email entry
- secure token generation
- token hash persistence only
- expiry timestamp creation
- link generation
- optional revocation of previous active session if that is current business rule

### Stage E: Recipient verification
Must support:
- link parsing
- token/session lookup
- recipient email verification
- denial states for wrong email / expired / revoked / malformed / missing session

### Stage F: Signing experience
Must support:
- private document rendering for validated recipient
- completion flow
- capture of signer name / typed signature data required for demo
- prevention of invalid completion when session is no longer valid

### Stage G: Completion and owner visibility
Must support:
- updating session state
- updating document state
- showing completion to owner
- ideally generating a final signed PDF artifact or clearly recording signing completion

---

## 11. Non-Functional Requirements

### Security
- no secret leakage
- private file storage only
- token hashing only
- anti-forgery coverage on mutating routes
- server-side authorization on every sensitive path
- graceful denial behavior

### Reliability
- page flows survive refreshes
- migrations apply cleanly
- demo path should not depend on flaky external services unless explicitly chosen

### Usability
- owner flow should be obvious without explanation
- recipient flow should be simple and non-technical
- error copy should be clear without disclosing internals

### Maintainability
- shared styles, low duplication
- explicit services/interfaces
- tests accompany meaningful changes
- small commits

---

## 12. Test and Verification Policy

### Mandatory for every meaningful code change
1. Run targeted tests for affected area when available.
2. Run full web test project.
3. Review diff for obvious secret leakage.
4. Verify schema/migration impact if DB model changed.
5. Record what was manually verified if UI changed.

### Standard test command
```bash
/home/evanm/.dotnet/dotnet test tests/PdfSigning.Web.Tests/PdfSigning.Web.Tests.csproj -v minimal
```

### Manual verification expectations
For UI/security-sensitive slices, verify at least:
- owner can access only own document routes
- recipient cannot access without valid session
- revoked/expired/wrong-email flows deny access cleanly
- uploads remain private
- no raw secrets or tokens are rendered unexpectedly

### Pre-demo verification checklist
- latest tests green
- demo seed data prepared
- sample PDF present
- link creation works
- recipient happy path rehearsed
- at least one failure path rehearsed
- no sensitive secrets in git diff, docs, screenshots, or console output

---

## 13. Definition of Done

A phase is only done when all of the following are true:
- scoped tasks are complete
- targeted tests pass
- full test suite passes
- manual verification notes exist if UI/security flow changed
- no obvious secret leakage in diff
- documentation updated if behavior/setup/security changed
- changes are committed locally

### Push policy
Default recommendation:
- commit each meaningful slice locally
- push when a coherent feature milestone is complete or when the user asks

---

## 14. Orchestrator Working Rules

1. Read this file before planning any work.
2. If a requirement is unclear, prefer the defaults in this file and explicitly note the assumption.
3. Break work into small achievable phases and sub-steps.
4. Prefer TDD or at minimum tests-first for risky/security-sensitive code.
5. Use one implementer subagent per focused task if running in a subagent workflow.
6. Run spec review before code-quality review.
7. Do not mix large unrelated changes into one slice.
8. Do not introduce new infrastructure unless it directly helps the demo.
9. Stop and surface decisions that materially change security, hosting, auth, or data handling.
10. Never expose the user’s passwords, keys, secrets, or real recipient data.

---

## 15. Exhaustive Delivery Phases

Each phase below should be treated as a small milestone. Within each phase, tasks should be further broken into 2-5 minute implementable steps.

### Phase 0: Scope freeze and decision log
Goal:
- lock demo assumptions and prevent scope creep

Deliverables:
- approved assumptions list
- in-scope / out-of-scope list
- golden demo path
- open questions log

Exit criteria:
- user approves the demo shape or accepts defaults

### Phase 1: Secret hygiene and environment safety
Goal:
- make sure development and demo setup cannot leak secrets accidentally

Tasks:
- verify `.env.local` usage pattern
- verify User Secrets usage pattern
- confirm no real secrets in repo
- confirm docs use placeholders only
- add or improve secret-handling notes if needed

Exit criteria:
- clear documented secret-storage rules
- no obvious sensitive values in tracked files

### Phase 2: Private storage baseline
Goal:
- ensure PDFs are always private and retrievable only via app logic

Tasks:
- verify storage path is outside webroot
- verify file-serving routes enforce authorization
- verify file store path handling is safe
- verify missing-file behavior is safe and user-friendly

Exit criteria:
- uploaded files are not directly publicly reachable

### Phase 3: Owner document flow stabilization
Goal:
- make upload, listing, details, and view flows reliable for the owner

Tasks:
- validate upload restrictions
- verify document metadata correctness
- verify details page stability
- verify owner-only access rules
- polish UI where confusing

Exit criteria:
- owner can upload and manage sample document reliably

### Phase 4: Signature field workflow stabilization
Goal:
- make field placement predictable and demo-safe

Tasks:
- verify add field flow
- verify delete field flow
- verify persistence across refresh
- validate coordinate/page constraints where missing
- ensure clear UI feedback

Exit criteria:
- field placement works repeatedly without page corruption or stale state confusion

### Phase 5: Secure session creation hardening
Goal:
- make signing session creation secure, consistent, and explainable

Tasks:
- verify cryptographic token generation
- verify hash-only persistence
- verify expiry handling
- verify revoke behavior
- verify one-active-session rule if intended
- verify no token leakage in logs or UI

Exit criteria:
- secure link creation is stable and follows invariants

### Phase 6: Recipient verification gate hardening
Goal:
- enforce that recipients cannot view documents without valid verification

Tasks:
- verify token/session lookup logic
- verify email match requirement
- verify expired/revoked/completed denial states
- improve denial messaging without leaking internals
- verify session misuse/replay is blocked appropriately

Exit criteria:
- invalid recipient access paths consistently fail closed

### Phase 7: Signing completion and status transitions
Goal:
- ensure the signing event updates state correctly and safely

Tasks:
- verify required-field completion rules
- verify signed-by metadata capture
- verify document status transition rules
- formalize state machine behavior if still implicit
- verify completed sessions cannot be reused incorrectly

Exit criteria:
- successful signing yields a trustworthy completion state

### Phase 8: Final signed artifact strategy
Goal:
- make the demo end with a concrete result the client can understand

Required outcome:
- generate and store a final signed PDF artifact privately

Tasks:
- verify artifact generation path or define minimal viable output
- verify artifact storage and retrieval authorization
- verify final output naming/storage conventions

Exit criteria:
- demo has a clear end-state deliverable

### Phase 9: Owner visibility and audit trail polish
Goal:
- make the owner side demonstrate confidence and traceability

Tasks:
- surface session status
- surface expiry/revoke/completion timestamps
- show clear document lifecycle state
- add minimal audit event visibility if appropriate

Exit criteria:
- owner can easily explain what happened and when

### Phase 10: Demo UX polish
Goal:
- reduce confusion and improve perceived quality

Tasks:
- tighten labels and action copy
- reduce redundant or ugly styling
- improve loading/error/success states
- confirm consistent shared styling

Exit criteria:
- happy path can be run smoothly with minimal narration

### Phase 11: Negative-path demo safety
Goal:
- avoid embarrassing failures during the live demo

Tasks:
- rehearse wrong-email case
- rehearse expired-link case
- rehearse revoked-link case
- verify invalid direct URL access is denied

Exit criteria:
- failure cases are controlled and understandable

### Phase 12: Deployment/demo packaging
Goal:
- package the app so it can be shown reliably in the intended environment

Default assumption:
- internet-accessible deployment hosted from the user's own computer, with local rehearsal still supported on the same machine

Tasks:
- confirm runtime startup steps
- confirm database startup steps
- confirm seed/sample data steps
- confirm secret injection steps
- choose a public-access strategy appropriate for a home-hosted demo:
  - HTTPS-capable reverse proxy with a real domain or DDNS name
  - safe exposure mechanism such as controlled port forwarding or tunnel
  - documented router / firewall / host access rules
- document how TLS certificates are provisioned and renewed
- document how to keep the SQL Server instance non-public while only the web app is exposed
- document how to stop the public demo endpoint quickly if needed
- confirm cleanup/reset steps after demo rehearsal

Exit criteria:
- demo can be started repeatably from documented steps and accessed remotely over HTTPS

### Phase 13: Final rehearsal and release gate
Goal:
- prove the project is demo-ready

Tasks:
- run full tests
- rehearse full happy path end to end
- review for secrets one more time
- confirm demo data only
- note known limitations and talking points

Exit criteria:
- user is comfortable showing the app live

---

## 16. Phase Breakdown Style for Future Agents

When converting any phase into actionable work, break it down like this:
- inspect relevant files
- write or update failing tests first where practical
- implement the smallest safe change
- run targeted tests
- run full tests
- manually verify affected UI/security path
- review diff for secret leakage
- commit with a small descriptive message

Do not write giant tasks like:
- "finish security"
- "build signing flow"
- "make demo ready"

Instead write atomic tasks like:
- "Add denial test for revoked signing session"
- "Verify document PDF route checks owner ownership before streaming"
- "Add expiry timestamp display to owner details page"
- "Reject non-PDF upload by extension and content type"

---

## 17. Open Decisions to Confirm with User

These defaults are recommended, but the user may override them:
- session lifetime length
- whether to plan for multi-signer workflows now or later
- which home-hosting strategy to use for remote access: direct reverse proxy + port forwarding, DDNS, or a tunnel-based approach

---

## 18. Security Checklist Before Any Push or Demo

- no real passwords in repo
- no real API keys in repo
- no raw signing tokens in repo or docs
- no user secrets file tracked
- no connection string with real credentials tracked
- no logs/screenshots shared with sensitive values
- all demo accounts and recipient data are fake
- private PDFs remain outside webroot
- recipient routes fail closed on invalid access
- latest tests are green

---

## 19. Final Note for Orchestrators

If the user does not specify a product decision, do not stall unnecessarily.
Use the recommended defaults in this file, label them as assumptions, and keep moving in small, secure, test-verified phases.
