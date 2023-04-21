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
                /*
                /* Here we are stripping out all the comments from the file
                /* First we are looking for all single line comments that use the /* */
                /*      notation to define the comment.
                /* Next we look for all single line comments that use the // notation
                /* As we remove the comments, we are also checking for empty lines, and if the line is empty after removing the comments, we simply skip adding it to the string builder
                /* Lastly, we repeat the first search but this time we allow the modifier SingleLine
                /*      meaning the search will lazily (because it has a '?' flag after it so it will stop at the first closing comment it finds).
                /* Removing the comments is necessary as the comments could break the parsing
                /*/
                Regex openCloseLineComment  = new Regex(@"\/\*.*?\*\/", RegexOptions.Compiled);
                Regex singleLineComment = new Regex(@"\/\/.*", RegexOptions.Compiled);
                Regex multiLineComment = new Regex(@"\/\*.*?\*\/", RegexOptions.Singleline | RegexOptions.Compiled);
                foreach (string line in File.ReadLines(FileNameWithPath))
                {
                    string commentRemoved = openCloseLineComment.Replace(line, "");
                    commentRemoved        = singleLineComment.Replace(commentRemoved, "");
                    if (string.IsNullOrWhiteSpace(commentRemoved.Trim(new char[] { ' ', '\r', '\n' }))) continue;
                    _ = sb.AppendLine(commentRemoved);
                }
                // Now with all the single line comments removed as well as empty lines, we can repeat the original search with an extra modifier to remove multi-line comments
                // We do this separately rather than first just to be cautious and avoid getting unwanted matches 
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
        /// Extracts the encodings defined in the Signal encoding types node
        /// A signal encoding type must be defined as such:<br/><br/>
        /// <code>
        /// Signal-Encoding-Name {
        ///     [physical_value | logical_value], Address, [arguments[, arguments]] "Description";
        ///     ..
        ///     [physical_value | logical_value], Address, [arguments[, arguments]] "Description";
        /// }
        /// </code>
        /// A physical value must be defined as such:<br/><br/>
        /// <code>
        /// physical_value, Address, [arguments[, arguments]] "Description";
        /// </code>
        /// A logical value must be defined as such:<br/><br/>
        /// <code>
        /// logical_value, Address, "Description";
        /// </code>
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
            Regex physicalEncodingPattern = new Regex(@"^\s*(physical_value)\s*,\s*(\d+)\s*,\s*((?:0x)?[0-9A-F]{1,})\s*,\s*(\d+)\s*,\s*(\d+)\s*,\s*\x22([^\x22]+)\x22\s*;", RegexOptions.Multiline | RegexOptions.Compiled);
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
        /// Extracts the frames from the LDF File frame node<br/>
        /// A frame must be defined as such:<br/><br/>
        /// <code>
        /// Frame-Name : ID, M, Some-ID, Length {
        ///     Signal-Name-1, Signal-1-Index;
        ///     Signal-Name-2, Signal-2-Index;
        ///     ...
        ///     Signal-Name-N, Signal-N-Index;
        /// }
        /// </code>
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
        /// Extracts the signals from the LDF File signal node<br/>
        /// A signal must be defined as such:<br/><br/>
        /// <code>
        /// Signal-Name : Size, InitialValue, Other-Data[, Other-Data-N]; 
        /// </code>
        /// </summary>
        /// <param name="signalContent">Content of the signal.</param>
        /// <exception cref="ArgumentException">'{nameof(signalContent)}' cannot be null or empty. - signalContent</exception>
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
                string signalName           = signal.Groups[1].Value.ToString();
                string signalSize           = signal.Groups[2].Value.ToString();
                string signalInitialValue   = signal.Groups[3].Value.ToString();
                string[] lineItemsSeparated = signal.Groups[0].Value.ToString().Trim(new char[] { ' ', '\r', '\n' }).Split(new char[] { ',', ';', ':', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
                Signals.Add(signalName, new Signal(signalName, signalSize, signalInitialValue, lineItemsSeparated));
            }
        }

        /// <summary>
        /// <para>This method will return the Frames node from the ldf file.</para>
        /// <para>
        ///     We search the file using a regular expression where we look for the following conditions:<br/>
        ///     The starting block needs to find a line that starts with the word 'Frames', this will be the key requirement for starting the match<br/>
        ///     'Frames' needs to be followed by an opening brace '{' (char 123 or 0x7B in hex), there can be any amount of whitespace before/after the brace<br/>
        ///     After the opening brace, there needs to be at least 1 frame defined<br/><br/>
        ///     A frame must be defined as such:<br/><br/>
        ///     <code>
        ///     Frame-Name : ID, M, Some-ID, Length {
        ///         Signal-Name-1, Signal-1-Index;
        ///         Signal-Name-2, Signal-2-Index;
        ///         ...
        ///         Signal-Name-N, Signal-N-Index;
        ///     }
        ///     </code>
        ///     Any amount of whitespace is permitted between any of the punctuation, however each signal must be on it's own separate line to be collected by the parser<br/>
        ///     Lastly, it must have a closing brace (char 125 or 0x7D in hex), similarly any amount of whitespace can be present before the final brace
        /// </para>
        /// </summary>
        /// <returns>Frame node content as a string</returns>
        private string GetFrameNodeContent() => GetNodeContent(new Regex(@"^Frames.*?\x7B(?:\s*?(?:[A-Z]\w+)\s*:\s*(?:0x)?(?:[0-9A-F]{1,}),\s*(?:\S+),\s*(?:\d+)\s*\x7B.*?\x7D){1,}\s*?\x7D", RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.Compiled));

        /// <summary>
        /// <para>This method will return the Signals node from the ldf file.</para>
        /// <para>
        ///     We search the file using a regular expression where we look for the following conditions:<br/>
        ///     The starting block needs to find a line that starts with the word 'Signals', this will be the key requirement for starting the match<br/>
        ///     'Signals' needs to be followed by an opening brace '{' (char 123 or 0x7B in hex), there can be any amount of whitespace before/after the brace<br/>
        ///     After the opening brace, there needs to be at least 1 signal defined<br/><br/>
        ///     A signal must be defined as such:<br/><br/>
        ///     <code>
        ///     Signal-Name : Size, InitialValue, Other-Data[, Other-Data-N]; 
        ///     </code>
        ///     Any amount of whitespace is permitted between any of the punctuation, however each signal must be on it's own separate line to be collected by the parser<br/>
        ///     Lastly, it must have a closing brace (char 125 or 0x7D in hex), similarly any amount of whitespace can be present before the final brace
        /// </para>
        /// </summary>
        /// <returns>Signal node content as a string</returns>
        private string GetSignalNodeContent() => GetNodeContent(new Regex(@"^Signals.*?\x7B(?:\s*?(?:[A-Z]\w+)\s*:\s*(?:\d+),\s*(?:\d+),(?:\s*\S+,){1,}\s*\S+\s*;){1,}\s*?\x7D", RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.Compiled));
        /// <summary>
        /// <para>This method will return the Signal encoding types node from the ldf file.</para>
        /// <para>
        ///     We search the file using a regular expression where we look for the following conditions:<br/>
        ///     The starting block needs to find a line that starts with the word 'Signal_encoding_types', this will be the key requirement for starting the match<br/>
        ///     'Signal_encoding_types' needs to be followed by an opening brace '{' (char 123 or 0x7B in hex), there can be any amount of whitespace before/after the brace<br/>
        ///     After the opening brace, there needs to be at least 1 signal defined<br/><br/>
        ///     A signal encoding type must be defined as such:<br/><br/>
        ///     <code>
        ///     Signal-Encoding-Name {
        ///         [physical_value | logical_value], Address, [arguments[, arguments]] "Description";
        ///         ..
        ///         [physical_value | logical_value], Address, [arguments[, arguments]] "Description";
        ///     }
        ///     </code>
        ///     A physical value must be defined as such:<br/><br/>
        ///     <code>
        ///     physical_value, Address, [arguments[, arguments]] "Description";
        ///     </code>
        ///     A logical value must be defined as such:<br/><br/>
        ///     <code>
        ///     logical_value, Address, "Description";
        ///     </code>
        ///     Any amount of whitespace is permitted between any of the punctuation, however each signal encoding must be on it's own separate line to be collected by the parser<br/>
        ///     Lastly, it must have a closing brace (char 125 or 0x7D in hex), similarly any amount of whitespace can be present before the final brace
        /// </para>
        /// </summary>
        /// <returns>Signal node content as a string</returns>
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
