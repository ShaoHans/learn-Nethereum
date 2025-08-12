using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nethereum.JsonRpc.WebSocketStreamingClient;
using Nethereum.RPC.Reactive.Eth.Subscriptions;
using Nethereum.Web3;

namespace DepositMonitoring;

internal class WsMonitoringBgService : BackgroundService
{
    private readonly ILogger<WsMonitoringBgService> _logger;
    private readonly string _rpcUrl = string.Empty;
    private readonly string _wsRpcUrl = string.Empty;
    private readonly string _toAddress = string.Empty;

    public WsMonitoringBgService(
        IConfiguration configuration,
        ILogger<WsMonitoringBgService> logger
    )
    {
        _logger = logger;
        _rpcUrl = configuration["Ethereum:NodeUrl"]!;
        _wsRpcUrl = configuration["Ethereum:WsNodeUrl"]!;
        _toAddress = configuration["Ethereum:ToAddress"]!;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var web3 = new Web3(_rpcUrl);
        using var client = new StreamingWebSocketClient(_wsRpcUrl);

        // ETH 充值监听（订阅新区块头）
        var newBlockSubscription = new EthNewBlockHeadersObservableSubscription(client);
        newBlockSubscription
            .GetSubscribeResponseAsObservable()
            .Subscribe(id =>
            {
                Console.WriteLine($"新区块订阅 ID: {id}");
            });
        newBlockSubscription
            .GetSubscriptionDataResponsesAsObservable()
            .Subscribe(async blockHeader =>
            {
                var block = await web3.Eth.Blocks.GetBlockWithTransactionsByHash.SendRequestAsync(
                    blockHeader.BlockHash
                );

                foreach (var tx in block.Transactions)
                {
                    //if (
                    //    !string.IsNullOrEmpty(tx.To)
                    //    && tx.To.Equals(_toAddress, StringComparison.OrdinalIgnoreCase)
                    //)
                    {
                        Console.WriteLine(
                            $"[ETH 充值] 来自 {tx.From} 金额 {Web3.Convert.FromWei(tx.Value)} ETH 哈希 {tx.TransactionHash}"
                        );
                    }
                }
            });

        await client.StartAsync();
        _logger.LogInformation("充值监听后台任务已启动...");

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
