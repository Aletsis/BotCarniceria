using System.Text.Json;
using BotCarniceria.Core.Application.DTOs.WhatsApp;
using FluentAssertions;
using Xunit;

namespace BotCarniceria.UnitTests.Application.DTOs;

public class WhatsAppSerializationTests
{
    [Fact]
    public void WebhookPayload_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = @"
        {
            ""object"": ""whatsapp_business_account"",
            ""entry"": [
                {
                    ""id"": ""12345"",
                    ""changes"": [
                        {
                            ""value"": {
                                ""messaging_product"": ""whatsapp"",
                                ""metadata"": {
                                    ""display_phone_number"": ""1234567890"",
                                    ""phone_number_id"": ""98765""
                                },
                                ""contacts"": [
                                    {
                                        ""profile"": {
                                            ""name"": ""John Doe""
                                        },
                                        ""wa_id"": ""123123123""
                                    }
                                ],
                                ""messages"": [
                                    {
                                        ""from"": ""123123123"",
                                        ""id"": ""wamid.HBgLM..."",
                                        ""timestamp"": ""1672531200"",
                                        ""type"": ""text"",
                                        ""text"": {
                                            ""body"": ""Hello World""
                                        }
                                    }
                                ]
                            },
                            ""field"": ""messages""
                        }
                    ]
                }
            ]
        }";

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // Act
        var payload = JsonSerializer.Deserialize<WebhookPayload>(json, options);

        // Assert
        payload.Should().NotBeNull();
        payload!.Object.Should().Be("whatsapp_business_account");
        payload.Entry.Should().HaveCount(1);

        var entry = payload.Entry![0];
        entry.Id.Should().Be("12345");
        entry.Changes.Should().HaveCount(1);

        var change = entry.Changes![0];
        change.Field.Should().Be("messages");
        change.Value.Should().NotBeNull();

        var value = change.Value!;
        value.Messaging_Product.Should().Be("whatsapp");
        value.Metadata.Should().NotBeNull();
        value.Metadata!.Display_Phone_Number.Should().Be("1234567890");
        value.Metadata.Phone_Number_Id.Should().Be("98765");

        value.Contacts.Should().HaveCount(1);
        value.Contacts![0].Profile!.Name.Should().Be("John Doe");
        value.Contacts![0].Wa_Id.Should().Be("123123123");

        value.Messages.Should().HaveCount(1);
        var message = value.Messages![0];
        message.From.Should().Be("123123123");
        message.Type.Should().Be("text");
        message.Text.Should().NotBeNull();
        message.Text!.Body.Should().Be("Hello World");
    }

    [Fact]
    public void WebhookPayload_ShouldSerializeCorrectly()
    {
        // Arrange
        var payload = new WebhookPayload
        {
            Object = "whatsapp_business_account",
            Entry = new List<WhatsAppEntry>
            {
                new WhatsAppEntry
                {
                    Id = "12345",
                    Changes = new List<WhatsAppChange>
                    {
                        new WhatsAppChange
                        {
                            Field = "messages",
                            Value = new WhatsAppValue
                            {
                                Messaging_Product = "whatsapp",
                                Metadata = new WhatsAppMetadata
                                {
                                    Display_Phone_Number = "1234567890",
                                    Phone_Number_Id = "98765"
                                },
                                Contacts = new List<WhatsAppContact>
                                {
                                    new WhatsAppContact
                                    {
                                        Profile = new WhatsAppProfile { Name = "John Doe" },
                                        Wa_Id = "123123123"
                                    }
                                },
                                Messages = new List<WhatsAppMessage>
                                {
                                    new WhatsAppMessage
                                    {
                                        From = "123123123",
                                        Id = "wamid.HBgLM...",
                                        Timestamp = "1672531200",
                                        Type = "text",
                                        Text = new WhatsAppText { Body = "Hello World" }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(payload);

        // Assert
        json.Should().Contain("\"Object\":\"whatsapp_business_account\""); // Assuming PascalCase by default from property names
        json.Should().Contain("\"Messaging_Product\":\"whatsapp\"");
        json.Should().Contain("\"Display_Phone_Number\":\"1234567890\"");
    }
}
