using System;
using System.Collections;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Presentation.Shapes;
using Microsoft.SPOT.Touch;

using Gadgeteer.Networking;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Gadgeteer.Modules.GHIElectronics;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Text;
using System;
using System.Net;
using Microsoft.SPOT.Net.NetworkInformation;
using GHI.Networking;
using Microsoft.SPOT.Hardware;

namespace WifiTestGT
{
   public partial class Program
    {
        static readonly string MqttBrokerIp = "192.241.182.227";
        static readonly string MqttTopic = "/device/xxxxx/key/xxxxxx";
        static readonly string Ssid = "xxxx";
        static readonly string Ssid_Password = "xxxx";

        private MqttClient mqttClient;
        private GT.Timer wifiTimer;

        public Program() {

            wifiTimer = new GT.Timer(1000);
            mqttClient = new MqttClient(IPAddress.Parse(MqttBrokerIp));          
        }
        private void ProgramStarted()
        {
            tempHumidity.MeasurementInterval = 5000;

            wifiTimer.Tick += (timer) => {
                timer.Stop();
           
                wifi_RS21.NetworkInterface.Open();
                wifi_RS21.NetworkInterface.EnableDhcp();
                wifi_RS21.NetworkInterface.EnableDynamicDns();
                wifi_RS21.NetworkInterface.Join(Ssid, Ssid_Password);
                 
                Debug.Print("Network joined");    
            };

            measurementTimer.Tick += (timer) => {
                tempHumidity.RequestSingleMeasurement();
            };

            NetworkChange.NetworkAvailabilityChanged += (sender, e) => {
                Debug.Print("Network availability: " + e.IsAvailable.ToString());
                Thread.Sleep(3000);
                mqttClient.Connect(Guid.NewGuid().ToString());

                tempHumidity.StartTakingMeasurements();               
            }; 

            tempHumidity.MeasurementComplete += (sender, e) => {
                publishMessage("{\"sensors\":[{\"value\":\"" + ((int)e.Temperature) + "\",\"tag\":\"temperature\"},{\"value\":\"" + ((int)e.RelativeHumidity) + "\",\"tag\":\"humidity\"}] }");

            };
                       
            wifiTimer.Start();
        }
        
        private void publishMessage(string message)
        {  
            mqttClient.Publish(MqttTopic, Encoding.UTF8.GetBytes(message), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, true); 
        }
        
   }
}
