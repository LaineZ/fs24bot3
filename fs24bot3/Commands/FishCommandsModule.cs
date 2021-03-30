using fs24bot3.Models;
using Qmmands;
using System;
using System.Linq;

namespace fs24bot3.Commands
{
    public sealed class FishCommandsModule : ModuleBase<CommandProcessor.CustomCommandContext>
    {

        public CommandService Service { get; set; }

        [Command("buyrod")]
        [Description("Купить удочочку - если параметр rodname пуст, напишет список удочек")]
        public async void Buyrod(string rodname = "")
        {
            if (rodname.Any())
            {
                var query = Context.Connection.Table<SQL.FishingRods>().Where(v => v.RodName.Equals(rodname)).ToList();
                var user = new User(Context.Sender, Context.Connection, Context);

                if (query.Any())
                {
                    if (user.RemItemFromInv("money", query[0].Price))
                    {
                        FishingError.RodErrors rodState = user.AddRod(rodname);

                        switch (rodState)
                        {
                            case FishingError.RodErrors.RodOk:
                                Context.SendMessage(Context.Channel, $"{IrcColors.Green}Удочка {rodname} куплена!");
                                break;
                            case FishingError.RodErrors.RodAreadyExists:
                                Context.SendMessage(Context.Channel, $"{IrcColors.Gray}У вас уже есть какая-то удочка, введите @sellrod чтобы продать текущую удочку");
                                break;
                            default:
                                Context.SendMessage(Context.Channel, $"{IrcColors.Red}Чёто не так причина: {rodState}... Деньги возвращены");
                                user.AddItemToInv("money", query[0].Price);
                                break;
                        }
                    }
                }
                else
                {
                    Context.SendMessage(Context.Channel, $"{IrcColors.Gray}Удочка not found...");
                }
            }
            else
            {
                var query = Context.Connection.Table<SQL.FishingRods>().ToList();
                string link = await new HttpTools().UploadToTrashbin(string.Join("\n", query.Select(x => $"{x.RodName}\tРазмер лески: {x.FishingLine} Крутость поплавка: {x.HookSize} Цена: {x.Price}")), "addplain");
                Context.SendMessage(Context.Channel, "Все удочки: " + link);
            }
        }

        [Command("sellrod")]
        [Description("Продать свою удочку")]
        public void SellRod()
        {
            var user = new User(Context.Sender, Context.Connection, Context);

            (FishingError.RodErrors, SQL.UserFishingRods) rodState = user.DelRod();

            switch (rodState.Item1)
            {
                case FishingError.RodErrors.RodNotFound:
                    Context.SendMessage(Context.Channel, $"{IrcColors.Gray}Удочка не найдена");
                    break;
                case FishingError.RodErrors.RodOk:
                    var rod = Context.Connection.Table<SQL.FishingRods>().Where(v => v.RodName.Equals(rodState.Item2.RodName)).ToList()[0];

                    int price = rod.Price / 2 - rodState.Item2.RodDurabillity;

                    user.AddItemToInv("money", price);
                    Context.SendMessage(Context.Channel, $"{IrcColors.Green}Вы продали свою удочку {rodState.Item2.RodName} за {price} денег");
                    break;
                default:
                    Context.SendMessage(Context.Channel, $"{IrcColors.Red}Чёто не так причина: {rodState}... =( =(");
                    break;
            }
        }

        [Command("nest")]
        [Description("Установить место рыбалки - если параметр nestname пуст, напишет список мест")]
        [Remarks("RLF - требуемый размер лески F - количество рыбы")]
        public async void SetNest(string nestname = "")
        {
            var user = new User(Context.Sender, Context.Connection, Context);
            string rodname = user.GetRod().RodName;
            var query = Context.Connection.Table<SQL.FishingRods>().Where(v => v.RodName.Equals(rodname)).FirstOrDefault();

            if (nestname.Any())
            {
                if (user.GetRod() == null)
                {
                    Context.SendMessage(Context.Channel, "У вас нету удочки =(");
                    return;
                }

                var state = user.SetNest(nestname);

                if (state.Item2 == null)
                {
                    switch (state.Item1)
                    {
                        case FishingError.RodErrors.RodNotFound:
                            Context.SendMessage(Context.Channel, $"{IrcColors.Gray}Удочка не найдена");
                            break;
                        case FishingError.RodErrors.RodUnknownError:
                            Context.SendMessage(Context.Channel, $"{IrcColors.Gray}Такого места для рыбалки не сущесвует");
                            break;
                    }

                    return;
                }

                // TODO: Fix
                if (state.Item2.FishingLineRequired <= user.GetRod().RodDurabillity)
                {
                    Context.SendMessage(Context.Channel, $"Установлено место рыбалки {nestname}");
                }
                else
                {
                    Context.SendMessage(Context.Channel, $"{IrcColors.Gray}Слишком маленькая длинна лески! {state.Item2.FishingLineRequired} > {query.FishingLine}");
                }
            }
            else
            {
                var queryFish = Context.Connection.Table<SQL.FishingNests>().ToList();

                string link = await new HttpTools().UploadToTrashbin(string.Join("\n", queryFish.Select(x => $"{x.Name}\tТребуемый уровень удочки:{x.FishingLineRequired}\tКоличество рыбы:{x.FishCount} ")), "addplain");
                Context.SendMessage(Context.Channel, "Все места для рыбалки: " + link);
            }
        }

