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

                string bandName = line.Substring(2);
                _band = new Band
                {
                    name = bandName,
                    path = _rootPath + $"\\{bandName}",
                    albuns = new List<Album>(),
                    linkSingles = new List<string>()
                };
            }
            else if(line.StartsWith("- "))
            {
                string? albumLink;
                if((albumLink = songList.ReadLine()) == null)
                    break;

				string albumName = line.Substring(2);
                Album _album = new Album
                {
                    name = albumName,
                    path = _band.path + $"\\{albumName}",
                    link = albumLink
                };

                _band.linkSingles.AddRange(GetSinglesLink(songList));
                _band.albuns.Add(_album);
            }
        }
        songList.Close();

        Console.WriteLine("--Finished--");
        string songsInBatch = string.Empty;

        List<Album> allAlbuns = new List<Album>();
        foreach(Band band in bands)
        {
            foreach(Album album in band.albuns)
            {
                allAlbuns.Add(album);
            }
        }

        for(int i = 0; i + _maxThreads < allAlbuns.Count; i += _maxThreads) 
        {

            for(int j = 0; j < _maxThreads; j++)
            {
                songsInBatch += $"\n\t{allAlbuns[j + i].path}";
                _processInfo.Arguments = $" --hls-prefer-ffmpeg -x --audio-format mp3 -o \"{allAlbuns[j + i].path}\\%(title)s.%(ext)s\" \"{allAlbuns[j + i].link}\"";
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
			foreach (string single in band.linkSingles)
			{
				Console.WriteLine(single);
			}
		}
	}

    private List<string> GetSinglesLink(StreamReader songList)
    {
        List<string> _singles = new List<string>();

        string line;
        while((line = songList.ReadLine()!) != null)
        {
            if ((Char)songList.Peek() == ':' || (Char)songList.Peek() == '-')
                break;
			if (string.IsNullOrEmpty(line))
				continue;
			_singles.Add(line);
        }
        return _singles;
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
		public List<string> linkSingles;
	}
}
