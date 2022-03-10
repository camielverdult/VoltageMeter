using System;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;

using Windows.Devices.SerialCommunication;
using Windows.Devices.Enumeration;
using System.IO.Ports;
using System.Threading;
using System.ComponentModel;
using Windows.System;
using System.Threading.Tasks;
using System.Collections.Generic;

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

        private string voltage;
        private DispatcherQueue mainThread;

        public Voltage(DispatcherQueue thread)
        {
            voltage = "";
            mainThread = thread;
        }

        public string Value
        {
            get { return voltage; }
            set {
                voltage = value;
                if (mainThread != null)
                {
                    mainThread.TryEnqueue(() =>
                    {
                        System.Diagnostics.Debug.WriteLine("Invoking propery changed on main thread!");
                        if (PropertyChanged == null)
                        {
                            System.Diagnostics.Debug.WriteLine("PropertyChanged is null!");
                        }
                        else
                        {
                            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                        }
                    });
                }
            }
        }

        //public void InvokePropertyChanged()
        //{
        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(voltage)));
        //}
    }

    public sealed partial class MainPage : Page
    {

        public Voltage voltage;
        private SerialPort arduinoPort { get; set; }

        Thread readThread;

        private void ReadVoltage()
        {
            System.Diagnostics.Debug.WriteLine($"Reading from Arduino Uno on port {arduinoPort.PortName}...");

            string serialBuffer = "";
            while (arduinoPort.IsOpen)
            {
                // C# has no regard to new line on serial port for some reason
                // so we are going to use this mess of a solution
                // which takes way too many loops, but whatever
                
                serialBuffer += (char)arduinoPort.ReadByte();

                // Check if we have the dot point at the second location, otherwise our input doesn't make any sense
                if (serialBuffer.Length == 2)
                {
                    if (serialBuffer[1] != '.')
                    {
                        // We want the value to be displayed properly, so we flush it until we get a dot in the second position
                        serialBuffer = "";
                    }
                }

                // We want the string to be 4 long
                // 4.23 for example
                if (serialBuffer.Length == 4) { 
               
                    // System.Diagnostics.Debug.WriteLine(serialBuffer);

                    // Update displayed voltage
                    voltage.Value = serialBuffer;

                    // Reset buffer
                    serialBuffer = "";
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

        private void OnDeviceAdded(DeviceWatcher sender, DeviceInformation deviceInformation)
        {
            System.Diagnostics.Debug.WriteLine("Opening Arduino port...");

            try
            {
                voltage.Value = "Opening serial port...";
            } catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Could not update text: {ex}");
            }
            // Try opening serial port
            arduinoPort = new SerialPort();

            arduinoPort.PortName = GetComPortFromUSBName(deviceInformation.Name);
            arduinoPort.BaudRate = 9600;
            arduinoPort.DataBits = 8;
            arduinoPort.StopBits = StopBits.Two;

            arduinoPort.Open();
            readThread = new Thread(ReadVoltage);
            readThread.Start();
        }

        private void OnDeviceRemoved(DeviceWatcher sender, DeviceInformationUpdate deviceInformationUpdate)
        {
            System.Diagnostics.Debug.WriteLine("Removing Arduino...");
            arduinoPort.Close();
            arduinoPort.Dispose();
            arduinoPort = null;
            voltage.Value = "Arduino disconnected.";
        }


        public MainPage()
        {
            InitializeComponent();
            DispatcherQueue mainThread = DispatcherQueue.GetForCurrentThread();
            voltage = new Voltage(mainThread);

            voltage.Value = "No Arduino found.";

            string deviceSelector = SerialDevice.GetDeviceSelectorFromUsbVidPid(ArduinoDevice.VendorID, ArduinoDevice.ProductID);

            var deviceWatcher = DeviceInformation.CreateWatcher(deviceSelector);
            deviceWatcher.Added += new TypedEventHandler<DeviceWatcher, DeviceInformation>(OnDeviceAdded);
            deviceWatcher.Removed += new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>(OnDeviceRemoved);


            if ((deviceWatcher.Status != DeviceWatcherStatus.Started) && (deviceWatcher.Status != DeviceWatcherStatus.EnumerationCompleted))
            {
                deviceWatcher.Start();
            }

            //Task.Run(() =>
            //{
            //    Thread.Sleep(10000);
            //    _voltage.Value = "test";
            //    _voltage.InvokePropertyChanged();
            //});
        }
    }
}
