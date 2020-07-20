﻿using ConverterTool.Logger;
using ConverterTool.WrapperTypes;
using System;
using System.Collections.Generic;
using System.IO;

namespace ConverterTool.LanguageRules
{
    internal class JsonRule : LanguageRule
    {
        public JsonRule(string filename) : base(LanguageType.MARKUP_LANG, ProgramType.JSON, filename)
        {

        }

        public override void ScanFile()
        {
            var fileContents = File.ReadAllText(this.FullFile);
            bool isLocked = false;
            string hold = string.Empty;
            Log.Info("Scanning Json file.");
            for (int index = 0; index < fileContents.Length; index++)
            {
                if (isLocked)
                {
                    if (fileContents[index] == '\"')
                    {
                        this.TokenList.Add(hold);
                        this.TokenList.Add(fileContents[index].ToString());
                        hold = string.Empty;
                        isLocked = false;
                        continue;
                    }
                    hold += fileContents[index].ToString();
                }
                else
                {
                    if (fileContents[index] == ' ' || fileContents[index] == '\n' || fileContents[index] == '\r') continue;
                    else if (fileContents[index] == '-' || char.IsDigit(fileContents[index]))
                    {
                        string number = fileContents[index++].ToString();
                        while(char.IsDigit(fileContents[index]) || fileContents[index] == '.')
                        {
                            number += fileContents[index++];
                        }
                        this.TokenList.Add(number);
                        index -= 1;
                        continue;
                    }
                    else if (fileContents[index] == 't')
                    {
                        if (fileContents[index++] != 't')
                            throw new Exception();
                        if (fileContents[index++] != 'r')
                            throw new Exception();
                        if (fileContents[index++] != 'u')
                            throw new Exception();
                        if (fileContents[index++] != 'e')
                            throw new Exception();
                        this.TokenList.Add("true");
                        index -= 1;
                    }
                    else if (fileContents[index] == 'f')
                    {
                        if (fileContents[index++] != 'f')
                            throw new Exception();
                        if (fileContents[index++] != 'a')
                            throw new Exception();
                        if (fileContents[index++] != 'l')
                            throw new Exception();
                        if (fileContents[index++] != 's')
                            throw new Exception();
                        if (fileContents[index++] != 'e')
                            throw new Exception();
                        this.TokenList.Add("false");
                        index -= 1;
                    }
                    else
                    {
                        switch (fileContents[index])
                        {
                            case '\"':
                                isLocked = true;
                                this.TokenList.Add(fileContents[index].ToString());
                                continue;
                            case '{':
                            case '}':
                            case '[':
                            case ']':
                            case ':':
                            case ',':
                                this.TokenList.Add(fileContents[index].ToString());
                                continue;
                        }
                    }
                }
            }
            Log.Success("Scanning Json File Completed.");
        }

        public override void ParseFile()
        {
            Log.Info("Staring to Parse Json file.");
            WrapperType mainNode;
            int index = 0;
            if (this.TokenList[index] == "{")
            {
                mainNode = new WrapperObject(this.Filename.Split('.')[0], null);
                _ = ParseObject(++index, mainNode as WrapperObject);
            }
            else if (this.TokenList[index] == "[")
            {
                mainNode = new WrapperArray(this.Filename.Split('.')[0], null);
                _ = ParseArray(++index, mainNode as WrapperArray);
            }
            else
            {
                throw new Exception("Invalid start to JSON Parsing.");
            }
            this.Structure.Add(mainNode);
            Log.Success("Parsing Json is Completed.");
        }

        private int ParseValue(int index, string valueName, WrapperObject parentNode)
        {
            WrapperType actualValue = null;

            if (this.TokenList[index].Contains("\""))
            {
                if (this.TokenList[index++] != "\"")
                    throw new Exception("Invalid Token. Need first double quote for string value");
                actualValue = new WrapperString(valueName, this.TokenList[index++]);
                if (this.TokenList[index++] != "\"")
                    throw new Exception("Invalid Token. Need last double quote for string value");
            }
            else if (this.TokenList[index].Contains("."))
            {
                var proposedDouble = this.TokenList[index].Split(".");
                if (int.TryParse(proposedDouble[0], out _) && int.TryParse(proposedDouble[1], out _))
                {
                    actualValue = new WrapperDouble(valueName, double.Parse(this.TokenList[index++]));
                }
                else
                {
                    throw new Exception("This is an invlid Double Value");
                }
            }
            else if (int.TryParse(this.TokenList[index], out _))
            {
                actualValue = new WrapperInt(valueName, int.Parse(this.TokenList[index++]));
            }
            else if (this.TokenList[index] == "true" || this.TokenList[index] == "false")
            {
                bool boolVal = bool.Parse(this.TokenList[index++]);
                actualValue = new WrapperBool(valueName, boolVal);
            }
            else if (this.TokenList[index] == "{")
            {
                index++;
                actualValue = new WrapperObject(valueName, null);
                index = ParseObject(index, actualValue as WrapperObject);
                index++;
            }
            else if (this.TokenList[index] == "[")
            {
                index++;
                actualValue = new WrapperArray(valueName, null);
                index = ParseArray(index, actualValue as WrapperArray);
                index++;
            }
            
            parentNode.Value.Add(actualValue);
            return index;
        }

