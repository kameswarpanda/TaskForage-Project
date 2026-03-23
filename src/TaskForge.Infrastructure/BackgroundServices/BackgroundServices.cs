using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TaskForge.Infrastructure.BackgroundServices;

/// <summary>
/// Background service for processing audit log entries asynchronously.
/// Uses ConcurrentQueue for thread-safe message passing.
/// Demonstrates: IHostedService, BackgroundService, ConcurrentQueue, async patterns.
/// </summary>
public class AuditLogProcessor : BackgroundService
{
    private readonly ILogger<AuditLogProcessor> _logger;
    private static readonly ConcurrentQueue<AuditLogEntry> _auditQueue = new();

    public AuditLogProcessor(ILogger<AuditLogProcessor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Enqueue an audit log entry for background processing.
    /// Thread-safe via ConcurrentQueue.
    /// </summary>
    public static void Enqueue(string entityName, int entityId, string action, string? performedBy)
    {
        _auditQueue.Enqueue(new AuditLogEntry
        {
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            PerformedBy = performedBy,
            Timestamp = DateTime.UtcNow
        });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AuditLogProcessor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            while (_auditQueue.TryDequeue(out var entry))
            {
                try
                {
                    _logger.LogInformation(
                        "[AUDIT] {Action} on {Entity}#{Id} by {User} at {Time}",
                        entry.Action, entry.EntityName, entry.EntityId,
                        entry.PerformedBy, entry.Timestamp);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing audit log entry");
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }

        _logger.LogInformation("AuditLogProcessor stopped");
    }
}

public class AuditLogEntry
{
    public string EntityName { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? PerformedBy { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Background service for simulating notification processing (e.g., overdue task reminders).
/// Demonstrates periodic background task execution.
/// </summary>
public class TaskNotificationService : BackgroundService
{
    private readonly ILogger<TaskNotificationService> _logger;

    public TaskNotificationService(ILogger<TaskNotificationService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TaskNotificationService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogDebug("Checking for overdue tasks...");
                // In production, this would query the database and send notifications.
                // This demonstrates the BackgroundService pattern.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in task notification service");
            }

            // Check every 60 seconds
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }

        _logger.LogInformation("TaskNotificationService stopped");
    }
}
