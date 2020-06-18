using DGTLBackendMock.Common.DTO.Account.V2;
using DGTLBackendMock.Common.DTO.SecurityList;
using DGTLBackendMock.Common.DTO.Temp.Positions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.Util.Settlement
{
    public class PositionsCalculator
    {
        #region Public Static Methods

        public static NetPositionDTO[] FindNetPositionsForFirm(SecurityMasterRecord[] securities, UserRecord[] UserRecords, ClientPosition[] Positions, 
                                                         string firmId)
        {
            List<UserRecord> usersForFirm = UserRecords.Where(x => x.FirmId == firmId).ToList();
            List<NetPositionDTO> netPositionsArr = new List<NetPositionDTO>();
            foreach (SecurityMasterRecord security in securities)
            {
                double netContracts = 0;
                foreach (UserRecord user in usersForFirm)
                {
                    Positions.Where(x => x.Symbol == security.Symbol && x.UserId == user.UserId)
                             .ToList().ForEach(x => netContracts += x.Contracts);
                }

                if (netContracts != 0)
                {
                    netPositionsArr.Add(new NetPositionDTO()
                    {
                        FirmId = firmId,
                        AssetClass = security.AssetClass,
                        Symbol = security.Symbol,
                        MaturityDate = security.GetMaturityDate(),
                        NetContracts = netContracts
                    });
                }
            }


            return netPositionsArr.ToArray();
        }

        #endregion
    }
}
