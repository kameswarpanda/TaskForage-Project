-- =============================================
-- TaskForge Stored Procedures
-- Demonstrates: CRUD SPs, JOINs (Inner, Left),
-- GROUP BY, CTE, Window Functions, Temp Tables
-- =============================================

USE TaskForgeDB;
GO

-- =============================================
-- SP 1: Get Task Summary Report
-- Uses: GROUP BY, CASE expression, aggregation
-- =============================================
CREATE OR ALTER PROCEDURE sp_GetTaskSummaryReport
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        COUNT(*) AS TotalTasks,
        SUM(CASE WHEN Status = 'Pending' THEN 1 ELSE 0 END) AS PendingTasks,
        SUM(CASE WHEN Status = 'InProgress' THEN 1 ELSE 0 END) AS InProgressTasks,
        SUM(CASE WHEN Status = 'Completed' THEN 1 ELSE 0 END) AS CompletedTasks,
        SUM(CASE WHEN DueDate < GETUTCDATE() AND Status NOT IN ('Completed', 'Cancelled') THEN 1 ELSE 0 END) AS OverdueTasks
    FROM TaskItems;
END
GO

-- =============================================
-- SP 2: Get Project Task Reports
-- Uses: INNER JOIN, GROUP BY, calculated fields
-- =============================================
CREATE OR ALTER PROCEDURE sp_GetProjectTaskReports
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.Id AS ProjectId,
        p.Name AS ProjectName,
        COUNT(t.Id) AS TotalTasks,
        SUM(CASE WHEN t.Status = 'Completed' THEN 1 ELSE 0 END) AS CompletedTasks,
        CASE
            WHEN COUNT(t.Id) = 0 THEN 0.0
            ELSE ROUND(
                CAST(SUM(CASE WHEN t.Status = 'Completed' THEN 1 ELSE 0 END) AS FLOAT) /
                CAST(COUNT(t.Id) AS FLOAT) * 100, 2)
        END AS CompletionPercentage
    FROM Projects p
    INNER JOIN TaskItems t ON p.Id = t.ProjectId
    WHERE p.IsActive = 1
    GROUP BY p.Id, p.Name
    ORDER BY CompletionPercentage DESC;
END
GO

-- =============================================
-- SP 3: Get User Productivity
-- Uses: CTE, LEFT JOIN, Window Functions (ROW_NUMBER, RANK)
-- =============================================
CREATE OR ALTER PROCEDURE sp_GetUserProductivity
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH UserTaskCTE AS (
        -- CTE: Calculate task counts per user
        SELECT
            u.Id AS UserId,
            u.Username,
            COUNT(t.Id) AS AssignedTasks,
            SUM(CASE WHEN t.Status = 'Completed' THEN 1 ELSE 0 END) AS CompletedTasks
        FROM Users u
        LEFT JOIN TaskItems t ON u.Id = t.AssignedToId
        WHERE u.Status = 'Active'
        GROUP BY u.Id, u.Username
    )
    SELECT
        UserId,
        Username,
        AssignedTasks,
        CompletedTasks,
        -- Window function: RANK by completed tasks
        RANK() OVER (ORDER BY CompletedTasks DESC) AS [Rank]
    FROM UserTaskCTE
    ORDER BY [Rank];
END
GO

-- =============================================
-- SP 4: Get Tasks With Details (Complex JOIN)
-- Uses: Multiple JOINs, LEFT JOIN for nullable FK
-- =============================================
CREATE OR ALTER PROCEDURE sp_GetTasksWithDetails
    @ProjectId INT = NULL,
    @Status NVARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        t.Id,
        t.Title,
        t.Description,
        t.Status,
        t.Priority,
        t.DueDate,
        t.CreatedAt,
        t.CompletedAt,
        p.Name AS ProjectName,
        CONCAT(u.FirstName, ' ', u.LastName) AS AssignedToName,
        ROW_NUMBER() OVER (PARTITION BY t.ProjectId ORDER BY t.CreatedAt DESC) AS TaskRowNum
    FROM TaskItems t
    INNER JOIN Projects p ON t.ProjectId = p.Id
    LEFT JOIN Users u ON t.AssignedToId = u.Id
    WHERE (@ProjectId IS NULL OR t.ProjectId = @ProjectId)
      AND (@Status IS NULL OR t.Status = @Status)
    ORDER BY t.Priority DESC, t.DueDate ASC;
