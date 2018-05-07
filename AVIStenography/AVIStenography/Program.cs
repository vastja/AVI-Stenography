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
            if (info.Item1.cb == 0) {
                IOUtils.ConsolePrintFailure();
                Console.WriteLine("AVI file does not contain video stream header. Execution ABBORTED.");
                Exit(-1);
            }


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

    }

}

