namespace Glihm.Networking.PushNotifications.APNs;

/// <summary>
/// Options required to setup APNs push notification provider.
/// </summary>
public class APNsOptions
{
    /// <summary>
    /// Team ID from Apple developer account.
    /// </summary>
    public string TeamID { get; set; }

    /// <summary>
    /// Key ID generated from Apple developer account.
    /// </summary>
    public string KeyID { get; set; }

    /// <summary>
    /// Private key (.p8 file) obtained from Apple developer account.
    /// This can be imported as a file or safer alternatives, in this example
    /// it's not very secure as the key is in plain text.
    /// But this examples aims at being used locally for testing APNs.
    /// </summary>
    public string TokenSigningPrivateKey { get; set; }

    /// <summary>
    /// App identifier from Apple developer account.
    /// </summary>
    public string AppID { get; set; }

    /// <summary>
    /// Host DNS to be resolve to reach APNs.
    /// <see href="https://developer.apple.com/documentation/usernotifications/setting_up_a_remote_notification_server/sending_notification_requests_to_apns#2947606">Apple's documentation for current hosts.</see>.
    /// </summary>
    public string APNsHost { get; set; }

    /// <summary>
    /// Lifespan of the JWT token for APNs token-based requests.
    /// </summary>
    public int JWTRenewFreqMinutes { get; set; }

    /// <summary>
    /// Default Ctor.
    /// </summary>
    public APNsOptions()
    {
        this.TeamID = string.Empty;
        this.KeyID = string.Empty;
        this.AppID = string.Empty;
        this.TokenSigningPrivateKey = string.Empty;
        this.JWTRenewFreqMinutes = 45;
        this.APNsHost = "api.sandbox.push.apple.com";
    }
}
