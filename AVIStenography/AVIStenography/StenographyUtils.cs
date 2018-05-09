using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVIStenography {

    static class StenogrpahyUtils {


        private static char MESSAGE_END = (char) 0x03;

        public static void HideMessage(AVIFileHandler avifh, string message, bool force, IEnumerable<Options.DataTypes> hideTo) {

            (Int32, Int32, Int32, Int32) chunkSizes = GetStreamsAvailableChunksSize(avifh);
            Console.WriteLine($"Available free junk space: {chunkSizes.Item1}B");
            Console.WriteLine($"Available free video space: {chunkSizes.Item2 + chunkSizes.Item3}B");
            Console.WriteLine($"Available free audio space: {chunkSizes.Item4}B");

            message += MESSAGE_END;
            //TODO check if there is enough space
            foreach (Options.DataTypes type in hideTo) {

                Dictionary<Int32, CHUNK> chunks;

                if (type == Options.DataTypes.vids && avifh.AviVideoStreamInfo.Item1.dwQuality != 10000) {
                    IOUtils.ConsolePrintWarning();
                    if (force) {
                        Console.WriteLine("Writing to compressed video stream");
                    }
                    else {
                        Console.WriteLine("Video stream was skipped due its compression. Writing to compressed video stream can be enable with --force flag.");
                        continue;
                    }
                }

                    if (avifh.Chunks.TryGetValue(AVIFileHandler.GetChunkName(type), out chunks)) {

                        foreach (KeyValuePair<Int32, CHUNK> kvp in chunks) {
                            message = HideData(avifh.Avi, kvp.Key, kvp.Value.ckSize, message);
                            if (message == null) {
                                return;
                            }
                        }

                    }

            }
            
        }

        public static string ExtractMessage(AVIFileHandler avifh, IEnumerable<Options.DataTypes> extractFrom) {

            string message = "";
            bool EndReached = false;
            foreach (Options.DataTypes type in extractFrom) {

                Dictionary<Int32, CHUNK> chunks;
                if (avifh.Chunks.TryGetValue(AVIFileHandler.GetChunkName(type), out chunks)) {
                    foreach (KeyValuePair<Int32, CHUNK> kvp in chunks) {
                        EndReached = ExtractData(avifh.Avi, kvp.Key, kvp.Value.ckSize, ref message);
                        if (EndReached) {
                            return message;
                        }
                    }
                }
            }

            return null;

        }

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

        private static bool ExtractData(byte[] avi, int chunkDataStartIndex, int chunkDataSize, ref string message) {

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

                if (letter == MESSAGE_END) {
                    message += sb.ToString();
                    return true;
                }

                sb.Append((char)letter);

            }

            message += sb.ToString();
            return false;

        }

        private static int GetAvailableSize(Dictionary<Int32, CHUNK> chunks) {
            int size = 0;
            foreach (CHUNK chunk in chunks.Values) {
                size += chunk.ckSize - chunk.ckSize % 8;
            }
            return size;
        }

        public static (int, int, int, int) GetStreamsAvailableChunksSize(AVIFileHandler avifh) {
            Int32 junkSize = 0, vidsCompressSize = 0, vidsUncompressedSize = 0, audsSize = 0;

            Dictionary<Int32, CHUNK> chunkType;
            if (avifh.Chunks.TryGetValue(AVIFileHandler.JUNK_CHUNK, out chunkType)) {
                junkSize = GetAvailableSize(chunkType);
            }
            if (avifh.Chunks.TryGetValue(AVIFileHandler.AUDIO_CHUNK, out chunkType)) {
                audsSize = GetAvailableSize(chunkType);
            }
            if (avifh.Chunks.TryGetValue(AVIFileHandler.VIDEO_CHUNK_COMPRESSED, out chunkType)) {
                vidsCompressSize = GetAvailableSize(chunkType);
            }
            if (avifh.Chunks.TryGetValue(AVIFileHandler.VIDEO_CHUNK_UNCOMPRESSED, out chunkType)) {
                vidsUncompressedSize = GetAvailableSize(chunkType);
            }

            return (junkSize, vidsUncompressedSize, vidsCompressSize, audsSize);
        }

    }
}
