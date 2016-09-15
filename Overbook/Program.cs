using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml;
using System.IO;

namespace Overbook
{
    class Program
    {
        public class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding { get { return Encoding.UTF8; } }
        }

        public static string Serialize<T>(T value)
        {
            if (value == null)
                return null;

            var serializer = new XmlSerializer(typeof(T));

            var ns = new XmlSerializerNamespaces();
            ns.Add("", "");

            var settings = new XmlWriterSettings();
            settings.Encoding = new UTF8Encoding(false, false);
            settings.Indent = false;
            settings.OmitXmlDeclaration = false;
            settings.Indent = true;

            using (var textWriter = new Utf8StringWriter())
            {
                using (var xmlWriter = XmlWriter.Create(textWriter, settings))
                {
                    serializer.Serialize(xmlWriter, value, ns);
                }
                return textWriter.ToString();
            }
        }

        public static T Deserialize<T>(string xml)
        {
            if (string.IsNullOrEmpty(xml))
                return default(T);

            var serializer = new XmlSerializer(typeof(T));

            var settings = new XmlReaderSettings();
            // No settings need modifying here

            using (var textReader = new StringReader(xml))
            {
                using (var xmlReader = XmlReader.Create(textReader, settings))
                {
                    return (T)serializer.Deserialize(xmlReader);
                }
            }
        }

        static void Main(string[] args)
        {
            var items = new List<Item>();
            items.Add(new Item
            {
                Title = "Our first Item",
                Link = "http://testing.com/Our_first_item.mp3",
                PubDate = "Wed, 13 Aug 2014 15:47:00 GMT",
                Description = "This is our first item in our feed",
                Enclosure = new Enclosure
                {
                    Url = "http://files.idrsolutions.com/Java_PDF_Podcasts/Interview_4_Advice_and_XFA.mp3",
                    Length = "11779397",
                    Type = "audio/mpeg"
                },
                Summary = "Our fiest item",
                Image = new Image
                {
                    Href = "http://files.idrsolutions.com/Java_PDF_Podcasts/idrlogo.png"
                },
                Guid = new Guid
                {
                    IsPermaLink = "false",
                    Text = "ce094c6b-4918-4833-a3fc-c466b3431cd0"
                }
            });
            var rss = new Rss
            {
                Atom = "http://www.w3.org/2005/Atom",
                Itunes = "http://www.itunes.com/dtds/podcast-1.0.dtd",
                Version = "2.0",
                Channel = new Channel
                {
                    Title = "This is our Feed title",
                    Link = "http://www.idrsolutions.com",
                    Description = "This is a breif description of our podcast",
                    Language = "en-us",
                    Copyright = "IDRSolutions copyright 2014",
                    AtomLink = new AtomLink
                    {
                        Href = "https://ogilvie.gq/overbook/feed.xml",
                        Rel = "self",
                        Type = "application/rss+xml"
                    },
                    LastBuildDate = "Wed, 13 Aug 2014 15:47:00 GMT",
                    Author = "IDRSolutions",
                    Summary = "Our First itunes feed",
                    Owner = new Owner
                    {
                        Name = "IDRSolutions",
                        Email = "contact2007@idrsolutions.com"
                    },
                    Explicit = "No",
                    Image = new Image
                    {
                        Href = "http://files.idrsolutions.com/Java_PDF_Podcasts/idrlogo.png"
                    },
                    Category = new Category
                    {
                        Text = "Technology"
                    },
                    Items = items
                }
            };
            var xml = Serialize(rss);
            Console.WriteLine(xml.Replace("\r", ""));
        }
    }
}
