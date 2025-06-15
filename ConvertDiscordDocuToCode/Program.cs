using System.Text;

namespace ConvertDiscordDocuToCode
{
    internal class Program
    {
        private static readonly Dictionary<DiscordDataType, JsonConverter> _typeToConverter = new()
        {
            { DiscordDataType.snowflake, JsonConverter.SnowflakeConverter },
        };

        static void Main()
        {
            PrintOptions();
            ConvertDocuToClass();
            Console.WriteLine("");
            Main();
        }

        static void PrintOptions()
        {
            Console.WriteLine("Discord documentation to code converter! BY Cristiano3120!");
            Console.WriteLine("1. Convert to class");
            Console.WriteLine("2. Convert to enum");
            Console.WriteLine("3. Convert to Flags-Enum");
            Console.Write("Enter option: ");
            ReadOptions();
        }

        static void ReadOptions()
        {
            ConsoleKey key = Console.ReadKey().Key;
            switch (key)
            {
                case ConsoleKey.D1:
                    Console.WriteLine("\n");
                    ConvertDocuToClass();
                    break;
                case ConsoleKey.D2:
                    ConvertToEnum();
                    break;
                case ConsoleKey.D3:
                    ConvertToFlagsEnum();
                    break;
                default:
                    Console.WriteLine("Invalid option :(");
                    PrintOptions();
                    break;
            }
        }

        static void ConvertDocuToClass()
        {
            Console.Write("Paste the documentation in and then press enter 2 times: ");
            var lines = new List<string>();
            string? line;
            while ((line = Console.ReadLine()) is not null)
            {
                if (line == "")
                    break;

                lines.Add(line);
            }

            List<string> @classCode = [];
            CreateClass(classCode);

            foreach (string copiedLine in lines)
            {
                ConvertLine(classCode, copiedLine);
            }

            OutputDocu(classCode);
        }

        static void ConvertToEnum()
        {
            //Musst manchmal in 3 teilen manchmal in 2
        }

        static void ConvertToFlagsEnum()
        {

        }

        static void ConvertLine(List<string> codeParts, string line)
        {
            string[] parts = line.Split('\t', 3, StringSplitOptions.RemoveEmptyEntries);
            bool nullable = CheckForNullable(parts);
            bool convertionSucceded = false;
            string dataType;
            
            if (Enum.TryParse($"{parts[1].Replace("?", "").Replace(" ", "")}", out DiscordDataType discordDataType))
            {
                dataType = ToCSharpType(discordDataType, nullable);
                convertionSucceded = true;
            }
            else
            {
                dataType = GetCSharpDataType(parts[1], nullable);
            }

            AddSummary(codeParts, parts[2]);
            AddJsonPropertyName(codeParts, parts[0]);
            if (convertionSucceded)
                AddConverter(codeParts, discordDataType);
            AddProperty(codeParts, dataType, parts[0]);
        }

        static bool CheckForNullable(string[] parts)
            => parts[0].Contains('?') || parts[1].StartsWith('?');

        static void OutputDocu(List<string> parts)
        {
            parts.RemoveAt(parts.Count - 2);
            foreach (string codePart in parts)
            {
                Console.WriteLine(codePart);
            }
        }

