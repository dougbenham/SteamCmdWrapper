using System.Diagnostics;
using System.IO.Compression;

namespace SteamCmdWrapper
{
    internal class Program
    {
        static void Main(string[] args)
        {
	        try
	        {
		        var contentDir = $@"{AppContext.BaseDirectory}downloader_helper\steamapps\workshop\content";
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
	        }
	        catch (Exception ex)
	        {
		        Console.WriteLine(ex.ToString());
		        Console.ReadLine();
		        return;
	        }

	        Process.Start($@"{AppContext.BaseDirectory}steamcmd-original.exe", args).WaitForExit();
        }
    }
}