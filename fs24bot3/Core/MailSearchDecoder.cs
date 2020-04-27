using fs24bot3.Models;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace fs24bot3.Core
{
    public static class MailSearchDecoder
    {
        public static MailSearch.RootObject PerformDecode(string code)
        {
            string startString = "go.dataJson = {";
            string stopString = "};";

            string searchDataTemp = code.Substring(code.IndexOf(startString) + startString.Length - 1);
            string searchData = searchDataTemp.Substring(0, searchDataTemp.IndexOf(stopString) + 1);

            var searchResults = new List<MailSearch.Result>();

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            try
            {
                return JsonConvert.DeserializeObject<MailSearch.RootObject>(searchData, settings);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
