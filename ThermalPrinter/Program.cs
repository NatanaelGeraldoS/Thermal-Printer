using System;
using System.IO;
using System.Net;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Drawing;
using ThermalPrinter.Helpers;

namespace ThermalPrinterTesting
{
    class Program
    {
        static void Main(string[] args)
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:8080/print/");
            listener.Start();
            Console.WriteLine("Listening on localhost:8080/print/...");

            while (true)
            {
                HttpListenerContext context = listener.GetContext();
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                try
                {
                    string printerName = "PDFCreator";
                    using (SafeFileHandle printerHandle = PrinterHelper.OpenPrinter(printerName))
                    using (FileStream printerAsFile = new FileStream(printerHandle, FileAccess.ReadWrite))
                    {
                        printerAsFile.WriteByte(0x1b);
                        printerAsFile.WriteByte(0x40);

                        string imageUrl = "https://bssschoolstorage.blob.core.windows.net/floor/Binus%20School%20Logo.bmp"; // Replace with your actual image URL
                        //PrintImageFromUrl(printerAsFile, imageUrl);

                        PrintHeader(printerAsFile);

                        PrintClinicPassDetails(printerAsFile);

                        PrinterHelper.CutPaper(printerAsFile);
                    }

                    string responseString = "Printing triggered successfully!";
                    byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    string responseString = $"Error: {ex.Message}";
                    byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                }
                finally
                {
                    response.OutputStream.Close();
                }
            }
        }

        private static async void PrintImageFromUrl(FileStream printerAsFile, string imageUrl)
        {
            using (var httpClient = new HttpClient())
            {
                byte[] imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
                using (var ms = new MemoryStream(imageBytes))
                using (var image = Image.FromStream(ms))
                {
                    // Resize image if needed
                    int maxWidth = 384; // Adjust based on your printer's capabilities
                    int newHeight = (int)((float)image.Height / image.Width * maxWidth);
                    var resizedImage = new Bitmap(image, new Size(maxWidth, newHeight));

                    // Convert image to black and white
                    var blackAndWhiteImage = ConvertToBlackAndWhite(resizedImage);

                    // Print the image
                    PrintBitmap(printerAsFile, blackAndWhiteImage);
                }
            }
        }

        private static Bitmap ConvertToBlackAndWhite(Bitmap original)
        {
            var blackAndWhite = new Bitmap(original.Width, original.Height);
            for (int x = 0; x < original.Width; x++)
            {
                for (int y = 0; y < original.Height; y++)
                {
                    Color originalColor = original.GetPixel(x, y);
                    int grayScale = (int)((originalColor.R * 0.3) + (originalColor.G * 0.59) + (originalColor.B * 0.11));
                    Color newColor = grayScale > 128 ? Color.White : Color.Black;
                    blackAndWhite.SetPixel(x, y, newColor);
                }
            }
            return blackAndWhite;
        }

        private static void PrintBitmap(FileStream printerAsFile, Bitmap bitmap)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;

            // ESC * command
            printerAsFile.WriteByte(0x1B);
            printerAsFile.WriteByte(0x2A);
            printerAsFile.WriteByte(33); // 24-dot double-density
            printerAsFile.WriteByte((byte)(width % 256));
            printerAsFile.WriteByte((byte)(width / 256));

            for (int y = 0; y < height; y += 24)
            {
                for (int x = 0; x < width; x++)
                {
                    int b = 0;
                    for (int n = 0; n < 24; n++)
                    {
                        if (y + n < height)
                        {
                            Color pixelColor = bitmap.GetPixel(x, y + n);
                            if (pixelColor.R == 0) // Black pixel
                            {
                                b |= 1 << n;
                            }
                        }
                    }
                    printerAsFile.WriteByte((byte)b);
                    printerAsFile.WriteByte((byte)(b >> 8));
                    printerAsFile.WriteByte((byte)(b >> 16));
                }
                printerAsFile.WriteByte(10); // Line feed
            }
        }

        private static void PrintHeader(FileStream printerAsFile)
        {
            // Print the centered header
            string header = "\n\nClinic Pass\n====================\n";
            printerAsFile.Write(Encoding.ASCII.GetBytes(header), 0, header.Length);

            // Reset alignment to left (default)
            byte[] leftAlign = new byte[] { 0x1B, 0x61, 0x00 };
            printerAsFile.Write(leftAlign, 0, leftAlign.Length);
        }

        private static void PrintClinicPassDetails(FileStream printerAsFile)
        {
            string name = "Wira Nugraha";
            string studentId = "123412312312";
            string grade = "7D";
            string datePrinted = DateTime.Now.ToString("ddd, dd MMM yyyy");
            string checkInTime = "10.00";
            string checkOutTime = "12.00";
            string printedBy = "Yohanes Damenta";

            string details = $"Name            : {name}\n" +
                             $"StudentID/Grade : {studentId} / {grade}\n" +
                             $"Printed On      : {datePrinted}\n" +
                             $"Check-in Time   : {checkInTime}\n" +
                             $"Check-out Time  : {checkOutTime}\n" +
                             $"Printed By      : {printedBy}\n";

            // Print details to the printer
            printerAsFile.Write(Encoding.ASCII.GetBytes(details), 0, details.Length);

            // Add a line break or advance the paper (optional)
            printerAsFile.WriteByte(0x1b); // ESC
            printerAsFile.WriteByte(Convert.ToByte('d'));
            printerAsFile.WriteByte(Convert.ToByte(3));
        }
    }
}
