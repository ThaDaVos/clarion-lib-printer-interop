namespace ArjSoftware.PrinterInterop
{

    using System;
    using System.Runtime.InteropServices;
    using RawPrint.NetStd;
    using System.IO;
    using System.Linq;
    using System.Drawing.Printing;
    using System.Runtime.CompilerServices;

    public static class External
    {
        private static string ResolvePrinterName(string name)
        {
            string baseName = name.Substring(0, name.LastIndexOf('('));

            while (baseName.Contains('('))
            {
                foreach (string printer in PrinterSettings.InstalledPrinters)
                {
                    if (printer.StartsWith(baseName))
                    {
                        return printer;
                    }
                }

                baseName = baseName.Substring(0, baseName.LastIndexOf('('));
            }

            return name;
        }

        private static byte[] ParseCommand([MarshalAs(UnmanagedType.LPStr)] string command)
        {
            string[] split = command.Split(' ', '-');

            return split.Select(s => Convert.ToByte(s)).ToArray();
        }

        [UnmanagedCallersOnly(EntryPoint = "openDrawer", CallConvs = new[] { typeof(CallConvCdecl) })]
        public static int OpenDrawerProcedure(
            IntPtr pName,
            IntPtr pCommand
        ) {
            try
            {
                string? name = Marshal.PtrToStringAnsi(pName);
                string? command = Marshal.PtrToStringAnsi(pCommand);

                if (name == null || command == null)
                {
                    return 400;
                }

                // 1. Resolve full printer name
                name = ResolvePrinterName(name);
                // 2. Convert command to bytes
                byte[] kickBytes = ParseCommand(command);
                // 3. Create ASCII character string
                // string charString = ASCIIEncoding.ASCII.GetString(kickBytes);
                // 4. Create Stream from string
                using var stream = new MemoryStream(kickBytes);
                // 5. Instantiate Printer
                IPrinter printer = new Printer();
                // 6. Send raw stream to printer to kick drawer
                printer.PrintRawStream(name, stream, @"ArjSoftware.CashDrawerKicker");

                return 0;
            }
            catch (Exception exception)
            {
                return 500;
            }
        }
    }
}