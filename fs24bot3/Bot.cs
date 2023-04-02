using fs24bot3.Systems;
using fs24bot3.Commands;
using fs24bot3.Core;
using fs24bot3.Helpers;
using fs24bot3.Models;
using fs24bot3.QmmandsProcessors;
using Qmmands;
using Serilog;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using fs24bot3.Backend;
using fs24bot3.EventProcessors;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace fs24bot3;

public class Bot
{
    public SQLiteConnection Connection = new SQLiteConnection("fsdb.sqlite");
    private CustomCommandProcessor CustomCommandProcessor;
    private readonly CommandService Service = new CommandService();
    public IMessagingClient Client { get; }
    public Shop Shop { get; }
    public Songame SongGame { get; set; }
    public List<string> AcknownUsers = new List<string>();
    public Profiler PProfiler { get; }
    
    private OnMsgEvent OnMsgEvent { get; }

    public Bot(IMessagingClient messagingClient)
    {
        Client = messagingClient;
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
        Service.AddTypeParser(new Parsers.LanugageParser());

        Database.InitDatabase(Connection);
        CustomCommandProcessor = new CustomCommandProcessor(this);
        OnMsgEvent = new OnMsgEvent(this);
        PProfiler = new Profiler();

        PProfiler.AddMetric("update");
        PProfiler.AddMetric("command");
        PProfiler.AddMetric("msg");

        // check for custom commands used in a bot
        Log.Information("Checking for user commands with incorrect names");
        foreach (var command in Connection.Table<SQL.CustomUserCommands>())
        {
            bool commandIntenral = Service.GetAllCommands()
                .Any(x => x.Aliases.Any(a => a == command.Command));

            if (!commandIntenral) continue;
            
            var user = new User(command.Nick, Connection);
            Log.Warning("User {0} have a command with internal name {1}!",
                user.Username,
                command.Command);
            user.AddWarning(
                $"Вы регистрировали команду {user.GetUserPrefix()}{command.Command}, в новой версии fs24bot " +
                $"добавилась команда с таким же именем, " +
                $"ВАША КАСТОМ-КОМАНДА БОЛЬШЕ НЕ БУДЕТ РАБОТАТЬ! Чтобы вернуть деньги за команду используйте " +
                $"{user.GetUserPrefix()}delcmd {command.Command}. И создайте команду с другим именем",
                this);
        }

        Shop = new Shop(this);
        SongGame = new Songame(Connection);

        Log.Information("Bot: Construction complete!");
    }

