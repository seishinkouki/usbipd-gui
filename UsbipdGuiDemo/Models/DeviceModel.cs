using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stylet;

namespace UsbipdGuiDemo.Models
{
    public class DeviceModel : PropertyChangedBase
    {
        private string _BUSID;
        public string BUSID 
        {
            get { return _BUSID; }
            set { SetAndNotify(ref _BUSID, value); }
        }
        private string _DEVICE_NAME;
        public string DEVICE_NAME 
        {
            get { return _DEVICE_NAME; }
            set { SetAndNotify(ref _DEVICE_NAME, value); }
        }
        private string _STATE;
        public string STATE 
        {
            get { return _STATE; }
            set { SetAndNotify(ref _STATE, value); }
        }
    }
}
