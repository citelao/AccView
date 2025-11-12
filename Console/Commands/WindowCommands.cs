using Windows.Win32;
using Windows.Win32.Foundation;

namespace Console.Commands
{
    public class WindowCommands
    {
        public static unsafe void ListWindows()
        {
            PInvoke.EnumWindows((hwnd, lParam) =>
            {
                const int maxLength = 256;
                Span<char> buffer = stackalloc char[maxLength];

                fixed (char* pBuffer = buffer)
                {
                    var pwstr = new PWSTR(pBuffer);
                    int length = PInvoke.GetWindowText(hwnd, pwstr, maxLength);
                    string title = length > 0 ? new string(pBuffer, 0, length) : string.Empty;
                    Console.WriteLine($"HWND: 0x{hwnd:X}, Title: '{title}'");
                }

                return true; // continue enumeration
            }, IntPtr.Zero);

        }
    }
}
