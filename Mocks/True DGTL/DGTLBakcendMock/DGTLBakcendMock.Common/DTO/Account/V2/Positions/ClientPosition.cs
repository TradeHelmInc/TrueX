using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGTLBackendMock.Common.DTO.Account.V2
{
    public class ClientPosition : WebSocketMessageV2
    {
        #region Public Attributes

        public string Symbol { get; set; }

        public string UserId { get; set; }

        public double Contracts { get; set; }

        public double Price { get; set; }

        public bool MarginFunded { get; set; }

        #endregion

        #region Public Static Methods

        public ClientPosition Clone()
        {
            return new ClientPosition()
            {
                Symbol = Symbol,
                UserId = UserId,
                Contracts = Contracts,
                Price = Price,
                MarginFunded = MarginFunded
            };
        
        }

        #endregion
    }
}
