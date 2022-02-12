using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Tomlyn;

namespace fs24bot3.Models
{
    public class Configuration
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "network")]
        public string Network { get; set; }
        [DataMember(Name = "channel")]
        public string Channel { get; set; }
        [DataMember(Name = "port")]
        public int Port { get; set; }
        [DataMember(Name = "nickserv_pass")]
        public string NickservPass { get; set; }
        [DataMember(Name = "server_pass")]
        public string ServerPassword { get; set; }
        [DataMember(Name = "prefix")]
        public string Prefix { get; set; }
        [DataMember(Name = "loglevel")]
        public string LogLevel { get; set; }
        [DataMember(Name = "jdoodle_client_id")]
        public string JdoodleClientID { get; set; }
        [DataMember(Name = "jdoodle_client_secret")]
        public string JdoodleClientSecret { get; set; }
        [DataMember(Name = "trashbin_url")]
        public string TrashbinUrl { get; set; }
        [DataMember(Name = "wolfram_id")]
        public string WolframID { get; set; }
        [DataMember(Name = "translate_key")]
        public string TranslateKey { get; set; }

        public Configuration()
        {
            Name = "fs24bot3";
            Network = "irc.esper.net";
            Channel = "#fl-studio";
            Port = 6667;
            NickservPass = "zxcvbm1";
            ServerPassword = "zxcvbm1";
            Prefix = "#";
            LogLevel = "Verbose";
            JdoodleClientID = "0";
            JdoodleClientSecret = "0";
            TrashbinUrl = "http://trashbin.140.ted.ge";
            WolframID = "0";
            TranslateKey = "0";
        }
    }
}
