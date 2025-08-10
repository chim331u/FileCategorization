using System.Security.Cryptography;
using System.Text;
using FileCategorization_Api.Interfaces;

namespace FileCategorization_Api.Services
{
    public class UtilityServices : IUtilityServices
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<IUtilityServices> _logger;
       
        
        public UtilityServices(IConfiguration configuration, ILogger<IUtilityServices> logger)
        {
            _logger = logger;
           
            // Initialize the configuration
            _configuration = configuration;
          
        }

        /// <summary>
        /// Get the time difference between two DateTime objects    
        /// /// </summary>
        /// <param name="start">Start DateTime</param>  
        /// <param name="end">End DateTime</param>
        /// <returns>String with time difference in milliseconds</returns>
        /// <remarks>Use this method to measure the time taken for a process</remarks>
        public async Task<string> TimeDiff(DateTime start, DateTime end)
        {
            TimeSpan _span = end - start;
            return string.Concat(((int)_span.TotalMilliseconds).ToString(), " ms");
        }

        /// <summary>
        /// Crypt a string - Default key
        /// </summary>
        /// <param name="plainText">text to encrypt</param>
        /// <returns>Encrypted string</returns>
        /// <remarks>Use this method to encrypt a string</remarks>
        public async Task<string> EncryptString(string plainText)
        {
            byte[] iv = new byte[16];
            byte[] array;

// key using the secret key from the configuration.
            string key =string.Empty;

            if (_configuration.GetSection("IsDev").Value != null)
            {
                //for debug only
                key = _configuration["CRYPTO:MASTERKEY"];
            
            }
            else
            {
                key = Environment.GetEnvironmentVariable("CRYPTO_MASTERKEY");
            }
            
            if (string.IsNullOrEmpty(key))
            {
                _logger.LogWarning($"No key found for MasterKeyCryptoName");
                return "No key found";
            }
            
            using (Aes aes = Aes.Create())
            {
                try
                {
                    byte[] keyBytes;
                    using (Rfc2898DeriveBytes pbkdf = new Rfc2898DeriveBytes(key, Encoding.UTF8.GetBytes(key)))
                    {
                        // here 16 bytes for AES128
                        keyBytes = pbkdf.GetBytes(16);
                    }

                    aes.Key = keyBytes;
                    aes.IV = iv;
                    aes.Padding = PaddingMode.PKCS7;

                    ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        using (CryptoStream cryptoStream =
                               new CryptoStream((Stream)memoryStream, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter streamWriter = new StreamWriter((Stream)cryptoStream))
                            {
                                streamWriter.Write(plainText);
                            }

                            array = memoryStream.ToArray();
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError($"Error encrypting: {e.Message}");
                    return e.Message;
                }
            }

            return Convert.ToBase64String(array);
        }


        /// <summary>
        /// Decrypt a string - Default key
        /// </summary>
        /// <param name="cipherText"></param>
        /// <returns>Decrypted string</returns>
        /// <remarks>Use this method to decrypt a string</remarks>
        public async Task<string> DecryptString(string cipherText)
        {
            byte[] iv = new byte[16];
            byte[] buffer = Convert.FromBase64String(cipherText);

            // key using the secret key from the configuration.
            string key =string.Empty;

             if (_configuration.GetSection("IsDev").Value != null)
             {
                 //for debug only
                 key = _configuration["CRYPTO:MASTERKEY"];
            
             }
             else
             {
                 key = Environment.GetEnvironmentVariable("CRYPTO_MASTERKEY");
             }



            if (string.IsNullOrEmpty(key))
            {
                _logger.LogWarning($"No key found for MasterKeyCryptoName");
                return "No key found";
            }

            try
            {
                byte[] keyBytes;
                using (Rfc2898DeriveBytes pbkdf = new Rfc2898DeriveBytes(key, Encoding.UTF8.GetBytes(key)))
                {
                    // here 16 bytes for AES128
                    keyBytes = pbkdf.GetBytes(16);
                }

                using (Aes aes = Aes.Create())
                {
                    aes.Key = keyBytes;
                    aes.IV = iv;
                    aes.Padding = PaddingMode.PKCS7;

                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                    using (MemoryStream memoryStream = new MemoryStream(buffer))
                    {
                        using (CryptoStream cryptoStream =
                               new CryptoStream((Stream)memoryStream, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader streamReader = new StreamReader((Stream)cryptoStream))
                            {
                                return streamReader.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Error decrypting: {e.Message}");
                return e.Message;
            }
        }


        /// <summary>
        /// Hash a string using SHA256
        /// </summary>
        /// <param name="input">string to hash</param>
        /// <returns>String with hash value in SHA256</returns>
        /// <remarks>Use this method to hash a string</remarks>
        public async Task<string> HashString_SHA256(string input)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // Convert the input string to a byte array and compute the hash.
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

                // Create a new Stringbuilder to collect the bytes and create a string.
                StringBuilder builder = new StringBuilder();

                // Loop through each byte of the hashed data
                // and format each one as a hexadecimal string.
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }


        /// <summary>
        /// Verify a hash using SHA256
        /// </summary>
        /// <param name="input">string to compare</param>
        /// <param name="hash">hash to campare</param>
        /// <returns>True id equals, else False</returns>
        /// <remarks>Use this method to verify a hash</remarks>
        public async Task<bool> VerifyHash_SHA256(string input, string hash)
        {
            // Hash the input.
            string hashOfInput = await HashString_SHA256(input);

            // Create a StringComparer an compare the hashes.
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;

            return comparer.Compare(hashOfInput, hash) == 0;
        }

        public string GetLogFileText(string logFilePath)
        {
            if (string.IsNullOrEmpty(logFilePath))
            {
                return null;
            }

            string logText = string.Empty;

            using (var sr = new StreamReader(logFilePath))
            {
                // Read the stream as a string, and write the string to the console.

                logText = sr.ReadToEnd();
            }

            return logText;
        }
    }
}