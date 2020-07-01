using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace fs24bot3.Models
{
    public class Commits
    {
        public partial class Commit
        {
            [JsonProperty("url")]
            public Uri Url { get; set; }

            [JsonProperty("sha")]
            public string Sha { get; set; }

            [JsonProperty("html_url")]
            public Uri HtmlUrl { get; set; }

            [JsonProperty("commit")]
            public CommitClass CommitCommit { get; set; }

            [JsonProperty("author")]
            public object Author { get; set; }

            [JsonProperty("committer")]
            public object Committer { get; set; }

            [JsonProperty("parents")]
            public Tree[] Parents { get; set; }
        }

        public partial class CommitClass
        {
            [JsonProperty("url")]
            public Uri Url { get; set; }

            [JsonProperty("author")]
            public Author Author { get; set; }

            [JsonProperty("committer")]
            public Author Committer { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }

            [JsonProperty("tree")]
            public Tree Tree { get; set; }
        }

        public partial class Author
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("email")]
            public string Email { get; set; }

            [JsonProperty("date")]
            public DateTimeOffset Date { get; set; }
        }

        public partial class Tree
        {
            [JsonProperty("url")]
            public Uri Url { get; set; }

            [JsonProperty("sha")]
            public string Sha { get; set; }
        }
    }
}
