using Microsoft.EntityFrameworkCore;
using TransactionalCheckout.DbContexts;

namespace TransactionalCheckout.Variations;

// Deleting "Old Webcam" three ways. Returns (still visible to normal queries?, row still in the table?).
public static class DeleteStyleVariation
{
    // ORIGINAL: Remove + SaveChanges. Our SaveChanges override turns it into IsDeleted = true (soft delete).
    public static (bool Visible, bool RowExists) WithRemove()
    {
        CheckoutTransactionVariation.ResetDatabase();
        using (var database = new ShopDbContext())
        {
            database.Products.Remove(database.Products.Single(product => product.Name == "Old Webcam"));
            database.SaveChanges();
        }
        return Check();
    }

    // ExecuteDelete: ONE "DELETE FROM" statement, straight to the database. It does NOT go through SaveChanges,
    // so the soft-delete override never runs - the row is REALLY gone. Easy to do by accident.
    public static (bool Visible, bool RowExists) WithExecuteDelete()
    {
        CheckoutTransactionVariation.ResetDatabase();
        using (var database = new ShopDbContext())
        {
            database.Products.Where(product => product.Name == "Old Webcam").ExecuteDelete();
        }
        return Check();
    }

    // ExecuteUpdate setting the flag yourself: a fast bulk SOFT delete (no loading), same end result as Remove.
    public static (bool Visible, bool RowExists) WithExecuteUpdate()
    {
        CheckoutTransactionVariation.ResetDatabase();
        using (var database = new ShopDbContext())
        {
            database.Products.Where(product => product.Name == "Old Webcam").ExecuteUpdate(setters => setters
                .SetProperty(product => product.IsDeleted, true)
                .SetProperty(product => product.DeletedAtUtc, DateTime.UtcNow));
        }
        return Check();
    }

    private static (bool Visible, bool RowExists) Check()
    {
        using var database = new ShopDbContext();
        bool visible = database.Products.Any(product => product.Name == "Old Webcam");
        bool rowExists = database.Products.IgnoreQueryFilters().Any(product => product.Name == "Old Webcam");
        return (visible, rowExists);
    }
}
