using Newtonsoft.Json;

namespace BotCarniceria.Infrastructure.Services.External.WhatsApp.Models;

public class MessagePayload
{
    [JsonProperty("messaging_product")]
    public string Messaging_Product { get; set; } = "whatsapp";
    
    [JsonProperty("recipient_type")]
    public string? Recipient_Type { get; set; } = "individual";
    
    [JsonProperty("to")]
    public string To { get; set; } = string.Empty;
    
    [JsonProperty("type")]
    public string Type { get; set; } = "text";
    
    [JsonProperty("text")]
    public TextPayload? Text { get; set; }
    [JsonProperty("interactive")]
    public InteractivePayload? Interactive { get; set; }
}

public class TextPayload
{
    [JsonProperty("preview_url")]
    public bool Preview_Url { get; set; } = false;
    [JsonProperty("body")]
    public string Body { get; set; } = string.Empty;
}

public class InteractivePayload
{
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty; // "button" or "list"
    [JsonProperty("header")]
    public InteractiveHeader? Header { get; set; }
    [JsonProperty("body")]
    public InteractiveBody Body { get; set; } = new();
    [JsonProperty("footer")]
    public InteractiveFooter? Footer { get; set; }
    [JsonProperty("action")]
    public InteractiveAction? Action { get; set; }
}

public class InteractiveHeader
{
    [JsonProperty("type")]
    public string Type { get; set; } = "text";
    [JsonProperty("text")]
    public string Text { get; set; } = string.Empty;
}

public class InteractiveBody
{
    [JsonProperty("text")]
    public string Text { get; set; } = string.Empty;
}

public class InteractiveFooter
{
    [JsonProperty("text")]
    public string Text { get; set; } = string.Empty;
}

public class InteractiveAction
{
    [JsonProperty("buttons")]
    public List<InteractiveButton>? Buttons { get; set; }
    [JsonProperty("button")]
    public string? Button { get; set; }
    [JsonProperty("sections")]
    public List<InteractiveSection>? Sections { get; set; }
}

public class InteractiveButton
{
    [JsonProperty("type")]
    public string Type { get; set; } = "reply";
    [JsonProperty("reply")]
    public InteractiveReply Reply { get; set; } = new();
}

public class InteractiveReply
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;
    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;
}

public class InteractiveSection
{
    [JsonProperty("title")]
    public string? Title { get; set; }
    [JsonProperty("rows")]
    public List<InteractiveRow> Rows { get; set; } = new();
}

public class InteractiveRow
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;
    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;
    [JsonProperty("description")]
    public string? Description { get; set; }
}

public class WhatsAppSendResponse
{
    [JsonProperty("messaging_product")]
    public string? MessagingProduct { get; set; }

    [JsonProperty("contacts")]
    public List<WhatsAppContact>? Contacts { get; set; }

    [JsonProperty("messages")]
    public List<WhatsAppSentMessage>? Messages { get; set; }
}

public class WhatsAppContact
{
    [JsonProperty("input")]
    public string? Input { get; set; }
    [JsonProperty("wa_id")]
    public string? WaId { get; set; }
}

public class WhatsAppSentMessage
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;
}
