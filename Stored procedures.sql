/********************************************
 * SP: Đăng ký user (basic) - sp_RegisterUser
 * Tham số: username, password (plain) - sẽ hash trong SP
 ********************************************/

IF OBJECT_ID('ev.sp_RegisterUser','P') IS NOT NULL
    DROP PROCEDURE ev.sp_RegisterUser;
GO

CREATE PROCEDURE ev.sp_RegisterUser
    @Username NVARCHAR(100),
    @Password NVARCHAR(200),
    @FullName NVARCHAR(200) = NULL,
    @Email NVARCHAR(200) = NULL,
    @Phone NVARCHAR(20) = NULL,
    @RoleName NVARCHAR(50) = 'SC'  -- mặc định SC (Service Center staff)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        DECLARE @roleId INT;

        SELECT @roleId = RoleId FROM ev.Roles WHERE RoleName = @RoleName;
        IF @roleId IS NULL
        BEGIN
            RAISERROR('Role does not exist',16,1);
            RETURN;
        END

        DECLARE @pwdHash VARBINARY(64) = HASHBYTES('SHA2_256', CONVERT(VARBINARY(4000), @Password));

        INSERT INTO ev.Users (Username, PasswordHash, FullName, Email, Phone, RoleId)
        VALUES (@Username, @pwdHash, @FullName, @Email, @Phone, @roleId);

        SELECT SCOPE_IDENTITY() AS NewUserId;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
GO

/********************************************
 * SP: Tạo yêu cầu bảo hành - sp_CreateClaim
 * Tham số: VIN, VehicleId (nullable), ServiceCenterId, CreatedByUserId, DateDiscovered, Description, ImageUrl
 ********************************************/
IF OBJECT_ID('ev.sp_CreateClaim','P') IS NOT NULL
    DROP PROCEDURE ev.sp_CreateClaim;
GO

CREATE PROCEDURE ev.sp_CreateClaim
    @VIN NVARCHAR(50),
    @VehicleId INT = NULL,
    @ServiceCenterId INT,
    @CreatedByUserId INT,
    @DateDiscovered DATE,
    @Description NVARCHAR(2000) = NULL,
    @ImageUrl NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRAN;

        -- Nếu vehicleId NULL, có thể cố gắng lookup theo VIN
        IF @VehicleId IS NULL
        BEGIN
            SELECT TOP(1) @VehicleId = VehicleId FROM ev.Vehicles WHERE VIN = @VIN;
        END

        INSERT INTO ev.WarrantyClaim (VIN, VehicleId, ServiceCenterId, CreatedByUserId, DateDiscovered, Description, ImageUrl, StatusCode)
        VALUES (@VIN, @VehicleId, @ServiceCenterId, @CreatedByUserId, @DateDiscovered, @Description, @ImageUrl, 'Pending');

        DECLARE @NewClaimId INT = SCOPE_IDENTITY();

        -- Log status change
        INSERT INTO ev.ClaimStatusLog (ClaimId, OldStatus, NewStatus, ChangedByUserId, Comment)
        VALUES (@NewClaimId, NULL, 'Pending', @CreatedByUserId, 'Claim created');

        COMMIT TRAN;

        SELECT @NewClaimId AS ClaimId;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRAN;
        THROW;
    END CATCH
END
GO

/********************************************
 * SP: Lấy claims pending (dành cho EVM Staff)
 ********************************************/
IF OBJECT_ID('ev.sp_GetPendingClaims','P') IS NOT NULL
    DROP PROCEDURE ev.sp_GetPendingClaims;
GO

CREATE PROCEDURE ev.sp_GetPendingClaims
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.ClaimId, c.VIN, v.Model, c.DateDiscovered, c.Description, sc.Name AS ServiceCenter, u.FullName AS CreatedBy, c.StatusCode, c.CreatedAt
    FROM ev.WarrantyClaim c
    LEFT JOIN ev.Vehicles v ON c.VehicleId = v.VehicleId
    LEFT JOIN ev.ServiceCenters sc ON c.ServiceCenterId = sc.ServiceCenterId
    LEFT JOIN ev.Users u ON c.CreatedByUserId = u.UserId
    WHERE c.StatusCode = 'Pending'
    ORDER BY c.CreatedAt DESC;
END
GO

/********************************************
 * SP: Cập nhật trạng thái claim (dành cho EVM Staff)
 * sp_UpdateClaimStatus(@ClaimId, @NewStatus, @ChangedByUserId, @Note, @Cost)
 ********************************************/
IF OBJECT_ID('ev.sp_UpdateClaimStatus','P') IS NOT NULL
    DROP PROCEDURE ev.sp_UpdateClaimStatus;
GO

CREATE PROCEDURE ev.sp_UpdateClaimStatus
    @ClaimId INT,
    @NewStatus NVARCHAR(30),
    @ChangedByUserId INT,
    @Note NVARCHAR(2000) = NULL,
    @Cost DECIMAL(18,2) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRAN;

        DECLARE @OldStatus NVARCHAR(30);
        SELECT @OldStatus = StatusCode FROM ev.WarrantyClaim WHERE ClaimId = @ClaimId;

        IF @OldStatus IS NULL
        BEGIN
            RAISERROR('Claim not found',16,1);
            ROLLBACK TRAN;
            RETURN;
        END

        UPDATE ev.WarrantyClaim
        SET StatusCode = @NewStatus,
            Note = COALESCE(@Note, Note),
            Cost = COALESCE(@Cost, Cost)
        WHERE ClaimId = @ClaimId;

        INSERT INTO ev.ClaimStatusLog (ClaimId, OldStatus, NewStatus, ChangedByUserId, Comment)
        VALUES (@ClaimId, @OldStatus, @NewStatus, @ChangedByUserId, @Note);

        COMMIT TRAN;
        SELECT 1 AS Success;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRAN;
        THROW;
    END CATCH
