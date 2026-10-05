using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

static class Program
{
    const string Phrase = "IPutMyTrustInYouPushedAsFarAsICouldGo";

    static byte[] Concat(params byte[][] parts)
    {
        int n = 0; foreach (var p in parts) n += p.Length;
        var o = new byte[n]; int k = 0;
        foreach (var p in parts) { Buffer.BlockCopy(p, 0, o, k, p.Length); k += p.Length; }
        return o;
    }

    static byte[] Be32(uint v) => new[] { (byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v };

    static byte[] Hkdf(byte[] ikm, byte[] salt, byte[] info, int len)
        => HKDF.DeriveKey(HashAlgorithmName.SHA256, ikm, len, salt, info);

    static byte[] Master() => SHA256.HashData(Encoding.UTF8.GetBytes(Phrase));

    sealed class Rec
    {
        public string Name; public byte[] Salt, Nonce, Iv, Ct, Mac; public uint Length; public byte[] Raw;
        public byte[] Plain;
    }

    static Rec Parse(string path)
    {
        var b = File.ReadAllBytes(path);
        if (b.Length < 54 + 32) return null;
        if (!(b[0] == (byte)'J' && b[1] == (byte)'R' && b[2] == (byte)'S' && b[3] == (byte)'F')) return null;
        var r = new Rec { Name = Path.GetFileNameWithoutExtension(path), Raw = b };
        r.Salt = b[6..22]; r.Nonce = b[22..34];
        r.Length = ((uint)b[34] << 24) | ((uint)b[35] << 16) | ((uint)b[36] << 8) | b[37];
        r.Iv = b[38..54];
        int ctLen = (int)r.Length;
        if (54 + ctLen + 32 != b.Length) return null;
        r.Ct = b[54..(54 + ctLen)]; r.Mac = b[(54 + ctLen)..];
        return r;
    }

    static bool VerifyMac(Rec r)
    {
        var macKey = Hkdf(Master(), r.Salt, Encoding.ASCII.GetBytes("mac"), 32);
        using var h = new HMACSHA256(macKey);
        var want = h.ComputeHash(Concat(r.Salt, r.Nonce, Be32(r.Length), r.Iv, r.Ct));
        return want.AsSpan().SequenceEqual(r.Mac);
    }

    static byte[] DecryptPayload(Rec r)
    {
        var encKey = Hkdf(Master(), r.Salt, Encoding.ASCII.GetBytes("enc"), 32);
        using var aes = Aes.Create();
        aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
        aes.Key = encKey; aes.IV = r.Iv;
        using var dec = aes.CreateDecryptor();
        var decrypted = dec.TransformFinalBlock(r.Ct, 0, r.Ct.Length);
        using var ms = new MemoryStream(decrypted);
        using var gz = new GZipStream(ms, CompressionMode.Decompress);
        using var outMs = new MemoryStream();
        gz.CopyTo(outMs);
        return outMs.ToArray();
    }

    static void Main(string[] args)
    {
        string dir = @"C:\Users\game\AppData\LocalLow\Mudtek\Dimraeth\Characters";
        string outDir = @"D:\SteamLibrary\steamapps\common\Dimraeth\modding\SaveProbe\out";
        Directory.CreateDirectory(outDir);

        foreach (var path in Directory.GetFiles(dir, "*.jrf").OrderBy(x => x))
        {
            var r = Parse(path);
            if (r == null) { Console.WriteLine($"SKIP {Path.GetFileName(path)}"); continue; }
            bool mac = VerifyMac(r);
            string txt = "";
            try
            {
                r.Plain = DecryptPayload(r);
                txt = Encoding.UTF8.GetString(r.Plain);
                File.WriteAllText(Path.Combine(outDir, r.Name + ".json"), txt);
            }
            catch (Exception e) { txt = "<decrypt failed: " + e.Message + ">"; }
            Console.WriteLine($"{r.Name,-10} macValid={mac} plainLen={(r.Plain?.Length ?? 0)} -> {Path.Combine(outDir, r.Name + ".json")}");
            Console.WriteLine("   preview: " + txt.Substring(0, Math.Min(200, txt.Length)).Replace("\n", " "));
        }
    }
}
