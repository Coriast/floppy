using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

namespace floppy.src;

public struct Options
{
    public int maxThreads;
}

public class Floppy
{
    private string _filePath;
    private string _rootPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\songs";
    private int _maxThreads = 3;
    private string youtubeCommand = "youtube-dl";

    public Floppy(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine("File does not exists.");
            Environment.Exit(0);
        }
        _filePath = filePath;
	}

    public void Run()
    {
        StreamReader songList = File.OpenText(_filePath);

        List<Band> bands = new List<Band>();
        Band? _band = null;
        string line;
        while ((line = songList.ReadLine()!) != null)
        {
            if (string.IsNullOrEmpty(line))
                continue;

            if(line.StartsWith(": "))
            {
                if (_band != null)
                    bands.Add(_band);

                string bandName = line.Substring(2).Replace(".", "");
                _band = new Band
                {
                    name = bandName,
                    path = _rootPath + $"\\{bandName}",
                    albuns = new List<Album>(),
                    singles = new List<Album>()
                };
				UpdateSingles(songList, _band);
			}
            else if(line.StartsWith("- "))
            {
                if (_band == null)
                    continue;

				string? albumLink;
                if((albumLink = songList.ReadLine()) == null)
                    break;

				string albumName = line.Substring(2).Replace(".", "");
                Album _album = new Album
                {
                    name = albumName,
                    path = _band.path + $"\\{albumName}",
                    link = albumLink
                };
				_band.albuns.Add(_album);
				UpdateSingles(songList, _band);
            }
            if (songList.Peek() == -1 && _band != null)
                bands.Add(_band);
		}
        songList.Close();

        List<Album> downloads = new List<Album>();

        bands.ForEach(band =>
        {
            downloads.AddRange(band.albuns);
            downloads.AddRange(band.singles);
        });

        Console.WriteLine("--Finished--");
        Parallel.ForEach(downloads, new ParallelOptions { MaxDegreeOfParallelism = _maxThreads }, download =>
        {
            var process = Process.Start(new ProcessStartInfo
			{
				CreateNoWindow = false,
				UseShellExecute = true,
				WindowStyle = ProcessWindowStyle.Normal,
				FileName = youtubeCommand,
                Arguments = $" -x --hls-prefer-ffmpeg --audio-format wav --audio-quality 0 -o \"{download.path}\\%(title)s.%(ext)s\" \"{download.link}\""
			});
            process.WaitForExit();
            Console.WriteLine($"{download.name} in {download.path}");
        });
    }

    private void PrintBands(List<Band> bands)
    {
		foreach (Band band in bands)
		{
			Console.WriteLine(band.name);
			Console.WriteLine(band.path);
			foreach (Album album in band.albuns)
			{
				Console.WriteLine("\t" + album.name);
				Console.WriteLine("\t" + album.path);
			}
			foreach (Album single in band.singles)
			{
				Console.WriteLine("\t" + single.name);
				Console.WriteLine("\t" + single.path);
			}
		}
	}

    private void UpdateSingles(StreamReader songList, Band band)
    {
        string line;
        while((line = songList.ReadLine()!) != null)
        {
            if ((Char)songList.Peek() == ':' || (Char)songList.Peek() == '-')
                break;
			if (string.IsNullOrEmpty(line))
				continue;

            band.singles.Add(new Album {
                name = band.name,
                path = band.path,
                link = line
            });
        }
    }
	class Album
	{
		public string name = string.Empty;
		public string path = string.Empty;
		public string link = string.Empty;
	}

	class Band
	{
		public string name = string.Empty;
		public string path = string.Empty;
		public List<Album> albuns = new List<Album>();
		public List<Album> singles = new List<Album>();
	}
}
