using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace AVIStenography {




    class Program {

        static void Main(string[] args) {

            byte[] avi = IOUtils.Load("For-the-birds.avi");
            if (avi == null) {
                Exit(-1);
            }

            AVIFileHandler handler = new AVIFileHandler(avi);

            AVIMAINHEADER avih = handler.GetAVIMainHeader();

            var info = handler.GetVideoStreamInfo();


            Int32 junkSize = handler.GetJunkChunksSize();
            IOUtils.ConsolePrintSuccess();
            Console.WriteLine($"Available free junk space: {junkSize}B");

            handler.SearchMoviList();

            Exit(0);

        }

        public static void Exit(int code) {
            Console.WriteLine("Press any key to exit ...");
            Console.ReadKey();
            Environment.Exit(code);
        }


        private static int GetChunkDataSize(byte[] avi, int chunkStartIndex) {

            for (int i = 0; i < 4; i++) {
                Console.Write((char)avi[chunkStartIndex + i]);
            }

            byte[] chunkDataSize = new byte[4];

            for (int i = 0; i < 4; i++) {
                chunkDataSize[i] = avi[chunkStartIndex + 4 + i];
            }

            return BitConverter.ToInt32(chunkDataSize, 0);
        }

    }

}

