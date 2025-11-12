-- Thêm vai trò mặc định
IF NOT EXISTS (SELECT 1 FROM ev.Roles WHERE RoleName = 'SC Staff')
    INSERT INTO ev.Roles (RoleName) VALUES ('SC Staff');
IF NOT EXISTS (SELECT 1 FROM ev.Roles WHERE RoleName = 'SC Technician')
    INSERT INTO ev.Roles (RoleName) VALUES ('SC Technician');
IF NOT EXISTS (SELECT 1 FROM ev.Roles WHERE RoleName = 'EVM Staff')
    INSERT INTO ev.Roles (RoleName) VALUES ('EVM Staff');
IF NOT EXISTS (SELECT 1 FROM ev.Roles WHERE RoleName = 'Admin')
    INSERT INTO ev.Roles (RoleName) VALUES ('Admin');

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

-- Tạo 1 khách hàng
INSERT INTO ev.Customers (FullName, Email, Phone) VALUES ('Nguyen Van Khach','khach@example.com','0900000001');

-- Tạo 3 phương tiện
INSERT INTO ev.Vehicles (VIN, Model, CustomerId, Year, RegistrationNumber) VALUES 
('VIN0001','ModelX',1,2022,'29A-00001'),
('VIN0002','ModelY',1,2021,'30A-00002'),
('VIN0003','ModelZ',1,2023,'31A-00003');

-- Tạo phụ tùng
INSERT INTO ev.Parts (PartCode, PartName, UnitPrice, WarrantyPeriodMonths) VALUES
('P-0001','Battery Module',1000.00,24),
('P-0002','Inverter',500.00,12),
('P-0003','Charging Port',150.00,12);

-- Tạo 3 yêu cầu bảo hành mẫu
EXEC ev.sp_CreateClaim @VIN='VIN0001', @ServiceCenterId=1, @CreatedByUserId=1, @DateDiscovered='2025-11-01', @Description='Pin lỗi không sạc';
EXEC ev.sp_CreateClaim @VIN='VIN0002', @ServiceCenterId=2, @CreatedByUserId=2, @DateDiscovered='2025-10-20', @Description='Không khởi động được';
EXEC ev.sp_CreateClaim @VIN='VIN0003', @ServiceCenterId=3, @CreatedByUserId=1, @DateDiscovered='2025-09-15', @Description='Lỗi cổng sạc chập chờn';