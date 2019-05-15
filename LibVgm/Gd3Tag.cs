using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LibVgm
{
    public class Gd3Tag
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

        public string Ident { get; private set; }
        public decimal Version { get; private set; }
        
        public MultiLanguageTag Title { get; set; }
        public MultiLanguageTag Game { get; set; }
        public MultiLanguageTag System { get; set; }
        public MultiLanguageTag Composer { get; set; }
        public string Date { get; set; }
        public string Ripper { get; set; }
        public string Notes { get; set; }

        public static Gd3Tag LoadFromVgm(string filename)
        {
            // Open the stream
            using (var s = new OptionalGzipStream(filename))
            using (var r = new BinaryReader(s, Encoding.ASCII))
            {
                r.ReadBytes(0x14);
                var offset = r.ReadUInt32() + 0x14;
                if (offset == 0)
                {
                    // No tag
                    return null;
                }

                if (offset > s.Length - 8 - 11*2)
                {
                    throw new InvalidDataException("Not enough room in file for GD3 offset");
                }

                var result = new Gd3Tag();
                result.Parse(s, offset);
                return result;
            }
        }

        public void Parse(Stream s, uint offset)
        {
            var tags = new List<string>();
            using (var r = new BinaryReader(s, Encoding.Unicode, true))
            {
                s.Seek(offset, SeekOrigin.Begin);
                Ident = Encoding.ASCII.GetString(r.ReadBytes(4));
                if (Ident != "Gd3 ")
                {
                    throw new InvalidDataException("GD3 header not found");
                }

                var version = r.ReadUInt32();
                // BCD to integer
                int scaled = 0;
                int factor = 1;
                for (int i = 0; i < 8; ++i)
                {
                    var digit = (int) version & 0xf;
                    scaled += digit * factor;
                    version >>= 4;
                    factor *= 10;
                }

                Version = (decimal) scaled / 100;

                if (Version >= 2.00m)
                {
                    throw new Exception($"GD3 version {Version} not supported");
                }

                var length = r.ReadUInt32();
                if (s.Length - s.Position > length)
                {
                    throw new Exception("File not big enough for GD3 data");
                }

                // We read out 11 UCS-2 strings
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

            // We then put them into our properties
            Title = new MultiLanguageTag {English = tags[0], Japanese = tags[1]};
            Game = new MultiLanguageTag {English = tags[2], Japanese = tags[3]};
            System = new MultiLanguageTag {English = tags[4], Japanese = tags[5]};
            Composer = new MultiLanguageTag {English = tags[6], Japanese = tags[7]};
            Date = tags[8];
            Ripper = tags[9];
            Notes = tags[10];
        }

        public override string ToString()
        {
            var title = Title.ToString();
            var game = Game.ToString();
            var system = System.English;
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