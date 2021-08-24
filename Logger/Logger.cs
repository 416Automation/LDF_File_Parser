using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LDF_File_Parser.Logger
{
    public static partial class Logger
    {
        public static string FileNameWithPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"LDF_{DateTime.Today:yyyy-MM-dd}.log");

        public static void LogInformation(string message)
        {
            string logLine = $"{DateTime.Now:yyyy-MM-dd hh:mm:ss:ffff tt}|Information|{message}{Environment.NewLine}";
            FileWriter.WriteData(FileNameWithPath, logLine);
        }

        public static void LogError(Exception exc, string messagePrefix = null)
        {
            string logLine = $"{DateTime.Now:yyyy-MM-dd hh:mm:ss:ffff tt}|Error|{messagePrefix}{exc}{Environment.NewLine}";
            FileWriter.WriteData(FileNameWithPath, logLine);
        }

        public static void LogWarning(string message = null)
        {
            string logLine = $"{DateTime.Now:yyyy-MM-dd hh:mm:ss:ffff tt}|Warning|{message}{Environment.NewLine}";
            FileWriter.WriteData(FileNameWithPath, logLine);
        }
    }
}
