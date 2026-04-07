using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;

namespace lr9.Code
{
    public static class SqlHighlighManager
    {
        public static void EnableCommentHighlighting(TextEditor editor)
        {
            var sqlHighlighting = new CustomSQLHighlightingDefinition();

            sqlHighlighting.MainRuleSet.Rules.Insert(0, new HighlightingRule
            {
                Regex = new Regex(@"--.*$", RegexOptions.Multiline),
                Color = new HighlightingColor
                {
                    Foreground = new SimpleHighlightingBrush(Colors.Green)
                }
            });

            sqlHighlighting.MainRuleSet.Rules.Insert(0, new HighlightingRule
            {
                Regex = new Regex(@"/\*.*?\*/", RegexOptions.Singleline),
                Color = new HighlightingColor
                {
                    Foreground = new SimpleHighlightingBrush(Colors.Green)
                }
            });

            editor.SyntaxHighlighting = sqlHighlighting;
        }
    }
}
