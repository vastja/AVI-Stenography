using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using CommandLine;

namespace AVIStenography {

    /// <summary>
    /// Command line arguments see CommandLineParser
    /// <see href="https://github.com/commandlineparser/commandline.git"></see>
    /// </summary>
    class Options {

        public enum Actions {extract, hide};
        public enum DataTypes { junk, vids, auds};

        private const string actionHelp = "H for message hiding, E for message extracting";
        private const string filePathHelp = "Path (absolute or relative) to .avi file";
        private const string messageHelp = "Message to be hidden into .avi file. Use prefix 'file:' to load message " +
                                           "from file (absolute or relative path specification follows prefix).";
        private const string forceHelp = "Enable writing to compressed data streams";
        private const string usedHelp = "Data streams used for hiding message. Stream types are separated with comma " +
                                        "values=junk,vids,auds (for example --used junk,vids,auds). Streams will be used" +
                                        " in order as specified (default order is junk,vids,auds).";
        private const string outputFilePathHelp = "Absolute or relative path to final file.";

        [Value(0, Required = true, HelpText = actionHelp)]
        public Actions Action { get; set; }

        [Value(1, Required = true, HelpText = filePathHelp)]
        public string FilePath { get; set; }

        [Option('m',"message", Required = false, HelpText = messageHelp)]
        public string Message { get; set; }

        [Option('f',"force", Required = false, Default = false, HelpText = forceHelp)]
        public bool Force { get; set; }

        [Option('u',"used", Required = false, Separator = ',', Min = 1, Max = 3, Default = new DataTypes[] { DataTypes.junk, DataTypes.vids, DataTypes.auds }, HelpText = usedHelp)]
        public IEnumerable<DataTypes> Used { get; set; }

        [Option('o', "output-file", Required = false, Default = "temp.avi", HelpText = outputFilePathHelp)]
        public string OutputFilePath { get; set; }

    }

    /// <summary>
    /// Controls program data flow
    /// </summary>
    class Program {

        static void Main(string[] args) {

            Options options = new Options();
            CommandLine.Parser.Default.ParseArguments<Options>(args)
            .WithParsed<Options>(opts => RunOptionsAndReturnExitCode(opts))
            .WithNotParsed<Options>((errs) => HandleParseError(errs));

            Exit(0);

        }

        /// <summary>
        /// Command line parser success handler
        /// <see href="https://github.com/commandlineparser/commandline.git"></see>
        /// </summary>
        /// <param name="options">Parsed options</param>
        public static void RunOptionsAndReturnExitCode(Options options) {

            if (options.Action == Options.Actions.hide && options.Message == null) {
                IOUtils.ConsolePrintFailure();
                Console.WriteLine("Message has to be specified in hide mode.");
                Exit(-1);
            }

            string message;
            if (options.Message.StartsWith("file:")) {
                message = IOUtils.LoadMessage(options.Message.Split(':')[1]);
            }
            else {
                message = options.Message;
            }

            if (message == null) {
                IOUtils.ConsolePrintFailure();
                Console.WriteLine("Message cannot be empty.");
                Exit(-1);
            }

            byte[] avi = IOUtils.LoadAvi(options.FilePath);
            if (avi == null) {
                Exit(-1);
            }

            AVIFileHandler avifh = new AVIFileHandler(avi);
            if (options.Action == Options.Actions.extract) {
                string hiddenMessage = StenogrpahyUtils.ExtractMessage(avifh, options.Used);
                IOUtils.ConsolePrintSuccess();
                Console.WriteLine($"Hidden message is: {hiddenMessage}");
            }
            else {
                if (StenogrpahyUtils.HideMessage(avifh, message, options.Force, options.Used)) {
                    IOUtils.ConsolePrintSuccess();
                    Console.WriteLine("Hiding data.");
                    IOUtils.SaveAvi(options.OutputFilePath, avifh.Avi);
                }
                else {
                    IOUtils.ConsolePrintFailure();
                    Console.WriteLine("Message hidding failed.");
                }
            }

        }

        /// <summary>
        /// Command line parser error handler
        /// <see href="https://github.com/commandlineparser/commandline.git"></see>
        /// </summary>
        /// <param name="error">Parser error</param>
        public static void HandleParseError(IEnumerable<Error> error) {
            // TODO
        }

        /// <summary>
        /// Application ending handler
        /// </summary>
        /// <param name="code">Exit code</param>
        public static void Exit(int code) {
            // Console.ReadKey();
            Environment.Exit(code);
        }

    }

}

