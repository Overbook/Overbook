using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtractAlbumArt
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length < 1)
            {
                Console.WriteLine("Usage: ExtractAlbumArt file.mp3");
                return;
            }
            System.IO.File.WriteAllBytes(System.IO.Path.GetFileNameWithoutExtension(args[0]) + ".jpeg", TagLib.File.Create(args[0]).Tag.Pictures[0].Data.Data);
        }
    }
}
