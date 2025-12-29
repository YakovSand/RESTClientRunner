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
                Console.WriteLine("Failed: collection response is empty!");
            }
            else
            {
                Console.WriteLine("Success: collection received.");
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                PostmanCollection collection =
                    JsonSerializer.Deserialize<PostmanCollection>(json, options)!;

                Console.WriteLine($"\nCollection: {collection.Info.Name}");

                bool exit = false;
                while (!exit)
                {
                    Console.WriteLine("\nMenu:");
                    Console.WriteLine("1 - List requests");
                    Console.WriteLine("2 - Run a request");
                    Console.WriteLine("3 - Add new request");
                    Console.WriteLine("4 - Save collection to JSON");
                    Console.WriteLine("Q - Quit");
                    Console.Write("Choice: ");
                    string input = Console.ReadLine()?.Trim().ToUpper() ?? "";
                    switch (input)
                    {
                        case "1":
                            ListRequests(collection);
                            break;
                        case "2":
                            await RunRequests(collection);
                            break;
                        case "3":
                            AddNewRequest(collection);
                            break;
                        case "4":
                            SaveCollection(collection);
                            break;
                        case "Q":
                            exit = true;
                            break;
                        default:
                            Console.WriteLine("Invalid input.");
                            break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to retrieve Postman Collection : {ex.Message}");
        }
    }

    // Choice 1: List all requests in the collection (run-time)
    static void ListRequests(PostmanCollection collection)
    {
        if (collection.Item.Count == 0)
        {
            Console.WriteLine("No requests in collection.");
            return;
        }

        Console.WriteLine("\nRequests:");
        for (int i = 0; i < collection.Item.Count; i++)
        {
            var r = collection.Item[i].Request;
            Console.WriteLine($"{i + 1}. {r.Method} {r.Url.Raw} ({collection.Item[i].Name})");
        }
    }

    // Choice 2: Run a request interactively
    static async Task RunRequests(PostmanCollection collection)
    {
        if (collection.Item.Count == 0)
        {
            Console.WriteLine("No requests to run.");
            return;
        }

        ListRequests(collection);
        Console.Write("Enter request number to run (0 = all): ");
        string input = Console.ReadLine() ?? "0";

        if (input == "0")
        {
            foreach (var item in collection.Item)
                await ExecuteRequest(item.Request, item.Event);
        }
        else if (int.TryParse(input, out int index) &&
                 index > 0 && index <= collection.Item.Count)
        {
            await ExecuteRequest(collection.Item[index - 1].Request, collection.Item[index - 1].Event);
        }
        else
        {
            Console.WriteLine("Invalid choice.");
        }
    }

    // Chice 3: Add a new request interactively (run-time)
    static void AddNewRequest(PostmanCollection collection)
    {
        Console.Write("Request name: ");
        string name = Console.ReadLine() ?? "New Request";

        Console.Write("Method (GET/POST/PUT/DELETE): ");
        string method = Console.ReadLine()?.ToUpper() ?? "GET";

        Console.Write("URL: ");
        string url = Console.ReadLine() ?? "";

        // Headers
        var headers = new List<PostmanHeader>();
        Console.Write("Add headers? (y/n): ");
        if ((Console.ReadLine()?.Trim().ToLower() ?? "") == "y")
        {
            while (true)
            {
                Console.Write("Header key (empty to finish): ");
                string key = Console.ReadLine() ?? "";
                if (string.IsNullOrWhiteSpace(key)) break;
                Console.Write("Header value: ");
                string value = Console.ReadLine() ?? "";
                headers.Add(new PostmanHeader { Key = key, Value = value });
            }
        }

        // Body
        PostmanBody? body = null;
        if (method == "POST" || method == "PUT")
        {
            Console.WriteLine("Enter request body (empty to skip):");
            string rawBody = Console.ReadLine() ?? "";
            if (!string.IsNullOrWhiteSpace(rawBody))
            {
                body = new PostmanBody { Raw = rawBody };
            }
        }

        // Post-request scripts
        List<string>? scriptLines = null;
        Console.Write("Add Post-request tests? (y/n): ");
        if ((Console.ReadLine()?.Trim().ToLower() ?? "") == "y")
        {
            scriptLines = new List<string>();
            Console.WriteLine("Enter Post-request script lines (empty line to finish):");
            while (true)
            {
                string line = Console.ReadLine() ?? "";
                if (string.IsNullOrWhiteSpace(line)) break;
                scriptLines.Add(line);
            }
        }

        List<PostmanEvent>? events = null;
        if (scriptLines != null && scriptLines.Count > 0)
        {
            events = new List<PostmanEvent>
        {
            new PostmanEvent
            {
                Listen = "test",
                Script = new PostmanScript { Exec = scriptLines }
            }
        };
        }

        var newItem = new PostmanItem
        {
            Name = name,
            Request = new PostmanRequest
            {
                Method = method,
                Url = new PostmanUrl { Raw = url },
                Header = headers.Count > 0 ? headers : null,
                Body = body
            },
            Event = events
        };

        collection.Item.Add(newItem);
        Console.WriteLine($"Request '{name}' added to collection.");
    }


    // Choice 4: Save the Postman collection to a JSON file (run-time)
    static void SaveCollection(PostmanCollection collection)
    {
        Console.Write("Enter file name to save (e.g., collection.json): ");
        string fileName = Console.ReadLine() ?? "collection.json";

        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(collection, options);

        File.WriteAllText(fileName, json);
        Console.WriteLine($"Collection saved to {fileName}");
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
                    bool handled = false; // To track if we recognized the assertion
                    Console.WriteLine("\nRunning Post-request assertions:");
                    foreach (var line in ev.Script.Exec)
                    {
                        string trimmed = line.Trim();

                        // Simple assertion: pm.response.to.be.ok
                        if (trimmed.Contains("pm.response.to.be.ok"))
                        {
                            bool pass = ((int)response.StatusCode == 200);
                            Console.WriteLine($"Assert Status == 200: " + (pass ? "PASS" : "FAIL"));
                            handled = true;
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
                                            handled = true;
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

                    if (!handled)
                    {
                        Console.WriteLine($"Unsupported assertion!");
                    }
                }
            }

        }
    }

    // Helpers //
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
}
