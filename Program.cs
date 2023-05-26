// See https://aka.ms/new-console-template for more information

using Azure;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.Shared;

public partial class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        await CreateDeviceAsync();

    }
    

    async static Task CreateDeviceAsync()
    {
        string connectionString = "HostName=az220-iot-hub.azure-devices.net;SharedAccessKeyName=device;SharedAccessKey=bIOYKMnnhc2IcAaXzDTfIUPczm1ZSyMOWqU0FH6+Gis=";
        string deviceId = "sensor-device-02";

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
                await GetDeviceTwinAsync(connectionString, deviceId);
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
            await GetDeviceTwinAsync(connectionString, deviceId);
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

    async static Task GetDeviceTwinAsync(string connectionString, string deviceId)
    {

        DeviceClient deviceClient = DeviceClient.CreateFromConnectionString(connectionString, deviceId);

        try
        {
            Twin twin = await deviceClient.GetTwinAsync();
            

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
            await deviceClient.CloseAsync();
        }
    }

    async static Task GetDeviceListAsync(string connectionString)
    {
        
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

    async Task UpdateDesiredPropertiesAsync(string connectionString, string deviceId)
    {

        RegistryManager registryManager = RegistryManager.CreateFromConnectionString(connectionString);

        try
        {
            Twin twin = await registryManager.GetTwinAsync(deviceId);

            // Update desired properties
            twin.Properties.Desired["propertyName"] = "propertyValue";

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



}