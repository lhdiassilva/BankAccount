using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Account.Business
{
    public class Services
    {
        private readonly HttpClient _client;
        public Services(HttpClient client)
        {
            _client = client;
        }

        public class Account
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("accountNumber")]
            public string AccountNumber { get; set; }

            [JsonPropertyName("balance")]
            public double Balance { get; set; }
        }   

        public async Task<Account> GetAccount(string accountNumber)
        {
            var responseMessage = await _client.GetAsync($"/api/account/{accountNumber}");

            if(!responseMessage.IsSuccessStatusCode)
            {
                if(responseMessage.StatusCode == System.Net.HttpStatusCode.NotFound)
                    throw new Exception($"Invalid account number: {accountNumber}");

                responseMessage.EnsureSuccessStatusCode();
            }
            
            var json = await responseMessage.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<Account>(json);
        }

        public class Entry
        {
            public Entry(Account account, double value, string type)
            {
                AccountNumber = account.AccountNumber;
                Value = value;
                Type = type;
            }

            [JsonPropertyName("accountNumber")]
            public string AccountNumber { get; set; }

            [JsonPropertyName("value")]
            public double Value { get; set; }

            [JsonPropertyName("type")]
            public string Type { get; set; }
        }

        public async Task AddEntry(Entry entry)
        {
            var content = new StringContent(JsonSerializer.Serialize(entry), System.Text.Encoding.UTF8, "application/json");
            var responseMessage = await _client.PostAsync("/api/account", content);
            
            responseMessage.EnsureSuccessStatusCode();
        }
    }
}