using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Account.WebAPI.Controllers
{
    public class LogAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext actionExecutingContext)
        {
            Console.WriteLine($"Request {actionExecutingContext.ActionDescriptor.DisplayName} em {DateTime.Now.ToShortDateString()} às {DateTime.Now.ToShortTimeString()}");
        }

        public override void OnActionExecuted(ActionExecutedContext actionExecutedContext)
        {
        }
    }

    [ApiController]
    [Route("[controller]")]
    [Log]
    public class FundTransferController : ControllerBase
    {
        private readonly Business.Account _account;
        public FundTransferController(Business.Account account)
        {
            _account = account;
        }

        /// <summary>
        /// Create a transfer transaction
        /// </summary>
        /// <param name="fundTransferData">Account origin, account destination and value</param>
        /// <returns>Transaction Id</returns>
        [HttpPost]
        public Business.Account.Transaction Post(Business.Account.FundTransfer fundTransferData)
        {
            return _account.CreateFundTransfer(fundTransferData);
        }

        /// <summary>
        /// Returns the transaction status
        /// </summary>
        /// <param name="transactionId">Transaction Id</param>
        /// <returns>Transfer transaction status and message in case of error</returns>
        [HttpGet]
        public Business.Account.FundTransferStatus Get(Guid transactionId)
        {
            return _account.GetFundTransferStatus(transactionId);
        }
    }
}
