-- Metadata Engine Auto-Generated Data Migration Script
-- Client Profile: Enterprise Logistics Corp
-- Timestamp: 2026-10-16 15:04:38

USE [DimPersonnelRegistry]; -- Inferred Database Context
GO

PRINT 'Initializing secure transaction block...';
BEGIN TRANSACTION;

INSERT INTO DimPersonnelRegistry ([EmployeeName], [CleanEmail], [DOB], [AnnualSalary], [VehicleNumber]) VALUES
('Aryan Patel', 'aryan.patel@logistics.in', '06-15-1995 12:00:00 AM', 14500, 'GJ03AA1234'),
('Rohan Sharma', 'rohan.sharma@corp.co.in', '11-23-1992 12:00:00 AM', 45000.50, 'MH12BB5678'),
('Priya Sharma', 'priya.s@global.com', '09-12-1997 12:00:00 AM', 62000, '');

COMMIT TRANSACTION;
PRINT 'Data installation completed successfully.';
GO
