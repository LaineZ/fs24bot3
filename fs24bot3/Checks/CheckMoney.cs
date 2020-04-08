using Qmmands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace fs24bot3.Checks
{
    public class CheckMoney : CheckAttribute
    {
        public int Count = 0;
        public CheckMoney(int count)
        { count = Count; }

        public override ValueTask<CheckResult> CheckAsync(CommandContext _)
        {

            var context = _ as CommandProcessor.CustomCommandContext;

            UserOperations usr = new UserOperations(context.Message.From, context.Connection);
            return usr.RemItemFromInv("money", Count)
                ? CheckResult.Successful
                : CheckResult.Unsuccessful("Недостаточно денег для выполнения данной команды!");
        }
    }
}