END
GO

-- =============================================
-- SP 5: Get Overdue Tasks Using Temp Tables
-- Uses: Temp Tables, INSERT INTO, JOIN
-- =============================================
CREATE OR ALTER PROCEDURE sp_GetOverdueTasks
AS
BEGIN
    SET NOCOUNT ON;

    -- Create temp table for overdue analysis
    CREATE TABLE #OverdueTasks (
        TaskId INT,
        Title NVARCHAR(200),
        ProjectName NVARCHAR(100),
        AssignedTo NVARCHAR(100),
        DueDate DATETIME2,
        DaysOverdue INT
    );

    -- Populate temp table
    INSERT INTO #OverdueTasks (TaskId, Title, ProjectName, AssignedTo, DueDate, DaysOverdue)
    SELECT
        t.Id,
        t.Title,
        p.Name,
        CONCAT(u.FirstName, ' ', u.LastName),
        t.DueDate,
        DATEDIFF(DAY, t.DueDate, GETUTCDATE())
    FROM TaskItems t
    INNER JOIN Projects p ON t.ProjectId = p.Id
    LEFT JOIN Users u ON t.AssignedToId = u.Id
    WHERE t.DueDate < GETUTCDATE()
      AND t.Status NOT IN ('Completed', 'Cancelled');

    -- Return results from temp table
    SELECT * FROM #OverdueTasks
    ORDER BY DaysOverdue DESC;

    -- Cleanup
    DROP TABLE #OverdueTasks;
END
GO

-- =============================================
-- SP 6: CRUD - Create Task
-- =============================================
CREATE OR ALTER PROCEDURE sp_CreateTask
    @Title NVARCHAR(200),
    @Description NVARCHAR(2000) = NULL,
    @Priority NVARCHAR(20) = 'Medium',
    @DueDate DATETIME2 = NULL,
    @ProjectId INT,
    @AssignedToId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO TaskItems (Title, Description, Priority, Status, DueDate, ProjectId, AssignedToId, CreatedAt)
    VALUES (@Title, @Description, @Priority, 'Pending', @DueDate, @ProjectId, @AssignedToId, GETUTCDATE());

    SELECT SCOPE_IDENTITY() AS NewTaskId;
END
GO

-- =============================================
-- SP 7: CRUD - Update Task Status
-- =============================================
CREATE OR ALTER PROCEDURE sp_UpdateTaskStatus
    @TaskId INT,
    @Status NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE TaskItems
    SET Status = @Status,
        UpdatedAt = GETUTCDATE(),
        CompletedAt = CASE WHEN @Status = 'Completed' THEN GETUTCDATE() ELSE CompletedAt END
    WHERE Id = @TaskId;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- =============================================
-- SP 8: Dashboard Stats with Multiple CTEs
-- Uses: Multiple CTEs, UNION-style approach
-- =============================================
CREATE OR ALTER PROCEDURE sp_GetDashboardStats
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH TaskStats AS (
        SELECT
            COUNT(*) AS TotalTasks,
            SUM(CASE WHEN Status = 'Completed' THEN 1 ELSE 0 END) AS CompletedTasks
        FROM TaskItems
    ),
    ProjectStats AS (
        SELECT
            COUNT(*) AS TotalProjects,
            SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) AS ActiveProjects
        FROM Projects
    ),
    UserStats AS (
        SELECT
            COUNT(*) AS TotalUsers,
            SUM(CASE WHEN Status = 'Active' THEN 1 ELSE 0 END) AS ActiveUsers
        FROM Users
    )
    SELECT
        ts.TotalTasks, ts.CompletedTasks,
        ps.TotalProjects, ps.ActiveProjects,
        us.TotalUsers, us.ActiveUsers
    FROM TaskStats ts
    CROSS JOIN ProjectStats ps
    CROSS JOIN UserStats us;
END
GO

PRINT '✅ All stored procedures created successfully.';
GO
