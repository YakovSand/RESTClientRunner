# RESTClientRunner

A simple C# tool to download, manage, and execute Postman Collections from REST APIs.

---

## Features

- Download a Postman Collection from a predefined URL.
- Parse the JSON into C# objects.
- List all requests with URL and method.
- Execute a single request or all requests.
- Supports Post-request scripts in the collection (basic assertions like status code check or JSON property check).
- Add new requests interactively:
  - Specify request name, method (GET/POST/PUT/DELETE), URL, headers, and body.
  - Automatically add to the in-memory collection.
  - Save updated collection back to JSON.
- Run newly added requests just like the existing ones.

---

## Prerequisites

- .NET 8 SDK or newer.
- Optional: Visual Studio 2022+ for Test Explorer and debugging.

---

## How to Execute

1. Build the project:

```bash
dotnet build
```
2. Run the application:
```bash
dotnet run --project RESTClientRunner
```
3. Follow the prompts to download a Postman Collection or add new requests.
4. Running tests
```bash
cd .\RESTClientRunner.Tests\
# List all tests
dotnet test --list-tests

# Run all tests
dotnet test
```
