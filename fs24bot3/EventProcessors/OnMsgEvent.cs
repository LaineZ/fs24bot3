using fs24bot3.Systems;
using fs24bot3.Core;
using fs24bot3.Models;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using fs24bot3.Helpers;
using HandlebarsDotNet;
using static fs24bot3.Models.OpenWeatherMapResponse;
using NetIRC;
using System.Collections.Generic;

namespace fs24bot3.EventProcessors;
public class OnMsgEvent
{
    private readonly Bot BotContext;
    private readonly Random Rand = new Random();
    private readonly Regex YoutubeRegex = new Regex(@"(?:https?:)?(?:\/\/)?(?:[0-9A-Z-]+\.)?(?:youtu\.be\/|youtube(?:-nocookie)?\.com\S*?[^\w\s-])([\w-]{11})(?=[^\w-]|$)(?![?=&+%\w.-]*(?:['\""][^<>]*>|<\/a>))[?=&+%\w.-]*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

    public OnMsgEvent(Bot botCtx)
    {
        BotContext = botCtx;
    }

    private static string FmtUploadDate(string date)
    {
        string year = date.Substring(0, 4);
        string month = date.Substring(4, 2);
        string day = date.Substring(6, 2); ;

        return year + "-" + month + "-" + day;
    }

    public async void LevelInscrease(Shop shop, MessageGeneric message)
    {
        message.Sender.CreateAccountIfNotExist();
        message.Sender.SetLastMessage();
        bool newLevel = message.Sender.IncreaseXp(message.Body.Length * new Random().Next(1, 3));
        if (newLevel)
        {
            var report = message.Sender.AddRandomRarityItem(shop, ItemInventory.ItemRarity.Rare);
            await BotContext.Client.SendMessage(message.Target, $"{message.Sender.Username}: У вас теперь {message.Sender.GetUserInfo().Level} уровень. Вы получили за это: {report.First().Value.Name}!");
        }
    }

    public async void DestroyWallRandomly(Shop shop, MessageGeneric message)
    {
        if (Rand.Next(0, 10) == 1 && await message.Sender.RemItemFromInv(shop, "wall", 1))
        {
            Log.Information("Breaking wall for {0}", message.Sender.Username);
        }
    }

    public void HandleYoutube(MessageGeneric message)
    {
        foreach (var match in YoutubeRegex.Matches(message.Body))
        {
            try
            {
                new Thread(async () =>
                {
                    Process p = new Process();
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.FileName = ConfigurationProvider.Config.Services.YoutubeDlPath;
                    p.StartInfo.Arguments = "--simulate --print-json \"" + match + "\"";
                    p.Start();
                    string output = await p.StandardOutput.ReadToEndAsync();
                    await p.WaitForExitAsync();

                    var jsonOutput = JsonConvert.DeserializeObject<Youtube.Root>(output,
                        Helpers.JsonSerializerHelper.OPTIMIMAL_SETTINGS);

                    if (jsonOutput != null)
                    {
                        var ts = TimeSpan.FromSeconds(jsonOutput.duration);
                        await BotContext.Client.SendMessage(message.Target,
                            $"[b]{jsonOutput.title}[r] от [b]{jsonOutput.channel}[r]. " +
                            $"[green]Длительность: [r][b]{ts:hh\\:mm\\:ss}[r] " +
                            $"[green]👍[r][b] {jsonOutput.like_count}[r] " +
                            $"[green]Просмотров: [r][b]{jsonOutput.view_count}[r] " +
                            $"[green]Дата загрузки: [r][b]{FmtUploadDate(jsonOutput.upload_date)}[r]");
                    }
                }).Start();
            }
            catch (Exception e)
            {
                Log.Error("Cannot handle youtube: {0}", e);
            }
        }
    }

    public void InsertMessages(MessageGeneric message)
    {
        BotContext.Connection.Insert(new SQL.Messages()
        {
            Message = message.Body,
            Nick = message.Sender.Username,
            Date = DateTime.Now
        });
    }

    public async void WhoWrotesMe(MessageGeneric message)
    {
        if (message.Body.ToLower().TrimStart().StartsWith("кто мне пишет"))
        {
            var wroteMe = BotContext.Connection.Table<SQL.Messages>().ToList().Where(x => x.Message.Contains(message.Sender.Username)).DistinctBy( x => x.Nick).Select(x => x.Nick);
            if (wroteMe.Any())
            {
                await BotContext.Client.SendMessage(message.Target, $"Уважаемый {message.Sender.Username} 🥰! Вам писали следующие люди: {MessageHelper.AntiHightlight(string.Join(", ", wroteMe))}");
                BotContext.Connection.Execute("DELETE FROM Messages WHERE Message LIKE ?", $"%{message.Sender.Username}%");
            }
            else
            {
                await BotContext.Client.SendMessage(message.Target, $"{message.Sender.Username}: вам никто не писал...");
            }
        }
    }

    public async void PrintWarningInformation(MessageGeneric message)
    {
        if (BotContext.AcknownUsers.All(x => x != message.Sender.Username) && message.Sender.GetWarnings().Any())
        {
            await BotContext.Client.SendMessage(message.Target,
            $"[gray]{message.Sender.Username}: " +
            $"У вас есть предупреждения используйте .warnings чтобы их прочесть!");
            BotContext.AcknownUsers.Add(message.Sender.Username);
        }
    }
}