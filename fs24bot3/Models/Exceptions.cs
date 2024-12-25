using System;
using System.Collections;

namespace fs24bot3.Models;

public class Exceptions
{
    public class UserNotFoundException : Exception
    {
        public override string Message => "Пользователь не найден!";
    }


    public class ItemNotFoundException : Exception
    {
        public override string Message => "Такого предмета не существует в базе данных!";
    }

    public class WrongTypeException : Exception
    {
        public override string Message => "Неверный тип!";
    }

    public class LyricsNotFoundException : Exception
    {
        public override string Message => "Слова не найдены";
    }

    public class SearchError : Exception
    {
        public override string Message => "Произошла ошибка поиска!";
    }

    public class RodError : Exception
    {
        public override string Message => "У вас нет такой удочки!";
    }
}
