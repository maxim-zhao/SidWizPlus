using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace SidWiz
{
    internal class Gd3Tag
    {
        public struct MultiLanguageTag
        {
            public string English { get; set; }
            public string Japanese { get; set; }

            public override string ToString()
            {
                return English.Length > 0 ? Japanese.Length > 0 ? $"{English} ({Japanese})" : $"{English}" : "";
            }
        }
        
        public MultiLanguageTag Title { get; set; }
        public MultiLanguageTag Game { get; set; }
        public MultiLanguageTag System { get; set; }
        public MultiLanguageTag Composer { get; set; }
        public string Date { get; set; }
        public string Ripper { get; set; }

        public static Gd3Tag LoadFromVgm(string filename)
        {
            var tags = new List<string>();
            // Open the stream
            using (var f = new FileStream(filename, FileMode.Open))
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

            return new Gd3Tag
            {
                Title = new MultiLanguageTag {English = tags[0], Japanese = tags[1]},
                Game = new MultiLanguageTag {English = tags[2], Japanese = tags[3]},
                System = new MultiLanguageTag {English = tags[4], Japanese = tags[5]},
                Composer = new MultiLanguageTag {English = tags[6], Japanese = tags[7]},
                Date = tags[8],
                Ripper = tags[9],
            };
        }

        public override string ToString()
        {
            var title = Title.ToString();
            var game = Game.ToString();
            var system = System.ToString();
            var composer = Composer.ToString();
            
            var sb = new StringBuilder();

            // Track Title - Game - System (Date)
            // Composer
            // Ripped by Ripper

            if (title.Length > 0) sb.Append(title);
            if (game.Length > 0) sb.Append($" – {game}");
            if (system.Length > 0) sb.Append($" – {system}");
            if (Date.Length > 0) sb.Append($" ({Date})");
            if (composer.Length > 0)
            {
                sb.AppendLine();
                sb.Append(composer);
            }
            if (Ripper.Length > 0)
            {
                sb.AppendLine();
                sb.Append($"Ripped by {Ripper}");
            }

            return sb.ToString();
        }
    }
}