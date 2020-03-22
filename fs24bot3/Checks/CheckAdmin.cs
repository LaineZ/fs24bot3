using Qmmands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace fs24bot3.Checks
{
    public class CheckAdmin : CheckAttribute
    {
        public CheckAdmin()
        { }

        public override ValueTask<CheckResult> CheckAsync(CommandContext _)
        {
            SQLTools sql = new SQLTools();

            var context = _ as CustomCommandContext;
            return sql.getUserInfo(context.Connection, context.Message.User).Admin == 2
                ? CheckResult.Successful
                : CheckResult.Unsuccessful("Это команда только для админов!");
        }
    }
}
