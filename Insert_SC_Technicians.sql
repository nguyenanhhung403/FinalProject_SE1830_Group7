-- =============================================
-- INSERT MULTIPLE SC TECHNICIAN USERS
-- =============================================

USE EVWarrantyManagement;
GO

-- Get RoleId for SC Technician role (assuming it exists, if not, create it)
DECLARE @SCTechnicianRoleId INT;
DECLARE @SCRoleId INT;
DECLARE @AdminUserId INT = 5; -- admin1

-- Get or create SC Technician role
SELECT @SCTechnicianRoleId = RoleId FROM ev.Roles WHERE RoleName = 'SC Technician';
IF @SCTechnicianRoleId IS NULL
BEGIN
    INSERT INTO ev.Roles (RoleName) VALUES ('SC Technician');
    SET @SCTechnicianRoleId = SCOPE_IDENTITY();
    PRINT 'Created SC Technician role';
END

-- Get SC role ID (for fallback)
SELECT @SCRoleId = RoleId FROM ev.Roles WHERE RoleName = 'SC';

-- Insert multiple SC Technician users
-- Password for all: 'password123' (SHA2_256 hash)
DECLARE @PasswordHash VARBINARY(64) = HASHBYTES('SHA2_256', 'password123');

-- SC Technician 3
IF NOT EXISTS (SELECT 1 FROM ev.Users WHERE Username = 'sc_tech3')
BEGIN
    INSERT INTO ev.Users (Username, PasswordHash, FullName, Email, Phone, RoleId, IsActive)
    VALUES ('sc_tech3', @PasswordHash, N'Nguyễn Văn C', 'sctech3@example.com', '0911111111', @SCTechnicianRoleId, 1);
    PRINT 'Created sc_tech3';
END
GO

-- SC Technician 4
IF NOT EXISTS (SELECT 1 FROM ev.Users WHERE Username = 'sc_tech4')
BEGIN
    DECLARE @PasswordHash VARBINARY(64) = HASHBYTES('SHA2_256', 'password123');
    DECLARE @SCTechnicianRoleId INT = (SELECT RoleId FROM ev.Roles WHERE RoleName = 'SC Technician');
    INSERT INTO ev.Users (Username, PasswordHash, FullName, Email, Phone, RoleId, IsActive)
    VALUES ('sc_tech4', @PasswordHash, N'Trần Thị D', 'sctech4@example.com', '0922222222', @SCTechnicianRoleId, 1);
    PRINT 'Created sc_tech4';
END
GO

-- SC Technician 5
IF NOT EXISTS (SELECT 1 FROM ev.Users WHERE Username = 'sc_tech5')
BEGIN
    DECLARE @PasswordHash VARBINARY(64) = HASHBYTES('SHA2_256', 'password123');
    DECLARE @SCTechnicianRoleId INT = (SELECT RoleId FROM ev.Roles WHERE RoleName = 'SC Technician');
    INSERT INTO ev.Users (Username, PasswordHash, FullName, Email, Phone, RoleId, IsActive)
    VALUES ('sc_tech5', @PasswordHash, N'Lê Văn E', 'sctech5@example.com', '0933333333', @SCTechnicianRoleId, 1);
    PRINT 'Created sc_tech5';
END
GO

-- SC Technician 6
IF NOT EXISTS (SELECT 1 FROM ev.Users WHERE Username = 'sc_tech6')
BEGIN
    DECLARE @PasswordHash VARBINARY(64) = HASHBYTES('SHA2_256', 'password123');
    DECLARE @SCTechnicianRoleId INT = (SELECT RoleId FROM ev.Roles WHERE RoleName = 'SC Technician');
    INSERT INTO ev.Users (Username, PasswordHash, FullName, Email, Phone, RoleId, IsActive)
    VALUES ('sc_tech6', @PasswordHash, N'Phạm Thị F', 'sctech6@example.com', '0944444444', @SCTechnicianRoleId, 1);
    PRINT 'Created sc_tech6';
END
GO

