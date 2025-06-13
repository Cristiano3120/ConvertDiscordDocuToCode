using System.Text;
using System.Text.Json.Serialization;

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
            Console.Write("Paste the documentation in and then press enter 2 times: ");
            ConvertDocu();
            Console.WriteLine("");
            Main();
        }

        static void ConvertDocu()
        {
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

        static void ConvertLine(List<string> codeParts, string line)
        {
            string[] parts = line.Split([' ', '\t'], 3, StringSplitOptions.RemoveEmptyEntries);
            DiscordDataType discordDataType = Enum.Parse<DiscordDataType>($"{parts[1].Replace("?", "")}");
            bool nullable = CheckForNullable(parts);

            AddSummary(codeParts, parts[2]);
            AddJsonPropertyName(codeParts, parts[0]);
            AddConverter(codeParts, discordDataType);
            AddProperty(codeParts, discordDataType, parts[0], nullable);
        }

        static bool CheckForNullable(string[] parts)
            => parts[0].EndsWith('?') || parts[1].StartsWith('?');

        static void OutputDocu(List<string> parts)
        {
            parts.RemoveAt(parts.Count - 2);
            foreach (string codePart in parts)
            {
                Console.WriteLine(codePart);
            }
        }

        #region CreateCodeParts

        static void CreateClass(List<string> codeParts)
        {
            codeParts.Add("public class TestClass");
            codeParts.Add("{");
            codeParts.Add("}");
        }

        static void AddProperty(List<string> codeParts, DiscordDataType discordDataType, string jsonName, bool nullable)
        {
            codeParts.Insert(codeParts.Count - 1, $"{AddTab()}public {ToCSharpType(discordDataType, nullable)} {ToPascalCase(jsonName)} " + "{ get; init; }");
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
            if (jsonName.EndsWith('?'))
            {
                jsonName = jsonName[..^1];
            }

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

        static string ToCSharpType(DiscordDataType dataType, bool nullable)
        {
            CsharpDataType csharpDataType = (CsharpDataType)dataType;
            string dataTypeStr = csharpDataType.ToString()[0..];

            return nullable 
                ? dataTypeStr + "?"
                : dataTypeStr;
        }

        static string ToPascalCase(string str)
        {
            StringBuilder stringBuilder = new(str.Length);
            bool upperNext = true;

            foreach (char c in str)
            {
                if (c == '?')
                    continue;

                if (c == '_')
                {
                    upperNext = true;
                    continue;
                }

                stringBuilder.Append(upperNext ? char.ToUpperInvariant(c) : c);
                upperNext = false;
            }

            return stringBuilder.ToString();
        }


        #endregion
    }
}


