﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Networking;
using Windows.Networking.Sockets;

namespace imBMW.Clients
{
    public class BluetoothClient : SocketClient
    {
        static BluetoothClient current;

        public string DeviceName { get; private set; }

        protected BluetoothClient()
        {
        }

        public override async Task Connect()
        {
            CheckAlreadyConnected();
            try
            {
                State = ConnectionState.Connecting;
                DeviceName = null;

                var selector = RfcommDeviceService.GetDeviceSelector(RfcommServiceId.SerialPort);
                var deviceList = await DeviceInformation.FindAllAsync(selector);
                var serviceList = new List<RfcommDeviceService>();
                foreach (var device in deviceList)
                {
                    serviceList.Add(await RfcommDeviceService.FromIdAsync(device.Id));
                }

                var sppService = serviceList.FirstOrDefault(s => s.Device.Name.Contains("imBMW") && s.Device.Name.Contains("BlackBox"));
                if (sppService == null)
                {
                    sppService = serviceList.FirstOrDefault(s => s.Device.Name.Contains("imBMW"));
                }
                if (sppService == null)
                {
                    throw new Exception("imBMW Bluetooth device not found.");
                }
                DeviceName = sppService.Device.Name;

                try
                {
                    await Connect(new SocketConnectionSettings(sppService.ConnectionHostName, sppService.ConnectionServiceName));
                }
                catch (Exception ex)
                {
                    throw new Exception($"Can't connect to {sppService.Device.Name} Bluetooth device. Check that it's paired and online. " + ex.Message, ex);
                }
            }
            catch
            {
                Disconnect();
                throw;
            }
        }

        protected override void OnDisconnected()
        {
            base.OnDisconnected();

            DeviceName = null;
        }

        public static BluetoothClient Current
        {
            get
            {
                if (current == null)
                {
                    current = new BluetoothClient();
                }
                return current;
            }
        }
    }
}