-- SC Technician 7
IF NOT EXISTS (SELECT 1 FROM ev.Users WHERE Username = 'sc_tech7')
BEGIN
    DECLARE @PasswordHash VARBINARY(64) = HASHBYTES('SHA2_256', 'password123');
    DECLARE @SCTechnicianRoleId INT = (SELECT RoleId FROM ev.Roles WHERE RoleName = 'SC Technician');
    INSERT INTO ev.Users (Username, PasswordHash, FullName, Email, Phone, RoleId, IsActive)
    VALUES ('sc_tech7', @PasswordHash, N'Hoàng Văn G', 'sctech7@example.com', '0955555555', @SCTechnicianRoleId, 1);
    PRINT 'Created sc_tech7';
END
GO

-- SC Technician 8
IF NOT EXISTS (SELECT 1 FROM ev.Users WHERE Username = 'sc_tech8')
BEGIN
    DECLARE @PasswordHash VARBINARY(64) = HASHBYTES('SHA2_256', 'password123');
    DECLARE @SCTechnicianRoleId INT = (SELECT RoleId FROM ev.Roles WHERE RoleName = 'SC Technician');
    INSERT INTO ev.Users (Username, PasswordHash, FullName, Email, Phone, RoleId, IsActive)
    VALUES ('sc_tech8', @PasswordHash, N'Vũ Thị H', 'sctech8@example.com', '0966666666', @SCTechnicianRoleId, 1);
    PRINT 'Created sc_tech8';
END
GO

-- SC Technician 9
IF NOT EXISTS (SELECT 1 FROM ev.Users WHERE Username = 'sc_tech9')
BEGIN
    DECLARE @PasswordHash VARBINARY(64) = HASHBYTES('SHA2_256', 'password123');
    DECLARE @SCTechnicianRoleId INT = (SELECT RoleId FROM ev.Roles WHERE RoleName = 'SC Technician');
    INSERT INTO ev.Users (Username, PasswordHash, FullName, Email, Phone, RoleId, IsActive)
    VALUES ('sc_tech9', @PasswordHash, N'Đỗ Văn I', 'sctech9@example.com', '0977777777', @SCTechnicianRoleId, 1);
    PRINT 'Created sc_tech9';
END
GO

-- SC Technician 10
IF NOT EXISTS (SELECT 1 FROM ev.Users WHERE Username = 'sc_tech10')
BEGIN
    DECLARE @PasswordHash VARBINARY(64) = HASHBYTES('SHA2_256', 'password123');
    DECLARE @SCTechnicianRoleId INT = (SELECT RoleId FROM ev.Roles WHERE RoleName = 'SC Technician');
    INSERT INTO ev.Users (Username, PasswordHash, FullName, Email, Phone, RoleId, IsActive)
    VALUES ('sc_tech10', @PasswordHash, N'Bùi Thị J', 'sctech10@example.com', '0988888888', @SCTechnicianRoleId, 1);
    PRINT 'Created sc_tech10';
END
GO

-- =============================================
-- Assign technicians to Service Centers
-- =============================================

-- Get UserIds and ServiceCenterIds
DECLARE @Tech3Id INT = (SELECT UserId FROM ev.Users WHERE Username = 'sc_tech3');
DECLARE @Tech4Id INT = (SELECT UserId FROM ev.Users WHERE Username = 'sc_tech4');
DECLARE @Tech5Id INT = (SELECT UserId FROM ev.Users WHERE Username = 'sc_tech5');
DECLARE @Tech6Id INT = (SELECT UserId FROM ev.Users WHERE Username = 'sc_tech6');
DECLARE @Tech7Id INT = (SELECT UserId FROM ev.Users WHERE Username = 'sc_tech7');
DECLARE @Tech8Id INT = (SELECT UserId FROM ev.Users WHERE Username = 'sc_tech8');
DECLARE @Tech9Id INT = (SELECT UserId FROM ev.Users WHERE Username = 'sc_tech9');
DECLARE @Tech10Id INT = (SELECT UserId FROM ev.Users WHERE Username = 'sc_tech10');

