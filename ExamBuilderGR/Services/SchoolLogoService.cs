using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ExamBuilderGR.Services;

/// <summary>
/// Διαχειρίζεται το λογότυπο του σχολείου. Το επιλεγμένο αρχείο αντιγράφεται
/// σε σταθερό φάκελο της εφαρμογής ώστε να μη χαθεί όταν μετακινηθεί το αρχικό.
/// </summary>
public sealed class SchoolLogoService
{
    private static readonly string[] SupportedExtensions = [".png", ".jpg", ".jpeg", ".bmp"];

    public string AssetsFolder { get; }

    public SchoolLogoService()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "ExamBuilder GR");
        AssetsFolder = Path.Combine(root, "Στοιχεία εφαρμογής");
        Directory.CreateDirectory(AssetsFolder);
    }

    public string Import(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            throw new FileNotFoundException("Το αρχείο λογοτύπου δεν βρέθηκε.", sourcePath);

        var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        if (!SupportedExtensions.Contains(extension))
            throw new InvalidDataException("Υποστηρίζονται αρχεία PNG, JPG, JPEG και BMP.");

        Directory.CreateDirectory(AssetsFolder);
        var destination = Path.Combine(AssetsFolder, $"school-logo{extension}");

        if (string.Equals(Path.GetFullPath(sourcePath), Path.GetFullPath(destination),
                StringComparison.OrdinalIgnoreCase))
            return destination;

        foreach (var oldLogo in Directory.EnumerateFiles(AssetsFolder, "school-logo.*"))
        {
            if (!string.Equals(oldLogo, destination, StringComparison.OrdinalIgnoreCase))
            {
                try { File.Delete(oldLogo); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        File.Copy(sourcePath, destination, overwrite: true);
        if (LoadImage(destination) is null)
        {
            try { File.Delete(destination); } catch { }
            throw new InvalidDataException("Το επιλεγμένο αρχείο δεν είναι έγκυρη εικόνα.");
        }

        return destination;
    }

    public static ImageSource? LoadImage(string? path, bool grayscale = false)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();

            if (!grayscale) return bitmap;

            var converted = new FormatConvertedBitmap(bitmap, PixelFormats.Gray32Float, null, 0);
            converted.Freeze();
            return converted;
        }
        catch
        {
            return null;
        }
    }
}