        private int ParseArray(int index, WrapperArray parentNode)
        {
            parentNode.Value = new List<WrapperObject>();
            int id = 0;
            while (this.TokenList[index] != "]")
            {
                if (this.TokenList[index++] != "{")
                    throw new Exception("Need object for arrays");

                var actualValue = new WrapperObject(string.Format("ID-", id++), null);
                index = ParseObject(index, actualValue);
                index++;

                bool hasNoId = true;
                foreach (var new_id in actualValue.Value)
                {
                    if (new_id.VariableName.ToLower() == "id")
                    {
                        var tempNode = new_id as WrapperInt;
                        actualValue.VariableName = tempNode.VariableName + "-" + tempNode.Value;
                        hasNoId = false;
                        break;
                    }
                }

                if (hasNoId)
                    throw new Exception("Array entry has no valid id. Please place id value in Json.");

                parentNode.Value.Add(actualValue);

                if (this.TokenList[index] == ",")
                {
                    index++;
                }
                else if (this.TokenList[index] == "]")
                {
                    // will cancel out
                }
                else
                {
                    throw new Exception("Invalid Token. Need last double quote for name of value");
                }
            }
            return index;
        }

        private int ParseObject(int index, WrapperObject parentNode)
        {
            parentNode.Value = new List<WrapperType>();
            while (this.TokenList[index] != "}")
            {
                string valueName = string.Empty;
                
                if (this.TokenList[index++] != "\"")
                    throw new Exception("Invalid Token. Need first double quote for name of value");
                valueName = this.TokenList[index++];
                if (this.TokenList[index++] != "\"")
                    throw new Exception("Invalid Token. Need last double quote for name of value");
                if (this.TokenList[index++] != ":")
                    throw new Exception("Invalid Token. Need \":\" for divider of value");

                index = ParseValue(index, valueName, parentNode);
                
                if (this.TokenList[index] == ",")
                {
                    index++;
                }
                else if (this.TokenList[index] == "}")
                {
                    // will cancel out
                }
                else
                {
                    throw new Exception("Invalid Token. Need last double quote for name of value");
                }
            }
            return index;
        }

        private void BuildObject(WrapperObject mainNode, string tabs)
        {
            if (mainNode.VariableName != string.Empty)
                this.Results += $"{tabs}\"{mainNode.VariableName}\": {{\n";
            for (int index = 0; index < mainNode.Value.Count; index++)
            {
                var node = mainNode.Value[index];
                switch (node)
                {
                    case WrapperArray wrapperArray:
                        BuildArray(wrapperArray, tabs + "\t");
                        break;
                    case WrapperObject wrapperObject:
                        BuildObject(wrapperObject, tabs + "\t");
                        break;
                    case WrapperBool wrapperBool:
                        this.Results += $"{tabs + "\t"}\"{wrapperBool.VariableName}\": {wrapperBool.Value.ToString().ToLower()}";
                        break;
                    case WrapperDouble wrapperDouble:
                        this.Results += $"{tabs + "\t"}\"{wrapperDouble.VariableName}\": {wrapperDouble.Value}";
                        break;
                    case WrapperInt wrapperInt:
                        this.Results += $"{tabs + "\t"}\"{wrapperInt.VariableName}\": {wrapperInt.Value}";
                        break;
                    case WrapperString wrapperString:
                        this.Results += $"{tabs + "\t"}\"{wrapperString.VariableName}\": \"{wrapperString.Value}\"";
                        break;
                    default:
                        throw new Exception("This type is invalid for build the file.");
                }
                if (index != mainNode.Value.Count - 1)
                    this.Results += ",\n";
                else
                    this.Results += "\n";
            }
            if (mainNode.VariableName != string.Empty)
                this.Results += $"{tabs}}}";
        }

        private void BuildArray(WrapperArray mainNode, string tabs)
        {
            if (mainNode.VariableName != string.Empty)
                this.Results += $"{tabs}\"{mainNode.VariableName}\": [\n";
            for (int index = 0; index < mainNode.Value.Count; index++)
            {
                var node = mainNode.Value[index];
                switch (node)
                {
                    case WrapperObject wrapperObject:
                        wrapperObject.VariableName = string.Empty;
                        this.Results += $"{tabs + "\t"}{{\n";
                        BuildObject(wrapperObject, tabs + "\t");
                        if (index != mainNode.Value.Count - 1)
                            this.Results += $"{tabs + "\t"}}},\n";
                        else
                            this.Results += $"{tabs + "\t"}}}\n";
                        break;
                    default:
                        throw new Exception("This type is invalid for build the file.");
                }
            }
            if (mainNode.VariableName != string.Empty)
                this.Results += $"{tabs}]";
        }

        public override void BuildFile()
        {
            this.Structure[0].VariableName = string.Empty;
            switch (this.Structure[0])
            {
                case WrapperArray wrapperArray:
                    this.Results += "[\n";
                    BuildArray(wrapperArray, "");
                    this.Results += "]\n";
                    break;
                case WrapperObject wrapperObject:
                    this.Results += "{\n";
                    BuildObject(wrapperObject, "");
                    this.Results += "}\n";
                    break;
            }

            // Create a file to write to.
            using (StreamWriter sw = File.CreateText(this.FullFile))
            {
                string results = this.Results;
                foreach (var character in results)
                {
                    sw.Write(character);
                }
            }
        }

        protected override void CreateKeywords()
        {
            throw new Exception("Json Rules do not need to create Keywords.");
        }
    }
}
