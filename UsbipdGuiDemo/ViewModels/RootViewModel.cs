using Stylet;
using UsbipdGuiDemo.Models;
using System.Diagnostics;
using System.Text;
using System.Windows.Threading;
using System;

using MessageBox = HandyControl.Controls.MessageBox;

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

        private string _busid;
        public string BUSID
        {
            get { return _busid; }
            set
            {
                if(value != _busid)
                {
                    SetAndNotify(ref _busid, value);
                }
            }
        }

        private string _device_name;
        public string DEVICE_NAME
        {
            get { return _device_name; }
            set
            {
                if(value != _device_name)
                {
                    SetAndNotify(ref _device_name, value);
                }           
            }
        }

        private string _state;
        public string STATE
        {
            get { return _state; }
            set
            {
                if(value!= _state)
                {
                    SetAndNotify(ref _state, value);
                }
            }
        }
        public void ListDevices()
        {
            DEVICE.Clear();
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
            while (!listProcess.StandardOutput.EndOfStream)
            {
                string line = listProcess.StandardOutput.ReadLine();
                var line_item = line.Split("    ");
                if (line.Contains("GUID"))
                {
                    break;
                }
                if (line_item[0].Contains('-'))
                {
                    DEVICE.Add(new DeviceModel
                    {
                        BUSID = line_item[0],
                        DEVICE_NAME = line_item[1],
                        STATE = line_item[line_item.Length - 1].Trim()
                    });
                    if(line_item[0] == lastAttachedDevice&& line_item[line_item.Length - 1].Trim() == "Shared")
                    {
                        AttachDevices(line_item[0]);
                    }
                }
            }

        }

        public void AttachDevices(string _busid)
        {
            var attachProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "usbipd",
                    Arguments = "wsl attach --busid " + _busid,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                }
            };
            attachProcess.Start();
        }

        public void DetachDevices(string _busid)
        {
            if(_busid == lastAttachedDevice)
            {
                lastAttachedDevice = null;
            }
            var detachProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "usbipd",
                    Arguments = "wsl detach --busid " + _busid,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                }
            };
            detachProcess.Start();
        }

        public RootViewModel()
        {
            ListDevices();
            DispatcherTimer timer = new DispatcherTimer();
            timer.Tick += (s, e) =>
            {
                
                ListDevices();
            };
            timer.Interval = TimeSpan.FromMilliseconds(3000);
            timer.Start();
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
                    string messageBoxText = "you have to make device shared by cmdline first!";
                    string caption = "device not shareable";
                    MessageBox.Warning(messageBoxText, caption);
                    break;
            }            
        }
    }
}
