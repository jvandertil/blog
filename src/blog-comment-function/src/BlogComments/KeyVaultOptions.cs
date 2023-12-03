using System;

namespace BlogComments
{
    public class KeyVaultOptions
    {
        public Uri Url { get; set; }

        public string KeyName { get; set; }

        public KeyVaultOptions()
        {
            Url = null!;
            KeyName = null!;
        }
    }
}
