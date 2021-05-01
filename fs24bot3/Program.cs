using fs24bot3.Models;
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

            Log.Information("Connecting to: {0}:{1}", Configuration.network, (int)Configuration.port);
            Task.Run(() => Botara.BotClient.ConnectAsync(Configuration.network, (int)Configuration.port));
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
                    await client.SendAsync(new PrivMsgMessage(target, $"{nick}: НЕДОПУСТИМАЯ ОПЕРАЦИЯ"));
                    new Core.User(nick, Botara.Connection).AddItemToInv("beer", 1);
                }

                switch (result)
                {
                    case ChecksFailedResult err:
                        var errStr = new StringBuilder();
                        foreach (var (check, error) in err.FailedChecks)
                        {
                            errStr.Append(error.FailureReason);
                        }
                        await client.SendAsync(new PrivMsgMessage(target, $"Требования не выполнены: {errStr}"));
                        break;
                    case TypeParseFailedResult err:
                        await client.SendAsync(new PrivMsgMessage(target, $"Ошибка типа в `{err.Parameter}` необходимый тип `{err.Parameter.Type.Name}` вы же ввели `{err.Value.GetType().Name}`"));
                        break;
                    case ArgumentParseFailedResult err:
                        await client.SendAsync(new PrivMsgMessage(target, $"Ошибка парсера: `{err.FailureReason}`"));
                        break;
                    case OverloadsFailedResult _:
                        await client.SendAsync(new PrivMsgMessage(target, "Команда выключена..."));
                        break;
                    case CommandNotFoundResult _:
                        await Botara.CustomCommandProcessor.ProcessCmd(nick, target, message.Trailing.TrimEnd());
                        break;
                    case CommandExecutionFailedResult err:
                        await client.SendAsync(new PrivMsgMessage(target, $"{IrcColors.Red}Ошибка: {err.Exception.Message}"));
                        await client.SendAsync(new PrivMsgMessage(target, err.Exception.StackTrace));
                        break;
                }
            }

            if (message.IRCCommand == IRCCommand.ERROR)
            {
                Log.Error("Connection closed due to error... Exiting");
                Environment.Exit(1);
            }

            if (message.IRCCommand == IRCCommand.KICK)
            {
                if (message.Parameters[1] == Configuration.name)
                {
                    Log.Warning("I've got kick from {0} rejoining...", message.Prefix);
                    await client.SendRaw("JOIN " + Configuration.channel);
                    await client.SendAsync(new PrivMsgMessage(Configuration.channel, "За что?"));
                }
            }
        }

        private async static void Client_OnRegister(object sender, EventArgs _)
        {
            await Botara.BotClient.SendRaw("JOIN " + Configuration.channel);
            await Botara.SendMessage("Nickserv", "IDENTIFY " + Configuration.nickservPass);
            await Botara.SendMessage(Configuration.channel, Core.Database.GetRandomLyric(Botara.Connection));
        }

        private static void Client_OnRawDataReceived(Client client, string rawData)
        {
            Log.Information(rawData);
        }
    }
}
