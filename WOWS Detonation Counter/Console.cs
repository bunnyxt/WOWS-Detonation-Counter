using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WOWS_Detonation_Counter
{
    static class Console
    {
        public static string ReadLine()
        {
            return System.Console.ReadLine();
        }

        public static ConsoleKeyInfo ReadKey()
        {
            return System.Console.ReadKey();
        }

        public static ConsoleKeyInfo ReadKey(bool intercept)
        {
            return System.Console.ReadKey(intercept);
        }

        public static void WriteLine(int value, bool isLog = true)
        {
            System.Console.WriteLine(value);
            if (isLog)
            {
                WriteFile(value.ToString() + "\r\n");
            }
        }

        public static void WriteLine(string value, bool isLog = true)
        {
            System.Console.WriteLine(value);
            if (isLog)
            {
                WriteFile(value + "\r\n");
            }
        }

        public static void WriteLine(bool isLog = true)
        {
            System.Console.WriteLine();
            if (isLog)
            {
                WriteFile("\r\n");
            }
        }

        public static void WriteLine(string format, params object[] arg)
        {
            System.Console.WriteLine(format, arg);
            WriteFile(string.Format(format, arg) + "\r\n");
        }

        public static void Write(int value, bool isLog = true)
        {
            System.Console.Write(value);
            if (isLog)
            {
                WriteFile(value.ToString());
            }
        }

        public static void Write(string value, bool isLog = true)
        {
            System.Console.Write(value);
            if (isLog)
            {
                WriteFile(value);
            }
        }

        public static void Write(string format, params object[] arg)
        {
            System.Console.Write(format, arg);
            WriteFile(string.Format(format, arg));
        }

        private static bool IsFirst = true;
        private static string StartDate;

        private static void WriteFile(string value)
        {
            if (IsFirst)
            {
                StartDate = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                System.IO.File.Delete(System.IO.Path.Combine(Environment.CurrentDirectory,
                                      "log_" + StartDate + ".txt"));
                IsFirst = false;
            }
            System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.CurrentDirectory,
                                         "log_" + StartDate + ".txt"), value, Encoding.Default);
        }

        private static bool IsFirstWaring = true;

        public static void WriteWarning(string value)
        {
            if (IsFirstWaring)
            {
                System.IO.File.Delete(System.IO.Path.Combine(Environment.CurrentDirectory,
                                      "log_" + StartDate + "_warning.txt"));
                IsFirstWaring = false;
            }

            value = DateTime.Now.ToString() + " " + value;
            System.IO.File.AppendAllText(System.IO.Path.Combine(Environment.CurrentDirectory,
                                         "log_" + StartDate + "_warning.txt"), value, Encoding.Default);
        }
    }
}
