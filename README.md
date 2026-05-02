# PdfSigningApp

PdfSigningApp is a .NET 8 Razor Pages application for secure, browser-based PDF signing. It is designed around a simple owner-to-recipient workflow: an authenticated owner uploads a PDF, places signature fields, creates a time-limited signing link for a recipient, and receives a private signed PDF artifact when signing is completed.

## Current capabilities

- ASP.NET Core Razor Pages application on .NET 8
- ASP.NET Identity for owner authentication
- PDF upload with private filesystem storage outside `wwwroot`
- Signature field placement metadata stored in SQL Server
- Secure recipient signing links with hashed access tokens
- Recipient email confirmation gate before document access
- 24-hour signing session expiry
- Single-recipient, single-use signing flow
- Typed signature capture
- Final signed PDF artifact generation on signing completion
- Owner-only retrieval of both the original PDF and signed PDF artifact

## Security model

This project is intentionally built around private storage and controlled document access.

- PDF files are stored under `App_Data`, not in public static paths
- Secure signing links use token hashes in the database rather than storing raw tokens
- Recipient access is gated by email confirmation before the signing PDF is served
- Signed PDF artifacts are stored separately from the original uploaded PDF
- The original upload remains immutable after signing
- Application secrets are expected to live in local environment variables or .NET User Secrets, not in git

## Architecture overview

Core stack:

- .NET 8
- ASP.NET Core Razor Pages
- ASP.NET Identity
- Entity Framework Core
- SQL Server 2022
- Docker Compose for local SQL Server
- PdfSharpCore for signed PDF artifact generation

High-level flow:

1. Owner signs in
2. Owner uploads a PDF
3. Owner places signature fields
4. Owner creates a signing session for a recipient email
5. App creates a time-limited access token and stores only its hash
6. Recipient opens the signing link and confirms their email
7. Recipient completes signing with a typed name
8. App generates a separate signed PDF artifact and stores it privately
9. Owner downloads the signed PDF artifact from an authenticated route

## Demo walkthrough

A simple demo run should look like this:

1. Owner signs in
2. Owner uploads a sample PDF
3. Owner places one or more required signature fields
4. Owner creates a signing link for a sample recipient email
5. Owner copies the generated link and shares it manually for the demo
6. Recipient opens the link and confirms the expected email address
7. Recipient opens the protected PDF and completes signing with a typed name
8. App marks the session complete, updates the document status, and generates a signed PDF artifact
9. Owner returns to the document details page and downloads the final signed PDF artifact

## Project structure

- `src/PdfSigning.Web/` — main web application
- `src/PdfSigning.Web/Pages/` — Razor Pages for owner and recipient flows
- `src/PdfSigning.Web/Services/Documents/` — document storage, read, workflow, signing, and artifact services
- `src/PdfSigning.Web/Data/` — EF Core DbContext and migrations
- `tests/PdfSigning.Web.Tests/` — unit and page-model tests
- `docs/PDFSIGNING_PROJECT_MAP.md` — master demo-delivery and implementation plan
- `docs/DEPLOYMENT_READINESS_CHECKLIST.md` — deployment and secret-hygiene checklist
- `docs/plans/` — targeted implementation plans for discrete work items
- `docker-compose.yml` — local SQL Server definition

## Local development setup

### 1. Start SQL Server

This repo includes a Docker Compose definition for SQL Server 2022.

1. Copy the environment template:
   - `cp .env.example .env.local`
2. Edit `.env.local` and choose a strong `MSSQL_SA_PASSWORD`
3. Start SQL Server:
   - `sg docker -c 'docker compose --env-file .env.local up -d'`

If your shell already has Docker access, you can also use:
- `docker compose --env-file .env.local up -d`

### 2. Configure the app connection string

Keep the application connection string out of the repository.

Recommended approach: .NET User Secrets

- `dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=PdfSigningApp;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=true" --project src/PdfSigning.Web/PdfSigning.Web.csproj`

Alternative: environment variable

- `ConnectionStrings__DefaultConnection=Server=localhost,1433;Database=PdfSigningApp;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=true`

### 3. Apply database migrations

- `dotnet ef database update --project src/PdfSigning.Web/PdfSigning.Web.csproj --startup-project src/PdfSigning.Web/PdfSigning.Web.csproj`

### 4. Run the app

- `dotnet run --project src/PdfSigning.Web/PdfSigning.Web.csproj`

## Testing

Run the full test suite with:

- `dotnet test tests/PdfSigning.Web.Tests/PdfSigning.Web.Tests.csproj -v minimal`

The current implementation includes tests covering:

- document file storage behavior
- document read projections
- signing session creation and completion
- signed PDF artifact generation
- owner-only signed PDF retrieval page behavior

## Signed PDF artifact behavior

When a recipient completes signing:

- the signing session is marked complete
- the signer name is recorded
- the document status is updated to signed
- a new signed PDF artifact is generated
- the artifact is stored privately under the application data root
- the artifact storage key is saved in SQL Server
- the owner can download the signed artifact from an authenticated route

This means the database stores document and workflow metadata, while the PDF binaries themselves remain on private disk storage.

## Sensitive information rules

Do not commit any of the following:

- `.env.local`
- real SQL passwords
- connection strings with real credentials
- .NET User Secrets content
- copied signing links from real sessions
- private keys, API keys, or tokens

Before pushing changes, verify that configuration and secrets remain local-only.

## Roadmap notes

The current demo-oriented implementation is optimized for:

- a single document owner
- a single recipient signer per document
- 24-hour signing sessions
- typed signatures
- controlled sharing of signing links

Potential next steps include richer audit history, clearer signed-artifact metadata in the owner UI, deployment hardening, and more robust handling of unsupported or encrypted PDFs.
