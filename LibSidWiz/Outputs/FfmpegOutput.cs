using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using SkiaSharp;

namespace LibSidWiz.Outputs
{
    public class FfmpegOutput : IGraphicsOutput
    {
        private readonly bool _throwStandardError;
        private readonly Process _process;
        private readonly BinaryWriter _writer;

        public FfmpegOutput(string pathToExe, string filename, int width, int height, int fps, string extraArgs, string masterAudioFilename, string videoCodec, string audioCodec, bool throwStandardError)
        {
            _throwStandardError = throwStandardError;
            // Build the FFMPEG commandline
            var arguments = "-y -hide_banner"; // Overwrite, don't show banner at startup

            // Audio input
            if (File.Exists(masterAudioFilename))
            {
                arguments += $" -i \"{masterAudioFilename}\"";
            }

            // Video input
            arguments += $" -f rawvideo -pixel_format bgr0 -video_size {width}x{height} -framerate {fps} -i pipe:";
            
            // Audio output
            arguments += $" -codec:a {audioCodec}";

            // Video output
            arguments += $" -codec:v {videoCodec} -movflags +faststart";

            // Extra args
            arguments += $" {extraArgs} \"{filename}\"";

            Console.WriteLine($"Starting FFMPEG: {pathToExe} {arguments}");

            // We don't want a BOM to be injected if the system code page is set to UTF-8.
            // This fails sometimes, so we swallow the error...
            try
            {
                Console.InputEncoding = Encoding.ASCII;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to change console encoding to ASCII. You may get video corruption. Exception said: {e.Message}");
            }

            // Start it up
            _process = Process.Start(
                new ProcessStartInfo
                {
                    FileName = pathToExe,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardError = throwStandardError,
                    RedirectStandardOutput = false,
                    CreateNoWindow = throwStandardError // makes it inline in console mode
                }
            );

            if (_process == null)
            {
                throw new Exception($"Couldn't start FFMPEG with commandline {pathToExe} {arguments}");
            }

            _writer = new BinaryWriter(_process.StandardInput.BaseStream);
        }

        public void Write(SKImage _, byte[] data, double __, TimeSpan ___)
        {
            try
            {
                _writer.Write(data);
            }
            catch (Exception e)
            {
                if (_throwStandardError)
                {
                    if (_process.HasExited)
                    {
                        throw new Exception(
                            $"""
                             FFMPEG has exited unexpectedly.
                             Exit code was {_process.ExitCode}.
                             Standard error was:
                             {_process.StandardError.ReadToEnd()}
                             """, e);
                    }

                    throw new Exception(
                        $"""
                         Cannot write to FFMPEG.
                         Standard error was:
                         {_process.StandardError.ReadToEnd()}
                         """, e);
                }

                throw;
            }
        }

        public void Dispose()
        {
            // This triggers a shutdown
            _process?.StandardInput.BaseStream.Close();
            _process?.StandardError.Close();
            // And we wait for it to finish...
            _process?.WaitForExit();
            _process?.Dispose();
            _writer?.Dispose();
        }
    }
}