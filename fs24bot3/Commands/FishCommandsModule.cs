using Qmmands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fs24bot3.Models;

namespace fs24bot3
{
    public sealed class FishCommandsModule : ModuleBase<CommandProcessor.CustomCommandContext>
    {

        public CommandService Service { get; set; }

        [Command("buyrod")]
        [Description("Купить удочочку - если параметр rodname пуст, напишет список удочек")]
        public void Buyrod(string rodname = "")
        {
            if (rodname.Any())
            {
                var query = Context.Connection.Table<SQL.FishingRods>().Where(v => v.RodName.Equals(rodname)).ToList();
                var user = new UserOperations(Context.Message.From, Context.Connection, Context);
                
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
                                Context.SendMessage(Context.Channel, $"{IrcColors.Red}Чёто не так причина: {rodState.ToString()}... Деньги возвращены");
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

                Context.SendMessage(Context.Channel, string.Join(" ", query.Select(x => x.RodName)));
            }
        }

        [Command("sellrod")]
        [Description("Продать свою удочку")]
        public void SellRod()
        {
            var user = new UserOperations(Context.Message.From, Context.Connection, Context);

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
                    Context.SendMessage(Context.Channel, $"{IrcColors.Red}Чёто не так причина: {rodState.ToString()}... =( =(");
                    break;
            }
        }

        [Command("nest")]
        [Description("Установить место рыбалки - если параметр nestname пуст, напишет список мест")]
        [Remarks("RLF - требуемый размер лески F - количество рыбы")]
        public void SetNest(string nestname = "")
        {
            var user = new UserOperations(Context.Message.From, Context.Connection, Context);
            string rodname = user.GetRod().RodName;
            var query = Context.Connection.Table<SQL.FishingRods>().Where(v => v.RodName.Equals(rodname)).ToList()[0];

            if (nestname.Any())
            {
                if (user.GetRod() == null)
                {
                    Context.SendMessage(Context.Channel, "У вас нету удочки =(");
                    return;
                }

                var state = user.SetNest(nestname);

                if (state.Item2.FishingLineRequired <= user.GetRod().RodDurabillity)
                {
                    switch (state.Item1)
                    {
                        case FishingError.RodErrors.RodNotFound:
                            Context.SendMessage(Context.Channel, $"{IrcColors.Gray}Удочка не найдена");
                            break;
                        case FishingError.RodErrors.RodOk:
                            Context.SendMessage(Context.Channel, $"Установлено место рыбалки {nestname}");
                            break;
                        default:
                            Context.SendMessage(Context.Channel, $"{IrcColors.Red}Чёто не так причина: {state.Item1}... =( =(");
                            break;
                    }
                }
                else
                {
                    Context.SendMessage(Context.Channel, $"{IrcColors.Gray}Слишком маленькая длинна лески! {state.Item2.FishingLineRequired} > {query.FishingLine}");
                }
            }
            else
            {
                var queryFish = Context.Connection.Table<SQL.FishingNests>().ToList();
                Context.SendMessage(Context.Channel, string.Join(" | ", queryFish.Select(x => $"{x.Name} RFL:{x.FishingLineRequired} F:{x.FishCount} ")));
            }
        }

        [Command("fish")]
        [Description("Рыбачить!")]
        public void Fish()
        {
            var user = new UserOperations(Context.Message.From, Context.Connection, Context);
            var userRod = user.GetRod();

            if (userRod == null)
            {
                Context.SendMessage(Context.Channel, $"{IrcColors.Gray}Удочка не найдена");
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
            if (rand.Next(0, 5) == 2)
            {
                // TODO: Refactor
                if (nest.Level == 1)
                {
                    string[] fish = { "fish", "veriplace", "ffish"};
                    string catched = fish[rand.Next(0, fish.Length)];
                    user.AddItemToInv(catched, 1);
                    Context.SendMessage(Context.Channel, $"Вы поймали {Shop.GetItem(catched).Name}!");
                    ;               }
                if (nest.Level == 2)
                {
                    string[] fish = { "fish", "veriplace", "ffish", "pike", "som"};
                    string catched = fish[rand.Next(0, fish.Length)];
                    user.AddItemToInv(catched, 1);
                    Context.SendMessage(Context.Channel, $"Вы поймали {Shop.GetItem(catched).Name}!");
                }
                if (nest.Level == 3)
                {
                    string[] fish = { "fish", "veriplace", "ffish", "pike", "som", "weirdfishes", "worm", "wrench", "wrenchadv", "dj", "pistol" };
                    string catched = fish[rand.Next(0, fish.Length)];
                    user.AddItemToInv(catched, rand.Next(1, 2));
                    Context.SendMessage(Context.Channel, $"Вы поймали {Shop.GetItem(catched).Name}!");
                }
            }
            else
            {
                Context.SendMessage(Context.Channel, $"{IrcColors.Gray}Рыба сорвалась!");
            }

            Context.Connection.Execute("UPDATE UserFishingRods SET RodDurabillity = RodDurabillity - 1 WHERE Username = ?", Context.Message.From);
        }

        [Command("rodinfo")]
        [Description("Инфо о удочке")]
        public void Rodinfo(string rodname = "")
        {
            if (!rodname.Any())
            {
                UserOperations user = new UserOperations(Context.Message.From, Context.Connection);

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