DECLARE @HanoiSCId INT = 1; -- SC - Hanoi Center
DECLARE @SaigonSCId INT = 2; -- SC - Saigon Center
DECLARE @DaNangSCId INT = 3; -- SC - Da Nang Center

-- Assign sc_tech3 to Hanoi
IF @Tech3Id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ev.ServiceCenterTechnicians WHERE ServiceCenterId = @HanoiSCId AND UserId = @Tech3Id AND IsActive = 1)
BEGIN
    INSERT INTO ev.ServiceCenterTechnicians (ServiceCenterId, UserId, AssignedByUserId, AssignedAt, IsActive)
    VALUES (@HanoiSCId, @Tech3Id, @AdminUserId, SYSUTCDATETIME(), 1);
    PRINT 'Assigned sc_tech3 to SC - Hanoi Center';
END
GO

-- Assign sc_tech4 to Hanoi
IF EXISTS (SELECT 1 FROM ev.Users WHERE Username = 'sc_tech4')
BEGIN
    DECLARE @Tech4Id INT = (SELECT UserId FROM ev.Users WHERE Username = 'sc_tech4');
    DECLARE @HanoiSCId INT = 1;
    DECLARE @AdminUserId INT = 5;
    IF NOT EXISTS (SELECT 1 FROM ev.ServiceCenterTechnicians WHERE ServiceCenterId = @HanoiSCId AND UserId = @Tech4Id AND IsActive = 1)
    BEGIN
        INSERT INTO ev.ServiceCenterTechnicians (ServiceCenterId, UserId, AssignedByUserId, AssignedAt, IsActive)
        VALUES (@HanoiSCId, @Tech4Id, @AdminUserId, SYSUTCDATETIME(), 1);
        PRINT 'Assigned sc_tech4 to SC - Hanoi Center';
    END
END
GO

-- Assign sc_tech5 to Saigon
IF EXISTS (SELECT 1 FROM ev.Users WHERE Username = 'sc_tech5')
BEGIN
    DECLARE @Tech5Id INT = (SELECT UserId FROM ev.Users WHERE Username = 'sc_tech5');
    DECLARE @SaigonSCId INT = 2;
    DECLARE @AdminUserId INT = 5;
    IF NOT EXISTS (SELECT 1 FROM ev.ServiceCenterTechnicians WHERE ServiceCenterId = @SaigonSCId AND UserId = @Tech5Id AND IsActive = 1)
    BEGIN
        INSERT INTO ev.ServiceCenterTechnicians (ServiceCenterId, UserId, AssignedByUserId, AssignedAt, IsActive)
        VALUES (@SaigonSCId, @Tech5Id, @AdminUserId, SYSUTCDATETIME(), 1);
        PRINT 'Assigned sc_tech5 to SC - Saigon Center';
    END
END
GO

-- Assign sc_tech6 to Saigon
IF EXISTS (SELECT 1 FROM ev.Users WHERE Username = 'sc_tech6')
BEGIN
    DECLARE @Tech6Id INT = (SELECT UserId FROM ev.Users WHERE Username = 'sc_tech6');
    DECLARE @SaigonSCId INT = 2;
    DECLARE @AdminUserId INT = 5;
    IF NOT EXISTS (SELECT 1 FROM ev.ServiceCenterTechnicians WHERE ServiceCenterId = @SaigonSCId AND UserId = @Tech6Id AND IsActive = 1)
    BEGIN
        INSERT INTO ev.ServiceCenterTechnicians (ServiceCenterId, UserId, AssignedByUserId, AssignedAt, IsActive)
        VALUES (@SaigonSCId, @Tech6Id, @AdminUserId, SYSUTCDATETIME(), 1);
        PRINT 'Assigned sc_tech6 to SC - Saigon Center';
    END
END
GO

