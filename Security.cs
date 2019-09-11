using System;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Xml.Linq;
using System.Web;
using Framework.Helpers;

namespace Framework
{
    public class Security
    {
        static readonly string PasswordHash = "P@@Sw0rd";
        static readonly string SaltKey = "S@LT&KEY";
        static readonly string VIKey = "@1B2c3D4e5F6g7H8";

        public static string Encrypt(string plainText)
        {
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);

            byte[] keyBytes = new Rfc2898DeriveBytes(PasswordHash, Encoding.ASCII.GetBytes(SaltKey)).GetBytes(256 / 8);
            var symmetricKey = new RijndaelManaged() { Mode = CipherMode.CBC, Padding = PaddingMode.Zeros };
            var encryptor = symmetricKey.CreateEncryptor(keyBytes, Encoding.ASCII.GetBytes(VIKey));

            byte[] cipherTextBytes;

            using (var memoryStream = new MemoryStream())
            {
                using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                    cryptoStream.FlushFinalBlock();
                    cipherTextBytes = memoryStream.ToArray();
                    cryptoStream.Close();
                }
                memoryStream.Close();
            }
            return Convert.ToBase64String(cipherTextBytes);
        }

        public static string Decrypt(string encryptedText)
        {
            byte[] cipherTextBytes = Convert.FromBase64String(encryptedText);
            byte[] keyBytes = new Rfc2898DeriveBytes(PasswordHash, Encoding.ASCII.GetBytes(SaltKey)).GetBytes(256 / 8);
            var symmetricKey = new RijndaelManaged() { Mode = CipherMode.CBC, Padding = PaddingMode.None };

            var decryptor = symmetricKey.CreateDecryptor(keyBytes, Encoding.ASCII.GetBytes(VIKey));
            var memoryStream = new MemoryStream(cipherTextBytes);
            var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
            byte[] plainTextBytes = new byte[cipherTextBytes.Length];

            int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
            memoryStream.Close();
            cryptoStream.Close();
            return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount).TrimEnd("\0".ToCharArray());
        }

        public static bool IsAuthorized(string page, string action)
        {
            bool isAuthorized = false;
            int IdPerm =  CheckPermissions(page, action);

            if (Session.SessionUser != null)
            {
                //isAuthorized = (Session.SessionUser.Permisos.Find(c => c.Id == IdPerm) is null ? false : true);
            }
            else
            {
                return false;
            }

            return isAuthorized;
        }

        public static bool IsAuthorized(Int32 IdPerm)
        {
            bool isAuthorized = false;
            //int IdPerm = CheckPermissions(page, action);

            if (Session.SessionUser != null)
            {
                //isAuthorized = (Session.SessionUser.Permisos.Find(c => c.Id == IdPerm) is null ? false : true);
            }
            else
            {
                isAuthorized = false;
            }

            return isAuthorized;
        }

        public static Int32 CheckPermissions(string page, string action)
        {
            Int32 IdPerm = 0;
            XElement root = XElement.Load(HttpContext.Current.Server.MapPath("~/Config/Security.xml"));

            foreach (XElement el in root.Elements(page).Elements(action))
            {
                IdPerm = Convert.ToInt32(el.Attribute("id").Value);
            }
            
            return IdPerm;
        }
    }
}