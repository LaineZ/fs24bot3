﻿using fs24bot3.Models;
using fs24bot3.QmmandsProcessors;
using NetIRC;
using NetIRC.Messages;
using Qmmands;
using Serilog;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace fs24bot3
{
    class Program
    {
        public static Bot Botara;

        static void Main()
        {
            Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .MinimumLevel.ControlledBy(Configuration.LoggerSw)
            .CreateLogger();

            Log.Information("fs24_bot 3 by 140bpmdubstep");

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                Console.OutputEncoding = Encoding.Unicode;
            }

            Botara = new Bot();

            Botara.BotClient.OnRawDataReceived += Client_OnRawDataReceived;
            Botara.BotClient.OnIRCMessageParsed += Client_OnIRCMessageParsed;
            Botara.BotClient.RegistrationCompleted += Client_OnRegister;

            Log.Information("Connecting to: {0}:{1}", Configuration.Network, (int)Configuration.Port);
            Task.Run(() => Botara.BotClient.ConnectAsync(Configuration.Network, (int)Configuration.Port));
            Log.Information("First init is ok!");

            Botara.ProccessInfinite();
        }

        private async static void Client_OnIRCMessageParsed(Client client, ParsedIRCMessage message)
        {
            if (message.IRCCommand == IRCCommand.PRIVMSG)
            {
                string nick = message.Prefix.From;
                string target = message.Parameters[0];

                if (message.Parameters[0] == client.User.Nick)
                {
                    target = message.Prefix.From;
                }

                Botara.MessageTrigger(nick, target, message);

                if (!CommandUtilities.HasPrefix(message.Trailing.TrimEnd().TrimStart('p'), '@', out string output))
                    return;

                bool ppc = message.Trailing.StartsWith("p") && Core.Transalator.AlloPpc;
                var result = await Botara.Service.ExecuteAsync(output, new CommandProcessor.CustomCommandContext(target, message, Botara, ppc));

                if (!result.IsSuccessful && ppc)
                {
                    await Botara.SendMessage(target, $"{nick}: НЕДОПУСТИМАЯ ОПЕРАЦИЯ");
                    new Core.User(nick, Botara.Connection).AddItemToInv(Botara.Shop, "beer", 1);
                }

                switch (result)
                {
                    case ChecksFailedResult err:
                        await Botara.SendMessage(target, $"Требования не выполнены: {string.Join(" ", err.FailedChecks)}");
                        break;
                    case TypeParseFailedResult err:
                        await Botara.SendMessage(target, $"Ошибка в `{err.Parameter}` необходимый тип `{err.Parameter.Type.Name}` вы же ввели `{err.Value.GetType().Name}` введите @helpcmd {err.Parameter.Command} чтобы узнать как правильно пользоватся этой командой");
                        break;
                    case ArgumentParseFailedResult err:
                        await Botara.SendMessage(target, $"Ошибка парсера: `{err.FailureReason}`");
                        break;
                    case OverloadsFailedResult:
                        await Botara.SendMessage(target, "Команда выключена...");
                        break;
                    case CommandNotFoundResult:
                        Botara.CustomCommandProcessor.ProcessCmd(nick, target, message.Trailing.TrimEnd());
                        break;
                    case CommandExecutionFailedResult err:
                        await Botara.SendMessage(target, $"{IrcClrs.Red}Ошибка: {err.Exception.Message}{err.Exception.StackTrace}");
                        Botara.Connection.Insert(new SQL.UnhandledExceptions(err.Exception.Message + ": " + err.Exception.StackTrace, nick, message.Trailing.TrimEnd()));
                        break;
                }
            }

            if (message.IRCCommand == IRCCommand.ERROR)
            {
                Log.Error("Connection closed due to error... RECONNECTION!!!");
                Botara.Reconnect();
            }

            if (message.NumericReply == IRCNumericReply.ERR_NICKNAMEINUSE)
            {
                await Botara.BotClient.SendRaw("NICK " + Configuration.Name + new Random().Next(int.MinValue, int.MaxValue));
            }

            if (message.NumericReply == IRCNumericReply.ERR_PASSWDMISMATCH)
            {
                await client.SendRaw("PASS " + Configuration.ServerPassword);
            }

            if (message.IRCCommand == IRCCommand.KICK)
            {
                if (message.Parameters[1] == Configuration.Name)
                {
                    Log.Warning("I've got kick from {0} rejoining...", message.Prefix);
                    await client.SendRaw("JOIN " + message.Parameters[0]);
                    await Botara.SendMessage(Configuration.Channel, "За что?");
                }
            }
        }

        private async static void Client_OnRegister(object sender, EventArgs _)
        {
            await Botara.BotClient.SendRaw("JOIN " + Configuration.Channel);
            await Botara.SendMessage("Nickserv", "IDENTIFY " + Configuration.NickservPass);

            var res = new Helpers.InternetServicesHelper().InPearls().Result.Random();
            await Botara.SendMessage(Configuration.Channel, res);
        }

        private static void Client_OnRawDataReceived(Client client, string rawData)
        {
            Log.Information(rawData);
        }
    }
}
