-- =============================================
-- TaskForge Triggers
-- Demonstrates: AFTER triggers for audit logging
-- =============================================

USE TaskForgeDB;
GO

-- =============================================
-- Trigger: Audit INSERT on TaskItems
-- =============================================
CREATE OR ALTER TRIGGER trg_TaskItems_Insert
ON TaskItems
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO AuditLogs (EntityName, EntityId, Action, NewValues, PerformedAt)
    SELECT
        'TaskItem',
        i.Id,
        'INSERT',
        CONCAT(
            '{"Title":"', i.Title,
            '","Status":"', i.Status,
            '","Priority":"', i.Priority,
            '","ProjectId":', i.ProjectId,
            ',"AssignedToId":', ISNULL(CAST(i.AssignedToId AS NVARCHAR(10)), 'null'),
            '}'
        ),
        GETUTCDATE()
    FROM inserted i;
END
GO

-- =============================================
-- Trigger: Audit UPDATE on TaskItems
-- =============================================
CREATE OR ALTER TRIGGER trg_TaskItems_Update
ON TaskItems
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO AuditLogs (EntityName, EntityId, Action, OldValues, NewValues, PerformedAt)
    SELECT
        'TaskItem',
        i.Id,
        'UPDATE',
        -- Old values
        CONCAT(
            '{"Title":"', d.Title,
            '","Status":"', d.Status,
            '","Priority":"', d.Priority,
            '","AssignedToId":', ISNULL(CAST(d.AssignedToId AS NVARCHAR(10)), 'null'),
            '}'
        ),
        -- New values
        CONCAT(
            '{"Title":"', i.Title,
            '","Status":"', i.Status,
            '","Priority":"', i.Priority,
            '","AssignedToId":', ISNULL(CAST(i.AssignedToId AS NVARCHAR(10)), 'null'),
            '}'
        ),
        GETUTCDATE()
    FROM inserted i
    INNER JOIN deleted d ON i.Id = d.Id;
END
GO

-- =============================================
-- Trigger: Audit DELETE on TaskItems
-- =============================================
CREATE OR ALTER TRIGGER trg_TaskItems_Delete
ON TaskItems
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO AuditLogs (EntityName, EntityId, Action, OldValues, PerformedAt)
    SELECT
        'TaskItem',
        d.Id,
        'DELETE',
        CONCAT(
            '{"Title":"', d.Title,
            '","Status":"', d.Status,
            '","ProjectId":', d.ProjectId,
            '}'
        ),
        GETUTCDATE()
    FROM deleted d;
END
GO

PRINT '✅ All triggers created successfully.';
GO
