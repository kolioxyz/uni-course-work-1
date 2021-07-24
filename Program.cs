/*
 * Курсова работа по ИНФ2 ТУ-София 2021
 * Никола Йорданов / ФПМИ / ПМИ / 1-ви курс / 181220005
 * 
 * Имплементация на алгоритъм за копресия на данни по подобие на спецификацията: RFC-1951
 * https://www.ietf.org/rfc/rfc1951.txt
 * 
 * Abstract
 * 
 * This specification defines a lossless compressed data format.  The
 * data can be produced or consumed, even for an arbitrarily long
 * sequentially presented input data stream, using only an a priori
 * bounded amount of intermediate storage.  The format presently uses
 * the DEFLATE compression method but can be easily extended to use
 * other compression methods.  It can be implemented readily in a manner
 * not covered by patents.  This specification also defines the ADLER-32
 * checksum (an extension and improvement of the Fletcher checksum),
 * used for detection of data corruption, and provides an algorithm for
 * computing it.
 */

// FILE: Program.cs
// contains:
// Main()
// Compress7bitLZ77(string filepath)
// Decompress7bitLZ77(string filepath)
// struct Pointer7bitLZ77

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace info2kursova
{
    class MainClass
    {
        public static string HELP = "USAGE: [options] [file]\noptions:\n-e\tencode\n-d\tdecode\n-h\tdisplay this help message";

        public static void Main(string[] args)
        {
            string filepath = "";
            string option = "";

            if (args.Length == 0)
            {
                Console.WriteLine(HELP);
                Console.WriteLine("Please enter an option");
                option = Console.ReadLine();
                Console.WriteLine("Please enter file path");
                filepath = Console.ReadLine();
            }
            // else

            switch (option)
            {
                case "-h":
                    Console.WriteLine(HELP);
                    break;
                case "-e":
                    int exitCode = Compress7bitLZ77(filepath);
                    if (exitCode != 0)
                        Console.WriteLine("Error: file compression unsuccessful.");
                    else
                        Console.WriteLine("File compression was successful!");
                    break;
                case "-d":
                    Decompress7bitLZ77(filepath);
                    break;
                default:
                    Console.WriteLine("invalid option. . .\nterminating.");
                    break;
            }
        }

        static int Compress7bitLZ77(string filepath)
        {
            byte[] inputBytes = File.ReadAllBytes(filepath);
            int SEARCH_BUFFER_SIZE = Pointer7bitLZ77.MAX_OFFSET;
            int LOOKAHEAD_BUFFER_SIZE = Pointer7bitLZ77.MAX_LENGHT;

            Queue<byte> search = new Queue<byte>();
            Queue<byte> lahead = new Queue<byte>();
            search.Enqueue(inputBytes[0]);
            int strIndex;
            for (strIndex = 1; strIndex < LOOKAHEAD_BUFFER_SIZE + 1; strIndex++)
                lahead.Enqueue(inputBytes[strIndex]);


            byte[] laheadArr = lahead.ToArray();
            byte[] searchArr = search.ToArray().Concat(laheadArr).ToArray(); // this is actually the entire window array

            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(inputBytes[0]);

            // loop while lahead is not empty

            while (laheadArr.Length > 0)
            {
                int searchIndex = searchArr.Length - laheadArr.Length - 1;

                int maxMatchLenght = 0;
                int maxMatchOffset = -1;
                while (searchIndex > -1)
                {
                    int matchLenght = 0;
                    int laheadIndex = 0;
                    int tempSIndex = searchIndex;
                    while (laheadIndex < laheadArr.Length && laheadArr[laheadIndex] == searchArr[tempSIndex])
                    {
                        laheadIndex++;
                        tempSIndex++;

                        matchLenght++;
                    }

                    if (matchLenght > maxMatchLenght)
                    {
                        maxMatchLenght = matchLenght;
                        maxMatchOffset = searchArr.Length - laheadArr.Length - searchIndex;
                    }

                    searchIndex--;
                }
                if (maxMatchLenght > Pointer7bitLZ77.MEMORY_SIZE_OF_POINTER) // match is usefull
                {
                    // BINGO
                    Pointer7bitLZ77 myLz = new Pointer7bitLZ77(maxMatchLenght, maxMatchOffset);
                    writer.Write((byte)(myLz.value >> 8));
                    writer.Write((byte)myLz.value);
                    for (int i = 0; i < maxMatchLenght; i++)
                    {
                        search.Enqueue(lahead.First());
                        if (search.Count > SEARCH_BUFFER_SIZE)
                            search.Dequeue();
                        lahead.Dequeue();
                        if (strIndex < inputBytes.Length - 1)
                            lahead.Enqueue(inputBytes[strIndex++]);
                    }
                }
                else
                {
                    // we could make this into a separate method. . .
                    writer.Write(lahead.First());
                    search.Enqueue(lahead.First());
                    if (search.Count > SEARCH_BUFFER_SIZE)
                        search.Dequeue();
                    lahead.Dequeue();
                    if (strIndex < inputBytes.Length - 1)
                        lahead.Enqueue(inputBytes[strIndex++]);
                }

                laheadArr = lahead.ToArray();
                searchArr = search.ToArray().Concat(laheadArr).ToArray(); // this is actually the entire window array

            }

            using (FileStream fs = new FileStream(filepath + ".7lz77", FileMode.OpenOrCreate))
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    bw.Write(stream.ToArray());
                    bw.Write(0x0a);
                }
            }

            return 0; // success
        }

        static int Decompress7bitLZ77(string filepath)
        {
            byte[] inputBytes = File.ReadAllBytes(filepath);

            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(inputBytes[0]); // write 1st byte since it can't be a pointer

            int arrIndex = 1;

            Queue<byte> decoded = new Queue<byte>();
            decoded.Enqueue(inputBytes[0]);

            byte[] decodedArr;

            while (arrIndex < inputBytes.Length)
            {
                int offset;
                int lenght;
                ushort pointerBytes;

                if ((inputBytes[arrIndex] & Pointer7bitLZ77.POINTER_FLAG) != 0)
                {
                    // we have a pointer
                    pointerBytes = (ushort)((inputBytes[arrIndex] << 8) + inputBytes[arrIndex + 1]);
                    offset = pointerBytes & Pointer7bitLZ77.OFFSET_MASK;
                    lenght = (pointerBytes & Pointer7bitLZ77.LENGHT_MASK) >> 11;

                    arrIndex += 2; // skip over pointer

                    decodedArr = decoded.ToArray();

                    if (lenght > offset)
                    {
                        // hack hack hack all day long
                        // hack hack hack while i sing this song
                        byte[] hackArr = new byte[offset];
                        Array.Copy(decodedArr, decodedArr.Length - offset, hackArr, 0, offset);

                        for (int i = 0; i < lenght; i++)
                        {
                            decoded.Enqueue(hackArr[i % offset]);
                            writer.Write(hackArr[i % offset]);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < lenght; i++)
                        {
                            decoded.Enqueue(decodedArr[decodedArr.Length - offset + i]);
                            writer.Write(decodedArr[decodedArr.Length - offset + i]);
                        }
                    }

                    Console.WriteLine(arrIndex);
                }
                else
                {
                    decoded.Enqueue(inputBytes[arrIndex]);
                    writer.Write(inputBytes[arrIndex]);
                    arrIndex++;
                }
            }

            using (FileStream fs = new FileStream(filepath + ".decoded", FileMode.OpenOrCreate))
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    bw.Write(stream.ToArray());
                    // bw.Write(0x0a);
                }
            }

            return 0; // success
        }

        struct Pointer7bitLZ77
        {
            /* LZ77 POINTER: (for encoding 7bit ascii)
             * 0000 0000    0000 0000
             * 1                        pointer     1 bit
             *  xxx x                   lenght      4 bits  max value 15
             *       xxx    xxxx xxxx   disntace    11 bits max value 2047
             *             
             * memory of struct: 2 bytes
             */

            // hardcoded for now . . .
            public static int MAX_LENGHT = 15;
            public static int MAX_OFFSET = 2047;
            public static byte POINTER_FLAG  = 0b10000000;
            public static ushort LENGHT_MASK   = 0b0111100000000000;
            public static ushort OFFSET_MASK = 0b0000011111111111;
            public static int MEMORY_SIZE_OF_POINTER = 2; // 2 bytes, any sequence longer than 2 should be encoded

            public ushort value { get; set; } // the sacred 2 bytes

            public Pointer7bitLZ77(int lenght, int offset)
            {
                value = 0;
                value += (ushort)(POINTER_FLAG << 8);
                if (lenght <= MAX_LENGHT)
                    value += (ushort)(lenght << 11);
                else
                    Console.WriteLine("ERROR");

                if (offset <= MAX_OFFSET)
                    value += (ushort)offset;
                else
                    Console.WriteLine("ERROR");
            }
        }
    }
}