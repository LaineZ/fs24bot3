using fs24bot3.QmmandsProcessors;
using Qmmands;
using System;
using System.Text.RegularExpressions;

namespace fs24bot3.Commands
{
    public sealed class SearchQueryCommands : ModuleBase<SearchCommandProcessor.CustomCommandContext>
    {

        [Command("page")]
        [Checks.PreProcess]
        public void FilterPage(uint page)
        {
            Context.Page = (int)page;
        }

        [Command("max")]
        [Checks.PreProcess]
        public void FilterMaxDepth(uint maxdepth)
        {
            Context.Max = (int)Math.Clamp(maxdepth, 0, 5);
        }

        [Command("limit")]
        [Checks.PreProcess]
        public void FilterLimit(uint limit)
        {
            Context.Limit = (int)Math.Clamp(limit, 1, 5);
        }

        [Command("site")]
        [Checks.PreProcess]
        public void NarrowBySite(string site)
        {
            Context.Site = site;
        }

        [Command("random")]
        public void RandomOption(bool rnd)
        {
            Context.Random = rnd;
        }

        [Command("include")]
        public void FilterInclude(string contains)
        {
            Context.SearchResults.RemoveAll(s => !s.Title.ToLower().Contains(contains.ToLower()) || !s.Description.ToLower().Contains(contains.ToLower()));
        }

        [Command("exclude")]
        public void FilterExclude(string contains)
        {
            Context.SearchResults.RemoveAll(s => s.Title.ToLower().Contains(contains.ToLower()) || s.Description.ToLower().Contains(contains.ToLower()));
        }

        [Command("regex")]
        public void FilterRegex(string regex)
        {
            var reg = new Regex(regex);
            var stuffToRemove = Context.SearchResults.RemoveAll(s => !reg.IsMatch(s.Title) || !reg.IsMatch(s.Description));
        }
    }
}