END
GO

/********************************************
 * SP: Thêm phụ tùng đã dùng cho claim - sp_AddUsedPart
 ********************************************/
IF OBJECT_ID('ev.sp_AddUsedPart','P') IS NOT NULL
    DROP PROCEDURE ev.sp_AddUsedPart;
GO

CREATE PROCEDURE ev.sp_AddUsedPart
    @ClaimId INT,
    @PartId INT,
    @Quantity INT = 1,
    @PartCost DECIMAL(18,2) = NULL,
    @AddedByUserId INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRAN;

        -- Kiểm tra claim tồn tại
        IF NOT EXISTS (SELECT 1 FROM ev.WarrantyClaim WHERE ClaimId = @ClaimId)
        BEGIN
            RAISERROR('Claim not found',16,1);
            ROLLBACK TRAN;
            RETURN;
        END

        INSERT INTO ev.UsedParts (ClaimId, PartId, Quantity, PartCost)
        VALUES (@ClaimId, @PartId, @Quantity, @PartCost);

        -- Optionally update claim total cost
        UPDATE ev.WarrantyClaim
        SET TotalCost = COALESCE(TotalCost,0) + ISNULL(@PartCost,0) * @Quantity
        WHERE ClaimId = @ClaimId;

        COMMIT TRAN;
        SELECT SCOPE_IDENTITY() AS UsedPartId;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRAN;
        THROW;
    END CATCH
END
GO

/********************************************
 * SP: Mark claim in progress / complete (SC Technician)
 * sp_CompleteClaim(@ClaimId, @TechnicianId, @CompletionDate)
 * Khi hoàn tất sẽ cập nhật Status -> Completed
 ********************************************/
IF OBJECT_ID('ev.sp_CompleteClaim','P') IS NOT NULL
    DROP PROCEDURE ev.sp_CompleteClaim;
GO

CREATE PROCEDURE ev.sp_CompleteClaim
    @ClaimId INT,
    @TechnicianId INT,
    @CompletionDate DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRAN;

        IF NOT EXISTS (SELECT 1 FROM ev.WarrantyClaim WHERE ClaimId = @ClaimId)
        BEGIN
            RAISERROR('Claim not found',16,1);
            ROLLBACK TRAN;
            RETURN;
        END

        UPDATE ev.WarrantyClaim
        SET StatusCode = 'Completed',
            TechnicianId = @TechnicianId,
            CompletionDate = COALESCE(@CompletionDate, CONVERT(DATE, SYSUTCDATETIME()))
        WHERE ClaimId = @ClaimId;

        INSERT INTO ev.ClaimStatusLog (ClaimId, OldStatus, NewStatus, ChangedByUserId, Comment)
        VALUES (@ClaimId, 'InProgress', 'Completed', @TechnicianId, 'Work completed by technician');

        COMMIT TRAN;
        SELECT 1 AS Success;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRAN;
        THROW;
    END CATCH
END
GO

/********************************************
 * SP: Di chuyển claim hoàn tất vào WarrantyHistory và mark Closed
 * sp_ArchiveClaim(@ClaimId, @ArchiverUserId)
 ********************************************/
IF OBJECT_ID('ev.sp_ArchiveClaim','P') IS NOT NULL
    DROP PROCEDURE ev.sp_ArchiveClaim;
GO

CREATE PROCEDURE ev.sp_ArchiveClaim
    @ClaimId INT,
    @ArchiverUserId INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRAN;

        -- Lấy dữ liệu claim
        DECLARE @VIN NVARCHAR(50),
                @VehicleId INT,
                @ServiceCenterId INT,
                @TechnicianId INT,
                @CompletionDate DATE,
                @TotalCost DECIMAL(18,2),
                @Note NVARCHAR(2000);

        SELECT @VIN = VIN, @VehicleId = VehicleId, @ServiceCenterId = ServiceCenterId,
               @TechnicianId = TechnicianId, @CompletionDate = CompletionDate,
               @TotalCost = TotalCost, @Note = Note
        FROM ev.WarrantyClaim WHERE ClaimId = @ClaimId;

        IF @VIN IS NULL
        BEGIN
            RAISERROR('Claim not found',16,1);
            ROLLBACK TRAN;
            RETURN;
        END

        INSERT INTO ev.WarrantyHistory (ClaimId, VIN, VehicleId, ServiceCenterId, CompletedByUserId, CompletionDate, TotalCost, Note)
        VALUES (@ClaimId, @VIN, @VehicleId, @ServiceCenterId, @TechnicianId, @CompletionDate, @TotalCost, @Note);

        -- Update claim status to Closed
        UPDATE ev.WarrantyClaim
        SET StatusCode = 'Closed'
        WHERE ClaimId = @ClaimId;

        INSERT INTO ev.ClaimStatusLog (ClaimId, OldStatus, NewStatus, ChangedByUserId, Comment)
        VALUES (@ClaimId, 'Completed', 'Closed', @ArchiverUserId, 'Archived to history');

        COMMIT TRAN;
        SELECT 1 AS Success;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRAN;
        THROW;
    END CATCH
END
GO