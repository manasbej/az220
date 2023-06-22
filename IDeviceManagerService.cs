using System.Text;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

public interface IDeviceManagerService
{
    Task<string> CreateDeviceAsync(string deviceId);
    Task<IEnumerable<string>> GetDeviceListAsync();
    Task<string> DeleteDeviceAsync(string deviceId);
    Task<string> UpdateDesiredPropertiesAsync(string deviceId);
    Task<string> UpdateReportedPropertiesAsync(string deviceId);
    Task<string> SendDeviceTelemetryMessagesAsync(string deviceId);
}

public class DeviceManagerService : IDeviceManagerService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureQueue> _logger;
    private readonly string _connectionString;
    public DeviceManagerService(IConfiguration configuration, ILogger<AzureQueue> logger)
    {
         _configuration = configuration;
        _logger = logger;
        _connectionString = _configuration["IotHubConnectionString"];  
    }
    public async Task<string> CreateDeviceAsync(string deviceId)
    {
        RegistryManager registryManager = RegistryManager.CreateFromConnectionString(_connectionString);

        try
        {
            // Check if the device already exists
            Device device = await registryManager.GetDeviceAsync(deviceId);
            if (device != null)
            {
                Console.WriteLine("Device already exists!");
                return "Device already exists!";
            }

            // Create a new device instance
            device = new Device(deviceId);

            // Register the device in the IoT Hub
            device = await registryManager.AddDeviceAsync(device);

            var deviceInfo = new
                {
                    Msg = "Device Created",
                    Id = device.Id,
                    PrimaryKey = device.Authentication.SymmetricKey.PrimaryKey,
                    SecondaryKey = device.Authentication.SymmetricKey.SecondaryKey
                };
                
            return  JsonConvert.SerializeObject(deviceInfo);     
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
        return "";
    }
    public async Task<IEnumerable<string>> GetDeviceListAsync()
    {
        RegistryManager registryManager = RegistryManager.CreateFromConnectionString(_connectionString);
        List<string> deviceList = new List<string>();
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
                    deviceList.Add(twin.DeviceId);
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
        return deviceList;
    }
    public async Task<string> DeleteDeviceAsync(string deviceId)
    {
        Console.WriteLine($"Deleting device - {deviceId}");
        RegistryManager registryManager = RegistryManager.CreateFromConnectionString(_connectionString);
        string message = "";
        try
        {
            await registryManager.RemoveDeviceAsync(deviceId);
            message = $"Device {deviceId} deleted successfully!";
        }
        catch (Exception ex)
        {
            message = $"Error: {ex.Message}";
        }
        finally
        {
            await registryManager.CloseAsync();
        }
        return message;
    }
    public async Task<string> UpdateDesiredPropertiesAsync(string deviceId)
    {
        Console.WriteLine("UpdateDesiredPropertiesAsync started....");
        RegistryManager registryManager = RegistryManager.CreateFromConnectionString(_connectionString);
        string mesage = string.Empty;
        //deviceClient.UpdateReportedPropertiesAsync()

        try
        {
            Twin twin = await registryManager.GetTwinAsync(deviceId);

            // Update desired properties
            twin.Properties.Desired["temp"] = "40";
            
            // Update the twin in the IoT Hub
            await registryManager.UpdateTwinAsync(twin.DeviceId, twin, twin.ETag);    
            
            mesage = "Desired properties ['temp':40]updated successfully!";
        }
        catch (Exception ex)
        {
            mesage = $"Error: {ex.Message}";
        }
        finally
        {
            await registryManager.CloseAsync();
        }
        return mesage;
    }

    public async Task<string> UpdateReportedPropertiesAsync(string deviceId)
    {
        string mesage = string.Empty;
        Console.WriteLine("UpdateReportedPropertiesAsync started....");
        TwinCollection twinCollection = new TwinCollection();
        twinCollection["DeviceLocation"] = new { lat = 47.64263, lon = -122.13035, alt = 0 };
        try
        {
            DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(_connectionString, deviceId, Microsoft.Azure.Devices.Client.TransportType.Mqtt);
            await deviceClient.UpdateReportedPropertiesAsync(twinCollection);
            mesage = "Reported properties updated successfully! " + JsonConvert.SerializeObject(twinCollection); ;
        }
        catch (Exception ex)
        {
            mesage = $"Error: {ex.Message}";
        }
        return mesage;        
    }
    public async Task<string> SendDeviceTelemetryMessagesAsync(string deviceId)
    {
        string msg = string.Empty;
        Console.WriteLine("SendDeviceTelemetryMessagesAsync started....");
        //DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(connectionString, Microsoft.Azure.Devices.Client.TransportType.Mqtt);  
        DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(_connectionString, deviceId, Microsoft.Azure.Devices.Client.TransportType.Mqtt);
        try
        {
            double minTemperature = 20;
            double minHumidity = 60;
            Random rand = new Random();
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
           
            await deviceClient.SendEventAsync(message);
            msg = string.Format("{0} > Sent message: {1}", DateTime.Now, messageString);
            
        }
        catch (Exception ex)
        {
            msg = $"Error: {ex.Message}";
        }
        return msg;
    }

}