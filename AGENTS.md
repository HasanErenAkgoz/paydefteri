# Repository Guidelines

## Project Structure & Module Organization

PayDefteri is an Angular 19 frontend and ASP.NET Core 8 API for shared installment and expense tracking. Keep business rules in `src/api/FuzulTaksitTakip.Domain`, application commands/queries in `Application`, EF Core and external services in `Infrastructure`, and HTTP controllers/composition in `Api`. The Angular app is in `src/web/src/app`; place feature UI under `features/`, reusable UI under `shared/`, and API models/services under `core/`. Tests mirror the backend split in `tests/FuzulTaksitTakip.Domain.Tests` and `tests/FuzulTaksitTakip.Api.Tests`. Static web assets belong in `src/web/public`.

## Build, Test, and Development Commands

- `docker compose up -d` starts local PostgreSQL (`localhost:5432`).
- `dotnet run --project src/api/FuzulTaksitTakip.Api` starts the API on `http://localhost:5096` (Swagger: `/swagger`).
- `npm start --prefix src/web` starts Angular on `http://localhost:4200`.
- `dotnet build FuzulTaksitTakip.sln` builds all .NET projects.
- `dotnet test` runs the .NET test suite; target an individual project when iterating.
- `npm run build` from `src/web` creates the production frontend build.
- Add migrations with `dotnet ef migrations add Name -p src/api/FuzulTaksitTakip.Infrastructure -s src/api/FuzulTaksitTakip.Api -o Persistence/Migrations`.

## Coding Style & Naming Conventions

Use nullable-aware C# and conventional .NET naming: PascalCase types/methods, camelCase locals, and one command/query per focused file or feature. Preserve the Clean Architecture dependency direction; controllers should delegate to MediatR rather than contain business rules. Angular uses 2-space indentation, single quotes in TypeScript, PascalCase components, and kebab-case file names such as `expenses.component.ts`. Keep user-facing language consistent with the PayDefteri product name, even where legacy namespaces retain `FuzulTaksitTakip`.

## Testing Guidelines

Backend tests use xUnit and FluentAssertions. Name tests by behavior, for example `Positive_member_can_manage_own_expense_but_not_the_owner_expense`; cover both successful and rejected paths. Add API coverage for endpoint-visible behavior and domain coverage for calculation rules. Angular tests use Jasmine/Karma (`npm test --prefix src/web`) when adding client logic.

## Commit & Pull Request Guidelines

Use short imperative commit subjects, as in `Improve mobile layout for nav, setup, and main screens.` Keep commits focused. PRs should explain user-visible behavior, list validation commands, link relevant issues, include screenshots for UI changes, and call out migrations, environment variables, or deployment steps. Never commit secrets; use local configuration or deployment environment variables.
