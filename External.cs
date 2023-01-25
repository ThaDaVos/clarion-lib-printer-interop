namespace ArjSoftware.PrinterInterop
{

    using System;
    using System.Runtime.InteropServices;
    using RawPrint.NetStd;
    using System.IO;
    using System.Linq;
    using System.Drawing.Printing;
    using System.Runtime.CompilerServices;
    using System.Collections.Generic;

    public static class External
    {
        private static string? SearchForPrinter(string name)
        {
            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                if (printer.StartsWith(name))
                {
                    return printer;
                }
            }

            return null;
        }

        private static string ResolvePrinterName(string name)
        {
            string? printer = SearchForPrinter(name);

            if (printer != null)
            {
                return printer;
            }

            string workingName = name.Substring(0, name.LastIndexOf('('));

            while (true)
            {
                printer = SearchForPrinter(workingName);

                if (printer != null)
                {
                    return printer;
                }

                if (workingName.Contains('('))
                {
                    workingName = workingName.Substring(0, workingName.LastIndexOf('('));
                }
                else
                {
                    return name;
                }
            }
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
        )
        {
            List<string> lines = new List<string>();

            try
            {
                string? name = Marshal.PtrToStringBSTR(pName);
                string? command = Marshal.PtrToStringBSTR(pCommand);

                if (name == null)
                {
                    lines.Add($"[{DateTime.Now.ToString("dd-MM-yyyy|HH:mm:ss")}] | Name could not be marshalled");
                    return 400;
                }

                if (command == null)
                {
                    lines.Add($"[{DateTime.Now.ToString("dd-MM-yyyy|HH:mm:ss")}] | Command could not be marshalled");
                    return 400;
                }

                lines.Add($"[{DateTime.Now.ToString("dd-MM-yyyy|HH:mm:ss")}] | Printer({name}) : Command({command})");

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

                lines.Add($"[{DateTime.Now.ToString("dd-MM-yyyy|HH:mm:ss")}] | Opened cashdrawer");
                return 0;
            }
            catch (Exception exception)
            {
                lines.Add($"An exception occured {exception.Message}");
                return 500;
            }
            finally
            {
                if (lines.Count > 0)
                {
                    try
                    {
                        using StreamWriter file = new("printerop.log", append: true);
                        lines.ForEach(line => file.WriteLine(line));
                    }
                    catch (Exception exception) { }
                }
            }
        }
    }
}