using System.Transactions;
using Microsoft.EntityFrameworkCore;
using TransactionalCheckout.DbContexts;
using TransactionalCheckout.Models;

namespace TransactionalCheckout.Variations;

// Alice buys 3 mice (37.50). She can pay, but only 2 are in stock - so the LAST step fails.
// Four ways to write the checkout; each starts from a fresh database and reports what was left behind.
public static class CheckoutTransactionVariation
{
    // ORIGINAL style: explicit BeginTransaction + several SaveChanges + Commit/Rollback. Everything undone.
    public static CheckoutOutcomeVariation WithExplicitTransaction()
    {
        ResetDatabase();
        string? error = null;
        using (var database = new ShopDbContext())
        using (var transaction = database.Database.BeginTransaction())
        {
            try
            {
                RunSteps(database, saveAfterEachStep: true);
                transaction.Commit();
            }
            catch (InvalidOperationException exception)
            {
                transaction.Rollback();
                error = exception.Message;
            }
        }
        return ReadOutcome(error);
    }

    // NO transaction: each SaveChanges is permanent at once. The order and the payment are saved, then the stock
    // step fails - Alice has paid for an order that can't be delivered.
    public static CheckoutOutcomeVariation WithoutTransaction()
    {
        ResetDatabase();
        string? error = null;
        using (var database = new ShopDbContext())
        {
            try
            {
                RunSteps(database, saveAfterEachStep: true);
            }
            catch (InvalidOperationException exception)
            {
                error = exception.Message;
            }
        }
        return ReadOutcome(error);
    }

    // ONE SaveChanges at the end: EF wraps a single SaveChanges in its own transaction automatically.
    // Simplest - when all the changes can be made in memory first. (Not possible if you need the new order's
    // database Id before the next step, which is why the original saves after each step.)
    public static CheckoutOutcomeVariation WithSingleSaveChanges()
    {
        ResetDatabase();
        string? error = null;
        using (var database = new ShopDbContext())
        {
            try
            {
                RunSteps(database, saveAfterEachStep: false);
                database.SaveChanges();
            }
            catch (InvalidOperationException exception)
            {
                error = exception.Message;
            }
        }
        return ReadOutcome(error);
    }

    // TransactionScope (System.Transactions): an "ambient" transaction - everything inside the using block joins
    // it automatically, even code that doesn't know about it. Without scope.Complete() it rolls back.
    // It works with SQL Server, but NOT with SQLite: EF Core refuses at the first query with
    // "An ambient transaction has been detected, but the current provider does not support ambient transactions"
    // (AmbientTransactionWarning, turned into an error). Nothing is saved - because nothing ran.
    public static CheckoutOutcomeVariation WithTransactionScope()
    {
        ResetDatabase();
        string? error = null;
        try
        {
            using var scope = new TransactionScope();
            using var database = new ShopDbContext();
            RunSteps(database, saveAfterEachStep: true);
            scope.Complete();
        }
        catch (InvalidOperationException exception)
        {
            error = exception.Message;
        }
        return ReadOutcome(error);
    }

    private static void RunSteps(ShopDbContext database, bool saveAfterEachStep)
    {
        CustomerModel alice = database.Customers.Single();
        ProductModel mouse = database.Products.Single(product => product.Id == 2);
        const int quantity = 3;
        decimal total = mouse.Price * quantity;

        var order = new OrderModel { CustomerId = alice.Id, CreatedAtUtc = DateTime.UtcNow, Total = total };
        order.Lines.Add(new OrderLineModel { ProductId = mouse.Id, Quantity = quantity, UnitPrice = mouse.Price });
        database.Orders.Add(order);
        Save(database, saveAfterEachStep);

        if (alice.WalletBalance < total)
        {
            throw new InvalidOperationException("Not enough money.");
        }
        alice.WalletBalance -= total;
        Save(database, saveAfterEachStep);

        if (mouse.Stock < quantity)
        {
            throw new InvalidOperationException($"Only {mouse.Stock} {mouse.Name} in stock.");
        }
        mouse.Stock -= quantity;
        Save(database, saveAfterEachStep);
    }

    private static void Save(ShopDbContext database, bool now)
    {
        if (now)
        {
            database.SaveChanges();
        }
    }

    public static void ResetDatabase()
    {
        using var database = new ShopDbContext();
        database.Database.EnsureDeleted();
        database.Database.EnsureCreated();
    }

    private static CheckoutOutcomeVariation ReadOutcome(string? error)
    {
        using var database = new ShopDbContext();
        return new CheckoutOutcomeVariation(
            database.Orders.Count(),
            database.Customers.Single().WalletBalance,
            database.Products.Single(product => product.Id == 2).Stock,
            error);
    }
}
