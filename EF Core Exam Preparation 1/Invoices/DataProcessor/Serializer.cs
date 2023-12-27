namespace Invoices.DataProcessor
{
    using Invoices.Data;
    using Invoices.DataProcessor.ExportDto;
    using Invoices.DataProcessor.ImportDto;
    using Microsoft.EntityFrameworkCore;
    using Newtonsoft.Json;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Xml.Serialization;

    public class Serializer
    {
        public static string ExportClientsWithTheirInvoices(InvoicesContext context, DateTime date)
        {
            //using Data Transfer Object Class to map it with Clients
            XmlSerializer serializer = new XmlSerializer(typeof(ExportClientsDTO[]), new XmlRootAttribute("Clients"));

            //using StringBuilder to gather all info in one string
            StringBuilder sb = new StringBuilder();

            // "using" automatically closes opened connections
            using var writer = new StringWriter(sb);

            var xns = new XmlSerializerNamespaces();

            //one way to display empty namespace in resulted file
            xns.Add(string.Empty, string.Empty);

            var clientsAndInvoices = context.Clients
                .Where(c => c.Invoices.Any(i => i.IssueDate > date))
                .Select(c => new ExportClientsDTO
                {
                    //using identical properties in order to map successfully
                    InvoicesCount = c.Invoices.Count,
                    ClientName = c.Name,
                    VatNumber = c.NumberVat,
                    Invoices = c.Invoices
                    .OrderBy(i => i.IssueDate)
                    .ThenByDescending(i => i.DueDate)
                    .Select(i => new ExportClientsInvoicesDTO
                    {
                        InvoiceNumber = i.Number,
                        InvoiceAmount = decimal.Parse(i.Amount.ToString("0.##")), //two transformations in order to reach needed format
                        DueDate = i.DueDate.ToString("d", CultureInfo.InvariantCulture), //using culture-independent format
                        Currency = i.CurrencyType.ToString()
                    })                  
                    .ToArray()
                })
                .OrderByDescending(c => c.InvoicesCount)
                .ThenBy(c => c.ClientName)
                .ToArray();

            //Serialize method needs file, TextReader object and namespace to convert/map
            serializer.Serialize(writer, clientsAndInvoices, xns);

            //explicitly closing connection in terms of reaching edge cases
            writer.Close();

            //using TrimEnd() to get rid of white spaces
            return sb.ToString();
        }

        public static string ExportProductsWithMostClients(InvoicesContext context, int nameLength)
        {
            //turning needed info about products into a collection using anonymous object
            //using less data
            var productsAndClients = context.Products
                .Where(c => c.ProductsClients.Any(pc => pc.Client.Name.Length >= nameLength))
                .Select(p => new
                {
                    Name = p.Name,
                    Price = decimal.Parse(p.Price.ToString("0.##")), //two transformations in order to reach needed format
                    Category = p.CategoryType.ToString(),
                    Clients = p.ProductsClients
                    .Where(pc => pc.Client.Name.Length >= nameLength)
                    .Select(c => new
                    {
                        Name = c.Client.Name,
                        NumberVat = c.Client.NumberVat
                    })
                    .OrderBy(c => c.Name)
                    .ToArray()
                })
                .OrderByDescending(c => c.Clients.Count())
                .ThenBy(p => p.Name)
                .Take(5)
                .ToArray();

            //Serialize method needs object to convert/map
	        //adding Formatting for better reading 
            return JsonConvert.SerializeObject(productsAndClients, Formatting.Indented);
        }
    }
}
