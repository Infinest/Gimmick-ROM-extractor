using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Gimmick_ROM_extractor
{
    internal class Program
    {
        private const int GENERIC_PROCESSING_ERROR = 13804;
        private const int SUCCESS = 0;
        private const uint ROM_SIZE = 0x60010;
        private const string AES_KEY = "E543JJD8439FF";
        private static readonly byte[] INES_HEADER_BYTES = { 0x4E, 0x45, 0x53, 0x1A, 0x10, 0x10, 0x50, 0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        static void Main(string[] args)
        {
            bool hasConfirmed = false;
            do
            {
                Console.Clear();
                Console.WriteLine("Extract ROM from AR_win32.mdf? [y/n]");
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

            byte[] unpaddedKey = Encoding.ASCII.GetBytes(AES_KEY);
            byte[] key = new byte[16];
            unpaddedKey.CopyTo(key, 0);
            byte[] iv = new byte[16];

            while (true)
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.None;

                    byte[] outputBuffer = new byte[INES_HEADER_BYTES.Length];

                    try
                    {
                        using (FileStream fs = File.Open(Path.Combine(Directory.GetCurrentDirectory(), "AR_win32.mdf"), FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                            /*
                             * To dynamically find where inside the AR_win32.mdf the ROM is stored, we encrypt the INES Header for GIMMICK! with the encryption
                             * the games employs itself. Then we search for they encrypted file inside of AR_win32.mdf
                             */
                            encryptor.TransformBlock(INES_HEADER_BYTES, 0, INES_HEADER_BYTES.Length, outputBuffer, 0);
                            Console.WriteLine("Searching for encrypted ROM in AR_win32.mdf...");
                            IntPtr romOffset = findOffset(outputBuffer, fs);

                            if (romOffset == IntPtr.Zero)
                            {
                                Console.WriteLine("Could not find encrypted ROM inside AR_win32.mdf");
                                Environment.Exit(GENERIC_PROCESSING_ERROR);
                            }

                            fs.Seek((long)romOffset, SeekOrigin.Begin);
                            byte[] buffer = new byte[ROM_SIZE];
                            outputBuffer = new byte[ROM_SIZE];
                            fs.Read(buffer, 0, buffer.Length);
                            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, iv);
                            Console.WriteLine("Decrypting data...");
                            decryptor.TransformBlock(buffer, 0, 0x60010, outputBuffer, 0);
                        }
                    }
                    catch (System.IO.FileNotFoundException e)
                    {
                        Console.WriteLine("Could not find AR_win32.mdf file. Please ensure this application and AR_win32.mdf are located in the same directory.");
                        Environment.Exit(GENERIC_PROCESSING_ERROR);
                    }

                    string outputPath = Path.Combine(Directory.GetCurrentDirectory(), "RealGimmick.nes");
                    using (FileStream fs = File.Open(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        fs.Write(outputBuffer, 0, outputBuffer.Length);
                        Console.WriteLine(String.Format("Wrote ROM to {0}", outputPath));
                        Environment.Exit(SUCCESS);
                    }
                }
            }
        }

        private static IntPtr findOffset(byte[] searchBytes, FileStream fs)
        {
            byte[] buffer = new byte[1024];
            int comparatorCounter = 0;

            fs.Seek(0, SeekOrigin.Begin);
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
    }
}
