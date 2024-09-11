using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ThermalPrinter.Helpers
{
    public static class PrinterHelper
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern SafeFileHandle CreateFile(string lpFileName, FileAccess dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, FileMode dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        public static SafeFileHandle OpenPrinter(string printerName)
        {
            string fullPrinterLocation = $"\\\\{Environment.MachineName}\\{printerName}";

            SafeFileHandle fh = CreateFile(fullPrinterLocation, FileAccess.Write, 0, IntPtr.Zero, FileMode.OpenOrCreate, 0, IntPtr.Zero);

            if (fh.IsInvalid)
            {
                throw new InvalidOperationException("Error opening printer");
            }

            return fh;
        }

        public static void CutPaper(FileStream printerAsFile)
        {
            printerAsFile.WriteByte(0x1d);
            printerAsFile.WriteByte(0x56);
            printerAsFile.WriteByte(66);
            printerAsFile.WriteByte(3);
        }
    }
}
