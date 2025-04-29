using System;
using System.IO;
using System.Linq;
using HtmlAgilityPack;
using System.Collections.Generic;

namespace ScreenScrapeReplit
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 4 || args[0] != "-file" || args[2] != "-out")
            {
                Console.WriteLine("Usage: program -file <filepath> -out <outputpath>");
                return;
            }

            string filePath = args[1];
            string outputPath = args[3];

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"File not found: {filePath}");
                return;
            }

            try
            {
                using (var writer = new StreamWriter(outputPath))
                {
                    var doc = new HtmlDocument();
                    doc.Load(filePath);

                    writer.WriteLine($"{"Days",5}, {"Message#",8}, Text");
                    writer.WriteLine(new string('-', 80));

                    var userMessages = doc.DocumentNode.SelectNodes("//*[@data-cy='user-message']");
                    
                    if (userMessages != null)
                    {
                        int messageNumber = 1;
                        foreach (var message in userMessages)
                        {
                            ProcessUserMessage(message, messageNumber, writer);
                            messageNumber++;
                        }
                    }
                }
                Console.WriteLine($"Output written to: {outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing file: {ex.Message}");
            }
        }
        static void ProcessUserMessage(HtmlNode message,int messageNumber,StreamWriter writer)
        {
            var paragraphs = message.SelectNodes(".//p|.//span");
            if (paragraphs != null)
            {
                var messageText = new List<(int days, string text, int idx)>();
                int days = -1;
                var tempMessages = new List<(string text, int idx)>();

                for (int idx = 0; idx < paragraphs.Count; idx++)
                {
                    var p = paragraphs[idx];
                    var text = p.InnerText.Trim();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        tempMessages.Add((text, idx));
                    }
                }

                // Find highest days value and filter messages
                int highestDays = 0;
                var remainingMessages = new List<(string text, int idx)>();

                foreach (var (text, idx) in tempMessages)
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(text, @"^\d+ days? ago$"))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(text, @"(\d+)");
                        if (match.Success)
                        {
                            var daysValue = int.Parse(match.Groups[1].Value);
                            highestDays = Math.Max(highestDays, daysValue);
                        }
                        continue;
                    }

                    if (remainingMessages.Count == 0 || remainingMessages[remainingMessages.Count - 1].text != text)
                    {
                        remainingMessages.Add((text, idx));
                    }
                }

                // Before adding to messageText, check if first message starts with "Forbidden"
                if (remainingMessages.Count > 0 && remainingMessages[0].text.StartsWith("Forbidden", StringComparison.OrdinalIgnoreCase))
                {
                    string combinedText = string.Join(" ", remainingMessages.Select(m => m.text.Replace("\r", " ").Replace("\n", " ").Trim()));
                    messageText.Add((highestDays, combinedText, remainingMessages[0].idx));
                }
                else
                {
                    messageText.AddRange(remainingMessages.Select(m => (highestDays, m.text.Replace("\r", " ").Replace("\n", " ").Trim(), m.idx)));
                }

                foreach (var (ddays, text, idx) in messageText)
                {
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        string daysStr = ddays >= 0 ? ddays.ToString() : "-";
                        // Escape any quotes in the text and ensure the whole text is quoted
                        string escapedText = text.Replace("\"", "\"\"");
                        writer.WriteLine($"{daysStr,5},{message.Line},\"{escapedText}\"");
                    }
                }
            }

        }
    }
}
