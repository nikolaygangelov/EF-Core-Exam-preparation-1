namespace Invoices.DataProcessor
{
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using System.Text;
    using System.Xml.Serialization;
    using Invoices.Data;
    using Invoices.Data.Models;
    using Invoices.DataProcessor.ImportDto;
    using Newtonsoft.Json;

    public class Deserializer
    {
        private const string ErrorMessage = "Invalid data!";

        private const string SuccessfullyImportedClients
            = "Successfully imported client {0}.";

        private const string SuccessfullyImportedInvoices
            = "Successfully imported invoice with number {0}.";

        private const string SuccessfullyImportedProducts
            = "Successfully imported product - {0} with {1} clients.";


        public static string ImportClients(InvoicesContext context, string xmlString)
        {
            var serializer = new XmlSerializer(typeof(ImportClientsDTO[]), new XmlRootAttribute("Clients"));
            using StringReader inputReader = new StringReader(xmlString);
            var clientsArrayDTOs = (ImportClientsDTO[])serializer.Deserialize(inputReader);

            StringBuilder sb = new StringBuilder();
            List<Client> clientsXML = new List<Client>();

            foreach (ImportClientsDTO client in clientsArrayDTOs)
            {
                Client clientToAdd = new Client
                {
                    Name = client.Name,
                    NumberVat = client.NumberVat,
                };

                if (!IsValid(client))
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                foreach (var address in client.Addresses)
                {
                    if (!IsValid(address))
                    {
                        sb.AppendLine(ErrorMessage);
                        continue;
                    }

                    clientToAdd.Addresses.Add(new Address()
                    {
                        City = address.City,
                        Country = address.Country,
                        PostCode = address.PostCode,
                        StreetName = address.StreetName,
                        StreetNumber = address.StreetNumber
                    });
                }

                clientsXML.Add(clientToAdd);
                sb.AppendLine(string.Format(SuccessfullyImportedClients, client.Name));
            }

            context.Clients.AddRange(clientsXML);

            context.SaveChanges();

            return sb.ToString().TrimEnd();
        }


        public static string ImportInvoices(InvoicesContext context, string jsonString)
        {
            var invoicesArray = JsonConvert.DeserializeObject<ImportInvoicesDTO[]>(jsonString);

            StringBuilder sb = new StringBuilder();
            List<Invoice> invoices = new List<Invoice>();

            foreach (ImportInvoicesDTO invoice in invoicesArray)
            {

                if (!IsValid(invoice))
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                if (DateTime.Compare(invoice.IssueDate, invoice.DueDate) > 0)
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                Invoice invoiceToAdd = new Invoice
                {
                    Number = invoice.Number,
                    IssueDate = invoice.IssueDate,
                    DueDate = invoice.DueDate,
                    Amount = invoice.Amount,
                    CurrencyType = invoice.CurrencyType,
                    ClientId = invoice.ClientId
                };

                invoices.Add(invoiceToAdd);
                sb.AppendLine(string.Format(SuccessfullyImportedInvoices, invoice.Number));
            }

            context.Invoices.AddRange(invoices);
            context.SaveChanges();

            return sb.ToString().TrimEnd();
        }

        public static string ImportProducts(InvoicesContext context, string jsonString)
        {
            var productsArray = JsonConvert.DeserializeObject<ImportProductsDTO[]>(jsonString);

            StringBuilder sb = new StringBuilder();


            List<Product> products = new List<Product>();

            int[] uniqueClients = context.Clients
                .Select(c => c.Id)
                .ToArray();

            foreach (ImportProductsDTO product in productsArray)
            {
                Product productToAdd = new Product
                {
                    Name = product.Name,
                    Price = product.Price,
                    CategoryType = product.CategoryType
                };

                if (!IsValid(product))
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                foreach (int clientId in product.Clients.Distinct())
                {
                    if (!uniqueClients.Contains(clientId))
                    {
                        sb.AppendLine(ErrorMessage);
                        continue;
                    }

                    productToAdd.ProductsClients.Add(new ProductClient()
                    {
                        ClientId = clientId
                    });
                }


                products.Add(productToAdd);
                sb.AppendLine(string.Format(SuccessfullyImportedProducts, productToAdd.Name, productToAdd.ProductsClients.Count));
            }

            context.Products.AddRange(products);
            context.SaveChanges();

            return sb.ToString().TrimEnd();
        }

        public static bool IsValid(object dto)
        {
            var validationContext = new ValidationContext(dto);
            var validationResult = new List<ValidationResult>();

            return Validator.TryValidateObject(dto, validationContext, validationResult, true);
        }
    } 
}
