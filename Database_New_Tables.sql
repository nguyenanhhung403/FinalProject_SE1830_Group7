-- =============================================
-- NEW TABLES FOR SERVICE CENTER & INVENTORY MANAGEMENT
-- DO NOT MODIFY EXISTING TABLES
-- =============================================

USE EVWarrantyManagement;
GO

-- =============================================
-- 1. Service Center Technicians (Junction Table)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ServiceCenterTechnicians' AND schema_id = SCHEMA_ID('ev'))
BEGIN
    CREATE TABLE ev.ServiceCenterTechnicians (
        ServiceCenterTechnicianId INT IDENTITY(1,1) PRIMARY KEY,
        ServiceCenterId INT NOT NULL,
        UserId INT NOT NULL,
        AssignedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        AssignedByUserId INT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CONSTRAINT FK_ServiceCenterTechnicians_ServiceCenter 
            FOREIGN KEY (ServiceCenterId) REFERENCES ev.ServiceCenters(ServiceCenterId),
        CONSTRAINT FK_ServiceCenterTechnicians_User 
            FOREIGN KEY (UserId) REFERENCES ev.Users(UserId),
        CONSTRAINT FK_ServiceCenterTechnicians_AssignedBy 
            FOREIGN KEY (AssignedByUserId) REFERENCES ev.Users(UserId)
    );
    
    -- Filtered unique index to prevent duplicate active technician assignments
    CREATE UNIQUE NONCLUSTERED INDEX UQ_ServiceCenterTechnician_Active 
        ON ev.ServiceCenterTechnicians(ServiceCenterId, UserId) 
        WHERE IsActive = 1;
    
    CREATE INDEX IX_ServiceCenterTechnicians_ServiceCenterId 
        ON ev.ServiceCenterTechnicians(ServiceCenterId);
    CREATE INDEX IX_ServiceCenterTechnicians_UserId 
        ON ev.ServiceCenterTechnicians(UserId);
    CREATE INDEX IX_ServiceCenterTechnicians_IsActive 
        ON ev.ServiceCenterTechnicians(IsActive);
END
GO

-- =============================================
-- 2. Part Inventory (Stock Levels)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PartInventory' AND schema_id = SCHEMA_ID('ev'))
BEGIN
    CREATE TABLE ev.PartInventory (
        InventoryId INT IDENTITY(1,1) PRIMARY KEY,
        PartId INT NOT NULL UNIQUE,
        StockQuantity INT NOT NULL DEFAULT 0,
        MinStockLevel INT NULL,
        LastUpdated DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedByUserId INT NULL,
        CONSTRAINT FK_PartInventory_Part 
            FOREIGN KEY (PartId) REFERENCES ev.Parts(PartId),
        CONSTRAINT FK_PartInventory_UpdatedBy 
            FOREIGN KEY (UpdatedByUserId) REFERENCES ev.Users(UserId)
    );
    
    CREATE INDEX IX_PartInventory_PartId 
        ON ev.PartInventory(PartId);
    CREATE INDEX IX_PartInventory_StockQuantity 
        ON ev.PartInventory(StockQuantity);
END
GO

-- =============================================
-- 3. Part Stock Movements (History)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PartStockMovements' AND schema_id = SCHEMA_ID('ev'))
BEGIN
    CREATE TABLE ev.PartStockMovements (
        MovementId INT IDENTITY(1,1) PRIMARY KEY,
        PartId INT NOT NULL,
        MovementType NVARCHAR(20) NOT NULL, -- 'IN', 'OUT', 'ADJUSTMENT', 'RESERVED', 'RELEASED'
        Quantity INT NOT NULL, -- positive for IN, negative for OUT
        ReferenceType NVARCHAR(50) NULL, -- 'CLAIM', 'PURCHASE', 'ADJUSTMENT', etc.
        ReferenceId INT NULL, -- ClaimId, PurchaseOrderId, etc.
        Note NVARCHAR(500) NULL,
        CreatedByUserId INT NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_PartStockMovements_Part 
            FOREIGN KEY (PartId) REFERENCES ev.Parts(PartId),
        CONSTRAINT FK_PartStockMovements_CreatedBy 
            FOREIGN KEY (CreatedByUserId) REFERENCES ev.Users(UserId),
        CONSTRAINT CK_PartStockMovements_Type 
            CHECK (MovementType IN ('IN', 'OUT', 'ADJUSTMENT', 'RESERVED', 'RELEASED'))
    );
    
    CREATE INDEX IX_PartStockMovements_PartId 
        ON ev.PartStockMovements(PartId);
    CREATE INDEX IX_PartStockMovements_CreatedAt 
        ON ev.PartStockMovements(CreatedAt);
    CREATE INDEX IX_PartStockMovements_Reference 
        ON ev.PartStockMovements(ReferenceType, ReferenceId);
END
GO

-- =============================================
-- Initialize PartInventory for existing Parts
-- =============================================
INSERT INTO ev.PartInventory (PartId, StockQuantity, MinStockLevel, LastUpdated)
SELECT PartId, 0, NULL, SYSUTCDATETIME()
FROM ev.Parts
WHERE PartId NOT IN (SELECT PartId FROM ev.PartInventory);
GO

PRINT 'New tables created successfully!';
PRINT 'ServiceCenterTechnicians: Junction table for assigning technicians to service centers';
PRINT 'PartInventory: Stock levels for each part';
PRINT 'PartStockMovements: History of all stock movements';

