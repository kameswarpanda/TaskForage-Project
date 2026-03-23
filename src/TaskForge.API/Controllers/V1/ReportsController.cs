using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskForge.Application.DTOs;
using TaskForge.Application.Interfaces;

namespace TaskForge.API.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// Get overall task summary report (total, pending, in-progress, completed, overdue).
    /// Uses stored procedure: sp_GetTaskSummaryReport.
    /// </summary>
    [HttpGet("task-summary")]
    [ProducesResponseType(typeof(ApiResponse<TaskSummaryReport>), 200)]
    public async Task<IActionResult> GetTaskSummary()
    {
        var report = await _reportService.GetTaskSummaryAsync();
        return Ok(ApiResponse<TaskSummaryReport>.Ok(report));
    }

    /// <summary>
    /// Get per-project task completion reports.
    /// Uses stored procedure: sp_GetProjectTaskReports (JOINs + GROUP BY).
    /// </summary>
    [HttpGet("project-tasks")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProjectTaskReport>>), 200)]
    public async Task<IActionResult> GetProjectTaskReports()
    {
        var reports = await _reportService.GetProjectTaskReportsAsync();
        return Ok(ApiResponse<IEnumerable<ProjectTaskReport>>.Ok(reports));
    }

    /// <summary>
    /// Get user productivity rankings.
    /// Uses stored procedure: sp_GetUserProductivity (CTE + ROW_NUMBER + RANK).
    /// </summary>
    [HttpGet("user-productivity")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserProductivityReport>>), 200)]
    public async Task<IActionResult> GetUserProductivity()
    {
        var reports = await _reportService.GetUserProductivityAsync();
        return Ok(ApiResponse<IEnumerable<UserProductivityReport>>.Ok(reports));
    }
}
