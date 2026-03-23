using Microsoft.Extensions.Logging;
using TaskForge.Application.DTOs;
using TaskForge.Application.Interfaces;
using TaskForge.Domain.Interfaces;

namespace TaskForge.Infrastructure.Services;

/// <summary>
/// Reporting service that uses ADO.NET stored procedures for complex queries.
/// Demonstrates: SP execution, JOINs, GROUP BY, CTEs, window functions, temp tables.
/// </summary>
public class ReportService : IReportService
{
    private readonly IStoredProcedureExecutor _spExecutor;
    private readonly ICacheService _cache;
    private readonly ILogger<ReportService> _logger;

    public ReportService(IStoredProcedureExecutor spExecutor, ICacheService cache, ILogger<ReportService> logger)
    {
        _spExecutor = spExecutor;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Calls sp_GetTaskSummaryReport stored procedure.
    /// </summary>
    public async Task<TaskSummaryReport> GetTaskSummaryAsync()
    {
        var cacheKey = "report_task_summary";
        var cached = _cache.Get<TaskSummaryReport>(cacheKey);
        if (cached != null) return cached;

        try
        {
            var results = await _spExecutor.ExecuteQueryAsync<TaskSummaryReport>("sp_GetTaskSummaryReport");
            var report = results.FirstOrDefault() ?? new TaskSummaryReport();
            _cache.Set(cacheKey, report, TimeSpan.FromMinutes(2));
            return report;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Stored procedure sp_GetTaskSummaryReport failed, returning empty report");
            return new TaskSummaryReport();
        }
    }

    /// <summary>
    /// Calls sp_GetProjectTaskReports stored procedure (uses JOINs + GROUP BY).
    /// </summary>
    public async Task<IEnumerable<ProjectTaskReport>> GetProjectTaskReportsAsync()
    {
        var cacheKey = "report_project_tasks";
        var cached = _cache.Get<IEnumerable<ProjectTaskReport>>(cacheKey);
        if (cached != null) return cached;

        try
        {
            var results = await _spExecutor.ExecuteQueryAsync<ProjectTaskReport>("sp_GetProjectTaskReports");
            _cache.Set(cacheKey, results, TimeSpan.FromMinutes(2));
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Stored procedure sp_GetProjectTaskReports failed, returning empty list");
            return Enumerable.Empty<ProjectTaskReport>();
        }
    }

    /// <summary>
    /// Calls sp_GetUserProductivity stored procedure (uses CTE + Window Functions: ROW_NUMBER, RANK).
    /// </summary>
    public async Task<IEnumerable<UserProductivityReport>> GetUserProductivityAsync()
    {
        var cacheKey = "report_user_productivity";
        var cached = _cache.Get<IEnumerable<UserProductivityReport>>(cacheKey);
        if (cached != null) return cached;

        try
        {
            var results = await _spExecutor.ExecuteQueryAsync<UserProductivityReport>("sp_GetUserProductivity");
            _cache.Set(cacheKey, results, TimeSpan.FromMinutes(2));
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Stored procedure sp_GetUserProductivity failed, returning empty list");
            return Enumerable.Empty<UserProductivityReport>();
        }
    }
}
