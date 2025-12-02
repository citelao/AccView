using Windows.Win32;
using Windows.Win32.System.Com;
using Windows.Win32.System.Variant;

namespace Shared.UnitTests;

public class SafeArrayHelpersTests
{
    [Fact]
    public unsafe void ConvertsSafeArrayOfIntsToManagedArray()
    {
        // Create a SAFEARRAY of ints with values 1, 2, 3, 4, 5
        SAFEARRAYBOUND sab = new SAFEARRAYBOUND
        {
            cElements = 5,
            lLbound = 0
        };

        SAFEARRAY* psa = PInvoke.SafeArrayCreate(VARENUM.VT_I4, 1, &sab);
        try
        {
            for (int i = 0; i < 5; i++)
            {
                int value = i + 1;
                PInvoke.SafeArrayPutElement(psa, i, &value).ThrowOnFailure();
            }

            // Convert to managed array
            int[] result = SafeArrayHelpers.ToArray<int>(psa);

            // Verify the result
            Assert.Equal(new int[] { 1, 2, 3, 4, 5 }, result);
        }
        finally
        {
            // Clean up
            PInvoke.SafeArrayDestroy(psa);
        }
    }
}