IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106065446_InitialCreate'
)
BEGIN
    CREATE TABLE [Customers] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_Customers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106065446_InitialCreate'
)
BEGIN
    CREATE TABLE [Orders] (
        [Id] int NOT NULL IDENTITY,
        [CustomerId] int NOT NULL,
        [Total] decimal(18,2) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_Orders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Orders_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106065446_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Orders_CustomerId] ON [Orders] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106065446_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261106065446_InitialCreate', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106065453_AddCustomerEmail'
)
BEGIN
    ALTER TABLE [Customers] ADD [Email] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106065453_AddCustomerEmail'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261106065453_AddCustomerEmail', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114032721_AddOrderStoredProcedures'
)
BEGIN
    EXEC(N'
    CREATE OR ALTER PROCEDURE GetOrdersForCustomer
        @CustomerId INT
    AS
    BEGIN
        SET NOCOUNT ON;
        SELECT Id, CustomerId, Total, CreatedAtUtc
        FROM Orders
        WHERE CustomerId = @CustomerId
        ORDER BY CreatedAtUtc DESC;
    END');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114032721_AddOrderStoredProcedures'
)
BEGIN
    EXEC(N'
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
    END');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114032721_AddOrderStoredProcedures'
)
BEGIN
    EXEC(N'
    CREATE OR ALTER PROCEDURE GetCustomerOrderTotal
        @CustomerId INT,
        @Total DECIMAL(18, 2) OUTPUT
    AS
    BEGIN
        SET NOCOUNT ON;
        SELECT @Total = COALESCE(SUM(Total), 0)
        FROM Orders
        WHERE CustomerId = @CustomerId;
    END');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114032721_AddOrderStoredProcedures'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261114032721_AddOrderStoredProcedures', N'10.0.14');
END;

COMMIT;
GO

