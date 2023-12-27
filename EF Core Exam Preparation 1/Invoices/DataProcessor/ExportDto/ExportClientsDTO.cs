using Invoices.Data.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Invoices.DataProcessor.ExportDto
{
    [XmlType("Client")]
    public class ExportClientsDTO
    {
        [XmlAttribute]
        public int InvoicesCount { get; set; }

        [Required]
        [MaxLength(25)]
        [MinLength(10)]
        [XmlElement("ClientName")]
        public string ClientName { get; set; }

        [Required]
        [MaxLength(15)]
        [MinLength(10)]
        [XmlElement("VatNumber")]
        public string VatNumber { get; set; }

        [XmlArray("Invoices")]
        public ExportClientsInvoicesDTO[] Invoices { get; set; }
    }
}
