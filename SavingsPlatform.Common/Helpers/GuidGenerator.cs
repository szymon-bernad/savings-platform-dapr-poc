using System.Security.Cryptography;
using System.Text;

namespace SavingsPlatform.Common.Helpers
{
    public static class GuidGenerator
    {
        public static Guid AsGuid(string seed)
        {
            byte[] data = MD5.Create()
                            .ComputeHash(Encoding.Default.GetBytes(seed));
            return new Guid(data);
        }
    }
}
