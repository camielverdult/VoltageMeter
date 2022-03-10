using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;


using Windows.Devices.SerialCommunication;
using Windows.Devices.Enumeration;
using System.IO.Ports;
using System.Threading;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Voltage_Displayer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 
    public class ArduinoDevice
    {
        public const UInt16 VendorID = 0x2341;
        public const UInt16 ProductID = 0x0043;
    }
    public sealed partial class MainPage : Page
    {
        public string Voltage { get; set; }

        private SerialPort arduinoPort { get; set; }
        Thread readThread;

        private void ReadVoltage()
        {
            while(arduinoPort.IsOpen)
            {
                var readVoltage = arduinoPort.ReadLine();

                if (readVoltage != Voltage)
                {
                    // Update displayed voltage

                }
            }
        }

        string GetComPortFromUSBName(string name)
        {
            if (!name.Contains("COM"))
            {
                return "";
            }

            var comLocation = name.IndexOf("COM");
            var lastBracket = name.LastIndexOf(")");
            string portName = name.Substring(comLocation, lastBracket - comLocation);
            System.Diagnostics.Debug.WriteLine($"Found COM port {portName}");

            return portName;
        }

        private async void OnDeviceAdded(DeviceWatcher sender, DeviceInformation deviceInformation)
        {
            System.Diagnostics.Debug.WriteLine("Adding Arduino!");
            // Try opening serial port
            arduinoPort = new SerialPort();

            arduinoPort.PortName = GetComPortFromUSBName(deviceInformation.Name);
            arduinoPort.BaudRate = 9600;
            
            arduinoPort.Open();
            readThread = new Thread(ReadVoltage);
            readThread.Start();
        }

        private async void OnDeviceRemoved(DeviceWatcher sender, DeviceInformationUpdate deviceInformationUpdate)
        {
            System.Diagnostics.Debug.WriteLine("Removing Arduino...");
            arduinoPort.Close();
            arduinoPort.Dispose();
            arduinoPort = null;
            Voltage = "Arduino disconnected.";
        }


        public MainPage()
        {
            this.InitializeComponent();

            Voltage = "No Arduino found.";

            string deviceSelector = SerialDevice.GetDeviceSelectorFromUsbVidPid(ArduinoDevice.VendorID, ArduinoDevice.ProductID);

            var deviceWatcher = DeviceInformation.CreateWatcher(deviceSelector);
            deviceWatcher.Added += new TypedEventHandler<DeviceWatcher, DeviceInformation>(OnDeviceAdded);
            deviceWatcher.Removed += new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>(OnDeviceRemoved);


            if ((deviceWatcher.Status != DeviceWatcherStatus.Started) && (deviceWatcher.Status != DeviceWatcherStatus.EnumerationCompleted))
            {
                deviceWatcher.Start();
            }
        }
    }
}
