namespace findrbordr_native;

public partial class App : Application
{
    private static readonly string MutexName = "findrbordr_native_SingleInstance";
    private static Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        bool createdNew;
        _mutex = new Mutex(true, MutexName, out createdNew);

        if (!createdNew)
        {
            _mutex = null;
            Shutdown();
            return;
        }

        this.DispatcherUnhandledException += (s, args) =>
        {
            Debug.WriteLine($"[CRASH PREVENTED] Dispatcher Exception: {args.Exception}");
            args.Handled = true;
        };

        MainWindow mainWindow = new MainWindow();
        mainWindow.Show();
    }

    public void ReleaseMutexForRelaunch()
    {
        if (_mutex != null)
        {
            _mutex.ReleaseMutex();
            _mutex.Dispose();
            _mutex = null;
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_mutex != null)
        {
            _mutex.ReleaseMutex();
            _mutex.Dispose();
            _mutex = null;
        }
        base.OnExit(e);
    }
}