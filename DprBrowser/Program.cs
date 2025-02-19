using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using System.Reflection;
using Microsoft.Web.WebView2.Core;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Microsoft.Web.WebView2.Wpf;
using WebView2 = Microsoft.Web.WebView2.WinForms.WebView2;
using System.Diagnostics;
public class DprBrowserForm : Form
{
    private WebView2 webViewControl;
    private string browserDataFolderPath;
    private string iscPwdResetUrl = "https://www.sailpoint.com";  //  "https://www.hhs.gov/";
    private string browserWindowText = "SailPoint DPR";
    private string browserTitleBarIconResource = "DprBrowser.spBrowserTitleBar.ico";

    public DprBrowserForm(string dirPath)
    {
        browserDataFolderPath = dirPath;

        _ = InitializeWebView2Async();

    }

    private Icon GetEmbeddedIcon(string resourceName)
    {
        using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
        {
            if (stream == null)
            {
                throw new Exception($"Resource {resourceName} not found.");
            }
            return new Icon(stream);
        }
    }

    static private void DeleteDataDir(string dirPath)
    {
        try
        {
            Directory.Delete(dirPath, true);
        }
        catch (Exception e)
        {
            string toHex = e.HResult.ToString("X");  //hexadecimal

            //Check and retry for ERROR_SHARING_VIOLATION, ERROR_ACCESS_DENIED
            //https://learn.microsoft.com/en-us/windows/win32/seccrypto/common-hresult-values
            if (toHex == "80070020" || toHex == "80070005")
            {
                //System.Threading.Thread.Sleep(100);
                Directory.Delete(dirPath, true);
            }

        }
    }

    private async Task InitializeWebView2Async()
    {
        this.SuspendLayout();
        this.Name = "DprBrowserForm";

        this.FormBorderStyle = FormBorderStyle.FixedDialog;


        this.TopMost = true;
        this.Text = browserWindowText;

        this.WindowState = FormWindowState.Maximized;

        //if we're not in DEBUG mode, disable unwanted buttons
#if !DEBUG
        this.MinimizeBox = false;
        this.MaximizeBox = false;
#endif

        this.Icon = GetEmbeddedIcon(browserTitleBarIconResource);

        var primaryScreen = Screen.PrimaryScreen;
        this.Location = new System.Drawing.Point(primaryScreen.WorkingArea.Left + (primaryScreen.WorkingArea.Width - this.Width) / 2,
                                             primaryScreen.WorkingArea.Top + (primaryScreen.WorkingArea.Height - this.Height) / 2);
        this.CenterToScreen();

        this.Load += new System.EventHandler(this.DprBrowserForm_Load);

        this.webViewControl = new Microsoft.Web.WebView2.WinForms.WebView2();

        ((System.ComponentModel.ISupportInitialize)(this.webViewControl)).BeginInit();

        webViewControl.Dock = DockStyle.Fill;

        // must create a data folder if running out of a secured folder that can't write like Program Files
        CoreWebView2EnvironmentOptions options = new CoreWebView2EnvironmentOptions()
        {
            // TO DO:
            // Find out what all flags need to be enabled or disabled.
            // https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/webview-features-flags?tabs=dotnetcsharp

            // "BlockInsecurePrivateNetworkRequests" :
            // A situation where web application (in a public environment) tries to make requests to resources,
            // on a private network(such as an internal server or local device).

            // "block-new-web-contents: :
            // Block all pop-ups and calls to window.open.

            // "incognito" :
            // enabling the incognito-like behavior, We don’t want to leave traces of the user’s session on the machine,

            AdditionalBrowserArguments = "--enable-features=BlockInsecurePrivateNetworkRequests block-new-web-contents --incognito",

            // Uses OS's primary account to automatically sign in to web services that support authentication.
            AllowSingleSignOnUsingOSPrimaryAccount = true,
            // Block potentially harmful trackers and trackers from sites that aren't visited before
            EnableTrackingPrevention = true,
            Language = System.Globalization.CultureInfo.CurrentCulture.Name,
        };

        string culture = System.Globalization.CultureInfo.CurrentCulture.Name;
        CoreWebView2Environment env = await CoreWebView2Environment.CreateAsync(null, userDataFolder:browserDataFolderPath, options);
        
        CoreWebView2ControllerOptions ctrlOptions = env.CreateCoreWebView2ControllerOptions();
        ctrlOptions.IsInPrivateModeEnabled = true;
        ctrlOptions.ScriptLocale = culture;
        // Create profile name as of directory name
        ctrlOptions.ProfileName = Path.GetFileName(browserDataFolderPath);

        this.webViewControl.AllowExternalDrop = false;
        // this waits until the first page is navigated - then continue executing the next line of code!
        await webViewControl.EnsureCoreWebView2Async(env, ctrlOptions);       
        
        bool isInprivate = webViewControl.CoreWebView2.Profile.IsInPrivateModeEnabled;

        // Setting the initial URL when initializing WebView2
        this.webViewControl.Source = new Uri(iscPwdResetUrl);
        // URL to navigate
        // webViewControl.CoreWebView2.Navigate(iscPwdResetUrl);

        this.Controls.Add(webViewControl);
        ((System.ComponentModel.ISupportInitialize)(this.webViewControl)).EndInit();


        this.ResumeLayout(true);
    }

    [STAThread]
    static void Main()
    {
        string sessionNumber = new Random().Next(1, Int32.MaxValue).ToString();
        string dataDirPath = Path.Combine(Path.GetTempPath(), "spdpr" + sessionNumber);
        try
        {
            _ = IsWebView2RuntimeInstalled();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new DprBrowserForm(dataDirPath));


            DeleteDataDir(dataDirPath);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
    
    private void DprBrowserForm_Load(object sender, EventArgs e)
    {
        return;
    }

    public static async Task<bool> IsWebView2RuntimeInstalled()
    {

        using (StreamWriter writer = new StreamWriter("c:\\tool\\webview.txt"))
        {
            try
            {
                await CoreWebView2Environment.CreateAsync(null, ""); // This will throw if not installed
                writer.WriteLine("WebView2 Runtime  Found");
                return true;
            }
            catch (WebView2RuntimeNotFoundException)
            {
                writer.WriteLine("WebView2 Runtime NOT  Found");
                return false;
            }
            catch (Exception ex)
            {
                // Log or handle other exceptions
                writer.WriteLine("WebView2 Runtime Check error " + ex.Message);
                return false;
            }
        }
    }
}