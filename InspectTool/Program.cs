using System;
using System.IO;
using Iced.Intel;

class Program
{
    static void Main()
    {
        byte[] bytes = File.ReadAllBytes(@"D:\SteamLibrary\steamapps\common\Dimraeth\GameAssembly.dll");
        int fileOffset = 0x10C07D0; // Offset from out\Runes.cs
        var reader = new ByteArrayCodeReader(bytes, fileOffset, 0x100);
        var decoder = Decoder.Create(64, reader);
        decoder.IP = 0x10C17D0; // RVA
        var formatter = new NasmFormatter();
        var output = new StringOutput();

        Console.WriteLine($"Disassembly at FileOffset 0x{fileOffset:X}:");
        for (int i = 0; i < 30 && reader.CanReadByte; i++)
        {
            decoder.Decode(out var instr);
            output.Reset();
            formatter.Format(instr, output);
            Console.WriteLine($"0x{instr.IP:X8}: {output.ToStringAndReset()}");
        }
    }
}