        static string GetCSharpDataType(string discordDataType, bool nullable)
        {
            if (discordDataType.Contains(" object", StringComparison.OrdinalIgnoreCase))
            {
                discordDataType = discordDataType.Replace(" object", "", StringComparison.OrdinalIgnoreCase);
            }
            else if (discordDataType.Contains(" objects", StringComparison.OrdinalIgnoreCase))
            {
                discordDataType = discordDataType.Replace("objects", "", StringComparison.OrdinalIgnoreCase);
            }

            if (discordDataType.Contains("partial", StringComparison.OrdinalIgnoreCase))
            {
                discordDataType = discordDataType.Replace("partial", "", StringComparison.OrdinalIgnoreCase).Trim();
            }

            if (discordDataType.StartsWith("array of ", StringComparison.OrdinalIgnoreCase))
            {
                string innerType = discordDataType[9..];
                discordDataType = $"{ToCSharpType(innerType)}[]";
            }
            else if (discordDataType.StartsWith("List of ", StringComparison.OrdinalIgnoreCase))
            {
                string innerType = discordDataType[8..];
                discordDataType = $"List<{ToCSharpType(innerType)}>";
            }
            else if (discordDataType.StartsWith("Map of ", StringComparison.OrdinalIgnoreCase))
            {
                discordDataType = discordDataType.Replace("Map of", "", StringComparison.OrdinalIgnoreCase).Trim();
                string[] parts = discordDataType.Split(" ", 3);
                discordDataType = $"Dictionary<{ToCSharpType(parts[0])}, {ToCSharpType(parts[2])}>";
            }

            if (discordDataType.Contains("IS08601 timestamp", StringComparison.OrdinalIgnoreCase))
            {
                discordDataType = "DateTimeOffset";
            }
            
            if (nullable && discordDataType.Last() != '?')
            {
                discordDataType += "?";
            }
            
            if (discordDataType is "integer or string" or "integer or string?")
                return nullable == true
                    ? "string?"
                    : "string";

            return ToPascalCase(discordDataType);
        }

        #region CreateCodeParts

        static void CreateClass(List<string> codeParts)
        {
            codeParts.Add("public class TestClass");
            codeParts.Add("{");
            codeParts.Add("}");
        }

        static void AddProperty(List<string> codeParts, string dataType, string jsonName)
        {
            jsonName = ToPascalCase(jsonName);
            jsonName = new string([.. jsonName.TakeWhile(char.IsLetter)]);
            codeParts.Insert(codeParts.Count - 1, $"{AddTab()}public {dataType} {jsonName} " + "{ get; init; }");
            AddEmptyLine(codeParts);
        }

        static void AddSummary(List<string> codeParts, string summary)
        {
            codeParts.Insert(codeParts.Count - 1, $"{AddTab()}/// <summary>");
            codeParts.Insert(codeParts.Count - 1, $"{AddTab()}/// {summary}");
            codeParts.Insert(codeParts.Count - 1, $"{AddTab()}/// </summary>");
        }

        static void AddJsonPropertyName(List<string> codeParts, string jsonName)
        {
            jsonName = new string([.. jsonName.TakeWhile(x => char.IsLetter(x) || x == '_')]);
            codeParts.Insert(codeParts.Count - 1, $"{AddTab()}[JsonPropertyName(\"{jsonName}\")]");
        }

        static void AddConverter(List<string> codeParts, DiscordDataType discordDataType)
        {
            if (_typeToConverter.TryGetValue(discordDataType, out JsonConverter converter))
            {
                codeParts.Insert(codeParts.Count - 1, $"{AddTab()}[JsonConverter(typeof({converter}))]");
            }
        }

        static void AddEmptyLine(List<string> codeParts)
            => codeParts.Insert(codeParts.Count - 1, "");

        static string AddTab()
            => "\t";


        #endregion

        #region ToMethods(Converts)

        static string ToCSharpType(DiscordDataType discordDataType, bool nullable)
        {
            CsharpDataType csharpDataType = (CsharpDataType)discordDataType;

            return nullable
                ? csharpDataType.ToString() + "?"
                : csharpDataType.ToString();
        }

        static string ToCSharpType(string str)
        {
            if (Enum.TryParse<CsharpDataType>(str, out CsharpDataType dataType))
            {
                return dataType.ToString();
            }

            return str;
        }

        static string ToPascalCase(string str)
        {
            StringBuilder stringBuilder = new(str.Length);
            bool upperNext = true;

            foreach (char c in str)
            {
                if (c == '_' || c == ' ' || c == '?')
                {
                    upperNext = true;
                    continue;
                }

                stringBuilder.Append(upperNext ? char.ToUpperInvariant(c) : c);
                upperNext = false;
            }

            if (str.Last() == '?')
                stringBuilder.Append('?');

            return stringBuilder.ToString();
        }

        #endregion
    }
}


