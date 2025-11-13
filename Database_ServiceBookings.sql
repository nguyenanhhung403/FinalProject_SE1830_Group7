-- Script to create ServiceBookings infrastructure (DB-first friendly)
-- Includes main ServiceBookings table and status logs without altering existing tables

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'ev')
BEGIN
    PRINT 'Schema ev does not exist. Please create it before running this script.';
    RETURN;
END

IF OBJECT_ID('ev.ServiceBookings', 'U') IS NULL
BEGIN
    CREATE TABLE ev.ServiceBookings
    (
        ServiceBookingId        INT             IDENTITY(1,1) PRIMARY KEY,
        CustomerId              INT             NOT NULL,
        VehicleId               INT             NOT NULL,
        ServiceCenterId         INT             NOT NULL,
        AssignedTechnicianId    INT             NULL,
        ApprovedByUserId        INT             NULL,
        CancelledByUserId       INT             NULL,
        ServiceType             NVARCHAR(100)   NOT NULL,
        Status                  NVARCHAR(30)    NOT NULL DEFAULT ('Pending'),
        PreferredStart          DATETIME2(0)    NOT NULL,
        PreferredEnd            DATETIME2(0)    NULL,
        ConfirmedStart          DATETIME2(0)    NULL,
        ConfirmedEnd            DATETIME2(0)    NULL,
        CustomerNote            NVARCHAR(1000)  NULL,
        InternalNote            NVARCHAR(1000)  NULL,
        RejectionReason         NVARCHAR(1000)  NULL,
        EstimatedDurationMinutes INT            NOT NULL DEFAULT (60),
        CreatedAt               DATETIME2(0)    NOT NULL DEFAULT (SYSUTCDATETIME()),
        UpdatedAt               DATETIME2(0)    NOT NULL DEFAULT (SYSUTCDATETIME()),
        ApprovedAt              DATETIME2(0)    NULL,
        CompletedAt             DATETIME2(0)    NULL,
        CancelledAt             DATETIME2(0)    NULL,

        CONSTRAINT FK_ServiceBookings_Customers FOREIGN KEY (CustomerId) REFERENCES ev.Customers(CustomerId),
        CONSTRAINT FK_ServiceBookings_Vehicles FOREIGN KEY (VehicleId) REFERENCES ev.Vehicles(VehicleId),
        CONSTRAINT FK_ServiceBookings_ServiceCenters FOREIGN KEY (ServiceCenterId) REFERENCES ev.ServiceCenters(ServiceCenterId),
        CONSTRAINT FK_ServiceBookings_AssignedTechnician FOREIGN KEY (AssignedTechnicianId) REFERENCES ev.Users(UserId),
        CONSTRAINT FK_ServiceBookings_ApprovedBy FOREIGN KEY (ApprovedByUserId) REFERENCES ev.Users(UserId),
        CONSTRAINT FK_ServiceBookings_CancelledBy FOREIGN KEY (CancelledByUserId) REFERENCES ev.Users(UserId)
    );

    CREATE INDEX IX_ServiceBookings_Status ON ev.ServiceBookings(Status);
    CREATE INDEX IX_ServiceBookings_ServiceCenter_Date ON ev.ServiceBookings(ServiceCenterId, PreferredStart);
    CREATE INDEX IX_ServiceBookings_AssignedTechnician ON ev.ServiceBookings(AssignedTechnicianId, PreferredStart) WHERE AssignedTechnicianId IS NOT NULL;
END
ELSE
BEGIN
    PRINT 'Table ev.ServiceBookings already exists.';
END

IF OBJECT_ID('ev.ServiceBookingStatusLogs', 'U') IS NULL
BEGIN
    CREATE TABLE ev.ServiceBookingStatusLogs
    (
        ServiceBookingStatusLogId INT             IDENTITY(1,1) PRIMARY KEY,
        ServiceBookingId          INT             NOT NULL,
        OldStatus                 NVARCHAR(30)    NULL,
        NewStatus                 NVARCHAR(30)    NOT NULL,
        Note                      NVARCHAR(1000)  NULL,
        ChangedByUserId           INT             NULL,
        ChangedAt                 DATETIME2(0)    NOT NULL DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT FK_ServiceBookingStatusLogs_ServiceBookings FOREIGN KEY (ServiceBookingId) REFERENCES ev.ServiceBookings(ServiceBookingId) ON DELETE CASCADE,
        CONSTRAINT FK_ServiceBookingStatusLogs_Users FOREIGN KEY (ChangedByUserId) REFERENCES ev.Users(UserId)
    );

    CREATE INDEX IX_ServiceBookingStatusLogs_Booking ON ev.ServiceBookingStatusLogs(ServiceBookingId);
    CREATE INDEX IX_ServiceBookingStatusLogs_ChangedAt ON ev.ServiceBookingStatusLogs(ChangedAt);
END
ELSE
BEGIN
    PRINT 'Table ev.ServiceBookingStatusLogs already exists.';
END
