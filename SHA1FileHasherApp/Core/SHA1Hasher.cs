using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SHA1FileHasherApp.Core
{
    public class SHA1Hasher
    {
        public string ComputeSHA1(string p_filePath, int p_bytesToRead)
        {
            try
            {
                using (FileStream stream = new FileStream(p_filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    Debug.WriteLine($"Computing hash for file: {p_filePath}");
                    byte[] buffer = new byte[p_bytesToRead];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);

                    using (SHA1 sha1 = SHA1.Create())
                    {
                        byte[] hash = sha1.ComputeHash(buffer, 0, bytesRead);
                        return BitConverter.ToString(hash).Replace("-", "").ToLower();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing file {p_filePath}: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
