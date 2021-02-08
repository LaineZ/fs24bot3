﻿using Qmmands;
using System.Threading.Tasks;

namespace fs24bot3.Checks
{
    public class CheckAdmin : CheckAttribute
    {
        public CheckAdmin()
        { }

        public override ValueTask<CheckResult> CheckAsync(CommandContext _)
        {
            var context = _ as CommandProcessor.CustomCommandContext;

            UserOperations usr = new UserOperations(context.Message.From, context.Connection);
            return usr.GetUserInfo().Admin == 2
                ? CheckResult.Successful
                : CheckResult.Unsuccessful("Это команда только для админов!");
        }

        public override string ToString()
        {
            return "Права администратора";
        }
    }
}
