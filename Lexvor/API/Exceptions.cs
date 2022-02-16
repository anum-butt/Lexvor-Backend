using System;

namespace Lexvor.API
{
    public class StripeTestSubInLiveModeException : Exception {
        public StripeTestSubInLiveModeException() : base("The subscription attached to this account is for test mode, we are in live mode.") {}
    }
}
