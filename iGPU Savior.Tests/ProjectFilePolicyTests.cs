using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Xunit;

namespace IGPUSavior.Tests
{
    public class ProjectFilePolicyTests
    {
        [Fact]
        public void PublicizerPackageVersionIsPinned()
        {
            var document = LoadProjectFile();
            var package = FindPublicizerPackage(document);

            Assert.Equal("0.4.3", (string)package.Attribute("Version"));
        }

        [Fact]
        public void RuntimeReferencesAreNotCopiedToPluginOutput()
        {
            var document = LoadProjectFile();
            var references = document.Descendants("Reference")
                .Where(element => element.Element("HintPath") != null)
                .ToArray();

            Assert.NotEmpty(references);
            Assert.All(references, reference => Assert.Equal("false", (string)reference.Attribute("Private")));
        }

        [Fact]
        public void PublicizerPackageDoesNotCopyBuildDependenciesToOutput()
        {
            var document = LoadProjectFile();
            var package = FindPublicizerPackage(document);

            var assets = ((string)package.Element("IncludeAssets"))
                .Split(';')
                .Select(asset => asset.Trim())
                .OrderBy(asset => asset)
                .ToArray();

            Assert.Equal(new[] { "build", "contentfiles" }, assets);
            Assert.Equal("all", (string)package.Element("PrivateAssets"));
        }

        private static XElement FindPublicizerPackage(XDocument document)
        {
            var package = document.Descendants("PackageReference")
                .SingleOrDefault(element => (string)element.Attribute("Include") == "BepInEx.AssemblyPublicizer.MSBuild");
            Assert.NotNull(package);
            return package;
        }

        private static XDocument LoadProjectFile([CallerFilePath] string testFilePath = "")
        {
            var testsDir = Path.GetDirectoryName(testFilePath)!;
            var repoRoot = Path.GetDirectoryName(testsDir)!;
            var projectFile = Path.Combine(repoRoot, "iGPU Savior", "iGPU Savior.csproj");
            return XDocument.Load(projectFile);
        }
    }
}
