using System;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace Overbook
{
    [XmlRoot(ElementName = "link", Namespace = "http://www.w3.org/2005/Atom")]
    public class Link
    {
        [XmlAttribute(AttributeName = "href")]
        public string Href { get; set; }

        [XmlAttribute(AttributeName = "rel")]
        public string Rel { get; set; }

        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }
    }

    [XmlRoot(ElementName = "owner", Namespace = "http://www.itunes.com/dtds/podcast-1.0.dtd")]
    public class Owner
    {
        [XmlElement(ElementName = "name", Namespace = "http://www.itunes.com/dtds/podcast-1.0.dtd")]
        public string Name { get; set; }

        [XmlElement(ElementName = "email", Namespace = "http://www.itunes.com/dtds/podcast-1.0.dtd")]
        public string Email { get; set; }
    }

    [XmlRoot(ElementName = "image", Namespace = "http://www.itunes.com/dtds/podcast-1.0.dtd")]
    public class Image
    {
        [XmlAttribute(AttributeName = "href")]
        public string Href { get; set; }
    }

    [XmlRoot(ElementName = "link", Namespace = "http://www.w3.org/2005/Atom")]
    public class AtomLink
    {
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
        [XmlAttribute(AttributeName = "text")]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "enclosure")]
    public class Enclosure
    {
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
