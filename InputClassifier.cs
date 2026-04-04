using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TemplateEdit;
    public enum InputKind
    {
        RemoteUri,
        LocalFile,
        WpfResource,
        ArbitraryText
    }
public record ResolvedInput(
    InputKind Kind,
    string Original,
    string? NormalizedPath,
    Uri? Uri,
    bool Exists
);

public static class InputClassifier
    {
        public static InputKind Classify(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return InputKind.ArbitraryText;

            input = input.Trim();

            // 1. WPF Pack URI
            if (Uri.TryCreate(input, UriKind.Absolute, out var packUri) &&
                packUri.Scheme.StartsWith("pack", StringComparison.OrdinalIgnoreCase))
            {
                return InputKind.WpfResource;
            }

            // 2. Remote URI
            if (Uri.TryCreate(input, UriKind.Absolute, out var remoteUri) &&
                (remoteUri.Scheme == Uri.UriSchemeHttp ||
                 remoteUri.Scheme == Uri.UriSchemeHttps ||
                 remoteUri.Scheme == Uri.UriSchemeFtp))
            {
                return InputKind.RemoteUri;
            }

            // 3. Local file URI (file:///C:/path)
            if (Uri.TryCreate(input, UriKind.Absolute, out var fileUri) &&
                fileUri.IsFile)
            {
                return InputKind.LocalFile;
            }

            // 4. Windows-style paths (C:\..., \\server\share)
            if (Path.IsPathRooted(input))
                return InputKind.LocalFile;

            // 5. Fallback
            return InputKind.ArbitraryText;
        }
    }

public static class InputResolver
{
    public static ResolvedInput Resolve(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new ResolvedInput(InputKind.ArbitraryText, input, null, null, false);

        input = input.Trim();

        // 1. WPF Pack URI
        if (Uri.TryCreate(input, UriKind.Absolute, out var packUri) &&
            packUri.Scheme.StartsWith("pack", StringComparison.OrdinalIgnoreCase))
        {
            bool exists = ResourceExists(packUri);
            return new ResolvedInput(InputKind.WpfResource, input, null, packUri, exists);
        }

        // 2. Remote URI
        if (Uri.TryCreate(input, UriKind.Absolute, out var remoteUri) &&
            (remoteUri.Scheme == Uri.UriSchemeHttp ||
             remoteUri.Scheme == Uri.UriSchemeHttps ||
             remoteUri.Scheme == Uri.UriSchemeFtp))
        {
            return new ResolvedInput(InputKind.RemoteUri, input, remoteUri.AbsoluteUri, remoteUri, true);
        }

        // 3. Local file URI (file:///C:/path)
        if (Uri.TryCreate(input, UriKind.Absolute, out var fileUri) && fileUri.IsFile)
        {
            string localPath = NormalizePath(fileUri.LocalPath);
            bool exists = File.Exists(localPath);
            return new ResolvedInput(InputKind.LocalFile, input, localPath, fileUri, exists);
        }

        // 4. Windows-style paths (C:\..., \\server\share)
        if (Path.IsPathRooted(input))
        {
            string localPath = NormalizePath(input);
            bool exists = File.Exists(localPath);
            return new ResolvedInput(InputKind.LocalFile, input, localPath, new Uri(localPath), exists);
        }

        // 5. Relative path → resolve against application base
        string resolved = ResolveRelative(input);
        if (File.Exists(resolved))
        {
            return new ResolvedInput(InputKind.LocalFile, input, resolved, new Uri(resolved), true);
        }

        // 6. Fallback
        return new ResolvedInput(InputKind.ArbitraryText, input, null, null, false);
    }

    private static string NormalizePath(string path)
    {
        return Path.GetFullPath(path);
    }

    private static string ResolveRelative(string relative)
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        return Path.GetFullPath(Path.Combine(baseDir, relative));
    }

    private static bool ResourceExists(Uri packUri)
    {
        try
        {
            var stream = Application.GetResourceStream(packUri);
            return stream != null;
        }
        catch
        {
            return false;
        }
    }
    public static ImageSource? LoadImage(ResolvedInput input)
    {
        if (input.Kind == InputKind.WpfResource && input.Uri != null)
            return new BitmapImage(input.Uri);

        if (input.Kind == InputKind.LocalFile && input.Exists && input.NormalizedPath != null)
            return new BitmapImage(new Uri(input.NormalizedPath));

        return null;
    }

}

