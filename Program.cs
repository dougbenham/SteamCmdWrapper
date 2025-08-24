using System.Diagnostics;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace SteamCmdWrapper
{
    internal class Program
    {
	    private static readonly Regex _scriptArgRegex = new(@"\+runscript ""(.+)""");
	    private static readonly Regex _forceInstallDirRegex = new(@"force_install_dir ""(.+)""");
        private const string Core = "core";
	    private const string SteamCmd = "steamcmd.exe";

	    static void MoveDirectory(string sourceDir, string destinationDir)
	    {
		    var dir = new DirectoryInfo(sourceDir);
			if (!dir.Exists)
			    throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");
			
			var dirs = dir.GetDirectories(); // cache before, in case we are moving to a subfolder of source folder

		    Directory.CreateDirectory(destinationDir);
			
		    foreach (FileInfo file in dir.GetFiles())
		    {
			    string targetFilePath = Path.Combine(destinationDir, file.Name);
			    Microsoft.VisualBasic.FileIO.FileSystem.MoveFile(file.FullName, targetFilePath);
		    }

		    foreach (DirectoryInfo subDir in dirs)
		    {
			    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
			    Microsoft.VisualBasic.FileIO.FileSystem.MoveDirectory(subDir.FullName, newDestinationDir);
		    }
	    }

	    static void Main(string[] args)
        {
			try
	        {
		        var processDirectory = Path.GetDirectoryName(Environment.ProcessPath);
		        var processFileName = Path.GetFileName(Environment.ProcessPath);
		        if (processFileName != SteamCmd)
		        {
			        if (!File.Exists($"{processDirectory}\\{SteamCmd}"))
			        {
				        throw new Exception($"{SteamCmd} was not found");
			        }

					Console.WriteLine($@"Moving {SteamCmd} to \{Core}\ subfolder and setting up symlink..");

					MoveDirectory(processDirectory, $@"{processDirectory}\{Core}");

			        File.Move($"{processDirectory}\\{Core}\\{processFileName}", $"{processDirectory}\\{SteamCmd}");
			        return;
		        }
				else if (!File.Exists($"{processDirectory}\\{Core}\\{SteamCmd}"))
			        throw new Exception($@"Invalid state, running process is {SteamCmd} but original {SteamCmd} was not found in \{Core}\ subfolder");

                Match? scriptMatch = null;
                foreach (var arg in args)
                {
	                scriptMatch = _scriptArgRegex.Match(arg);
	                if (scriptMatch.Success)
		                break;
                }
                if (scriptMatch == null)
	                throw new Exception($"Missing required argument (\"{_scriptArgRegex}\")");

                var scriptPath = scriptMatch.Groups[1].Value;
                if (!File.Exists(scriptPath))
                    throw new Exception($"Script file not found: {scriptPath}");
                var script = File.ReadAllLines(scriptPath);
				
                Match? forceInstallDirMatch = null;
                foreach (var arg in script)
                {
	                forceInstallDirMatch = _forceInstallDirRegex.Match(arg);
	                if (forceInstallDirMatch.Success)
		                break;
                }
                if (forceInstallDirMatch == null)
	                throw new Exception($"Missing required script arg (\"{_forceInstallDirRegex}\")");

                var contentDir = $@"{forceInstallDirMatch.Groups[1].Value}\steamapps\workshop\content";
		        var datetime = $"Backup-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}";
				var appIds = new HashSet<string>();
				var tempDir = Path.GetTempPath();

		        foreach (var arg in script)
		        {
                    if (arg.StartsWith("workshop_download_item "))
			        {
				        var split = arg.Split(' ');
				        var appId = split[1];
				        var itemId = split[2];
				        var appDir = $@"{contentDir}\{appId}";
				        var itemDir = $@"{appDir}\{itemId}";
						if (Directory.Exists(itemDir))
				        {
					        Console.WriteLine($@"Backing up {appId}\{itemId}..");

					        if (!Directory.Exists($@"{tempDir}\{datetime}"))
						        Directory.CreateDirectory($@"{tempDir}\{datetime}");

							Directory.Move(itemDir, $@"{tempDir}\{datetime}\{itemId}");

							appIds.Add(appId);
				        }
			        }
		        }

		        foreach (var appId in appIds)
		        {
			        Console.WriteLine($"Zipping backed up items for {appId} to {datetime}.zip..");

			        var appDir = $@"{contentDir}\{appId}";
			        ZipFile.CreateFromDirectory($@"{tempDir}\{datetime}", $@"{appDir}\{datetime}.zip", CompressionLevel.Optimal, false);
			        Directory.Delete($@"{tempDir}\{datetime}", true);
		        }
				
		        var process = Process.Start($@"{processDirectory}\{Core}\{SteamCmd}", args);
		        process.WaitForExit();

				if (!Directory.Exists($@"{processDirectory}\logs"))
					new DirectoryInfo($@"{processDirectory}\logs").CreateAsSymbolicLink($@"{Core}\logs");
	        }
	        catch (Exception ex)
	        {
		        Console.WriteLine(ex.ToString());
		        Console.ReadLine();
	        }
        }
    }
}