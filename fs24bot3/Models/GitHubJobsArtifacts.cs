using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace fs24bot3.Models;

public class GitHubJobsArtifacts
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Artifact
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("node_id")]
        public string NodeId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("size_in_bytes")]
        public int SizeInBytes { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("archive_download_url")]
        public string ArchiveDownloadUrl { get; set; }

        [JsonProperty("expired")]
        public bool Expired { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonProperty("expires_at")]
        public DateTime ExpiresAt { get; set; }

        [JsonProperty("workflow_run")]
        public WorkflowRun WorkflowRun { get; set; }
    }

    public class Root
    {
        [JsonProperty("total_count")]
        public int TotalCount { get; set; }

        [JsonProperty("artifacts")]
        public List<Artifact> Artifacts { get; set; }
    }

    public class WorkflowRun
    {
        [JsonProperty("id")]
        public object Id { get; set; }

        [JsonProperty("repository_id")]
        public int RepositoryId { get; set; }

        [JsonProperty("head_repository_id")]
        public int HeadRepositoryId { get; set; }

        [JsonProperty("head_branch")]
        public string HeadBranch { get; set; }

        [JsonProperty("head_sha")]
        public string HeadSha { get; set; }
    }
}