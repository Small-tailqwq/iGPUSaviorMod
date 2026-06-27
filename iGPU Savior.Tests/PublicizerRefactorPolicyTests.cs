using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

namespace IGPUSavior.Tests
{
    public class PublicizerRefactorPolicyTests
    {
        [Theory]
        [InlineData("Patches", "CostumePatch.cs", "GetMethod(")]
        [InlineData("Patches", "CostumePatch.cs", "FieldInfo")]
        [InlineData("Patches", "CostumePatch.cs", "GetField(")]
        [InlineData("Patches", "CostumePatch.cs", "GetValue(")]
        [InlineData("Patches", "InputPatches.cs", "FieldInfo")]
        [InlineData("Patches", "InputPatches.cs", "GetField(")]
        [InlineData("Patches", "InputPatches.cs", "GetValue(")]
        [InlineData("Patches", "InputPatches.cs", "Assembly.Load")]
        [InlineData("Patches", "NoteExportPatch.cs", "AccessTools.Field(")]
        [InlineData("Patches", "NoteExportPatch.cs", "FieldInfo")]
        [InlineData("Patches", "NoteExportPatch.cs", "GetValue(")]
        [InlineData("Patches", "ExitConfirmationDialogHelper.cs", "Assembly.Load")]
        [InlineData("Patches", "ExitConfirmationDialogHelper.cs", "FieldInfo")]
        [InlineData("Patches", "ExitConfirmationDialogHelper.cs", "GetField(")]
        [InlineData("Patches", "ExitConfirmationDialogHelper.cs", "GetValue(")]
        [InlineData("Patches", "NoteDeleteConfirmPatch.cs", "Assembly.Load")]
        [InlineData("Patches", "NoteDeleteConfirmPatch.cs", "FieldInfo")]
        [InlineData("Patches", "NoteDeleteConfirmPatch.cs", "GetMethod(")]
        [InlineData("Patches", "NoteDeleteConfirmPatch.cs", "GetValue(")]
        [InlineData("UI", "ModPulldownCloner.cs", "GetPulldownUIType")]
        [InlineData("UI", "ModPulldownCloner.cs", "GetMethod(")]
        [InlineData("UI", "ModPulldownCloner.cs", "GetField(")]
        [InlineData("UI", "ModPulldownCloner.cs", "SetValue(")]
        [InlineData("UI", "ModSettingsIntegration.cs", "AccessTools.Field(typeof(ModSettingsIntegration)")]
        [InlineData("UI", "ModSettingsIntegration.cs", "AccessTools.Field(typeof(SettingUI)")]
        [InlineData("UI", "ModSettingsIntegration.cs", "GetType().GetMethod(")]
        public void PublicizedGameTypesDoNotUseReflectionHotspots(string folder, string fileName, string forbiddenText)
        {
            var source = ReadSource(folder, fileName);

            Assert.DoesNotContain(forbiddenText, source);
        }

        [Fact]
        public void ObsoleteTypeLookupHelperIsRemoved()
        {
            Assert.False(
                File.Exists(GetSourcePath("Utilities", "TypeHelper.cs")),
                "TypeHelper.cs should have been deleted as part of the publicizer refactor.");
        }

        [Fact]
        public void ModSettingsIntegrationRetainsDynamicFieldDiscovery()
        {
            var source = ReadSource("UI", "ModSettingsIntegration.cs");
            Assert.Contains("AccessTools.GetDeclaredFields", source);
            Assert.Contains("GetValue(", source);
        }

        private static string ReadSource(string folder, string fileName, [CallerFilePath] string testFilePath = "")
        {
            var path = GetSourcePath(folder, fileName, testFilePath);
            Assert.True(File.Exists(path), $"Source file not found: {path}");
            return File.ReadAllText(path);
        }

        private static string GetSourcePath(string folder, string fileName, [CallerFilePath] string testFilePath = "")
        {
            var testsDir = Path.GetDirectoryName(testFilePath)!;
            var repoRoot = Path.GetDirectoryName(testsDir)!;
            return Path.Combine(repoRoot, "iGPU Savior", folder, fileName);
        }
    }
}
