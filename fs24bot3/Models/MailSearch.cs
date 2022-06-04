using System.Collections.Generic;

namespace fs24bot3.Models
{
    public class MailSearch
    {
        public class Common
        {
            public string q { get; set; }
        }

        public class Sticky
        {
        }

        public class Params
        {
            public Common common { get; set; }
            public Sticky sticky { get; set; }
        }

        public class Antirobot
        {
            public string qid { get; set; }
            public string message { get; set; }
            public bool blocked { get; set; }
        }

        public class Alq
        {
            public string query { get; set; }
            public string hl_query { get; set; }
        }

        public class MicrodataVideo
        {
            public string publish_date { get; set; }
            public string author { get; set; }
            public string category { get; set; }
            public int duration_time { get; set; }
            public int views_count { get; set; }
            public int width { get; set; }
            public int height { get; set; }
            public int type { get; set; }
        }

        public class Sitelink
        {
            public string title { get; set; }
            public string url { get; set; }
            public string snippet { get; set; }
        }

        public class Address
        {
            public string address { get; set; }
            public string phone { get; set; }
            public double longitude { get; set; }
            public double latitude { get; set; }
            public string map_url { get; set; }
            public List<string> schedule_list { get; set; }
            public string schedule { get; set; }
        }

        public class Image
        {
            public int width { get; set; }
            public int height { get; set; }
            public string url { get; set; }
            public string orig_url { get; set; }
            public int size { get; set; }
            public string redir_url { get; set; }
            public string ext { get; set; }
        }

        public class Thumb
        {
            public int width { get; set; }
            public int height { get; set; }
            public string url { get; set; }
        }

        public class Player
        {
            public string url { get; set; }
            public string type { get; set; }
            public int width { get; set; }
            public int height { get; set; }
        }

        public class Preview
        {
            public string url { get; set; }
            public int width { get; set; }
            public int height { get; set; }
            public string local_url { get; set; }
            public string vague_local_url { get; set; }
        }

        public class Result2
        {
            public int number { get; set; }
            public string page_url { get; set; }
            public string urlhash { get; set; }
            public Image image { get; set; }
            public string urls_sign { get; set; }
            public Thumb thumb { get; set; }
            public string redir_page_url { get; set; }
            public string content { get; set; }
            public string id { get; set; }
            public string description { get; set; }
            public int? duration { get; set; }
            public int? time { get; set; }
            public string url { get; set; }
            public string sig { get; set; }
            public bool? hd { get; set; }
            public bool? full_hd { get; set; }
            public string name { get; set; }
            public string source_name { get; set; }
            public Player player { get; set; }
            public int? source_id { get; set; }
            public Preview preview { get; set; }
        }

        public class Image2
        {
            public string url { get; set; }
            public string local_url { get; set; }
            public int height { get; set; }
            public int width { get; set; }
        }

        public class Preview2
        {
            public string url { get; set; }
            public string local_url { get; set; }
            public int height { get; set; }
            public int width { get; set; }
        }

        public class Link
        {
            public string title { get; set; }
            public string passage { get; set; }
            public string url { get; set; }
            public Preview2 preview { get; set; }
        }

        public class Carousel
        {
            public string author { get; set; }
            public int published { get; set; }
            public string src { get; set; }
            public string passage { get; set; }
            public string url { get; set; }
            public Image2 image { get; set; }
            public int number { get; set; }
            public Link link { get; set; }
        }

        public class Rating
        {
            public int stars { get; set; }
            public int count { get; set; }
        }

        public class Breadcrumb
        {
            public string title { get; set; }
            public string url { get; set; }
        }

        public class Result
        {
            public int number { get; set; }
            public string doc_id { get; set; }
            public string srch_id { get; set; }
            public bool is_navig { get; set; }
            public bool is_porno { get; set; }
            public string orig_url { get; set; }
            public string hl_url { get; set; }
            public string saved_url { get; set; }
            public string title { get; set; }
            public string title_source { get; set; }
            public string passage { get; set; }
            public string passage_source { get; set; }
            public string favicon { get; set; }
            public string favicon_hr { get; set; }
            public MicrodataVideo microdata_video { get; set; }
            public string url { get; set; }
            public string redir_url { get; set; }
            public string snip_type { get; set; }
            public List<Sitelink> sitelinks { get; set; }
            public string sitelinks_type { get; set; }
            public List<Address> address { get; set; }
            public string smack_type { get; set; }
            public List<Result2> results { get; set; }
            public bool? rich { get; set; }
            public int? total { get; set; }
            public List<Carousel> carousel { get; set; }
            public Rating rating { get; set; }
            public List<Breadcrumb> breadcrumbs { get; set; }
        }

        public class Serp
        {
            public int count { get; set; }
            public int count_show { get; set; }
            public List<Result> results { get; set; }
        }

        public class SideSerp
        {
            public List<object> results { get; set; }
        }

        public class Title
        {
            public bool hl { get; set; }
            public string text { get; set; }
        }

        public class Text
        {
            public string text { get; set; }
        }

        public class Hldomain
        {
            public string text { get; set; }
        }

        public class Title2
        {
            public string text { get; set; }
        }

        public class Sitelink2
        {
            public List<Title2> title { get; set; }
            public string url { get; set; }
            public string redir_url { get; set; }
            public string no_redirect_url { get; set; }
        }

        public class Path
        {
            public string text { get; set; }
        }

        public class Bottom
        {
            public string url { get; set; }
            public string domain { get; set; }
            public string idna_domain { get; set; }
            public List<Title> title { get; set; }
            public List<Text> text { get; set; }
            public List<Hldomain> hldomain { get; set; }
            public List<Sitelink2> sitelinks { get; set; }
            public string fav_domain { get; set; }
            public string no_redirect_url { get; set; }
            public List<Path> path { get; set; }
            public string path_url { get; set; }
            public string redir_url { get; set; }
            public string redir_path_url { get; set; }
        }

        public class YaDirect
        {
            public string pixel { get; set; }
            public List<Bottom> bottom { get; set; }
        }

        public class Page
        {
            public int number { get; set; }
            public int scroll { get; set; }
            public bool current { get; set; }
        }

        public class Pager
        {
            public int current { get; set; }
            public int current_page { get; set; }
            public int total { get; set; }
            public int total_pages { get; set; }
            public int per_page { get; set; }
            public int next { get; set; }
            public int next_page { get; set; }
            public int volume_pages { get; set; }
            public List<Page> pages { get; set; }
            public int next_volume { get; set; }
        }

        public class Current
        {
            public int id { get; set; }
            public string name { get; set; }
            public string name_locative { get; set; }
        }

        public class Geo
        {
            public Current current { get; set; }
        }

        public class RootObject
        {
            public Params @params { get; set; }
            public int localtime { get; set; }
            public Antirobot antirobot { get; set; }
            public string auto_query { get; set; }
            public List<Alq> alqs { get; set; }
            public Serp serp { get; set; }
            public SideSerp side_serp { get; set; }
            public YaDirect ya_direct { get; set; }
            public Pager pager { get; set; }
            public Geo geo { get; set; }
            public string rd_template { get; set; }
        }
    }
}
