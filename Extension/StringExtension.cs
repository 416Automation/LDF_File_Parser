using System.Collections;

namespace LDF_File_Parser.Extension
{

    public static class StringExtension
    {
        /// <summary>Converts to hexadecimal in the format "0x".</summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///   <br />
        /// </returns>
        public static string ConvertToHex(this string value)
        {
            if (int.TryParse(value, out int number))
                return string.Format("0x{0:X}", number.ToString("X2"));
            else
                // Assume it is already hexadecimal 
                return value;

            //int value = (int)new System.ComponentModel.Int32Converter().ConvertFromString(hexString);
        }

    }
}
