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
    public class SettlementCalculator
    {
        #region Public Statuc Methods

        public static  NetPositionDTO GetNetPositionsToSettl(SecurityMasterRecord[] securities, UserRecord[] users, 
                                                            ClientPosition[] positions,string firmId, string instrumentId)
        {
            NetPositionDTO[] netPosForFirm = PositionsCalculator.FindNetPositionsForFirm(securities, users, positions, firmId);

            return netPosForFirm.Where(x => x.Symbol == instrumentId || instrumentId == null)
                                .OrderBy(x => x.MaturityDate).FirstOrDefault();
        }

        public static NetPositionDTO[] GetMatchingCounterparties(NetPositionDTO mySide,SecurityMasterRecord[] securities, UserRecord[] users, ClientPosition[] positions)
        {
            

            List<string> firmList = new List<string>();
            foreach (UserRecord user in users.Where(x=>x.FirmId!=mySide.FirmId))
            {
                if(!firmList.Contains(user.FirmId))
                    firmList.Add(user.FirmId);
            }

            List<NetPositionDTO> possibleCounterparties = new List<NetPositionDTO>();
            foreach (string firmId in firmList)
            {
                NetPositionDTO opSide = GetNetPositionsToSettl(securities, users, positions, firmId, mySide.Symbol);

                if (opSide != null && Math.Sign(mySide.NetContracts) != Math.Sign(opSide.NetContracts))
                {
                    possibleCounterparties.Add(opSide);    
                }
            }

            //imperfect algo to match counterparites
            double acum = 0;
            List<NetPositionDTO> matchingCounterparties = new List<NetPositionDTO>();
            foreach (NetPositionDTO opSide in possibleCounterparties.OrderBy(x=>Math.Abs(x.NetContracts)))
            {
                acum += Math.Abs(opSide.NetContracts);

                matchingCounterparties.Add(opSide);

                if (acum >= mySide.NetContracts)
                    break;
            
            }


            return matchingCounterparties.ToArray();
        
        }

        #endregion
    }
}
