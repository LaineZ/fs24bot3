using fs24bot3.Core;
using fs24bot3.Models;
using fs24bot3.QmmandsProcessors;
using Qmmands;
using System.Threading.Tasks;

namespace fs24bot3.Checks;
public sealed class CheckAdmin : CheckAttribute
{
    public override ValueTask<CheckResult> CheckAsync(CommandContext _)
    {
        var context = _ as CommandProcessor.CustomCommandContext;

        try
        {
            return (context.User.GetUserInfo().Admin >= 1 && context.IsAuthorizedAction) || ConfigurationProvider.Config.Backend == Models.Backend.Basic
            ? CheckResult.Successful
            : CheckResult.Failed("Эта команда только для админов!");
        }
        catch (Exceptions.UserNotFoundException)
        {
            return CheckResult.Failed("Эта команда только для пользователей fs24_bot и только для админов!");
        }
    }

    public override string ToString()
    {
        return "Права администратора";
    }
}
