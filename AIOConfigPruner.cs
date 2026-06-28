using System;
using System.Collections.Generic;
using System.IO;

public static class AIOConfigPruner
{
    public static void Prune(string path, IEnumerable<string> visibleKeys)
    {
        try
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path) || visibleKeys == null)
                return;

            HashSet<string> keep = new HashSet<string>(visibleKeys, StringComparer.OrdinalIgnoreCase);
            string[] lines = File.ReadAllLines(path);
            List<string> output = new List<string>();
            List<string> pendingComments = new List<string>();
            string section = string.Empty;
            bool sectionHasVisibleKeys = false;
            int sectionHeaderIndex = -1;

            Action flushPending = () =>
            {
                for (int i = 0; i < pendingComments.Count; i++)
                    output.Add(pendingComments[i]);
                pendingComments.Clear();
            };

            for (int i = 0; i < lines.Length; i++)
            {
                string raw = lines[i] ?? string.Empty;
                string trim = raw.Trim();

                if (trim.StartsWith("[") && trim.EndsWith("]"))
                {
                    pendingComments.Clear();
                    section = trim.Substring(1, trim.Length - 2).Trim();
                    sectionHeaderIndex = output.Count;
                    output.Add(raw);
                    sectionHasVisibleKeys = false;
                    continue;
                }

                if (trim.Length == 0)
                {
                    if (sectionHasVisibleKeys)
                        output.Add(raw);
                    else
                        pendingComments.Clear();
                    continue;
                }

                if (trim.StartsWith("#") || trim.StartsWith(";"))
                {
                    pendingComments.Add(raw);
                    continue;
                }

                int eq = raw.IndexOf('=');
                if (eq < 0)
                    continue;

                string key = raw.Substring(0, eq).Trim();
                string full = section + "." + key;
                if (keep.Contains(full))
                {
                    flushPending();
                    output.Add(raw);
                    sectionHasVisibleKeys = true;
                }
                else
                {
                    pendingComments.Clear();
                }

                if (!sectionHasVisibleKeys && sectionHeaderIndex >= 0 &&
                    (i + 1 == lines.Length || ((lines[i + 1] ?? string.Empty).Trim().StartsWith("["))))
                {
                    // Remove empty sections left behind by pruning.
                    if (sectionHeaderIndex >= 0 && sectionHeaderIndex < output.Count)
                    {
                        output.RemoveAt(sectionHeaderIndex);
                        sectionHeaderIndex = -1;
                    }
                }
            }

            // Remove trailing empty lines.
            while (output.Count > 0 && string.IsNullOrWhiteSpace(output[output.Count - 1]))
                output.RemoveAt(output.Count - 1);

            File.WriteAllText(path, string.Join(Environment.NewLine, output) + Environment.NewLine);
        }
        catch { }
    }
}
