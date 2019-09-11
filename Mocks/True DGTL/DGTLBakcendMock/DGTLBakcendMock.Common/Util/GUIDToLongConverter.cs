using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.Util
{
    public class GUIDToLongConverter
    {
        public static long GUIDToLong(string strGuid)
        {
            Guid guid = new Guid(strGuid);
            byte[] gb = guid.ToByteArray();
            int i = BitConverter.ToInt32(gb, 0);

            long l = BitConverter.ToInt64(gb, 0);

            if (l < 0)
                l *= -1;

            return l;
        }
    }
}
