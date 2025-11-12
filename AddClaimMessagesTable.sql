-- Script to add ClaimMessages table for SignalR chat functionality
-- Execute this in SQL Server Management Studio or Azure Data Studio

USE [EVWarrantyManagement]
GO

-- Check if table already exists, drop if it does (for re-running script)
IF OBJECT_ID('[ev].[ClaimMessages]', 'U') IS NOT NULL
    DROP TABLE [ev].[ClaimMessages]
GO

-- Create ClaimMessages table
CREATE TABLE [ev].[ClaimMessages](
    [MessageId] [int] IDENTITY(1,1) NOT NULL,
    [ClaimId] [int] NOT NULL,
    [UserId] [int] NOT NULL,
    [Message] [nvarchar](2000) NOT NULL,
    [Timestamp] [datetime2](7) NOT NULL,
    [IsRead] [bit] NOT NULL,
    [ReadAt] [datetime2](7) NULL,
    CONSTRAINT [PK__ClaimMes__C87C0C9C1234ABCD] PRIMARY KEY CLUSTERED ([MessageId] ASC)
        WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

-- Add default constraints
ALTER TABLE [ev].[ClaimMessages] ADD CONSTRAINT [DF_ClaimMessages_Timestamp] DEFAULT (SYSUTCDATETIME()) FOR [Timestamp]
GO

ALTER TABLE [ev].[ClaimMessages] ADD CONSTRAINT [DF_ClaimMessages_IsRead] DEFAULT ((0)) FOR [IsRead]
GO

-- Add foreign key to WarrantyClaim table (singular, not plural)
ALTER TABLE [ev].[ClaimMessages] WITH CHECK ADD CONSTRAINT [FK_ClaimMessage_WarrantyClaim]
    FOREIGN KEY([ClaimId])
    REFERENCES [ev].[WarrantyClaim] ([ClaimId])
    ON DELETE CASCADE
GO

ALTER TABLE [ev].[ClaimMessages] CHECK CONSTRAINT [FK_ClaimMessage_WarrantyClaim]
GO

-- Add foreign key to Users table
ALTER TABLE [ev].[ClaimMessages] WITH CHECK ADD CONSTRAINT [FK_ClaimMessage_User]
    FOREIGN KEY([UserId])
    REFERENCES [ev].[Users] ([UserId])
GO

ALTER TABLE [ev].[ClaimMessages] CHECK CONSTRAINT [FK_ClaimMessage_User]
GO

-- Create indexes for better query performance
CREATE NONCLUSTERED INDEX [IX_ClaimMessage_ClaimId] ON [ev].[ClaimMessages]
(
    [ClaimId] ASC
) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_ClaimMessage_Timestamp] ON [ev].[ClaimMessages]
(
    [Timestamp] ASC
) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

PRINT 'ClaimMessages table created successfully!'
GO
