using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace TemplateEdit;
public interface IFileDialogService
{
    string? OpenFile(string filter = "All files (*.*)|*.*", bool multiselect = false);
    string? SaveFile(string filter = "All files (*.*)|*.*");
}

public class FileDialogService : IFileDialogService
{
    public string? OpenFile(string filter = "All files (*.*)|*.*", bool multiselect = false)
    {
        var dlg = new OpenFileDialog
        {
            Filter = filter,
            Multiselect = multiselect
        };

        return dlg.ShowDialog() == true
            ? (multiselect ? string.Join(";", dlg.FileNames) : dlg.FileName)
            : null;
    }

    public string? SaveFile(string filter = "All files (*.*)|*.*")
    {
        var dlg = new SaveFileDialog
        {
            Filter = filter
        };

        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }
}
public class FileOpenViewModel
{
    public string Filter { get; set; } = "All files (*.*)|*.*";
    
    private readonly IFileDialogService _fileDialogService;
    public string? FilePath { get; set; }
    public FileOpenViewModel(IFileDialogService fileDialogService)
    {
        _fileDialogService = fileDialogService;
    }

    public string? SelectFileCommand() => FilePath = _fileDialogService.OpenFile("Text files (*.txt)|*.txt|All files (*.*)|*.*");
}
