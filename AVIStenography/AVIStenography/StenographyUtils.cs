using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVIStenography {

    static class StenogrpahyUtils {

        public static void HideMessage(AVIFileHandler avifh, string message) {

            var info = avifh.GetVideoStreamInfo();
            if (info.Item1.cb == 0) {
                IOUtils.ConsolePrintFailure();
                Console.WriteLine("AVI file does not contain video stream header. Execution ABBORTED.");
                Program.Exit(-1);
            }

            IOUtils.ConsolePrintSuccess();
            Console.WriteLine("Start seaching in AVI file for CHUNKS.");
            avifh.Search();
            IOUtils.ConsolePrintSuccess();
            Console.WriteLine("Searching in AVI file completed.");

            Int32 junkSize = avifh.GetJunkChunksSize();
            IOUtils.ConsolePrintSuccess();
            Console.WriteLine($"Available free junk space: {junkSize}B");

            if (IOUtils.ConsoleOption("Do you want to use JUNK CHUNKS?")) {
                IOUtils.ConsolePrintSuccess();
                Console.WriteLine("Start writing to JUNK CHUNKS.");
                foreach (KeyValuePair<Int32, CHUNK> kvp in avifh.Junks) {
                    if (message.Length > 0) {
                        message = HideData(avifh.Avi, kvp.Key, kvp.Value.ckSize, message);
                    }
                }
                IOUtils.ConsolePrintSuccess();
                Console.WriteLine("Writing to JUNK CHUNKS completed.");
            }

            IOUtils.Save("JUNK-TEST.avi",avifh.Avi);

        }

        //public string ExtractMessage(AVIFileHandler avifh) {

        //    string message;

        //}

        private static string HideData(byte[] avi, int chunkDataStartIndex, int chunkDataSize, string message) {

            byte letter, code;

            int limit = chunkDataSize - chunkDataSize % 8 - 8;

            int index = 0, letterIndex = 0;
            while (index < limit && letterIndex < message.Length) {

                letter = (byte)message[letterIndex];
                for (int i = 0; i < 8; i++) {
                    code = (byte)((letter >> 7 - i) & 1);
                    avi[chunkDataStartIndex + index] = (byte)(avi[chunkDataStartIndex + index] & 254 | code);
                    index++;
                }
                letterIndex++;
            }

            return message.Substring(letterIndex);

        }

        private static bool ExtractData(byte[] avi, int chunkDataStartIndex, int chunkDataSize, string message) {

            StringBuilder sb = new StringBuilder();

            int limit = chunkDataSize - chunkDataSize % 8;

            byte letter;

            int index = 0;
            while (index < limit) {

                letter = 0;
                for (int i = 0; i < 8; i++) {
                    letter = (byte)((letter << 1) | (avi[chunkDataStartIndex + index] & 1));
                    index++;
                }

                sb.Append((char)letter);

                if (letter == 0x3) {
                    message += sb.ToString();
                    return true;
                }

            }

            message += sb.ToString();
            return false;

        }

    }
}
