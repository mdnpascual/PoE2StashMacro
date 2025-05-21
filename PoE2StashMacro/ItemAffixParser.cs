using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace PoE2StashMacro
{
    internal class ItemAffixParser
    {
        public class Item
        {
            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("baseType")]
            public string? BaseType { get; set; }

            [JsonProperty("stat")]
            public List<Stat> Stats { get; set; }

            [JsonProperty("implicitStatsToMatch")]
            public List<string>? ImplicitStatsToMatch { get; set; }

            [JsonProperty("augmentedStatsToMatch")]
            public List<string>? AugmentedStatsToMatch { get; set; }

            [JsonProperty("enabled")]
            public bool Enabled { get; set; }
        }

        public class Stat
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("description")]
            public string Description { get; set; }

            [JsonProperty("rank")]
            public int Rank { get; set; }

            [JsonProperty("tier")]
            public int Tier { get; set; }

            [JsonProperty("enabled")]
            public bool Enabled { get; set; }
        }

        public List<Item> items;

        public ItemAffixParser()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();

                // Specify the resource name (usually the default namespace + folder + filename)
                string resourceName = "PoE2StashMacro.ItemAffix.json";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string json = reader.ReadToEnd();

                    var interim = JsonConvert.DeserializeObject<List<Item>>(json);
                    this.items = interim
                        .Where(item => item.Enabled) // Keep only items where enabled is true
                        .Select(item => new Item
                        {
                            Type = item.Type,
                            Name = item.Name,
                            BaseType = item.BaseType,
                            ImplicitStatsToMatch = item.ImplicitStatsToMatch,
                            AugmentedStatsToMatch = item.AugmentedStatsToMatch,
                            Enabled = item.Enabled,
                            Stats = item.Stats.Where(stat => stat.Enabled).ToList() // Keep only enabled stats
                        })
                        .ToList();

                    Console.WriteLine(this.items);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        public List<string> ParseString(string str)
        {
            if (str == "") return new List<string>();

            // Initialize variables
            List<string> matchedStrings = new List<string>();
            bool inStatsSection = false;

            // Split the input string into lines
            string[] lines = str.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // Remove fractured Line
            if (lines.Contains("Fractured Item\r"))
            {
                // Create a new array excluding the last two entries
                lines = lines.Take(lines.Length - 2).ToArray();
            }

            List<int> separatorLineIndex = lines
                .Select((line, index) => new { line, index })
                .Where(x => x.line == "--------\r")
                .Select(x => x.index)
                .ToList();

            int firstSeparatorLineIndex = separatorLineIndex.FirstOrDefault();

            int modifierStartIndex = lines
                .Select((line, index) => new { line, index })
                .FirstOrDefault(x => x.line.Contains("{ "))?.index ?? lines.Length;

            string? itemType = lines
                .FirstOrDefault(line => line.Trim().StartsWith("Item Class: "), String.Empty)
                ?.Substring("Item Class: ".Length).Trim();

            List<string> augmentedStats = lines
                .Where(line => line.Trim().EndsWith("(augmented)", StringComparison.OrdinalIgnoreCase)
                            && !line.Trim().StartsWith("Quality:", StringComparison.OrdinalIgnoreCase))
                .Select(line => line.Trim().Split(':')[0]) // Get the stat name before the colon
                .ToList();

            List<string> implicitStats = lines
                .Where(line => line.Trim().EndsWith("(implicit)", StringComparison.OrdinalIgnoreCase))
                .Select(line => line.Trim().Split(" (implicit)")[0]) // Get the stat name before the colon
                .ToList();

            var filteredItemType = items.Where(item => item.Type == itemType);

            var noBaseType = filteredItemType.Where(item => item.BaseType == "");
            var wBaseType = filteredItemType.Where(item => item.BaseType != "" && item.BaseType == lines[firstSeparatorLineIndex - 1]);

            var filtersItemWBase = wBaseType != null && wBaseType.Any() ? wBaseType :
                       noBaseType != null && noBaseType.Any() ? noBaseType :
                       null;

            if (filtersItemWBase != null && filtersItemWBase.Any())
            {
                var filtersItemWBaseWAugment = filtersItemWBase
                    .Where(item => item.AugmentedStatsToMatch.Count == 0 ||
                            item.AugmentedStatsToMatch.OrderBy(stat => stat).SequenceEqual(augmentedStats.OrderBy(stat => stat)));

                foreach (var item in filtersItemWBaseWAugment)
                {
                    List<string> interimMatchedLines = new List<string>();
                    for (int i = modifierStartIndex + 1; i < lines.Length; i++)
                    {
                        string line = lines[i].Trim();
                        var modifierMatch = Regex.Match(line, @"Modifier\s*""([^""]+)""");

                        if (modifierMatch.Success)
                        {
                            string modifierName = modifierMatch.Groups[1].Value;
                            var stat = item.Stats.Find(s => s.Name == modifierName);

                            if (stat != null)
                            {
                                var nextLineIndex = i + 1;
                                while (nextLineIndex < lines.Length && !lines[nextLineIndex].StartsWith('{'))
                                {
                                    if (nextLineIndex < lines.Length)
                                    {
                                        interimMatchedLines.Add(lines[nextLineIndex].Trim());
                                        nextLineIndex++;
                                    }
                                }
                            }
                        }
                    }
                    
                    if (interimMatchedLines.Count > 0)
                    {
                        matchedStrings.Add(item.Name + ":");
                        matchedStrings.AddRange(interimMatchedLines);
                    }
                }
            }

            return matchedStrings;
        }
    }
}
