using System.Diagnostics;
using System.IO.Compression;

namespace SteamCmdWrapper
{
    internal class Program
    {
	    private const string RequiredArg = "+force_install_dir downloader_helper";
	    private const string Core = "core";
	    private const string SteamCmd = "steamcmd.exe";
	    private const string Download = "downloader_helper";

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

					new DirectoryInfo($@"{processDirectory}\{Download}").CreateAsSymbolicLink($@"{Core}\{Download}");

			        File.Move($"{processDirectory}\\{Core}\\{processFileName}", $"{processDirectory}\\{SteamCmd}");
			        return;
		        }
				else if (!File.Exists($"{processDirectory}\\{Core}\\{SteamCmd}"))
			        throw new Exception($@"Invalid state, running process is {SteamCmd} but original {SteamCmd} was not found in \{Core}\ subfolder");
				
		        if (args.All(s => s != RequiredArg))
		        {
			        throw new Exception($"Missing required argument (\"{RequiredArg}\")");
		        }

		        var contentDir = $@"{processDirectory}\{Download}\steamapps\workshop\content";
		        var datetime = $"Backup-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}";
				var appIds = new HashSet<string>();

		        foreach (var arg in args)
		        {
			        if (arg.StartsWith("+workshop_download_item "))
			        {
				        var split = arg.Split(' ');
				        var appId = split[1];
				        var itemId = split[2];
				        var appDir = $@"{contentDir}\{appId}";
				        var itemDir = $@"{appDir}\{itemId}";
						if (Directory.Exists(itemDir))
				        {
					        Console.WriteLine($@"Backing up {appId}\{itemId}..");

					        if (!Directory.Exists($@"{appDir}\{datetime}"))
						        Directory.CreateDirectory($@"{appDir}\{datetime}");

							Directory.Move(itemDir, $@"{appDir}\{datetime}\{itemId}");

							appIds.Add(appId);
				        }
			        }
		        }

		        foreach (var appId in appIds)
		        {
			        Console.WriteLine($"Zipping backed up items for {appId} to {datetime}.zip..");

			        var appDir = $@"{contentDir}\{appId}";
			        ZipFile.CreateFromDirectory($@"{appDir}\{datetime}", $@"{appDir}\{datetime}.zip", CompressionLevel.Optimal, false);
			        Directory.Delete($@"{appDir}\{datetime}", true);
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