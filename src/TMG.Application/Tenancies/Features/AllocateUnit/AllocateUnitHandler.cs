using TMG.Application.Authentication.Constants;
using TMG.Contracts.Events;
using TMG.Domain.Authentication.Entities;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Common.Messaging;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Properties.Entities;
using TMG.Domain.Properties.Specifications;
using TMG.Domain.Stakeholders.Entities;
using TMG.Domain.Stakeholders.Specifications;
using TMG.Domain.Tenancies.Entities;
using TMG.Domain.Tenancies.Specifications;

namespace TMG.Application.Tenancies.Features.AllocateUnit;

public sealed class AllocateUnitHandler(
    IAuthenticationIdentityService identityService,
    IRepository<Stakeholder> stakeholderRepository,
    IRepository<StakeholderType> stakeholderTypeRepository,
    IRepository<Property> propertyRepository,
    IRepository<Unit> unitRepository,
    IRepository<Tenancy> tenancyRepository,
    IEventPublisher eventPublisher,
    IUnitOfWork unitOfWork)
{
    public async Task<AllocateUnitResult> HandleAsync(AllocateUnitCommand command, CancellationToken cancellationToken)
    {
        if (command.ActorContext.ClientId is not { } clientId ||
            command.ActorContext.StakeholderId is not { } managerStakeholderId)
        {
            return new AllocateUnitResult(AllocateUnitStatus.NotAuthenticated);
        }

        var manager = await stakeholderRepository.GetByIdAsync(managerStakeholderId, cancellationToken);
        if (manager is null)
        {
            return new AllocateUnitResult(AllocateUnitStatus.NotAuthenticated);
        }

        var unit = await unitRepository.FirstOrDefaultAsync(
            new UnitByIdSpecification(command.UnitId, clientId),
            cancellationToken);
        if (unit is null)
        {
            return new AllocateUnitResult(AllocateUnitStatus.UnitNotFound);
        }

        if (unit.Status != UnitStatus.Available ||
            await tenancyRepository.AnyAsync(new ActiveTenancyByUnitSpecification(unit.Id), cancellationToken))
        {
            return new AllocateUnitResult(AllocateUnitStatus.UnitNotAvailable);
        }

        var property = await propertyRepository.GetByIdAsync(unit.PropertyId, cancellationToken);

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        // Allocate to an existing account when the email is already registered under this client;
        // otherwise provision a new pending tenant account.
        var existingUser = await identityService.FindByEmailAsync(command.TenantEmail);
        Guid tenantStakeholderId;
        if (existingUser is null)
        {
            var tenantType = await stakeholderTypeRepository.FirstOrDefaultAsync(
                new StakeholderTypeByClientAndKeySpecification(clientId, StakeholderDefaults.Types.TenantKey),
                cancellationToken);
            if (tenantType is null)
            {
                return new AllocateUnitResult(AllocateUnitStatus.TenantTypeNotConfigured);
            }

            var user = AppUser.Create(command.TenantEmail, command.TenantFirstName, command.TenantLastName);
            var createResult = await identityService.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                return new AllocateUnitResult(AllocateUnitStatus.EmailRegisteredToAnotherClient);
            }

            var tenant = Stakeholder.Create(
                user.Id,
                clientId,
                manager.CountryId,
                tenantType.Id,
                command.TenantFirstName,
                command.TenantLastName);
            await stakeholderRepository.AddAsync(tenant, cancellationToken);
            tenantStakeholderId = tenant.Id;
        }
        else
        {
            var existingStakeholder = await stakeholderRepository.FirstOrDefaultAsync(
                new StakeholderByAppUserIdSpecification(existingUser.Id),
                cancellationToken);
            if (existingStakeholder is null || existingStakeholder.ClientId != clientId)
            {
                return new AllocateUnitResult(AllocateUnitStatus.EmailRegisteredToAnotherClient);
            }

            tenantStakeholderId = existingStakeholder.Id;
        }

        var tenancy = Tenancy.Invite(
            clientId,
            unit.PropertyId,
            unit.Id,
            tenantStakeholderId,
            command.TenantEmail,
            command.LeaseStartDate,
            command.TermMonths);
        await tenancyRepository.AddAsync(tenancy, cancellationToken);

        unit.MarkOccupied();
        unitRepository.Update(unit);

        await eventPublisher.PublishAsync(new TenantInvited
        {
            StakeholderId = tenantStakeholderId,
            ClientId = clientId,
            FlowId = command.ActorContext.FlowId,
            UnitLabel = unit.Label,
            PropertyName = property?.Name ?? string.Empty
        }, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new AllocateUnitResult(AllocateUnitStatus.Success, tenancy.Id);
    }
}
