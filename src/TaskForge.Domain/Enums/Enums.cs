namespace TaskForge.Domain.Enums;

public enum TaskItemStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    OnHold = 3,
    Cancelled = 4
}

public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public enum UserStatus
{
    Active = 0,
    Inactive = 1,
    Suspended = 2
}
