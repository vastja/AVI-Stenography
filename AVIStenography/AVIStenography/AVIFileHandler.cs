using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace AVIStenography {

    /// <summary>
    /// Handle .avi file
    /// <see href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd318189(v=vs.85).aspx"></see>
    /// <see href="https://cdn.hackaday.io/files/274271173436768/avi.pdf"></see>
    /// </summary>
    class AVIFileHandler {

        /*
        * AVI FILE TYPES
        * see https://msdn.microsoft.com/en-us/library/windows/desktop/dd318189(v=vs.85).aspx and 
        * https://cdn.hackaday.io/files/274271173436768/avi.pdf for specification
        */

        public static readonly Int32 VIDEO_CHUNK_COMPRESSED = 25444; // dc
        public static readonly Int32 VIDEO_CHUNK_UNCOMPRESSED = 25188; //db
        public static readonly Int32 AUDIO_CHUNK = 25207; //wb
        public static readonly Int32 JUNK_CHUNK = 1263424842; //JUNK

        public static readonly byte[] JUNK = new byte[] { 0x4a, 0x55, 0x4e, 0x4b };
        private static readonly byte[] RIFF = new byte[] { 0x52, 0x49, 0x46, 0x46 };
        private static readonly byte[] LIST = new byte[] { 0x4c, 0x49, 0x53, 0x54 };

        private static readonly byte[] REC = new byte[] { 0x72, 0x65, 0x63, 0x20 };

        private static readonly byte[] hdrl = new byte[] { 0x68, 0x64, 0x72, 0x6c };
        private static readonly byte[] avih = new byte[] { 0x61, 0x76, 0x69, 0x68 };
        private static readonly byte[] strl = new byte[] { 0x73, 0x74, 0x72, 0x6c };
        private static readonly byte[] strh = new byte[] { 0x73, 0x74, 0x72, 0x68 };
        private static readonly byte[] strf = new byte[] { 0x73, 0x74, 0x72, 0x66 };
        private static readonly byte[] movi = new byte[] { 0x6d, 0x6f, 0x76, 0x69 };

        private static readonly byte[] auds = new byte[] { 0x61, 0x75, 0x64, 0x73 };
        private static readonly byte[] vids = new byte[] { 0x76, 0x69, 0x64, 0x73 };

        public Dictionary<Int32, Dictionary<Int32, CHUNK>> Chunks { get; protected set; }

        public byte[] Avi { get; protected set; }


        public AVIMAINHEADER AviMainHeader { get; protected set; }
        public (AVISTREAMHEADER, BITMAPINFOHEADER) AviVideoStreamInfo { get; protected set; }

        public AVISTREAMHEADER AviAudioStreamInfo { get; protected set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="avi">byte representation of .avi file</param>
        public AVIFileHandler(byte[] avi) {
            Avi = avi;

            Chunks = new Dictionary<Int32, Dictionary<Int32, CHUNK>> {
                { JUNK_CHUNK, new Dictionary<Int32, CHUNK>()},
                { VIDEO_CHUNK_COMPRESSED, new Dictionary<Int32, CHUNK>()},
                { VIDEO_CHUNK_UNCOMPRESSED, new Dictionary<Int32, CHUNK>()},
                { AUDIO_CHUNK, new Dictionary<Int32, CHUNK>()},
            };

            AviMainHeader = GetAVIMainHeader();

            AviVideoStreamInfo = GetVideoStreamInfo();
            if (AviVideoStreamInfo.Item1.cb == 0) {
                IOUtils.ConsolePrintFailure();
                Console.WriteLine("AVI file does not contain video stream header. Execution ABBORTED.");
                Program.Exit(-1);
            }

            AviAudioStreamInfo = GetAudioStreamInfo();
            if (AviAudioStreamInfo.cb == 0) {
                IOUtils.ConsolePrintFailure();
                Console.WriteLine("AVI file does not contain audio stream header. Execution ABBORTED.");
                Program.Exit(-1);
            }

            IOUtils.ConsolePrintSuccess();
            Console.WriteLine("Start seaching in AVI file for CHUNKS.");
            Search();
            IOUtils.ConsolePrintSuccess();
            Console.WriteLine("Searching in AVI file completed.");

        }

        /// <summary>
        /// Options.DataType to Int32 code convertor
        /// </summary>
        /// <param name="type">Chunk type</param>
        /// <returns>Int32 code for given chunk type</returns>
        public static Int32 GetChunkName(Options.DataTypes type) {

            switch (type) {
                case Options.DataTypes.junk: return JUNK_CHUNK;
                case Options.DataTypes.vids: return VIDEO_CHUNK_COMPRESSED;
                case Options.DataTypes.auds: return AUDIO_CHUNK;
                default: return -1;
            }

        }

        /// <returns>Size of RIFF (.avi) file</returns>
        public UInt32 GetRIFFFileSize() {
            return BitConverter.ToUInt32(Avi, 4);
        }

        public AVIMAINHEADER GetAVIMainHeader() {

            int index = Find(hdrl, 0, Avi.Length);
            if (index < 0) {
                //IOUtils.ConsolePrintFailure();
                //Console.WriteLine("AVI file does not contain AVIMAINHEADER. Execution ABORTED.");
                return new AVIMAINHEADER();
            }

            return new AVIMAINHEADER(Avi, index);
        }

        public (AVISTREAMHEADER, BITMAPINFOHEADER) GetVideoStreamInfo() {

            int index = Find(strh, 0, Avi.Length);
            while (index > 0) {
                AVISTREAMHEADER avish = new AVISTREAMHEADER(Avi, index - 4);

                if (avish.fccType.Equals(BitConverter.ToUInt32(vids, 0))) {
                    BITMAPINFOHEADER bih = new BITMAPINFOHEADER(Avi, index + 12 + (int) avish.cb);
                    return (avish, bih);
                }

                index = Find(strh, index, Avi.Length);
            }

            return (new AVISTREAMHEADER(), new BITMAPINFOHEADER());
        }

        public AVISTREAMHEADER GetAudioStreamInfo() {

            int index = Find(strh, 0, Avi.Length);
            while (index > 0) {
                AVISTREAMHEADER avish = new AVISTREAMHEADER(Avi, index - 4);

                if (avish.fccType.Equals(BitConverter.ToUInt32(auds, 0))) {
                    return avish;
                }

                index = Find(strh, index, Avi.Length);
            }

            return new AVISTREAMHEADER();
        }

        /// <summary>
        /// Searches chunks in movi list 
        /// </summary>
        private void SearchMoviList() {
                     
            int index = Find(LIST, 0, Avi.Length);
            while (index > 0) {
                CHUNK chunk = new CHUNK(Avi, index - 4);
                if (BitConverter.ToInt32(Avi, index + 4).Equals(BitConverter.ToInt32(movi, 0))) {
                    FindAll(index + 8, index + chunk.ckSize + 8);
                    break;
                }

                index = Find(LIST, index, Avi.Length);
            }

        }

        /// <summary>
        /// Searches .avi file for chunks
        /// </summary>
        private void Search() {
            SearchMoviList();
            SearchJunks();
        }

        /// <summary>
        /// Searches for chunks
        /// </summary>
        /// <param name="startIndex">Searching starts from this index</param>
        /// <param name="endIndex">Searching ends at this index</param>
        /// <returns>Index where searching ended</returns>
        private int FindAll(int startIndex, int endIndex) {

            Dictionary<Int32, CHUNK> items;
            byte[] chunkId = new byte[2];

            int index = startIndex;
            while (index < endIndex) {
                CHUNK chunk = new CHUNK(Avi, index);

                if (BitConverter.GetBytes(chunk.ckID).Equals(LIST)) {
                    if (BitConverter.ToInt32(Avi, index + 8).Equals(BitConverter.ToInt32(REC, 0))) {
                        index = FindAll(index + 8, index + 8 + chunk.ckSize);
                    }
                }
                else {
                    Array.Copy(BitConverter.GetBytes(chunk.ckID), 2, chunkId, 0, 2);
                    Chunks.TryGetValue(BitConverter.ToInt16(chunkId, 0), out items);
                    items?.Add(index + 8, chunk);
                    index += chunk.ckSize + 8;

                    if (index % 2 != 0) {
                        index++;
                    }
                }
            }

            return index;

        }

        /// <summary>
        /// Counts size of given avi stream type
        /// </summary>
        /// <param name="chunks">Chunks of one avi stream type</param>
        /// <returns>Size in bytes</returns>
        private int GetSize(Dictionary<Int32, CHUNK> chunks) {
            int size = 0;
            foreach (CHUNK chunk in chunks.Values) {
                size += chunk.ckSize;
            }
            return size;
        }

        /// <summary>
        /// Counts size of all supported avi stream types
        /// </summary>
        /// <param name="avifh">.avi file handler</param>
        /// <returns>(Junk size, Audio size, Compressed vide size, Uncompressed video size)</returns>
        public (int, int, int, int) GetStreamsChunksSize() {
            Int32 junkSize = 0, vidsCompressSize = 0, vidsUncompressedSize = 0, audsSize = 0;

            Dictionary<Int32, CHUNK> chunkType;
            if (Chunks.TryGetValue(JUNK_CHUNK, out chunkType)) {
                junkSize = GetSize(chunkType);
            }
            if (Chunks.TryGetValue(AUDIO_CHUNK, out chunkType)) {
                audsSize = GetSize(chunkType);
            }
            if (Chunks.TryGetValue(VIDEO_CHUNK_COMPRESSED, out chunkType)) {
                vidsCompressSize = GetSize(chunkType);
            }
            if (Chunks.TryGetValue(VIDEO_CHUNK_UNCOMPRESSED, out chunkType)) {
                vidsUncompressedSize = GetSize(chunkType);
            }

            return (junkSize, vidsUncompressedSize, vidsCompressSize, audsSize);
        }

        /// <summary>
        /// Seraches for all junk chunks in .avi file
        /// </summary>
        private void SearchJunks() {

            Dictionary<Int32, CHUNK> junks;
            Chunks.TryGetValue(JUNK_CHUNK, out junks);

            int index = Find(JUNK, 0, Avi.Length);
            while (index > 0) {
                CHUNK chunk = new CHUNK(Avi, index - 4);
                junks?.Add(index + 4, chunk);
                index = Find(JUNK, index, Avi.Length);
            }

        }

        /// <summary>
        /// Finds given byte sequence in .avi file
        /// </summary>
        /// <param name="searched">Searched byte sequence</param>
        /// <param name="startIndex">Searching starts from this index</param>
        /// <param name="endIndex">Searching ends at this index</param>
        /// <returns>Index of last byte in sequence in case sequence were found else -1</returns>
        private int Find(byte[] searched, int startIndex, int endIndex) {

            bool found = false;
            int index = startIndex;

            while (!found && index < endIndex - searched.Length) {
                found = true;
                for (int i = 0; i < searched.Length; i++) {
                    if (Avi[index++] != searched[i]) {
                        found = false;
                        break;
                    }
                }
            }

            return found ? index : -1;

        }

        public bool IsStreamUncompressed(Options.DataTypes type) {

            switch (type) {
                case Options.DataTypes.auds: return AviAudioStreamInfo.dwQuality == 10000;
                case Options.DataTypes.vids: return AviVideoStreamInfo.Item1.dwQuality == 10000;
                default: return true;
            }

        }

    }

    

    /*
     * AVI FILE STRUCTURES
     * see https://msdn.microsoft.com/ for specification
     */

    public struct RCFRAME {
        public Int16 left;
        public Int16 top;
        public Int16 right;
        public Int16 bottom;

        public RCFRAME(byte[] avi, int index) {
            left = BitConverter.ToInt16(avi, index);
            top = BitConverter.ToInt16(avi, index + 2);
            right = BitConverter.ToInt16(avi, index + 4);
            bottom = BitConverter.ToInt16(avi, index + 6);
        }
    }

    public struct BITMAPINFOHEADER {
        public UInt32 biSize;
        public Int32 biWidth;
        public Int32 biHeight;
        public UInt16 biPlanes;
        public UInt16 biBitCount;
        public UInt32 biCompression;
        public UInt32 biSizeImage;
        public Int32 biXPelsPerMeter;
        public Int32 biYPelsPerMeter;
        public UInt32 biClrUsed;
        public UInt32 biClrImportant;

        public BITMAPINFOHEADER(byte[] avi, int index) {
            biSize = BitConverter.ToUInt32(avi, index);
            biWidth = BitConverter.ToInt32(avi, index + 4);
            biHeight = BitConverter.ToInt32(avi, index + 8);
            biPlanes = BitConverter.ToUInt16(avi, index + 12);
            biBitCount = BitConverter.ToUInt16(avi, index + 14);
            biCompression = BitConverter.ToUInt32(avi, index + 16);
            biSizeImage = BitConverter.ToUInt32(avi, index + 20);
            biXPelsPerMeter = BitConverter.ToInt32(avi, index + 24);
            biYPelsPerMeter = BitConverter.ToInt32(avi, index + 28);
            biClrUsed = BitConverter.ToUInt32(avi, index + 32);
            biClrImportant = BitConverter.ToUInt32(avi, index + 36);
        }
    }

    public struct AVIMAINHEADER {
        public UInt32 fcc;
        public UInt32 cb;
        public UInt32 dwMicroSecPerFrame;
        public UInt32 dwMaxBytesPerSec;
        public UInt32 dwPaddingGranularity;
        public UInt32 dwFlags;
        public UInt32 dwTotalFrames;
        public UInt32 dwInitialFrames;
        public UInt32 dwStreams;
        public UInt32 dwSuggestedBufferSize;
        public UInt32 dwWidth;
        public UInt32 dwHeight;
        public UInt32[] dwReserved;

        public AVIMAINHEADER(byte[] avi, int index) {
            fcc = BitConverter.ToUInt32(avi, index);
            cb = BitConverter.ToUInt32(avi, index + 4);
            dwMicroSecPerFrame = BitConverter.ToUInt32(avi, index + 8);
            dwMaxBytesPerSec = BitConverter.ToUInt32(avi, index + 12);
            dwPaddingGranularity = BitConverter.ToUInt32(avi, index + 16);
            dwFlags = BitConverter.ToUInt32(avi, index + 20);
            dwTotalFrames = BitConverter.ToUInt32(avi, index + 24);
            dwInitialFrames = BitConverter.ToUInt32(avi, index + 28);
            dwStreams = BitConverter.ToUInt32(avi, index + 32);
            dwSuggestedBufferSize = BitConverter.ToUInt32(avi, index + 36);
            dwWidth = BitConverter.ToUInt32(avi, index + 40);
            dwHeight = BitConverter.ToUInt32(avi, index + 44);

            dwReserved = new UInt32[4];
            for (int i = 0; i < 4; i++) {
                dwReserved[i] = avi[index + +i];
            }
        }
    }

    public struct AVISTREAMHEADER {
        public UInt32 fcc;
        public UInt32 cb;
        public UInt32 fccType;
        public UInt32 fccHandler;
        public UInt32 dwFlags;
        public UInt16 wPriority;
        public UInt16 wLanguage;
        public UInt32 dwInitialFrames;
        public UInt32 dwScale;
        public UInt32 dwRate;
        public UInt32 dwStart;
        public UInt32 dwLength;
        public UInt32 dwSuggestedBufferSize;
        public UInt32 dwQuality;
        public UInt32 dwSampleSize;
        public RCFRAME rcFrame;


        public AVISTREAMHEADER(byte[] avi, int index) {
            fcc = BitConverter.ToUInt32(avi, index);
            cb = BitConverter.ToUInt32(avi, index + 4);
            fccType = BitConverter.ToUInt32(avi, index + 8);
            fccHandler = BitConverter.ToUInt32(avi, index + 12);
            dwFlags = BitConverter.ToUInt32(avi, index + 16);
            wPriority = BitConverter.ToUInt16(avi, index + 20);
            wLanguage = BitConverter.ToUInt16(avi, index + 22);
            dwScale = BitConverter.ToUInt32(avi, index + 24);
            dwRate = BitConverter.ToUInt32(avi, index + 28);
            dwStart = BitConverter.ToUInt32(avi, index + 32);
            dwLength = BitConverter.ToUInt32(avi, index + 36);
            dwInitialFrames = BitConverter.ToUInt32(avi, index + 40);
            dwSuggestedBufferSize = BitConverter.ToUInt32(avi, index + 44);
            dwQuality = BitConverter.ToUInt32(avi, index + 48);
            dwSampleSize = BitConverter.ToUInt32(avi, index + 52);
            rcFrame = new RCFRAME(avi, index + 56);
        }
    }

    struct BITMAPFILEHEADER {

        public UInt16 bfType;
        public UInt32 bfSize;
        public UInt16 bfReserved1;
        public UInt16 bfReserved2;
        public UInt32 bfOffBits;

        public BITMAPFILEHEADER(byte[] avi, int index) {
            bfType = BitConverter.ToUInt16(avi, index);
            bfSize = BitConverter.ToUInt32(avi, index + 2);
            bfReserved1 = BitConverter.ToUInt16(avi, index + 6);
            bfReserved2 = BitConverter.ToUInt16(avi, index + 8);
            bfOffBits = BitConverter.ToUInt32(avi, index + 10);
        }
    }

    struct CHUNK {
        public Int32 ckID;
        public Int32 ckSize;

        public CHUNK(byte[] avi, int index) {
            ckID = BitConverter.ToInt32(avi, index);
            ckSize = BitConverter.ToInt32(avi, index + 4);
        }
    }
}
