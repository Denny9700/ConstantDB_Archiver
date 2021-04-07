namespace ConstantDB_Archiver
{
  using System;
  using System.Drawing;
  using System.IO;
  using System.Linq;
  using System.Text;

  using Ionic.Zip;
  using Ionic.Zlib;

  using Colorful;
  using Console = Colorful.Console;

  class Program
  {
    private static readonly string project_name = AppDomain.CurrentDomain.FriendlyName;
    private static readonly byte[] key = { 0x10, 0x13, 0x21, 0x14, 0x09, 0x04,
                                           0x01, 0x18, 0x17, 0x02, 0x08, 0x13,
                                           0x0A, 0x12, 0x02, 0x05, 0x07, 0x09,
                                           0x03, 0x07, 0x01, 0x17, 0x0F, 0x0B,
                                           0x11, 0x10                         };

    private static Formatter[] entryFormatter = null;
    private static Formatter[] importFormatter = null;

    static void Main(string[] args)
    {
      Console.WriteAscii("DIP Archiver", Color.Red);

      if (args.Length < 2)
      {
        Console.WriteLine("You must to insert at last 2 args:", Color.Red);
        printUsage();

        Close();
        return;
      }

      entryFormatter = new Formatter[]
      {
        new Formatter($"Name", Color.Orange),
        new Formatter($"Compressed size", Color.Orange),
        new Formatter($"Encrypted", Color.Orange),
        new Formatter($"Compression", Color.Orange),
        new Formatter($"CRC", Color.Orange)
      };

      importFormatter = new Formatter[]
      {
        new Formatter("Added", Color.Orange)
      };

      switch (args[0])
      {
        case "-p":
          {
            var dirPath = args[1];
            var pwdPath = args.Length > 2 ? args[2] : string.Empty;

            if (!Directory.Exists(dirPath))
            {
              Console.WriteLine("Invalid path selected, try again..");
              break;
            }

            Console.WriteLine("Creating new cgd.dip archive..", Color.White);
            using (ZipFile zip = new ZipFile
            {
              CompressionLevel = CompressionLevel.BestCompression,
              Password = string.IsNullOrEmpty(pwdPath) ? Encoding.UTF8.GetString(key)
                                                       : Encoding.UTF8.GetString(File.ReadAllBytes(pwdPath))
          })
            {
              var files = Directory.GetFiles(dirPath, "*",
                  SearchOption.AllDirectories).
                  Where(f => Path.GetExtension(f).
                      ToLowerInvariant() != ".dip").ToArray();

              foreach (var f in files)
              {
                zip.AddFile(f,
                    Path.GetDirectoryName(f).
                    Replace(dirPath, string.Empty));

                Console.WriteFormatted("{0}: " + f + "\n", Color.White, importFormatter);
              }

              zip.Save(Path.ChangeExtension(dirPath, ".dip"));
              Console.WriteLine("Archive saved", Color.Green);
            }
            break;
          }
        case "-u":
          {
            var dipPath = args[1];
            var pwdPath = args.Length > 2 ? args[2] : string.Empty;
            var outPath = Path.Combine(Path.GetDirectoryName(dipPath), "dip_out");

            if (!Path.GetExtension(dipPath).Equals(".dip"))
            {
              Console.WriteLine("Invalid file extension", Color.Red);
              break;
            }

            if (!ZipFile.CheckZipPassword(dipPath, string.IsNullOrEmpty(pwdPath) ? Encoding.UTF8.GetString(key)
                                                                                 : Encoding.UTF8.GetString(File.ReadAllBytes(pwdPath))))
            {
              Console.WriteLine("Invalid archive password", Color.Red);
              break;
            }

            Console.WriteLine("Starting the extraction of cdb files..", Color.White);

            var file = new ZipFile(dipPath);
            file.Password = string.IsNullOrEmpty(pwdPath) ? Encoding.UTF8.GetString(key)
                                                          : Encoding.UTF8.GetString(File.ReadAllBytes(pwdPath));

            Console.WriteLine($"Entries found: {file.Entries.Count}\n", Color.White);
            foreach (var entry in file.Entries)
            {
              Console.WriteFormatted("\t{0}: " + entry.FileName +
                                     "\n\t{1}: " + entry.CompressedSize +
                                     "\n\t{2}: " + entry.UsesEncryption +
                                     "\n\t{3}: " + entry.CompressionMethod + " [" + entry.CompressionLevel + "]" +
                                     "\n\t{4}: " + entry.Crc, Color.White, entryFormatter);

              Console.WriteLine("\n\t-------------------\n\n", Color.White);
              entry.Extract(outPath, ExtractExistingFileAction.OverwriteSilently);
            }

            Console.WriteLine("CDB files extracted", Color.Green);
            break;
          }
      }

      Close();
    }

    static void printUsage()
    {
      var formatter = new Formatter[]
      {
        new Formatter($"Usage:", Color.Orange),
        new Formatter($"{project_name}", Color.Gray),
        new Formatter($"-u <dip file> <key file (optional)>", Color.Orange),
        new Formatter($"-p <dip folder> <key file (optional)>", Color.Orange)
      };

      Console.WriteFormatted("{0}\n\tTo unpack: {1} {2}\n\tTo pack: {1} {3}", Color.White, formatter);
    }

    static void Close()
    {
      Console.WriteLine();
      Console.WriteLine("Press any key to exit..", Color.White);
      Console.ReadKey();
    }
  }
}
