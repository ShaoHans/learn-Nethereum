using Microsoft.Extensions.Configuration;
using Nethereum.HdWallet;
using Nethereum.Web3;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>()
    .Build();

var wallet = new Wallet(configuration["Ethereum:Words"], "");
var account = wallet.GetAccount(1);
Console.WriteLine(account.Address);
var web3 = new Web3(account, configuration["Ethereum:NodeUrl"]);
var balance = await web3.Eth.GetBalance.SendRequestAsync(account.Address);
Console.WriteLine(balance);