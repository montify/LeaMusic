using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LeaMusicGui.Views.DialogServices
{
    public class DialogService : IDialogService
    {
        public string? OpenFile(string filter)
        {
            var dialog = new OpenFileDialog { Filter = filter };
            return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;

        }
        
        public string? Save()
        {
            var saveDialog = new FolderBrowserDialog();
            return saveDialog.ShowDialog() == DialogResult.OK ? saveDialog.SelectedPath : null;
        }
    }
}
