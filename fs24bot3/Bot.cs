﻿using fs24bot3.Systems;
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
using fs24bot3.Backend;

namespace fs24bot3;
public class Bot
{
    public SQLiteConnection Connection = new SQLiteConnection("fsdb.sqlite");
    public CustomCommandProcessor CustomCommandProcessor;
    public readonly CommandService Service = new CommandService();
    public IMessagingClient Client { get; private set; }
    public Shop Shop { get; set; }
    public Songame SongGame { get; set; }
    public List<string> AcknownUsers = new List<string>();
    public Profiler PProfiler { get; private set; }
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

        Database.InitDatabase(Connection);
        CustomCommandProcessor = new CustomCommandProcessor(this);
        PProfiler = new Profiler();

        PProfiler.AddMetric("update");
        PProfiler.AddMetric("update_stats");
        PProfiler.AddMetric("command");
        PProfiler.AddMetric("msg");

        // check for custom commands used in a bot
        Log.Information("Checking for user commands with incorrect names");
        foreach (var command in Connection.Table<SQL.CustomUserCommands>())
        {
            bool commandIntenral = Service.GetAllCommands().Any(x => x.Aliases.Any(a => a == command.Command));

            if (commandIntenral)
            {
                var user = new Core.User(command.Nick, Connection);
                Log.Warning("User {0} have a command with internal name {1}!", user.Username, command.Command);
                user.AddWarning($"Вы регистрировали команду {user.GetUserPrefix()}{command.Command}, в новой версии fs24bot добавилась команда с таким же именем, ВАША КАСТОМ-КОМАНДА БОЛЬШЕ НЕ БУДЕТ РАБОТАТЬ! Чтобы вернуть деньги за команду используйте {user.GetUserPrefix()}delcmd {command.Command}. И создайте команду с другим именем", this);
            }
        }
        
        Shop = new Shop(this);
        SongGame = new Songame(Connection);
        
        Log.Information("Bot: Construction complete!");
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

    public async void ProccessInfinite()
    {
        while (true)
        {
            Thread.Sleep(1000);
            PProfiler.BeginMeasure("update");
            var users = Connection.Table<SQL.UserStats>();
            foreach (var user in users)
            {
                var onTick = new EventProcessors.OnTick(user.Nick, Connection);
                onTick.UpdateUserPaydays(Shop);
                onTick.RemoveLevelOneAccs();
            }
            var reminds = Connection.Table<SQL.Reminds>();
            foreach (var item in reminds)
            {
                DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                dtDateTime = dtDateTime.AddSeconds(item.RemindDate).ToLocalTime();
                if (dtDateTime <= DateTime.Now)
                {
                    string ch = item.Channel ?? ConfigurationProvider.Config.Channel;
                    await Client.SendMessage(ch, $"{item.Nick}: {item.Message}!");
                    Connection.Delete(item);
                }
            }
            PProfiler.EndMeasure("update");
        }
    }

    public void MessageTrigger(MessageGeneric message)
    {
        var queryIfExt = Connection.Table<SQL.Ignore>().Where(v => v.Username.Equals(message.Sender.Username)).Any();
        if (queryIfExt) { return; }

        new Thread(() =>
        {
            PProfiler.BeginMeasure("msg");
            EventProcessors.OnMsgEvent events = new EventProcessors.OnMsgEvent(this, in message);
            events.DestroyWallRandomly(Shop);
            events.LevelInscrease(Shop);
            events.PrintWarningInformation();
            events.HandleYoutube();
            PProfiler.EndMeasure("msg");
        }).Start();
    }

    public async Task ExecuteCommand(MessageGeneric message, string prefix, bool ppc = false)
    {
        var prefixes = new string[] { prefix, Client.Name + ":" };
        if (!CommandUtilities.HasAnyPrefix(message.Body.TrimStart('p'), prefixes, out string _, out string output))
            return;

        PProfiler.BeginMeasure("command");
        var result = await Service.ExecuteAsync(output, new CommandProcessor.CustomCommandContext(this, in message, ppc));

        if (!result.IsSuccessful && ppc)
        {
            await Client.SendMessage(message.Target, $"{message.Sender.Username}: НЕДОПУСТИМАЯ ОПЕРАЦИЯ");
            message.Sender.AddItemToInv(Shop, "beer", 1);
        }

        switch (result)
        {
            case ChecksFailedResult err:
                await Client.SendMessage(message.Target, $"Требования не выполнены: {string.Join(" ", err.FailedChecks)}");
                break;
            case TypeParseFailedResult err:
                await Client.SendMessage(message.Target, $"Ошибка в `{err.Parameter}` необходимый тип `{err.Parameter.Type.Name}` вы же ввели `{err.Value.GetType().Name}` введите #helpcmd {err.Parameter.Command} чтобы узнать как правильно пользоватся этой командой");
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
            case CommandNotFoundResult _:
                if (!CustomCommandProcessor.ProcessCmd(prefix, in message))
                {
                    string cmdName = message.Body.Split(" ")[0];
                    var cmds = CommandSuggestion(prefix, cmdName);
                    if (!string.IsNullOrWhiteSpace(cmds))
                    {
                        await Client.SendMessage(message.Target, $"Команда {IrcClrs.Bold}{cmdName}{IrcClrs.Reset} не найдена, возможно вы хотели написать: {IrcClrs.Bold}{cmds}");
                    }
                }
                break;
            case CommandExecutionFailedResult err:
                await Client.SendMessage(message.Target, $"{IrcClrs.Red}Ошибка: {err.Exception.GetType().Name}: {err.Exception.Message}");
                Connection.Insert(new SQL.UnhandledExceptions(err.Exception.Message + ": " + err.Exception.StackTrace, message.Sender.Username, message.Body));
                break;
        }
        PProfiler.EndMeasure("command");
    }
}
