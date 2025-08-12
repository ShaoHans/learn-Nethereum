using System.Numerics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;

namespace PollingMonitoring;

internal class TransactionMonitoringBgService : BackgroundService
{
    // 需要的确认数
    private const int RequiredConfirmations = 12;

    // 扫块间隔（毫秒）
    private const int PollingIntervalMilliSeconds = 1000;

    // 记录上次处理到的块
    private static BigInteger lastProcessedBlock = 0;

    private readonly string _rpcUrl = string.Empty;
    private readonly string _toAddress = string.Empty;

    private readonly ILogger _logger;

    public TransactionMonitoringBgService(
        IConfiguration configuration,
        ILogger<TransactionMonitoringBgService> logger
    )
    {
        _logger = logger;
        _rpcUrl = configuration["Ethereum:NodeUrl"]!;
        _toAddress = configuration["Ethereum:ToAddress"]!;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var web3 = new Web3(_rpcUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var latestBlockNumber = (BigInteger)
                    await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
                _logger.LogInformation("最新区块号是：{@latestBlockNumber}", latestBlockNumber);

                if (lastProcessedBlock == 0)
                {
                    lastProcessedBlock = latestBlockNumber;
                }

                // 扫描新块（不包含还未达到确认数的块）
                var toBlock = latestBlockNumber - RequiredConfirmations;
                if (toBlock <= lastProcessedBlock)
                {
                    await Task.Delay(PollingIntervalMilliSeconds, stoppingToken);
                    continue;
                }

                for (
                    var blockNumber = lastProcessedBlock + 1;
                    blockNumber <= toBlock;
                    blockNumber++
                )
                {
                    var block =
                        await web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(
                            new BlockParameter((ulong)blockNumber)
                        );

                    _logger.LogInformation(
                        "正在处理区块: {@blockNumber}, 交易数: {@transactionCount}",
                        blockNumber,
                        block?.Transactions?.Length ?? 0
                    );

                    if (block?.Transactions != null)
                    {
                        foreach (
                            var tx in block.Transactions.Where(x =>
                                x.To != null
                                && x.To.Equals(_toAddress, StringComparison.OrdinalIgnoreCase)
                            )
                        )
                        {
                            var confirmations = (int)(latestBlockNumber - blockNumber + 1);
                            Console.WriteLine($"检测到充值交易: {tx.TransactionHash}");
                            Console.WriteLine($"来自: {tx.From} -> {tx.To}");
                            Console.WriteLine($"金额: {Web3.Convert.FromWei(tx.Value)} BNB");
                            Console.WriteLine(
                                $"所在区块: {blockNumber}, 当前确认数: {confirmations}"
                            );
                            Console.WriteLine("----------------------------------------------");
                        }
                    }
                }

                lastProcessedBlock = toBlock;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "轮询链上交易出现异常");
            }
            finally
            {
                await Task.Delay(PollingIntervalMilliSeconds, stoppingToken);
            }
        }
    }
}
