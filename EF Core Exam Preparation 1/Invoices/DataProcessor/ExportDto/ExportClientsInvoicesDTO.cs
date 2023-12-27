using Invoices.Data.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace Invoices.DataProcessor.ExportDto
{
    [XmlType("Invoice")]
    public class ExportClientsInvoicesDTO
    {
        [Required]
        [Range(1000000000, 1500000000)]
        [XmlElement("InvoiceNumber")]
        public int InvoiceNumber { get; set; }

        [Required]
        [XmlElement("InvoiceAmount")]
        public decimal InvoiceAmount { get; set; }

        [Required]
        [XmlElement("DueDate")]
        public string DueDate { get; set; }

        [Required]
        [EnumDataType(typeof(CurrencyType))]
        public string Currency { get; set; }
    }
}