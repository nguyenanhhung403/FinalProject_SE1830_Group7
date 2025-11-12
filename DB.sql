-- Chuyển context nếu cần
CREATE DATABASE EVWarrantyManagement;
GO;
USE EVWarrantyManagement;
GO

-- Tạo schema nếu muốn tách
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'ev')
    EXEC('CREATE SCHEMA ev;');
GO

/********************************************
 * 1. Roles, Users, ServiceCenters
 ********************************************/
CREATE TABLE ev.Roles (
    RoleId INT IDENTITY(1,1) PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL UNIQUE -- 'SC', 'EVM', 'Admin'
);
GO

CREATE TABLE ev.Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash VARBINARY(64) NOT NULL,
    FullName NVARCHAR(200) NULL,
    Email NVARCHAR(200) NULL,
    Phone NVARCHAR(20) NULL,
    RoleId INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES ev.Roles(RoleId)
);
GO

CREATE TABLE ev.ServiceCenters (
    ServiceCenterId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Address NVARCHAR(400) NULL,
    ContactName NVARCHAR(200) NULL,
    ContactPhone NVARCHAR(20) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

/********************************************
 * 2. Vehicles, Customers
 ********************************************/
CREATE TABLE ev.Customers (
    CustomerId INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(200) NOT NULL,
    Email NVARCHAR(200) NULL,
    Phone NVARCHAR(20) NULL,
    Address NVARCHAR(400) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

CREATE TABLE ev.Vehicles (
    VehicleId INT IDENTITY(1,1) PRIMARY KEY,
    VIN NVARCHAR(50) NOT NULL UNIQUE,
    Model NVARCHAR(200) NULL,
    CustomerId INT NULL,
    Year INT NULL,
    RegistrationNumber NVARCHAR(50) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Vehicles_Customers FOREIGN KEY (CustomerId) REFERENCES ev.Customers(CustomerId)
);
GO

/********************************************
 * 3. Parts
 ********************************************/
CREATE TABLE ev.Parts (
    PartId INT IDENTITY(1,1) PRIMARY KEY,
    PartCode NVARCHAR(100) NOT NULL UNIQUE,
    PartName NVARCHAR(300) NOT NULL,
    UnitPrice DECIMAL(18,2) NULL,
    WarrantyPeriodMonths INT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

/********************************************
 * 4. ClaimStatus lookup
 ********************************************/
CREATE TABLE ev.ClaimStatuses (
    StatusCode NVARCHAR(30) PRIMARY KEY, -- 'Pending','OnHold','Approved','Rejected','InProgress','Completed','Closed'
    Description NVARCHAR(200) NULL
);
GO

INSERT INTO ev.ClaimStatuses (StatusCode, Description)
VALUES
('Pending','Chờ duyệt'),
('OnHold','Chờ bổ sung thông tin'),
('Approved','Đã phê duyệt'),
('Rejected','Từ chối'),
('InProgress','Đang thực hiện'),
('Completed','Hoàn thành'),
('Closed','Đóng (lưu kho lịch sử)')
;
GO

/********************************************
 * 5. WarrantyClaim, UsedParts, WarrantyHistory
 ********************************************/
CREATE TABLE ev.WarrantyClaim (
    ClaimId INT IDENTITY(1,1) PRIMARY KEY,
    VIN NVARCHAR(50) NOT NULL,
    VehicleId INT NULL, -- tiện lookup
    ServiceCenterId INT NOT NULL,
    CreatedByUserId INT NOT NULL, -- SC staff tạo
    DateDiscovered DATE NOT NULL,
    Description NVARCHAR(2000) NULL,
    ImageUrl NVARCHAR(1000) NULL,
    StatusCode NVARCHAR(30) NOT NULL DEFAULT 'Pending',
    Cost DECIMAL(18,2) NULL, -- chi phí hãng chi trả hoặc thống nhất
    Note NVARCHAR(2000) NULL,
    TechnicianId INT NULL, -- userId của technician khi thực hiện
    CompletionDate DATE NULL,
    TotalCost DECIMAL(18,2) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Claim_Status FOREIGN KEY (StatusCode) REFERENCES ev.ClaimStatuses(StatusCode),
    CONSTRAINT FK_Claim_ServiceCenter FOREIGN KEY (ServiceCenterId) REFERENCES ev.ServiceCenters(ServiceCenterId),
    CONSTRAINT FK_Claim_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES ev.Users(UserId),
    CONSTRAINT FK_Claim_Technician FOREIGN KEY (TechnicianId) REFERENCES ev.Users(UserId)
);
GO

-- Used parts per claim
CREATE TABLE ev.UsedParts (
    UsedPartId INT IDENTITY(1,1) PRIMARY KEY,
    ClaimId INT NOT NULL,
    PartId INT NOT NULL,
    Quantity INT NOT NULL DEFAULT 1,
    PartCost DECIMAL(18,2) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_UsedParts_Claim FOREIGN KEY (ClaimId) REFERENCES ev.WarrantyClaim(ClaimId),
    CONSTRAINT FK_UsedParts_Part FOREIGN KEY (PartId) REFERENCES ev.Parts(PartId)
);
GO

-- Warranty history: snapshot khi hoàn tất
CREATE TABLE ev.WarrantyHistory (
    HistoryId INT IDENTITY(1,1) PRIMARY KEY,
    ClaimId INT NOT NULL,
    VIN NVARCHAR(50) NOT NULL,
    VehicleId INT NULL,
    ServiceCenterId INT NULL,
    CompletedByUserId INT NULL,
    CompletionDate DATE NULL,
    TotalCost DECIMAL(18,2) NULL,
    Note NVARCHAR(2000) NULL,
    ArchivedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

/********************************************
 * 6. Optional: Audit table for status changes
 ********************************************/
CREATE TABLE ev.ClaimStatusLog (
    LogId INT IDENTITY(1,1) PRIMARY KEY,
    ClaimId INT NOT NULL,
    OldStatus NVARCHAR(30) NULL,
    NewStatus NVARCHAR(30) NOT NULL,
    ChangedByUserId INT NOT NULL,
    ChangedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    Comment NVARCHAR(1000) NULL,
    CONSTRAINT FK_ClaimStatusLog_Claim FOREIGN KEY (ClaimId) REFERENCES ev.WarrantyClaim(ClaimId),
    CONSTRAINT FK_ClaimStatusLog_User FOREIGN KEY (ChangedByUserId) REFERENCES ev.Users(UserId)
);
GO

/********************************************
 * Indexes để tăng tốc truy vấn
 ********************************************/
CREATE NONCLUSTERED INDEX IX_WarrantyClaim_Status ON ev.WarrantyClaim (StatusCode);
CREATE NONCLUSTERED INDEX IX_WarrantyClaim_VIN ON ev.WarrantyClaim (VIN);
CREATE NONCLUSTERED INDEX IX_Vehicles_VIN ON ev.Vehicles (VIN);
GO
