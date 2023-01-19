using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XMADownloader.PatreonDownloader.Models
{
    public class Data
    {
        [JsonProperty("attributes", NullValueHandling = NullValueHandling.Ignore)]
        public Attributes Attributes;
    }

    public class Attributes
    {
        [JsonProperty("comment_count", NullValueHandling = NullValueHandling.Ignore)]
        public int CommentCount;

        [JsonProperty("content", NullValueHandling = NullValueHandling.Ignore)]
        public string Content;

        [JsonProperty("created_at", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime CreatedAt;

        [JsonProperty("current_user_can_delete", NullValueHandling = NullValueHandling.Ignore)]
        public bool CurrentUserCanDelete;

        [JsonProperty("current_user_can_view", NullValueHandling = NullValueHandling.Ignore)]
        public bool CurrentUserCanView;

        [JsonProperty("current_user_has_liked", NullValueHandling = NullValueHandling.Ignore)]
        public bool CurrentUserHasLiked;

        [JsonProperty("deleted_at", NullValueHandling = NullValueHandling.Ignore)]
        public object DeletedAt;

        [JsonProperty("edit_url", NullValueHandling = NullValueHandling.Ignore)]
        public string EditUrl;

        [JsonProperty("edited_at", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime EditedAt;

        [JsonProperty("embed", NullValueHandling = NullValueHandling.Ignore)]
        public object Embed;

        [JsonProperty("has_ti_violation", NullValueHandling = NullValueHandling.Ignore)]
        public bool HasTiViolation;

        [JsonProperty("image", NullValueHandling = NullValueHandling.Ignore)]
        public Image Image;

        [JsonProperty("is_automated_monthly_charge", NullValueHandling = NullValueHandling.Ignore)]
        public bool IsAutomatedMonthlyCharge;

        [JsonProperty("is_paid", NullValueHandling = NullValueHandling.Ignore)]
        public bool IsPaid;

        [JsonProperty("like_count", NullValueHandling = NullValueHandling.Ignore)]
        public int LikeCount;

        [JsonProperty("min_cents_pledged_to_view", NullValueHandling = NullValueHandling.Ignore)]
        public int MinCentsPledgedToView;

        [JsonProperty("moderation_status", NullValueHandling = NullValueHandling.Ignore)]
        public string ModerationStatus;

        [JsonProperty("patreon_url", NullValueHandling = NullValueHandling.Ignore)]
        public string PatreonUrl;

        [JsonProperty("pledge_url", NullValueHandling = NullValueHandling.Ignore)]
        public string PledgeUrl;

        [JsonProperty("post_file", NullValueHandling = NullValueHandling.Ignore)]
        public PostFile PostFile;

        [JsonProperty("post_level_suspension_removal_date", NullValueHandling = NullValueHandling.Ignore)]
        public object PostLevelSuspensionRemovalDate;

        [JsonProperty("post_type", NullValueHandling = NullValueHandling.Ignore)]
        public string PostType;

        [JsonProperty("preview_asset_type", NullValueHandling = NullValueHandling.Ignore)]
        public object PreviewAssetType;

        [JsonProperty("published_at", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime PublishedAt;

        [JsonProperty("scheduled_for", NullValueHandling = NullValueHandling.Ignore)]
        public object ScheduledFor;

        [JsonProperty("teaser_text", NullValueHandling = NullValueHandling.Ignore)]
        public string TeaserText;

        [JsonProperty("thumbnail", NullValueHandling = NullValueHandling.Ignore)]
        public Thumbnail Thumbnail;

        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        public string Title;

        [JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
        public string Url;

        [JsonProperty("video_preview", NullValueHandling = NullValueHandling.Ignore)]
        public object VideoPreview;

        [JsonProperty("was_posted_by_campaign_owner", NullValueHandling = NullValueHandling.Ignore)]
        public bool WasPostedByCampaignOwner;
    }

    public class Image
    {
        [JsonProperty("height", NullValueHandling = NullValueHandling.Ignore)]
        public int Height;

        [JsonProperty("large_url", NullValueHandling = NullValueHandling.Ignore)]
        public string LargeUrl;

        [JsonProperty("thumb_square_large_url", NullValueHandling = NullValueHandling.Ignore)]
        public string ThumbSquareLargeUrl;

        [JsonProperty("thumb_square_url", NullValueHandling = NullValueHandling.Ignore)]
        public string ThumbSquareUrl;

        [JsonProperty("thumb_url", NullValueHandling = NullValueHandling.Ignore)]
        public string ThumbUrl;

        [JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
        public string Url;

        [JsonProperty("width", NullValueHandling = NullValueHandling.Ignore)]
        public int Width;
    }

    public class PostFile
    {
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name;

        [JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
        public string Url;
    }

    public class PatreonPostRoot
    {
        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
        public Data Data;

        [JsonProperty("included", NullValueHandling = NullValueHandling.Ignore)]
        public List<Included> Included;
    }

    public class Thumbnail
    {
        [JsonProperty("large", NullValueHandling = NullValueHandling.Ignore)]
        public string Large;

        [JsonProperty("large_2", NullValueHandling = NullValueHandling.Ignore)]
        public string Large2;

        [JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
        public string Url;
    }

    public class Included
    {
        [JsonProperty("attributes", NullValueHandling = NullValueHandling.Ignore)]
        public IncludedAttributes Attributes;

        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id;

        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type;
    }

    public class IncludedAttributes
    {
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name;

        [JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
        public string Url;
    }
}
