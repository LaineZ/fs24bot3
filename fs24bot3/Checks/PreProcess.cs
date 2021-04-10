using Qmmands;
using System.Threading.Tasks;

namespace fs24bot3.Checks
{
    public class PreProcess : CheckAttribute
    {
        public PreProcess()
        { }

        public override ValueTask<CheckResult> CheckAsync(CommandContext _)
        {
            var context = _ as SearchCommandProcessor.CustomCommandContext;

            return context.PreProcess
                ? CheckResult.Successful
                : CheckResult.Failed("препроц");
        }
    }
}
