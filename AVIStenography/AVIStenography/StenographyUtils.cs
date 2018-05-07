using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVIStenography {

    static class StenogrpahyUtils {

        public static string HideData(byte[] avi, int chunkDataStartIndex, int chunkDataSize, string message) {

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

        public static bool ExtractData(byte[] avi, int chunkDataStartIndex, int chunkDataSize, string message) {

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
