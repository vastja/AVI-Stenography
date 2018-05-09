using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace AVIStenography {

    /// <summary>
    /// IO tools
    /// </summary>
    static class IOUtils {

        /// <summary>
        /// Loads given .avi file as byte array
        /// </summary>
        /// <param name="filePath">Path to .avi file</param>
        /// <returns>.avi file as byte array</returns>
        public static byte[] LoadAvi(string filePath) {
            try {
                byte[] avi = File.ReadAllBytes(filePath);
                ConsolePrintSuccess();
                Console.WriteLine($"{filePath} loading.");
                return avi;
            }
            catch (IOException) {
                ConsolePrintFailure();
                Console.WriteLine($"{filePath} loading.");
                return null;
            }
        }

        /// <summary>
        /// Loads text of given file
        /// </summary>
        /// <param name="filePath">Path to file</param>
        /// <returns>Text of given file</returns>
        public static string LoadMessage(string filePath) {
            try {
                string message = File.ReadAllText(filePath);
                ConsolePrintSuccess();
                Console.WriteLine($"{filePath} loading.");
                return message;
            }
            catch (IOException) {
                ConsolePrintFailure();
                Console.WriteLine($"{filePath} loading.");
                return null;
            }
        }

        /// <summary>
        /// Saves byte array into .avi file
        /// </summary>
        /// <param name="filePath">Path to .avi file</param>
        /// <param name="data">Byte array for saving</param>
        public static void SaveAvi(string filePath, byte[] data) {
            try {
                File.WriteAllBytes(filePath, data);
                ConsolePrintSuccess();
                Console.WriteLine($"Saving data to {filePath}.");
            }
            catch (IOException e) {
                ConsolePrintFailure();
                Console.WriteLine($"Saving data to {filePath}");
            }
        }

        public static void ConsolePrintSuccess() {
            Console.Write("[ ");
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Write("OK");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(" ] ");
        }

        public static void ConsolePrintFailure() {
            Console.Write("[ ");
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Write("ERROR");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(" ] ");
        }

        public static void ConsolePrintWarning() {
            Console.Write("[ ");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write("WARNING");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(" ] ");
        }

        //public static bool ConsoleOption(string text) {
        //    Console.WriteLine(text);

        //    Char key;
        //    do {
        //        Console.WriteLine("Press 'Y' to continue or 'N' to abort.");
        //        key = Console.ReadKey().KeyChar;
        //    } while (!(key == 'y' || key == 'n' || key == 'Y' || key == 'N'));
        //    Console.WriteLine();

        //    return key == 'y' || key == 'Y' ? true : false;
        //}

    }

}
