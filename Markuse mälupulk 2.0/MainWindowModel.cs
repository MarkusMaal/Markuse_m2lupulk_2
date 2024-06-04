using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Markuse_mälupulk_2._0;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Markuse_mälupulk_2_0
{
    public class MainWindowModel
    {
        public ObservableCollection<Folder> Folders { get; set; }
        public string url;
        public string toptext;
        public string info;
        public string infotopic;

        public MainWindowModel()
        {
            Folders =
            [
                new() {Name = ".", Type = "Kataloog*", Image = GetResource("Kaust")},
                new() {Name = "..", Type = "Kataloog*", Image = GetResource("Kaust")}
            ];
            this.url = "";
            this.toptext = "";
            this.info = "";
            this.infotopic = "";
        }

        public static Bitmap GetResource(string name)
        {
            object? resource = App.Current?.Resources[name];
            if (resource == null)
            {
                return new Bitmap(".");
            } else
            {
                Bitmap? bmp = (Bitmap?)((Image?)resource)?.Source;
                return bmp ?? new Bitmap(".");
            }
        }

        public static string GetTypeDescription(string ext)
        {
            string description = ext + " fail";
            switch (ext)
            {
                case "dll":
                    return "Rakenduse laiend";
                case "sys":
                    return "Süsteemifail";
                case "cnf":
                    return "Markuse asjade konfiguratsioon";
                case "ini":
                    return "Konfiguratsioonisätted";
                case "reg":
                    return "Registriväärtused";
                case "chm":
                    return "Spikrifail";
                case "nfo":
                    return "Infofail";
                case "com":
                    return "MS-DOS rakendus";
                case "mp3":
                    return "MP3 helifail";
                case "wav":
                    return "WAV helifail";
                case "ogg":
                    return "OGG helifail";
                case "wma":
                    return "WMA helifail";
                case "asf":
                    return "ASF video";
                case "wmv":
                    return "Windows Media video";
                case "mp4":
                    return "MP4 video";
                case "mpg":
                    return "MPEG video";
                case "aiff":
                    return "AIFF helifail";
                case "flac":
                    return "FLAC helifail";
                case "xm":
                    return "XM helirada";
                case "mod":
                    return "MOD helirada";
                case "au":
                    return "Heli";
                case "sf2":
                    return "SF2 helirada";
                case "mid":
                    return "MIDI jada";
                case "alac":
                    return "ALAC helirada";
                case "ogv":
                    return "OGG video";
                case "avi":
                    return "AVI video";
                case "vob":
                    return "DVD videofail";
                case "txt":
                    return "Tekstifail";
                case "inf":
                    return "Installiteave";
                case "bat":
                    return "MS-DOSi ja Windowsi pakkfail";
                case "cmd":
                    return "Windowsi pakkfail";
                case "nt":
                    return "Windowsi pakkfail";
                case "sh":
                    return "UNIX skriptifail";
                case "cfg":
                    return "Markuse asjad konfiguratsioonifail";
                case "asm":
                    return "Assembler kood";
                case "js":
                    return "Javascript";
                case "css":
                    return "Kaskaadlaadistikud";
                case "html":
                    return "HTML dokument";
                case "htm":
                    return "HTML dokument";
                case "log":
                    return "Logifail";
                case "prf":
                    return "Reeglite loend";
                case "xml":
                    return "Ulatuslik märgistikukeel (XML)";
                case "iso":
                    return "Kettatõmmis";
                case "cue":
                    return "CD-plaadi sessiooniteave";
                case "bup":
                    return "IFO varukoopia";
                case "img":
                    return "Kettatõmmis";
                case "ima":
                    return "Kettatõmmis";
                case "vhd":
                    return "Kettatõmmis";
                case "vmdk":
                    return "Kettatõmmis";
                case "vdi":
                    return "Kettatõmmis";
                case "ico":
                    return "Ikoon";
                case "cur":
                    return "Kursor";
                case "ani":
                    return "Animeeritud kursor";
                case "exe":
                    return "Windowsi rakendus";
                default:
                    break;
            }
            return description;
        }

        public static Bitmap GetTypeIcon(string ext)
        {
            switch (ext)
            {
                case "dll":
                    return GetResource("Mutrid");
                case "sys":
                    return GetResource("Mutrid");
                case "cnf":
                    return GetResource("Mutrid");
                case "ini":
                    return GetResource("Mutrid");
                case "reg":
                    return GetResource("Mutrid");
                case "chm":
                    return GetResource("Spikker");
                case "hlp":
                    return GetResource("Spikker");
                case "nfo":
                    return GetResource("Spikker");
                case "com":
                    return GetResource("Rakendused");
                case "mp3":
                    return GetResource("Esitusnupp");
                case "wav":
                    return GetResource("Esitusnupp");
                case "ogg":
                    return GetResource("Esitusnupp");
                case "wma":
                    return GetResource("Esitusnupp");
                case "asf":
                    return GetResource("Esitusnupp");
                case "wmv":
                    return GetResource("Esitusnupp");
                case "mp4":
                    return GetResource("Esitusnupp");
                case "mpg":
                    return GetResource("Esitusnupp");
                case "aiff":
                    return GetResource("Esitusnupp");
                case "flac":
                    return GetResource("Esitusnupp");
                case "xm":
                    return GetResource("Esitusnupp");
                case "mod":
                    return GetResource("Esitusnupp");
                case "au":
                    return GetResource("Esitusnupp");
                case "sf2":
                    return GetResource("Esitusnupp");
                case "mid":
                    return GetResource("Esitusnupp");
                case "alac":
                    return GetResource("Esitusnupp");
                case "ogv":
                    return GetResource("Esitusnupp");
                case "avi":
                    return GetResource("Esitusnupp");
                case "vob":
                    return GetResource("Esitusnupp");
                case "txt":
                    return GetResource("Märkmik");
                case "inf":
                    return GetResource("Märkmik");
                case "bat":
                    return GetResource("Märkmik");
                case "cmd":
                    return GetResource("Märkmik");
                case "nt":
                    return GetResource("Märkmik");
                case "sh":
                    return GetResource("Märkmik");
                case "cfg":
                    return GetResource("Märkmik");
                case "asm":
                    return GetResource("Märkmik");
                case "js":
                    return GetResource("Märkmik");
                case "css":
                    return GetResource("Märkmik");
                case "html":
                    return GetResource("Märkmik");
                case "htm":
                    return GetResource("Märkmik");
                case "log":
                    return GetResource("Märkmik");
                case "prf":
                    return GetResource("Märkmik");
                case "xml":
                    return GetResource("Märkmik");
                case "iso":
                    return GetResource("Plaat");
                case "cue":
                    return GetResource("Plaat");
                case "bup":
                    return GetResource("Plaat");
                case "img":
                    return GetResource("Diskett");
                case "ima":
                    return GetResource("Diskett");
                case "vhd":
                    return GetResource("Kõvaketas");
                case "vmdk":
                    return GetResource("Kõvaketas");
                case "vdi":
                    return GetResource("Kõvaketas");
                case "ico":
                    return GetResource("Tundmatu");
                case "cur":
                    return GetResource("Tundmatu");
                case "ani":
                    return GetResource("Tundmatu");
                case "exe":
                    return GetResource("Tundmatu");
                default:
                    break;
            }
            return GetResource("Tundmatu");
        }

        public void Up(string url)
        {
            DirectoryInfo d = new(url);
            Navigate(d.Parent != null ? d.Parent.FullName : url);
        }

        public void Navigate(string url) {
            Folders = [];
            this.url = url;
            if ((File.Exists(url)) || (this.url.EndsWith('.') && !this.url.EndsWith("..")))
            {
                Process p = new();
                p.StartInfo.FileName = url;
                p.StartInfo.UseShellExecute = true;
                p.Start();
                if (url.EndsWith("."))
                {
                    url = url[..^1]; // remove last char
                }
                else
                {
                    Up(url);
                }
            }
            Folders.Add(new Folder() { Image = GetResource("Kaust"), Name = ".", Type = "Kataloog*" });
            Folders.Add(new Folder() { Image = GetResource("Kaust"), Name = "..", Type = "Kataloog*" });
            if (Directory.Exists(url))
            {
                DirectoryInfo folder = new(url);

                foreach (DirectoryInfo directoryInfo in folder.GetDirectories())
                {
                    if (directoryInfo.Name.Contains(' '))
                    {
                        if (directoryInfo.Name.EndsWith("---"))
                        {
                            this.toptext = directoryInfo.Name.Replace("-", "").Replace(" ", "");
                            continue;
                        }
                        if (directoryInfo.Name.Contains("Mine"))
                        {
                            continue;
                        }
                    }
                    Folder f = new() { Name = directoryInfo.Name, Type = "Kataloog", Image = GetResource("Kaust") };
                    Folders.Add(f);
                }
                foreach (FileInfo file in folder.GetFiles())
                {
                    if (file.Name.StartsWith("Mis on ") && file.Extension.Equals(".txt"))
                    {
                        this.info = string.Join("\n", File.ReadAllText(file.FullName).Split("\r\n").Skip(5));
                        this.infotopic = file.Name.Replace(".txt", "");
                        continue;
                    }
                    if (file.Name.Equals(".directory") || file.Name.ToLower().Equals("desktop.ini"))
                    {
                        continue;
                    }
                    string extension = file.Extension;
                    if (extension.Length > 0)
                    {
                        extension = extension[1..];
                    }
                    Folder f = new() { Name = file.Name, Type = GetTypeDescription(extension), Image = GetTypeIcon(extension) };
                    Folders.Add(f);
                }
            } else
            {
                Up(url);
            }
        }
    }
}
