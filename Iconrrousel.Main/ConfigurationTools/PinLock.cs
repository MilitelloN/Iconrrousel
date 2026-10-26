using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Iconrrousel.Main.Configuration
{
    // PIN basico de 4 digitos para proteger los rangos horarios. Se guarda como
    // "salt:hash" (SHA-256) en Settings.json, nunca en texto plano. No es una
    // proteccion fuerte (son 10.000 combinaciones y el archivo es editable),
    // solo evita cambiar los rangos desde la configuracion sin pensarlo.
    public static class PinLock
    {
        public const int Length = 4;

        public static bool IsValidFormat(string pin) =>
            pin != null && pin.Length == Length && pin.All(char.IsAsciiDigit);

        public static string CreateHash(string pin)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(Hash(salt, pin))}";
        }

        public static bool Verify(string pin, string storedHash)
        {
            if (!IsValidFormat(pin) || string.IsNullOrEmpty(storedHash))
                return false;

            var parts = storedHash.Split(':');
            if (parts.Length != 2)
                return false;

            try
            {
                byte[] salt = Convert.FromBase64String(parts[0]);
                byte[] expected = Convert.FromBase64String(parts[1]);
                return CryptographicOperations.FixedTimeEquals(Hash(salt, pin), expected);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static byte[] Hash(byte[] salt, string pin)
        {
            byte[] pinBytes = Encoding.UTF8.GetBytes(pin);
            return SHA256.HashData(salt.Concat(pinBytes).ToArray());
        }
    }
}
