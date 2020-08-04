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

        public static string Encrypt(this string clearText, string encryptionKey)
        {
            if (string.IsNullOrEmpty(encryptionKey))
                return clearText;

            byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(encryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    clearText = Convert.ToBase64String(ms.ToArray());
                }
            }
            return clearText;
        }
        public static string Decrypt(this string cipherText, string encryptionKey)
        {
            if (string.IsNullOrEmpty(encryptionKey))
                return cipherText;

            cipherText = cipherText.Replace(" ", "+");
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(encryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }
                    cipherText = Encoding.Unicode.GetString(ms.ToArray());
                }
            }
            return cipherText;
        }
    }
}
