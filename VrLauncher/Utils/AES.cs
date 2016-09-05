using System;
using System.Text;

public class AES
{

    private const string _key = "1994111012345678";

    /// <summary>
    ///  AES 加密
    /// </summary>
    /// <param name="str"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static byte[] AesEncrypt(byte[] str, int count_, int index_ = 0, string key = _key)
    {
        // *remark*: ignore
        //if (string.IsNullOrEmpty(Encoding.UTF8.GetString(str, index_, count_))) return null;
        //Byte[] toEncryptArray = str;
        byte[] toEncryptArray = new byte[count_];
        Array.Copy(str, index_, toEncryptArray, 0, count_);

        System.Security.Cryptography.RijndaelManaged rm = new System.Security.Cryptography.RijndaelManaged
        {
            Key = Encoding.ASCII.GetBytes(key),
            Mode = System.Security.Cryptography.CipherMode.ECB,
            Padding = System.Security.Cryptography.PaddingMode.PKCS7
        };

        System.Security.Cryptography.ICryptoTransform cTransform = rm.CreateEncryptor();
        Byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
        //return resultArray;

        string ret = Convert.ToBase64String(resultArray, 0, resultArray.Length);
        //UnityEngine.Debug.Log("<b>encrypted</b>:" + ret);
        //return Encoding.Default.GetBytes(ret);
        return Encoding.ASCII.GetBytes(ret);
    }

    /// <summary>
    ///  AES 解密
    /// </summary>
    /// <param name="str"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static byte[] AesDecrypt(byte[] str, int count_, int index_ = 0, string key = _key)
    {
        // *remark*: ignore
        //if (string.IsNullOrEmpty(Encoding.UTF8.GetString(str, index_, count_))) return null;
        //Byte[] toEncryptArray = Convert.FromBase64String(Encoding.UTF8.GetString(str, index_, count_));
        Byte[] toEncryptArray = Convert.FromBase64String(Encoding.ASCII.GetString(str, index_, count_));

        System.Security.Cryptography.RijndaelManaged rm = new System.Security.Cryptography.RijndaelManaged
        {
            Key = Encoding.ASCII.GetBytes(key),
            Mode = System.Security.Cryptography.CipherMode.ECB,
            Padding = System.Security.Cryptography.PaddingMode.PKCS7
        };

        System.Security.Cryptography.ICryptoTransform cTransform = rm.CreateDecryptor();
        Byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

        return resultArray;
    }
}
