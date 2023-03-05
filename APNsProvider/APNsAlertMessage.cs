namespace Glihm.Networking.PushNotifications.APNs;

/// <summary>
/// Simple example for APNs alert message content.
/// Please refer to <see href="https://developer.apple.com/documentation/usernotifications/setting_up_a_remote_notification_server/generating_a_remote_notification">Apple's documentation</see> for more info.
/// </summary>
public class APNsAlertMessage
{
    /// <summary>
    /// Textual title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Textual Subtitle.
    /// </summary>
    public string? Subtitle { get; set; }

    /// <summary>
    /// Textual body.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Custom data for the push notification.
    /// <para>
    /// Custom data must be added to the payload.
    /// </para>
    /// </summary>
    public Dictionary<string, string>? Data { get; set; }
}
