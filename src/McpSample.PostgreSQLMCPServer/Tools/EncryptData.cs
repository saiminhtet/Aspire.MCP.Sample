using McpSample.PostgreSQLMCPServer.Helper;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace McpSample.PostgreSQLMCPServer
{
    public partial class Tools
    {
        [McpServerTool(
            Title = "Encrypt Data",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false),
            Description("Encrypts plain text using AES-GCM encryption. Returns encrypted string in format: base64(encrypted)|||||||base64(iv)|||||||base64(tag)|||||||")]
        public async Task<DbOperationResult> EncryptData(
            [Description("Plain text value to encrypt")] string plainText)
        {
            try
            {
                if (string.IsNullOrEmpty(plainText))
                {
                    return new DbOperationResult(
                        success: false,
                        error: "Plain text cannot be null or empty");
                }

                var encryptedValue = AESGCMEncryption.Encrypt(plainText);

                if (encryptedValue == null)
                {
                    return new DbOperationResult(
                        success: false,
                        error: "Encryption failed - returned null");
                }

                _logger.LogInformation("Successfully encrypted data (length: {Length} -> {EncryptedLength})",
                    plainText.Length, encryptedValue.Length);

                return new DbOperationResult(
                    success: true,
                    data: new
                    {
                        plainText = plainText,
                        encryptedValue = encryptedValue,
                        encryptedLength = encryptedValue.Length
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Encryption failed: {Message}", ex.Message);
                return new DbOperationResult(success: false, error: ex.Message);
            }
        }

        [McpServerTool(
            Title = "Decrypt Data",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false),
            Description("Decrypts encrypted text using AES-GCM decryption. Accepts encrypted string in format: base64(encrypted)|||||||base64(iv)|||||||base64(tag)|||||||")]
        public async Task<DbOperationResult> DecryptData(
            [Description("Encrypted value to decrypt (format: base64|||||||base64|||||||base64|||||||)")] string encryptedText)
        {
            try
            {
                if (string.IsNullOrEmpty(encryptedText))
                {
                    return new DbOperationResult(
                        success: false,
                        error: "Encrypted text cannot be null or empty");
                }

                if (!AESGCMEncryption.IsEncrypted(encryptedText))
                {
                    return new DbOperationResult(
                        success: false,
                        error: "Input does not appear to be encrypted (missing delimiter pattern)");
                }

                var decryptedValue = AESGCMEncryption.Decrypt(encryptedText);

                if (decryptedValue == null)
                {
                    return new DbOperationResult(
                        success: false,
                        error: "Decryption failed - returned null");
                }

                _logger.LogInformation("Successfully decrypted data (encrypted length: {EncryptedLength} -> {Length})",
                    encryptedText.Length, decryptedValue.Length);

                return new DbOperationResult(
                    success: true,
                    data: new
                    {
                        encryptedText = encryptedText,
                        decryptedValue = decryptedValue,
                        decryptedLength = decryptedValue.Length
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Decryption failed: {Message}", ex.Message);
                return new DbOperationResult(success: false, error: ex.Message);
            }
        }
    }
}
