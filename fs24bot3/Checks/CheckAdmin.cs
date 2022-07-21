using fs24bot3.Core;
using fs24bot3.QmmandsProcessors;
using Qmmands;
using System.Threading.Tasks;

namespace fs24bot3.Checks;
public sealed class CheckAdmin : CheckAttribute
{
    public CheckAdmin()
    { }

    public override ValueTask<CheckResult> CheckAsync(CommandContext _)
    {
        var context = _ as CommandProcessor.CustomCommandContext;

        return context.User.GetUserInfo().Admin == 2
            ? CheckResult.Successful
            : CheckResult.Failed("Это команда только для админов!");
    }

    public override string ToString()
    {
        return "Права администратора";
    }
}
