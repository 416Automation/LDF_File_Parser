using System;
using System.IO;
using System.Threading;

namespace LDF_File_Parser.Logger
{
    static partial class Logger
    {
        static class FileWriter
        {
            private static ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
            public static void WriteData(string filePath, string data)
            {
                _lock.EnterWriteLock();
                try
                {
                    File.AppendAllText(filePath, data);
                }
                catch (Exception exc)
                {
                    var message = exc.Message;
                    Console.WriteLine(message);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
        }
    }
}
