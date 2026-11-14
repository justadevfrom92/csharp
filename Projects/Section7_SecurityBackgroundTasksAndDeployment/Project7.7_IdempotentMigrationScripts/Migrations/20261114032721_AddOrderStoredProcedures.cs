using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdempotentMigrationScripts.Migrations
{
    /// <summary>
    /// Adds three STORED PROCEDURES (named pieces of SQL saved inside the database).
    /// EF has no "CreateProcedure" method, so the SQL is written by hand with migrationBuilder.Sql.
    /// CREATE OR ALTER: running it again replaces the procedure instead of failing - safe to re-run.
    ///
    /// WHY EXEC(N'...'): SQL Server only accepts CREATE PROCEDURE as the FIRST statement of a batch.
    /// The idempotent script (migrations.sql) puts every step inside "IF NOT EXISTS (...) BEGIN ... END",
    /// so a plain CREATE PROCEDURE there is a syntax error. EXEC runs the text as its own little batch.
    /// Inside the N'...' text, every single quote must be doubled ('' instead of ').
    /// </summary>
    public partial class AddOrderStoredProcedures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. RETURNS ROWS: every order of one customer, newest first.
            //    It returns ALL the Orders columns, so EF can turn each row into an OrderModel.
            migrationBuilder.Sql(@"EXEC(N'
CREATE OR ALTER PROCEDURE GetOrdersForCustomer
    @CustomerId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, CustomerId, Total, CreatedAtUtc
    FROM Orders
    WHERE CustomerId = @CustomerId
    ORDER BY CreatedAtUtc DESC;
END');");

            // 2. CHANGES DATA, RETURNS NO ROWS: lower every order total of one customer by a percentage.
            //    No SET NOCOUNT ON here, so SQL Server reports how many rows changed (EF returns that number).
            migrationBuilder.Sql(@"EXEC(N'
CREATE OR ALTER PROCEDURE ApplyCustomerDiscount
    @CustomerId INT,
    @Percent INT
AS
BEGIN
    IF @Percent < 1 OR @Percent > 50
        THROW 50001, ''Discount must be 1 to 50 percent.'', 1;

    UPDATE Orders
    SET Total = ROUND(Total * (100 - @Percent) / 100.0, 2)
    WHERE CustomerId = @CustomerId;
END');");

            // 3. OUTPUT PARAMETER: hands one value back through a parameter instead of a result row.
            migrationBuilder.Sql(@"EXEC(N'
CREATE OR ALTER PROCEDURE GetCustomerOrderTotal
    @CustomerId INT,
    @Total DECIMAL(18, 2) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT @Total = COALESCE(SUM(Total), 0)
    FROM Orders
    WHERE CustomerId = @CustomerId;
END');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS GetCustomerOrderTotal;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS ApplyCustomerDiscount;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS GetOrdersForCustomer;");
        }
    }
}
