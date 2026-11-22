using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DukeShopBackend.Models;

namespace DukeShopBackend.Services
{
    public class AuthTokenPayload
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "STAFF";
        public long Exp { get; set; }
    }

    public class TokenService
    {
        private readonly string _secretKey;
        private readonly int _expiryHours;
        private static readonly ConcurrentDictionary<string, long> RevokedTokens = new();

        public TokenService(IConfiguration configuration)
        {
            _secretKey = configuration["Security:TokenSecret"]
                         ?? "DukeShop_Enterprise_Default_Signing_Key_2026!";
            _expiryHours = configuration.GetValue<int>("Security:TokenExpiryHours", 12);
        }

        public string GenerateToken(User user)
        {
            var headerJson = JsonSerializer.Serialize(new { alg = "HS256", typ = "JWT" });
            var payload = new AuthTokenPayload
            {
                UserId = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Role = user.Role,
                Exp = DateTimeOffset.UtcNow.AddHours(_expiryHours).ToUnixTimeSeconds()
            };
            var payloadJson = JsonSerializer.Serialize(payload);

            string headerEncoded = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
            string payloadEncoded = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
            string signingInput = $"{headerEncoded}.{payloadEncoded}";
            string signatureEncoded = ComputeSignature(signingInput);

            return $"{signingInput}.{signatureEncoded}";
        }

        public AuthTokenPayload? ValidateRequest(HttpRequest request)
        {
            string? token = ExtractBearerToken(request);
            if (string.IsNullOrWhiteSpace(token)) return null;
            return ValidateToken(token);
        }

        public AuthTokenPayload? ValidateToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token) || RevokedTokens.ContainsKey(token))
                return null;

            string[] parts = token.Split('.');
            if (parts.Length != 3) return null;

            string signingInput = $"{parts[0]}.{parts[1]}";
            string expectedSig = ComputeSignature(signingInput);

            byte[] actualSigBytes = Encoding.UTF8.GetBytes(parts[2]);
            byte[] expectedSigBytes = Encoding.UTF8.GetBytes(expectedSig);
            if (!CryptographicOperations.FixedTimeEquals(actualSigBytes, expectedSigBytes))
                return null;

            try
            {
                byte[] payloadBytes = Base64UrlDecode(parts[1]);
                var payload = JsonSerializer.Deserialize<AuthTokenPayload>(payloadBytes);
                if (payload == null) return null;

                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (payload.Exp < now) return null;

                return payload;
            }
            catch
            {
                return null;
            }
        }

        public void RevokeRequestToken(HttpRequest request)
        {
            string? token = ExtractBearerToken(request);
            if (!string.IsNullOrWhiteSpace(token))
            {
                RevokedTokens[token] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
        }

        private static string? ExtractBearerToken(HttpRequest request)
        {
            if (!request.Headers.TryGetValue("Authorization", out var authHeader))
                return null;

            string raw = authHeader.ToString().Trim();
            if (raw.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return raw.Substring("Bearer ".Length).Trim();
            }
            return null;
        }

        private string ComputeSignature(string input)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey));
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Base64UrlEncode(hash);
        }

        private static string Base64UrlEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static byte[] Base64UrlDecode(string input)
        {
            string padded = input.Replace('-', '+').Replace('_', '/');
            switch (padded.Length % 4)
            {
                case 2: padded += "=="; break;
                case 3: padded += "="; break;
            }
            return Convert.FromBase64String(padded);
        }
    }
}
