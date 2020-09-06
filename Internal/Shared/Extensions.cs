using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using EP94.WebSocketRpc.Internal.Shared.Models;
using EP94.WebSocketRpc.Internal.Shared.Models.Responses;
using EP94.WebSocketRpc.Public.Shared.Models;
using EP94.WebSocketRpc.Public.Shared.Models.Responses;
using Newtonsoft.Json;

namespace EP94.WebSocketRpc.Internal.Shared
{
    internal static class Extensions
    {
        private static readonly byte[] _iv = new byte[] { 0x20, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x40, 0x00, 0x00, 0x30, 0x00, 0x00, 0x02, 0x00, 0x00 };
        public static byte[] GetBytes(this string message)
        {
            return Encoding.Default.GetBytes(message);
        }

        public static string GetString(this byte[] bytes)
        {
            return Encoding.Default.GetString(bytes);
        }

        public static string ToJson(this JsonRpcResponse jsonRpcResponse)
        {
            return JsonConvert.SerializeObject(jsonRpcResponse,
                Formatting.None,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });
        }

        public static string ToJson(this JsonRpcMessage jsonRpcMessage)
        {
            return JsonConvert.SerializeObject(jsonRpcMessage,
                Formatting.None,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    StringEscapeHandling = StringEscapeHandling.Default
                });
        }

        public static RijndaelManaged GetRijndaelManaged(String secretKey)
        {
            var keyBytes = new byte[16];
            var secretKeyBytes = Encoding.UTF8.GetBytes(secretKey);
            Array.Copy(secretKeyBytes, keyBytes, Math.Min(keyBytes.Length, secretKeyBytes.Length));
            return new RijndaelManaged
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                KeySize = 128,
                BlockSize = 128,
                Key = keyBytes,
                IV = keyBytes
            };
        }

        public static byte[] Encrypt(byte[] plainBytes, RijndaelManaged rijndaelManaged)
        {
            return rijndaelManaged.CreateEncryptor()
                .TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        }

        public static byte[] Decrypt(byte[] encryptedData, RijndaelManaged rijndaelManaged)
        {
            return rijndaelManaged.CreateDecryptor()
                .TransformFinalBlock(encryptedData, 0, encryptedData.Length);
        }

        /// <summary>
        /// Encrypts plaintext using AES 128bit key and a Chain Block Cipher and returns a base64 encoded string
        /// </summary>
        /// <param name="plainText">Plain text to encrypt</param>
        /// <param name="key">Secret key</param>
        /// <returns>Base64 encoded string</returns>
        public static String Encrypt(this String plainText, String key)
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(Encrypt(plainBytes, GetRijndaelManaged(key)));
        }

        /// <summary>
        /// Decrypts a base64 encoded string using the given key (AES 128bit key and a Chain Block Cipher)
        /// </summary>
        /// <param name="encryptedText">Base64 Encoded String</param>
        /// <param name="key">Secret Key</param>
        /// <returns>Decrypted String</returns>
        public static String Decrypt(this String encryptedText, String key)
        {
            encryptedText = encryptedText.Replace("\0", "").Replace("\n", "");
            var encryptedBytes = Convert.FromBase64String(encryptedText);
            return Encoding.UTF8.GetString(Decrypt(encryptedBytes, GetRijndaelManaged(key)));
        }
    }
}
