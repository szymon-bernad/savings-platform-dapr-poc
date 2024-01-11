using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SavingsPlatform.Contracts.Payments.Requests
{
    public record ProcessOutboundPayment(
         string BeneficiaryAccountRef,
         string DebtorAccountRef,
         decimal Amount,
         DateTime TransactionDate,
         string Reference);
}
