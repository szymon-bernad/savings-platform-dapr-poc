﻿using MediatR;
using SavingsPlatform.Accounts.Aggregates.InstantAccess;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts.Commands;

namespace SavingsPlatform.Accounts.Handlers
{
    public class CreateInstantSavingsAccountCommandHandler : IRequestHandler<CreateInstantSavingsAccountCommand>
    {
        private readonly IAggregateRootFactory<InstantAccessSavingsAccount, InstantAccessSavingsAccountState> _aggregateFactory;
        private readonly IStateEntryRepository<InstantAccessSavingsAccountState> _repository;

        public CreateInstantSavingsAccountCommandHandler(
            IAggregateRootFactory<InstantAccessSavingsAccount, InstantAccessSavingsAccountState> aggregateFactory,
            IStateEntryRepository<InstantAccessSavingsAccountState> repository)
        {
            _aggregateFactory = aggregateFactory;
            _repository = repository;
        }

        public async Task Handle(CreateInstantSavingsAccountCommand request, CancellationToken cancellationToken)
        {
            var instance = await _aggregateFactory.GetInstanceAsync();
            await instance.CreateAsync(request);
        }
    }
}
