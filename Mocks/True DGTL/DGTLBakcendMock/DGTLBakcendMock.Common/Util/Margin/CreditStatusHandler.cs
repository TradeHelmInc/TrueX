using DGTLBackendMock.Common.DTO.Account.V2.Credit_UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.Util.Margin
{
    public class CreditStatusHandler
    {
        public static  FirmsTradingStatusUpdateResponse ProcessFirmsTradingStatusUpdateRequest (FirmsCreditRecord[] firms, string firmId, char status,
                                                                                        string token, string uuid)
        {
            TimeSpan epochElapsed = DateTime.Now - new DateTime(1970, 1, 1);
            FirmsCreditRecord firm = firms.Where(x => x.FirmId == firmId).FirstOrDefault();

            if (firm != null)
            {
                try
                {
                    firm.cTradingStatus = status;

                    FirmsTradingStatusUpdateResponse resp = new FirmsTradingStatusUpdateResponse()
                    {
                        Success = true,
                        Firm = firm,
                        JsonWebToken = token,
                        Msg = "FirmsTradingStatusUpdateResponse",
                        Time = Convert.ToInt64(epochElapsed.TotalMilliseconds),
                        Uuid = uuid
                    };

                    return resp;
                }
                catch (Exception ex)
                {
                    FirmsTradingStatusUpdateResponse resp = new FirmsTradingStatusUpdateResponse()
                    {
                        Success = false,
                        JsonWebToken = token,
                        Message = ex.Message,
                        Msg = "FirmsTradingStatusUpdateResponse",
                        Time = Convert.ToInt64(epochElapsed.TotalMilliseconds),
                        Uuid = uuid
                    };

                    return resp;
                }
            }
            else
            {

                FirmsTradingStatusUpdateResponse resp = new FirmsTradingStatusUpdateResponse()
                {
                    Success = false,
                    JsonWebToken = token,
                    Message = string.Format("FirmId {0} not found", firm),
                    Msg = "FirmsTradingStatusUpdateResponse",
                    Time = Convert.ToInt64(epochElapsed.TotalMilliseconds),
                    Uuid = uuid

                };

                return resp;
            }
        }
    }
}
