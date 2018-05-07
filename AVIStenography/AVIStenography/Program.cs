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

            string message = "abcdefghijklmnopqrstuvwxyz" + (char)0x03;

            AVIFileHandler handler = new AVIFileHandler(avi);
            StenogrpahyUtils.HideMessage(handler, message);

            //AVIMAINHEADER avih = handler.GetAVIMainHeader();

            


            Exit(0);

        }

        public static void Exit(int code) {
            Console.WriteLine("Press any key to exit ...");
            Console.ReadKey();
            Environment.Exit(code);
        }

    }

}

