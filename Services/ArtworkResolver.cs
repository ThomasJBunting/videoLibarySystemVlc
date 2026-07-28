using System.ComponentModel;
using System.Diagnostics;
using VideoLibrarySystemVlc.Models;

namespace VideoLibrarySystemVlc.Services;

public sealed class ArtworkResolver
{
    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp"];
    private static readonly string[] PreferredImageNames = ["poster", "folder", "cover", "art", "thumb", "thumbnail"];
    private const string GeneratedThumbnailFileName = ".vls-thumbnail.jpg";

    private readonly FfmpegLocator ffmpegLocator = new();

    public void PopulateArtwork(IEnumerable<MediaItem> items)
    {
        foreach (var item in items)
        {
            var folderArtwork = FindFolderArtwork(item);
            if (!string.IsNullOrWhiteSpace(folderArtwork))
            {
                item.ArtworkPath = folderArtwork;
                continue;
            }

            if (!string.IsNullOrWhiteSpace(item.ArtworkPath) && File.Exists(item.ArtworkPath))
            {
                continue;
            }

            item.ArtworkPath = TryGenerateThumbnail(item);
        }
    }

    private static string? FindFolderArtwork(MediaItem item)
    {
        foreach (var folder in EnumerateCandidateFolders(item))
        {
            if (!Directory.Exists(folder))
            {
                continue;
            }

            try
            {
                var preferred = Directory.EnumerateFiles(folder)
                    .FirstOrDefault(file =>
                    {
                        var name = Path.GetFileNameWithoutExtension(file);
                        var extension = Path.GetExtension(file);
                        return ImageExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase) &&
                               PreferredImageNames.Any(prefix => name.Contains(prefix, StringComparison.OrdinalIgnoreCase));
                    });

                if (preferred is not null)
                {
                    return preferred;
                }

                var anyImage = Directory.EnumerateFiles(folder)
                    .FirstOrDefault(file =>
                        ImageExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase) &&
                        !string.Equals(Path.GetFileName(file), GeneratedThumbnailFileName, StringComparison.OrdinalIgnoreCase));
                if (anyImage is not null)
                {
                    return anyImage;
                }

                var generatedImage = Directory.EnumerateFiles(folder)
                    .FirstOrDefault(file =>
                        string.Equals(Path.GetFileName(file), GeneratedThumbnailFileName, StringComparison.OrdinalIgnoreCase));
                if (generatedImage is not null)
                {
                    return generatedImage;
                }
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }
            catch (IOException)
            {
                continue;
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateCandidateFolders(MediaItem item)
    {
        yield return item.ContainerPath;

        var parent = Directory.GetParent(item.ContainerPath);
        if (parent is not null)
        {
            yield return parent.FullName;
        }
    }

    private string? TryGenerateThumbnail(MediaItem item)
    {
        var ffmpeg = ffmpegLocator.Resolve();
        if (ffmpeg is null)
        {
            return null;
        }

        var source = item.PrimaryPath;
        if (string.IsNullOrWhiteSpace(source) || !File.Exists(source))
        {
            return null;
        }

        Directory.CreateDirectory(item.ContainerPath);
        var artworkPath = Path.Combine(item.ContainerPath, GeneratedThumbnailFileName);
        if (File.Exists(artworkPath))
        {
            return artworkPath;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpeg,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            startInfo.ArgumentList.Add("-y");
            startInfo.ArgumentList.Add("-ss");
            startInfo.ArgumentList.Add("00:00:05");
            startInfo.ArgumentList.Add("-i");
            startInfo.ArgumentList.Add(source);
            startInfo.ArgumentList.Add("-frames:v");
            startInfo.ArgumentList.Add("1");
            startInfo.ArgumentList.Add("-q:v");
            startInfo.ArgumentList.Add("2");
            startInfo.ArgumentList.Add(artworkPath);

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return null;
            }

            process.OutputDataReceived += (_, _) => { };
            process.ErrorDataReceived += (_, _) => { };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            if (!process.WaitForExit(15000))
            {
                try
                {
                    process.Kill(true);
                }
                catch
                {
                }

                process.WaitForExit(2000);
            }

            if (process.ExitCode != 0 || !File.Exists(artworkPath))
            {
                if (File.Exists(artworkPath))
                {
                    File.Delete(artworkPath);
                }

                return null;
            }

            return artworkPath;
        }
        catch (Win32Exception)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}
