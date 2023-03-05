# APNs provider in .NET

This repository aims at illustrating how to send push notifications to
Apple devices via Apple Push Notification service (APNs).

This implementation is based on [Apple recommendations](https://developer.apple.com/documentation/usernotifications/setting_up_a_remote_notification_server/sending_notification_requests_to_apns).

The most "tricky" part is the managment of the `JWT` token and how the `HTTP2` request is formatted.

Please note that the current implementation **is not exhaustive at all**.
The `alert` that is implemented is very basic, and you should refer to [Apple's documentation](https://developer.apple.com/documentation/usernotifications/setting_up_a_remote_notification_server/generating_a_remote_notification)
to extend this project to your needs.

As a security note, please consider that this is an example library. The `p8` file
containing the signing key in this example is imported as a base64 string in the `appsettings`.
You must find a more secure way to provide this signing key to a production environment.

