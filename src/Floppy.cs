using System.Diagnostics;

namespace floppy.src;

public class Floppy
{
    private string _filePath;
    private string _rootPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\songs";
    private int _maxThreads = 3;
    private Process[] processes; 
    private string youtubeCommand = "youtube-dl";
    private ProcessStartInfo _processInfo;

    public Floppy(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine("File does not exists.");
            Environment.Exit(0);
        }
        _filePath = filePath;

        _processInfo = new ProcessStartInfo
        {
            CreateNoWindow = false,
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Normal,
            FileName = youtubeCommand,
        };
        processes = new Process[_maxThreads];
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

                string bandName = line.Substring(2).Replace('.', ' ');
                _band = new Band
                {
                    name = bandName,
                    path = _rootPath + $"\\{bandName}",
                    albuns = new List<Album>(),
                    singles = new List<Album>()
                };
                
            }
            else if(line.StartsWith("- "))
            {
                if (_band == null)
                    continue;

				string? albumLink;
                if((albumLink = songList.ReadLine()) == null)
                    break;

				string albumName = line.Substring(2).Replace('.', ' ');
                Album _album = new Album
                {
                    name = albumName,
                    path = _band.path + $"\\{albumName}",
                    link = albumLink
                };

                UpdateSingles(songList, _band);
                _band.albuns.Add(_album);
            }
        }
        songList.Close();

        Console.WriteLine("--Finished--");

        List<Album> allAlbuns = new List<Album>();
        foreach(Band band in bands)
        {
            allAlbuns.AddRange(band.albuns);
            allAlbuns.AddRange(band.singles);
        }

        PrintBands(bands);

		string songsInBatch = string.Empty;
        for (int i = 0; i + _maxThreads < allAlbuns.Count; i += _maxThreads)
        {

            for (int j = 0; j < _maxThreads; j++)
            {
                songsInBatch += $"\n\t{allAlbuns[j + i].path}";
                _processInfo.Arguments = $" -x -o \"{allAlbuns[j + i].path}\\%(title)s.%(ext)s\" \"{allAlbuns[j + i].link}\"";
                var process = Process.Start(_processInfo);
                processes[j] = process;
            }

            for (int j = 0; j < _maxThreads; j++)
            {
                processes[j].WaitForExit();
            }

            Console.Write(songsInBatch);
            songsInBatch = string.Empty;
        }

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
		public string name;
		public string path;
		public string link;
	}

	class Band
	{
		public string name;
		public string path;
		public List<Album> albuns;
		public List<Album> singles;
	}
}
