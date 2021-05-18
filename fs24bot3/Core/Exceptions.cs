﻿using System;
using System.Collections;

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

        public class WrongTypeException : Exception
        {
            public override string Message
            {
                get
                {
                    return "Неверный тип!";
                }
            }
        }

        public class LyricsNotFoundException : Exception
        {
            public override string Message
            {
                get
                {
                    return "Слова не найдены";
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
