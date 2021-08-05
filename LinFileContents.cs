using LDF_File_Parser.Exceptions;
using LDF_File_Parser.Extension;
using LDF_File_Parser.Logger;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace LDF_FILEPARSER
{
    public class LinFileContents : INotifyPropertyChanged
    {
        public IDictionary<string, EncodingNode> Encodings { get; set; } = new Dictionary<string, EncodingNode>();

        public string FileName { get; private set; } = string.Empty;

        public string FileNameWithPath { get; private set; } = string.Empty;

        public string FileRawData { get; }

        public ICollection<Frame> Frames { get; set; } = new HashSet<Frame>();

        public IDictionary<string, Signal> Signals { get; set; } = new Dictionary<string, Signal>();

        public LinFileContents(string fileNameWithPath)
        {
            if (string.IsNullOrEmpty(fileNameWithPath))
                throw new ArgumentException($"'{nameof(fileNameWithPath)}' cannot be null or empty.", nameof(fileNameWithPath));

            try
            {
                FileNameWithPath = fileNameWithPath;

                FileName = Path.GetFileName(FileNameWithPath);

                FileRawData = File.ReadAllText(FileNameWithPath);

                var signalContent = GetNodeContent(NodeNames.Signals);
                ExtractSignals(signalContent);

                var framesContent = GetNodeContent(NodeNames.Frames);
                ExtractFrames(framesContent);

                var signalEncodingsContent = GetNodeContent(NodeNames.SignalEncodingTypes);
                ExtractEncodings(signalEncodingsContent);

                var signalEncodingRepresentation = GetNodeContent(NodeNames.SignalRepresentation);
                ExtractEncodingsRepresentation(signalEncodingRepresentation);
            }
            catch (Exception exc)
            {
                Logger.LogError(exc,$"Parsing file unsuccessful, file name with path: {fileNameWithPath}");
                throw;
            }

            #region If bit values changed by encoding
            //var signalsWithNoEncoding = Signals.Where(s => s.Value.Encoding == null).ToArray();
            //List<int> Size = new List<int>();

            //foreach (var signal in signalsWithNoEncoding)
            //{

            //    signal.Value.Encoding = new EncodingNode("Undefined");

            //    int size = signal.Value.Size;

            //    if (size == 1)
            //    {
            //        signal.Value.Encoding.EncodingTypes.Add(new LogicalEncodingValue("0x00", "FALSE - 0"));
            //        signal.Value.Encoding.EncodingTypes.Add(new LogicalEncodingValue("0x01", "TRUE  - 1"));

            //    }
            //    else 
            //    {

            //        var numberOfPossiblities = Math.Pow(2, size);
            //        for (int i = 0; i < numberOfPossiblities; i++)
            //        {
            //            string hexAddress = i.ToString().ConvertToHex();

            //            signal.Value.Encoding.EncodingTypes.Add(new LogicalEncodingValue(hexAddress, hexAddress));
            //        }
            //    }
            //} 
            #endregion
        }

        public event PropertyChangedEventHandler PropertyChanged;

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

            // There can be cases where there are no encodings mentioned in the LDF File
            if (string.IsNullOrEmpty(signalEncodingsContent))
            {
                Logger.LogWarning($"{FileName} does not consist any Encodings at {FileNameWithPath}");
                return;
            }

            // Finds the string with end "{" and multiple whitespaces in front (Used for parsing the name and other description of frame)
            var encodingMatchingExpression = @"^\s+.*{";

            // Finds the string with end "}" and multiple whitespaces in front (Used for parsing the end of frame)
            var bracketMatchingExpression = @"^\s+\}";

            // Finds the individual encoding values from encodings (Keep in mind , these are defined in lower characters)
            var encodingValueMatchingExpression = @"^\s+[a-z].*\;";

            var encodingMatchingRegex = new Regex(encodingMatchingExpression, RegexOptions.Multiline);
            var bracketMatchingRegex = new Regex(bracketMatchingExpression, RegexOptions.Multiline);
            var encodingValueMatchRegex = new Regex(encodingValueMatchingExpression, RegexOptions.Multiline);

            var encodingGroup = encodingMatchingRegex.Matches(signalEncodingsContent);
            var bracketmatches = bracketMatchingRegex.Matches(signalEncodingsContent);


            // The number of encoding names and their node end brackets should be same in number to loop through them
            if (encodingGroup.Count != bracketmatches.Count)
            {
                // TODO see if you can throw an exception
                throw new BracketMatchException($"Number of encoding names and their node endings do not match at {nameof(ExtractEncodings)}");
            }

            // As the matches are linear and the count are same, 
            for (int i = 0; i < encodingGroup.Count; i++)
            {
                var encodingStart = encodingGroup[i].Index;
                var encodingEnd = bracketmatches[i].Index;

                // We are parsing the {GS_PosRq} out of the string hence the array will consist of only one element
                //     GS_PosRq {
                var encodingNameArray = encodingGroup[i].Value.Split(new char[] { CharSymbol.TabSpace, CharSymbol.WhiteSpace, '{' }, StringSplitOptions.RemoveEmptyEntries);

                // The encoding name array should contain only one name of the encoding, hence the length should not be more than one
                if (encodingNameArray.Length > 1)
                {
                    // throw invalid
                    // as encodingNameArray cant have more values
                    throw new InvalidDataException($"After parsing the encoding group, the array should contain only one element which is the name. Array elements: {encodingNameArray.Join()}, Count: {encodingNameArray.Length}");
                }

                string encodingName = encodingNameArray.First();

                // Substring the descriptions of the signals mentioned in the parsed encoding
                var encodingValues = signalEncodingsContent.Substring(encodingStart, (encodingEnd - encodingStart) + 1);

                var encodingValueMatches = encodingValueMatchRegex.Matches(encodingValues);

                var encodingNode = new EncodingNode(encodingName);
                foreach (var encodingValue in from Match signalmatch in encodingValueMatches
                                              let encodingValue = signalmatch.Value.Split(new char[] { ',', ';', CharSymbol.TabSpace, CharSymbol.WhiteSpace, '\"' }, StringSplitOptions.RemoveEmptyEntries)
                                              select encodingValue)
                {

                    // EncodingValue should have at least three values 
                    // TODO type of encoding, hex address , description
                    if (encodingValue.Length < 3)
                        throw new InvalidDataException($"After parsing the encoding values, the array should contain atleast 3 elememts (Encoding type, Hex address, Description). Array elements: {encodingValue.Join()}, Count: {encodingValue.Length}");

                    // the first description is the type of encoding value
                    var encodingValueType = encodingValue[0];

                    // the second description is the hex address
                    var encodingValueAddress = encodingValue[1].ConvertToHex();

                    // Check if the encoding type is logical / physical 
                    if (string.Equals(encodingValueType, EncodingValueType.Logical))
                    {
                        string encodingDescription = encodingValue[2].Trim(CharSymbol.TabSpace, CharSymbol.WhiteSpace, '"', '\"');
                        encodingNode.EncodingTypes.Add(new LogicalEncodingValue(encodingValueAddress, encodingDescription));
                    }
                    else if (string.Equals(encodingValueType, EncodingValueType.Physical))
                    {
                        PhyscialEncodingValue physicalEncoding = new PhyscialEncodingValue(encodingValueAddress);

                        // TODO I am not sure of what the other physical encoding values represent, hence storing them in a list of strings
                        // In future need to get rid of it 
                        // Skip 2 because the first value is name and the second value is hex address for all the encoding value
                        foreach (var unknownValue in encodingValue.Skip(2))
                            physicalEncoding.Values.Add(unknownValue);

                        encodingNode.EncodingTypes.Add(physicalEncoding);
                    }
                    else
                    {
                        // Invalid Encoding type, let the user know in logs
                        // Should never be the case as long as something new is added
                        throw new InvalidEncodingTypeException($"Encoding type: {encodingValueType} is invalid, the defined encodings are: {EncodingValueType.Physical}, {EncodingValueType.Logical}");
                    }
                }
                Encodings.Add(encodingName, encodingNode);
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

            var encodingMatchingExpression = @"^\s+[A-Z].*\;";

            var encodingMatchRegex = new Regex(encodingMatchingExpression, RegexOptions.Multiline);

            var encodingRepresentations = encodingMatchRegex.Matches(signalEncodingRepresentation);

            foreach (var encodingMatchArray in from Match encodingMatch in encodingRepresentations
                                               let encodingMatchArray = encodingMatch.Value.Split(new char[] { CharSymbol.TabSpace, ';', ',', ':' }, StringSplitOptions.RemoveEmptyEntries)
                                               select encodingMatchArray)
            {
                if (encodingMatchArray.Length < 2)
                {
                    // TODO the matching array should atleast have two element , first is the name of encoding and the second or the rest should be the signals it belongs to
                    throw new InvalidDataException($"After parsing the encoding matches, the array should contain atleast 2 elememts (Name , Signal linked to). Array elements: {encodingMatchArray.Join()}, Count: {encodingMatchArray.Length}");
                }

                string encodingName = encodingMatchArray[0].Trim(CharSymbol.WhiteSpace, CharSymbol.TabSpace);
                if (Encodings.TryGetValue(encodingName, out EncodingNode encodingNode))
                {
                    foreach (string signalNameValue in encodingMatchArray.Skip(1))
                    {
                        var signalName = signalNameValue.Trim(CharSymbol.WhiteSpace);

                        if (Signals.TryGetValue(signalName, out Signal signal))
                        {
                            signal.UpdateEncoding(encodingNode);
                        }
                        else
                        {
                            // TODO throw because you were not able to detect any signals present
                            throw new SignalNotFoundException($"Signal named - {signalName} not found from the given signals in LDF file in the func {nameof(ExtractEncodingsRepresentation)}; List of signals: {Signals.Select(s => s.Value.Name).Join()}");
                        }
                    }
                }
                else
                {
                    // TODO throw because you were not able to detect any encoding names
                    throw new EncodingNameNotFoundException($"Encoding Name: {encodingName} , not found in the list of encodings defined at {nameof(ExtractEncodingsRepresentation)}();");
                }
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
            // Finds the string with end "{" and multiple whitespaces in front (Used for parsing the name and other description of frame)
            var frameMatchingExpression = @"^\s+.*{";

            // Finds the string with end "}" and multiple whitespaces in front (Used for parsing the end of frame)
            var bracketMatchingExpression = @"^\s+\}";

            // Finds the individual signals inside each frame
            var signalMatchingExpression = @"^\s+[A-Z].*\;";

            var frameMatchingRegex = new Regex(frameMatchingExpression, RegexOptions.Multiline);
            var bracketMatchingRegex = new Regex(bracketMatchingExpression, RegexOptions.Multiline);
            var signalMatchRegex = new Regex(signalMatchingExpression, RegexOptions.Multiline);

            var frameGroup = frameMatchingRegex.Matches(framesContent);
            var bracketmatches = bracketMatchingRegex.Matches(framesContent);


            if (frameGroup.Count != bracketmatches.Count)
            {
                // The count should be the same , as the count defines the number of frames present in the file
                throw new BracketMatchException($"Number of frames and their node endings do not match at {nameof(ExtractFrames)}");
            }

            // As the matches are linear and the count are same, 
            for (int i = 0; i < frameGroup.Count; i++)
            {
                var frameStart = frameGroup[i].Index;
                var frameEnd = bracketmatches[i].Index;

                var frameDescription = frameGroup[i].Value.Split(new char[] { ':', '{', ',' }, StringSplitOptions.RemoveEmptyEntries);

                string frameName = frameDescription[0].Trim(CharSymbol.WhiteSpace, CharSymbol.TabSpace);

                Frame frame = new Frame(frameName, frameDescription[1], int.Parse(frameDescription[3]));

                var signalString = framesContent.Substring(frameStart, (frameEnd - frameStart) + 1);

                var signalMatches = signalMatchRegex.Matches(signalString);

                foreach (var (signalValues, signalName) in from Match signalmatch in signalMatches
                                                           let signalValues = signalmatch.Value.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                                           let signalName = signalValues.First().Trim(CharSymbol.WhiteSpace, CharSymbol.TabSpace)
                                                           select (signalValues, signalName))
                {
                    if (Signals.TryGetValue(signalName, out Signal signal))
                    {
                        signal.StartAddress = int.Parse(signalValues[1]);
                        frame.Signals.Add(signal);
                        continue;
                    }
                    else
                    {
                        // throw because you were not able to detect any signals present
                        throw new SignalNotFoundException($"Signal named - {signalName} not found from the given signals in LDF file in the func {nameof(ExtractFrames)}; List of signals: {Signals.Select(s => s.Value.Name).Join()}");
                    }
                }

                Frames.Add(frame);
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

            var allsignals = new Regex(@"^\s+[A-Z].*", RegexOptions.Multiline);

            MatchCollection signalValue = allsignals.Matches(signalContent);

            foreach (var signalArray in from Match item in signalValue
                                        let signalArray = item.Value.Split(new char[] { ',', ';', ':', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                                        select signalArray)
            {
                if (signalArray.Length < 3)
                {
                    //TODO the extracted signal string must have atleast three values , {Name, size, initial value}
                    throw new InvalidDataException($"After parsing the signal matches, the array should contain atleast 3 elememts (Name, size, Initial value). Array elements: {signalArray.Join()}, Count: {signalArray.Length}");
                }

                string signalName = signalArray[0].Trim(CharSymbol.WhiteSpace, CharSymbol.TabSpace);
                Signals.Add(signalName, new Signal(signalName, signalArray[1], signalArray[2], signalArray));
            }
        }

        /// <summary>
        /// Gets the content of the node from the LDF file in a string format
        /// </summary>
        /// <param name="nameOfNode">The name of node.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">'{nameof(nameOfNode)}' cannot be null or whitespace. - nameOfNode</exception>
        /// <exception cref="Exception"></exception>
        private string GetNodeContent(string nameOfNode)
        {
            if (string.IsNullOrWhiteSpace(nameOfNode))
                throw new ArgumentException($"'{nameof(nameOfNode)}' cannot be null or whitespace.", nameof(nameOfNode));

            var regexString = @"^(?<Name>(nameOfNode).*)\s+\{";

            regexString = Regex.Replace(regexString, "(nameOfNode)", nameOfNode);

            var nameMatchingExpression = new Regex(regexString, RegexOptions.Multiline);
            var brackeMatchingExpression = new Regex(@"^\}", RegexOptions.Multiline);
            var matches = nameMatchingExpression.Matches(FileRawData);

            string nodeContent = string.Empty;
            foreach (var contents in from Match match in matches
                                     let m = brackeMatchingExpression.Match(FileRawData, match.Index)
                                     let contents = FileRawData.Substring(match.Index, (m.Index - match.Index) + 1)
                                     select contents)
            {
                nodeContent = contents + Environment.NewLine;
            }

            return nodeContent;
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
