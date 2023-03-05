using System.Security.Claims;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;

using Microsoft.IdentityModel.Tokens;

namespace Glihm.Networking.PushNotifications.APNs;

/// <summary>
/// APNs JWT token generator.
/// Follow recommendations from
/// <see href="https://developer.apple.com/documentation/usernotifications/setting_up_a_remote_notification_server/establishing_a_token-based_connection_to_apns">Apple's documentation</see>.
/// </summary>
public class APNsJWTGenerator
{
    /// <summary>
    /// Timestamp at which the JWT token is generated.
    /// </summary>
    private DateTimeOffset _jwtGenerationTimeUTC;

    /// <summary>
    /// Generated token, conserved to respect APNs token lifespan.
    /// </summary>
    private string _currentJWT;

    /// <summary>
    /// Renewal frequency for the JWT, in minutes.
    /// Must be no more than once every 20 minutes and
    /// no less than once every 60 minutes.
    /// </summary>
    private int _jwtRenewFreq;

    /// <summary>
    /// Ctor.
    /// </summary>
    public APNsJWTGenerator(int jwtRenewFreq = 45)
    {
        this._currentJWT = string.Empty;

        if (jwtRenewFreq < 20 || jwtRenewFreq > 59)
        {
            throw new ArgumentOutOfRangeException(
                $"JWT must be renew no more than once every" +
                $"20 minutes and no less than once every 60 minutes." +
                $" You provided {jwtRenewFreq} minutes.");
        }

        this._jwtRenewFreq = jwtRenewFreq;
    }

    /// <summary>
    /// Generates a JWT token following APNs specifications.
    /// </summary>
    /// <param name="teamID">Team ID from Apple developer account.</param>
    /// <param name="keyID">Key ID generated from Apple developer account.</param>
    /// <param name="ecdsaSigner">Initialized instance for ECDsa signing.</param>
    /// <returns>Generated (or re-used) JWT token.</returns>
    public string
    GetToken(string teamID, string keyID, ECDsa ecdsaSigner)
    {
        if (!this.DoRefreshJWT())
        {
            return this._currentJWT;
        }

        this._jwtGenerationTimeUTC = DateTimeOffset.UtcNow;

        JwtSecurityTokenHandler tokenHandler = new();

        // Key ID must be set here as header claims are automatically set
        // by the Microsoft library.
        ECDsaSecurityKey ecdsaKey = new(ecdsaSigner);
        ecdsaKey.KeyId = keyID;

        // APNs only works with ES256 -> ECDsa P256 and SHA256 HMAC.
        SigningCredentials creds = new SigningCredentials(ecdsaKey, SecurityAlgorithms.EcdsaSha256);

        // Principal claims.
        Claim teamIDClaim = new Claim("iss", $"{teamID}");
        Claim tokenGenTimeClaim = new Claim("iat", $"{this._jwtGenerationTimeUTC.ToUnixTimeSeconds()}");

        Claim[] claims = new[] { teamIDClaim, tokenGenTimeClaim };

        SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            SigningCredentials = creds,
        };

        SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);

        this._currentJWT = tokenHandler.WriteToken(token);

        return this._currentJWT;
    }

    /// <summary>
    /// Verifies if the JWT token must be refreshed or not.
    /// APNs requires a JWT refresh no more than once every 20 minutes
    /// and no less than once every 60 minutes.
    /// </summary>
    /// <returns>True if the JWT must be refreshed, false otherwise.</returns>
    private bool
    DoRefreshJWT()
    {
        if (string.IsNullOrEmpty(this._currentJWT))
        {
            return true;
        }

        TimeSpan ts = DateTimeOffset.UtcNow - this._jwtGenerationTimeUTC;
        return ts.TotalMinutes > this._jwtRenewFreq;
    }

}
