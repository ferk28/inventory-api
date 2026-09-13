-- Inventory database schema and seed data.
-- Executed by the sqlserver-init container. Idempotent: safe to run more than once.
-- The database name is injected by sqlcmd: -v DbName="<value from DB_NAME env var>"
IF DB_ID('$(DbName)') IS NULL
BEGIN
    CREATE DATABASE [$(DbName)];
END;
GO
USE [$(DbName)];
GO
IF OBJECT_ID('dbo.Categories', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Categories (
        Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Categories PRIMARY KEY,
        Name        NVARCHAR(100)     NOT NULL,
        Description NVARCHAR(500)     NULL,
        IsActive    BIT               NOT NULL CONSTRAINT DF_Categories_IsActive DEFAULT 1,
        CreatedAt   DATETIME2(0)      NOT NULL CONSTRAINT DF_Categories_CreatedAt DEFAULT SYSUTCDATETIME()
    );
    CREATE UNIQUE INDEX UX_Categories_Name ON dbo.Categories (Name);
END;
GO
-- BR-03 needs an inactive category to be a thing. Added after the first release,
-- so databases created before it get the column here instead.
IF COL_LENGTH('dbo.Categories', 'IsActive') IS NULL
BEGIN
    ALTER TABLE dbo.Categories
        ADD IsActive BIT NOT NULL CONSTRAINT DF_Categories_IsActive DEFAULT 1;
END;
GO
IF OBJECT_ID('dbo.Products', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products (
        Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
        Sku         NVARCHAR(50)      NOT NULL,
        Name        NVARCHAR(150)     NOT NULL,
        Description NVARCHAR(500)     NULL,
        Price       DECIMAL(18,2)     NOT NULL CONSTRAINT CK_Products_Price CHECK (Price >= 0),
        Stock       INT               NOT NULL CONSTRAINT DF_Products_Stock DEFAULT 0 CONSTRAINT CK_Products_Stock CHECK (Stock >= 0),
        CategoryId  INT               NOT NULL CONSTRAINT FK_Products_Categories FOREIGN KEY REFERENCES dbo.Categories (Id),
        IsActive    BIT               NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT 1,
        CreatedAt   DATETIME2(0)      NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt   DATETIME2(0)      NULL
    );
    CREATE UNIQUE INDEX UX_Products_Sku ON dbo.Products (Sku);
    CREATE INDEX IX_Products_CategoryId ON dbo.Products (CategoryId);
    CREATE INDEX IX_Products_IsActive ON dbo.Products (IsActive) INCLUDE (Name, Sku);
END;
GO
IF OBJECT_ID('dbo.InventoryMovements', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.InventoryMovements (
        Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_InventoryMovements PRIMARY KEY,
        ProductId   INT               NOT NULL CONSTRAINT FK_InventoryMovements_Products FOREIGN KEY REFERENCES dbo.Products (Id),
        Type        TINYINT           NOT NULL CONSTRAINT CK_InventoryMovements_Type CHECK (Type IN (1, 2)),
        Quantity    INT               NOT NULL CONSTRAINT CK_InventoryMovements_Quantity CHECK (Quantity > 0),
        Reason      NVARCHAR(250)     NULL,
        CreatedAt   DATETIME2(0)      NOT NULL CONSTRAINT DF_InventoryMovements_CreatedAt DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_InventoryMovements_ProductId_CreatedAt ON dbo.InventoryMovements (ProductId, CreatedAt DESC);
END;
GO
-- Seed data (only when the database is empty).
IF NOT EXISTS (SELECT 1 FROM dbo.Categories)
BEGIN
    INSERT INTO dbo.Categories (Name, Description) VALUES
        (N'Electronics', N'Devices and accessories'),
        (N'Office Supplies', N'Stationery and office consumables'),
        (N'Furniture', N'Desks, chairs and storage');
    INSERT INTO dbo.Products (Sku, Name, Description, Price, Stock, CategoryId) VALUES
        (N'ELEC-0001', N'Wireless Mouse', N'2.4 GHz optical mouse', 19.99, 25, 1),
        (N'ELEC-0002', N'USB-C Hub', N'7-in-1 USB-C hub', 34.50, 10, 1),
        (N'OFFC-0001', N'A4 Paper Ream', N'500 sheets, 75 g/m2', 5.25, 100, 2),
        (N'FURN-0001', N'Ergonomic Chair', N'Adjustable office chair', 189.00, 4, 3);
    INSERT INTO dbo.InventoryMovements (ProductId, Type, Quantity, Reason) VALUES
        (1, 1, 30, N'Initial purchase'),
        (1, 2, 5,  N'Sale'),
        (2, 1, 10, N'Initial purchase'),
        (3, 1, 100, N'Initial purchase'),
        (4, 1, 4,  N'Initial purchase');
END;
GO
