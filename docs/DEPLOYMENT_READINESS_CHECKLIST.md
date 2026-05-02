# Deployment Readiness Checklist

This checklist is for preparing the PdfSigningApp demo for internet-accessible use without exposing development secrets or internal services.

## Public exposure rules

- Expose only the web application to the internet
- Do not expose SQL Server directly to the public internet
- Use HTTPS for any remote-accessible demo
- Prefer a tunnel or reverse proxy in front of the app rather than direct raw port exposure
- Keep sample PDFs and recipient identities fake and demo-only

## Secrets and credentials

- Keep the application connection string in .NET User Secrets or environment variables only
- Keep `MSSQL_SA_PASSWORD` in `.env.local` only
- Never commit `.env.local`
- Never commit real signing links, tokens, passwords, private keys, or provider credentials
- Rotate any secret immediately if it is accidentally shown in logs, screenshots, or chat

## Host hardening

- Run the app in Production mode for the actual demo
- Verify `UseExceptionHandler` and `UseHsts` are active in non-development environments
- Verify HTTPS redirection works from the external entry point
- Restrict SQL Server access to the local machine / Docker network only if possible
- Make sure only the intended web port is reachable from outside

## Application verification

- Confirm the database migrations apply cleanly before demo day
- Confirm owner login works with the intended demo account
- Confirm PDF upload works with the exact sample files you plan to show
- Confirm signature field placement works on the sample PDFs
- Confirm signing-link creation works and shows the expected 24-hour expiry
- Confirm recipient email confirmation still gates PDF access
- Confirm signed PDF artifact generation works end-to-end
- Confirm the owner can download the signed artifact after completion
- Confirm expired, revoked, and reused links fail safely with non-leaky messaging

## Logging and privacy

- Do not log raw access tokens
- Do not log full connection strings
- Avoid showing filesystem paths to end users
- Avoid showing detailed exception traces in the public demo
- Review browser autofill and clipboard habits before the demo so real credentials are not accidentally shown

## Network and hosting checks

- Verify the chosen public URL resolves correctly
- Verify HTTPS certificates are valid and current
- Verify the app works from a device outside the home network
- Verify home router / firewall rules expose only the intended web endpoint
- Have a quick shutdown path ready if something unexpected is exposed

## Demo rehearsal

- Rehearse the full owner-to-recipient flow with fresh fake data
- Rehearse a backup happy-path document in case the main sample PDF behaves unexpectedly
- Keep one pre-created owner account ready
- Keep one backup sample recipient identity ready
- Have a fallback explanation for manual link sharing vs full email delivery

## Current repo-specific notes

- SQL Server currently runs locally through Docker Compose for development
- The app expects `ConnectionStrings:DefaultConnection` from User Secrets or env vars
- Private PDF storage is under the app data area, outside `wwwroot`
- Signed PDF artifacts are stored separately from original uploads
- Signing sessions are currently designed for a 24-hour lifetime and a single recipient signer
