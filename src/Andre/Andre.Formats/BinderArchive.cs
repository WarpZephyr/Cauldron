using Andre.Core;
using Andre.Core.Util;
using Andre.Formats.Util;
using DotNext.IO.MemoryMappedFiles;
using SoulsFormats;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;

// Credit to GoogleBen (https://github.com/googleben/Smithbox/tree/VFS)
namespace Andre.Formats
{
    public class BinderArchive : IDisposable, IAsyncDisposable
    {
        public static int ThreadsForDecryption = Environment.ProcessorCount > 4 ? Environment.ProcessorCount / 2 : 4;

        private BHD5 bhd;
        public FileStream Bdt { get; }

        public MemoryMappedFile BdtMmf { get; }

        public bool BhdWasEncrypted { get; }

        public BinderArchive(BHD5 bhd, FileStream bdt, MemoryMappedFile bdtMmf, bool wasEncrypted = false)
        {
            this.bhd = bhd;
            this.Bdt = bdt;
            this.BdtMmf = bdtMmf;
            this.BhdWasEncrypted = wasEncrypted;
        }

        public BinderArchive(BHD5 bhd, FileStream bdt, bool wasEncrypted = false)
        {
            this.bhd = bhd;
            Bdt = bdt;
            BhdWasEncrypted = wasEncrypted;
            BdtMmf = MemoryMappedFile.CreateFromFile(bdt, bdt.Name, bdt.Length, MemoryMappedFileAccess.Read,
                HandleInheritability.None, true);
        }

        public static bool IsBhdEncrypted(Memory<byte> bhd)
        {
            string sig = "";
            try
            {
                sig = System.Text.Encoding.ASCII.GetString(bhd.Span[..4]);
            }
            catch
            {
                //assume this means it's encrypted
                return true;
            }

            return sig != "BHD5";
        }

        public static byte[] Decrypt(Memory<byte> encryptedBhd, string bhdPath, string decryptionKey)
        {
            return NativeRsa.Decrypt(encryptedBhd, decryptionKey, ThreadsForDecryption);
        }

        public static byte[] Decrypt(string bhdPath, string decryptionKey)
        {
            return SoulsFormats.Util.CryptographyUtility.DecryptRsa(bhdPath, decryptionKey).ToArray();
        }

        public BinderArchive(string bhdPath, string bdtPath, BHD5.Game game, bool isEncrypted, string? decryptionKey)
        {
            var fs = new FileStream(bhdPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var file = MemoryMappedFile.CreateFromFile(fs, null, 0, MemoryMappedFileAccess.Read,
                HandleInheritability.None, leaveOpen: false);
            using var accessor = file.CreateMemoryAccessor(0, 0, MemoryMappedFileAccess.Read);

            if (IsBhdEncrypted(accessor.Memory))
            {
                // Encrypted
                if (!isEncrypted)
                {
                    // Handle when invalid or encrypted archives are passed when the parameter says otherwise by doing this
                    throw new ArgumentException("Archive was determined to be possibly encrypted, but the constructor was told otherwise, is the archive valid?", nameof(isEncrypted));
                }

                if (string.IsNullOrWhiteSpace(decryptionKey))
                {
                    throw new ArgumentException("Archive was determined to be encrypted but no valid key was passed to decrypt it with.", nameof(decryptionKey));
                }

#if DEBUG
                Debug.WriteLine($"Decrypting {Path.GetFileName(bhdPath)}");
#endif
                byte[] decrypted;
#if WINDOWS
                decrypted = Decrypt(accessor.Memory, bhdPath, decryptionKey);
#else
                decrypted = Decrypt(bhdPath, encryptionKey);
#endif
                bhd = BHD5.Read(decrypted, game);
                BhdWasEncrypted = true;
            }
            else
            {
                bhd = BHD5.Read(accessor.Memory, game);
                BhdWasEncrypted = true;
            }

            Bdt = File.OpenRead(bdtPath);
            BdtMmf = MemoryMappedFile.CreateFromFile(Bdt, null, 0, MemoryMappedFileAccess.Read,
                HandleInheritability.None, true);
        }

        public BHD5.FileHeader? TryGetFileFromHash(ulong hash)
        {
            return bhd.Buckets.SelectMany(b => b.Where(f => f.FileNameHash == hash)).FirstOrDefault();
        }

        public byte[] ReadFile(BHD5.FileHeader file) => file.ReadFile(Bdt);

        public byte[]? TryReadFileFromHash(ulong hash) => TryGetFileFromHash(hash)?.ReadFile(Bdt);

        public List<BHD5.Bucket> Buckets => bhd.Buckets;

        public IEnumerable<BHD5.FileHeader> EnumerateFiles() =>
            Buckets.Select(b => b.AsEnumerable()).Aggregate(Enumerable.Concat);

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Bdt.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            await Bdt.DisposeAsync();
        }
    }
}