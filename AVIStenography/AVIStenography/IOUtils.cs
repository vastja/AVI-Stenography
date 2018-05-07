using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace AVIStenography {

    static class IOUtils {

        public static byte[] Load(string filePath) {
            try {
                byte[] avi = File.ReadAllBytes(filePath);
                ConsolePrintSuccess();
                Console.WriteLine($"{filePath} loading.");
                return avi;
            }
            catch (IOException e) {
                ConsolePrintFailure();
                Console.WriteLine($"{filePath} loading.");
                return null;
            }
        }

        public static void Save(string filePath, byte[] data) {
            try {
                File.WriteAllBytes(filePath, data);
                ConsolePrintSuccess();
                Console.WriteLine($"Saving data to {filePath}.");
            }
            catch (IOException e) {
                ConsolePrintFailure();
                Console.WriteLine($"Saving data to {filePath}");

                ConsolePrintWarning();
                Console.WriteLine($"Saving data to current directory as TEMP.avi");
                File.WriteAllBytes("TEMP.avi", data);
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

    }

}
