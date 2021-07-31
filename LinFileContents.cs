using LDF_File_Parser.Extension;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LDF_FILEPARSER
{
    public class LinFileContents
    {
        public string FileNameWithPath { get; private set; } = string.Empty;
        public string FileRawData { get; }
        public ICollection<Frame> Frames { get; set; } = new HashSet<Frame>();
        public IDictionary<string, Signal> Signals { get; set; } = new Dictionary<string, Signal>();

        public ICollection<EncodingNode> Encodings { get; set; } = new HashSet<EncodingNode>();

        public LinFileContents(string fileNameWithPath)
        {
            FileNameWithPath = fileNameWithPath;

            FileRawData = File.ReadAllText(FileNameWithPath);

            var signalContent = GetNodeContent(NodeNames.Signals);
            ExtractSignals(signalContent);

            var framesContent = GetNodeContent(NodeNames.Frames);
            ExtractFrames(framesContent);

            var signalEncodingsContent = GetNodeContent(NodeNames.SignalEncodingTypes);

            if (string.IsNullOrEmpty(signalEncodingsContent) == false)
            {

                ExtractEncodings(signalEncodingsContent);
            }

        }

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
                // Got to refigure the regex
            }


            // As the matches are linear and the count are same, 
            for (int i = 0; i < frameGroup.Count; i++)
            {
                var frameStart = frameGroup[i].Index;
                var frameEnd = bracketmatches[i].Index;

                var frameDescription = frameGroup[i].Value.Split(new char[] { ':', '{', ',' }, StringSplitOptions.RemoveEmptyEntries);

                string frameName = frameDescription[0].Trim(CharSymbol.WhiteSpace);

                Frame frame = new Frame(frameName, frameDescription[1], int.Parse(frameDescription[3]));

                var signalString = framesContent.Substring(frameStart, (frameEnd - frameStart) + 1);

                var signalMatches = signalMatchRegex.Matches(signalString);

                foreach (Match signalmatch in signalMatches)
                {
                    var signalValues = signalmatch.Value.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

                    var signalName = signalValues.First().Trim(CharSymbol.WhiteSpace);

                    if (Signals.TryGetValue(signalName, out Signal signal))
                    {
                        signal.StartAddress = int.Parse(signalValues[1]);
                        frame.Signals.Add(signal);
                        continue;
                    }
                }
                Frames.Add(frame);
            }
        }

        private void ExtractEncodings(string signalEncodingsContent)
        {
            // Figure out how Encodings work

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


            if (encodingGroup.Count != bracketmatches.Count)
            {
                // Got to refigure the regex
            }


            // As the matches are linear and the count are same, 
            for (int i = 0; i < encodingGroup.Count; i++)
            {
                var encodingStart = encodingGroup[i].Index;
                var encodingEnd = bracketmatches[i].Index;


                // We are parsing the {GS_PosRq} out of the string hence the array will consist of only one element
                //     GS_PosRq {
                var encodingNameArray = encodingGroup[i].Value.Split(new char[] { CharSymbol.TabSpace, CharSymbol.WhiteSpace, '{' }, StringSplitOptions.RemoveEmptyEntries);

                if (encodingNameArray.Length > 1)
                {
                    // throw invalid
                    /// as it cant have more values
                    throw new Exception("as it cant have more values");
                }

                string encodingName = encodingNameArray.First();

                var encodingValues = signalEncodingsContent.Substring(encodingStart, (encodingEnd - encodingStart) + 1);

                var encodingValueMatches = encodingValueMatchRegex.Matches(encodingValues);

                var encodingNode = new EncodingNode(encodingName);

                foreach (Match signalmatch in encodingValueMatches)
                {
                    var encodingValue = signalmatch.Value.Split(new char[] { ',', ';', CharSymbol.TabSpace, CharSymbol.WhiteSpace }, StringSplitOptions.RemoveEmptyEntries);

                    if (encodingValue.Length < 3)
                    {
                        // it should have atleast three values 
                        // type of encoding, hex address , description
                        throw new Exception("it should have atleast three values");
                    }

                    // the first description is the type of encoding value
                    var encodingValueType = encodingValue[0];

                    // the second description is the hex address
                    var encodingValueAddress = encodingValue[1].ConvertToHex();

                    if (string.Equals(encodingValueType, EncodingValueType.Logical))
                    {
                        encodingNode.EncodingTypes.Add(new LogicalEncodingValue(encodingValueAddress, encodingValue[2]));
                    }
                    else if (string.Equals(encodingValueType, EncodingValueType.Physical))
                    {
                        PhyscialEncodingValue physicalEncoding = new PhyscialEncodingValue(encodingValueAddress);

                        // TODO I am not sure of what the other physical encoding values represent, hence storing them in a list of strings
                        // In future need to get rid of it 

                        foreach (var unknownValue in encodingValue.Skip(2))
                        {
                            physicalEncoding.Values.Add(unknownValue);
                        }

                        encodingNode.EncodingTypes.Add(physicalEncoding);
                    }
                }
                Encodings.Add(encodingNode);
            }
        }

        private void ExtractSignals(string signalContent)
        {
            if (string.IsNullOrEmpty(signalContent))
                throw new ArgumentException($"'{nameof(signalContent)}' cannot be null or empty.", nameof(signalContent));

            var allsignals = new Regex(@"^\s+[A-Z].*", RegexOptions.Multiline);

            MatchCollection signalValue = allsignals.Matches(signalContent);

            foreach (Match item in signalValue)
            {
                var signalArray = item.Value.Split(new char[] { ',', ';', ':', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                if (signalArray is null || signalArray.Length <= 3)
                {
                    // Add exception
                    throw new Exception();
                }

                string signalName = signalArray[0].Trim(CharSymbol.WhiteSpace);


                Signals.Add(signalName, new Signal(signalName, signalArray[1], signalArray[2], signalArray));
            }
        }


        private string GetNodeContent(string nameOfNode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(nameOfNode))
                    throw new ArgumentException($"'{nameof(nameOfNode)}' cannot be null or whitespace.", nameof(nameOfNode));

                var regexString = @"^(?<Name>(nameOfNode).*)\s+\{";

                regexString = Regex.Replace(regexString, "(nameOfNode)", nameOfNode);

                var nameMatchingExpression = new Regex(regexString, RegexOptions.Multiline);
                var brackeMatchingExpression = new Regex(@"^\}", RegexOptions.Multiline);
                var matches = nameMatchingExpression.Matches(FileRawData);

                string nodeContent = string.Empty;

                foreach (Match match in matches)
                {
                    var m = brackeMatchingExpression.Match(FileRawData, match.Index);
                    var contents = FileRawData.Substring(match.Index, (m.Index - match.Index) + 1);
                    nodeContent = contents + Environment.NewLine;
                }


                return nodeContent;
            }
            catch (Exception exc)
            {
                Console.WriteLine($"Error: {exc}");
                throw;
            }
        }
    }
}