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

            [JsonProperty("stat")]
            public List<Stat> Stats { get; set; }

            [JsonProperty("implicitStatsToMatch")]
            public List<string> ImplicitStatsToMatch { get; set; }

            [JsonProperty("enabled")]
            public bool Enabled { get; set; }
        }

        public class Stat
        {
            [JsonProperty("name")]
            public string Name { get; set; }

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
                            ImplicitStatsToMatch = item.ImplicitStatsToMatch,
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
            // Initialize variables
            string itemType = string.Empty;
            List<string> matchedStrings = new List<string>();
            bool inStatsSection = false;

            // Split the input string into lines
            string[] lines = str.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            int lastSeparatorLineIndex = lines
                .Select((line, index) => new { line, index })
                .Where(x => x.line == "--------\r")
                .Select(x => x.index)
                .ToList()
                .LastOrDefault();

            // Iterate over the lines to find the item type and the stats section
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                // Check for item type
                if (line.StartsWith("Item Class: "))
                {
                    itemType = line.Substring("Item Class: ".Length).Trim();
                    i = lastSeparatorLineIndex;
                    continue; // Move to last LineIndex of separator
                }

                // Use regex to find the modifier name
                var modifierMatch = Regex.Match(line, @"Modifier\s*""([^""]+)""");

                if (modifierMatch.Success && itemType != string.Empty)
                {
                    string modifierName = modifierMatch.Groups[1].Value;

                    var item = items.Find(i => i.Type == itemType);

                    if (item != null)
                    {
                        var stat = item.Stats.Find(s => s.Name == modifierName);

                        if (stat != null)
                        {
                            var nextLineIndex = i + 1;
                            if (nextLineIndex < lines.Length)
                            {
                                matchedStrings.Add(lines[nextLineIndex].Trim());
                            }
                        }
                    }
                }
            }

            // Return the matched strings as a list
            return matchedStrings;
        }
    }
}
