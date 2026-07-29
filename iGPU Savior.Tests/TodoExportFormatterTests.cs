using System;
using System.Collections.Generic;
using PotatoOptimization.Utilities;
using Xunit;

namespace IGPUSavior.Tests
{
    public class TodoExportFormatterTests
    {
        [Fact]
        public void Format_WritesMarkdownCheckboxesAndOptionalDueDate()
        {
            var items = new List<TodoExportItem>
            {
                new TodoExportItem("Pending", false, null),
                new TodoExportItem("Completed", true, new DateTime(2026, 8, 3, 18, 30, 0)),
            };

            string content = TodoExportFormatter.Format(items, ExportLanguage.Chinese);

            Assert.Equal(
                "- [ ] Pending\r\n\r\n- [x] Completed\r\n  - 到期日：2026-08-03",
                content);
        }

        [Fact]
        public void Format_IndentsContinuationLinesAndNormalizesLineEndings()
        {
            var items = new List<TodoExportItem>
            {
                new TodoExportItem("First\rSecond\nThird", false, null),
            };

            string content = TodoExportFormatter.Format(items, ExportLanguage.English);

            Assert.Equal("- [ ] First\r\n  Second\r\n  Third", content);
        }

        [Fact]
        public void Format_AllowsEmptyList()
        {
            string content = TodoExportFormatter.Format(
                Array.Empty<TodoExportItem>(),
                ExportLanguage.English);

            Assert.Equal(string.Empty, content);
        }
    }
}
