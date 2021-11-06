using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Account.Business
{
    public class Account
    {
        private Task _processQueueTask;
        private readonly Dictionary<Guid, FundTransfer> _FundTransfers;
        private readonly Queue<Guid> _FundTransferQueue;

        private readonly Business.Services _services;
        public Account(Business.Services services)
        {
            _services = services;
            _FundTransfers = new Dictionary<Guid, FundTransfer>();
            _FundTransferQueue = new Queue<Guid>();
        }

        public Transaction CreateFundTransfer(FundTransfer fundTransferData)
        {
            var transaction = new Transaction();

            _FundTransfers.Add(transaction.transactionId, fundTransferData);
            _FundTransferQueue.Enqueue(transaction.transactionId);

            if(_processQueueTask?.IsCompleted ?? true)
                _processQueueTask = ProcessQueue();

            return transaction;
        }

        public class Transaction
        {
            public Transaction()
            {
                transactionId = System.Guid.NewGuid();
            }

            public Guid transactionId { get; set; }
        }

        public FundTransferStatus GetFundTransferStatus(Guid transactionId)
        {
            if (_FundTransfers.TryGetValue(transactionId, out FundTransfer value))
                return value.status;
            else
                return new FundTransferStatus(FundTransferStatus.StatusType.Error, "Transaction not found");
        }

        public class FundTransferStatus
        {
            public FundTransferStatus()
            {
                this.statusType = StatusType.InQueue;
            }

            public FundTransferStatus(StatusType statusType, string message)
            {
                this.statusType = statusType;
                this.message = message;
            }

            public enum StatusType
            {
                InQueue,
                Processing,
                Confirmed,
                Error
            }
            internal StatusType statusType { get; set; }
            public string status => statusType.ToString();
            public string message { get; set; }
        }

        public class FundTransfer
        {
            public FundTransfer()
            {
                status = new FundTransferStatus();
            }

            public string accountOrigin { get; set; }
            public string accountDestination { get; set; }
            public double value { get; set; }
            internal FundTransferStatus status { get; set; }
        }
        
        private async Task ProcessQueue() 
        {
            while(_FundTransferQueue.Any())
            {
                var transaction = _FundTransfers[_FundTransferQueue.Dequeue()];

                if(transaction.status.statusType != FundTransferStatus.StatusType.InQueue)
                    continue;

                transaction.status.statusType = FundTransferStatus.StatusType.Processing;

                try
                {
                    var accountOriginTask = _services.GetAccount(transaction.accountOrigin);
                    var accountDestinationTask = _services.GetAccount(transaction.accountDestination);

                    await Task.WhenAll(accountOriginTask, accountDestinationTask);

                    var accountOrigin = accountOriginTask.Result;
                    var accountDestination = accountDestinationTask.Result;

                    if (accountOrigin.Balance < transaction.value)
                        throw new Exception("No funds available on origin account");

                    var entry = new Services.Entry(accountOrigin, transaction.value, "Debit");
                    await _services.AddEntry(entry);

                    try
                    {
                        entry = new Services.Entry(accountDestination, transaction.value, "Credit");
                        await _services.AddEntry(entry);
                    }
                    catch
                    {
                        entry = new Services.Entry(accountOrigin, transaction.value, "Credit");
                        await _services.AddEntry(entry);
                        throw;
                    }

                    transaction.status.statusType = FundTransferStatus.StatusType.Confirmed;
                }
                catch (System.Exception ex)
                {
                    transaction.status.statusType = FundTransferStatus.StatusType.Error;
                    transaction.status.message = ex.Message;
                }
            }
        }
    }
}
