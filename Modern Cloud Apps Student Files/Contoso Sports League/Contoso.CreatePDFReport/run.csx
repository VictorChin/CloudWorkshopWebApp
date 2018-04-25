#load "StorageMethods.csx"
#load "ViewModels.csx"
#load "CreatePdfReport.csx"
#r "Newtonsoft.Json"


using Newtonsoft.Json;
using System;
using System.Net;

public static async Task<object> Run(HttpRequestMessage req, TraceWriter log)
{
    OrderViewModel Order = null;

    try
    {
        string jsonContent = await req.Content.ReadAsStringAsync();
        dynamic payload = JsonConvert.DeserializeObject(jsonContent);
        if (payload != null && payload.Order != null)
        {
            var base64EncodedData = payload.Order.Value;
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            Order = JsonConvert.DeserializeObject<OrderViewModel>(System.Text.Encoding.UTF8.GetString(base64EncodedBytes));
            
        }

        if (Order == null || Order.OrderId <= 0)
        {
            return req.CreateResponse(HttpStatusCode.BadRequest, new
            {
                error = "Please pass an Order property in the input object containing a Base64 representation of the Order to process. Example: { \"Order\": \"eyAiT3JkZXJJZCIgOiAiMzgiLCAiT3JkZXJEYXRlIiA6ICIyMDE3LTAzLTEwVDE5OjQ4OjAyLjZaIiwgIkZpcnN0TmFtZSIgOiAiQm9iIiwgIkxhc3ROYW1lIiA6ICJMb2JsYXciLCAiQWRkcmVzcyIgOiAiMTMxMyBNb2NraW5nYmlyZCBMYW5lIiwgIkNpdHkiIDogIlZpcmdpbmlhIEJlYWNoIiwgIlN0YXRlIiA6ICJWQSIsICJQb3N0YWxDb2RlIiA6ICIyMzQ1NiIsICJDb3VudHJ5IiA6ICJVbml0ZWQgU3RhdGVzIiwgIlBob25lIiA6ICI1NTUxMjM0NTY3OCIsICJFbWFpbCIgOiAiYm9ibG9ibGF3QGNvbnRvc29zcG9ydHNsZWFndWUuY29tIiwgIlRvdGFsIiA6ICI4NzkuNDUiIH0=\" }"
            });
        }

        log.Info($"Webhook was triggered! Order: {Order.OrderId} ");
    
        return req.CreateResponse(HttpStatusCode.OK, await Task.Run(() => ProcessOrder(Order, log)));
    }
    catch (Exception ex)
    {
        return req.CreateResponse(HttpStatusCode.InternalServerError, new
        {
            error = string.Format("Error Processing Order: {0}", ex.Message)
        });
    }
}

static OrderViewModel ProcessOrder(OrderViewModel Order, TraceWriter log)
{
    string fileName = string.Format("ContosoSportsLeague-Store-Receipt-{0}.pdf", Order.OrderId);
    log.Info($"Using Filename {fileName}");

    var receipt = CreatePdfReport(Order, fileName,log);
    log.Info("PDF generated. Saving to blob storage...");
    Order.ReceiptUrl = UploadPdfToBlob(receipt, fileName, log);
        log.Info($"Using Order.ReceiptUrl {Order.ReceiptUrl}");
    return Order;
}