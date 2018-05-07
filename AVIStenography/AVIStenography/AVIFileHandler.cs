using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace AVIStenography {



    class AVIFileHandler {

        public static readonly byte[] VIDEO_CHUNK_COMPRESSED = new byte[] { 0x64, 0x63 }; // dc
        public static readonly byte[] VIDEO_CHUNK_UNCOMPRESSED = new byte[] { 0x64, 0x62 }; //db
        public static readonly byte[] AUDIO_CHUNK = new byte[] { 0x77, 0x62 }; //wb
        public static readonly byte[] JUNK_CHUNK = new byte[] { 0x4a, 0x55, 0x4e, 0x4b }; //JUNK


        private static readonly byte[] RIFF = new byte[] { 0x52, 0x49, 0x46, 0x46 };
        private static readonly byte[] LIST = new byte[] { 0x4c, 0x49, 0x53, 0x54 };

        private static readonly byte[] REC = new byte[] { 0x72, 0x65, 0x63, 0x20 };

        private static readonly byte[] hdrl = new byte[] { 0x68, 0x64, 0x72, 0x6c };
        private static readonly byte[] avih = new byte[] { 0x61, 0x76, 0x69, 0x68 };
        private static readonly byte[] strl = new byte[] { 0x73, 0x74, 0x72, 0x6c };
        private static readonly byte[] strf = new byte[] { 0x73, 0x74, 0x72, 0x66 };
        private static readonly byte[] movi = new byte[] { 0x6d, 0x6f, 0x76, 0x69 }; // movi

        private static readonly byte[] auds = new byte[] { 0x61, 0x75, 0x64, 0x73 };
        private static readonly byte[] vids = new byte[] { 0x76, 0x69, 0x64, 0x73 };


        private Dictionary<byte[], List<Int32>> moviList;
        private Dictionary<Int32, CHUNK> junks;

        private byte[] avi;

        public AVIFileHandler(byte[] avi) {
            this.avi = avi;

            int index = Find(strf, 0, avi.Length);
            BITMAPINFOHEADER bih = new BITMAPINFOHEADER(avi, index);
        }

        public UInt32 GetRIFFFileSize() {
            return BitConverter.ToUInt32(avi, 4);
        }

        public AVIMAINHEADER GetAVIMainHeader() {

            int index = Find(hdrl, 0, avi.Length);
            if (index < 0) {
                IOUtils.ConsolePrintFailure();
                Console.WriteLine("AVI file does not contain AVIMAINHEADER. Execution ABORTED.");
                Program.Exit(-1);
            }

            return new AVIMAINHEADER(avi, index);
        }

        public void SearchMoviList() {

            moviList = new Dictionary<byte[], List<Int32>> {
                { VIDEO_CHUNK_COMPRESSED, new List<Int32>()},
                { VIDEO_CHUNK_UNCOMPRESSED, new List<Int32>()},
                { AUDIO_CHUNK, new List<Int32>()},
            };

            int index = Find(LIST, 0, avi.Length);
            while (index > 0) {
                CHUNK chunk = new CHUNK(avi, index - 4);
                if (BitConverter.ToInt32(avi, index + 4).Equals(BitConverter.ToInt32(movi, 0))) {
                    FindAll(index + 12, index + chunk.ckSize + 12);
                    break;
                }

                index = Find(LIST, index, avi.Length);
            }

        }

        public int FindAll(int startIndex, int endIndex) {

            List<Int32> items;

            int index = startIndex;
            while (index < endIndex) {
                CHUNK chunk = new CHUNK(avi, index);

                if (BitConverter.GetBytes(chunk.ckID).Equals(LIST)) {
                    if (BitConverter.ToInt32(avi, index + 8).Equals(BitConverter.ToInt32(REC, 0))) {
                        index = FindAll(index + 8, index + 8 + chunk.ckSize);
                    }
                }
                else {
                    moviList.TryGetValue(BitConverter.GetBytes(chunk.ckID), out items);
                    items?.Add(index);
                    index += chunk.ckSize;
                }
            }

            return index;

        }

        public Int32 GetJunkChunksSize() {
            Int32 size = 0;

            if (junks == null) {
                SearcheJunks();
            }

            foreach (CHUNK chunk in junks.Values) {
                size += chunk.ckSize;
            }

            return size;
        }

        public Dictionary<Int32, CHUNK> GetJunks() {
            return junks;
        }

        private void SearcheJunks() {

            junks = new Dictionary<Int32, CHUNK>();

            int index = Find(JUNK_CHUNK, 0, avi.Length);
            while (index > 0) {
                CHUNK chunk = new CHUNK(avi, index - 4);
                junks.Add(index + 8, chunk);
                index = Find(JUNK_CHUNK, index, avi.Length);
            }

        }

        private int Find(byte[] searched, int startIndex, int endIndex) {

            bool found = false;
            int index = startIndex;

            while (!found && index < endIndex - searched.Length) {
                found = true;
                for (int i = 0; i < searched.Length; i++) {
                    if (avi[index++] != searched[i]) {
                        found = false;
                        break;
                    }
                }
            }

            return found ? index : -1;

        }

    }


    public struct RCFRAME {
        public Int16 left;
        public Int16 top;
        public Int16 right;
        public Int32 bottom;

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
        public Int16 biPlanes;
        public Int16 biBitCount;
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
            biPlanes = BitConverter.ToInt16(avi, index + 12);
            biBitCount = BitConverter.ToInt16(avi, index + 14);
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
        public Int32 cb;
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
            cb = BitConverter.ToInt32(avi, index + 4);
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

        public Int16 bfType;
        public Int32 bfSize;
        public Int16 bfReserved1;
        public Int16 bfReserved2;
        public Int32 bfOffBits;

        public BITMAPFILEHEADER(byte[] avi, int index) {
            bfType = BitConverter.ToInt16(avi, index);
            bfSize = BitConverter.ToInt16(avi, index + 2);
            bfReserved1 = BitConverter.ToInt16(avi, index + 6);
            bfReserved2 = BitConverter.ToInt16(avi, index + 8);
            bfOffBits = BitConverter.ToInt16(avi, index + 10);
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
