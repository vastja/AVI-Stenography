using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVIStenography {

    /// <summary>
    /// Stenography tools
    /// </summary>
    static class StenogrpahyUtils {

        /// <summary>
        /// End sequence for end message detection for extracting tool
        /// </summary>
        public static readonly char MESSAGE_END = (char) 0x03;


        /// <summary>
        /// Hides message into .avi file
        /// </summary>
        /// <param name="avifh">.avi file handler</param>
        /// <param name="message">String to hide</param>
        /// <param name="force">Hide into compressed streams</param>
        /// <param name="hideTo">List of streams the message will be hidden into (in given order)</param>
        /// <returns>True if message was hide successfuly else false</returns>
        public static bool HideMessage(AVIFileHandler avifh, string message, bool force, IEnumerable<Options.DataTypes> hideTo) {
  
            message += MESSAGE_END;
            
            if (!CheckAvailableSpace(avifh, message, hideTo)) {
                IOUtils.ConsolePrintFailure();
                Console.WriteLine("There is not enough space for message");
                return false;
            }
            else {
                IOUtils.ConsolePrintSuccess();
                Console.WriteLine("Space check.");
            }

            foreach (Options.DataTypes type in hideTo) {

                Dictionary<Int32, CHUNK> chunks;

                if (CheckCompression(avifh.IsStreamUncompressed(type), force, type)) {
                    if (avifh.Chunks.TryGetValue(AVIFileHandler.GetChunkName(type), out chunks)) {

                        foreach (KeyValuePair<Int32, CHUNK> kvp in chunks) {
                            message = HideData(avifh.Avi, kvp.Key, kvp.Value.ckSize, message);
                            if (message == String.Empty) {
                                return true;
                            }
                        }

                    }
                }

            }

            return false;
            
        }

        /// <summary>
        /// Extracts hidden message from .avi file
        /// </summary>
        /// <param name="avifh">.avi file handler</param>
        /// <param name="extractFrom">List of streams the message will be extracted from (in given order)</param>
        /// <returns>Extracted message</returns>
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

        /// <summary>
        /// Hide part of message (as much as possible) into given chunk
        /// </summary>
        /// <param name="avi">.avi file handler</param>
        /// <param name="chunkDataStartIndex">Index of data part of chunk</param>
        /// <param name="chunkDataSize">Chunk's data size</param>
        /// <param name="message">Message to hide</param>
        /// <returns>Rest of message (in case message is larger then data chunk's size)</returns>
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

        /// <summary>
        /// Extract part of message (as much as possible) from given chunk
        /// </summary>
        /// <param name="avi">.avi file handler</param>
        /// <param name="chunkDataStartIndex">Index of data part of chunk</param>
        /// <param name="chunkDataSize">Chunk's data size</param>
        /// <param name="message">Already extracted message</param>
        /// <returns>True in case end of message (MESSAGE_END) was reached else false</returns>
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

        /// <summary>
        /// Counts available space for data hiding of given avi stream type
        /// </summary>
        /// <param name="chunks">Chunks of one avi stream type</param>
        /// <returns>Available space in bytes</returns>
        private static int GetAvailableSize(Dictionary<Int32, CHUNK> chunks) {
            int size = 0;
            foreach (CHUNK chunk in chunks.Values) {
                size += chunk.ckSize - chunk.ckSize % 8;
            }
            return size;
        }

        /// <summary>
        /// Counts available space for data hiding of all supported avi stream types
        /// </summary>
        /// <param name="avifh">.avi file handler</param>
        /// <returns>(Junk available size, Audio available size, Compressed video available size, Uncompressed video available size)</returns>
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

        /// <summary>
        /// Checks if there is enough free space to hide message
        /// </summary>
        /// <param name="avifh">.avi file handler</param>
        /// <param name="message">Message to hide</param>
        /// <param name="streams">List of stream for message hiding</param>
        /// <returns>true if there is enough space for message hiding else false</returns>
        private static bool CheckAvailableSpace(AVIFileHandler avifh, string message, IEnumerable<Options.DataTypes> streams) {

            (Int32, Int32, Int32, Int32) chunkSizes = GetStreamsAvailableChunksSize(avifh);
            IOUtils.ConsolePrintInfo();
            Console.WriteLine($"Available free junk space: {chunkSizes.Item1}B");
            IOUtils.ConsolePrintInfo();
            Console.WriteLine($"Available free video space: {chunkSizes.Item2 + chunkSizes.Item3}B");
            IOUtils.ConsolePrintInfo();
            Console.WriteLine($"Available free audio space: {chunkSizes.Item4}B");

            int[] sizes = new int[3] { chunkSizes.Item1, chunkSizes.Item2 + chunkSizes.Item3, chunkSizes.Item4};

            int size = 0;
            foreach (int type in streams) {
                size += sizes[type];
            }

            return size / 8 > message.Length;

        }

        private static bool CheckCompression(bool uncompressed, bool force, Options.DataTypes type) {

            if (!uncompressed && force) {
                IOUtils.ConsolePrintWarning();
                Console.WriteLine($"Writing to compressed {type} stream due to force.");
                return true;
            }
            else if (!uncompressed) {
                IOUtils.ConsolePrintWarning();
                Console.WriteLine($"Writing to compressed {type} is not allowed. See --force flag to enable writing to compressed streams.");
                return false;
            }
            else {
                return true;
            }

        }

    }
}
