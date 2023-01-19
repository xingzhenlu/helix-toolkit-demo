using DevExpress.Mvvm.Native;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Assimp;
using HelixToolkit.Wpf.SharpDX;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;

namespace Demo.Modules;

public partial class UserControl1 : UserControl
{
    public UserControl1() => InitializeComponent();
}

[GenerateViewModel]
public partial class UserControl1ViewModel : IDisposable
{
    public UserControl1ViewModel()
    {
        Camera = new HelixToolkit.Wpf.SharpDX.OrthographicCamera()
        {
            LookDirection = new Vector3D(0, -10, -10),
            Position = new Point3D(0, 10, 10),
            UpDirection = new Vector3D(0, 1, 0),
            FarPlaneDistance = 5000,
            NearPlaneDistance = 0.1f
        };
        EffectsManager ??= new DefaultEffectsManager();
    }

    #region Member fields
    SharpDX.BoundingBox _modelBound = new();
    Viewport3DX _viewport3;
    HelixToolkitScene _scene;
    public IEffectsManager EffectsManager { set; get; }
    #endregion

    #region Notification Properties
    [GenerateProperty]
    HelixToolkit.Wpf.SharpDX.Camera _Camera;

    [GenerateProperty]
    Point3D _ModelCentroid = default;

    [GenerateProperty]
    SceneNodeGroupModel3D _GroupModel = new();

    [GenerateProperty]
    bool _IsLoading = false;
    #endregion

    #region Commands

    /// <summary>
    /// Loaded Event(Use <see cref="EventBindingExtension"/> to implement)
    /// </summary>
    /// <param name="e"></param>
    [GenerateCommand]
    void Viewport3DXLoaded(RoutedEventArgs e)
    {
        _viewport3 = e.Source as Viewport3DX;
        Open3DFile();
    }

    /// <summary>
    /// UnLoaded Event(Use <see cref="EventBindingExtension"/> to implement)
    /// </summary>
    /// <param name="e"></param>
    [GenerateCommand]
    void UserControlUnLoaded(RoutedEventArgs e)
    {
        Dispose();
    }

    [GenerateCommand(CanExecuteMethod = nameof(CanFocusCameraToScene))]
    void FocusCameraToScene()
    {
        var maxWidth = Math.Max(Math.Max(_modelBound.Width, _modelBound.Height), _modelBound.Depth);
        var pos = _modelBound.Center + new SharpDX.Vector3(0, 0, maxWidth);
        Camera.Position = pos.ToPoint3D();
        Camera.LookDirection = (_modelBound.Center - pos).ToVector3D();
        Camera.UpDirection = SharpDX.Vector3.UnitY.ToVector3D();
        if (Camera is HelixToolkit.Wpf.SharpDX.OrthographicCamera orthCam) orthCam.Width = maxWidth;
    }
    bool CanFocusCameraToScene() => Camera != null;
    #endregion

    #region Methods 
    async void Open3DFile()
    {
        var modelfile = new FileInfo(@".\Assets\Substation\scene.gltf");
        if (!modelfile.Exists)
        {
            var modelZip = new FileInfo(@".\Assets\Substation.zip");
            var target = modelZip.FullName.Replace(modelZip.Extension, "");
            System.IO.Compression.ZipFile.ExtractToDirectory(modelZip.FullName, target);
        }
        var syncContext = System.Threading.SynchronizationContext.Current;
        await Task.Run(() =>
        {
            using var loader = new HelixToolkit.SharpDX.Core.Assimp.Importer();
            _scene = loader.Load(modelfile.FullName);
            _scene.Root.Attach(EffectsManager);
            _scene.Root.UpdateAllTransformMatrix();
            if (_scene.Root.TryGetBound(out var bound))
            {
                syncContext.Post((o) => { _modelBound = bound; }, null);
            }
            if (_scene.Root.TryGetCentroid(out var centroid))
            {
                syncContext.Post((o) => { ModelCentroid = centroid.ToPoint3D(); }, null);
            }
            return _scene;
        }).ContinueWith(rs =>
        {
            var _scene = rs.Result;
            GroupModel.Clear();
            if (_scene != null)
            {
                GroupModel.AddNode(_scene.Root);
                FocusCameraToScene();
            }
        }, TaskScheduler.FromCurrentSynchronizationContext());
        IsLoading = !IsLoading;
    }

    ~UserControl1ViewModel()
    {
        Dispose();
    }

    /// <summary>
    /// Dispose resources
    /// </summary>
    private bool disposing = false;
    public void Dispose()
    {
        if (!disposing)
        {
            var effectManager = EffectsManager as IDisposable;
            Disposer.RemoveAndDispose(ref effectManager);
            EffectsManager = null;
            _GroupModel.GroupNode.Traverse().ForEach(x => x.Dispose());
            _GroupModel.Clear();
            _GroupModel = null;
            Camera = null;
            _scene.Root.Dispose();
            _scene.Root.Items.ForEach(x => x.Dispose());
            _scene = null;
            //EffectsManager.Dispose();
            //var effectManager = EffectsManager as IDisposable;
            //Disposer.RemoveAndDispose(ref effectManager);
            //EffectsManager = null;
            _viewport3.EffectsManager = null;
            _viewport3.Items.Clear();
            _viewport3.DataContext = null;
            _viewport3.Dispose();
            _viewport3 = null;
            GC.Collect();
            GC.WaitForFullGCComplete();
            disposing = true;
            GC.SuppressFinalize(this);
        }
    }
    #endregion
}