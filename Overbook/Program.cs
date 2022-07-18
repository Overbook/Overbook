using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using System.IO;
using System.Text.RegularExpressions;
using NAudio.Wave;
using System.Linq;

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
                if (!match.Success || match.Groups.Count == 0)
                    continue;
                chapters.Add(new Chapter
                {
                    Number = int.Parse(match.Groups[1].Value),
                    Name = match.Groups.Count > 1 ? match.Groups[2].Value : "",
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
            if (string.IsNullOrEmpty(Name))
                return $"Chapter {Number}";
            else
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
        class BookMetadata
        {
            public string Title;
            public string Author;
            public string Description;
            public string PictureExtension;
            public byte[] PictureData;
            public byte[] RawId3v2Tag;
            public string[] Mp3Files;

            public static BookMetadata FromFolder(string folder)
            {
                var files = Directory.EnumerateFiles(folder, "*.mp3").ToArray();
                foreach (var file in files)
                {
                    var tfile = TagLib.File.Create(file);
                    var title = tfile.Tag.Album;
                    if (string.IsNullOrEmpty(title))
                        title = tfile.Tag.Title;
                    title = title?.Replace("Unabridged", "").Replace("Abridged", "").TrimEnd(' ', '(', ')', '-');
                    var author = tfile.Tag.FirstAlbumArtist;
                    if (string.IsNullOrEmpty(author))
                        author = tfile.Tag.FirstPerformer;
                    var description = tfile.Tag.Comment;
                    var picture = tfile.Tag.Pictures.FirstOrDefault();
                    if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(author))
                    {
                        using (var reader = new Mp3FileReader(file))
                        {
                            var meta = new BookMetadata
                            {
                                Title = title,
                                Author = author,
                                Description = description?.Replace("\r", "").Trim(),
                                Mp3Files = files,
                                RawId3v2Tag = reader.Id3v2Tag?.RawData,
                            };
                            if (picture != null)
                            {
                                switch (picture.MimeType)
                                {
                                    case "image/jpeg":
                                        meta.PictureExtension = ".jpg";
                                        break;
                                    default:
                                        throw new InvalidDataException($"Unknown picture mime type {picture.MimeType}");
                                }
                                meta.PictureData = picture.Data.Data;
                            }
                            return meta;
                        }
                    }
                }
                throw new ArgumentException($"No metadata found in folder {folder}");
            }

            public class ProcessResult
            {
                public string MergedFile;
                public long MergedFileSize => new FileInfo(MergedFile).Length;
                public string PictureFile;
            }

            public ProcessResult Process(string outputFolder)
            {
                var baseName = $"{Author} - {Title}".Replace(":", "");
                var result = new ProcessResult
                {
                    MergedFile = Path.Combine(outputFolder, baseName + ".mp3"),
                    PictureFile = Path.Combine(outputFolder, baseName + PictureExtension),
                };
                if (Mp3Files.Length == 0)
                {
                    throw new InvalidDataException("No MP3 files found in input folder");
                }
                else if (Mp3Files.Length == 1)
                {
                    File.Copy(Mp3Files[0], result.MergedFile, true);
                    if (PictureData != null)
                    {
                        File.WriteAllBytes(result.PictureFile, PictureData);
                    }
                }
                else
                {
                    // Based on: https://stackoverflow.com/a/4126991
                    using (var output = new FileStream(result.MergedFile, FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        foreach (var file in Mp3Files)
                        {
                            Console.WriteLine($"Merging {file}");
                            var reader = new Mp3FileReader(file);
                            if (output.Position == 0 && RawId3v2Tag != null)
                            {
                                output.Write(RawId3v2Tag, 0, RawId3v2Tag.Length);
                            }
                            Mp3Frame frame;
                            while ((frame = reader.ReadNextFrame()) != null)
                            {
                                output.Write(frame.RawData, 0, frame.RawData.Length);
                            }
                        }
                    }
                    if (PictureData != null)
                    {
                        File.WriteAllBytes(result.PictureFile, PictureData);
                    }
                }
                return result;
            }
        }

        public static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: Overbook <audiobook-folder>");
                return 1;
            }
            var audiobookFolder = args[0].TrimEnd('\"');
            var wwwPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "www");
            if (!Directory.Exists(wwwPath))
            {
                Directory.CreateDirectory(wwwPath);
            }
            var feedXmlFile = Path.Combine(wwwPath, "feed.xml");
            if (!File.Exists(feedXmlFile))
            {
                Console.WriteLine("www/feed.xml not found, copy a template there to get started");
                return 1;
            }
            var utf8EncodingNoBom = new UTF8Encoding(false);
            var feed = Utils.Deserialize<Rss>(File.ReadAllText(feedXmlFile, utf8EncodingNoBom));
            if (feed.Channel.AtomLink == null || feed.Channel.AtomLink.Rel != "self")
            {
                Console.WriteLine("<atom:link /> not correct (needs to have rel=\"self\"");
                return 1;
            }
            var selfLink = feed.Channel.AtomLink.Href;
            var lastSlashIdx = selfLink.LastIndexOf('/');
            if (lastSlashIdx == -1)
                throw new InvalidDataException("Invalid URL " + selfLink);
            var baseUrl = selfLink.Substring(0, lastSlashIdx);
            Console.WriteLine($"Detected base URL: {baseUrl}");
            var meta = BookMetadata.FromFolder(audiobookFolder);
            var result = meta.Process(wwwPath);
            var bookUrl = $"{baseUrl}/{Uri.EscapeUriString(Path.GetFileName(result.MergedFile))}";
            var imageUrl = $"{baseUrl}/{Uri.EscapeUriString(Path.GetFileName(result.PictureFile))}";
            var date = DateTime.UtcNow;
            var dateText = Utils.DateToString(date);
            var item = new Item
            {
                Title = $"{meta.Author} - {meta.Title}",
                Link = bookUrl,
                PubDate = dateText,
                Description = meta.Description,
                Enclosure = new Enclosure
                {
                    Url = bookUrl,
                    Length = result.MergedFileSize,
                    Type = "audio/mpeg",
                },
                Summary = "",
                Guid = new Guid(System.Guid.NewGuid().ToString()),
            };
            if (File.Exists(result.PictureFile))
            {
                item.Image = new Image(imageUrl);
            }
            feed.Channel.Items.Add(item);
            feed.Channel.LastBuildDate = dateText;
            Console.WriteLine($"Adding audiobook: {item.Title}");
            var newFeedXml = Utils.Serialize(feed);
            var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var backupFeedFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"feed_{Math.Floor((date - origin).TotalMilliseconds)}.xml");
            File.Copy(feedXmlFile, backupFeedFile);
            File.WriteAllText(feedXmlFile, newFeedXml, utf8EncodingNoBom);
            return 0;
        }

        public static void MainOld(string[] args)
        {
            if (args.Length < 4)
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
            var chapters = Utils.GetChaptersFromDirectory(dir, regex); // @".+- (\d+) - (.+).mp3"
            var date = DateTime.Today;
            foreach (var ch in chapters)
            {
                var item = ch.ToItem(url);
                item.PubDate = Utils.DateToString(date);
                date = date.AddMinutes(1);
                items.Add(item);
            }
            var channel = Utils.Deserialize<Rss>(File.ReadAllText(channelFile, Encoding.UTF8)).Channel;
            channel.LastBuildDate = Utils.DateToString(date);
            channel.Items = items;
            var rss = new Rss(channel);
            var xml = Utils.Serialize(rss);
            Console.OutputEncoding = Encoding.UTF8;
            Console.Write(xml);
        }
    }
}
