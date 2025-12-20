namespace BotCarniceria.Core.Domain.Constants;

public static class ConfigurationKeys
{
    public static class WhatsApp
    {
        public const string PhoneNumberId = "WhatsApp_PhoneNumberId";
        public const string AccessToken = "WhatsApp_AccessToken";
        public const string VerifyToken = "WhatsApp_VerifyToken";
        public const string AppSecret = "WhatsApp_AppSecret";
    }

    public static class Business
    {
        public const string Schedule = "Negocio_Horarios";
        public const string Address = "Negocio_Direccion";
        public const string Phone = "Negocio_Telefono";
        public const string DeliveryTime = "Negocio_TiempoEntrega";
    }

    public static class Printers
    {
        public const string Name = "Printers.Name";
        public const string IpAddress = "Printers.IpAddress";
        public const string Port = "Printers.Port";
        public const string Configuration = "Printers.Configuration";
    }

    public static class Session
    {
        public const string BotTimeoutMinutes = "Session.BotTimeoutMinutes";
        public const string BotWarningMinutes = "Session.BotWarningMinutes";
        public const string BlazorTimeoutMinutes = "Session.BlazorTimeoutMinutes";
        public const string BlazorWarningMinutes = "Session.BlazorWarningMinutes";
    }

    public static class Orders
    {
        public const string LateOrderWarningStartHour = "Orders.LateOrderWarningStartHour";
    }

    public static class System
    {
        public const string PrintRetryCount = "System.PrintRetryCount";
        public const string MessageRetryCount = "System.MessageRetryCount";
        public const string RetryIntervalSeconds = "System.RetryIntervalSeconds";
        public const string WorkQueueCount = "System.WorkQueueCount";
        public const string TimeZoneId = "System.TimeZoneId";
    }
}
