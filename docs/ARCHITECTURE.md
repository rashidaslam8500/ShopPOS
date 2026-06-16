# Bhai Gee Crockery Store — System Architecture

## Overview

Production POS for **Bhai Gee Crockery Store** (Karkhana Bazar, Sargodha).  
Layered C# / WPF application with **Microsoft SQL Server** and full audit trail.

## Layer Diagram

```
┌──────────────────────────────────────────────────────────┐
│  ShopPOS.WPF                                             │
│  LoginWindow · MainWindow · Views · ViewModels (MVVM)    │
├──────────────────────────────────────────────────────────┤
│  ShopPOS.Business                                        │
│  Auth · Audit · Sale · Product · Settings · Barcode ·    │
│  ReceiptPrint · Dashboard                                │
├──────────────────────────────────────────────────────────┤
│  ShopPOS.Data                                            │
│  EF Core SQL Server · Repositories · PasswordHasher ·    │
│  DatabaseSeeder                                          │
├──────────────────────────────────────────────────────────┤
│  ShopPOS.Domain                                          │
│  Entities · Enums · Interfaces · DTOs                    │
└──────────────────────────────────────────────────────────┘
                          ↓
              BhaiGeePOS (SQL Server)
         Managed via SSMS / connection string
```

## Database — SQL Server

Connection string in `src/ShopPOS.WPF/appsettings.json`:

```json
"DefaultConnection": "Server=localhost;Database=BhaiGeePOS;Trusted_Connection=True;TrustServerCertificate=True;"
```

Manual setup: run `database/schema.sql` in SSMS.  
Automatic setup: EF Core `EnsureCreated` on first app launch.

## Phase 1 — Database Schema

| Table | Purpose |
|-------|---------|
| `Users` | Owner / Salesman accounts (PBKDF2 password hash) |
| `Products` | Inventory with barcode, SKU, current price |
| `Sales` | Invoice header with cashier, status, returned amount |
| `SaleItems` | **UnitPriceAtSale** frozen at billing time |
| `SaleReturns` | Return lines referencing original invoice rate |
| `AuditLogs` | Immutable log: timestamp, user, action, details |
| `ShopSettings` | Shop name, address, phone, tax, currency |

**Data integrity rule:** Returns never read `Products.Price`. They always use `SaleItems.UnitPriceAtSale`.

## Phase 2 — Authentication & RBAC

| Role | Access |
|------|--------|
| **Salesman** | Billing, Returns |
| **Owner** | + Inventory, Sales Report, Dashboard, Audit Logs, Settings |

## Phase 3 — UI

- Branded login (Bhai Gee Crockery Store)
- Owner: full sidebar | Salesman: Billing + Returns only
- Currency: PKR (`Rs.`)

## Phase 4 — Billing Engine

Price snapshot on sale; returns use original `UnitPriceAtSale`.

## Phase 5 — Hardware

Barcode scan/generate (ZXing), thermal receipt print (Windows PrintDocument).

## Default Credentials

- Owner: `owner` / `owner123`
- Salesman: `sales` / `sales123`
