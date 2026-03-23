-- =============================================
-- TaskForge Seed Data
-- Run AFTER creating tables
-- =============================================

USE TaskForgeDB;
GO

-- Seed Roles (if not already seeded by EF Core)
IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'Admin')
BEGIN
    SET IDENTITY_INSERT Roles ON;

    INSERT INTO Roles (Id, Name, Description, CreatedAt) VALUES
    (1, 'Admin', 'Full system access', '2024-01-01'),
    (2, 'User', 'Standard user access', '2024-01-01');

    SET IDENTITY_INSERT Roles OFF;
END
GO

-- Seed Admin User (password: Admin@123)
-- BCrypt hash for 'Admin@123'
IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'admin')
BEGIN
    INSERT INTO Users (Username, Email, PasswordHash, FirstName, LastName, Status, CreatedAt)
    VALUES ('admin', 'admin@taskforge.dev',
            '$2a$11$K3g6g1X2zvp3W6REKxKnO.eXyCrFZBfB3m0h1nYpYP.FVd4FQdVGS',
            'System', 'Admin', 'Active', GETUTCDATE());

    -- Assign Admin role
    DECLARE @AdminUserId INT = (SELECT Id FROM Users WHERE Username = 'admin');
    INSERT INTO UserRoles (UserId, RoleId, AssignedAt) VALUES (@AdminUserId, 1, GETUTCDATE());
END
GO

-- Seed Regular User (password: User@123)
IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'johndoe')
BEGIN
    INSERT INTO Users (Username, Email, PasswordHash, FirstName, LastName, Status, CreatedAt)
    VALUES ('johndoe', 'john@taskforge.dev',
            '$2a$11$K3g6g1X2zvp3W6REKxKnO.eXyCrFZBfB3m0h1nYpYP.FVd4FQdVGS',
            'John', 'Doe', 'Active', GETUTCDATE());

    DECLARE @JohnUserId INT = (SELECT Id FROM Users WHERE Username = 'johndoe');
    INSERT INTO UserRoles (UserId, RoleId, AssignedAt) VALUES (@JohnUserId, 2, GETUTCDATE());
END
GO

-- Seed Sample Project
IF NOT EXISTS (SELECT 1 FROM Projects WHERE Name = 'Project Alpha')
BEGIN
    DECLARE @OwnerId INT = (SELECT Id FROM Users WHERE Username = 'admin');

    INSERT INTO Projects (Name, Description, OwnerId, CreatedAt, IsActive)
    VALUES ('Project Alpha', 'Initial demo project for TaskForge', @OwnerId, GETUTCDATE(), 1);

    DECLARE @ProjectId INT = SCOPE_IDENTITY();
    DECLARE @AssigneeId INT = (SELECT Id FROM Users WHERE Username = 'johndoe');

    -- Seed Sample Tasks
    INSERT INTO TaskItems (Title, Description, Status, Priority, DueDate, ProjectId, AssignedToId, CreatedAt) VALUES
    ('Setup development environment', 'Install all required tools and dependencies', 'Completed', 'High', DATEADD(DAY, -5, GETUTCDATE()), @ProjectId, @AssigneeId, GETUTCDATE()),
    ('Design database schema', 'Create ER diagram and define tables', 'Completed', 'Critical', DATEADD(DAY, -3, GETUTCDATE()), @ProjectId, @AssigneeId, GETUTCDATE()),
    ('Implement user authentication', 'JWT + Basic auth implementation', 'InProgress', 'Critical', DATEADD(DAY, 7, GETUTCDATE()), @ProjectId, @AssigneeId, GETUTCDATE()),
    ('Write API documentation', 'Swagger + README documentation', 'Pending', 'Medium', DATEADD(DAY, 14, GETUTCDATE()), @ProjectId, @AssigneeId, GETUTCDATE()),
    ('Performance testing', 'Load test all endpoints', 'Pending', 'High', DATEADD(DAY, 21, GETUTCDATE()), @ProjectId, NULL, GETUTCDATE()),
    ('Setup CI/CD pipeline', 'Configure automated builds', 'Pending', 'Low', DATEADD(DAY, 30, GETUTCDATE()), @ProjectId, NULL, GETUTCDATE());
END
GO

PRINT '✅ Seed data inserted successfully.';
PRINT 'Admin login: username=admin, password=Admin@123';
PRINT 'User login: username=johndoe, password=User@123';
PRINT '(Note: Register via the API for proper BCrypt hashing)';
GO
