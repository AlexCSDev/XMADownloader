using System;
using XMADownloader.Implementation.Enums;
using UniversalDownloaderPlatform.DefaultImplementations.Models;

namespace XMADownloader.Implementation.Models
{
    public class XmaCrawledUrl : CrawledUrl
    {
        public long UserId { get; set; }
        public string ModId { get; set; }
        public string Name { get; set; }
        public DateTime PublishedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        /*public XmaCrawledUrlType UrlType { get; set; }

        public string UrlTypeAsFriendlyString
        {
            get
            {
                switch (UrlType)
                {
                    case XmaCrawledUrlType.Unknown:
                        return "Unknown";
                    case XmaCrawledUrlType.PostFile:
                        return "File";
                    case XmaCrawledUrlType.PostAttachment:
                        return "Attachment";
                    case XmaCrawledUrlType.PostMedia:
                        return "Media";
                    case XmaCrawledUrlType.ExternalUrl:
                        return "External Url";
                    case XmaCrawledUrlType.CoverFile:
                        return "Cover";
                    case XmaCrawledUrlType.AvatarFile:
                        return "Avatar";
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }*/

        public object Clone()
        {
            return new XmaCrawledUrl
            {
                ModId = ModId,
                Url = Url,
                Filename = Filename,
                //UrlType = UrlType, 
                Name = Name,
                PublishedAt = PublishedAt,
                UpdatedAt = UpdatedAt,
                UserId = UserId
            };
        }
    }
}
