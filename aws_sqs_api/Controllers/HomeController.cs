using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace aws_sqs_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : Controller
    {
        IAmazonSQS sqsClient { get; set; }
        private readonly IConfiguration configuration;
        private readonly IConfigurationSection sqsConfig;
        public HomeController(IConfiguration configuration,IAmazonSQS sqsClient)
        {
            this.configuration = configuration;
            sqsConfig = configuration.GetSection("AwsSqsQueueUrl");
            this.sqsClient = sqsClient;
        }

       [HttpPost("SendSqsMsg/{QueueName}/{Message}/{PostedBy}")]
       public async Task<int> SendSqsMsg(string QueueName,string Message, string PostedBy)
        {
            var queueUrl = sqsConfig.GetSection(QueueName).Value;
            // int delaySeconds = 0;
            var request = new SendMessageRequest
            {
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    {"PostedBy",new MessageAttributeValue{DataType="String",StringValue=PostedBy} }
                },
                MessageBody = Message,
                QueueUrl = queueUrl
            };
            var response = await sqsClient.SendMessageAsync(request);
            return (int)response.HttpStatusCode;
        }

        [HttpPost("ReadSqsMsg/{QueueName}")]
        public async Task<List<string>> ReadSqsMsg(string QueueName)
        {
            var queueUrl = sqsConfig.GetSection(QueueName).Value;
            var messageList = new List<string>();
            var receiveMsgRequest = new ReceiveMessageRequest();
            receiveMsgRequest.QueueUrl = queueUrl;
            receiveMsgRequest.MaxNumberOfMessages = 1;

            var response = await sqsClient.ReceiveMessageAsync(receiveMsgRequest);
            if (response.Messages.Any())
            {
                foreach(var msg in response.Messages)
                {
                    messageList.Add(msg.Body);
                    var deleteMsgRequest = new DeleteMessageRequest();
                    deleteMsgRequest.QueueUrl = queueUrl;
                    deleteMsgRequest.ReceiptHandle = msg.ReceiptHandle;
                    var result = sqsClient.DeleteMessageAsync(deleteMsgRequest).Result;
                }
            }
            return messageList;
        }

        public async Task<int> DeleteSqsQueue(string QueueName)
        {
            var queueUrl = sqsConfig.GetSection(QueueName).Value;
            var request = new DeleteQueueRequest
            {
                QueueUrl = queueUrl
            };
            var response = await sqsClient.DeleteQueueAsync(request);
            return (int)response.HttpStatusCode;
        }
    }
}