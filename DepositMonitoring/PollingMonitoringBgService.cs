using System.Numerics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;

namespace DepositMonitoring;

internal class PollingMonitoringBgService : BackgroundService
{
    // 需要的确认数
    private const int RequiredConfirmations = 12;

    // 扫块间隔（毫秒）
    private const int PollingIntervalMilliSeconds = 1000;

    // 记录上次处理到的块
    private static BigInteger lastProcessedBlock = 0;

    // ERC20 Token 合约地址（示例：BSC USDT）
    private const string TokenContractAddress = "0x55d398326f99059ff775485246999027b3197955";

    private readonly string _rpcUrl = string.Empty;
    private readonly string _toAddress = string.Empty;

    private readonly ILogger _logger;

    public PollingMonitoringBgService(
        IConfiguration configuration,
        ILogger<PollingMonitoringBgService> logger
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

                    if (
                        block == null
                        || block.Transactions == null
                        || block.Transactions.Length <= 0
                    )
                    {
                        continue;
                    }

                    foreach (var tx in block.Transactions)
                    {
                        if (string.IsNullOrEmpty(tx.To))
                        {
                            continue;
                        }

                        // 1️ 主币充值检测
                        if (tx.To.Equals(_toAddress, StringComparison.OrdinalIgnoreCase) && tx.Value.Value > 0)
                        {
                            var confirmations = (int)(latestBlockNumber - blockNumber + 1);
                            Console.WriteLine($"【主币充值】交易: {tx.TransactionHash}");
                            Console.WriteLine($"来自: {tx.From} -> {tx.To}");
                            Console.WriteLine($"金额: {Web3.Convert.FromWei(tx.Value)} BNB");
                            Console.WriteLine($"区块: {blockNumber}, 确认数: {confirmations}");
                            Console.WriteLine("--------------------------------------------------");
                        }

                        // 2️ Token充值检测（通过事件日志解析 Transfer）
                        //if (tx.To.Equals(TokenContractAddress, StringComparison.OrdinalIgnoreCase))
                        //{
                        //    var receipt =
                        //        await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(
                        //            tx.TransactionHash
                        //        );
                        //    if (receipt?.Logs == null)
                        //    {
                        //        continue;
                        //    }

                        //    foreach (var log in receipt.Logs)
                        //    {
                        //        if (log.Topics == null || log.Topics.Length != 3)
                        //        {
                        //            continue;
                        //        }

                        //        // ERC20 Transfer事件签名哈希
                        //        if (
                        //            log.Topics[0].ToString()
                        //            == "0xddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef"
                        //        )
                        //        {
                        //            var toAddress = string.Concat(
                        //                "0x",
                        //                log.Topics[2].ToString().AsSpan(26)
                        //            );
                        //            //if (toAddress.ToLower() == _toAddress)
                        //            {
                        //                var value = new BigInteger(log.Data.HexToByteArray());
                        //                var confirmations = (int)(
                        //                    latestBlockNumber - blockNumber + 1
                        //                );

                        //                Console.WriteLine(
                        //                    $"【Token充值】交易: {tx.TransactionHash}"
                        //                );
                        //                Console.WriteLine($"Token合约: {TokenContractAddress}");
                        //                Console.WriteLine($"来自: {tx.From} -> {toAddress}");
                        //                Console.WriteLine(
                        //                    $"金额: {Web3.Convert.FromWei(value)} USDT"
                        //                );
                        //                Console.WriteLine(
                        //                    $"区块: {blockNumber}, 确认数: {confirmations}"
                        //                );
                        //                Console.WriteLine(
                        //                    "-----------------------------------------------"
                        //                );
                        //            }
                        //        }
                        //    }
                        //}
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
