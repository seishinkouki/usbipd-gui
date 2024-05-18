using Stylet;
using UsbipdGuiDemo.Models;
using System.Diagnostics;
using System.Text;
using System.Windows.Threading;
using System;
using MessageBox = HandyControl.Controls.MessageBox;
using System.Threading.Tasks;
using System.Linq;
using HandyControl.Tools.Extension;
using System.Reflection;
using System.Management;
using System.Security.Principal;
using System.Windows;

namespace UsbipdGuiDemo.ViewModels
{
    public class RootViewModel : PropertyChangedBase
    {
        private string lastAttachedDevice = null;
        private string _title = "Usbipd Demo";
        public string Title
        {
            get { return _title; }
            set { SetAndNotify(ref _title, value); }
        }
        private string _theme = "dark";
        public string Theme
        {
            get { return _theme; }
            set { SetAndNotify(ref _theme, value); }
        }

        private BindableCollection<DeviceModel> _device = new BindableCollection<DeviceModel>();

        public BindableCollection<DeviceModel> DEVICE
        {
            get { return _device; }
            set { SetAndNotify(ref _device, value); }
        }

        public void ListDevices()
        {
            var listProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "usbipd",
                    Arguments = "list",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8
                }
            };
            listProcess.Start();
            foreach (var device in _device)
            {
                device.IsExits = false;
            }

            while (!listProcess.StandardOutput.EndOfStream)
            {

                string line = listProcess.StandardOutput.ReadLine();
                var line_item = line.Split("  ", StringSplitOptions.RemoveEmptyEntries);
                if (line.Contains("GUID") || line_item.Length < 1)
                {
                    break;
                }
                if (line_item[0].Contains('-'))
                {
                    if (line_item[0] == lastAttachedDevice && line_item[line_item.Length - 1].Trim() == "Shared")
                    {
                        Task task = new Task(() => AttachDevices(line_item[0]));
                        task.Start();
                        continue;
                    }

                    if (!DEVICE.Any(device => device.BUSID == line_item[0]))
                    {
                        DEVICE.Add(new DeviceModel
                        {
                            BUSID = line_item[0].Trim(),
                            VIDPID = line_item[1].Trim(),
                            DEVICE_NAME = line_item[2].Trim(),
                            STATE = line_item[3].Trim(),
                            IsExits = true
                        });
                    }
                    else
                    {
                        var device = DEVICE.FirstOrDefault(device => device.BUSID == line_item[0]);
                        if (device != null)
                        {
                            device.VIDPID = line_item[1].Trim();
                            device.DEVICE_NAME = line_item[2].Trim();
                            device.STATE = line_item[3].Trim();
                            device.IsExits = true;
                        }

                    }
                }
            }
            DEVICE.Where(l => l.IsExits == false).ToList().ForEach(i => DEVICE.Remove(i));
        }

        public static void AttachDevices(string _busid)
        {
            var attachProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "usbipd",
                    Arguments = "attach --busid " + _busid + " --wsl",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                }
            };
            attachProcess.Start();
        }

        public void DetachDevices(string _busid)
        {
            if (_busid == lastAttachedDevice)
            {
                lastAttachedDevice = null;
            }
            var detachProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "usbipd",
                    Arguments = "detach --busid " + _busid,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                }
            };
            detachProcess.Start();
        }
        public void BindDevices(string _busid)
        {
            var BindProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "usbipd",
                    Arguments = " bind --busid " + _busid,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                }
            };
            BindProcess.Start();
            BindProcess.WaitForExit();
            AttachDevices(_busid);
        }

        public RootViewModel()
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Tick += DispatcherTimer_Tick;
            timer.Interval = TimeSpan.FromMilliseconds(3000);
            timer.Start();

            ListDevices();
            ManagementEventWatcher watcher = new ManagementEventWatcher();
            WqlEventQuery query = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2 OR EventType = 3");
            watcher.EventArrived += new EventArrivedEventHandler(watcher_EventArrived);
            watcher.Query = query;
            watcher.Start();
        }

        public  void watcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            Task.Factory.StartNew(AsynchronousRefresh);
        }
        public void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            Task.Factory.StartNew(AsynchronousRefresh);
        }

        public void AsynchronousRefresh()
        {
            Task task = new Task(() => ListDevices());
            task.Start();
        }

        public void DoConnect(object _device)
        {
            DeviceModel vm = _device as DeviceModel;
            switch (vm.STATE)
            {
                case "Shared":
                    AttachDevices(vm.BUSID);
                    lastAttachedDevice = vm.BUSID;
                    break;
                case "Attached":
                    DetachDevices(vm.BUSID);
                    break;
                case "Not shared":
                    if (!IsAdministrator())
                    {
                        MessageBoxResult userResult = MessageBox.Show("绑定新设备需要管理器权限, 是否以管理员权限重启应用", "提示", MessageBoxButton.YesNo);
                        if (userResult == MessageBoxResult.Yes)
                        {
                            PerformRestart();
                        }
                    }
                    else
                    {
                        Task task = new Task(() => BindDevices(vm.BUSID));
                        task.Start();
                    }
                    break;
            }
        }
        public static void PerformRestart()
        {
            Process proc = new Process();
            proc.StartInfo.FileName = Process.GetCurrentProcess().MainModule.FileName;
            proc.StartInfo.UseShellExecute = true;
            proc.StartInfo.Verb = "runas";
            proc.Start();
            Application.Current.Shutdown();
        }
        public static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
