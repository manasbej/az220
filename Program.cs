using System.Text;
using Azure;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

public partial class Program
{
    private const string connectionString = "HostName=az220-iot-hub.azure-devices.net;SharedAccessKeyName=device;SharedAccessKey=hq0H98HQ4eUC9PEN4/EjmWyhlwShJwLZPhZXhTTx5Bc=";
    public static async Task Main(string[] args)
    {
        string deviceId = "sensor-device-01";
        Console.WriteLine("Hello, World!");
        await CreateDeviceAsync(deviceId);
        //await GetDeviceTwinAsync(deviceId);
        //await GetDeviceTwinAsync(deviceId);
        //await DeleteDeviceAsync(connectionString, deviceId);
        //await SendDeviceTelemetryMessagesAsync(deviceId);
        //await UpdateDesiredPropertiesAsync(deviceId);
        await UpdateReportedPropertiesAsync(deviceId);
         
    }
    

    async static Task CreateDeviceAsync(string deviceId)
    {
        Console.WriteLine("CreateDeviceAsync started....");
        // Create a new instance of the RegistryManager using the IoT Hub connection string
        RegistryManager registryManager = RegistryManager.CreateFromConnectionString(connectionString);

        try
        {
            // Check if the device already exists
            Device device = await registryManager.GetDeviceAsync(deviceId);
            if (device != null)
            {
                Console.WriteLine("Device already exists!");
                Console.WriteLine($"Reading Device twin: ");
                Console.WriteLine($"-----------------------");
                await GetDeviceTwinAsync(deviceId);
                return;
            }

            // Create a new device instance
            device = new Device(deviceId);

            // Register the device in the IoT Hub
            device = await registryManager.AddDeviceAsync(device);

            Console.WriteLine("Device created successfully!");
            Console.WriteLine($"Device ID: {device.Id}");
            Console.WriteLine($"Primary key: {device.Authentication.SymmetricKey.PrimaryKey}");
            Console.WriteLine($"Secondary key: {device.Authentication.SymmetricKey.SecondaryKey}");
            Console.WriteLine($"Device twin: ");
            Console.WriteLine($"---------------");
            await GetDeviceTwinAsync( deviceId);
        }
        catch (DeviceAlreadyExistsException)
        {
            Console.WriteLine("Device already exists!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating device: {ex.Message}");
        }
        finally
        {
            // Remember to dispose the RegistryManager when done
            await registryManager.CloseAsync();
        }
    }

    async static Task GetDeviceTwinAsync(string deviceId)
    {
        Console.WriteLine("GetDeviceTwinAsync started....");
        RegistryManager registryManager = RegistryManager.CreateFromConnectionString(connectionString);
        
        try
        {
            Twin twin = await registryManager.GetTwinAsync(deviceId);
            
            //Console.WriteLine($"Device Twin received. Device ID: {twin.ToJson()}");
             Console.WriteLine($"Device Twin received. Device ID: {twin.DeviceId}");
             Console.WriteLine($"Desired properties: {twin.Properties.Desired.ToJson()}");
             Console.WriteLine($"Reported properties: {twin.Properties.Reported.ToJson()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            await registryManager.CloseAsync();
        }
    }

    async static Task GetDeviceListAsync()
    {
        Console.WriteLine("GetDeviceListAsync started....");
        RegistryManager registryManager = RegistryManager.CreateFromConnectionString(connectionString);

        try
        {
            var devices = registryManager.CreateQuery("SELECT * FROM devices", 100);

            while (devices.HasMoreResults)
            {
                var page = await devices.GetNextAsTwinAsync();

                foreach (var twin in page)
                {                    
                    Console.WriteLine($"Device ID: {twin.DeviceId}");
                    Console.WriteLine($"Connection state: {twin.ConnectionState}");
                    Console.WriteLine($"Last activity: {twin.LastActivityTime}");
                    Console.WriteLine("-------------------------------------");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            await registryManager.CloseAsync();
        }
    }

    async static Task DeleteDeviceAsync(string connectionString, string deviceId)
    {
        Console.WriteLine($"Deleting device - {deviceId}");
        RegistryManager registryManager = RegistryManager.CreateFromConnectionString(connectionString);

        try
        {
            await registryManager.RemoveDeviceAsync(deviceId);
            Console.WriteLine($"Device {deviceId} deleted successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            await registryManager.CloseAsync();
        }
    }

    async static Task UpdateReportedPropertiesAsync(string deviceId)
    {
        Console.WriteLine("UpdateReportedPropertiesAsync started....");
        TwinCollection twinCollection = new TwinCollection();
        twinCollection["DeviceLocation"] = new { lat = 47.64263, lon = -122.13035, alt = 0 };
        try
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(connectionString, deviceId, Microsoft.Azure.Devices.Client.TransportType.Mqtt);
            await deviceClient.UpdateReportedPropertiesAsync(twinCollection);
            Console.WriteLine("Reported properties updated successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        
    }
    async static Task UpdateDesiredPropertiesAsync(string deviceId)
    {
        Console.WriteLine("UpdateDesiredPropertiesAsync started....");
        RegistryManager registryManager = RegistryManager.CreateFromConnectionString(connectionString);
        
        //deviceClient.UpdateReportedPropertiesAsync()

        try
        {
            Twin twin = await registryManager.GetTwinAsync(deviceId);

            // Update desired properties
            twin.Properties.Desired["temp"] = "40";
            
            // Update the twin in the IoT Hub
            await registryManager.UpdateTwinAsync(twin.DeviceId, twin, twin.ETag);    
            
            Console.WriteLine("Desired properties updated successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            await registryManager.CloseAsync();
        }
    }

    async static Task SendDeviceTelemetryMessagesAsync(string deviceId)
    {
        Console.WriteLine("SendDeviceTelemetryMessagesAsync started....");
        //DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(connectionString, Microsoft.Azure.Devices.Client.TransportType.Mqtt);  
        DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(connectionString, deviceId, Microsoft.Azure.Devices.Client.TransportType.Mqtt);
        try
        {
            double minTemperature = 20;
            double minHumidity = 60;
            Random rand = new Random();

            while (true)
            {

                double currentTemperature = minTemperature + rand.NextDouble() * 15;
                double currentHumidity = minHumidity + rand.NextDouble() * 20;
                Console.WriteLine("{0} > Preparing message: {1} {2}", DateTime.Now, currentTemperature, currentHumidity);
                // Create JSON message  

                var telemetryDataPoint = new
                {

                    temperature = currentTemperature,
                    humidity = currentHumidity
                };

                string messageString = "";



                messageString = JsonConvert.SerializeObject(telemetryDataPoint);

                var message = new Microsoft.Azure.Devices.Client.Message(Encoding.ASCII.GetBytes(messageString));

                // Add a custom application property to the message.  
                // An IoT hub can filter on these properties without access to the message body.  
                //message.Properties.Add("temperatureAlert", (currentTemperature > 30) ? "true" : "false");  

                // Send the telemetry message  
                await deviceClient.SendEventAsync(message);
                Console.WriteLine("{0} > Sent message: {1}", DateTime.Now, messageString);
                await Task.Delay(1000 * 10);
                
            }
        }
        catch (Exception ex)
        {

            throw ex;
        }
    }

}
