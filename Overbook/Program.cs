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
            private static readonly string[] SupportedExtensions = { ".mp3", ".m4a", ".m4b" };

            public string Title;
            public string Author;
            public string Description;
            public string PictureExtension;
            public byte[] PictureData;
            public byte[] RawId3v2Tag;
            public string[] AudioFiles;
            public string OutputExtension;
            public string MimeType;

            public string DisplayTitle
            {
                get
                {
                    if (string.IsNullOrEmpty(Author))
                        return Title;
                    return $"{Author} - {Title}";
                }
            }

            private static bool IsSupportedAudioFile(string file)
            {
                return SupportedExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase);
            }

            private static bool IsMp3File(string file)
            {
                return string.Equals(Path.GetExtension(file), ".mp3", StringComparison.OrdinalIgnoreCase);
            }

            private static string GetMimeType(string file)
            {
                switch (Path.GetExtension(file).ToLowerInvariant())
                {
                    case ".mp3":
                        return "audio/mpeg";
                    case ".m4a":
                        return "audio/x-m4a";
                    case ".m4b":
                        return "audio/x-m4b";
                    default:
                        throw new InvalidDataException($"Unsupported audio extension {Path.GetExtension(file)}");
                }
            }

            private static string CleanTitle(string title)
            {
                return title?.Replace("Unabridged", "").Replace("Abridged", "").TrimEnd(' ', '(', ')', '-');
            }

            private static string SanitizeFileName(string name)
            {
                if (string.IsNullOrWhiteSpace(name))
                    return "Audiobook";
                foreach (var invalidChar in Path.GetInvalidFileNameChars())
                    name = name.Replace(invalidChar, '_');
                return name.Trim();
            }

            private static void CopyFile(string sourceFile, string destinationFile)
            {
                if (string.Equals(Path.GetFullPath(sourceFile), Path.GetFullPath(destinationFile), StringComparison.OrdinalIgnoreCase))
                    return;
                File.Copy(sourceFile, destinationFile, true);
            }

            private static void ApplyFilenameFallback(string file, ref string title, ref string author)
            {
                var filename = Path.GetFileNameWithoutExtension(file);
                var parts = filename
                    .Split(new[] { " - " }, StringSplitOptions.None)
                    .Select(part => part.Trim())
                    .Where(part => part.Length > 0)
                    .ToArray();

                if (string.IsNullOrEmpty(title))
                {
                    if (parts.Length >= 3)
                        title = $"{parts[0]} - {string.Join(" - ", parts.Skip(2).ToArray())}";
                    else if (parts.Length >= 1)
                        title = parts[0];
                    else
                        title = filename;
                }
                if (string.IsNullOrEmpty(author) && parts.Length >= 2)
                    author = parts[1];
            }

            private static void SetPicture(BookMetadata meta, TagLib.IPicture picture)
            {
                if (picture == null)
                    return;

                switch ((picture.MimeType ?? "").ToLowerInvariant())
                {
                    case "image/jpeg":
                    case "image/jpg":
                        meta.PictureExtension = ".jpg";
                        break;
                    case "image/png":
                        meta.PictureExtension = ".png";
                        break;
                    default:
                        throw new InvalidDataException($"Unknown picture mime type {picture.MimeType}");
                }
                meta.PictureData = picture.Data.Data;
            }

            private static BookMetadata ReadMetadata(string file, string[] files, bool useFilenameFallback)
            {
                var meta = new BookMetadata
                {
                    AudioFiles = files,
                    OutputExtension = Path.GetExtension(files[0]).ToLowerInvariant(),
                    MimeType = GetMimeType(files[0]),
                };

                try
                {
                    using (var tfile = TagLib.File.Create(file))
                    {
                        var title = tfile.Tag.Album;
                        if (string.IsNullOrEmpty(title))
                            title = tfile.Tag.Title;
                        meta.Title = CleanTitle(title);

                        meta.Author = tfile.Tag.FirstAlbumArtist;
                        if (string.IsNullOrEmpty(meta.Author))
                            meta.Author = tfile.Tag.FirstPerformer;

                        meta.Description = tfile.Tag.Comment?.Replace("\r", "").Trim();
                        SetPicture(meta, tfile.Tag.Pictures.FirstOrDefault());
                    }

                    if (IsMp3File(file))
                    {
                        using (var reader = new Mp3FileReader(file))
                            meta.RawId3v2Tag = reader.Id3v2Tag?.RawData;
                    }
                }
                catch (Exception ex)
                {
                    if (!useFilenameFallback)
                        throw;
                    Console.WriteLine($"Warning: Could not read metadata from {file}: {ex.Message}");
                }

                if (useFilenameFallback)
                    ApplyFilenameFallback(file, ref meta.Title, ref meta.Author);

                return meta;
            }

            public static BookMetadata FromPath(string path)
            {
                if (Directory.Exists(path))
                    return FromFolder(path);
                if (File.Exists(path))
                {
                    if (!IsSupportedAudioFile(path))
                        throw new ArgumentException($"Unsupported audio file {path}. Supported extensions: {string.Join(", ", SupportedExtensions)}");
                    return FromFiles(new[] { path }, $"file {path}");
                }
                throw new FileNotFoundException($"Input path not found: {path}", path);
            }

            public static BookMetadata FromFolder(string folder)
            {
                var files = Directory
                    .EnumerateFiles(folder, "*.*")
                    .Where(IsSupportedAudioFile)
                    .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                return FromFiles(files, $"folder {folder}");
            }

            private static BookMetadata FromFiles(string[] files, string source)
            {
                if (files.Length == 0)
                    throw new ArgumentException($"No supported audio files found in {source}. Supported extensions: {string.Join(", ", SupportedExtensions)}");

                if (files.Length > 1 && files.Any(file => !IsMp3File(file)))
                    throw new InvalidDataException("M4A/M4B merging is not supported. Point Overbook at a single .m4a or .m4b file.");

                var allowMissingAuthor = files.Length == 1;
                foreach (var file in files)
                {
                    var meta = ReadMetadata(file, files, allowMissingAuthor);
                    if (!string.IsNullOrEmpty(meta.Title) && (allowMissingAuthor || !string.IsNullOrEmpty(meta.Author)))
                        return meta;
                }
                throw new ArgumentException($"No metadata found in {source}");
            }

            public class ProcessResult
            {
                public string MergedFile;
                public long MergedFileSize => new FileInfo(MergedFile).Length;
                public string PictureFile;
                public string MimeType;
            }

            public ProcessResult Process(string outputFolder)
            {
                var baseName = SanitizeFileName(DisplayTitle);
                var result = new ProcessResult
                {
                    MergedFile = Path.Combine(outputFolder, baseName + OutputExtension),
                    PictureFile = PictureData == null ? null : Path.Combine(outputFolder, baseName + PictureExtension),
                    MimeType = MimeType,
                };
                if (AudioFiles.Length == 0)
                {
                    throw new InvalidDataException("No audio files found in input path");
                }
                else if (AudioFiles.Length == 1)
                {
                    Console.WriteLine($"Copying {AudioFiles[0]}");
                    CopyFile(AudioFiles[0], result.MergedFile);
                }
                else
                {
                    if (AudioFiles.Any(file => !IsMp3File(file)))
                        throw new InvalidDataException("M4A/M4B merging is not supported. Point Overbook at a single .m4a or .m4b file.");

                    // Based on: https://stackoverflow.com/a/4126991
                    using (var output = new FileStream(result.MergedFile, FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        foreach (var file in AudioFiles)
                        {
                            Console.WriteLine($"Merging {file}");
                            using (var reader = new Mp3FileReader(file))
                            {
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
                    }
                }

                if (PictureData != null && result.PictureFile != null)
                    File.WriteAllBytes(result.PictureFile, PictureData);

                return result;
            }
        }

        public static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: Overbook <audiobook-folder-or-file>");
                return 1;
            }
            var audiobookPath = args[0].TrimEnd('\"');
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
            var meta = BookMetadata.FromPath(audiobookPath);
            var result = meta.Process(wwwPath);
            var bookUrl = $"{baseUrl}/{Uri.EscapeUriString(Path.GetFileName(result.MergedFile))}";
            var imageUrl = result.PictureFile == null ? null : $"{baseUrl}/{Uri.EscapeUriString(Path.GetFileName(result.PictureFile))}";
            var date = DateTime.UtcNow;
            var dateText = Utils.DateToString(date);
            var item = new Item
            {
                Title = meta.DisplayTitle,
                Link = bookUrl,
                PubDate = dateText,
                Description = meta.Description,
                Enclosure = new Enclosure
                {
                    Url = bookUrl,
                    Length = result.MergedFileSize,
                    Type = result.MimeType,
                },
                Summary = "",
                Guid = new Guid(System.Guid.NewGuid().ToString()),
            };
            if (result.PictureFile != null && File.Exists(result.PictureFile))
            {
                item.Image = new Image(imageUrl);
            }
            if (feed.Channel.Items == null)
                feed.Channel.Items = new List<Item>();
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
