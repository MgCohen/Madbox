using System;
using System.Threading;
using GameModuleDTO.Modules.Gold;
using GameModuleDTO.ModuleRequests;
using Madbox.LiveOps;
using Madbox.Gold.Contracts;
using System.Threading.Tasks;
using UnityEngine;

namespace Madbox.Gold
{
    public class GoldService : GameClientModuleBase<GoldGameData>, IGoldService, IResponseHandler<GoldChangedResponse>
    {
        public GoldService(ILiveOpsService liveOpsService)
        {
            if (liveOpsService == null)
            {
                throw new ArgumentNullException(nameof(liveOpsService));
            }

            this.liveOpsService = liveOpsService;
            wallet = new GoldWallet();
        }

        private readonly ILiveOpsService liveOpsService;
        private GoldWallet wallet;
        private int pendingOptimisticGold;

        public GoldWallet GetWallet()
        {
            return wallet;
        }

        public void Add(int amount)
        {
            GuardAmount(amount);
            wallet.Add(amount);
            _ = AddAsync(amount);
        }

        public void ApplyOptimisticCompletionReward(int amount)
        {
            GuardAmount(amount);
            wallet.Add(amount);
            checked
            {
                pendingOptimisticGold += amount;
            }
        }

        private void GuardAmount(int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
            }
        }

        private async Task AddAsync(int amount, CancellationToken cancellationToken = default)
        {
            try
            {
                AddGoldRequest request = new AddGoldRequest(amount);
                _ = await liveOpsService.CallAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public void Handle(GoldChangedResponse response)
        {
            if (response == null)
            {
                return;
            }

            ApplyServerGold(response);
        }

        private void ApplyServerGold(GoldChangedResponse response)
        {
            if (response == null || response.Diff <= 0)
            {
                return;
            }

            int diff = response.Diff > int.MaxValue ? int.MaxValue : (int)response.Diff;
            if (pendingOptimisticGold > 0)
            {
                int consumed = Math.Min(pendingOptimisticGold, diff);
                pendingOptimisticGold -= consumed;
                diff -= consumed;
            }

            if (diff > 0)
            {
                wallet.Add(diff);
            }
        }

        protected override Task OnInitializedAsync(GoldGameData moduleData)
        {
            int liveOpsGold = moduleData != null ? (moduleData.Current > int.MaxValue ? int.MaxValue : (int)moduleData.Current) : 0;
            wallet = new GoldWallet(liveOpsGold);
            pendingOptimisticGold = 0;
            return Task.CompletedTask;
        }
    }
}
