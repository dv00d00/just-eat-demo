using System;

namespace HelloWorld.Interfaces
{
    public static class WellKnownIds
    {
        public const string StreamProvider = "SMSProvider";
        public const string StreamOrdersNamespace = "Orders";
        public static readonly Guid OrderUpdates = Guid.Parse("060133EE-321E-48D8-860A-35279AA525AE");
    }
}