namespace LeaMusicGui.Views.DialogServices
{
    public interface IDialogService
    {
        string? Save();
        string? OpenFile(string filter);
    }
}
