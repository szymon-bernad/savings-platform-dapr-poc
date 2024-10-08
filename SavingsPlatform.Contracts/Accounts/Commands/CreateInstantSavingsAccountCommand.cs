﻿using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SavingsPlatform.Contracts.Accounts.Commands
{
    public record CreateInstantSavingsAccountCommand(
        string ExternalRef,
        decimal InterestRate,
        string PlatformId,
        string TransferId) : IRequest;
}