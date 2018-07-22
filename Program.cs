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
        public class Options
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
            Console.WriteLine("Number of transactions logs: {0}", opts.TransactionFilenames.Count());
            foreach (var trans in opts.TransactionFilenames)
            {
                Console.WriteLine("[i] Transaction file: {0}", trans);
            }

            /* Online: commitRegistry */
            // TODO

            /* Offline */
            var registryHive = new RegistryHive(opts.DirtyHiveFilename);
            byte[] mergedHive = registryHive.ProcessTransactionLogs(opts.TransactionFilenames.ToList());

            Console.WriteLine("[*] Merged {0} bytes", mergedHive.Length);
            Console.WriteLine("[*] Writing to {0}", opts.OutputFilename);

            using (var fs = new FileStream(opts.OutputFilename, FileMode.Create))
            {
                using (var bw = new BinaryWriter(fs))
                {
                    bw.Write(mergedHive);
                }
            }

            Console.WriteLine("[*] Done");
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
