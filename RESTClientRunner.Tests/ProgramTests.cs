using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using System.Collections.Generic;

namespace RESTClientRunner.Tests
{
    public class ProgramTests
    {
        [Fact]
        public void AddNewRequest_ShouldAddRequestWithHeadersAndBodyAndScripts()
        {
            // Arrange
            var collection = new PostmanCollection();
            var itemName = "Test Request";

            // Simulate user input
            var headers = new List<PostmanHeader> { new() { Key = "Content-Type", Value = "application/json" } };
            var body = new PostmanBody { Raw = "{\"user\":\"john\",\"email\":\"john@e.com\"}" };
            var scriptLines = new List<string> { "pm.test('Status is 200', function () {pm.response.to.be.ok})" };
            var events = new List<PostmanEvent> { new() { Listen = "test", Script = new PostmanScript { Exec = scriptLines } } };

            var newItem = new PostmanItem
            {
                Name = itemName,
                Request = new PostmanRequest
                {
                    Method = "POST",
                    Url = new PostmanUrl { Raw = "https://postman-echo.com/post" },
                    Header = headers,
                    Body = body
                },
                Event = events
            };

            collection.Item.Add(newItem);

            // Act
            var addedItem = collection.Item[0];

            // Assert
            Assert.Equal(itemName, addedItem.Name);  // Check name
            Assert.Equal("POST", addedItem.Request.Method); // Check method
            Assert.Equal("https://postman-echo.com/post", addedItem.Request.Url.Raw); // Check URL
            Assert.Single(addedItem.Request.Header); // Check headers
            Assert.Equal("{\"user\":\"john\",\"email\":\"john@e.com\"}", addedItem.Request.Body.Raw); // Check body
            Assert.Single(addedItem.Event); // Check events
            Assert.Single(addedItem.Event[0].Script.Exec); // Check script lines
        }
    }
}