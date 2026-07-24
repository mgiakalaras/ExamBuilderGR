using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ExamBuilderGR.Models;

namespace ExamBuilderGR.Services;

public static class ExamImageService
{
    private const int MaxPixelDimension = 2400;

    public static ExamImageAsset CreateFromFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            throw new FileNotFoundException("Το αρχείο εικόνας δεν βρέθηκε.", path);

        var extension = Path.GetExtension(path).ToLowerInvariant();
        if (extension is not ".png" and not ".jpg" and not ".jpeg" and not ".bmp" and not ".gif")
            throw new InvalidOperationException("Υποστηρίζονται αρχεία PNG, JPG, JPEG, BMP και GIF.");

        using var input = File.OpenRead(path);
        var decoder = BitmapDecoder.Create(input, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        if (decoder.Frames.Count == 0)
            throw new InvalidOperationException("Το αρχείο δεν περιέχει έγκυρη εικόνα.");

        BitmapSource source = decoder.Frames[0];
        var maxDimension = Math.Max(source.PixelWidth, source.PixelHeight);
        if (maxDimension > MaxPixelDimension)
        {
            var scale = MaxPixelDimension / (double)maxDimension;
            var transformed = new TransformedBitmap(source, new ScaleTransform(scale, scale));
            transformed.Freeze();
            source = transformed;
        }

        BitmapEncoder encoder;
        string mimeType;
        string normalizedExtension;

        if (extension is ".jpg" or ".jpeg")
        {
            encoder = new JpegBitmapEncoder { QualityLevel = 90 };
            mimeType = "image/jpeg";
            normalizedExtension = ".jpg";
        }
        else
        {
            encoder = new PngBitmapEncoder();
            mimeType = "image/png";
            normalizedExtension = ".png";
        }

        encoder.Frames.Add(BitmapFrame.Create(source));
        using var output = new MemoryStream();
        encoder.Save(output);

        return new ExamImageAsset
        {
            FileName = Path.GetFileNameWithoutExtension(path) + normalizedExtension,
            MimeType = mimeType,
            DataBase64 = Convert.ToBase64String(output.ToArray()),
            PixelWidth = source.PixelWidth,
            PixelHeight = source.PixelHeight,
            WidthCm = 8.0,
            Placement = ExamImagePlacement.AfterText,
            Alignment = ExamImageAlignment.Center,
            ShowInAnswerKey = true
        };
    }

    public static BitmapSource? CreateBitmapSource(string? dataBase64)
    {
        if (string.IsNullOrWhiteSpace(dataBase64)) return null;

        try
        {
            var bytes = Convert.FromBase64String(dataBase64);
            using var stream = new MemoryStream(bytes, writable: false);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }
}
