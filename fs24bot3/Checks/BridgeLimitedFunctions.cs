using fs24bot3.Core;
using fs24bot3.Models;
using fs24bot3.QmmandsProcessors;
using Qmmands;
using System.Threading.Tasks;

namespace fs24bot3.Checks;
public sealed class BridgeLimitedFunctions : CheckAttribute
{
    public override async ValueTask<CheckResult> CheckAsync(CommandContext _)
    {
        var context = _ as CommandProcessor.CustomCommandContext;

        if (context.FromBridge)
        {
            await context.SendMessage(context.Channel, "Внимание: данная команда не оптимизирована для дискорднутых!");
        }
        return CheckResult.Successful;
    }

    public override string ToString()
    {
        return "Права администратора";
    }
}
