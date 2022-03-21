using fs24bot3.BotSystems;
using fs24bot3.Commands;
using fs24bot3.Core;
using fs24bot3.Helpers;
using fs24bot3.Models;
using fs24bot3.QmmandsProcessors;
using NetIRC;
using NetIRC.Connection;
using NetIRC.Messages;
using Qmmands;
using Serilog;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace fs24bot3
{
    public class Bot
    {

        public SQLiteConnection Connection = new SQLiteConnection("fsdb.sqlite");
        public CustomCommandProcessor CustomCommandProcessor;
        public readonly CommandService Service = new CommandService();
        public Client BotClient { get; private set; }
        public Shop Shop { get; set; }
        public Songame SongGame { get; set; }

        public int Tickrate = 15000;

        public string Name { get; private set; }

        public Bot()
        {
            Service.AddModule<GenericCommandsModule>();
            Service.AddModule<SystemCommandModule>();
            Service.AddModule<InventoryCommandsModule>();
            Service.AddModule<InternetCommandsModule>();
            Service.AddModule<NetstalkingCommandsModule>();
            Service.AddModule<CustomCommandsModule>();
            Service.AddModule<StatCommandModule>();
            Service.AddModule<BandcampCommandsModule>();
            Service.AddModule<TranslateCommandModule>();
            Service.AddModule<FishCommandsModule>();

            Database.InitDatabase(Connection);
            BotClient = new Client(new NetIRC.User(ConfigurationProvider.Config.Name, "Sopli IRC 3.0"), new TcpClientConnection(ConfigurationProvider.Config.Network, ConfigurationProvider.Config.Port));
            CustomCommandProcessor = new CustomCommandProcessor(this);
            Name = ConfigurationProvider.Config.Name;

            // check for custom commands used in a bot
            Log.Information("Checking for user commands with incorrect names");
            foreach (var command in Connection.Table<SQL.CustomUserCommands>())
            {
                bool commandIntenral = Service.GetAllCommands().Any(x => x.Aliases.Any(a => a == command.Command));

                if (commandIntenral)
                {
                    var user = new Core.User(command.Nick, Connection);
                    Log.Warning("User {0} have a command with internal name {1}!", user.Username, command.Command);
                    user.AddWarning($"Вы регистрировали команду {user.GetUserPrefix()}{command.Command}, в новой версии fs24bot добавилась команда с таким же именем, ВАША КАСТОМ-КОМАНДА БОЛЬШЕ НЕ БУДЕТ РАБОТАТЬ! Чтобы вернуть деньги за команду используйте {user.GetUserPrefix()}delcmd {command.Command}. И создайте команду с другим именем");
                }
            }

            new Thread(async () =>
            {
                Log.Information("Reminds thread started!");
                while (true)
                {
                    Thread.Sleep(1000);
                    var query = Connection.Table<SQL.Reminds>();

                    foreach (var item in query)
                    {
                        DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                        dtDateTime = dtDateTime.AddSeconds(item.RemindDate).ToLocalTime();
                        if (dtDateTime <= DateTime.Now)
                        {
                            string ch = item.Channel ?? ConfigurationProvider.Config.Channel;
                            await SendMessage(ch, $"{item.Nick}: {item.Message}!");
                            Connection.Delete(item);
                        }
                    }
                }
            }).Start();
            Log.Information("Bot: Construction complete!");
        }

        public async void SetupNick(string nickname)
        {
            await BotClient.SendRaw("NICK " + nickname);
            Name = nickname;
        }
        public string CommandSuggestion(string prefix, string command)
        {
            var totalCommands = new List<string>();

            foreach (var cmd in Service.GetAllCommands())
            {
                totalCommands.AddRange(cmd.Aliases.Select(x => prefix + x));
            }

            foreach (var cmd in Connection.Table<SQL.CustomUserCommands>())
            {
                totalCommands.Add(prefix + cmd.Command);
            }

            return string.Join(" ", totalCommands.SkipWhile(x => MessageHelper.LevenshteinDistance(command, x) >= 10)
                .OrderBy(i => MessageHelper.LevenshteinDistance(command, i))
                .Take(5));
        }

        public void ProccessInfinite()
        {
            // start shop
            Shop = new Shop(this);
            SongGame = new Songame(Connection);

            while (true)
            {
                Thread.Sleep(Tickrate);
                var query = Connection.Table<SQL.UserStats>();
                foreach (var users in query)
                {
                    var onTick = new EventProcessors.OnTick(users.Nick, Connection);
                    onTick.UpdateUserPaydays(Shop);
                    onTick.RemoveLevelOneAccs();
                }
                Shop.UpdateShop();
            }
        }

        public void MessageTrigger(string nick, string target, ParsedIRCMessage message)
        {
            var queryIfExt = Connection.Table<SQL.Ignore>().Where(v => v.Username.Equals(nick)).Count();
            if (queryIfExt <= 0)
            {
                new Thread(() =>
                {
                    if (target != BotClient.User.Nick)
                    {
                        EventProcessors.OnMsgEvent events = new EventProcessors.OnMsgEvent(BotClient, nick, target, message.Trailing.Trim(), Connection);
                        events.DestroyWallRandomly(Shop);
                        events.LevelInscrease(Shop);
                    }
                }).Start();
            }
        }

        public async Task SendMessage(string channel, string message)
        {
            List<string> msgLines = message.Split("\n").Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            int count = 0;

            foreach (string outputstr in msgLines)
            {
                if (outputstr.Length < 1000)
                {
                    await BotClient.SendAsync(new PrivMsgMessage(channel, outputstr));
                    count++;
                }
                else
                {
                    await BotClient.SendAsync(new PrivMsgMessage(channel, $"Слишком жесткое сообщение с длинной {outputstr.Length} символов! Психанул?!?!?!"));
                    return;
                }

                if (count > 4)
                {
                    string link = await InternetServicesHelper.UploadToTrashbin(MessageHelper.StripIRC(message), "addplain");
                    await BotClient.SendAsync(new PrivMsgMessage(channel, "Полный вывод здесь: " + link));
                    return;
                }
            }
        }

        public async Task ExecuteCommand(string nick, string target, string messageString, ParsedIRCMessage message, string prefix, bool ppc = false)
        {
            var prefixes = new string[] { prefix, Name + ":" };
            if (!CommandUtilities.HasAnyPrefix(messageString.TrimStart('p'), prefixes, out string pfx, out string output))
                return;

            var result = await Service.ExecuteAsync(output, new CommandProcessor.CustomCommandContext(target, message, this, ppc));

            if (!result.IsSuccessful && ppc)
            {
                await SendMessage(target, $"{nick}: НЕДОПУСТИМАЯ ОПЕРАЦИЯ");
                new Core.User(nick, Connection).AddItemToInv(Shop, "beer", 1);
            }

            switch (result)
            {
                case ChecksFailedResult err:
                    await SendMessage(target, $"Требования не выполнены: {string.Join(" ", err.FailedChecks)}");
                    break;
                case TypeParseFailedResult err:
                    await SendMessage(target, $"Ошибка в `{err.Parameter}` необходимый тип `{err.Parameter.Type.Name}` вы же ввели `{err.Value.GetType().Name}` введите #helpcmd {err.Parameter.Command} чтобы узнать как правильно пользоватся этой командой");
                    break;
                case ArgumentParseFailedResult err:
                    var parserResult = err.ParserResult as DefaultArgumentParserResult;

                    switch (parserResult.Failure)
                    {
                        case DefaultArgumentParserFailure.NoWhitespaceBetweenArguments:
                            await SendMessage(target, $"Нет пробелов между аргументами!");
                            break;
                        case DefaultArgumentParserFailure.TooManyArguments:
                            await SendMessage(target, $"Слишком много аргрументов!!!");
                            break;
                        default:
                            await SendMessage(target, $"Ошибка парсера: `{err.ParserResult.FailureReason}`");
                            break;
                    }

                    break;
                case OverloadsFailedResult:
                    await SendMessage(target, "Команда выключена...");
                    break;
                case CommandNotFoundResult _:
                    if (!CustomCommandProcessor.ProcessCmd(prefix, nick, target, messageString))
                    {
                        string cmdName = messageString.Split(" ")[0];
                        var cmds = CommandSuggestion(prefix, cmdName);
                        if (!string.IsNullOrWhiteSpace(cmds))
                        {
                            await SendMessage(target, $"Команда {IrcClrs.Bold}{cmdName}{IrcClrs.Reset} не найдена, возможно вы хотели написать: {IrcClrs.Bold}{cmds}");
                        }
                    }
                    break;
                case CommandExecutionFailedResult err:
                    await SendMessage(target, $"{IrcClrs.Red}Ошибка: {err.Exception.GetType().Name}: {err.Exception.Message}");
                    Connection.Insert(new SQL.UnhandledExceptions(err.Exception.Message + ": " + err.Exception.StackTrace, nick, message.Trailing.TrimEnd()));
                    break;
            }
        }
    }
}
