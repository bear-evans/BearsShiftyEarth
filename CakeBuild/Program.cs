using System;
using System.IO;
using Cake.Common;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Clean;
using Cake.Common.Tools.DotNet.Publish;
using Cake.Core;
using Cake.Frosting;
using Cake.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;

namespace CakeBuild
{
    public static class Program
    {
        #region Methods

        public static int Main(string[] args)
        {
            return new CakeHost()
                .UseContext<BuildContext>()
                .Run(args);
        }

        #endregion Methods
    }

    public class BuildContext : FrostingContext
    {
        #region Properties

        public string BuildConfiguration { get; }
        public string Version { get; }
        public string Name { get; }
        public bool SkipJsonValidation { get; }

        #endregion Properties

        #region Fields

        public const string ProjectName = "BearsShiftyEarth";

        #endregion Fields

        #region Constructors

        public BuildContext(ICakeContext context)
            : base(context)
        {
            BuildConfiguration = context.Argument("configuration", "Release");
            SkipJsonValidation = context.Argument("skipJsonValidation", false);
            ModInfo modInfo = context.DeserializeJsonFromFile<ModInfo>($"../{ProjectName}/modinfo.json");
            Version = modInfo.Version;
            Name = modInfo.ModID;
        }

        #endregion Constructors
    }

    [TaskName("ValidateJson")]
    public sealed class ValidateJsonTask : FrostingTask<BuildContext>
    {
        #region Methods

        public override void Run(BuildContext context)
        {
            if (context.SkipJsonValidation) {
                return;
            }
            Cake.Core.IO.FilePathCollection jsonFiles = context.GetFiles($"../{BuildContext.ProjectName}/assets/**/*.json");
            foreach (Cake.Core.IO.FilePath file in jsonFiles) {
                try {
                    var json = File.ReadAllText(file.FullPath);
                    _ = JToken.Parse(json);
                }
                catch (JsonException ex) {
                    throw new Exception($"Validation failed for JSON file: {file.FullPath}{Environment.NewLine}{ex.Message}", ex);
                }
            }
        }

        #endregion Methods
    }

    [TaskName("Build")]
    [IsDependentOn(typeof(ValidateJsonTask))]
    public sealed class BuildTask : FrostingTask<BuildContext>
    {
        #region Methods

        public override void Run(BuildContext context)
        {
            context.DotNetClean($"../{BuildContext.ProjectName}/{BuildContext.ProjectName}.csproj",
                new DotNetCleanSettings {
                    Configuration = context.BuildConfiguration
                });

            context.DotNetPublish($"../{BuildContext.ProjectName}/{BuildContext.ProjectName}.csproj",
                new DotNetPublishSettings {
                    Configuration = context.BuildConfiguration
                });
        }

        #endregion Methods
    }

    [TaskName("Package")]
    [IsDependentOn(typeof(BuildTask))]
    public sealed class PackageTask : FrostingTask<BuildContext>
    {
        #region Methods

        public override void Run(BuildContext context)
        {
            context.EnsureDirectoryExists("../Releases");
            context.CleanDirectory("../Releases");
            context.EnsureDirectoryExists($"../Releases/{context.Name}");
            context.CopyFiles($"../{BuildContext.ProjectName}/bin/{context.BuildConfiguration}/Mods/mod/publish/*", $"../Releases/{context.Name}");
            if (context.DirectoryExists($"../{BuildContext.ProjectName}/assets")) {
                context.CopyDirectory($"../{BuildContext.ProjectName}/assets", $"../Releases/{context.Name}/assets");
            }
            context.CopyFile($"../{BuildContext.ProjectName}/modinfo.json", $"../Releases/{context.Name}/modinfo.json");
            if (context.FileExists($"../{BuildContext.ProjectName}/modicon.png")) {
                context.CopyFile($"../{BuildContext.ProjectName}/modicon.png", $"../Releases/{context.Name}/modicon.png");
            }
            context.Zip($"../Releases/{context.Name}", $"../Releases/{context.Name}_{context.Version}.zip");
        }

        #endregion Methods
    }

    [TaskName("Default")]
    [IsDependentOn(typeof(PackageTask))]
    public class DefaultTask : FrostingTask
    {
    }
}