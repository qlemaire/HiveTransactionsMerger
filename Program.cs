using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Registry;

namespace HiveTransactionsMerger
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: HiveTransactionMerger.exe <hive> <transactionFiles...>", args[0]);
                Environment.Exit(1);
            }

            /* Arguments management */
            var hiveFilename = args[1];

            // dirty
            var logTransactions = new List<string>();
            foreach (var transactionFilename in args)
            {
                logTransactions.Add(transactionFilename);
            }
            logTransactions.RemoveAt(0); // remove first arg <== it's the hive file

            /* Online: commitRegistry */
            // TODO

            /* Offline: TODO */
            var registryHive = new RegistryHive(hiveFilename);
            byte[] mergedHive = registryHive.ProcessTransactionLogs(logTransactions);

            Console.WriteLine("Merged {0} bytes", mergedHive.Length);
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
