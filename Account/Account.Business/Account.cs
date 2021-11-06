using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Account.Business
{
    public class Account
    {
        private readonly Dictionary<Guid, FundTransfer> _fundTransfers;
        private readonly Queue<Guid> _fundTransferQueue;
        private Task _processQueueTask;

        private readonly Business.Services _services;
        public Account(Business.Services services)
        {
            _services = services;
            _fundTransfers = new Dictionary<Guid, FundTransfer>();
            _fundTransferQueue = new Queue<Guid>();
        }

        public class Transaction
        {
            public Transaction()
            {
                TransactionId = System.Guid.NewGuid();
            }

            public Guid TransactionId { get; set; }
        }

        public class FundTransferStatus
        {
            public FundTransferStatus()
            {
                this.StatusType = StatusTypes.InQueue;
            }

            public FundTransferStatus(StatusTypes statusType, string message)
            {
                this.StatusType = statusType;
                this.Message = message;
            }

            public enum StatusTypes
            {
                InQueue,
                Processing,
                Confirmed,
                Error
            }
            
            internal StatusTypes StatusType { get; set; }
            public string Status => StatusType.ToString();
            public string Message { get; set; }
        }

        public class FundTransfer
        {
            public FundTransfer()
            {
                Status = new FundTransferStatus();
            }

            public string AccountOrigin { get; set; }
            public string AccountDestination { get; set; }
            public double Value { get; set; }
            internal FundTransferStatus Status { get; set; }
        }

        public Transaction CreateFundTransfer(FundTransfer fundTransferData)
        {
            var transaction = new Transaction();

            _fundTransfers.Add(transaction.TransactionId, fundTransferData);
            _fundTransferQueue.Enqueue(transaction.TransactionId);

            if (_processQueueTask?.IsCompleted ?? true)
                _processQueueTask = ProcessQueue();

            return transaction;
        }

        public FundTransferStatus GetFundTransferStatus(Guid transactionId)
        {
            if (_fundTransfers.TryGetValue(transactionId, out FundTransfer value))
                return value.Status;
            else
                return new FundTransferStatus(FundTransferStatus.StatusTypes.Error, "Transaction not found");
        }

        private async Task ProcessQueue()
        {
            while (_fundTransferQueue.Any())
            {
                var transaction = _fundTransfers[_fundTransferQueue.Dequeue()];

                if (transaction.Status.StatusType != FundTransferStatus.StatusTypes.InQueue)
                    continue;

                transaction.Status.StatusType = FundTransferStatus.StatusTypes.Processing;

                try
                {
                    if (transaction.Value <= 0)
                        throw new Exception("Transaction value must be a positive number");

                    var accountOriginTask = _services.GetAccount(transaction.AccountOrigin);
                    var accountDestinationTask = _services.GetAccount(transaction.AccountDestination);

                    await Task.WhenAll(accountOriginTask, accountDestinationTask);

                    var accountOrigin = accountOriginTask.Result;
                    var accountDestination = accountDestinationTask.Result;

                    if (accountOrigin.Balance < transaction.Value)
                        throw new Exception("No funds available in origin account");

                    var entry = new Services.Entry(accountOrigin, transaction.Value, "Debit");
                    await _services.AddEntry(entry);

                    try
                    {
                        entry = new Services.Entry(accountDestination, transaction.Value, "Credit");
                        await _services.AddEntry(entry);
                    }
                    catch
                    {
                        entry = new Services.Entry(accountOrigin, transaction.Value, "Credit");
                        await _services.AddEntry(entry);
                        throw;
                    }

                    transaction.Status.StatusType = FundTransferStatus.StatusTypes.Confirmed;
                }
                catch (System.Exception ex)
                {
                    transaction.Status.StatusType = FundTransferStatus.StatusTypes.Error;
                    transaction.Status.Message = ex.Message;
                }
            }
        }
    }
}
