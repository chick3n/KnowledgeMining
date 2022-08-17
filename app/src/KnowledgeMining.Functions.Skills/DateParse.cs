using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using KnowledgeMining.Functions.Skills.Models;
using System.Linq;
using System.Text.RegularExpressions;

namespace KnowledgeMining.Functions.Skills
{
    public static class DateParse
    {
        private const string PATTERN = "^[0-9]{4}--[0-9]{1,2}-[0-9]{1,2}$";

        [FunctionName("dateParse")]
        public static async Task<IActionResult> RunDateParse(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] WebApiSkillRequest req,
            ILogger log)
        {
            var record = req.Values.FirstOrDefault();
            if (record == null)
                throw new ArgumentNullException(nameof(WebApiRequestRecord));

            var response = new WebApiSkillResponse();
            var responseRecord = new WebApiResponseRecord();

            responseRecord.RecordId = record.RecordId;
            record!.Data.TryGetValue("date", out var recordDate);

            responseRecord.Data["datetime"] = null;

            if (string.IsNullOrWhiteSpace(recordDate?.ToString()))
            {
                responseRecord.Warnings.Add(new WebApiErrorWarning
                {
                    Message = $"{nameof(DateParse)} - Error processing the request record {record.RecordId}: Date is null or empty."
                });
            }
            else
            {
                var stringDate = recordDate?.ToString() ?? string.Empty;

                if (Regex.Match(stringDate, PATTERN).Success)
                {
                    try
                    {
                        responseRecord.Data["datetime"] = ParseSitRepDate(stringDate);
                    } 
                    catch(Exception ex)
                    {
                        responseRecord.Warnings.Add(new WebApiErrorWarning
                        {
                            Message = $"{nameof(DateParse)} - Error parsing date: {ex.Message}"
                        });
                    }
                }
                else
                {
                    responseRecord.Warnings.Add(new WebApiErrorWarning
                    {
                        Message = $"{nameof(DateParse)} - Error processing the request record {record.RecordId}: {stringDate} is not recognized."
                    });
                }
            }

            response.Values.Add(responseRecord);
            return new OkObjectResult(response);
        }

        private static DateTimeOffset ParseSitRepDate(string date)
        {
            var values = date.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (!int.TryParse(values[0], out int year) ||
                !int.TryParse(values[1], out int month) ||
                !int.TryParse(values[2], out int day))
                throw new ArgumentException($"{date} is not in valid format ####--##?-##?");


            return new DateTimeOffset(new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc));
        }
    }
}
