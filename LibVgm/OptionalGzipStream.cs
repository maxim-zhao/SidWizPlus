using System.IO;
using System.IO.Compression;

namespace LibVgm
{
    /// <summary>
    /// Stream class which transparently supports GZipped or uncompressed files
    /// </summary>
    internal class OptionalGzipStream : Stream
    {
        private readonly Stream _stream;

        public OptionalGzipStream(string filename)
        {
            var fileStream = new FileStream(filename, FileMode.Open);
            // Check if it's GZipped
            bool needGzip = fileStream.ReadByte() == 0x1f && fileStream.ReadByte() == 0x8b;
            fileStream.Seek(0, SeekOrigin.Begin);
            if (needGzip)
            {
                using (var gZipStream = new GZipStream(fileStream, CompressionMode.Decompress))
                {
                    _stream = new MemoryStream();
                    gZipStream.CopyTo(_stream);
                    _stream.Seek(0, SeekOrigin.Begin);
                }
            }
            else
            {
                _stream = fileStream;
            }
        }

        public override void Flush() => _stream.Flush();
        public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);
        public override void SetLength(long value) => _stream.SetLength(value);
        public override int Read(byte[] buffer, int offset, int count) => _stream.Read(buffer, offset, count);
        public override void Write(byte[] buffer, int offset, int count) => _stream.Write(buffer, offset, count);

        public override bool CanRead => _stream.CanRead;
        public override bool CanSeek => _stream.CanSeek;
        public override bool CanWrite => _stream.CanWrite;
        public override long Length => _stream.Length;
        public override long Position
        {
            get => _stream.Position;
            set => _stream.Position = value;
        }

        protected override void Dispose(bool disposing)
        {
            _stream?.Dispose();
            base.Dispose(disposing);
        }
    }
}
