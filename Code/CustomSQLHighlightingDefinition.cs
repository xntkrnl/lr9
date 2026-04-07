using ICSharpCode.AvalonEdit.Highlighting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lr9.Code
{
    public class CustomSQLHighlightingDefinition : IHighlightingDefinition
    {
        public string Name => "SQL";

        public HighlightingRuleSet MainRuleSet { get; } = new HighlightingRuleSet();

        public IEnumerable<HighlightingColor> NamedHighlightingColors => new List<HighlightingColor>();

        public IDictionary<string, string> Properties => new Dictionary<string, string>();

        public HighlightingRuleSet GetNamedRuleSet(string name) => MainRuleSet;

        public HighlightingColor GetNamedColor(string name) => null!;
    }
}
