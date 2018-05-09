using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace AVIStenography {

    static class IOUtils {

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

        public static bool ConsoleOption(string text) {
            Console.WriteLine(text);

            Char key;
            do {
                Console.WriteLine("Press 'Y' to continue or 'N' to abort.");
                key = Console.ReadKey().KeyChar;
            } while (!(key == 'y' || key == 'n' || key == 'Y' || key == 'N'));
            Console.WriteLine();

            return key == 'y' || key == 'Y' ? true : false;
        }

        public static void ConsolePrintHelp() {

            Console.WriteLine("Usage: avis.exe < <-E> | <-H> > <file_path.avi> [-f] <message> [--force] ");
            Console.WriteLine();
            Console.WriteLine("Options: ");
            Console.WriteLine("-E\t\tExtract hidden message.\n-H\t\tHide message.\n-f\t\tMessage will be loaded from file.\n--force\t\tHide message even if avi file is compressed.");

        }

    }

}
