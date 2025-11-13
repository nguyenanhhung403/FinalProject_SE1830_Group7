-- Thêm vai trò mặc định
IF NOT EXISTS (SELECT 1 FROM ev.Roles WHERE RoleName = 'SC Staff')
    INSERT INTO ev.Roles (RoleName) VALUES ('SC Staff');
IF NOT EXISTS (SELECT 1 FROM ev.Roles WHERE RoleName = 'SC Technician')
    INSERT INTO ev.Roles (RoleName) VALUES ('SC Technician');
IF NOT EXISTS (SELECT 1 FROM ev.Roles WHERE RoleName = 'EVM Staff')
    INSERT INTO ev.Roles (RoleName) VALUES ('EVM Staff');
IF NOT EXISTS (SELECT 1 FROM ev.Roles WHERE RoleName = 'Admin')
    INSERT INTO ev.Roles (RoleName) VALUES ('Admin');
IF NOT EXISTS (SELECT 1 FROM ev.Roles WHERE RoleName = 'Customer')
    INSERT INTO ev.Roles (RoleName) VALUES ('Customer');

-- Tạo 3 service centers
INSERT INTO ev.ServiceCenters (Name, Address, ContactName, ContactPhone) VALUES
('SC - Hanoi Center','Hanoi street','Tran A','0123456789'),
('SC - Saigon Center','Saigon street','Nguyen B','0987654321'),
('SC - Da Nang Center','Da Nang street','Le C','0912345678');

-- Tạo người dùng mẫu (mật khẩu plain: password1..4, adminpass)
IF NOT EXISTS (SELECT 1 FROM ev.Users WHERE Username = 'sc_staff1')
    INSERT INTO ev.Users (Username, PasswordHash, FullName, Email, RoleId)
    VALUES ('sc_staff1', HASHBYTES('SHA2_256','password1'), N'SC Staff 1', 'scstaff1@example.com', (SELECT RoleId FROM ev.Roles WHERE RoleName='SC Staff'));

IF NOT EXISTS (SELECT 1 FROM ev.Users WHERE Username = 'sc_tech1')
    INSERT INTO ev.Users (Username, PasswordHash, FullName, Email, RoleId)
    VALUES ('sc_tech1', HASHBYTES('SHA2_256','password2'), N'SC Technician 1', 'sctech1@example.com', (SELECT RoleId FROM ev.Roles WHERE RoleName='SC Technician'));

IF NOT EXISTS (SELECT 1 FROM ev.Users WHERE Username = 'evm_user1')
    INSERT INTO ev.Users (Username, PasswordHash, FullName, Email, RoleId)
    VALUES ('evm_user1', HASHBYTES('SHA2_256','password3'), N'EVM Staff 1', 'evm1@example.com', (SELECT RoleId FROM ev.Roles WHERE RoleName='EVM Staff'));

IF NOT EXISTS (SELECT 1 FROM ev.Users WHERE Username = 'admin1')
    INSERT INTO ev.Users (Username, PasswordHash, FullName, Email, RoleId)
    VALUES ('admin1', HASHBYTES('SHA2_256','adminpass'), N'Admin 1', 'admin@example.com', (SELECT RoleId FROM ev.Roles WHERE RoleName='Admin'));

-- Tạo người dùng khách hàng (mật khẩu: password123)
IF NOT EXISTS (SELECT 1 FROM ev.Users WHERE Username = 'customer1')
    INSERT INTO ev.Users (Username, PasswordHash, FullName, Email, RoleId, Phone)
    VALUES ('customer1', HASHBYTES('SHA2_256','password123'), N'Nguyễn Thị Mai', 'customer1@example.com', (SELECT RoleId FROM ev.Roles WHERE RoleName='Customer'), '0901001001');

IF NOT EXISTS (SELECT 1 FROM ev.Users WHERE Username = 'customer2')
    INSERT INTO ev.Users (Username, PasswordHash, FullName, Email, RoleId, Phone)
    VALUES ('customer2', HASHBYTES('SHA2_256','password123'), N'Trần Minh Quân', 'customer2@example.com', (SELECT RoleId FROM ev.Roles WHERE RoleName='Customer'), '0902002002');

IF NOT EXISTS (SELECT 1 FROM ev.Users WHERE Username = 'customer3')
    INSERT INTO ev.Users (Username, PasswordHash, FullName, Email, RoleId, Phone)
    VALUES ('customer3', HASHBYTES('SHA2_256','password123'), N'Lê Mỹ Hạnh', 'customer3@example.com', (SELECT RoleId FROM ev.Roles WHERE RoleName='Customer'), '0903003003');

