using EfCoreModelling.Models;
using Microsoft.EntityFrameworkCore;

namespace EfCoreModelling.Seeders;

// The same demo data for the app, the tests and every variation DbContext (they all map the same classes).
// Acme bought 25 kg cement, 40 m cable and 3 h installation; Globex bought 10 kg cement.
public static class ShopSeeder
{
    public static (Guid AcmeInvoiceId, Guid CementId) Seed(DbContext database)
    {
        var cement = new MaterialModel("Cement", "kg");
        var cable = new MaterialModel("Cable", "m");
        var install = new ServiceModel("Installation", 85m);
        var acme = new InvoiceModel("Acme");
        acme.AddLine(cement, 25m);
        acme.AddLine(cable, 40m);
        acme.AddLine(install, 3m);
        var globex = new InvoiceModel("Globex");
        globex.AddLine(cement, 10m);
        database.AddRange(cement, cable, install, acme, globex);
        database.SaveChanges();
        return (acme.PublicId, cement.PublicId);
    }
}
