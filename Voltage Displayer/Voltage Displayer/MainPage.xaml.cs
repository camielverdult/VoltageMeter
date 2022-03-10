﻿using System;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;

using Windows.Devices.SerialCommunication;
using Windows.Devices.Enumeration;
using System.IO.Ports;
using System.Threading;
using System.ComponentModel;

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

    public partial class Voltage : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        private string _voltage;

        public Voltage()
        {
            _voltage = "";
        }

        public string Value
        {
            get { return _voltage; }
            set {
                _voltage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(_voltage)));
            }
        }
    }

    public sealed partial class MainPage : Page
    {
        public Voltage _voltage = new Voltage();
        private SerialPort arduinoPort { get; set; }

        Thread readThread;

        private void ReadVoltage()
        {
            System.Diagnostics.Debug.WriteLine($"Reading from Arduino Uno on port {arduinoPort.PortName}...");
            while (arduinoPort.IsOpen)
            {
                try
                {
                    var readVoltage = arduinoPort.ReadLine();

                    if (readVoltage != _voltage.Value)
                    {
                        // Update displayed voltage
                        _voltage.Value = readVoltage;
                    }
                } catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Serial port crashed.");
                    return;
                }
            }
        }

        string GetComPortFromUSBName(string name)
        {
            if (!name.Contains("COM"))
            {
                System.Diagnostics.Debug.WriteLine("Found Arduino Uno without a COM port?");
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
            _voltage.Value = "Arduino disconnected.";
        }


        public MainPage()
        {
            InitializeComponent();

            _voltage.Value = "No Arduino found.";

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
