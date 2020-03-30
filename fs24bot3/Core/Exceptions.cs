using System;
using System.Collections.Generic;
using System.Text;

namespace fs24bot3.Core
{
    public class Exceptions
    {
        public class UserNotFoundException : Exception
        {
            public override string Message
            {
                get
                {
                    return "User not found!";
                }
            }
        }
    }
}
