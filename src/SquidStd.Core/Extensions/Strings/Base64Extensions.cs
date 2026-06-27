using System.Text;

namespace SquidStd.Core.Extensions.Strings;

/// <summary>
///     Provides base64 conversion helpers for strings and byte arrays.
/// </summary>
public static class Base64Extensions
{
    /// <param name="value">The string to inspect or convert.</param>
    extension(string value)
    {
        /// <summary>
        ///     Determines whether the string is a well-formed base64 value.
        /// </summary>
        /// <returns><c>true</c> when the string decodes as base64; otherwise <c>false</c>.</returns>
        public bool IsBase64String()
        {
            if (string.IsNullOrEmpty(value) || value.Length % 4 != 0)
            {
                return false;
            }

            var buffer = new byte[value.Length];

            return Convert.TryFromBase64String(value, buffer, out _);
        }

        /// <summary>
        ///     Encodes the UTF-8 bytes of the string as base64.
        /// </summary>
        /// <returns>The base64 representation.</returns>
        public string ToBase64()
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        }

        /// <summary>
        ///     Decodes a base64 string into its UTF-8 text.
        /// </summary>
        /// <returns>The decoded text.</returns>
        public string FromBase64()
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(value));
        }

        /// <summary>
        ///     Decodes a base64 string into its raw bytes.
        /// </summary>
        /// <returns>The decoded bytes.</returns>
        public byte[] FromBase64ToByteArray()
        {
            return Convert.FromBase64String(value);
        }
    }

    /// <param name="value">The bytes to encode.</param>
    extension(byte[] value)
    {
        /// <summary>
        ///     Encodes the bytes as base64.
        /// </summary>
        /// <returns>The base64 representation.</returns>
        public string ToBase64()
        {
            return Convert.ToBase64String(value);
        }
    }
}
