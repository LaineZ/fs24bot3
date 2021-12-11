# fs24_bot3 aka Sopli IRC 3.0

IRC бот, разработан для использования на IRC канале сайта computercraft.ru. Бот обладает широким набором функций

# Установка

Чтобы установить данную жесть себе на комп необходимо использовать .NET Core 3/1 комплиятор. Инструкцию по его установке можно найти [здесь](https://docs.microsoft.com/en-us/dotnet/core/install/)

Если у вас все установлено запускаем необходимые скрипты для сборки:

``./fs24bot3/build-linux.bat``, ``./fs24bot3/build-linux-shared.sh`` - собирает self-contained бинарь для linux x64

``./fs24bot3/build-linux-shared.bat`` - собирает .NET бинарь для linux x64, для его работы необходим пакет ``dotnet-runtime``

``./fs24bot3/build-linux-shared-dbg.bat`` - собирает дебаг версию .NET бинари для linux x64, для его работы необходим пакет ``dotnet-runtime``

Результат сборки будет в папке ``./fs24bot3/bin/``
