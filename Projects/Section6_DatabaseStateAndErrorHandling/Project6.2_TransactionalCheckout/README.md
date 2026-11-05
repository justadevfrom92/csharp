# Project 6.2 — Transactional Checkout, Seed Data & Soft Deletes

Console app using EF Core + SQLite (`shop.db`, recreated each run).

```bash
dotnet run
```

## What it covers

- **Seed data** with `HasData` (3 products, 1 customer), created by `EnsureCreated()`.
- **Explicit transaction** (`BeginTransaction` → `Commit` / `Rollback`) around a 3-step checkout:
  create order → take money → reduce stock. Each step calls `SaveChanges`.
  The demo's second order fails at step 3 *after* the money was taken — and the rollback undoes
  everything: no order, wallet unchanged.
- **Soft delete:** `Remove(product)` is turned into `IsDeleted = true` in `SaveChanges`.
- **Global query filter:** `HasQueryFilter(product => !product.IsDeleted)` hides deleted products
  from every query. `IgnoreQueryFilters()` shows them again (for admins, audits).

## Why a transaction

Each `SaveChanges` is saved straight away. Without the transaction, a crash after step 2 would
leave an order with money taken but stock not reduced. The transaction makes the 3 steps
"all or nothing": either every change is kept (`Commit`) or none (`Rollback`).
(If everything fits in *one* `SaveChanges`, EF already wraps it in a transaction for you. You only
need `BeginTransaction` when there are several `SaveChanges` calls that must succeed together.)

## Why soft delete instead of really deleting

1. **Old orders still make sense.** Order lines point at products. If we really deleted
   "Old Webcam", old orders would either break (foreign key error) or lose the product.
2. **Mistakes can be undone.** Set `IsDeleted = false` and it's back. A real `DELETE` is gone forever
   (unless you restore a backup).
3. **History / audit.** `DeletedAtUtc` records when it happened. Reports and legal audits often
   need to know what existed in the past.
4. **No code can forget it.** The global query filter is applied to *every* query automatically,
   so nobody can accidentally show a deleted product by forgetting `Where(!IsDeleted)`.

**Downsides (and when to really delete):** the table keeps growing, unique indexes must account
for deleted rows, and some laws (e.g. GDPR "right to be forgotten") require personal data to be
truly removed. So: soft delete business records like products and orders; hard delete (or
anonymise) personal data when the law says so.

**Why `UnitPrice` is copied onto the order line:** if the product price changes next week,
last week's orders must still show what the customer actually paid.

**Why `EnsureCreated` here and migrations in 6.1:** this demo throws the database away every run,
so migrations add nothing. Real apps that keep data should use migrations (6.1, 7.7).
