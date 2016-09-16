using System;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace Overbook
{
    [XmlRoot(ElementName = "owner", Namespace = "http://www.itunes.com/dtds/podcast-1.0.dtd")]
    public class Owner
    {
        public Owner()
        {
        }

        public Owner(string name, string email)
        {
            Name = name;
            Email = email;
        }

        [XmlElement(ElementName = "name", Namespace = "http://www.itunes.com/dtds/podcast-1.0.dtd")]
        public string Name { get; set; }

        [XmlElement(ElementName = "email", Namespace = "http://www.itunes.com/dtds/podcast-1.0.dtd")]
        public string Email { get; set; }
    }

    [XmlRoot(ElementName = "image", Namespace = "http://www.itunes.com/dtds/podcast-1.0.dtd")]
    public class Image
    {
        public Image()
        {
        }

        public Image(string href)
        {
            Href = href;
        }

        [XmlAttribute(AttributeName = "href")]
        public string Href { get; set; }
    }

    [XmlRoot(ElementName = "link", Namespace = "http://www.w3.org/2005/Atom")]
    public class AtomLink
    {
        public AtomLink()
        {
            Rel = "self";
            Type = "application/rss+xml";
        }

        public AtomLink(string href)
            : this()
        {
            Href = href;
        }

        [XmlAttribute(AttributeName = "href")]
        public string Href { get; set; }

        [XmlAttribute(AttributeName = "rel")]
        public string Rel { get; set; }

        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }
    }

    [XmlRoot(ElementName = "category", Namespace = "http://www.itunes.com/dtds/podcast-1.0.dtd")]
    public class Category
    {
        public Category()
        {
        }

        public Category(string text)
        {
            Text = text;
        }

        [XmlAttribute(AttributeName = "text")]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "enclosure")]
    public class Enclosure
    {
        public Enclosure()
        {
        }

        public Enclosure(string url, int length, string type)
        {
            Url = url;
            Length = length;
            Type = type;
        }

        [XmlAttribute(AttributeName = "url")]
        public string Url { get; set; }

        [XmlAttribute(AttributeName = "length")]
        public int Length { get; set; }

        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }
    }

    [XmlRoot(ElementName = "guid")]
    public class Guid
    {
        public Guid()
        {
            IsPermaLink = false;
        }

        public Guid(System.Guid guid)
            : this()
        {
            Text = guid.ToString();
        }

        public Guid(string guid)
        {
            Text = guid;
        }

        [XmlAttribute(AttributeName = "isPermaLink")]
        public bool IsPermaLink { get; set; }

        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "item")]
    public class Item
    {
        [XmlElement(ElementName = "title")]
        public string Title { get; set; }

        [XmlElement(ElementName = "link")]
        public string Link { get; set; }

        [XmlElement(ElementName = "pubDate")]
        public string PubDate { get; set; }

        [XmlElement(ElementName = "description")]
        public string Description { get; set; }

        [XmlElement(ElementName = "enclosure")]
        public Enclosure Enclosure { get; set; }

        [XmlElement(ElementName = "summary", Namespace = "http://www.itunes.com/dtds/podcast-1.0.dtd")]
        public string Summary { get; set; }

        [XmlElement(ElementName = "image", Namespace = "http://www.itunes.com/dtds/podcast-1.0.dtd")]
        public Image Image { get; set; }

        [XmlElement(ElementName = "guid")]
        public Guid Guid { get; set; }
    }

    [XmlRoot(ElementName = "channel")]
    public class Channel
    {
        public Channel()
        {
            Language = "en-us";
            Explicit = "No";
        }

        [XmlElement(ElementName = "title")]
        public string Title { get; set; }

        [XmlElement(ElementName = "link")]
        public string Link { get; set; }

        [XmlElement(ElementName = "description")]
        public string Description { get; set; }

        [XmlElement(ElementName = "language")]
        public string Language { get; set; }

        [XmlElement(ElementName = "copyright")]
        public string Copyright { get; set; }

        [XmlElement(ElementName = "link", Namespace = "http://www.w3.org/2005/Atom")]
        public AtomLink AtomLink { get; set; }

        [XmlElement(ElementName = "lastBuildDate")]
        public string LastBuildDate { get; set; }

        [XmlElement(ElementName = "author", Namespace = "http://www.itunes.com/dtds/podcast-1.0.dtd")]
        public string Author { get; set; }

        [XmlElement(ElementName = "summary", Namespace = "http://www.itunes.com/dtds/podcast-1.0.dtd")]
        public string Summary { get; set; }

        [XmlElement(ElementName = "owner", Namespace = "http://www.itunes.com/dtds/podcast-1.0.dtd")]
        public Owner Owner { get; set; }

        [XmlElement(ElementName = "explicit", Namespace = "http://www.itunes.com/dtds/podcast-1.0.dtd")]
        public string Explicit { get; set; }

        [XmlElement(ElementName = "image", Namespace = "http://www.itunes.com/dtds/podcast-1.0.dtd")]
        public Image Image { get; set; }

        [XmlElement(ElementName = "category", Namespace = "http://www.itunes.com/dtds/podcast-1.0.dtd")]
        public Category Category { get; set; }

        [XmlElement(ElementName = "item")]
        public List<Item> Items { get; set; }
    }

    [XmlRoot(ElementName = "rss")]
    public class Rss
    {
        public Rss()
        {
            Atom = "http://www.w3.org/2005/Atom";
            Itunes = "http://www.itunes.com/dtds/podcast-1.0.dtd";
            Version = "2.0";
        }

        public Rss(Channel channel)
            : this()
        {
            Channel = channel;
        }

        [XmlElement(ElementName = "channel")]
        public Channel Channel { get; set; }

        [XmlAttribute(AttributeName = "atom", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string Atom { get; set; }

        [XmlAttribute(AttributeName = "itunes", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string Itunes { get; set; }

        [XmlAttribute(AttributeName = "version")]
        public string Version { get; set; }
    }
}
