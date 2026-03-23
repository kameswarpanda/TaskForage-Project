-- =============================================
-- TaskForge Indexes for Query Optimization
-- Demonstrates: Composite indexes, covering indexes,
-- filtered indexes, and indexing strategies
-- =============================================

USE TaskForgeDB;
GO

-- =============================================
-- TaskItems Indexes
-- =============================================

-- Composite index: Frequently queried by ProjectId + Status (used in filtering)
CREATE NONCLUSTERED INDEX IX_TaskItems_ProjectId_Status
ON TaskItems (ProjectId, Status)
INCLUDE (Title, Priority, DueDate, AssignedToId, CreatedAt)
WITH (ONLINE = OFF);
GO

-- Index for assignment-based queries
CREATE NONCLUSTERED INDEX IX_TaskItems_AssignedToId
ON TaskItems (AssignedToId)
INCLUDE (Title, Status, Priority, ProjectId, DueDate)
WHERE AssignedToId IS NOT NULL;
GO

-- Index for due date queries (overdue tasks)
CREATE NONCLUSTERED INDEX IX_TaskItems_DueDate
ON TaskItems (DueDate)
INCLUDE (Title, Status, ProjectId, AssignedToId)
WHERE DueDate IS NOT NULL;
GO

-- =============================================
-- Projects Indexes
-- =============================================

-- Index for owner-based queries
CREATE NONCLUSTERED INDEX IX_Projects_OwnerId
ON Projects (OwnerId)
INCLUDE (Name, IsActive, CreatedAt);
GO

-- =============================================
-- AuditLogs Indexes (for querying audit trail)
-- =============================================

-- Composite index for entity-based audit queries
CREATE NONCLUSTERED INDEX IX_AuditLogs_EntityName_EntityId
ON AuditLogs (EntityName, EntityId)
INCLUDE (Action, PerformedAt);
GO

-- Index for time-based audit queries
CREATE NONCLUSTERED INDEX IX_AuditLogs_PerformedAt
ON AuditLogs (PerformedAt DESC)
INCLUDE (EntityName, EntityId, Action);
GO

-- =============================================
-- RefreshTokens Indexes
-- =============================================

-- Index for token lookup
CREATE NONCLUSTERED INDEX IX_RefreshTokens_UserId
ON RefreshTokens (UserId)
INCLUDE (Token, ExpiresAt, RevokedAt);
GO

PRINT '✅ All indexes created successfully.';
GO

-- =============================================
-- Verify index usage with execution plan hint:
--
-- SET STATISTICS IO ON;
-- SET STATISTICS TIME ON;
--
-- SELECT * FROM TaskItems
-- WHERE ProjectId = 1 AND Status = 'Pending';
--
-- -- Check execution plan:
-- SET SHOWPLAN_XML ON;
-- GO
-- SELECT * FROM TaskItems WHERE ProjectId = 1;
-- GO
-- SET SHOWPLAN_XML OFF;
-- GO
-- =============================================
