namespace BotCarniceria.Core.Application.DTOs.WhatsApp;

public class WebhookPayload
{
    public string? Object { get; set; }
    public List<WhatsAppEntry>? Entry { get; set; }
}

public class WhatsAppEntry
{
    public string? Id { get; set; }
    public List<WhatsAppChange>? Changes { get; set; }
}

public class WhatsAppChange
{
    public WhatsAppValue? Value { get; set; }
    public string? Field { get; set; }
}

public class WhatsAppValue
{
    public string? Messaging_Product { get; set; }
    public WhatsAppMetadata? Metadata { get; set; }
    public List<WhatsAppContact>? Contacts { get; set; }
    public List<WhatsAppMessage>? Messages { get; set; }
    public List<WhatsAppStatus>? Statuses { get; set; }
}

public class WhatsAppMetadata
{
    public string? Display_Phone_Number { get; set; }
    public string? Phone_Number_Id { get; set; }
}

public class WhatsAppContact
{
    public WhatsAppProfile? Profile { get; set; }
    public string? Wa_Id { get; set; }
}

public class WhatsAppProfile
{
    public string? Name { get; set; }
}

public class WhatsAppMessage
{
    public string? From { get; set; }
    public string? Id { get; set; }
    public string? Timestamp { get; set; }
    public string? Type { get; set; }
    public WhatsAppText? Text { get; set; }
    public WhatsAppInteractive? Interactive { get; set; }
    public WhatsAppMedia? Image { get; set; }
    public WhatsAppMedia? Audio { get; set; }
    public WhatsAppMedia? Video { get; set; }
    public WhatsAppMedia? Document { get; set; }
    public WhatsAppMedia? Sticker { get; set; }

    public WhatsAppLocation? Location { get; set; }
    public List<WhatsAppMessageContact>? Contacts { get; set; }
}

public class WhatsAppMessageContact
{
    public WhatsAppContactName? Name { get; set; }
    public List<WhatsAppContactPhone>? Phones { get; set; }
}

public class WhatsAppContactName
{
    public string? Formatted_Name { get; set; }
    public string? First_Name { get; set; }
}

public class WhatsAppContactPhone
{
    public string? Phone { get; set; }
    public string? Type { get; set; }
}

public class WhatsAppText
{
    public string? Body { get; set; }
}

public class WhatsAppMedia
{
    public string? Id { get; set; }
    public string? Mime_Type { get; set; }
    public string? Sha256 { get; set; }
    public string? Caption { get; set; }
    public string? Filename { get; set; }
    public bool? Voice { get; set; }
}

public class WhatsAppLocation
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Name { get; set; }
    public string? Address { get; set; }
}

public class WhatsAppInteractive
{
    public string? Type { get; set; }
    public WhatsAppButtonReply? Button_Reply { get; set; }
    public WhatsAppListReply? List_Reply { get; set; }
}

public class WhatsAppButtonReply
{
    public string? Id { get; set; }
    public string? Title { get; set; }
}

public class WhatsAppListReply
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
}

public class WhatsAppStatus
{
    public string? Id { get; set; }
    public string? Status { get; set; }
    public string? Timestamp { get; set; }
    public string? Recipient_Id { get; set; }
}
