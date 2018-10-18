using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

internal static class Gd3Parser
{
    public static string GetTagInfo(string vgmFile)
    {
        var tags = new List<string>();
        // Open the stream
        using (var f = new FileStream(vgmFile, FileMode.Open))
        using (var s = new GZipStream(f, CompressionMode.Decompress))
        using (var r = new BinaryReader(s, Encoding.Unicode))
        {
            // Skip to GD3 offset
            r.ReadBytes(0x14);
            var gd3Offset = r.ReadInt32();
            r.ReadBytes(gd3Offset + 8);
            var str = "";
            for (int i = 0; i < 11; ++i)
            {
                for (;;)
                {
                    var c = r.ReadChar();
                    if (c == '\0')
                    {
                        tags.Add(str);
                        str = "";
                        break;
                    }

                    str += c;
                }
            }
        }

        var title = FormatTags(tags[0], tags[1]);
        var game = FormatTags(tags[2], tags[3]);
        var system = FormatTags(tags[4], tags[5]);
        var composer = FormatTags(tags[6], tags[7]);
        var date = tags[8].Length > 0 ? tags[8] : null;
        var ripper = tags[9].Length > 0 ? tags[9] : null;
            
        var sb = new StringBuilder();

        // Track Title - Game - System (Date)
        // Composer
        // Ripped by Ripper

        if (title != null) sb.Append(title);
        if (game != null) sb.Append($" – {game}");
        if (system != null) sb.Append($" – {system}");
        if (date != null) sb.Append($" ({date})");
        sb.AppendLine();
        if (composer != null) sb.Append(composer);
        sb.AppendLine();
        if (ripper != null) sb.Append($"Ripped by {ripper}");

        return sb.ToString();
    }

    private static string FormatTags(string english, string japanese)
    {
        return english.Length > 0 ? japanese.Length > 0 ? $"{english} ({japanese})" : $"{english}" : null;
    }
}