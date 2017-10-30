using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpenRealSense.NativeMethods;

namespace OpenRealSense.Cmd {
    public class Program {
        public static void Main(string[] args) {
            Console.WriteLine("Start");
            var platform = IntPtr.Size == 8 ? "x64" : "x32";
            Console.WriteLine($"Platform: {platform}");

            var context = Context.Create(11201);
            Console.WriteLine("Context created");

            var num = context.GetDeviceCount();
            Console.WriteLine("Devices: " + num);

            if (num == 0) {
                Console.WriteLine("Camera not detected :(");
                Console.ReadKey();
                return;
            }

            var device = context.GetDevice(0);
            Console.WriteLine("Name: " + device.GetDeviceName());

            var height = 480;
            var width = 640;
            var oneMeter = device.GetOneMeterValeForDepth();

            Console.Clear();
            Console.BufferHeight = Console.WindowHeight = (height / 20 + 1) * 2 + 2;
            Console.BufferWidth = Console.WindowWidth = width / 10 + 2;

            device.EnableStream(StreamType.Infrared, width, height, FormatType.y16, 30);
            device.EnableStream(StreamType.Depth, width, height, FormatType.Z16, 30);
            device.StartInBackground(() => {
                Console.CursorVisible = false;
                Console.CursorLeft = 0;
                Console.CursorTop = 0;

                Console.WriteLine("IR:");

                var frameInBytes = device.GetFrameData(StreamType.Infrared).Bytes;
                using (var stream = new MemoryStream(frameInBytes)) {
                    using (var reader = new BinaryReader(stream))
                    {
                        Render(height, width, oneMeter, reader);
                    }
                }

                Console.CursorLeft = 0;
                Console.WriteLine("Depth:");


                frameInBytes = device.GetFrameData(StreamType.Depth).Bytes;
                using (var stream = new MemoryStream(frameInBytes))
                {
                    using (var reader = new BinaryReader(stream))
                    {
                        Render(height, width, oneMeter, reader);
                    }
                }
            });
            Console.ReadLine();
            Console.CursorVisible = true;
            device.Stop();
        }

        private static void Render(int height, int width, float oneMeter, BinaryReader reader)
        {
            var buffer = new char[(width / 10 + 1) * (height / 20 + 1)];
            var bufferIndex = 0;
            var coverage = new int[64];
            for (var y = 0; y < height; ++y)
            {
                for (var x = 0; x < width; ++x)
                {
                    var depth = reader.ReadUInt16();
                    if (depth > 0 && depth < oneMeter)
                    {
                        ++coverage[x / 10];
                    }
                }
                if (y % 20 == 19)
                {
                    for (var i = 0; i < coverage.Length; i++)
                    {
                        buffer[bufferIndex++] = " .:nhBXWW"[coverage[i] / 25];
                        coverage[i] = 0;
                    }
                    buffer[bufferIndex++] = '\n';
                }
            }
            buffer[bufferIndex] = ' ';
            Console.Write(buffer);
        }
    }
}