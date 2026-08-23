/// <summary>
/// Visual category for notification toast requests.
/// </summary>
public enum NotificationType
{
	Info,
	Quest,
	Save,
	Error,
	Loot
}

/// <summary>
/// UI event payload consumed by NotificationManager to show a short toast.
/// </summary>
public readonly record struct NotificationRequest(string Message, NotificationType Type);
