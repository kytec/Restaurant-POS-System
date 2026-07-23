# Restaurant POS Project Map

## Purpose

This repository contains a simple single-restaurant POS built with .NET 10, Blazor, ASP.NET Core Identity, EF Core, and SQL Server LocalDB for local development.

## Structure

- `src/RestaurantPOS.Api` is the backend HTTP API. New frontend screens should call this project instead of accessing data directly.
- `src/RestaurantPOS.Client` is the standalone Blazor WebAssembly frontend.
- `src/RestaurantPOS.Contracts` contains API routes and DTOs shared by the API and the WebAssembly client.
- `src/RestaurantPOS.Web` is the original Blazor Server application. During migration, it still owns most domain, data, identity, and feature service code.
- `src/RestaurantPOS.Web/Components/Pages` contains staff-facing screens grouped by feature.
- `src/RestaurantPOS.Web/Features` contains small service classes for workflows such as orders, payments, inventory, and reports.
- `src/RestaurantPOS.Web/Domain` contains simple entities and enums.
- `src/RestaurantPOS.Web/Data` contains EF Core context, configuration, migrations, and seed data.
- `src/RestaurantPOS.Web/Security` contains shared role names and authorization constants.
- `tests/RestaurantPOS.Tests` contains unit and integration-style tests.

## Commands

- Restore: `dotnet restore RestaurantPOS.sln`
- Build: `dotnet build RestaurantPOS.sln --no-restore`
- Test: `dotnet test RestaurantPOS.sln --no-build`
- Run backend API: `dotnet run --project src/RestaurantPOS.Api --launch-profile http`
- Run WebAssembly client: `dotnet run --project src/RestaurantPOS.Client --launch-profile http`
- Run original Blazor Server app: `dotnet run --project src/RestaurantPOS.Web`
- Add migration: `dotnet ef migrations add <Name> --project src/RestaurantPOS.Web --startup-project src/RestaurantPOS.Web`

## Naming Rules

- Pages use clear module names: `Orders`, `Kitchen`, `Payments`, `Inventory`.
- Service classes are named after workflows: `OrderService`, `PaymentService`, `InventoryService`.
- Domain entities stay simple and avoid UI-specific properties.
- Role strings come from `AppRoles`; do not duplicate role names in pages.

## Adding a Feature

1. Add or update the domain entity only if the database needs new state.
2. Add workflow logic to the matching feature service.
3. Add a page or component under `Components/Pages/<Feature>`.
4. Gate the page with the correct role constant from `AppRoles`.
5. Add tests for calculations, state transitions, or validation.
6. Add an EF migration if the database model changed.

## V1 Scope Guardrails

- Single restaurant only.
- Manual payment recording only.
- Prefer moving new UI work to `RestaurantPOS.Client` and exposing needed data through `RestaurantPOS.Api`.
- No multi-branch, subscription, or offline mode in v1.
