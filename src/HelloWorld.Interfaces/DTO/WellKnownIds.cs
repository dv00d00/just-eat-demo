using System;

namespace HelloWorld.Interfaces
{
    public static class WellKnownIds
    {
        public const string StreamProvider = "SMSProvider";
        public const string StreamOrdersNamespace = "Orders";
        public const string DatabaseUpdatesNamespace = "DatabaseUpdates";
        public static readonly Guid OrderUpdates = Guid.Parse("060133EE-321E-48D8-860A-35279AA525AE");
        public static readonly Guid DatabaseUpdates = Guid.Parse("3BB1BD6E-9238-4230-8A8D-077C02C9EB99");
    }
}