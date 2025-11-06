using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32;

namespace Shared
{
    public class CursorHelpers
    {
        /// <summary>
        /// Get the current mouse cursor position
        /// </summary>
        /// <returns>current mouse cursor position</returns>
        public static Point GetCursorPosition()
        {
            PInvokeAcc.GetCursorPos(out var lpPoint);
            return new Point(lpPoint.X, lpPoint.Y);
        }
    }
}
