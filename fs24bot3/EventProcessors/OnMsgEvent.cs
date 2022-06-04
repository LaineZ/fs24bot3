using fs24bot3.BotSystems;
using fs24bot3.Core;
using fs24bot3.Models;
using NetIRC;
using NetIRC.Messages;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace fs24bot3.EventProcessors
{
    public class OnMsgEvent
    {
        private readonly Bot BotContext;
        private readonly Core.User User;
        private readonly string Target;
        private readonly string Message;
        private readonly Random Rand = new Random();
        private readonly Regex YoutubeRegex = new Regex(@"(?:https?:)?(?:\/\/)?(?:[0-9A-Z-]+\.)?(?:youtu\.be\/|youtube(?:-nocookie)?\.com\S*?[^\w\s-])([\w-]{11})(?=[^\w-]|$)(?![?=&+%\w.-]*(?:['\""][^<>]*>|<\/a>))[?=&+%\w.-]*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

        public OnMsgEvent(Bot botCtx, string nick, string target, string message)
        {
            BotContext = botCtx;
            Message = message;
            Target = target;
            User = new Core.User(nick, botCtx.Connection);
        }

        public async void LevelInscrease(Shop shop)
        {
            User.CreateAccountIfNotExist();
            User.SetLastMessage();
            bool newLevel = User.IncreaseXp(Message.Length * new Random().Next(1, 3) + 1);
            if (newLevel)
            {
                var report = User.AddRandomRarityItem(shop, Models.ItemInventory.ItemRarity.Rare);
                await BotContext.SendMessage(Target, $"{User.Username}: У вас теперь {User.GetUserInfo().Level} уровень. Вы получили за это: {report.First().Value.Name}!");
            }
        }

        public async void DestroyWallRandomly(Shop shop)
        {
            if (Rand.Next(0, 10) == 1 && await User.RemItemFromInv(shop, "wall", 1))
            {
                Log.Information("Breaking wall for {0}", User.Username);
            }
        }

        public async void HandleYoutube()
        {
            foreach (var match in YoutubeRegex.Matches(Message))
            {
                try
                {
                    Process p = new Process();
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.FileName = ConfigurationProvider.Config.YoutubeDlPath;
                    p.StartInfo.Arguments = "--simulate --print-json \"" + match + "\"";
                    p.Start();
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();

                    var jsonOutput = JsonConvert.DeserializeObject<Youtube.Root>(output, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

                    if (jsonOutput != null)
                    {
                        var ts = TimeSpan.FromSeconds(jsonOutput.duration);
                        await BotContext.SendMessage(Target, $"{IrcClrs.Bold}{jsonOutput.title}{IrcClrs.Reset} от {IrcClrs.Bold}{jsonOutput.channel}{IrcClrs.Reset}. Длительность: {IrcClrs.Bold}{ts:hh\\:mm\\:ss}{IrcClrs.Reset} {IrcClrs.Green}👍{jsonOutput.like_count} {IrcClrs.Reset}Просмотров: {jsonOutput.view_count}");
                    }
                }
                catch (Exception e)
                {
                    Log.Error("Cannot handle youtube: {0}:{1}", e.GetType().Name, e.Message);
                }
            }
        }

        public async void PrintWarningInformation()
        {
            if (!BotContext.AcknownUsers.Any(x => x == User.Username) && User.GetWarnings().Any())
            {
                await BotContext.SendMessage(Target, $"{IrcClrs.Gray}{User.Username}: У вас есть предупреждения используйте {User.GetUserPrefix()}warnings чтобы их прочесть!");
                BotContext.AcknownUsers.Add(User.Username);
            }
        }
    }
}