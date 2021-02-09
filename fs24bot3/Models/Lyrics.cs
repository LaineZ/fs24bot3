using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace fs24bot3.Models
{
    public partial class Lyrics
    {
        [JsonProperty("meta", NullValueHandling = NullValueHandling.Ignore)]
        public Meta Meta { get; set; }

        [JsonProperty("response", NullValueHandling = NullValueHandling.Ignore)]
        public Response Response { get; set; }
    }

    public partial class Meta
    {
        [JsonProperty("status", NullValueHandling = NullValueHandling.Ignore)]
        public long? Status { get; set; }
    }

    public partial class Response
    {
        [JsonProperty("sections", NullValueHandling = NullValueHandling.Ignore)]
        public Section[] Sections { get; set; }

        [JsonProperty("next_page", NullValueHandling = NullValueHandling.Ignore)]
        public long? NextPage { get; set; }
    }

    public partial class Section
    {
        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }

        [JsonProperty("hits", NullValueHandling = NullValueHandling.Ignore)]
        public Hit[] Hits { get; set; }
    }

    public partial class Hit
    {
        [JsonProperty("highlights", NullValueHandling = NullValueHandling.Ignore)]
        public object[] Highlights { get; set; }

        [JsonProperty("index", NullValueHandling = NullValueHandling.Ignore)]
        public string Index { get; set; }

        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }

        [JsonProperty("result", NullValueHandling = NullValueHandling.Ignore)]
        public Result Result { get; set; }
    }

    public partial class Result
    {
        [JsonProperty("_type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }

        [JsonProperty("annotation_count", NullValueHandling = NullValueHandling.Ignore)]
        public long? AnnotationCount { get; set; }

        [JsonProperty("api_path", NullValueHandling = NullValueHandling.Ignore)]
        public string ApiPath { get; set; }

        [JsonProperty("full_title", NullValueHandling = NullValueHandling.Ignore)]
        public string FullTitle { get; set; }

        [JsonProperty("header_image_thumbnail_url", NullValueHandling = NullValueHandling.Ignore)]
        public Uri HeaderImageThumbnailUrl { get; set; }

        [JsonProperty("header_image_url", NullValueHandling = NullValueHandling.Ignore)]
        public Uri HeaderImageUrl { get; set; }

        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public long? Id { get; set; }

        [JsonProperty("instrumental", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Instrumental { get; set; }

        [JsonProperty("lyrics_owner_id", NullValueHandling = NullValueHandling.Ignore)]
        public long? LyricsOwnerId { get; set; }

        [JsonProperty("lyrics_state", NullValueHandling = NullValueHandling.Ignore)]
        public string LyricsState { get; set; }

        [JsonProperty("lyrics_updated_at", NullValueHandling = NullValueHandling.Ignore)]
        public long? LyricsUpdatedAt { get; set; }

        [JsonProperty("path", NullValueHandling = NullValueHandling.Ignore)]
        public string Path { get; set; }

        [JsonProperty("pyongs_count")]
        public long? PyongsCount { get; set; }

        [JsonProperty("song_art_image_thumbnail_url", NullValueHandling = NullValueHandling.Ignore)]
        public Uri SongArtImageThumbnailUrl { get; set; }

        [JsonProperty("song_art_image_url", NullValueHandling = NullValueHandling.Ignore)]
        public Uri SongArtImageUrl { get; set; }

        [JsonProperty("stats", NullValueHandling = NullValueHandling.Ignore)]
        public Stats Stats { get; set; }

        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        public string Title { get; set; }

        [JsonProperty("title_with_featured", NullValueHandling = NullValueHandling.Ignore)]
        public string TitleWithFeatured { get; set; }

        [JsonProperty("updated_by_human_at", NullValueHandling = NullValueHandling.Ignore)]
        public long? UpdatedByHumanAt { get; set; }

        [JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
        public Uri Url { get; set; }

        [JsonProperty("primary_artist", NullValueHandling = NullValueHandling.Ignore)]
        public PrimaryArtist PrimaryArtist { get; set; }
    }

    public partial class PrimaryArtist
    {
        [JsonProperty("_type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }

        [JsonProperty("api_path", NullValueHandling = NullValueHandling.Ignore)]
        public string ApiPath { get; set; }

        [JsonProperty("header_image_url", NullValueHandling = NullValueHandling.Ignore)]
        public Uri HeaderImageUrl { get; set; }

        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public long? Id { get; set; }

        [JsonProperty("image_url", NullValueHandling = NullValueHandling.Ignore)]
        public Uri ImageUrl { get; set; }

        [JsonProperty("index_character", NullValueHandling = NullValueHandling.Ignore)]
        public string IndexCharacter { get; set; }

        [JsonProperty("is_meme_verified", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsMemeVerified { get; set; }

        [JsonProperty("is_verified", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsVerified { get; set; }

        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty("slug", NullValueHandling = NullValueHandling.Ignore)]
        public string Slug { get; set; }

        [JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
        public Uri Url { get; set; }
    }

    public partial class Stats
    {
        [JsonProperty("unreviewed_annotations", NullValueHandling = NullValueHandling.Ignore)]
        public long? UnreviewedAnnotations { get; set; }

        [JsonProperty("concurrents", NullValueHandling = NullValueHandling.Ignore)]
        public long? Concurrents { get; set; }

        [JsonProperty("hot", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Hot { get; set; }

        [JsonProperty("pageviews", NullValueHandling = NullValueHandling.Ignore)]
        public long? Pageviews { get; set; }
    }
}