    private string CommandSuggestion(string prefix, string command)
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
            .Take(3));
    }

    public async void ProccessInfinite()
    {
        Log.Verbose("Processing started...");
        while (true)
        {
            Thread.Sleep(1000);
            PProfiler.BeginMeasure("update");
            var users = Connection.Table<SQL.UserStats>();
            foreach (var user in users)
            {
                var onTick = new OnTick(user.Nick, Connection);
                onTick.UpdateUserPaydays(Shop);
                //onTick.RemoveLevelOneAccs();
            }

            var reminds = Connection.Table<SQL.Reminds>();
            foreach (var item in reminds)
            {
                DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, 
                    DateTimeKind.Utc);
                dtDateTime = dtDateTime.AddSeconds(item.RemindDate).ToLocalTime();
                if (dtDateTime <= DateTime.Now)
                {
                    var channel = item.Channel ?? item.Nick;
                    await Client.SendMessage(channel, $"{item.Nick}: {item.Message}!");
                    Connection.Delete(item);
                }
            }

            PProfiler.EndMeasure("update");
        }
    }

    public void MessageTrigger(MessageGeneric message)
    {
        if (message.Sender.UserIsIgnored() || message.Kind == MessageKind.MessagePersonal) { return; }
        
        PProfiler.BeginMeasure("msg");
        OnMsgEvent.DestroyWallRandomly(Shop, message);
        OnMsgEvent.LevelInscrease(Shop, message);
        OnMsgEvent.PrintWarningInformation(message);
        OnMsgEvent.HandleYoutube(message);
        PProfiler.EndMeasure("msg");
    }

    public string HeuristicPrintErrorMessage(string message)
    {
        try
        {
            dynamic obj = JObject.Parse(message);

            if (obj.message != null)
            {
                return obj.message;
            }
            else if (obj.error != null)
            {
                return obj.error;
            }
            else if (obj.detail != null)
            {
                return obj.detail;
            }
            else
            {
                return message;
            }
        }
        catch (JsonReaderException)
        {
            return message;
        }

    }

    public async Task ExecuteCommand(MessageGeneric message, string prefix, bool ppc = false)
    {
        var prefixes = new[] { prefix, Client.Name + ":" };
        if (!CommandUtilities.HasAnyPrefix(message.Body.TrimStart('p'), prefixes, out _, out var output))
            return;

        PProfiler.BeginMeasure("command");

        if (PProfiler.GetMeasureAvg("command") > 2000)
        {
            await Client.SendMessage(message.Target, "Пожалуйста подождите...");
        }

        var result =
            await Service.ExecuteAsync(output, new CommandProcessor.CustomCommandContext(this, in message, ppc));

        if (!result.IsSuccessful && ppc)
        {
            await Client.SendMessage(message.Target, $"{message.Sender.Username}: НЕДОПУСТИМАЯ ОПЕРАЦИЯ");
            message.Sender.AddItemToInv(Shop, "beer", 1);
        }

        switch (result)
        {
            case ChecksFailedResult err:
                await Client.SendMessage(message.Target,
                    $"Требования не выполнены: {string.Join(" ", err.FailedChecks)}");
                break;
            case TypeParseFailedResult err:
                await Client.SendMessage(message.Target,
                    $"Ошибка в `{err.Parameter}` необходимый тип `{err.Parameter.Type.Name}` вы же ввели `{err.Value.GetType().Name}` введите #helpcmd {err.Parameter.Command} чтобы узнать как правильно пользоватся этой командой");
                break;
            case ArgumentParseFailedResult err:
                var parserResult = err.ParserResult as DefaultArgumentParserResult;

                switch (parserResult.Failure)
                {
                    case DefaultArgumentParserFailure.NoWhitespaceBetweenArguments:
                        await Client.SendMessage(message.Target, $"Нет пробелов между аргументами!");
                        break;
                    case DefaultArgumentParserFailure.TooManyArguments:
                        await Client.SendMessage(message.Target, $"Слишком много аргрументов!!!");
                        break;
                    default:
                        await Client.SendMessage(message.Target, $"Ошибка парсера: `{err.ParserResult.FailureReason}`");
                        break;
                }

                break;
            case OverloadsFailedResult:
                await Client.SendMessage(message.Target, "Команда выключена...");
                break;
            case CommandNotFoundResult err:
                if (!CustomCommandProcessor.ProcessCmd(prefix, in message))
                {
                    string cmdName = message.Body.Split(" ")[0];
                    var cmds = CommandSuggestion(prefix, cmdName);
                    if (!string.IsNullOrWhiteSpace(cmds))
                    {
                        await Client.SendMessage(message.Target,
                            $"Команда [b]{cmdName}[r] не найдена, возможно вы хотели написать: [b]{cmds}");
                    }
                }

                break;
            case CommandExecutionFailedResult err:
                if (err.Exception.GetType() == typeof(JsonReaderException) || err.Exception.GetType() == typeof(JsonException))
                {
                    await Client.SendMessage(message.Target, RandomMsgs.NetworkMessages.Random());
                }
                else
                {
                    var msg = HeuristicPrintErrorMessage(err.Exception.Message);
                    await Client.SendMessage(message.Target, $"[red]Ошибка: {msg}");
                }

                Log.Error(err.Exception.Message + ": " + err.Exception.StackTrace);
                Connection.Insert(new SQL.UnhandledExceptions(err.Exception.Message + ": " + err.Exception.StackTrace,
                    message.Sender.Username, message.Body));
                break;
        }

        PProfiler.EndMeasure("command");
    }
}