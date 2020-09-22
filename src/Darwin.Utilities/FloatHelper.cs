using System;
using System.Collections.Generic;
using System.Text;

namespace Darwin.Utilities
{
    public static class FloatHelper
    {
        public static byte[] ConvertToByteArray(float[] array)
        {
            var byteArray = new byte[array.Length * sizeof(float)];
            Buffer.BlockCopy(array, 0, byteArray, 0, byteArray.Length);
            return byteArray;
        }

        public static float[] ConvertFromByteArray(byte[] byteArray)
        {
            var array = new float[byteArray.Length / sizeof(float)];
            Buffer.BlockCopy(byteArray, 0, array, 0, byteArray.Length);

            return array;
        }

        public static string ConvertToBase64String(float[] array)
        {
            var byteArray = ConvertToByteArray(array);
            return Convert.ToBase64String(byteArray);
        }

        public static float[] ConvertFromBase64String(string base64String)
        {
            var byteArray = Convert.FromBase64String(base64String);
            return ConvertFromByteArray(byteArray);
        }
    }
}
