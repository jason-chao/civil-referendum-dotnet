using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReferendumData
{
    using System.Security.Cryptography;

    public class DataUtilities
    {
        static public string Hash_Salt { get { return "/* the salt */"; } }

        public enum GeneralValidationResult { Validated, IncorrectLength, NotNumber, NotDate, IncorrectCheckDigit, InvalidFormat, OtherReasons }

        static private Dictionary<char, int> letterMappingDictionary = null;

        static private Dictionary<char, int> LetterMap { get { if (letterMappingDictionary == null) { letterMappingDictionary = GetLetterMappingDictionary(); } return letterMappingDictionary; } }

        static private char[] possibleBIRNoFirstDigit = new char[] { '1', '5', '7' };

        static public string GenerateSMSVerificationCode()
        {
            Random rand = new Random(DateTime.Now.Millisecond);
            return rand.Next(100001, 999999).ToString();
        }
        
        static public GeneralValidationResult IsIDCardNumberValid(string ID_Number_Input)
        {
            try
            {
                try { Convert.ToInt64(ID_Number_Input); }
                catch { return GeneralValidationResult.NotNumber; }

                if (ID_Number_Input.Length == 9)
                {
                    if (possibleBIRNoFirstDigit.Contains(ID_Number_Input[0]))
                    {
                        if (GetCheckDigit(ID_Number_Input.Substring(0, 8)) == Convert.ToInt32(ID_Number_Input[8].ToString()))
                            return GeneralValidationResult.Validated;
                        else
                            return GeneralValidationResult.IncorrectCheckDigit;
                    }
                    else
                    {
                        return GeneralValidationResult.InvalidFormat;
                    }
                }
                else
                {
                    return GeneralValidationResult.IncorrectLength;
                }
            }
            catch { }

            return GeneralValidationResult.OtherReasons;
        }

        static public bool IsPhoneValid(string Phone)
        {
            if (!string.IsNullOrEmpty(Phone))
            {
                if (Phone.Length == 8)
                {
                    if (Phone.StartsWith("6"))
                    {
                        try
                        {
                            Convert.ToInt64(Phone);
                            return true;
                        }
                        catch { }
                    }
                }
            }

            return false;
        }

        static public GeneralValidationResult IsDateValid(string Date_Input)
        {
            try
            {
                if (Date_Input.Length == 7)
                {
                    if (GetCheckDigit(Date_Input.Substring(0, 6)) == Convert.ToInt32(Date_Input[6].ToString()))
                    {
                        try
                        {
                            UserInputStringToDate(Date_Input);
                            return GeneralValidationResult.Validated;
                        }
                        catch { return GeneralValidationResult.NotDate; }
                    }
                    else
                        return GeneralValidationResult.IncorrectCheckDigit;
                }
                else
                    return GeneralValidationResult.IncorrectLength;
            }
            catch { }

            return GeneralValidationResult.OtherReasons;
        }

        static public DateTime UserInputStringToDate(string Date_Input)
        {
            return new DateTime((1900 + Convert.ToInt32(Date_Input.Substring(0, 2))), Convert.ToInt32(Date_Input.Substring(2, 2)), Convert.ToInt32(Date_Input.Substring(4, 2)));
        }   
        
        static public int GetCheckDigit(string Letters)
        {
            int[] weights = new int[] { 7, 3, 1 };

            int product = 0;

            for (int i = 0; i < Letters.Length; i++)
            {
                char ch = Letters[i];
                int charNumber = 0;

                if (!Int32.TryParse(ch.ToString(), out charNumber))
                {
                    LetterMap.TryGetValue(ch, out charNumber);
                }

                product += (charNumber * weights[i % 3]);
            }

            return product % 10;
        }

        static private Dictionary<char, int> GetLetterMappingDictionary()
        {
            Dictionary<char, int> dict = new Dictionary<char, int>();

            for (int i = 0; i < 26; i++)
            {
                dict.Add(Convert.ToChar(0x41 + i), 10 + i);
            }

            return dict;
        }

        public static string GetSaltedHash(string Plaintext)
        {
            string newPlaintext = string.Empty; 

            Stack<char> plainTextStack = new Stack<char>();
            Stack<char> saltStack = new Stack<char>();

            if (!string.IsNullOrEmpty(Hash_Salt))
            {
                foreach(char c in Hash_Salt)
                {
                    saltStack.Push(c);
                }
            }

            if (!string.IsNullOrEmpty(Plaintext))
            {
                foreach (char c in Plaintext)
                {
                    plainTextStack.Push(c);
                }
            }

            while(plainTextStack.Any())
            {
                newPlaintext += plainTextStack.Pop();

                if (saltStack.Any())
                    newPlaintext += saltStack.Pop();
            }

            return GetHash(newPlaintext);
        }

        private static string GetHash(string Plaintext)
        {            
            SHA512 alg = SHA512.Create();
            byte[] result = alg.ComputeHash(Encoding.UTF8.GetBytes(Plaintext));
            return Convert.ToBase64String(result);
        }
    }


}
