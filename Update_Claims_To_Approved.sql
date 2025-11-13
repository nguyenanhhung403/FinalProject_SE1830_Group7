-- =============================================
-- UPDATE CLAIMS TO APPROVED STATUS FOR TESTING
-- =============================================

USE EVWarrantyManagement;
GO

-- Update some claims to Approved status
-- This will allow testing the "Revert to Pending" functionality

-- Update claim #18 to Approved (if it exists)
UPDATE ev.WarrantyClaim
SET StatusCode = 'Approved',
    Note = 'Updated to Approved for testing'
WHERE ClaimId = 18 AND StatusCode != 'Approved';
GO

-- Update first 3 pending claims to Approved
UPDATE TOP (3) ev.WarrantyClaim
SET StatusCode = 'Approved',
    Note = 'Updated to Approved for testing'
WHERE StatusCode = 'Pending';
GO

-- Log the status changes
INSERT INTO ev.ClaimStatusLog (ClaimId, OldStatus, NewStatus, ChangedByUserId, ChangedAt, Comment)
SELECT 
    ClaimId,
    'Pending' AS OldStatus,
    'Approved' AS NewStatus,
    5 AS ChangedByUserId, -- admin1
    SYSUTCDATETIME() AS ChangedAt,
    'Updated to Approved for testing' AS Comment
FROM ev.WarrantyClaim
WHERE StatusCode = 'Approved' 
    AND Note = 'Updated to Approved for testing'
    AND ClaimId NOT IN (
        SELECT ClaimId FROM ev.ClaimStatusLog 
        WHERE NewStatus = 'Approved' 
        AND Comment = 'Updated to Approved for testing'
    );
GO

PRINT '=============================================';
PRINT 'Claims updated to Approved status!';
PRINT 'You can now test the "Revert to Pending" button.';
PRINT '=============================================';
GO

