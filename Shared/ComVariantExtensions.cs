using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Windows.Win32.System.Com;

namespace Shared
{
    public static class ComVariantExtensions
    {
        public static SAFEARRAY AsSafeArray(this ComVariant comVariant)
        {
            // VT_ARRAY is often a flag combined with other types (e.g. VT_I4 | VT_ARRAY).
            var isArray = (comVariant.VarType & VarEnum.VT_ARRAY) == VarEnum.VT_ARRAY;
            if (!isArray)
            {
                throw new InvalidCastException("ComVariant is not a SAFEARRAY.");
            }

            var pointer = comVariant.GetRawDataRef<nint>();
            var safeArray = Marshal.PtrToStructure<SAFEARRAY>(pointer);
            return safeArray;
        }

        public static T[] AsArray<T>(this ComVariant comVariant) where T : unmanaged
        {
            // Validate the parameter
            if (typeof(T) == typeof(int))
            {
                ThrowIfNotVarType(GetSafeArrayType(comVariant), VarEnum.VT_I4, VarEnum.VT_INT);
            }
            else
            {
                // TODO: other types!
                throw new ArgumentException($"Unsupported type {typeof(T)} for AsArray.");
            }

            var safeArray = comVariant.AsSafeArray();
            var result = SafeArrayHelpers.ToArray<T>(safeArray);
            return result;
        }

        private static VarEnum GetSafeArrayType(ComVariant comVariant)
        {
            var remainder = comVariant.VarType & ~VarEnum.VT_ARRAY;
            return remainder;
        }

        private static void ThrowIfNotVarType(VarEnum varEnum, params VarEnum[] acceptable)
        {
            if (!acceptable.Contains(varEnum))
            {
                throw new InvalidCastException($"ComVariant {varEnum} is not of expected VarEnum type(s): {string.Join(", ", acceptable)}");
            }
        }
    }
}