        [Command("fish")]
        [Description("Рыбачить!")]
        public void Fish()
        {
            var user = new User(Context.Sender, Context.Connection, Context);
            var userRod = user.GetRod();

            if (userRod == null)
            {
                Context.SendMessage(Context.Channel, $"{IrcColors.Gray}Удочка не найдена @buyrod");
                return;
            }

            if (userRod.Nest == null)
            {
                Context.SendMessage(Context.Channel, $"{IrcColors.Gray}Место рыбалки не установлено, используйте @nest");
                return;
            }

            if (!user.RemItemFromInv("worm", 1))
            {
                Context.SendMessage(Context.Channel, $"{IrcColors.Gray}У вас нет наживки @buy worm");
                return;
            }

            if (userRod.RodDurabillity <= 0)
            {
                Context.SendMessage(Context.Channel, $"{IrcColors.Gray}У вас сломалась удочка!");
                return;
            }

            var rod = Context.Connection.Table<SQL.FishingRods>().Where(v => v.RodName.Equals(userRod.RodName)).ToList()[0];
            var nest = Context.Connection.Table<SQL.FishingNests>().Where(v => v.Name.Equals(userRod.Nest)).ToList()[0];


            Random rand = new Random();
            // TODO: Switch to normal
            if (rand.Next(0, 2) == 1)
            {
                // TODO: Refactor
                string[] fish = new string[15];

                if (nest.Level == 1)
                {
                    fish = new string[] { "fish", "veriplace", "ffish" };
                }
                if (nest.Level == 2)
                {
                    fish = new string[] { "fish", "veriplace", "ffish", "pike", "som" };
                }
                if (nest.Level == 3)
                {
                    fish = new string[] { "fish", "veriplace", "ffish", "pike", "som", "weirdfishes", "worm", "wrench", "wrenchadv" };
                }

                string catched = fish[rand.Next(0, fish.Length)];
                user.AddItemToInv(catched, 1);
                Context.SendMessage(Context.Channel, $"Вы поймали {Shop.GetItem(catched).Name}!");
            }
            else
            {
                Context.SendMessage(Context.Channel, $"{IrcColors.Gray}Рыба сорвалась!");
            }

            Context.Connection.Execute("UPDATE UserFishingRods SET RodDurabillity = RodDurabillity - 1 WHERE Username = ?", Context.Sender);
        }

        [Command("rodinfo")]
        [Description("Инфо о удочке")]
        public void Rodinfo(string rodname = "")
        {
            if (!rodname.Any())
            {
                User user = new User(Context.Sender, Context.Connection);

                var rod = user.GetRod();

                if (rod != null)
                {
                    var query = Context.Connection.Table<SQL.FishingRods>().ToList();
                    var queryRod = query.Find(x => x.RodName.Equals(rod.RodName));
                    Context.SendMessage(Context.Channel, $"🎣 {rod.RodName} - Прочность: {rod.RodDurabillity}/{queryRod.RodDurabillity} Размер лески: {queryRod.FishingLine} м Крутость поплавка: {queryRod.HookSize}");
                }
                else
                {
                    Context.SendMessage(Context.Channel, $"{IrcColors.Gray}У вас нету удочки...");
                }
            }
            else
            {
                var query = Context.Connection.Table<SQL.FishingRods>().Where(x => x.RodName.Equals(rodname)).ToList();

                if (query.Any())
                {
                    Context.SendMessage(Context.Channel, $"🎣 {query[0].RodName} - Прочность: {query[0].RodDurabillity} Размер лески: {query[0].FishingLine} м Крутость поплавка: {query[0].HookSize} {IrcColors.Blue}Цена: {query[0].Price}");
                }
                else
                {
                    Context.SendMessage(Context.Channel, $"{IrcColors.Gray}Удочка not found...");
                }
            }
        }
    }
}
