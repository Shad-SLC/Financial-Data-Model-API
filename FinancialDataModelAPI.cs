using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System;
using System.Data.SqlClient;

namespace FinancialDataModelAPI
{
    public class ValidateCostCode
    {
        private readonly ILogger<ValidateCostCode> _logger;

        public ValidateCostCode(ILogger<ValidateCostCode> log)
        {
            _logger = log;
        }

        [FunctionName("ValidateProjectCode")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "Validate Cost Codes" })]
        //[OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(CCObject), Description = "Request Body. Requires both Fund and Project Code", Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(string), Description = "Improper Request Body.")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "Validate")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string? fund = data.fund;
            string? projectCode = data.projectCode;

            if ((fund == null || projectCode == null)||(fund == "" || projectCode == ""))
            {
                return new BadRequestObjectResult("Request Body must include fund and project code and cannot be blank.");
            }

            RequestSingleObject response = new RequestSingleObject();

            string responseMessage = $"The fund is {fund} and the Project Code is {projectCode}";

            CCObjectValidated costCode = new CCObjectValidated();

            costCode.Fund = fund;
            costCode.ProjectCode = projectCode;

            //Connect to SQL Database

            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
            builder.DataSource = "slcise2";
            builder.UserID = "SLC_CashSystem_PaymentWCF";
            builder.Password = "#8tvqEzz9A";
            builder.InitialCatalog = "CashManagement";

            //using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
            //{

            //    string sql = "SELECT [cost_center] FROM [dbo].[cash_summary]";

            //    using (SqlCommand command = new SqlCommand(sql, connection))
            //    {
            //        connection.Open();
            //        using(SqlDataReader reader = command.ExecuteReader())
            //        {

            //            while (reader.Read())
            //            {
            //                Console.WriteLine(reader.GetString(0));
            //            }

            //        }
            //    }
            //}

           //Test for validity. Change later
           if (fund != null && projectCode != null)
            {
              costCode.isValid = true;
            }

            response.responseMessage = responseMessage;
            response.CCObject = costCode;

            return new OkObjectResult(response);
        }

        [FunctionName("ValidateProjectList")]
        [OpenApiOperation(operationId: "RunList", tags: new[] { "Validate Cost Code List" })]
        //[OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(List<CCObject>), Description = "Request Body. Requires list of objects including fund and Project Code", Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(string), Description = "Improper Request Body.")]
        public async Task<IActionResult> RunList(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "ValidateList")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject<List<CCObject>>(requestBody);
            CCListObjectValidated validatedList = new CCListObjectValidated();

            foreach (CCObject obj in data)
            {

                if ((obj.Fund == null || obj.ProjectCode == null) || (obj.Fund == "" || obj.ProjectCode == ""))
                {
                    return new BadRequestObjectResult("Request Body must include fund and project code for each list entry and cannot be blank.");
                }

                CCObjectValidated newObj = new CCObjectValidated();
                newObj.Fund = obj.Fund;
                newObj.ProjectCode = obj.ProjectCode;

                //Validate the code here!!!
                newObj.isValid = true;

                validatedList.costCenterCodes.Add(newObj);

            }

            return new OkObjectResult(validatedList);
        }

    }

    public class RequestSingleObject
    {
        public string responseMessage { get; set; }
        public CCObjectValidated CCObject { get; set; }        
    }

    public class CCObject
    {
        public string Fund { get; set; }
        public string ProjectCode { get; set; }

    }

    public class CCObjectValidated
    {
        public string Fund { get; set; }
        public string ProjectCode { get; set; }
        public bool isValid { get; set; }
    }

    public class CCListObjectValidated
    {
        public List<CCObjectValidated> costCenterCodes { get; set; } = new List<CCObjectValidated>();
    }
}

