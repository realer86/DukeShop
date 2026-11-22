using System.Security.Cryptography;
using System.Text;

namespace DukeShopBackend.Services
{
    public static class PasswordSecurity
    {
        private const int Iterations = 100_000;
        private const int HashSize = 32;

        public static string HashPassword(string plainPassword)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(plainPassword),
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                HashSize
            );
            return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        public static string HashPasswordWithFixedSalt(string plainPassword, string fixedSaltText)
        {
            byte[] salt = SHA256.HashData(Encoding.UTF8.GetBytes(fixedSaltText))[..16];
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(plainPassword),
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                HashSize
            );
            return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        public static bool VerifyPassword(string plainPassword, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(plainPassword) || string.IsNullOrWhiteSpace(storedHash))
                return false;

            string[] parts = storedHash.Split('.');
            if (parts.Length != 3 || !int.TryParse(parts[0], out int iterations))
                return false;

            byte[] salt = Convert.FromBase64String(parts[1]);
            byte[] expectedHash = Convert.FromBase64String(parts[2]);

            byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(plainPassword),
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expectedHash.Length
            );

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
    }
}
