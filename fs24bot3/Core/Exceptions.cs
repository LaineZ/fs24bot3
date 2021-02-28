using System;

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
                    return "Пользователь не найден!";
                }
            }
        }


        public class TypeNotFoundException : Exception
        {
            public override string Message
            {
                get
                {
                    return "Предмет с данным типом не найден!";
                }
            }
        }

        public class SearchError : Exception
        {
            public override string Message
            {
                get
                {
                    return "Произошла ошибка поиска!";
                }
            }
        }

        public class RodError : Exception
        {
            public override string Message
            {
                get
                {
                    return "У вас нет такой удочки!";
                }
            }
        }
    }
}
