using Microsoft.Extensions.Configuration;
using RESTClientRunner;
using RESTClientRunner.Configuration;
using System.Text.Json;

class Program
{
    static readonly HttpClient httpClient = new();
    public static AppSettings Settings { get; private set; } = new();

    // Application entry point
    static async Task Main()
    {
        LoadConfiguration();

        Console.WriteLine($"App: {Settings.Application.Name}");
        Console.WriteLine($"Default collection URL: {Settings.Application.DefaultCollectionUrl}");

        string collectionUrl = Settings.Application.DefaultCollectionUrl;

        try
        {
            Console.WriteLine("Downloading Postman collection...");
            string json = await httpClient.GetStringAsync(collectionUrl);
            if (string.IsNullOrEmpty(json))
            {
                Console.WriteLine("Failed: response is empty.");
            }
            else
            {
                Console.WriteLine("Success: response received.");
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                PostmanCollection collection =
                    JsonSerializer.Deserialize<PostmanCollection>(json, options)!;

                Console.WriteLine($"\nCollection: {collection.Info.Name}");

                Console.WriteLine("\nAvailable requests:");

                for (int i = 0; i < collection.Item.Count; i++)
                {
                    var r = collection.Item[i].Request;
                    Console.WriteLine($"{i + 1}. {r.Method} {r.Url.Raw}");
                }

                bool exit = false;
                while (!exit)
                {
                    Console.WriteLine("\nChoose:");
                    Console.WriteLine("0 - Run all");
                    Console.WriteLine("N - Run one request");
                    Console.WriteLine("Q - Quit");
                    Console.Write("Your choice: ");

                    string input = Console.ReadLine() ?? "";
                    if (string.Equals(input, "Q", StringComparison.OrdinalIgnoreCase))
                    {
                        exit = true;
                        Console.WriteLine("Exiting...");
                    }
                    else if (input == "0")
                    {
                        foreach (var item in collection.Item)
                            await ExecuteRequest(item.Request, item.Event);
                    }
                    else if (int.TryParse(input, out int index) &&
                             index > 0 &&
                             index <= collection.Item.Count)
                    {
                        await ExecuteRequest(collection.Item[index - 1].Request, collection.Item[index - 1].Event);
                    }
                    else
                    {
                        Console.WriteLine("Invalid input.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed: {ex.Message}");
        }
    }

    // Load configuration from appsettings.json
    static void LoadConfiguration()
    {
        string configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

        if (!File.Exists(configPath))
            throw new FileNotFoundException("Configuration file not found", configPath);

        IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile(configPath, optional: false, reloadOnChange: true)
            .Build();

        Settings = config.Get<AppSettings>() ?? new AppSettings();
    }

    // Execute a single Postman request
    static async Task ExecuteRequest(PostmanRequest request, List<PostmanEvent>? events = null)
    {
        Console.WriteLine($"\nExecuting: {request.Method} {request.Url.Raw}");

        var httpRequest = new HttpRequestMessage(
            new HttpMethod(request.Method),
            request.Url.Raw
        );

        var response = await httpClient.SendAsync(httpRequest);
        string body = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"Status: {(int)response.StatusCode}");
        Console.WriteLine(body);
        if (events != null)
        {
            foreach (var ev in events)
            {
                if (ev.Listen == "test" && ev.Script?.Exec != null)
                {
                    Console.WriteLine("\nRunning Post-request assertions:");
                    foreach (var line in ev.Script.Exec)
                    {
                        string trimmed = line.Trim();
                        // Simple assertion: pm.response.to.be.ok
                        if (trimmed.Contains("pm.response.to.be.ok"))
                        {
                            bool pass = ((int)response.StatusCode == 200);
                            Console.WriteLine($"Assert Status == 200: " + (pass ? "PASS" : "FAIL"));
                        }
                        else if (trimmed.Contains("pm.expect(pm.response.json()"))
                        {
                            try
                            {
                                using var doc = JsonDocument.Parse(body);
                                JsonElement current = doc.RootElement;

                                // Example: pm.expect(pm.response.json().args)
                                if (trimmed.Contains("args"))
                                {
                                    if (current.TryGetProperty("args", out JsonElement argsProp))
                                    {
                                        // Example: pm.expect(pm.response.json().args.source).to.eql("newman-sample-github-collection");
                                        if (argsProp.TryGetProperty("source", out JsonElement sourceProp))
                                        {
                                            string expected = "newman-sample-github-collection";
                                            bool pass = sourceProp.GetString() == expected;
                                            Console.WriteLine($"Assert args.source == '{expected}': " + (pass ? "PASS" : "FAIL"));
                                        }
                                    }
                                }
                            }
                            catch
                            {
                                Console.WriteLine("Failed to parse JSON response for assertion.");
                            }
                        }
                    }
                }
            }
        }

    }
}
