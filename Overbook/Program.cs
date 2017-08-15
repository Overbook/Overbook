using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using System.IO;
using System.Text.RegularExpressions;

namespace Overbook
{
    public static class Utils
    {
        public class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding { get { return Encoding.UTF8; } }
        }

        public static string DateToString(DateTime date)
        {
            var months = new[]
            {
                "Jan",
                "Feb",
                "Mar",
                "Apr",
                "May",
                "Jun",
                "Jul",
                "Aug",
                "Sep",
                "Oct",
                "Nov",
                "Dec"
            };
            return string.Format("{0}, {1} {2} {3} {4:D2}:{5:D2}:{6:D2} GMT",
                                 date.DayOfWeek.ToString().Substring(0, 3),
                                 date.Day,
                                 months[date.Month - 1],
                                 date.Year,
                                 date.Hour,
                                 date.Minute,
                                 date.Second);
        }

        public static string Serialize<T>(T value)
        {
            if (value == null)
                return null;

            var serializer = new XmlSerializer(typeof(T));
            var ns = new XmlSerializerNamespaces();
            ns.Add("", "");

            var settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false, false),
                Indent = true,
                OmitXmlDeclaration = false
            };

            using (var textWriter = new Utf8StringWriter())
            {
                using (var xmlWriter = XmlWriter.Create(textWriter, settings))
                    serializer.Serialize(xmlWriter, value, ns);
                return textWriter.ToString();
            }
        }

        public static T Deserialize<T>(string xml)
        {
            if (string.IsNullOrEmpty(xml))
                return default(T);

            var serializer = new XmlSerializer(typeof(T));
            using (var textReader = new StringReader(xml))
            using (var xmlReader = XmlReader.Create(textReader, new XmlReaderSettings()))
                return (T)serializer.Deserialize(xmlReader);
        }

        public static IEnumerable<Chapter> GetChaptersFromDirectory(string path, string regex)
        {
            var chapters = new List<Chapter>();
            //TODO: recursive + chapter directory structure
            foreach (var file in Directory.EnumerateFiles(path, "*.*"))
            {
                var filename = Path.GetFileName(file);
                var match = Regex.Match(filename, regex);
                if (!match.Success)
                    continue;
                chapters.Add(new Chapter
                {
                    Number = int.Parse(match.Groups[1].Value),
                    Name = match.Groups[2].Value,
                    File = file
                });
            }
            chapters.Sort((a, b) => a.Number.CompareTo(b.Number));
            return chapters;
        }
    }

    public class Chapter
    {
        public int Number;
        public string Name;
        public string File;

        public override string ToString()
        {
            return $"Chapter {Number}: {Name}";
        }

        public Item ToItem(string baseUrl)
        {
            var url = Uri.EscapeUriString(baseUrl + Path.GetFileName(File));
            return new Item
            {
                Title = ToString(),
                Link = url,
                PubDate = Utils.DateToString(DateTime.Today),
                Description = ToString(),
                Enclosure = new Enclosure
                {
                    Url = url,
                    Length = new FileInfo(File).Length,
                    Type = "audio/mpeg"
                },
                Summary = ToString(),
                Guid = new Guid(System.Guid.NewGuid().ToString())
            };
        }
    }

    public static class Program
    {
        public static void Main(string[] args)
        {
            if(args.Length < 4)
            {
                Console.WriteLine("Usage: Overbook dir regex channelFile url");
                Console.WriteLine("Example: Overbook \"C:\\Audiobook\" \".+ -(\\d +) - (.+).mp3\" channel.xml http://mysite/book1/");
                return;
            }
            var dir = args[0];
            var regex = args[1];
            var channelFile = args[2];            
            var url = args[3];
            var items = new List<Item>();
            var chapters = Utils.GetChaptersFromDirectory(dir, regex);// @".+- (\d+) - (.+).mp3"
            var date = DateTime.Today;
            foreach (var ch in chapters)
            {
                var item = ch.ToItem(url);
                item.PubDate = Utils.DateToString(date);
                date = date.AddMinutes(1);
                items.Add(item);
            }
            var channel = Utils.Deserialize<Channel>(File.ReadAllText(channelFile));
            channel.LastBuildDate = Utils.DateToString(date);
            channel.Items = items;
            var rss = new Rss(channel);
            var xml = Utils.Serialize(rss);
            Console.Write(xml);
        }
    }
}
