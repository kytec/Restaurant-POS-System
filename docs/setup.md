# Restaurant POS Setup

## Prerequisites

- .NET SDK 10.x
- SQL Server Express or SQL Server Developer

## First Run

1. Restore packages:
   `dotnet restore RestaurantPOS.sln`

2. Apply migrations:
   `dotnet ef database update --project src/RestaurantPOS.Web --startup-project src/RestaurantPOS.Web`

3. Run the app:
   `dotnet run --project src/RestaurantPOS.Web`

4. Sign in with the seeded admin account:
   - Email: `admin@restaurant.local`
   - Password: `ChangeMe123!`

Change the seeded password before using the app outside local development.

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
