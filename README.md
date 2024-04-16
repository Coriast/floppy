# floppy
floppy - Download youtube musics.

# How to install

### dependencies
youtube-dl - to download the files from the provided url. <br>
ffmpeg - to convert to mp3.

### Installing the dependencies:
```bash
  pip install youtube-dl
  pip install ffmpeg
```
### Building on the project directory:
```bash
  dotnet build
```
# How to use

**floppy** is a CLI program, it receives a file in a [specific format](#file-format) as argument, parses it and downloads the songs inside a songs folder in the user profile directory (users/[your-user] in Windows, /home/[your-user] in Linux), the songs are separated by band and album following the structure of the file.

```
  .\floppy songs.txt
```

# File Format

```
: band name 

- album 1 name
[youtube link to the playlist]

...

- album n name
[youtube link to the playlist]

[if you add a link without any album set before, it'll be downloaded at the band folder]

...

: band n name
...
```

## Some bugs that it has now
- Some album names have a # at the end
