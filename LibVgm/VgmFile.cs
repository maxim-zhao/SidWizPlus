using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibVgm
{
    public class VgmFile: IDisposable
    {
        // It's painful to seek in GZipped streams, so we don't bother...
        private readonly MemoryStream _stream;

        // ReSharper disable MemberCanBePrivate.Global
        // ReSharper disable UnusedAutoPropertyAccessor.Global
        // ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
        // ReSharper disable MemberCanBeProtected.Global
        public VgmHeader Header { get; }
        public Gd3Tag Gd3Tag { get; }

        public class VgmHeader
        {
            public string Ident { get; set; }
            public uint EndOfFileOffset { get; set; }
            public decimal Version { get; set; }
            public uint Sn76489Clock { get; set; }
            public uint Ym2413Clock { get; set; }
            public uint Gd3Offset { get; set; }
            public uint TotalSamples { get; set; }
            public uint LoopOffset { get; set; }
            public uint LoopSamples { get; set; }
            public uint Rate { get; set; }
            public uint SnFeedback { get; set; }
            public uint SnWidth { get; set; }
            public uint SnFlag { get; set; }
            public uint Ym2612Clock { get; set; }
            public uint Ym2151Clock { get; set; }
            public uint DataOffset { get; set; } = 0x40;
            public uint SegaPcmClock { get; set; }
            public uint SpcmInterface { get; set; }
            public uint Rf5C68Clock { get; set; }
            public uint Ym2203Clock { get; set; }
            public uint Ym2608Clock { get; set; }
            public uint Ym2610BClock { get; set; }
            public uint Ym3812Clock { get; set; }
            public uint Ym3526Clock { get; set; }
            public uint Y8950Clock { get; set; }
            public uint Ymf262Clock { get; set; }
            public uint Ymf278BClock { get; set; }
            public uint Ymf271Clock { get; set; }
            public uint Ymz280BClock { get; set; }
            public uint Rf5C164Clock { get; set; }
            public uint PwmClock { get; set; }
            public uint Ay8910Clock { get; set; }
            public uint AyType { get; set; }
            public uint AyFlags { get; set; }
            public uint VolumeModifier { get; set; }
            public uint LoopBase { get; set; }
            public uint LoopModifier { get; set; }
            public uint GbDmgClock { get; set; }
            public uint NesApuClock { get; set; }
            public uint MultiPcmClock { get; set; }
            public uint Upd7759Clock { get; set; }
            public uint Okim6258Clock { get; set; }
            public uint Okim6258Flags { get; set; }
            public uint K054539Flags { get; set; }
            public uint C140ChipType { get; set; }
            public uint Okim6295Clock { get; set; }
            public uint K051649Clock { get; set; }
            public uint K054539Clock { get; set; }
            public uint HuC6280Clock { get; set; }
            public uint C140Clock { get; set; }
            public uint K053260Clock { get; set; }
            public uint PokeyClock { get; set; }
            public uint QSoundClock { get; set; }
            public uint ScspClock { get; set; }
            public uint ExtraHeaderOffset { get; set; }
            public uint WonderSwanClock { get; set; }
            public uint VsuClock { get; set; }
            public uint Saa1099Clock { get; set; }
            public uint Es5503Clock { get; set; }
            public uint Es5506Clock { get; set; }
            public uint Es5503Channels { get; set; }
            public uint Es5506Channels { get; set; }
            public uint X1010Clock { get; set; }
            public uint C352Clock { get; set; }
            public uint Ga20Clock { get; set; }

            internal VgmHeader()
            {
                Ident = "Vgm ";
                Version = 1.10m;
            }

            internal void Parse(Stream s)
            {
                using var r = new BinaryReader(s, Encoding.ASCII, true);
                
                // VGM 1.00
                Ident = string.Concat(r.ReadChars(4));
                EndOfFileOffset = r.ReadUInt32();
                if (EndOfFileOffset == 0)
                {
                    EndOfFileOffset = (uint) s.Length;
                }
                else
                {
                    EndOfFileOffset += 4; // Make absolute
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
                Sn76489Clock = r.ReadUInt32();
                Ym2413Clock = r.ReadUInt32();
                Gd3Offset = r.ReadUInt32();
                if (Gd3Offset != 0)
                {
                    Gd3Offset += 0x14; // Make absolute
                }
                TotalSamples = r.ReadUInt32();
                LoopOffset = r.ReadUInt32();
                if (LoopOffset > 0)
                {
                    LoopOffset += 0x1c; // Make absolute
                }
                LoopSamples = r.ReadUInt32();
                if (Version > 1.01m)
                {
                    Rate = r.ReadUInt32();
                }

                // VGM 1.10
                if (Version > 1.10m)
                {
                    SnFeedback = r.ReadUInt16();
                    SnWidth = r.ReadByte();
                    if (Version > 1.51m)
                    {
                        SnFlag = r.ReadByte();
                    }
                    else
                    {
                        r.ReadByte();
                    }

                    Ym2612Clock = r.ReadUInt32();
                    Ym2151Clock = r.ReadUInt32();

                    if (Version > 1.50m)
                    {
                        DataOffset = r.ReadUInt32();
                        if (DataOffset == 0)
                        {
                            DataOffset = 0x40; // Assume default
                        }
                        else
                        {
                            DataOffset += 0x34; // Make absolute
                        }

                        if (Version > 1.51m)
                        {
                            SegaPcmClock = r.ReadUInt32();
                            SpcmInterface = r.ReadUInt32();
                            Rf5C68Clock = r.ReadUInt32();
                            Ym2203Clock = r.ReadUInt32();
                            Ym2608Clock = r.ReadUInt32();
                            Ym2610BClock = r.ReadUInt32();
                            Ym3812Clock = r.ReadUInt32();
                            Ym3526Clock = r.ReadUInt32();
                            Y8950Clock = r.ReadUInt32();
                            Ymf262Clock = r.ReadUInt32();
                            Ymf278BClock = r.ReadUInt32();
                            Ymf271Clock = r.ReadUInt32();
                            Ymz280BClock = r.ReadUInt32();
                            Rf5C164Clock = r.ReadUInt32();
                            PwmClock = r.ReadUInt32();
                            Ay8910Clock = r.ReadUInt32();
                            var n = r.ReadUInt32();
                            AyType = n >> 24;
                            AyFlags = n & 0xffffff;

                            if (Version > 1.60m)
                            {
                                VolumeModifier = r.ReadByte();
                                r.ReadByte();
                                LoopBase = r.ReadByte();
                            }
                            else
                            {
                                r.ReadBytes(3);
                            }

                            LoopModifier = r.ReadByte();
                            if (Version > 1.61m)
                            {
                                GbDmgClock = r.ReadUInt32();
                                NesApuClock = r.ReadUInt32();
                                MultiPcmClock = r.ReadUInt32();
                                Upd7759Clock = r.ReadUInt32();
                                Okim6258Clock = r.ReadUInt32();
                                Okim6258Flags = r.ReadByte();
                                K054539Flags = r.ReadByte();
                                C140ChipType = r.ReadByte();
                                r.ReadByte();
                                Okim6295Clock = r.ReadUInt32();
                                K051649Clock = r.ReadUInt32();
                                K054539Clock = r.ReadUInt32();
                                HuC6280Clock = r.ReadUInt32();
                                C140Clock = r.ReadUInt32();
                                K053260Clock = r.ReadUInt32();
                                PokeyClock = r.ReadUInt32();
                                QSoundClock = r.ReadUInt32();
                                if (Version > 1.70m)
                                {
                                    if (Version > 1.71m)
                                    {
                                        ScspClock = r.ReadUInt32();
                                    }
                                    else
                                    {
                                        r.ReadUInt32();
                                    }

                                    ExtraHeaderOffset = (uint) r.BaseStream.Position + r.ReadUInt32();

                                    if (Version > 1.71m)
                                    {
                                        WonderSwanClock = r.ReadUInt32();
                                        VsuClock = r.ReadUInt32();
                                        Saa1099Clock = r.ReadUInt32();
                                        Es5503Clock = r.ReadUInt32();
                                        Es5506Clock = r.ReadUInt32();
                                        Es5503Channels = r.ReadUInt16();
                                        Es5506Channels = r.ReadByte();
                                        r.ReadByte();
                                        X1010Clock = r.ReadUInt32();
                                        C352Clock = r.ReadUInt32();
                                        Ga20Clock = r.ReadUInt32();
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public interface ICommand
        {
        }

        public class GenericCommand: ICommand
        {
            public byte[] Values { get; set; }

            public GenericCommand(BinaryReader reader, int count)
            {
                Values = reader.ReadBytes(count);
            }

            public override string ToString() => $"Generic command {Values.Length} bytes";
        }

        public class AddressDataCommand: ICommand
        {
            public byte Type { get; }
            public byte Address { get; set; }
            public byte Data { get; set; }

            public AddressDataCommand(BinaryReader reader, byte type)
            {
                Type = type;
                Address = reader.ReadByte();
                Data = reader.ReadByte();
            }

            public override string ToString() => $"Type {Type:X} Address {Address:X} Data {Data:X}";
        }

        public class WaitCommand : ICommand
        {
            public int Ticks { get; set; }

            public WaitCommand(BinaryReader reader, byte type)
            {
                switch (type)
                {
                    case 0x61:
                        Ticks = reader.ReadUInt16();
                        break;
                    case 0x62: 
                        Ticks = 735; 
                        break;
                    case 0x63: 
                        Ticks = 882; 
                        break;
                    case 0x70: case 0x71: case 0x72: case 0x73: case 0x74: case 0x75: case 0x76: case 0x77: 
                    case 0x78: case 0x79: case 0x7a: case 0x7b: case 0x7c: case 0x7d: case 0x7e: case 0x7f:
                        Ticks = (type & 0xf) + 1;
                        break;
                }
            }

            public override string ToString() => $"Wait {Ticks} samples";
        }

        public class SampleWaitCommand : WaitCommand
        {
            public SampleWaitCommand(BinaryReader reader, byte type) : base(reader, type)
            {
                // Sample waits are one less
                --Ticks;
            }
            public override string ToString() => $"Wait {Ticks} samples and play sample";
        }

        public class StopCommand : ICommand
        {
            public override string ToString() => "Stop";
        }

        public class DataBlock : ICommand
        {
            public byte BlockType { get; set; }
            public byte[] Data { get; set; }

            public DataBlock(BinaryReader reader)
            {
                reader.ReadByte(); // Skip the stop command
                BlockType = reader.ReadByte();
                var count = reader.ReadInt32();
                Data = reader.ReadBytes(count);
            }
            public override string ToString() => $"Data block: type {BlockType:X} size {Data}";
        }

        public class PcmRamWrite : ICommand
        {
            public byte ChipType { get; set; }
            public int ReadOffset { get; set; }
            public int Count { get; set; }
            public int WriteOffset { get; set; }

            public PcmRamWrite(BinaryReader reader)
            {
                reader.ReadByte(); // Skip the stop command
                ChipType = reader.ReadByte();
                ReadOffset = reader.ReadUInt16() + reader.ReadByte() << 16; // 24-bit read
                WriteOffset = reader.ReadUInt16() + reader.ReadByte() << 16; // 24-bit read
                Count = reader.ReadUInt16() + reader.ReadByte() << 16; // 24-bit read
                if (Count == 0)
                {
                    Count = 1 << 24;
                }
            }
        }

        public abstract class DacStreamCommand : ICommand
        {
            public byte StreamId { get; set; }

            protected DacStreamCommand(BinaryReader reader)
            {
                StreamId = reader.ReadByte();
            }
        }

        public class DacStreamSetupCommand : DacStreamCommand
        {
            public DacStreamSetupCommand(BinaryReader reader) : base(reader)
            {
                ChipType = reader.ReadByte();
                Port = reader.ReadByte();
                Command = reader.ReadByte();
            }

            public byte Command { get; set; }

            public byte Port { get; set; }

            public byte ChipType { get; set; }
        }

        public class DacStreamDataCommand: DacStreamCommand
        {
            public DacStreamDataCommand(BinaryReader reader) : base(reader)
            {
                BankId = reader.ReadByte();
                StepSize = reader.ReadByte();
                StepBase = reader.ReadByte();
            }

            public byte StepBase { get; set; }

            public byte StepSize { get; set; }

            public byte BankId { get; set; }
        }

        public class DacStreamFrequencyCommand: DacStreamCommand
        {
            public DacStreamFrequencyCommand(BinaryReader reader) : base(reader)
            {
                Frequency = reader.ReadUInt32();
            }

            public uint Frequency { get; set; }
        }

        public class DacStreamStartCommand: DacStreamCommand
        {
            public DacStreamStartCommand(BinaryReader reader) : base(reader)
            {
                Offset = reader.ReadUInt32();
                LengthMode = reader.ReadByte();
                Count = reader.ReadUInt32();
            }

            public uint Count { get; set; }

            public byte LengthMode { get; set; }

            public uint Offset { get; set; }
        }

        public class DacStreamStopCommand : DacStreamCommand
        {
            public DacStreamStopCommand(BinaryReader reader) : base(reader) {}
        }
        public class DacStreamFastStartCommand : DacStreamCommand
        {
            public DacStreamFastStartCommand(BinaryReader reader) : base(reader)
            {
                BlockId = reader.ReadUInt16();
                Flags = reader.ReadByte();
            }

            public byte Flags { get; set; }

            public ushort BlockId { get; set; }
        }
        // ReSharper restore MemberCanBeProtected.Global
        // ReSharper restore UnusedAutoPropertyAccessor.Global
        // ReSharper restore MemberCanBePrivate.Global
        // ReSharper restore AutoPropertyCanBeMadeGetOnly.Global

        // ReSharper disable once UnusedMember.Global
        public VgmFile()
        {
            // Empty file
            _stream = new MemoryStream();
            Header = new VgmHeader();
            Gd3Tag = new Gd3Tag();
        }

        public VgmFile(string filename)
        {
            _stream = new MemoryStream();
            Header = new VgmHeader();
            Gd3Tag = new Gd3Tag();

            LoadFromFile(filename);
        }

        private void LoadFromFile(string filename)
        {
            // We copy all the data into a memory stream to allow seeking
            using (var s = new OptionalGzipStream(filename))
            {
                s.CopyTo(_stream);
                _stream.Seek(0, SeekOrigin.Begin);
            }

            // We parse the header
            Header.Parse(_stream);

            // And the GD3 tag, if present
            if (Header.Gd3Offset != 0)
            {
                Gd3Tag.Parse(_stream, Header.Gd3Offset);
            }

        }

        public IEnumerable<ICommand> Commands()
        {
            // Seek to the start
            _stream.Seek(Header.DataOffset, SeekOrigin.Begin);

            using var reader = new BinaryReader(_stream, Encoding.Default, true);
            while (_stream.Position < _stream.Length)
            {
                //var b = reader.ReadByte();

                switch (reader.ReadByte())
                {
                    case <= 0x2f:
                        // Unhandled
                        continue;
                    case >= 0x30 and <= 0x3f:
                        yield return new GenericCommand(reader, 1); // Reserved range
                        break;
                    case >= 0x40 and <= 0x4e:
                        yield return new GenericCommand(reader, 2); // Reserved range
                        break;
                    case >= 0x4f and <= 0x50:
                        yield return new GenericCommand(reader, 1); // GG stereo or PSG
                        break;
                    case var b and >= 0x51 and <= 0x5f:
                        yield return new AddressDataCommand(reader, b); // FM chips
                        break;
                    case 0x60:
                        // Unhandled
                        continue;
                    case var b and >= 0x61 and <= 0x63:
                        yield return new WaitCommand(reader, b);
                        break;
                    case <= 0x65 and >= 0x64:
                        // Unhandled
                        continue;
                    case 0x66:
                        yield return new StopCommand();
                        yield break;
                    case 0x67:
                        yield return new DataBlock(reader);
                        break;
                    case 0x68:
                        yield return new PcmRamWrite(reader);
                        break;
                    case < 0x6f and >= 0x69:
                        // Unhandled
                        continue;
                    case var b and >= 0x70 and <= 0x7f:
                        yield return new WaitCommand(reader, b);
                        break;
                    case var b and >= 0x80 and <= 0x8f:
                        yield return new SampleWaitCommand(reader, b);
                        break;
                    case 0x90:
                        yield return new DacStreamSetupCommand(reader);
                        break;
                    case 0x91:
                        yield return new DacStreamDataCommand(reader);
                        break;
                    case 0x92:
                        yield return new DacStreamFrequencyCommand(reader);
                        break;
                    case 0x93:
                        yield return new DacStreamStartCommand(reader);
                        break;
                    case 0x94:
                        yield return new DacStreamStopCommand(reader);
                        break;
                    case 0x95:
                        yield return new DacStreamFastStartCommand(reader);
                        break;
                    case <= 0x9f and >= 0x96:
                        // Unhandled
                        continue;
                    case var b and 0xa0:
                        yield return new AddressDataCommand(reader, b);
                        break;
                    case <= 0xaf and >= 0xa1:
                        yield return new GenericCommand(reader, 2); // Reserved range
                        break;
                    case var b and >= 0xb0 and <= 0xbf:
                        yield return new AddressDataCommand(reader, b);
                        break;
                    case <= 0xdf and >= 0xc0:
                        yield return new GenericCommand(reader, 3); // Reserved + some allocated
                        break;
                    case <= 0xff and >= 0xe0:
                        yield return new GenericCommand(reader, 4); // Reserved + some allocated
                        break;
                }
            }
        }
        public void Dispose()
        {
            _stream?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}