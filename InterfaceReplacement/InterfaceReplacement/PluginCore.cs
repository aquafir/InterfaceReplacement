namespace InventoryUI;
/// <summary>
/// This is the main plugin class. When your plugin is loaded, Startup() is called, and when it's unloaded Shutdown() is called.
/// </summary>
[FriendlyName("InventoryUI")]
public class PluginCore : PluginBase
{
    private InterfaceController ui;

    /// <summary>
    /// Assembly directory containing the plugin dll
    /// </summary>
    public static string AssemblyDirectory { get; internal set; }
    protected void FilterSetup(string assemblyDirectory) => AssemblyDirectory = assemblyDirectory;
    private void CharacterFilter_LoginComplete(object sender, EventArgs e) => StartUI();

    /// <summary>
    /// Called when your plugin is first loaded.
    /// </summary>
    protected override void Startup()
    {
        try
        {
            CoreManager.Current.CharacterFilter.LoginComplete += CharacterFilter_LoginComplete;
        }
        catch (Exception ex)
        {
            Log(ex);
        }
    }


    private void StartUI()
    {
        try
        {
            //  CoreManager.Current.Actions.AddChatText($"This is my new decal plugin. CharacterFilter_LoginComplete", 1);
            ui = new InterfaceController();

        }
        catch (Exception ex)
        {
            Log(ex);
        }
    }

    /// <summary>
    /// Called when your plugin is unloaded. Either when logging out, closing the client, or hot reloading.
    /// </summary>
    protected override void Shutdown()
    {
        try
        {
            CoreManager.Current.CharacterFilter.LoginComplete -= CharacterFilter_LoginComplete;

            // clean up our ui view
            ui?.Dispose();
        }
        catch (Exception ex)
        {
            Log(ex);
        }
    }

    #region Logging
    /// <summary>
    /// Log an exception to log.txt in the same directory as the plugin.
    /// </summary>
    /// <param name="ex"></param>
    internal static void Log(Exception ex) => Log(ex.ToString());

    /// <summary>
    /// Log a string to log.txt in the same directory as the plugin.
    /// </summary>
    /// <param name="message"></param>
    internal static void Log(string message)
    {
        try
        {
            File.AppendAllText(System.IO.Path.Combine(AssemblyDirectory, "log.txt"), $"{message}\n");
        }
        catch { }
    }
    #endregion // logging
}
