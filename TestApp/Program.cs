using Microsoft.Extensions.DependencyInjection;

using Glihm.Networking.PushNotifications.APNs;

using PushNotificationProvider;

HostApp app = new();

String deviceToken = "55a83e4.....94ee29f76";

APNsProvider apns = app.Host.Services.GetRequiredService<APNsProvider>();

APNsAlertMessage m = new()
{
    Title = "Notification's title",
    Subtitle = "Subtitle for details",
    Body = "A long body with some stuff in there.",
    Data = new Dictionary<String, String>()
    {
        { "RandomGuid", Guid.NewGuid().ToString() }
    },
};

Guid? apnsId = await apns.SendAlert(m, deviceToken);
