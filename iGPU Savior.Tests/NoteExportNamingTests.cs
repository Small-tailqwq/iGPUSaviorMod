using System;
using System.Text.RegularExpressions;
using PotatoOptimization.Utilities;
using Xunit;

namespace IGPUSavior.Tests
{
    public class NoteExportNamingTests
    {
        [Fact]
        public void GenerateExportFileName_RemovesInvalidCharacters_AndAppendsTimestamp()
        {
            string fileName = NoteExportNaming.GenerateExportFileName("  My:/\\*?\"<>| Note  ");

            Assert.Matches(@"^My Note_\d{8}_\d{6}\.txt$", fileName);

            foreach (char invalidChar in "<>:\"/\\|?*")
            {
                Assert.DoesNotMatch(new Regex(Regex.Escape(invalidChar.ToString())), fileName);
            }
        }

        [Fact]
        public void SanitizeTitle_RemovesWindowsInvalidCharacters_AndAsciiControlCharacters()
        {
            string sanitizedTitle = NoteExportNaming.SanitizeTitle(" A<:B\u001fC*?\"/\\|> Z ");

            Assert.Equal("ABC Z", sanitizedTitle);
        }

        [Fact]
        public void GenerateExportFileName_FallsBackToNote_WhenSanitizedTitleIsEmpty()
        {
            string fileName = NoteExportNaming.GenerateExportFileName("   :/\\*?\"<>|   ");

            Assert.Matches(@"^note_\d{8}_\d{6}\.txt$", fileName);
        }

        [Fact]
        public void FormatExportContent_ReturnsTitleBlankLineBody_WithCrLfLineEndings()
        {
            string content = NoteExportNaming.FormatExportContent("Title", "Line 1\nLine 2");

            Assert.Equal("Title\r\n\r\nLine 1\r\nLine 2", content);
        }

        [Fact]
        public void FormatExportContent_AllowsEmptyBody()
        {
            string content = NoteExportNaming.FormatExportContent("Title", string.Empty);

            Assert.Equal("Title\r\n\r\n", content);
        }
    }
}
