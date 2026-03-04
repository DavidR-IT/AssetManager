using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace AssetManager.Helpers
{
    public static class PasswordHelper
    {
        /// <summary>
        /// Hash a password using PBKDF2
        /// </summary>
        public static string HashPassword(string password)
        {
            // Generate a 128-bit salt
            byte[] salt = RandomNumberGenerator.GetBytes(128 / 8);

            // Hash the password with salt
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 600000,
                numBytesRequested: 256 / 8));

            // Return salt + hash combined
            return $"{Convert.ToBase64String(salt)}.{hashed}";
        }

        /// <summary>
        /// Verify a password against a hash
        /// </summary>
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            try
            {
                var parts = hashedPassword.Split('.');
                if (parts.Length != 2)
                    return false;

                byte[] salt = Convert.FromBase64String(parts[0]);
                string storedHash = parts[1];

                // Hash the provided password with the same salt
                string hash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                    password: password,
                    salt: salt,
                    prf: KeyDerivationPrf.HMACSHA256,
                    iterationCount: 600000,
                    numBytesRequested: 256 / 8));

                return CryptographicOperations.FixedTimeEquals(
                Convert.FromBase64String(hash),
                Convert.FromBase64String(storedHash));
            }
            catch (FormatException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
