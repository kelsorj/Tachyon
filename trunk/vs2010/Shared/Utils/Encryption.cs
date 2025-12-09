using System;
using System.IO;
using System.Security.Cryptography;

namespace BioNex.Shared.Utils
{
#if !HIG_INTEGRATION
    public class Encryption
    {
        private const string salt = "BioNex-Solutions";

        public static string Encrypt(string clear_text, string password)
        {
            var clear_bytes = System.Text.Encoding.Unicode.GetBytes(clear_text);
            var pdb = new PasswordDeriveBytes(password, System.Text.Encoding.Unicode.GetBytes(salt));
            var encrypted_bytes = Encrypt(clear_bytes, pdb.GetBytes(32), pdb.GetBytes(16));
            return Convert.ToBase64String(encrypted_bytes);
        }
     
        public static string Decrypt(string base64_encrypted_text, string password)
        {
            var encrypted_bytes = Convert.FromBase64String(base64_encrypted_text);
            var pdb = new PasswordDeriveBytes(password, System.Text.Encoding.Unicode.GetBytes(salt));
            var decrypted_bytes = Decrypt(encrypted_bytes, pdb.GetBytes(32), pdb.GetBytes(16));
            return System.Text.Encoding.Unicode.GetString(decrypted_bytes);
        }

        private static byte[] Encrypt(byte[] clear_bytes, byte[] key, byte[] initialization_vector)
        {
            var memory = new MemoryStream();
            var algorithm = Rijndael.Create(); // Rijndael ("Rain-doll") is AES encryption standard
            algorithm.Key = key;
            algorithm.IV = initialization_vector;
            var stream = new CryptoStream(memory, algorithm.CreateEncryptor(), CryptoStreamMode.Write);
            stream.Write(clear_bytes, 0, clear_bytes.Length);
            stream.Close();
            return memory.ToArray();
        }

        private static byte[] Decrypt(byte[] encrypted_bytes, byte[] key, byte[] initialization_vector)
        {
            var memory = new MemoryStream();
            var algorithm = Rijndael.Create(); // Rijndael ("Rain-doll") is AES encryption standard
            algorithm.Key = key;
            algorithm.IV = initialization_vector;
            var stream = new CryptoStream(memory, algorithm.CreateDecryptor(), CryptoStreamMode.Write);
            stream.Write(encrypted_bytes, 0, encrypted_bytes.Length);
            stream.Close();
            return memory.ToArray();
        }
    }
#endif
}
