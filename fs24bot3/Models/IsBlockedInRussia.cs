using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fs24bot3.Models
{
    public class IsBlockedInRussia
    {
        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        public class Blocked
        {
            public string decision_org { get; set; }
            public string decision_num { get; set; }
            public string decision_date { get; set; }
            public List<string> ips { get; set; }
            public List<string> domains { get; set; }
            public List<string> urls { get; set; }
        }

        public class Ip
        {
            public string value { get; set; }
            public List<Blocked> blocked { get; set; }
        }

        public class Domain
        {
            public string value { get; set; }
            public List<object> blocked { get; set; }
        }

        public class Url
        {
            public string value { get; set; }
            public List<object> blocked { get; set; }
        }

        public class Root
        {
            public List<object> blocked { get; set; }
            public List<Ip> ips { get; set; }
            public Domain domain { get; set; }
            public Url url { get; set; }
        }

        public class RequestRoot
        {
            public string host { get; set; }
        }

    }
}
