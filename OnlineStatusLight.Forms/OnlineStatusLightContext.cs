using Microsoft.Extensions.DependencyInjection;
using OnlineStatusLight.Application;
using OnlineStatusLight.Forms.Properties;
using System.ComponentModel;
using app = System.Windows.Forms;

namespace OnlineStatusLight.Forms
{
    public class OnlineStatusLightContext : app.ApplicationContext
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly ContextMenuStrip _contextMenu;
        private readonly ToolStripMenuItem _menuItem;
        private readonly IContainer _components;

        public OnlineStatusLightContext()
        {
            this._components = new Container();
            this._contextMenu = new ContextMenuStrip();
            this._menuItem = new ToolStripMenuItem();

            // Initialize contextMenu
            this._contextMenu.Items.AddRange(new ToolStripMenuItem[] { this._menuItem });

            // Initialize menuItem
            this._menuItem.ImageIndex = 0;
            this._menuItem.Text = "Exit";
            this._menuItem.Click += new EventHandler(this.MenuItem_Click);

            // Create the NotifyIcon
            this._notifyIcon = new NotifyIcon(this._components);

            // The Icon property sets the icon that will appear
            // in the systray for this application.
            _notifyIcon.Icon = Resources.TrafficLight;

            // The ContextMenu property sets the menu that will
            // appear when the systray icon is right clicked.
            _notifyIcon.ContextMenuStrip = this._contextMenu;

            // The Text property sets the text that will be displayed,
            // in a tooltip, when the mouse hovers over the systray icon.
            _notifyIcon.Text = Resources.AppName;
            _notifyIcon.Visible = true;

            // Handle the DoubleClick event to activate the form.
            // _notifyIcon.DoubleClick += new EventHandler(this.NotifyIcon_DoubleClick);
        }

        protected override void Dispose(bool disposing)
        {
            // Clean up any components being used.
            if (disposing)
                if (_components != null)
                    _components.Dispose();

            base.Dispose(disposing);
        }

        private void NotifyIcon_DoubleClick(object Sender, EventArgs e)
        {
            // Show the form when the user double clicks on the notify icon.
            if (this.MainForm == null)
            {
                this.MainForm = new MainForm();
            }

            // Set the WindowState to normal if the form is minimized.
            if (this.MainForm.WindowState == FormWindowState.Minimized)
                this.MainForm.WindowState = FormWindowState.Normal;

            this.MainForm.Show();

            // Activate the form.
            this.MainForm.Activate();
        }

        private void MenuItem_Click(object Sender, EventArgs e)
        {
            // Hide tray icon, otherwise it will remain shown until user mouses over it
            _notifyIcon.Visible = false;

            var _sync = Startup.AppHost.Services.GetRequiredService<SyncLightService>();
            _sync.Dispose();

            app.Application.Exit();
            Environment.Exit(1);
        }
    }
}
