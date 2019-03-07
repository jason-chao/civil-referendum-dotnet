using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReferendumData
{
    using System.Security.Cryptography;

    /// <summary>
    /// Common data validation functionality 
    /// </summary>
    public class DataUtilities
    {
        /// <summary>
        /// The salt for hashing - the real implementation is removed here for security consideration
        /// </summary>
        static public string Hash_Salt { get { return "/* the salt */"; } }

        /// <summary>
        /// Types of result of Macau ID number validation
        /// </summary>
        public enum GeneralValidationResult { Validated, IncorrectLength, NotNumber, NotDate, IncorrectCheckDigit, InvalidFormat, OtherReasons }

        /// <summary>
        /// Character-to-number map for the ICAO Machine Readable Zone (MRZ) check digit algorithm
        /// </summary>
        static private Dictionary<char, int> letterMappingDictionary = null;

        /// <summary>
        /// Character-to-number map for the ICAO Machine Readable Zone (MRZ) check digit algorithm
        /// </summary>
        static private Dictionary<char, int> LetterMap { get { if (letterMappingDictionary == null) { letterMappingDictionary = GetLetterMappingDictionary(); } return letterMappingDictionary; } }

        /// <summary>
        /// 1, 5 and 7 are the only possible digits for Macau ID card numbers.  See Article 12 of Decree-law no. 19/99/M of Macau.
        /// </summary>
        static private char[] possibleBIRNoFirstDigit = new char[] { '1', '5', '7' };

        /// <summary>
        /// Generate a random number for SMS verification
        /// </summary>
        static public string GenerateSMSVerificationCode()
        {
            Random rand = new Random(DateTime.Now.Millisecond);
            return rand.Next(100001, 999999).ToString();
        }
        
        /// <summary>
        /// Check whether a Macau ID card number is valid.  
        /// </summary>
        static public GeneralValidationResult IsIDCardNumberValid(string ID_Number_Input)
        {
            try
            {
                // Check if the ID is a number (int64)
                try { Convert.ToInt64(ID_Number_Input); }
                catch { return GeneralValidationResult.NotNumber; }

                // The length of a real Macau ID number is 8.  
                // Here, the 9th digit (the check digit) on the reverse of the ID card is defined by ICAO standards.
                if (ID_Number_Input.Length == 9)
                {
                    // The first digit must be 1, 5 or 7.    See Article 12 of Decree-law no. 19/99/M of Macau.
                    if (possibleBIRNoFirstDigit.Contains(ID_Number_Input[0]))
                    {
                        // The ID number is considered valid if the ICAO check digit is correct.
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

        /// <summary>
        /// Check whether the input is a correctly-formatted Macau phone number
        /// </summary>
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

        /// <summary>
        /// Check whether the input is a valid date
        /// </summary>
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

        /// <summary>
        /// Convert a string input to DateTime
        /// </summary>
        static public DateTime UserInputStringToDate(string Date_Input)
        {
            return new DateTime((1900 + Convert.ToInt32(Date_Input.Substring(0, 2))), Convert.ToInt32(Date_Input.Substring(2, 2)), Convert.ToInt32(Date_Input.Substring(4, 2)));
        }   
        
        /// <summary>
        /// Get the ICAO Machine Readable Zone (MRZ) check digit of a string input
        /// </summary>
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

        /// <summary>
        /// Generate a character-to-number map for the ICAO Machine Readable Zone (MRZ) check digit algorithm
        /// </summary>
        static private Dictionary<char, int> GetLetterMappingDictionary()
        {
            Dictionary<char, int> dict = new Dictionary<char, int>();

            for (int i = 0; i < 26; i++)
            {
                dict.Add(Convert.ToChar(0x41 + i), 10 + i);
            }

            return dict;
        }

        /// <summary>
        /// Blend salt into the plaintext before hashing.  Non-standard implentation is intended for added obscurity.
        /// </summary>
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

        /// <summary>
        /// Get the SHA512 hash value of a string
        /// </summary>
        private static string GetHash(string Plaintext)
        {            
            SHA512 alg = SHA512.Create();
            byte[] result = alg.ComputeHash(Encoding.UTF8.GetBytes(Plaintext));
            return Convert.ToBase64String(result);
        }
    }


}
