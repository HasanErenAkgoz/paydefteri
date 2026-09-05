# Security Policy

## Reporting a vulnerability

Please report suspected vulnerabilities privately rather than opening a public GitHub issue. Email the maintainer at the address in the repository's commit history, or open a private security advisory via GitHub ("Security" tab → "Report a vulnerability") on this repository.

Include, where possible:
- A description of the issue and its potential impact.
- Steps to reproduce (a minimal request/payload is ideal).
- The affected endpoint, file, or component.

We aim to acknowledge reports within a few days and to fix confirmed issues before public disclosure.

## Supported versions

This is a single-deployment application (see `docker-compose.prod.yml`), not a versioned library — only the code currently deployed to production is supported. There are no LTS/maintenance branches.

## Scope

In scope: the ASP.NET Core API (`src/api/`), the Angular web/mobile client (`src/web/`), and deployment configuration in this repository (`docker-compose*.yml`, `src/web/deploy/`).

Out of scope: third-party services this project depends on (PostgreSQL, Gemini/OpenAI APIs, hosting provider) — report those to their respective vendors.

## Known internal tracking

Open security findings from internal audits are tracked in `docs/` (not this file) and resolved before each production deploy; they are not republished here to avoid giving unresolved issues wider visibility than necessary.
