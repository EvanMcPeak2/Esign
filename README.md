# PdfSigningApp

Browser-based PDF signing app built with .NET 8 and Razor Pages.

## Local SQL Server setup

This repo includes a Docker Compose definition for SQL Server 2022.

1. Copy the env template:
   - `cp .env.example .env.local`
2. Edit `.env.local` and choose your own strong `MSSQL_SA_PASSWORD`
3. Start SQL Server:
   - `sg docker -c 'docker compose --env-file .env.local up -d'`
4. Connect in SSMS:
   - Server: `localhost,1433`
   - Authentication: `SQL Server Authentication`
   - Login: `sa`
   - Password: the value from `.env.local`

## App connection string

For local development, the safest approach is to keep the app connection string out of the repo entirely and store it in .NET User Secrets.

Example:
- `dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=PdfSigningApp;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=true" --project src/PdfSigning.Web/PdfSigning.Web.csproj`

If you prefer environment variables, you can also set:
- `ConnectionStrings__DefaultConnection=...`

The Docker Compose SQL Server container still uses `.env.local` for `MSSQL_SA_PASSWORD`, and that file is ignored by git.

## Secure recipient signing links

This app supports a private, tokenized signing flow for external recipients:

- the owner creates a secure signing link from the document details page
- the recipient verifies access with their email before the document opens
- the signing session expires and can be revoked
- the raw access token is never stored in the database; only a hash is kept
- PDFs are served through private app routes instead of public static URLs

Important:
- do not commit real passwords, tokens, connection strings, or user secrets
- keep `.env.local` and .NET User Secrets local-only
- do not paste real signing links or recipient emails into docs or screenshots

## Planned v1

- authentication
- PDF upload
- signature field placement
- typed recipient signatures
- signed PDF output
