using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.System.Com;

namespace Shared
{
    public class SafeArrayHelpers
    {
        /// <summary>
        /// Helper function to convert a SAFEARRAY of unmanaged types to a managed array.
        /// 
        /// e.g. int[] result = ToArray<int>(&safeArray);
        /// </summary>
        public unsafe static T[] ToArray<T>(SAFEARRAY* safeArray) where T : unmanaged
        {
            PInvokeAcc.SafeArrayGetLBound(safeArray, 1, out int lbound).ThrowOnFailure();
            PInvokeAcc.SafeArrayGetUBound(safeArray, 1, out int ubound).ThrowOnFailure();

            var count = ubound - lbound + 1;
            var result = new T[count];
            for (int i = lbound; i <= ubound; i++)
            {
                T value;
                PInvokeAcc.SafeArrayGetElement(safeArray, i, &value).ThrowOnFailure();
                result[i - lbound] = value;
            }

            return result;
        }

        public static T[] ToArray<T>(SAFEARRAY safeArray) where T : unmanaged
        {
            unsafe
            {
                return ToArray<T>(&safeArray);
            }
        }
    }
}
