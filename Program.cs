using LDF_File_Parser.Logger;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace LDF_FILEPARSER
{
    class Program
    {
        static void Main(string[] args)
        {
            List<LinFileContents> linFileContents = new List<LinFileContents>();

            try
            {
                var pathToLDFFiles = @"C:\Users\chauh\Downloads\SampleLINFiles";
                
                var fileNameWithPath = Directory.GetFiles(pathToLDFFiles);
                
                foreach (var filePath in fileNameWithPath)
                {
                    linFileContents.Add(new LinFileContents(filePath));
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
                Console.ReadLine();
        }

        // TODO Make properties with collections Read Only
        // TODO Create Exceptions
        // TODO Add Logs
        // TODO look for validations
    }
}