using System.Net.Http.Headers;
using System.Security.Cryptography; 
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Glihm.Networking.PushNotifications.APNs;

/// <summary>
/// Simple APNs push notification provider.
/// </summary>
public class APNsProvider
{
    /// <summary>
    /// Max size for the JSON payload.
    /// (VoIP can go up to 5120 bytes, you can adapt).
    /// </summary>
    public readonly static int PayloadMaxSize = 4096;

    /// <summary>
    /// HttpClientFactory to handle HTTP requests.
    /// Perhaps in the APNs scenario, the default factory is the best to handle APNs
    /// connection as they can be reused for a longer period of time.
    /// But it's the common way to dealing with HTTP in a dependency injection
    /// context.
    /// <see href="https://developer.apple.com/documentation/usernotifications/setting_up_a_remote_notification_server/sending_notification_requests_to_apns#2947606">Apple's documentation about request handling.</see>.
    /// </summary>
    private readonly IHttpClientFactory _httpFactory;

    /// <summary>
    /// Logger.
    /// </summary>
    private readonly ILogger<APNsProvider> _logger;

    /// <summary>
    /// Options for APNs configuration.
    /// </summary>
    private readonly APNsOptions _options;

    /// <summary>
    /// ECDsa signer where private key is imported.
    /// </summary>
    private readonly ECDsa _ecdsaSigner;

    /// <summary>
    /// Instance of a JWT generator, which lives
    /// inside the APNsProvider to keep track
    /// of the token lifespan.
    /// </summary>
    private readonly APNsJWTGenerator _jwtGenerator;

    /// <summary>
    /// DI Ctor.
    /// </summary>
    /// <param name="httpFactory">HttpFactory.</param>
    /// <param name="options">IOptions for <see cref="APNsOptions"/>.</param>
    public APNsProvider(IHttpClientFactory httpFactory, IOptions<APNsOptions> options, ILogger<APNsProvider> logger)
    {
        this._httpFactory = httpFactory;
        this._options = options.Value;
        this._logger = logger;

        this._jwtGenerator = new APNsJWTGenerator(jwtRenewFreq: this._options.JWTRenewFreqMinutes);
        this._ecdsaSigner = this.ImportECDsaPrivateKey(this._options.TokenSigningPrivateKey);
    }

    /// <summary>
    /// Sends an alert push notification to APNs.
    /// Very basic and not exhaustive example.
    /// Please refer to <see href="https://developer.apple.com/documentation/usernotifications/setting_up_a_remote_notification_server/generating_a_remote_notification">Apple's documentation</see>.
    /// </summary>
    /// <param name="message">Message with Alert push notification content.</param>
    /// <param name="deviceToken">Device token.</param>
    /// <returns>GUID of the successfully sent push notification, null otherwise.</returns>
    public async Task<Guid?>
    SendAlert(APNsAlertMessage message, string deviceToken)
    {
        HttpRequestMessage r = new()
        {
            // HTTP2.0 is mandatory.
            Version = new Version(2, 0),
            Method = HttpMethod.Post,
            RequestUri = new Uri($"https://{this._options.APNsHost}:443/3/device/{deviceToken}"),
        };

        // Get JWT based on the provided options. Token is reused internally if not expired.
        string jwt = this._jwtGenerator.GetToken(this._options.TeamID, this._options.KeyID, this._ecdsaSigner);
        r.Headers.Authorization = new AuthenticationHeaderValue("bearer", jwt);

        // Setup headers supported by APNs to configure the push notification.
        r.Headers.Add("apns-push-type", "alert");
        r.Headers.Add("apns-id", Guid.NewGuid().ToString().ToLower());
        r.Headers.Add("apns-topic", this._options.AppID);
        r.Headers.Add("apns-expiration", "0");
        //r.Headers.Add("apns-priority", "10");

        string payload = this.GenerateAlertJSONPayload(message);
        r.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        HttpClient hc = this._httpFactory.CreateClient();

        HttpResponseMessage rsp = await hc.SendAsync(r);
        this._logger.LogDebug("APNs response: {0}\n{1}\n{2}",
                              rsp.StatusCode,
                              JsonSerializer.Serialize(rsp.Headers),
                              await rsp.Content.ReadAsStringAsync());

        return this.ExtractResponseAPNsID(rsp);
    }