-- Assign sc_tech7 to Da Nang
IF EXISTS (SELECT 1 FROM ev.Users WHERE Username = 'sc_tech7')
BEGIN
    DECLARE @Tech7Id INT = (SELECT UserId FROM ev.Users WHERE Username = 'sc_tech7');
    DECLARE @DaNangSCId INT = 3;
    DECLARE @AdminUserId INT = 5;
    IF NOT EXISTS (SELECT 1 FROM ev.ServiceCenterTechnicians WHERE ServiceCenterId = @DaNangSCId AND UserId = @Tech7Id AND IsActive = 1)
    BEGIN
        INSERT INTO ev.ServiceCenterTechnicians (ServiceCenterId, UserId, AssignedByUserId, AssignedAt, IsActive)
        VALUES (@DaNangSCId, @Tech7Id, @AdminUserId, SYSUTCDATETIME(), 1);
        PRINT 'Assigned sc_tech7 to SC - Da Nang Center';
    END
END
GO

-- Assign sc_tech8 to Da Nang
IF EXISTS (SELECT 1 FROM ev.Users WHERE Username = 'sc_tech8')
BEGIN
    DECLARE @Tech8Id INT = (SELECT UserId FROM ev.Users WHERE Username = 'sc_tech8');
    DECLARE @DaNangSCId INT = 3;
    DECLARE @AdminUserId INT = 5;
    IF NOT EXISTS (SELECT 1 FROM ev.ServiceCenterTechnicians WHERE ServiceCenterId = @DaNangSCId AND UserId = @Tech8Id AND IsActive = 1)
    BEGIN
        INSERT INTO ev.ServiceCenterTechnicians (ServiceCenterId, UserId, AssignedByUserId, AssignedAt, IsActive)
        VALUES (@DaNangSCId, @Tech8Id, @AdminUserId, SYSUTCDATETIME(), 1);
        PRINT 'Assigned sc_tech8 to SC - Da Nang Center';
    END
END
GO

-- Assign sc_tech9 to Hanoi (additional)
IF EXISTS (SELECT 1 FROM ev.Users WHERE Username = 'sc_tech9')
BEGIN
    DECLARE @Tech9Id INT = (SELECT UserId FROM ev.Users WHERE Username = 'sc_tech9');
    DECLARE @HanoiSCId INT = 1;
    DECLARE @AdminUserId INT = 5;
    IF NOT EXISTS (SELECT 1 FROM ev.ServiceCenterTechnicians WHERE ServiceCenterId = @HanoiSCId AND UserId = @Tech9Id AND IsActive = 1)
    BEGIN
        INSERT INTO ev.ServiceCenterTechnicians (ServiceCenterId, UserId, AssignedByUserId, AssignedAt, IsActive)
        VALUES (@HanoiSCId, @Tech9Id, @AdminUserId, SYSUTCDATETIME(), 1);
        PRINT 'Assigned sc_tech9 to SC - Hanoi Center';
    END
END
GO

-- Assign sc_tech10 to Saigon (additional)
IF EXISTS (SELECT 1 FROM ev.Users WHERE Username = 'sc_tech10')
BEGIN
    DECLARE @Tech10Id INT = (SELECT UserId FROM ev.Users WHERE Username = 'sc_tech10');
    DECLARE @SaigonSCId INT = 2;
    DECLARE @AdminUserId INT = 5;
    IF NOT EXISTS (SELECT 1 FROM ev.ServiceCenterTechnicians WHERE ServiceCenterId = @SaigonSCId AND UserId = @Tech10Id AND IsActive = 1)
    BEGIN
        INSERT INTO ev.ServiceCenterTechnicians (ServiceCenterId, UserId, AssignedByUserId, AssignedAt, IsActive)
        VALUES (@SaigonSCId, @Tech10Id, @AdminUserId, SYSUTCDATETIME(), 1);
        PRINT 'Assigned sc_tech10 to SC - Saigon Center';
    END
END
GO

PRINT '=============================================';
PRINT 'SC Technician users and assignments created!';
PRINT '=============================================';
PRINT 'Created users: sc_tech3 through sc_tech10';
PRINT 'Password for all: password123';
PRINT '';
PRINT 'Assignments:';
PRINT '  - Hanoi Center: sc_tech3, sc_tech4, sc_tech9';
PRINT '  - Saigon Center: sc_tech5, sc_tech6, sc_tech10';
PRINT '  - Da Nang Center: sc_tech7, sc_tech8';
PRINT '=============================================';
GO

