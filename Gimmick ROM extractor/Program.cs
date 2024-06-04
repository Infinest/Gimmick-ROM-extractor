using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Gimmick_ROM_extractor
{
    internal class RomConfiguration
    {
        public List<List<byte>> INES_HEADERS { get; set; }
        public string TARGET_FILE { get; set; } = "AR_win32.mdf";
        public string AES_KEY { get; set; }
        public uint[] ROM_SIZES { get; set; }
        public string[] OUTPUT_NAMES { get; set; }
        public bool RESET_OFFSET_AFTER_SEARCH { get; set; }
    }

    internal class Program
    {
        private const int GENERIC_PROCESSING_ERROR = 13804;
        private const int SUCCESS = 0;

        [STAThreadAttribute]
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += OnResolveAssembly;
            RomConfiguration config = loadConfiguration();
            if (config == null)
            {
                Environment.Exit(GENERIC_PROCESSING_ERROR);
            }

            bool hasConfirmed = false;
            do
            {
                Console.WriteLine(String.Format("Extract ROM/s from {0}? [y/n]", config.TARGET_FILE));
                ConsoleKeyInfo input = Console.ReadKey();
                switch (input.Key)
                {
                    case ConsoleKey.Y:
                        hasConfirmed = true;
                        break;
                    case ConsoleKey.N:
                        Environment.Exit(SUCCESS);
                        break;
                }
            } while (!hasConfirmed);
            Console.WriteLine();

            byte[] unpaddedKey = Encoding.ASCII.GetBytes(config.AES_KEY);
            byte[] key = new byte[16];
            unpaddedKey.CopyTo(key, 0);
            byte[] iv = new byte[16];

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;

                try
                {
                    using (FileStream mdf = File.Open(Path.Combine(Directory.GetCurrentDirectory(), config.TARGET_FILE), FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        IntPtr romOffset = IntPtr.Zero;
                        for (int i = 0; i < config.INES_HEADERS.Count; i++)
                        {
                            byte[] outputBuffer = new byte[config.INES_HEADERS[i].Count];

                            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                            /*
                             * To dynamically find where inside the config.TARGET_FILE the ROM is stored, we encrypt the INES Header with the encryption
                             * the game employs itself. Then we search for the encrypted file inside of config.TARGET_FILE
                             */
                            encryptor.TransformBlock(config.INES_HEADERS[i].ToArray(), 0, config.INES_HEADERS[i].Count, outputBuffer, 0);
                            Console.WriteLine(String.Format("Searching for encrypted ROM in {0}...", config.TARGET_FILE));
                            romOffset = findOffset(outputBuffer, mdf, config.RESET_OFFSET_AFTER_SEARCH || romOffset == IntPtr.Zero ? IntPtr.Zero : romOffset + config.INES_HEADERS[i - 1].Count);

                            if (romOffset == IntPtr.Zero)
                            {
                                Console.WriteLine(String.Format("Could not find encrypted ROM inside {0}", config.TARGET_FILE));
                                Environment.Exit(GENERIC_PROCESSING_ERROR);
                            }

                            mdf.Seek((long)romOffset, SeekOrigin.Begin);
                            byte[] buffer = new byte[config.ROM_SIZES[i]];
                            outputBuffer = new byte[config.ROM_SIZES[i]];
                            mdf.Read(buffer, 0, buffer.Length);
                            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, iv);
                            Console.WriteLine("Decrypting data...");
                            decryptor.TransformBlock(buffer, 0, (int)config.ROM_SIZES[i], outputBuffer, 0);

                            string outputPath = Path.Combine(Directory.GetCurrentDirectory(), config.OUTPUT_NAMES[i]);
                            using (FileStream fs = File.Open(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                fs.Write(outputBuffer, 0, outputBuffer.Length);
                                Console.WriteLine(String.Format("Wrote ROM to {0}", outputPath));
                            }
                        }
                    }
                    Environment.Exit(SUCCESS);
                }
                catch (System.IO.FileNotFoundException e)
                {
                    Console.WriteLine(String.Format("Could not find {0} file. Please ensure this application and {0} are located in the same directory.", config.TARGET_FILE));
                    Environment.Exit(GENERIC_PROCESSING_ERROR);
                }
            }
        }

        /// <summary>
        /// Searches for an array of bytes in the specified FileStream.
        /// </summary>
        /// <param name="searchBytes"></param>
        /// <param name="fs"></param>
        /// <returns>
        /// The offset at which the beginning of the byte array was found.
        /// If the array wasn't found, returns IntPtr.Zero instead.
        /// </returns>
        private static IntPtr findOffset(byte[] searchBytes, FileStream fs, IntPtr offset)
        {
            byte[] buffer = new byte[1024];
            int comparatorCounter = 0;
            fs.Seek((long)offset, SeekOrigin.Begin);
            while (fs.Position < fs.Length)
            {
                fs.Read(buffer, 0, buffer.Length);
                for (int i = 0; i < buffer.Length; i++)
                {
                    if (buffer[i] == searchBytes[comparatorCounter])
                    {
                        comparatorCounter++;

                        if (comparatorCounter == searchBytes.Length)
                        {
                            return (IntPtr)fs.Position - comparatorCounter - buffer.Length + i + 1;
                        }
                    }
                    else
                    {
                        comparatorCounter = 0;
                    }
                }
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// Loads values from config.json into an instance of RomConfiguration.
        /// </summary>
        /// <returns>
        /// Returns the newly created instance of RomConfiguration.
        /// </returns>
        private static RomConfiguration loadConfiguration()
        {
            string executionPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string configFile = Path.Combine(Directory.GetCurrentDirectory(), "config.json");
            try
            {
                using (FileStream fs = File.Open(configFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    JsonSerializerOptions options = new JsonSerializerOptions();
                    options.Converters.Add(new UnsignedIntConverter());
                    options.Converters.Add(new ByteConverter());
                    options.AllowTrailingCommas = true;

                    RomConfiguration config = JsonSerializer.Deserialize<RomConfiguration>(fs, options);

                    int count = config.INES_HEADERS.Count;
                    if (config.ROM_SIZES.Length != count || config.OUTPUT_NAMES.Length != count)
                    {
                        Console.WriteLine("config.json is invalid. Make sure the amount of entries");
                    }

                    return config;
                }
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine("Could not find config.json file. Please ensure this application and config.json are located in the same directory.");
                return null;
            }
        }

        /// <summary>
        /// Hooks to assembly resolver and tries to load assembly (.dll)
        /// from executable resources it CLR can't find it locally.
        ///
        /// Used for embedding assemblies onto executables.
        ///
        /// See: http://www.digitallycreated.net/Blog/61/combining-multiple-assemblies-into-a-single-exe-for-a-wpf-application
        /// </summary>
        private static Assembly OnResolveAssembly(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);
            var path = assemblyName.Name + ".dll";
            var executingAssembly = Assembly.GetExecutingAssembly();
            if (!assemblyName.CultureInfo.Equals(CultureInfo.InvariantCulture))
            {
                path = $"{assemblyName.CultureInfo}\\${path}";
            }

            using (var stream = executingAssembly.GetManifestResourceStream(path))
            {
                if (stream == null)
                {
                    return null;
                }

                var assemblyRawBytes = new byte[stream.Length];
                stream.Read(assemblyRawBytes, 0, assemblyRawBytes.Length);
                return Assembly.Load(assemblyRawBytes);
            }
        }
    }
}
