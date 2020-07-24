﻿using ConverterTool.Logger;
using ConverterTool.WrapperTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ConverterTool.LanguageRules
{
    internal class JavaRule : LanguageRule
    {
        public JavaRule(string filename) : base(LanguageType.PROGRAM_LANG, ProgramType.JAVA, filename)
        {
            this.CreateKeywords();
        }

        public override void BuildFile()
        {
            Log.Warn("Java BuildFile is not ready.");
        }

        private int AddHeader(int index)
        {
            var otherHeaders = new WrapperObject("HEADERS", new List<WrapperType>());
            // TODO: make sure to implement this while multi file task
            while (this.TokenList[index].ToLower() != "class" &&
                this.TokenList[index].ToLower() != "public" &&
                this.TokenList[index].ToLower() != "private" &&
                this.TokenList[index].ToLower() != "protected")
            {
                if (this.TokenList[index].ToLower() == "import")
                {
                    index++;
                    if (this.TokenList[index].ToLower() == "java")
                    {
                        while (this.TokenList[index] != ";")
                            index++;
                    }
                    else
                    {
                        string location = string.Empty;
                        while (this.TokenList[index] != ";")
                            location += this.TokenList[index++];
                        otherHeaders.Value.Add(new WrapperString("IMPORT", location));
                    }
                }
                index++;
            }

            this.Structure.Add(otherHeaders);
            return index;
        }

        private int AddClass(int index)
        {
            var classObject = new WrapperObject("TEMP_NAME", new List<WrapperType>());

            if (this.TokenList[index].ToLower() == "public" ||
            this.TokenList[index].ToLower() == "private" ||
            this.TokenList[index].ToLower() == "protected")
            {
                classObject.Value.Add(new WrapperString("ACCESS_MOD", this.TokenList[index++].ToLower()));
            }
            else
            {
                classObject.Value.Add(new WrapperString("ACCESS_MOD", "public"));
            }

            classObject.Value.Add(new WrapperBool("IS_STATIC", this.TokenList[index].Contains("static")));

            if (this.TokenList[index].ToLower() != "class")
                throw new Exception("This is not an accurate class.");
            index++;

            classObject.VariableName = this.TokenList[index++];

            if (this.TokenList[index] != "{")
                throw new Exception("This is an invalid class opener.");
            index++;

            for (; index < this.TokenList.Count - 1; index++) ;

            if (this.TokenList[index] != "}")
                throw new Exception("This is an invalid class opener.");
            index++;

            this.Structure.Add(classObject);
            return index;
        }

        public override void ParseFile()
        {
            Log.Info("Staring to Parse Java file.");
            int index = 0;
            index = this.AddHeader(index);
            _ = this.AddClass(index);
            Log.Success("Parsing Java file is Completed.");
        }

        private string CleanSourceCode()
        {
            var fileList = File.ReadAllLines(this.FullFile);
            var ans = string.Empty;
            bool isMultiLine = false;
            Log.Info($"Removing all comments in {this.Filename}");
            foreach (var line in fileList)
            {
                string temp = line;
                string result = string.Empty;
                for (int index = 0; index < line.Length; index++)
                {
                    if (isMultiLine)
                    {
                        if (line[index] == '*' && line[index + 1] == '/')
                        {
                            index++;
                            isMultiLine = false;
                        }
                        continue;
                    }
                    else if (line[index] == '/')
                    {
                        if (line[index + 1] == '*')
                        {
                            isMultiLine = true;
                            result += line.Substring(0, index++);
                            continue;
                        }
                        if (line[index + 1] == '/')
                        {
                            result += line.Substring(0, index++);
                            break;
                        }
                    }
                    else if (index == line.Length - 1)
                    {
                        result += line.Substring(0, index + 1);
                    }
                }
                ans += result;
            }
            return ans;
        }

        public override void ScanFile()
        {
            var fileContents = this.CleanSourceCode();
            string hold = string.Empty;
            Log.Info("Scanning Java file.");
            for (int index = 0; index < fileContents.Length; index++)
            {
                if (fileContents[index] == ' ' || fileContents[index] == '\n' || fileContents[index] == '\r')
                {
                    if (!string.IsNullOrEmpty(hold) && !string.IsNullOrWhiteSpace(hold))
                    {
                        this.TokenList.Add(hold);
                        hold = string.Empty;
                    }
                    continue;
                }
                else if (this.ValidSymbol(fileContents[index]))
                {
                    if (fileContents[index] == '\"')
                    {
                        this.TokenList.Add(fileContents[index].ToString());
                        int subIndex = index + 1;
                        string content = string.Empty;
                        while (fileContents[subIndex] != '\"')
                        {
                            content += fileContents[subIndex++].ToString();
                        }
                        this.TokenList.Add(content);
                        index = subIndex;
                    }
                    else if (!string.IsNullOrEmpty(hold) && !string.IsNullOrWhiteSpace(hold))
                    {
                        this.TokenList.Add(hold);
                        hold = string.Empty;
                    }
                    this.TokenList.Add(fileContents[index].ToString());
                    continue;
                }
                else if (fileContents[index] == ';')
                {
                    if (!string.IsNullOrEmpty(hold) && !string.IsNullOrWhiteSpace(hold))
                    {
                        this.TokenList.Add(hold);
                        hold = string.Empty;
                    }
                    this.TokenList.Add(fileContents[index].ToString());
                    hold = string.Empty;
                    continue;
                }
                hold += fileContents[index];
                if (this.Keywords.Contains(hold.ToLower()))
                {
                    if (!string.IsNullOrEmpty(hold) && !string.IsNullOrWhiteSpace(hold))
                    {
                        this.TokenList.Add(hold);
                        hold = string.Empty;
                    }
                    continue;
                }
            }
            Log.Success("Scanning Java file is Completed.");
        }

        private bool ValidSymbol(char symbol)
        {
            switch (symbol)
            {
                case '{':
                case '}':
                case '(':
                case ')':
                case '[':
                case ']':
                case '-':
                case '+':
                case '%':
                case '^':
                case '*':
                case '\\':
                case '\"':
                case '\'':
                case '<':
                case '>':
                case '&':
                case '|':
                case '!':
                case '@':
                case '=':
                case ':':
                case '?':
                case ',':
                case '.':
                    return true;
                default:
                    return false;
            }
        }

        protected override void CreateKeywords()
        {
            this.Keywords = new List<string>()
            {
                "abstract",
                "assert",
                "boolean",
                "byte",
                "case",
                "catch",
                "char",
                "class",
                "const",
                "continue",
                "default",
                "do",
                "double",
                "else",
                "enum",
                "extends",
                "final",
                "finally",
                "float",
                "for",
                "goto",
                "if",
                "implements",
                "import",
                "instanceof",
                "int",
                "interface",
                "long",
                "native",
                "new",
                "package",
                "private",
                "protected",
                "public",
                "return",
                "short",
                "static",
                "strictfp",
                "super",
                "swtich",
                "synchronized",
                "this",
                "throw",
                "throws",
                "transient",
                "try",
                "void"
            };
        }
    }
}