-- Tạo khách hàng
IF NOT EXISTS (SELECT 1 FROM ev.Customers WHERE Email = 'khach@example.com')
    INSERT INTO ev.Customers (FullName, Email, Phone) VALUES ('Nguyen Van Khach','khach@example.com','0900000001');

IF NOT EXISTS (SELECT 1 FROM ev.Customers WHERE Email = 'customer1@example.com')
    INSERT INTO ev.Customers (FullName, Email, Phone, Address) VALUES (N'Nguyễn Thị Mai','customer1@example.com','0901001001', N'12 Lê Lợi, Hà Nội');

IF NOT EXISTS (SELECT 1 FROM ev.Customers WHERE Email = 'customer2@example.com')
    INSERT INTO ev.Customers (FullName, Email, Phone, Address) VALUES (N'Trần Minh Quân','customer2@example.com','0902002002', N'56 Nguyễn Huệ, TP.HCM');

IF NOT EXISTS (SELECT 1 FROM ev.Customers WHERE Email = 'customer3@example.com')
    INSERT INTO ev.Customers (FullName, Email, Phone, Address) VALUES (N'Lê Mỹ Hạnh','customer3@example.com','0903003003', N'89 Bạch Đằng, Đà Nẵng');

DECLARE @Customer1Id INT = (SELECT TOP 1 CustomerId FROM ev.Customers WHERE Email = 'customer1@example.com');
DECLARE @Customer2Id INT = (SELECT TOP 1 CustomerId FROM ev.Customers WHERE Email = 'customer2@example.com');
DECLARE @Customer3Id INT = (SELECT TOP 1 CustomerId FROM ev.Customers WHERE Email = 'customer3@example.com');

IF @Customer1Id IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM ev.Vehicles WHERE VIN = 'VIN0001')
        UPDATE ev.Vehicles
        SET CustomerId = @Customer1Id,
            Model = 'ModelX',
            Year = 2022,
            RegistrationNumber = '29A-00001'
        WHERE VIN = 'VIN0001';
    ELSE
        INSERT INTO ev.Vehicles (VIN, Model, CustomerId, Year, RegistrationNumber)
        VALUES ('VIN0001','ModelX',@Customer1Id,2022,'29A-00001');
END;

IF @Customer2Id IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM ev.Vehicles WHERE VIN = 'VIN0002')
        UPDATE ev.Vehicles
        SET CustomerId = @Customer2Id,
            Model = 'ModelY',
            Year = 2021,
            RegistrationNumber = '30A-00002'
        WHERE VIN = 'VIN0002';
    ELSE
        INSERT INTO ev.Vehicles (VIN, Model, CustomerId, Year, RegistrationNumber)
        VALUES ('VIN0002','ModelY',@Customer2Id,2021,'30A-00002');
END;

IF @Customer3Id IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM ev.Vehicles WHERE VIN = 'VIN0003')
        UPDATE ev.Vehicles
        SET CustomerId = @Customer3Id,
            Model = 'ModelZ',
            Year = 2023,
            RegistrationNumber = '31A-00003'
        WHERE VIN = 'VIN0003';
    ELSE
        INSERT INTO ev.Vehicles (VIN, Model, CustomerId, Year, RegistrationNumber)
        VALUES ('VIN0003','ModelZ',@Customer3Id,2023,'31A-00003');
END;

-- Tạo phụ tùng
INSERT INTO ev.Parts (PartCode, PartName, UnitPrice, WarrantyPeriodMonths) VALUES
('P-0001','Battery Module',1000.00,24),
('P-0002','Inverter',500.00,12),
('P-0003','Charging Port',150.00,12);

-- Tạo 3 yêu cầu bảo hành mẫu
EXEC ev.sp_CreateClaim @VIN='VIN0001', @ServiceCenterId=1, @CreatedByUserId=1, @DateDiscovered='2025-11-01', @Description='Pin lỗi không sạc';
EXEC ev.sp_CreateClaim @VIN='VIN0002', @ServiceCenterId=2, @CreatedByUserId=2, @DateDiscovered='2025-10-20', @Description='Không khởi động được';
EXEC ev.sp_CreateClaim @VIN='VIN0003', @ServiceCenterId=3, @CreatedByUserId=1, @DateDiscovered='2025-09-15', @Description='Lỗi cổng sạc chập chờn';