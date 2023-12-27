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
            //using Data Transfer Object Class to map it with Clients
            var serializer = new XmlSerializer(typeof(ImportClientsDTO[]), new XmlRootAttribute("Clients"));

            //Deserialize method needs TextReader object to convert/map 
            using StringReader inputReader = new StringReader(xmlString);
            var clientsArrayDTOs = (ImportClientsDTO[])serializer.Deserialize(inputReader);

            //using StringBuilder to gather all info in one string
            StringBuilder sb = new StringBuilder();

            //creating List where all valid clients can be kept
            List<Client> clientsXML = new List<Client>();

            foreach (ImportClientsDTO client in clientsArrayDTOs)
            {
                //validating info for client from data
                if (!IsValid(client))
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                //creating a valid client
                Client clientToAdd = new Client
                {
                    // using identical properties in order to map successfully
                    Name = client.Name,
                    NumberVat = client.NumberVat,
                };
                
                foreach (var address in client.Addresses)
                {
                    //validating info for address from data
                    if (!IsValid(address))
                    {
                        sb.AppendLine(ErrorMessage);
                        continue;
                    }

                    //adding valid address
                    clientToAdd.Addresses.Add(new Address()
                    {
                        // using identical properties in order to map successfully
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

            //actually importing info from data
            context.SaveChanges();

            //using TrimEnd() to get rid of white spaces
            return sb.ToString().TrimEnd();
        }


        public static string ImportInvoices(InvoicesContext context, string jsonString)
        {
            //using Data Transfer Object Class to map it with Invoices
            var invoicesArray = JsonConvert.DeserializeObject<ImportInvoicesDTO[]>(jsonString);

            //using StringBuilder to gather all info in one string
            StringBuilder sb = new StringBuilder();

            //creating List where all valid invoices can be kept
            List<Invoice> invoices = new List<Invoice>();

            foreach (ImportInvoicesDTO invoice in invoicesArray)
            {
                //validating info for invoice from data
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

                //creating a valid invoice
                Invoice invoiceToAdd = new Invoice
                {
                    // using identical properties in order to map successfully
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
            
            //actually importing info from data
            context.SaveChanges();

            //using TrimEnd() to get rid of white spaces
            return sb.ToString().TrimEnd();
        }

        public static string ImportProducts(InvoicesContext context, string jsonString)
        {
            //using Data Transfer Object Class to map it with products
            var productsArray = JsonConvert.DeserializeObject<ImportProductsDTO[]>(jsonString);

            //using StringBuilder to gather all info in one string
            StringBuilder sb = new StringBuilder();

            //creating List where all valid products can be kept
            List<Product> products = new List<Product>();

            // taking only unique clients
            int[] uniqueClients = context.Clients
                .Select(c => c.Id)
                .ToArray();

            foreach (ImportProductsDTO product in productsArray)
            {
                //validating info for product from data
                if (!IsValid(product))
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                //creating a valid product
                Product productToAdd = new Product
                {
                    // using identical properties in order to map successfully
                    Name = product.Name,
                    Price = product.Price,
                    CategoryType = product.CategoryType
                };

                foreach (int clientId in product.Clients.Distinct())
                {
                    //validating only unique clients
                    if (!uniqueClients.Contains(clientId))
                    {
                        sb.AppendLine(ErrorMessage);
                        continue;
                    }

                    //adding valid clients
                    productToAdd.ProductsClients.Add(new ProductClient()
                    {
                        ClientId = clientId
                    });
                }


                products.Add(productToAdd);
                sb.AppendLine(string.Format(SuccessfullyImportedProducts, productToAdd.Name, productToAdd.ProductsClients.Count));
            }

            context.Products.AddRange(products);

            //actually importing info from data
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
