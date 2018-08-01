﻿using System;
using System.Threading.Tasks;

namespace NCS.DSS.Interaction.GetInteractionByIdHttpTrigger.Service
{
    public interface IGetInteractionByIdHttpTriggerService
    {
        Task<Models.Interaction> GetInteractionForCustomerAsync(Guid customerId, Guid interactionId);
    }
}