    /// <summary>
    /// Extracts the apns-id header value
    /// to ensure good tracking of the notification's identifier
    /// in case of debugging required.
    /// </summary>
    /// <param name="rsp">Response from APNs.</param>
    /// <returns>Guid returned by APNs on success, null if "apns-id" header could'nt be found.</returns>
    private Guid?
    ExtractResponseAPNsID(HttpResponseMessage rsp)
    {
        string apnsIdKey = "apns-id";
        if (rsp.Headers.Contains(apnsIdKey))
        {
            IEnumerable<string> apnsValues = rsp.Headers.GetValues(apnsIdKey);
            if (apnsValues.Count() == 0)
            {
                this._logger.LogError("apns-id can't be extracted from response headers.");
                return null;
            }

            return Guid.Parse(apnsValues.First());
        }

        this._logger.LogError("apns-id can't be extracted from response headers.");
        return null;
    }

    /// <summary>
    /// Generates JSON payload from given message.
    /// Custom data are added at the top level of the
    /// JSON payload.
    /// </summary>
    /// <param name="message">Alert message to be serialized.</param>
    /// <returns>JSON payload.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Generated JSON is more than <c>PayloadMaxSize</c> bytes long.</exception>
    private string
    GenerateAlertJSONPayload(APNsAlertMessage message)
    {
        string json = $@"
{{
    ""aps"" : {{
        ""alert"" : {{
            ""title"" : ""{message.Title}"",
            ""subtitle"" : ""{message.Subtitle}"",
            ""body"" : ""{message.Body}""
        }},
        ""sound"": ""default""
    }},
    {this.CustomDataJSONKeys(message.Data)}
}}";

        int len = Encoding.UTF8.GetBytes(json).Length;
        if (len > PayloadMaxSize)
        {
            throw new ArgumentOutOfRangeException(
                $"APNs JSON payload must be lower than {PayloadMaxSize} bytes," +
                $"generated payload was {len} bytes long.");
        }

        this._logger.LogDebug("Generated JSON payload: {0}", json);
        return json;
    }

    /// <summary>
    /// Generates additional custom data as JSON keys.
    /// </summary>
    /// <param name="customData"></param>
    /// <returns>Serialized custom data.</returns>
    private string
    CustomDataJSONKeys(Dictionary<string, string>? customData)
    {
        if (customData is null || customData.Count == 0)
        {
            return string.Empty;
        }

        return string.Join("\n,", customData.Select(kp => $@"""{kp.Key}"": ""{kp.Value}"""));
    }

    /// <summary>
    /// <para>
    /// Imports ECDsa private key from options.
    /// </para>
    /// <para>
    /// The .p8 file provided by Apple is a PKCS8 private key distributed in PEM format.
    /// </para>
    /// So, converting the bytes to a string we get the PEM b64 string. Then, the <c>ImportFromPem</c>
    /// does import.
    /// </summary>
    /// <returns>Initialized ECDsa instance with the imported private key, ready to sign.</returns>
    /// <exception cref="InvalidOperationException">Private key can't be imported.</exception>
    private ECDsa
    ImportECDsaPrivateKey(string pkcs8PEM)
    {
        ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        try
        {
            ecdsa.ImportFromPem(pkcs8PEM);
        }
        catch (Exception e)
        {
            throw new InvalidOperationException(
                "Can't load ECDsa token signing key, " +
                $"required to send requests to APNs servers: {e}.");
        }

        return ecdsa;
    }

}
