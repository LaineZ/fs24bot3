using System;
using System.Collections.Generic;
using System.Text;

namespace fs24bot3.Models
{
    public class MailErrors
    {
        public enum SearchError
        {
            Banned,
            NotFound,
            UnknownError,
            None,
        }
    }
}
