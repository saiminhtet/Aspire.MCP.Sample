using System.Security.Cryptography;
using System.Collections.Concurrent;

namespace McpSample.PostgreSQLMCPServer.Helper
{
    public class AESGCMEncryption
    {
        private static readonly byte[] _KEY = new byte[32] {
            88, 161, 103, 41, 78, 153, 2, 141, 75, 78, 63, 104, 20, 31, 181, 19,
            205, 139, 166, 95, 207, 122, 242, 129, 64, 1, 125, 36, 61, 61, 144, 134
        };

        private static readonly byte[] _OLD_IV = new byte[16] {
            171, 217, 165, 168, 172, 230, 224, 152, 195, 225, 187, 15, 12, 128, 125, 96
        };

        private static readonly byte[] _ASSOCIATED_DATA = new byte[80] {
            81, 74, 18, 10, 2, 83, 160, 248, 17, 3, 100, 194, 83, 7, 93, 20,
            26, 236, 255, 3, 63, 87, 5, 1, 91, 73, 188, 96, 194, 78, 60, 103,
            35, 2, 34, 165, 8, 241, 98, 10, 92, 110, 39, 42, 40, 72, 42, 43,
            34, 90, 98, 76, 5, 4, 34, 56, 103, 84, 6, 188, 26, 77, 35, 56,
            35, 48, 8, 7, 100, 38, 46, 73, 61, 50, 217, 64, 65, 32, 25, 11
        };

        private const int _TAG_SIZE_V1 = 16;
        private const string _CIPHER_LIMITER_V1 = "|||||||";

        private static readonly int _ITERATION = ((DateTime.Now.Year * 11) - 10007);//supposed to increment at least once every 2 years, cannot be whole year never restart server/app

        // Cache for decrypted values to improve performance
        private static readonly ConcurrentDictionary<string, string?> _decryptionCache = new(StringComparer.Ordinal);
        private const int MAX_CACHE_SIZE = 10000;

        public static string? Encrypt(string? rawVal)
        {
            if (rawVal == null || rawVal.Length <= 0)
            {
                return null;
            }

            // Encrypt the string to an array of bytes.
            return EncryptStringToBytes_Aes(rawVal, _KEY, _ASSOCIATED_DATA, _ITERATION);
        }

        static string EncryptStringToBytes_Aes(string plainText, byte[] key, byte[] associatedData, int iterationCount)
        {
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (key == null || key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (associatedData == null || associatedData.Length <= 0)
                throw new ArgumentNullException("Associated Data");

            // Create an Aes object
            // with the specified key and IV.
            //using (Aes aesAlg = Aes.Create())
            //{
            //    aesAlg.Key = key;
            //    aesAlg.IV = iv;

            //    // Create an encryptor to perform the stream transform.
            //    ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            //    // Create the streams used for encryption.
            //    using (MemoryStream msEncrypt = new MemoryStream())
            //    {
            //        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            //        {
            //            using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
            //            {
            //                //Write all data to the stream.
            //                swEncrypt.Write(plainText);
            //            }
            //            encrypted = msEncrypt.ToArray();
            //        }
            //    }
            //}
            var returnedTag = new byte[_TAG_SIZE_V1];
            var plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            var keyGen = new Rfc2898DeriveBytes(Convert.ToBase64String(key), 64, iterationCount, HashAlgorithmName.SHA256);
            var iv = keyGen.GetBytes(12);
            byte[] encrypted = new byte[plainBytes.Length];

            string returnVal;
            using (var aesGcm = new AesGcm(key, _TAG_SIZE_V1))
            {
                aesGcm.Encrypt(iv, plainBytes, encrypted, returnedTag, associatedData);
                returnVal = Convert.ToBase64String(encrypted) + _CIPHER_LIMITER_V1 + Convert.ToBase64String(iv) + _CIPHER_LIMITER_V1 + Convert.ToBase64String(returnedTag) + _CIPHER_LIMITER_V1;//keep the end limiter so that can use to check version of encryption used
            }

            return returnVal;
        }

        public static string? Decrypt(string? encryptedVal)
        {
            if (encryptedVal == null)
            {
                return null;
            }

            return DecryptStringFromBytes_AesGcm(encryptedVal, _KEY, _ASSOCIATED_DATA);
        }

        /// <summary>
        /// Checks if a string value appears to be encrypted (contains the cipher delimiter)
        /// Optimized to check for delimiter more efficiently
        /// </summary>
        public static bool IsEncrypted(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            // Quick length check - encrypted values are always longer than delimiter
            if (value.Length < 20) // Minimum realistic encrypted string length
            {
                return false;
            }

            // Use IndexOf which is faster than Contains for this use case
            return value.IndexOf(_CIPHER_LIMITER_V1, StringComparison.Ordinal) >= 0;
        }

        /// <summary>
        /// Attempts to decrypt a value, returning the original value if decryption fails
        /// Uses caching to improve performance for repeated values
        /// </summary>
        public static string? TryDecrypt(string? encryptedVal)
        {
            if (encryptedVal == null || !IsEncrypted(encryptedVal))
            {
                return encryptedVal;
            }

            // Check cache first
            if (_decryptionCache.TryGetValue(encryptedVal, out var cachedValue))
            {
                return cachedValue;
            }

            try
            {
                var decrypted = Decrypt(encryptedVal);

                // Add to cache if not too large
                if (_decryptionCache.Count < MAX_CACHE_SIZE)
                {
                    _decryptionCache.TryAdd(encryptedVal, decrypted);
                }

                return decrypted;
            }
            catch (Exception ex)
            {
                // Log the specific error for debugging
                System.Diagnostics.Debug.WriteLine($"Decryption failed: {ex.Message}");
                // Return original value if decryption fails
                return encryptedVal;
            }
        }

        /// <summary>
        /// Attempts to decrypt a value with detailed error information
        /// </summary>
        public static (bool Success, string? Value, string? Error) TryDecryptWithError(string? encryptedVal)
        {
            if (encryptedVal == null)
            {
                return (true, null, null);
            }

            if (!IsEncrypted(encryptedVal))
            {
                return (true, encryptedVal, null);
            }

            // Check cache first
            if (_decryptionCache.TryGetValue(encryptedVal, out var cachedValue))
            {
                return (true, cachedValue, null);
            }

            try
            {
                var decrypted = Decrypt(encryptedVal);

                // Add to cache if not too large
                if (_decryptionCache.Count < MAX_CACHE_SIZE)
                {
                    _decryptionCache.TryAdd(encryptedVal, decrypted);
                }

                return (true, decrypted, null);
            }
            catch (Exception ex)
            {
                return (false, encryptedVal, $"Decryption failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears the decryption cache
        /// </summary>
        public static void ClearCache()
        {
            _decryptionCache.Clear();
        }

        static string DecryptStringFromBytes_AesGcm(string cipherText, byte[] key, byte[] associatedData)
        {
            //get iv and tag from cipher
            var splitted = cipherText.Split(_CIPHER_LIMITER_V1);

            if (splitted.Length != 4)
            {
                throw new ArgumentException("Invalid Decryption Value");
            }

            var encText = Convert.FromBase64String(splitted[0]);
            var iv = Convert.FromBase64String(splitted[1]);
            var tag = Convert.FromBase64String(splitted[2]);

            byte[] decrypted = new byte[encText.Length];
            using (var aesGcm = new AesGcm(key, _TAG_SIZE_V1))
            {
                aesGcm.Decrypt(iv, encText, tag, decrypted, associatedData);
            }

            return System.Text.Encoding.UTF8.GetString(decrypted);
        }
    }
}
