-- =============================================
-- TaskForge Database Creation & Tables
-- Run this script first on your SQL Server
-- =============================================

-- Create Database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'TaskForgeDB')
BEGIN
    CREATE DATABASE TaskForgeDB;
END
GO

USE TaskForgeDB;
GO

-- =============================================
-- TABLES
-- =============================================

-- Users Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        Username        NVARCHAR(50) NOT NULL,
        Email           NVARCHAR(100) NOT NULL,
        PasswordHash    NVARCHAR(256) NOT NULL,
        FirstName       NVARCHAR(50) NOT NULL,
        LastName        NVARCHAR(50) NOT NULL,
        Status          NVARCHAR(20) NOT NULL DEFAULT 'Active',
        CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt       DATETIME2 NULL,

        CONSTRAINT UQ_Users_Username UNIQUE (Username),
        CONSTRAINT UQ_Users_Email UNIQUE (Email)
    );
END
GO

-- Roles Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Roles')
BEGIN
    CREATE TABLE Roles (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        Name            NVARCHAR(50) NOT NULL,
        Description     NVARCHAR(200) NULL,
        CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT UQ_Roles_Name UNIQUE (Name)
    );
END
GO

-- UserRoles (Many-to-Many)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserRoles')
BEGIN
    CREATE TABLE UserRoles (
        UserId          INT NOT NULL,
        RoleId          INT NOT NULL,
        AssignedAt      DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        PRIMARY KEY (UserId, RoleId),
        FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
        FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE
    );
END
GO

-- Projects Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Projects')
BEGIN
    CREATE TABLE Projects (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        Name            NVARCHAR(100) NOT NULL,
        Description     NVARCHAR(500) NULL,
        OwnerId         INT NOT NULL,
        CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt       DATETIME2 NULL,
        IsActive        BIT NOT NULL DEFAULT 1,

        FOREIGN KEY (OwnerId) REFERENCES Users(Id) ON DELETE NO ACTION
    );
END
GO

-- TaskItems Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TaskItems')
BEGIN
    CREATE TABLE TaskItems (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        Title           NVARCHAR(200) NOT NULL,
        Description     NVARCHAR(2000) NULL,
        Status          NVARCHAR(20) NOT NULL DEFAULT 'Pending',
        Priority        NVARCHAR(20) NOT NULL DEFAULT 'Medium',
        DueDate         DATETIME2 NULL,
        ProjectId       INT NOT NULL,
        AssignedToId    INT NULL,
        CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt       DATETIME2 NULL,
        CompletedAt     DATETIME2 NULL,

        FOREIGN KEY (ProjectId) REFERENCES Projects(Id) ON DELETE CASCADE,
        FOREIGN KEY (AssignedToId) REFERENCES Users(Id) ON DELETE SET NULL
    );
END
GO

-- AuditLogs Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLogs')
BEGIN
    CREATE TABLE AuditLogs (
        Id              BIGINT IDENTITY(1,1) PRIMARY KEY,
        EntityName      NVARCHAR(100) NOT NULL,
        EntityId        INT NOT NULL,
        Action          NVARCHAR(20) NOT NULL,  -- INSERT, UPDATE, DELETE
        OldValues       NVARCHAR(MAX) NULL,
        NewValues       NVARCHAR(MAX) NULL,
        PerformedBy     NVARCHAR(100) NULL,
        PerformedAt     DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
END
GO

-- RefreshTokens Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RefreshTokens')
BEGIN
    CREATE TABLE RefreshTokens (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        Token           NVARCHAR(256) NOT NULL,
        UserId          INT NOT NULL,
        ExpiresAt       DATETIME2 NOT NULL,
        CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        RevokedAt       DATETIME2 NULL,

        CONSTRAINT UQ_RefreshTokens_Token UNIQUE (Token),
        FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
    );
END
GO

PRINT '✅ All tables created successfully.';
GO
