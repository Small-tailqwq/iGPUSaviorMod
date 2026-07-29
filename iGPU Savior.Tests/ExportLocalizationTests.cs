using PotatoOptimization.Utilities;
using Xunit;

namespace IGPUSavior.Tests
{
    public class ExportLocalizationTests
    {
        [Theory]
        [InlineData(ExportLanguage.English, "Due date: ", "note", "todo")]
        [InlineData(ExportLanguage.Japanese, "期限：", "ノート", "タスク")]
        [InlineData(ExportLanguage.Chinese, "到期日：", "笔记", "待办")]
        public void ExportText_CoversSupportedLanguages(
            ExportLanguage language,
            string dueDateLabel,
            string noteFallback,
            string todoFallback)
        {
            Assert.Equal(dueDateLabel, ExportLocalization.GetDueDatePrefix(language));
            Assert.Equal(noteFallback, ExportLocalization.GetNoteFallbackTitle(language));
            Assert.Equal(todoFallback, ExportLocalization.GetTodoFallbackTitle(language));
        }
    }
}
