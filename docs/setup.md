# Restaurant POS Setup

## Prerequisites

- .NET SDK 10.x
- SQL Server Express or SQL Server Developer

## First Run

1. Restore packages:
   `dotnet restore RestaurantPOS.sln`

2. Apply migrations:
   `dotnet ef database update --project src/RestaurantPOS.Web --startup-project src/RestaurantPOS.Web`

3. Run the backend API:
   `dotnet run --project src/RestaurantPOS.Api --launch-profile https`

4. In another terminal, run the WebAssembly client:
   `dotnet run --project src/RestaurantPOS.Client --launch-profile https`

5. Open the client:
   `http://localhost:5007`

The backend API runs on `http://localhost:5181` by default for local development. The WebAssembly client reads that URL from `src/RestaurantPOS.Client/wwwroot/appsettings.json`.

## Original Blazor Server App

The existing Blazor Server app is still available during the migration:

1. Run the app:
   `dotnet run --project src/RestaurantPOS.Web`

2. Sign in with the seeded admin account:
   - Email: `admin@restaurant.local`
   - Password: `ChangeMe123!`

3. Create staff users from Management > Staff Users. The admin chooses the role, and the app emails the new user temporary credentials. On first sign-in, the user must choose a username and password, then sign in again with the new credentials.

Change the seeded admin password before using the app outside local development. You can override the development password with `SeedAdmin:Password`. Configure a real `IEmailSender` implementation before production so staff invitation emails are delivered.

The default development database connection is:

```text
Server=.;Database=RestaurantPOS;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=False
```

## Roles

- `Admin`: menu, staff, inventory, reports, and all operational screens.
- `Cashier`: orders, tables, and payments.
- `Waiter`: orders and tables.
- `Kitchen`: kitchen display.

## Verification

Run:

```powershell
dotnet restore RestaurantPOS.sln
dotnet build RestaurantPOS.sln --no-restore
dotnet test RestaurantPOS.sln --no-build
```
