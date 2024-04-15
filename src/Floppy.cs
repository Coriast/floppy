using System.Diagnostics;

namespace floppy.src;

public struct Options
{
    public int maxThreads = 2;
    public string inputPath = string.Empty;
    public string outputPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\songs";

	public Options() {}
}

public class Floppy
{
    private string youtubeCommand = "youtube-dl";
    private Options _options;

    public Floppy(string[] args)
    {
		_options = new Options();
		for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("-"))
            {
                if (string.Equals(args[i], "-threads") || string.Equals(args[i], "-t"))
                {
                    _options.maxThreads = int.TryParse(args.ElementAtOrDefault(i+1), out int number) ? number : 2;
                    i++;
                }
                else if(string.Equals(args[i], "-output") || string.Equals(args[i], "-o"))
                {
                    string? _path = args.ElementAtOrDefault(i + 1);
                    if (_path != null)
                        _options.outputPath = _path;
                    i++;
                }

            }
            else
            {
                if (!File.Exists(args[i]))
                {
					Console.WriteLine("File does not exists.");
					Environment.Exit(0);
				}
                _options.inputPath = args[i];
            }
        }
	}

    public void Run()
    {
        StreamReader songList = File.OpenText(_options.inputPath);

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
                    path = _options.outputPath + $"\\{bandName}",
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
        Parallel.ForEach(downloads, new ParallelOptions { MaxDegreeOfParallelism = _options.maxThreads }, download =>
        {
            Process process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                CreateNoWindow = false,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal,
                FileName = youtubeCommand,
                Arguments = $" -x --hls-prefer-ffmpeg --audio-format wav --audio-quality 0 -o \"{download.path}\\%(title)s.%(ext)s\" \"{download.link}\""
            };
			process.Start();
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
