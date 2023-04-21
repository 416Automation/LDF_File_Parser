using LDF_File_Parser.Exceptions;
using LDF_File_Parser.Extension;
using LDF_File_Parser.Logger;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace LDF_FILEPARSER
{
    public class LinFileContents : INotifyPropertyChanged
    {
        public IDictionary<string, EncodingNode> Encodings { get; set; } = new Dictionary<string, EncodingNode>();
        public string FileName { get; private set; } = string.Empty;
        public string FileNameWithPath { get; private set; } = string.Empty;
        public string FileRawData { get; }
        public string FileParsedData { get; }
        public ICollection<Frame> Frames { get; set; } = new HashSet<Frame>();
        public string LanguageVersion { get; set; }
        public string ProtocolVersion { get; set; }
        public IDictionary<string, Signal> Signals { get; set; } = new Dictionary<string, Signal>();
        public string Speed { get; set; }

        public LinFileContents(string fileNameWithPath)
        {
            if (string.IsNullOrEmpty(fileNameWithPath))
                throw new ArgumentException($"'{nameof(fileNameWithPath)}' cannot be null or empty.", nameof(fileNameWithPath));

            // Check for MIME type too 
            var isExtensionValid = Path.GetExtension(fileNameWithPath) == ".ldf";
            if (isExtensionValid is false)
            {
                // throw file exception for file not valid ldf 
                throw new InvalidFileExtension($"Filename {Path.GetFileName(fileNameWithPath)} does not have .ldf extension");
            }

            try
            {
                FileNameWithPath = fileNameWithPath;
                FileName = Path.GetFileName(FileNameWithPath);
                FileRawData = File.ReadAllText(FileNameWithPath);

                StringBuilder sb        = new StringBuilder();
                Regex openCloseLineComment  = new Regex(@"\/\*.*\*\/", RegexOptions.Compiled);
                Regex singleLineComment = new Regex(@"\/\/.*", RegexOptions.Compiled);
                Regex multiLineComment = new Regex(@"\/\*.*\*\/", RegexOptions.Singleline | RegexOptions.Compiled);
                foreach (string line in File.ReadLines(FileNameWithPath))
                {
                    string commentRemoved = openCloseLineComment.Replace(line, "");
                    commentRemoved        = singleLineComment.Replace(commentRemoved, "");
                    if (string.IsNullOrWhiteSpace(commentRemoved.Trim(new char[] { ' ', '\r', '\n' }))) continue;
                    _ = sb.AppendLine(commentRemoved);
                }

                FileParsedData = multiLineComment.Replace(sb.ToString(), "");

                ExtractLINDescription();
                ExtractSignals(GetSignalNodeContent());
                ExtractFrames(GetFrameNodeContent());
                ExtractEncodings(GetSignalEncodingNodeContent());
                ExtractEncodingsRepresentation(GetSignalRepresentationNodeContent());

                foreach ((int largestSizeOfSignal, Signal signal, Frame frame) in from item in Frames
                                                                                  let largestSizeOfSignal = item.Signals.Max(s => s.Size)
                                                                                  from test in item.Signals
                                                                                  select (largestSizeOfSignal, test, item))
                {
                    signal.CreateBitValues(largestSizeOfSignal, frame);
                }
            }
            catch (Exception exc)
            {
                Logger.LogError(exc, $"Parsing file unsuccessful, file name with path: {fileNameWithPath}");
                throw;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Extracts the description of the LIN.
        /// </summary>
        /// <param name="newline">The newline.</param>
        /// <param name="index">The index.</param>
        /// <param name="IsSpeed">if set to <c>true</c> [is speed].</param>
        /// <returns></returns>
        private string ExtractLINDescription(string newline, int index, bool IsSpeed = false)
        {
            try
            {
                var result = newline.Split(new char[] { CharSymbol.WhiteSpace, ';', '"' }, StringSplitOptions.RemoveEmptyEntries);
                return result[index] + (IsSpeed ? + CharSymbol.WhiteSpace + result[3] : string.Empty);
            }
            catch (Exception exc)
            {
                Logger.LogError(exc);
                return "N/A";
            }
        }

        /// <summary>
        /// Extracts the encodings.
        /// </summary>
        /// <param name="signalEncodingsContent">Content of the signal encodings.</param>
        /// <exception cref="Exception">
        /// as it cant have more values
        /// or
        /// it should have at least three values
        /// or
        /// </exception>
        private void ExtractEncodings(string signalEncodingsContent)
        {

            if (string.IsNullOrWhiteSpace(signalEncodingsContent))
            {
                Logger.LogWarning($"{FileName} does not consist any Encodings at {FileNameWithPath}");
                return;
            }

            Regex encodingTypePattern     = new Regex(@"^\s*(([A-Z]\w+)\s*\x7B\s*(?:physical_value|logical_value).*?\x7D)", RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.Compiled);
            Regex physicalEncodingPattern = new Regex(@"^\s*(physical_value)\s*,\s*(\d+)\s*,\s*(?:0x)?([0-9A-F]{1,})\s*,\s*(\d+)\s*,\s*(\d+)\s*,\s*\x22([^\x22]+)\x22\s*;", RegexOptions.Multiline | RegexOptions.Compiled);
            Regex logicalEncodingPattern  = new Regex(@"^\s*(logical_value)\s*,\s*((?:0x)?[0-9A-F]{1,})\s*,\s*\x22([^\x22]+)\x22\s*;", RegexOptions.Multiline | RegexOptions.Compiled);

            MatchCollection encodingNodes = encodingTypePattern.Matches(signalEncodingsContent);
            foreach (Match encodingNode in encodingNodes)
            {
                string encodingName = encodingNode.Groups[2].Value.ToString();
                EncodingNode encoding = new EncodingNode(encodingName);

                MatchCollection physEncodingMatches = physicalEncodingPattern.Matches(encodingNode.Value);
                foreach (Match physicalMatch in physEncodingMatches)
                {
                    string address = physicalMatch.Groups[2].Value.ToString().ConvertToHex();
                    PhysicalEncodingValue physicalEncoding = new PhysicalEncodingValue(address);
                    int i = 3;
                    for (; i < physicalMatch.Groups.Count - 1; i++)
                        physicalEncoding.Values.Add(physicalMatch.Groups[i].Value.ToString());
                    physicalEncoding.Description = physicalMatch.Groups[i].Value.ToString();
                    encoding.EncodingTypes.Add(physicalEncoding);
                }
                MatchCollection logicEncodingMatches = logicalEncodingPattern.Matches(encodingNode.Value);
                foreach (Match logicalMatch in logicEncodingMatches)
                {
                    string address = logicalMatch.Groups[2].Value.ToString();
                    if (address.StartsWith("0x") == false)
                        address = address.ConvertToHex();
                    string description = logicalMatch.Groups[3].Value.ToString();
                    encoding.EncodingTypes.Add(new LogicalEncodingValue(address, description));
                }

                Encodings.Add(encodingName, encoding);
            }
        }

        /// <summary>
        /// Extracts the encodings representation.
        /// <para>
        /// There may or may not be encoding representations based on the encodings
        /// </para>
        /// </summary>
        /// <param name="signalEncodingRepresentation">The signal encoding representation.</param>
        /// <exception cref="Exception">
        /// </exception>
        private void ExtractEncodingsRepresentation(string signalEncodingRepresentation)
        {
            if (string.IsNullOrEmpty(signalEncodingRepresentation))
            {
                Logger.LogWarning($"{FileName} does not consist any Encoding Representations at {FileNameWithPath}");
                return;
            }

            Regex encodingRepresentationPattern = new Regex(@"^\s*([A-Z]\w+)\s*:\s*((?:.+?(?:,\s*|;)){1,})", RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.Compiled);
            MatchCollection encodingRepresentationMatches = encodingRepresentationPattern.Matches(signalEncodingRepresentation);
            char[] whiteSpaces = new char[] { ' ', '\r', '\n' };
            foreach (Match encodingRepresentation in encodingRepresentationMatches)
            {
                string encodingName = encodingRepresentation.Groups[1].Value.ToString();
                if (Encodings.TryGetValue(encodingName, out EncodingNode encodingNode))
                {
                    string[] signalNames = encodingRepresentation.Groups[2].Value.ToString().Split(',');
                    foreach (string signalName in signalNames)
                    {
                        if (Signals.TryGetValue(signalName.Trim(whiteSpaces), out Signal signal))
                        {
                            signal.UpdateEncoding(encodingNode);
                        }
                        else
                            Logger.LogWarning($"Encoding signal '{signalName}' not found in list of signals");
                    }
                }
                else
                    Logger.LogWarning($"Encoding '{encodingName}' not found in list of encodings");
            }
        }

        /// <summary>
        /// Extracts the frames from the LDF File
        /// </summary>
        /// <param name="framesContent">Content of the frames.</param>
        /// <exception cref="Exception">
        /// </exception>
        private void ExtractFrames(string framesContent)
        {
            Regex framePattern = new Regex(@"^\s*([A-Z]\w+)\s*:\s*((?:0x)?[0-9A-F]{1,2}),\s*(\S+),\s*(\d+)\s*\x7B.*?\x7D", RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.Compiled);
            Regex frameSignalPattern = new Regex(@"^\s*([A-Z]\w+),\s*(\d+)\s*;\s*?", RegexOptions.Multiline | RegexOptions.Compiled);

            MatchCollection frames = framePattern.Matches(framesContent);
            if (frames.Count == 0) throw new InvalidDataException("Unable to locate any frame definitions from extracted content");

            foreach (Match foundFrame in frames)
            {
                string frameName             = foundFrame.Groups[1].Value.ToString();
                string frameID               = foundFrame.Groups[2].Value.ToString();
                string frameLength           = foundFrame.Groups[4].Value.ToString();

                if (int.TryParse(frameLength, out int length) == false)
                    throw new Exception($"Frame {frameName} has an invalid frame length: {frameLength}");

                Frame frame = new Frame(frameName, frameID, length);

                MatchCollection frameSignals = frameSignalPattern.Matches(foundFrame.Value);
                foreach (Match frameSignal in frameSignals)
                {
                    string signalName       = frameSignal.Groups[1].Value.ToString();
                    string signalStartIndex = frameSignal.Groups[2].Value.ToString();
                    if (Signals.TryGetValue(signalName, out Signal signal))
                    {
                        if (int.TryParse(signalStartIndex, out int signalIndex) == false)
                            throw new Exception($"Signal {signalName} has an invalid starting index: {signalStartIndex}");

                        signal.StartAddress = signalIndex;
                        frame.Signals.Add(signal);
                    }
                    else
                        throw new SignalNotFoundException($"Signal named {signalName} is not found from the parsed data of the LDF file");
                }
                ConfigureNoDataBits(frame);
                frame.Signals = frame.Signals.OrderBy(s => s.StartAddress).ToList();
                Frames.Add(frame);
            }
        }

        private void ConfigureNoDataBits(Frame frame)
        {
            try
            {
                int j = 1;

                for (int i = 0; i < frame.Signals.Count; i++)
                {
                    if (j == frame.Signals.Count)
                    {
                        // Finished 
                        break;
                    }

                    int difference = frame.Signals[j].StartAddress - frame.Signals[i].StartAddress;
                    var dataIsGood = difference == frame.Signals[i].Size;

                    if (dataIsGood)
                    {
                        // we go on to next one
                        j++;
                        continue;
                    }

                    // tell the signal has no use data bits after

                    frame.Signals[i].HasNoUseDataBitsAfter = true;
                    frame.Signals[i].NumberOfUseDataBits= difference - frame.Signals[i].Size;

                    j++;
                }
            }
            catch (Exception exc) 
            {
                Logger.LogError(exc);
            }
        }

        private void ExtractLINDescription()
        {
            bool protocolVersionFound = false;
            bool languageVersionFound = false;
            bool speedFound = false;

            foreach (var newline in FileParsedData.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (newline.Contains(NodeNames.ProtocolVersion))
                {
                    ProtocolVersion = ExtractLINDescription(newline, 2);
                    protocolVersionFound = true;
                }
                else if (newline.Contains(NodeNames.LanguageVersion))
                {
                    var test = newline.Split(new char[] { CharSymbol.WhiteSpace, ';', '"' }, StringSplitOptions.RemoveEmptyEntries);
                    LanguageVersion = ExtractLINDescription(newline, 2);
                    languageVersionFound = true;
                }
                else if (newline.Contains(NodeNames.Speed))
                {
                    var test = newline.Split(new char[] { CharSymbol.WhiteSpace, ';', '"' }, StringSplitOptions.RemoveEmptyEntries);
                    Speed = ExtractLINDescription(newline, 2, true);
                    speedFound = true;
                }
                
                if (protocolVersionFound && languageVersionFound && speedFound)
                {
                    break;
                }
            }
        }
        /// <summary>
        /// Extracts the signals from the LDF File
        /// </summary>
        /// <param name="signalContent">Content of the signal.</param>
        /// <exception cref="ArgumentException">'{nameof(signalContent)}' cannot be null or empty. - signalContent</exception>
        /// <exception cref="Exception"></exception>
        private void ExtractSignals(string signalContent)
        {
            if (string.IsNullOrEmpty(signalContent))
                throw new ArgumentException($"'{nameof(signalContent)}' cannot be null or empty.", nameof(signalContent));

            var allsignals = new Regex(@"^\s*([A-Z]\w+):\s*(\d+),\s*(\d+),(?:\s*\S+,){1,}\s*\S+\s*;", RegexOptions.Multiline | RegexOptions.Compiled);

            MatchCollection signals = allsignals.Matches(signalContent);
            if (signals.Count == 0) throw new InvalidDataException("Unable to locate any signal definitions from extracted content");

            foreach (Match signal in signals)
            {
                if (signal.Success == false) throw new InvalidDataException($"Signal does not match expected format");

                // signal.Groups[0] - the entire line that matched
                // signal.Groups[1] - the signal name [the first backreference (first pair of parentheses)]
                // signal.Groups[2] - the signal bit length [the second backreference (second pair of parentheses)]
                // signal.Groups[3] - the signal initial value [the third backreference (third pair of parentheses)]
                string signalName         = signal.Groups[1].Value.ToString();
                string signalSize         = signal.Groups[2].Value.ToString();
                string signalInitialValue = signal.Groups[3].Value.ToString();
                string[] lineItemsSeparated = signal.Groups[0].Value.ToString().Trim(new char[] { ' ', '\r', '\n' }).Split(new char[] { ',', ';', ':', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
                Signals.Add(signalName, new Signal(signalName, signalSize, signalInitialValue, lineItemsSeparated));
            }
        }

        private string GetFrameNodeContent() => GetNodeContent(new Regex(@"^Frames.*?\x7B(?:\s*?(?:[A-Z]\w+)\s*:\s*(?:0x)?(?:[0-9A-F]{1,2}),\s*(?:\S+),\s*(?:\d+)\s*\x7B.*?\x7D){1,}\s*?\x7D", RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.Compiled));
        private string GetSignalNodeContent() => GetNodeContent(new Regex(@"^Signals.*?\x7B(?:\s*?(?:[A-Z]\w+):\s*(?:\d+),\s*(?:\d+),(?:\s*\S+,){1,}\s*\S+\s*;){1,}\s*?\x7D", RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.Compiled));
        private string GetSignalEncodingNodeContent() => GetNodeContent(new Regex(@"^Signal_encoding_types.*?\x7B(?:\s*?[A-Z]\w+\s*\x7B.*?\x7D){1,}\s*?\x7D", RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.Compiled));
        private string GetSignalRepresentationNodeContent() => GetNodeContent(new Regex(@"^Signal_representation.*?\x7B.*?\x7D", RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.Compiled));

        private string GetNodeContent(Regex nodePattern, bool mustExist = true, [CallerMemberName] string caller = null)
        {
            Match match = nodePattern.Match(FileParsedData);
            string nodeContent = string.Empty;
            if (match.Success == false && mustExist)
                throw new InvalidDataException($"Unable to locate desired content ({caller})");
            else if (match.Success == true)
                nodeContent = match.Value;
            return nodeContent;
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
