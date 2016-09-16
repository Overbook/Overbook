using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using System.IO;

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
    }

    public static class Program
    {
        public static void Main(string[] args)
        {
            var items = new List<Item>();
            items.Add(new Item
            {
                Title = "Our first Item",
                Link = "http://testing.com/Our_first_item.mp3",
                PubDate = Utils.DateToString(new DateTime(2014, 8, 13, 15, 47, 0)),
                Description = "This is our first item in our feed",
                Enclosure = new Enclosure
                {
                    Url = "http://files.idrsolutions.com/Java_PDF_Podcasts/Interview_4_Advice_and_XFA.mp3",
                    Length = 11779397,
                    Type = "audio/mpeg"
                },
                Summary = "Our first item",
                Image = new Image("http://files.idrsolutions.com/Java_PDF_Podcasts/idrlogo.png"),
                Guid = new Guid("ce094c6b-4918-4833-a3fc-c466b3431cd0")
            });
            var rss = new Rss(new Channel
            {
                Title = "This is our Feed title",
                Link = "http://www.idrsolutions.com",
                Description = "This is a brief description of our podcast",
                Language = "en-us",
                Copyright = "IDRSolutions copyright 2014",
                AtomLink = new AtomLink("https://ogilvie.gq/overbook/feed.xml"),
                LastBuildDate = Utils.DateToString(new DateTime(2014, 8, 13, 15, 47, 0)),
                Author = "IDRSolutions",
                Summary = "Our First itunes feed",
                Owner = new Owner("IDRSolutions", "contact2007@idrsolutions.com"),
                Image = new Image("http://files.idrsolutions.com/Java_PDF_Podcasts/idrlogo.png"),
                Category = new Category("Technology"),
                Items = items
            });
            var xml = Utils.Serialize(rss);
            Console.Write(xml);
        }
    }
}
