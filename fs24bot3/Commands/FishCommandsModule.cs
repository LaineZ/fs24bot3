using Newtonsoft.Json;
using Qmmands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
                var query = Context.Connection.Table<Models.SQL.FishingRods>().Where(v => v.RodName.Equals(rodname)).ToList();
                var user = new UserOperations(Context.Message.From, Context.Connection, Context);
                
                if (query.Any())
                {
                    if (user.RemItemFromInv("money", query[0].Price))
                    {
                        bool isGood = user.AddRod(rodname);
                        if (isGood)
                        {
                            Context.SendMessage(Context.Channel, $"{Models.IrcColors.Green}Удочка {rodname} куплена!");
                        }
                        else
                        {
                            Context.SendMessage(Context.Channel, $"{Models.IrcColors.Red}Чёто не так... Деньги возвращены");
                            user.AddItemToInv("money", query[0].Price);
                        }
                    }
                }
                else
                {
                    Context.SendMessage(Context.Channel, $"{Models.IrcColors.Gray}Удочка not found...");
                }
            }
            else
            {
                var query = Context.Connection.Table<Models.SQL.FishingRods>().ToList();

                Context.SendMessage(Context.Channel, string.Join(" ", query.Select(x => x.RodName)));
            }
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
                    var query = Context.Connection.Table<Models.SQL.FishingRods>().ToList();
                    var queryRod = query.Find(x => x.RodName.Equals(rod.RodName));
                    Context.SendMessage(Context.Channel, $"🎣 {rod.RodName} - Прочность: {rod.RodDurabillity}/{queryRod.RodDurabillity} Размер лески: {queryRod.FishingLine} м Крутость поплавка: {queryRod.HookSize}");
                }
                else
                {
                    Context.SendMessage(Context.Channel, $"{Models.IrcColors.Gray}У вас нету удочки...");
                }
            }
            else
            {
                var query = Context.Connection.Table<Models.SQL.FishingRods>().Where(x => x.RodName.Equals(rodname)).ToList();

                if (query.Any())
                {
                    Context.SendMessage(Context.Channel, $"🎣 {query[0].RodName} - Прочность: {query[0].RodDurabillity} Размер лески: {query[0].FishingLine} м Крутость поплавка: {query[0].HookSize} {Models.IrcColors.Blue}Цена: {query[0].Price}");
                }
                else
                {
                    Context.SendMessage(Context.Channel, $"{Models.IrcColors.Gray}Удочка not found...");
                }
            }
        }
    }
}
