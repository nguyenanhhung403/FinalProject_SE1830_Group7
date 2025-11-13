-- Script to create ServiceBookingParts table for tracking parts used in service bookings
-- Safe to run multiple times

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'ev')
BEGIN
    PRINT 'Schema ev does not exist. Please create it before running this script.';
    RETURN;
END

IF OBJECT_ID('ev.ServiceBookingParts', 'U') IS NULL
BEGIN
    CREATE TABLE ev.ServiceBookingParts
    (
        ServiceBookingPartId INT IDENTITY(1,1) PRIMARY KEY,
        ServiceBookingId     INT             NOT NULL,
        PartId               INT             NOT NULL,
        Quantity             INT             NOT NULL DEFAULT (1),
        PartCost             DECIMAL(18,2)   NULL,
        Note                 NVARCHAR(500)   NULL,
        CreatedByUserId      INT             NULL,
        CreatedAt            DATETIME2(0)    NOT NULL DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT FK_ServiceBookingParts_ServiceBookings FOREIGN KEY (ServiceBookingId)
            REFERENCES ev.ServiceBookings(ServiceBookingId) ON DELETE CASCADE,
        CONSTRAINT FK_ServiceBookingParts_Parts FOREIGN KEY (PartId)
            REFERENCES ev.Parts(PartId),
        CONSTRAINT FK_ServiceBookingParts_CreatedBy FOREIGN KEY (CreatedByUserId)
            REFERENCES ev.Users(UserId)
    );

    CREATE INDEX IX_ServiceBookingParts_Booking ON ev.ServiceBookingParts(ServiceBookingId);
    CREATE INDEX IX_ServiceBookingParts_Part ON ev.ServiceBookingParts(PartId);
END
ELSE
BEGIN
    PRINT 'Table ev.ServiceBookingParts already exists.';
END

