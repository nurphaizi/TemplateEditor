using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TemplateEdit;

namespace TemplateEdit
{
    /// <summary>
    /// Interaction logic for CreateReport.xaml
    /// </summary>

public enum CrystalReportSection
    {
        [Description("Report Header")]
        ReportHeader,

        [Description("Page Header")]
        PageHeader,

        [Description("Group Header")]
        GroupHeader,

        [Description("Details")]
        Details,

        [Description("Group Footer")]
        GroupFooter,

        [Description("Report Footer")]
        ReportFooter,

        [Description("Page Footer")]
        PageFooter
    }

    public static class EnumExtensions
    {
        public static string GetDescription(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = Attribute.GetCustomAttribute(field!, typeof(DescriptionAttribute)) as DescriptionAttribute;

            return attribute?.Description ?? value.ToString();
        }
    }
  

public class ReportSectionsViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<CrystalReportSection> ReportSections
        {
            get;set;
        }
            = new ObservableCollection<CrystalReportSection>();



        private CrystalReportSection? _selectedSection;
        public CrystalReportSection? SelectedSection
        {
            get => _selectedSection;
            set
            {
                _selectedSection = value;
                OnPropertyChanged(nameof(SelectedSection));
                (RemoveCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }





        // Expose enum values for dynamic menus
        public static Array AllSections => Enum.GetValues(typeof(CrystalReportSection));

        public ICommand AddCommand
        {
            get;
        }
        public ICommand AddSpecificCommand
        {
            get;
        }
        public ICommand RemoveCommand
        {
            get;
        }

        public ReportSectionsViewModel()
        {
            AddCommand = new RelayCommand(_ => AddNew());
            AddSpecificCommand = new RelayCommand(param => AddSpecific(param));
            RemoveCommand = new RelayCommand(_ => Remove(), _ => SelectedSection != null);
        }

        private void AddNew()
        {
            // Default: add first enum value
            var first = (CrystalReportSection)AllSections.GetValue(0);

            ReportSections.Add(first);
            SelectedSection = first;
        }

        private void AddSpecific(object param)
        {
            if (param is CrystalReportSection section)
            {
                ReportSections.Add(section);
                SelectedSection = section;
            }
        }

        private void Remove()
        {
            if (SelectedSection != null)
            {
                ReportSections.Remove(SelectedSection.Value);
                SelectedSection = null;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private RelayCommand setReportSectionCommand;
        public ICommand SetReportSectionCommand => setReportSectionCommand ??= new RelayCommand(SetReportSection);

        private void SetReportSection(object commandParameter)
        {
        }
    }



    public partial class CreateReport : PageFunction<String>, INotifyPropertyChanged
    {
        public ReportSectionsViewModel ReportSections { get; set; } = new ReportSectionsViewModel();
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        private Size  _PageSize;
        public Size PageSize
        {
            get => (Size)_PageSize;
            set
            {
                _PageSize=value;
                NotifyPropertyChanged(nameof(PageSize));
            }
        }
        public CreateReport()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        public CreateReport(Size pageSize)
        {
            InitializeComponent();
            PageSize = pageSize;
            DataContext= this;
        }

        private void Button_Click_Save(object sender, RoutedEventArgs e)
        {
            OnReturn(new ReturnEventArgs<string>(JsonHelper.Serialize<ReportSections>(new TemplateEdit.ReportSections(ReportSections.ReportSections, ReportSections.SelectedSection.Value))));
        }

        private void Button_Click_Cancel(object sender, RoutedEventArgs e)
        {
            OnReturn(null);
        }

        private void reportSections_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
