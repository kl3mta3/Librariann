using System.Xml.Serialization;

namespace Librariann.Models.DTOs.OPDS;

public sealed record FeedAuthor
{
    [XmlElement("name")]
    public string Name { get; set; }
    [XmlElement("uri")]
    public string Uri { get; set; }
}
