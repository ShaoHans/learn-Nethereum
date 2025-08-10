using Microsoft.Extensions.Configuration;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;

/*
 1.前往 https://www.infura.io 网站注册账号，获取一个免费的节点服务地址和ApiKey。
   开发测试阶段可以使用免费的节点网络sepolia：https://sepolia.infura.io/v3/
 2.在MetaMask钱包中查看你的Ethereum Mainnet钱包地址
 3.前往 https://sepoliafaucet.com 网站领取测试ETH


| 金额       | Wei 表示法                        |
| -------- | ------------------------------ |
| 1 ETH    | 1000000000000000000 Wei (10¹⁸) |
| 0.01 ETH | 10000000000000000 Wei (10¹⁶)   |
| 1 Gwei   | 1000000000 Wei (10⁹)           |
Wei 是以太坊的最小单位
Gas 是“燃料费”的计量单位，以太坊上执行任何操作（转账、智能合约）都需要计算资源
Gas Price（Gas 价格），表示“每单位计算资源愿意付多少 Wei”，单位：Wei per Gas
比如：
    普通转账 → Gas Used = 21,000
    Gas Price = 30 Gwei = 30 × 10⁹ Wei
    交易费 = 21,000 × 30 × 10⁹ = 630,000,000,000,000 Wei ≈ 0.00063 ETH
 */

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>()
    .Build();

var account = new Account(configuration["Ethereum:PrivateKey"]);
var web3 = new Web3(account, configuration["Ethereum:NodeUrl"]);
var transaction = await web3
    .Eth.GetEtherTransferService()
    .TransferEtherAndWaitForReceiptAsync(configuration["Ethereum:ToAddress"], 0.01m, 2);
Console.WriteLine($"From:{transaction.From},Root:{transaction.Root}");
