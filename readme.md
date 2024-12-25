# fs24_bot3 aka Sopli IRC 3.0

IRC бот разработан для использования на IRC канале сайта computercraft.ru. Он обладает широким набором функций: подсчет статистики, генерацию случайных чисел и многое другое. Документация легко доступна внутри бота по команде ``#help``.

# Установка

Чтобы установить данную жесть себе на комп необходимо использовать .NET 6.0 комплиятор. Инструкцию по его установке можно найти [здесь](https://docs.microsoft.com/en-us/dotnet/core/install/)

Для тестирования достаточно запустить команду ``dotnet run`` которая все сама соберет, и запустит

Если у вас все установлено, можете запустить скрипты для сборки:

``./fs24bot3/build-linux.bat`` - собирает self-contained бинарь для linux x64

``./fs24bot3/build-linux-shared.bat``,  ``./fs24bot3/build-linux-shared.sh`` - собирает .NET бинарь для linux x64, для его работы необходим пакет ``dotnet-runtime``

Результат сборки будет в папке ``./fs24bot3/releases``

Далее просто запускаем бота и ждем начальной инициализации... Далее вы можете покопаться по конфигам и настроить всё по своему вкусу...