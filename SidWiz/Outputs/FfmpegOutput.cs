using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace SidWiz.Outputs
{
    internal class FfmpegOutput : IGraphicsOutput
    {
        private readonly Process _process;
        private readonly BinaryWriter _writer;

        public FfmpegOutput(string pathToExe, string filename, int width, int height, int fps, string extraArgs, ICollection<string> waveFilenames)
        {
            // Build the FFMPEG commandline
            var arguments = "-y"; // Overwrite

            // Audio part
            if (waveFilenames.Count > 1)
            {
                arguments += $" -filter_complex amix=inputs={waveFilenames.Count}";
                foreach (var fn in waveFilenames)
                {
                    arguments += $" -i \"{fn}\"";
                }
                arguments += " -acodec aac";
            }

            // Video part
            arguments += $" -f rawvideo -pixel_format bgr24 -video_size {width}x{height} -framerate {fps} -i pipe:";

            // Extra args
            arguments += $" {extraArgs} \"{filename}\"";

            // Start it up
            _process = Process.Start(new ProcessStartInfo
            {
                FileName = pathToExe, 
                Arguments = arguments, 
                UseShellExecute = false, 
                RedirectStandardInput = true
            });
            if (_process == null)
            {
                throw new Exception($"Couldn't start FFMPEG with commandline {pathToExe} {arguments}");
            }
            _writer = new BinaryWriter(_process.StandardInput.BaseStream);
        }

        public void Write(byte[] data, Image image, double fractionComplete)
        {
            _writer.Write(data);
        }

        public void Dispose()
        {
            _process?.Dispose();
            _writer?.Dispose();
        }
    }
}