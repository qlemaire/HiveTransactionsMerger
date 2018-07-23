using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Registry;
using CommandLine;
using CommandLine.Text;
using System.Linq;
using System.IO;

namespace HiveTransactionsMerger
{
    class Program
    {
        internal class Logger
        {
            public enum Level
            {
                None,
                Info,
                Verbose
            }

            private Level level;
            private static Logger _instance;

            private Logger(Level level)
            {
                this.level = level;
            }

            public void SetLevel(Level level)
            {
                this.level = level;
            }

            public void Verbose(string message)
            {
                this.write(message, Level.Verbose);
            }

            public void Info(string message)
            {
                this.write(message, Level.Info);
            }

            public void Error(string message)
            {
                Console.Error.Write(message);
            }

            /**
             * param name="logLevel"
             * logLevel requested by the write
             */
            private void write(string message, Level logLevel)
            {
                if (this.level >= logLevel) // should we display the log?
                {
                    string prefix = Logger.getPrefix(logLevel);
                    var _previousForegroundColor = Console.ForegroundColor;
                    Console.ForegroundColor = Logger.getColor(logLevel);
                    Console.WriteLine(prefix + message);
                    Console.ForegroundColor = _previousForegroundColor;
                }
            }

            private static ConsoleColor getColor(Level level)
            {
                switch (level)
                {
                    case Level.None:
                        return ConsoleColor.Black;
                    case Level.Info:
                        return ConsoleColor.Blue;
                    case Level.Verbose:
                        return ConsoleColor.Green;
                    default:
                        return ConsoleColor.Black;
                } 
            }

            private static string getPrefix(Level level)
            {
                switch (level)
                {
                    case Level.None:
                        return "";
                    case Level.Info:
                        return "[i] ";
                    case Level.Verbose:
                        return "[*] ";
                    default:
                        return "";
                }
            }

            public static Logger GetInstance()
            {
                if (Logger._instance == null)
                {
                    Logger._instance = new Logger(Level.None);
                }
                return Logger._instance;
            }
        }

        internal class Options
        {
            [Option('d', "DirtyHive", Required = true, HelpText = "Dirty hive to process.")]
            public string DirtyHiveFilename { get; set; }

            /*
             * [OptionArray('t', "Transactions", DefaultValue = new[] {""}, HelpText = "Transaction files to process")]
             *  public string[] TransactionFilenames { get; set; }
             */
            [Option('t', "Transactions", Separator = ',', Required = true, HelpText = "Transaction files to process.")]
            public IEnumerable<string> TransactionFilenames { get; set; }

            [Option('o', "Output", Required = true, HelpText = "Output file")]
            public string OutputFilename { get; set; }

            [Option(Default = false, HelpText = "Prints all messages to standard output.")]
            public bool Verbose { get; set; }

            [Usage(ApplicationAlias = "HiveTransactionsMerger.exe")]
            public static IEnumerable<Example> Examples
            {
            get
                {
                    yield return new Example("Standard", new Options { DirtyHiveFilename = "NTUSER.dat", TransactionFilenames = new List<string> { "NTUSER.dat.log1", "NTUSER.dat.log2" }, OutputFilename = "NTUSER.dat.merged" });
                }
            }
        }

        static void Main(string[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(opts => Run(opts))
                .WithNotParsed<Options>((errs) => HandleParseError(errs));
        }

        static void HandleParseError(IEnumerable<Error> errors)
        {
            Environment.Exit(1);
        }

        static void Run(Options opts)
        {
            var logger = Logger.GetInstance();
            logger.SetLevel(opts.Verbose ? Logger.Level.Verbose : Logger.Level.Info);

            logger.Info(String.Format("Number of transactions logs: {0}", opts.TransactionFilenames.Count()));
            foreach (var trans in opts.TransactionFilenames)
            {
                logger.Verbose(String.Format("Transaction file: {0}", trans));
            }

            /* Online: commitRegistry */
            // TODO

            /* Offline */
            RegistryHive registryHive = null;
            try
            {
                registryHive = new RegistryHive(opts.DirtyHiveFilename);
            }
            catch (Exception exception)
            {
                logger.Error(String.Format("Impossible to parse hive: {0}", exception.Message));
                Environment.Exit(1);
            }

            byte[] mergedHive = null;
            try
            {
                mergedHive = registryHive.ProcessTransactionLogs(opts.TransactionFilenames.ToList());
            }
            catch (Exception exception)
            {
                logger.Error(String.Format("An exception occured while processing transaction logs: {0}", exception.Message));
                Environment.Exit(1);
            }
            

            logger.Info(String.Format("Merged {0} bytes", mergedHive.Length));
            logger.Info(String.Format("Writing to {0}", opts.OutputFilename));

            using (var fs = new FileStream(opts.OutputFilename, FileMode.Create))
            {
                using (var bw = new BinaryWriter(fs))
                {
                    bw.Write(mergedHive);
                }
            }

            logger.Info("Done");
        }



        [DllImport("Advapi32.dll", EntryPoint = "RegFlushKey")]
        public static extern int RegFlushKey(IntPtr hKey);

        const UInt32 HKEY_CLASSES_ROOT = 0x80000000;
        const UInt32 HKEY_CURRENT_USER = 0x80000001;
        const UInt32 HKEY_LOCAL_MACHINE = 0x80000002;
        const UInt32 HKEY_USERS = 0x80000003;

        public static int CommitRegistry()
        {
            IntPtr hkeyLocalMachine = new IntPtr(unchecked((int)HKEY_LOCAL_MACHINE));
            int status = RegFlushKey(hkeyLocalMachine);
            return status;
        }
    }
}
