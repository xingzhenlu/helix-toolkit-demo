global using DevExpress.Mvvm.CodeGenerators;
using DevExpress.Mvvm.Native;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace Demo;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();
}

[GenerateViewModel]
public partial class MainWindowViewModel
{
    #region Constructor
    public MainWindowViewModel()
    {
        InitMenus();
    }

    private void InitMenus()
    {
        var allTypes = Assembly.GetExecutingAssembly().GetTypes();
        var modulelist = allTypes.Where(t => t.BaseType == typeof(UserControl) && t.Namespace == "Demo.Modules");
        modulelist.ForEach(t => Menus.Add(new() { DisplayName = t.Name, Module = t }));
    }
    #endregion

    #region Notification Properties
    [GenerateProperty]
    ObservableCollection<ModuleInfo> _Menus = new();

    [GenerateProperty(OnChangedMethod = nameof(OnSelectedMenuChanged))]
    ModuleInfo _SelectedMenu;
    void OnSelectedMenuChanged()
    {
        if (SelectedMenu != null)
        {
            if (Body != null) GC.Collect();
            Body = System.Activator.CreateInstance(SelectedMenu.Module);
        }
        else
        {
            Body = null;
        }
    }

    [GenerateProperty]
    object _Body;
    #endregion
}

public class ModuleInfo
{
    public string DisplayName { get; set; }
    public Type Module { get; set; }
}