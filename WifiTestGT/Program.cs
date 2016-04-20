using Microsoft.SPOT;
using Microsoft.SPOT.Net.NetworkInformation;
using System;
using System.Net;
using System.Text;
using System.Threading;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using GT = Gadgeteer;

namespace WifiTestGT
{
   public partial class Program
    {
        static readonly string MqttBrokerIp = "54.174.197.219";
        static readonly string MqttTopic = "/device/xxxxx/key/xxxxx";  
        static readonly string Ssid = "xxxxx";  
        static readonly string Ssid_Password = "xxxxx";  

        private MqttClient mqttClient;
        private GT.Timer wifiTimer;

        public Program() {

            wifiTimer = new GT.Timer(2000); 
            mqttClient = new MqttClient(IPAddress.Parse(MqttBrokerIp));
          
        }
        private void ProgramStarted()
        {
            tempHumidity.MeasurementInterval = 5000;
                     

            wifiTimer.Tick += (timer) => {
                timer.Stop();

                try
                {
                    wifi_RS21.NetworkInterface.Open();
                    wifi_RS21.NetworkInterface.EnableDhcp();
                    wifi_RS21.NetworkInterface.EnableDynamicDns();
                    wifi_RS21.NetworkInterface.Join(Ssid, Ssid_Password);
                    while (wifi_RS21.NetworkInterface.IPAddress == "0.0.0.0")
                    {
                        Debug.Print("Waiting for DHCP...");
                        Thread.Sleep(1000);
                    }
                    Debug.Print("Network ready to use.");

                    Debug.Print("Conecting to MQTT broker ...");
                    mqttClient.Connect(Guid.NewGuid().ToString());
                    Debug.Print("Conected to MQTT broker!");

                    tempHumidity.StartTakingMeasurements();
                    Debug.Print("Sending measurements");
                }
                catch(Exception ex) {
                    wifiTimer.Start();
                }
            };

            NetworkChange.NetworkAvailabilityChanged += (sender, e) =>  {
                Debug.Print("Network availability: " + e.IsAvailable.ToString());
            };

            tempHumidity.MeasurementComplete += (sender, e) => {

                var temperature = "{\"value\":\"" + ((int)e.Temperature) + "\",\"tag\":\"temperature\"}";
                var humidity = "{\"value\":\"" + ((int)e.RelativeHumidity) + "\",\"tag\":\"humidity\"}";
              
                var builder = new StringBuilder("{\"sensors\":[@temperature,@humidity] }");

                builder.Replace("@temperature", temperature);
                builder.Replace("@humidity", humidity);

                publishMessage(builder.ToString());

            };
                       
            wifiTimer.Start();
        }
        
        private void publishMessage(string message)
        {  
            mqttClient.Publish(MqttTopic, Encoding.UTF8.GetBytes(message), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true); 
        }
        
   }
}
