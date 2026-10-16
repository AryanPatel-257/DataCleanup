-- Metadata Engine Auto-Generated Data Migration Script
-- Client Profile: Delta Global Solutions
-- Timestamp: 2026-10-16 14:52:35

USE [FactSalesSummary]; -- Inferred Database Context
GO

PRINT 'Initializing secure transaction block...';
BEGIN TRANSACTION;

INSERT INTO FactSalesSummary ([CleanEmail], [InvoiceAmount]) VALUES
('aryan@test.com', 1500);

COMMIT TRANSACTION;
PRINT 'Data installation completed successfully.';
GO
